using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// LopHocPhanCuaGV – các lớp học phần của một giảng viên. Lớp học phần = nhóm (maMon, namHoc, hocKy, maGV),
/// có thể gồm SV nhiều lớp sinh hoạt. Dùng cho G-01, G-02, đếm lớp học phần ở A-07.
///
///   1. $match   – SV có ít nhất một lần học thỏa điều kiện ($elemMatch, dùng index ix_monHoc_maGV_namHoc_hocKy).
///   2. $unwind  – mỗi lần học một document.                                         [barem: $unwind]
///   3. $match   – lọc lại đúng các lần học của giảng viên (bỏ lần học môn khác của cùng SV).
///   4–7. @quyDoi – quy đổi điểm, cần duDiem để đếm SV đã đủ điểm.                   [barem: $cond, $switch]
///   8. $group   – theo (maMon, namHoc, hocKy): siSo, soDuDiem ($cond), cacLop ($addToSet). [barem: $group]
///   9. $project – trường phẳng, cacLop sắp tăng dần.                                [barem: $project]
///   10. $sort   – học kỳ mới nhất trước, rồi mã môn.
/// </summary>
public static class LopHocPhanCuaGVPipeline
{
    public const string Json = """
        [
          { "$match": { "monHoc": { "$elemMatch": "@dieuKienTrongPhanTu" } } },
          { "$unwind": "$monHoc" },
          { "$match": "@dieuKienSauUnwind" },
          "@quyDoi",
          { "$group": {
              "_id": { "maMon": "$monHoc.maMon", "namHoc": "$monHoc.namHoc", "hocKy": "$monHoc.hocKy" },
              "tenMon": { "$first": "$monHoc.tenMon" },
              "soTinChi": { "$first": "$monHoc.soTinChi" },
              "trongSo": { "$first": "$monHoc.trongSo" },
              "siSo": { "$sum": 1 },
              "soDuDiem": { "$sum": { "$cond": [ "$monHoc.duDiem", 1, 0 ] } },
              "cacLop": { "$addToSet": "$maLop" } } },
          { "$project": {
              "_id": 0,
              "maMon": "$_id.maMon",
              "namHoc": "$_id.namHoc",
              "hocKy": "$_id.hocKy",
              "tenMon": 1,
              "soTinChi": 1,
              "trongSo": 1,
              "siSo": 1,
              "soDuDiem": 1,
              "cacLop": { "$sortArray": { "input": "$cacLop", "sortBy": 1 } } } },
          { "$sort": { "namHoc": -1, "hocKy": -1, "maMon": 1 } }
        ]
        """;

    /// <summary>namHoc/hocKy null → mọi học kỳ.</summary>
    public static BsonDocument[] Tao(string maGV, string? namHoc = null, int? hocKy = null)
    {
        var dieuKien = DieuKienLanHoc.Tao(maGV, null, namHoc, hocKy);
        return PipelineJson.Tao(Json, new Dictionary<string, BsonValue>
        {
            ["dieuKienTrongPhanTu"] = dieuKien.TrongPhanTu,
            ["dieuKienSauUnwind"] = dieuKien.SauUnwind
        });
    }
}
