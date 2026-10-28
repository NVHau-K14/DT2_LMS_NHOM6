using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// BangDiem – mọi lần học của MỘT sinh viên đã quy đổi (TK, chữ, hệ 4, đạt) + cờ tinhTichLuy.
/// Dùng cho: bảng điểm SV (S-02, A-06), nhận diện hình thức khi ghi danh (A-09).
///
///   1. $match          – lọc đúng sinh viên (dùng index ux_maSV).
///   2. $unwind         – tách mảng monHoc: mỗi lần học thành một document.   [barem: $unwind]
///   3–6. @quyDoi       – 4 stage quy đổi dùng chung ($cond, $switch).        [barem: $cond, $switch]
///   7. $setWindowFields – trong từng nhóm (maSV, maMon) sắp các lần học: hệ 4 ↓, TK ↓, năm học ↓, học kỳ ↓;
///                        lần đứng đầu là "lần học tốt nhất" (mục 3.3). maDKTotNhat = $first của cả nhóm
///                        (window unbounded). Không dùng $documentNumber vì MongoDB chỉ cho toán tử này
///                        đi với sortBy MỘT khóa, còn ở đây cần 4 khóa – kết quả tương đương "số thứ tự = 1".
///   8. $project        – trả phẳng các trường; tinhTichLuy = (là lần tốt nhất) VÀ (dat = true). [barem: $project]
///   9. $sort           – theo năm học, học kỳ, mã môn.
/// </summary>
public static class BangDiemPipeline
{
    public const string Json = """
        [
          { "$match": { "maSV": "@maSV" } },
          { "$unwind": "$monHoc" },
          "@quyDoi",
          { "$setWindowFields": {
              "partitionBy": { "maSV": "$maSV", "maMon": "$monHoc.maMon" },
              "sortBy": { "monHoc.diemHe4": -1, "monHoc.diemTK": -1, "monHoc.namHoc": -1, "monHoc.hocKy": -1 },
              "output": { "maDKTotNhat": { "$first": "$monHoc.maDK",
                                           "window": { "documents": [ "unbounded", "unbounded" ] } } } } },
          { "$project": {
              "_id": 0,
              "maSV": 1,
              "maDK": "$monHoc.maDK",
              "maMon": "$monHoc.maMon",
              "tenMon": "$monHoc.tenMon",
              "soTinChi": "$monHoc.soTinChi",
              "namHoc": "$monHoc.namHoc",
              "hocKy": "$monHoc.hocKy",
              "lanHoc": "$monHoc.lanHoc",
              "hinhThuc": "$monHoc.hinhThuc",
              "giangVien": "$monHoc.giangVien",
              "trongSo": "$monHoc.trongSo",
              "diem": "$monHoc.diem",
              "capNhatLuc": "$monHoc.capNhatLuc",
              "capNhatBoi": "$monHoc.capNhatBoi",
              "duDiem": "$monHoc.duDiem",
              "diemTK": "$monHoc.diemTK",
              "diemChu": "$monHoc.diemChu",
              "diemHe4": "$monHoc.diemHe4",
              "dat": "$monHoc.dat",
              "tinhTichLuy": { "$and": [
                  { "$eq": [ "$monHoc.maDK", "$maDKTotNhat" ] },
                  { "$eq": [ "$monHoc.dat", true ] } ] } } },
          { "$sort": { "namHoc": 1, "hocKy": 1, "maMon": 1 } }
        ]
        """;

    public static BsonDocument[] Tao(string maSV)
        => PipelineJson.Tao(Json, new Dictionary<string, BsonValue> { ["maSV"] = maSV });
}
