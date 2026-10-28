using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// BaoCaoHocLuc – mỗi sinh viên một dòng: gpa, xepLoai, tinChiTichLuy, soMonNo.
/// Dùng cho: A-10, G-04, A-03, A-04, S-01 và làm nền cho ThongKeXepLoai, GpaTheoLop.
///
/// Phạm vi TÍCH LŨY (mục 3.3): mỗi maMon lấy LẦN HỌC TỐT NHẤT; GPA chỉ tính các môn mà lần tốt nhất đạt.
/// Phạm vi MỘT HỌC KỲ: chỉ giữ lần học của học kỳ đó; GPA tính mọi lần học đủ điểm, kể cả F
/// (trùng định nghĩa GPA học kỳ); tinChiTichLuy = TC đạt trong kỳ, soMonNo = số môn F trong kỳ.
///
///   1. $match       – bộ lọc SV (lớp / danh sách MSSV / tất cả).
///   2. $addFields   – $filter giữ lại các lần học thuộc phạm vi (tích lũy: giữ hết).
///   3. $unwind      – preserveNullAndEmptyArrays: true → SV không có lần học nào vẫn còn 1 dòng,
///                     cuối cùng ra "Chưa xếp loại", gpa null, TC 0.                [barem: $unwind]
///   4–7. @quyDoi    – quy đổi điểm ($cond, $switch).                              [barem: $cond, $switch]
///   8. $sort        – theo maSV, maMon, rồi hệ 4 ↓, TK ↓, năm học ↓, học kỳ ↓ (lần chưa đủ điểm có hệ 4 null → xếp cuối).
///   9. $group       – theo (maSV, maMon), lấy $first = lần học tốt nhất.          [barem: $group]
///   10. $group      – theo maSV, cộng dồn bằng $cond:                             [barem: $group, $cond]
///                     tongDiemTC, tongTinChiGpa (điều kiện "@dieuKienTinhGpa"), tinChiTichLuy, soMonNo
///                     (môn nợ = lần tốt nhất đủ điểm mà vẫn không đạt ⇔ chưa lần nào đạt).
///   11–12. @gpaXepLoai – gpa làm tròn floor, xepLoai bằng $switch.                [barem: $switch]
///   13. $project    – trả về đúng các cột báo cáo.                                [barem: $project]
///   14. $sort       – theo maSV.
/// </summary>
public static class BaoCaoHocLucPipeline
{
    public const string Json = """
        [
          { "$match": "@boLoc" },
          { "$addFields": { "monHoc": { "$filter": {
              "input": "$monHoc",
              "as": "lan",
              "cond": "@locLanHoc" } } } },
          { "$unwind": { "path": "$monHoc", "preserveNullAndEmptyArrays": true } },
          "@quyDoi",
          { "$sort": { "maSV": 1, "monHoc.maMon": 1,
                       "monHoc.diemHe4": -1, "monHoc.diemTK": -1, "monHoc.namHoc": -1, "monHoc.hocKy": -1 } },
          { "$group": {
              "_id": { "maSV": "$maSV", "maMon": "$monHoc.maMon" },
              "hoTen": { "$first": "$hoTen" },
              "maLop": { "$first": "$maLop" },
              "soTinChi": { "$first": "$monHoc.soTinChi" },
              "diemHe4": { "$first": "$monHoc.diemHe4" },
              "duDiem": { "$first": "$monHoc.duDiem" },
              "dat": { "$first": "$monHoc.dat" } } },
          { "$group": {
              "_id": "$_id.maSV",
              "hoTen": { "$first": "$hoTen" },
              "maLop": { "$first": "$maLop" },
              "tongDiemTC": { "$sum": { "$cond": [ "@dieuKienTinhGpa",
                  { "$multiply": [ "$diemHe4", "$soTinChi" ] }, 0 ] } },
              "tongTinChiGpa": { "$sum": { "$cond": [ "@dieuKienTinhGpa", "$soTinChi", 0 ] } },
              "tinChiTichLuy": { "$sum": { "$cond": [ { "$eq": [ "$dat", true ] }, "$soTinChi", 0 ] } },
              "soMonNo": { "$sum": { "$cond": [
                  { "$and": [ { "$eq": [ "$duDiem", true ] }, { "$eq": [ "$dat", false ] } ] }, 1, 0 ] } } } },
          "@gpaXepLoai",
          { "$project": {
              "_id": 0,
              "maSV": "$_id",
              "hoTen": 1,
              "maLop": 1,
              "gpa": 1,
              "xepLoai": 1,
              "tinChiTichLuy": 1,
              "soMonNo": 1 } },
          { "$sort": { "maSV": 1 } }
        ]
        """;

    public static BsonDocument[] Tao(BoLocSinhVien boLoc, PhamViBaoCao phamVi)
    {
        // Điều kiện giữ lần học trong $filter ($$lan = phần tử đang xét của mảng monHoc)
        BsonValue locLanHoc = phamVi.LaTichLuy
            ? true
            : new BsonDocument("$and", new BsonArray
            {
                new BsonDocument("$eq", new BsonArray { "$$lan.namHoc", phamVi.NamHoc }),
                new BsonDocument("$eq", new BsonArray { "$$lan.hocKy", phamVi.HocKy!.Value })
            });

        // Lần học (tốt nhất) nào được cộng vào GPA:
        //   tích lũy → chỉ lần đạt: { $eq: ["$dat", true] }
        //   học kỳ   → mọi lần đủ điểm, kể cả F: { $eq: ["$duDiem", true] }
        BsonValue dieuKienTinhGpa = new BsonDocument("$eq",
            new BsonArray { phamVi.LaTichLuy ? "$dat" : "$duDiem", true });

        return PipelineJson.Tao(Json, new Dictionary<string, BsonValue>
        {
            ["boLoc"] = boLoc.DieuKien,
            ["locLanHoc"] = locLanHoc,
            ["dieuKienTinhGpa"] = dieuKienTinhGpa
        });
    }
}
