using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// GpaHocKy – GPA từng học kỳ của sinh viên (mục 3.3): mọi lần học trong học kỳ có đủ điểm, KỂ CẢ F (F = 0).
/// Dùng cho: cuối mỗi khối học kỳ ở S-02, biểu đồ GPA theo học kỳ ở S-01.
///
///   1. $match      – bộ lọc SV (thường là một maSV).
///   2. $unwind     – mỗi lần học một document.                                  [barem: $unwind]
///   3–6. @quyDoi   – quy đổi điểm ($cond, $switch).                              [barem: $cond, $switch]
///   7. $group      – gom theo (maSV, namHoc, hocKy):                             [barem: $group, $cond]
///                    tinChiDangKy = Σ TC mọi lần học; tinChiDat = Σ TC lần đạt;
///                    tongDiemTC / tongTinChiGpa = Σ(hệ4 × TC) / Σ TC của lần học đủ điểm.
///   8–9. @gpaXepLoai – gpa làm tròn floor 2 chữ số, xepLoai bằng $switch.         [barem: $switch]
///   10. $project   – đưa khóa nhóm ra trường phẳng, bỏ trường trung gian.        [barem: $project]
///   11. $sort      – theo maSV, năm học, học kỳ (3 = hè, sau HK2).
/// </summary>
public static class GpaHocKyPipeline
{
    public const string Json = """
        [
          { "$match": "@boLoc" },
          { "$unwind": "$monHoc" },
          "@quyDoi",
          { "$group": {
              "_id": { "maSV": "$maSV", "namHoc": "$monHoc.namHoc", "hocKy": "$monHoc.hocKy" },
              "tinChiDangKy": { "$sum": "$monHoc.soTinChi" },
              "tinChiDat": { "$sum": { "$cond": [ { "$eq": [ "$monHoc.dat", true ] }, "$monHoc.soTinChi", 0 ] } },
              "tongDiemTC": { "$sum": { "$cond": [ "$monHoc.duDiem",
                  { "$multiply": [ "$monHoc.diemHe4", "$monHoc.soTinChi" ] }, 0 ] } },
              "tongTinChiGpa": { "$sum": { "$cond": [ "$monHoc.duDiem", "$monHoc.soTinChi", 0 ] } } } },
          "@gpaXepLoai",
          { "$project": {
              "_id": 0,
              "maSV": "$_id.maSV",
              "namHoc": "$_id.namHoc",
              "hocKy": "$_id.hocKy",
              "tinChiDangKy": 1,
              "tinChiDat": 1,
              "gpa": 1,
              "xepLoai": 1 } },
          { "$sort": { "maSV": 1, "namHoc": 1, "hocKy": 1 } }
        ]
        """;

    public static BsonDocument[] Tao(BoLocSinhVien boLoc)
        => PipelineJson.Tao(Json, new Dictionary<string, BsonValue> { ["boLoc"] = boLoc.DieuKien });
}
