using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// GpaTheoLop – mỗi lớp sinh hoạt: sĩ số, số SV đã có GPA, GPA tích lũy trung bình. Dùng cho biểu đồ A-01.
/// GPA trung bình = trung bình cộng GPA tích lũy (đã làm tròn) của các SV có GPA ($avg bỏ qua null),
/// làm tròn 2 chữ số bằng cùng công thức floor.
///
///   1..n. @baoCaoHocLuc – BaoCaoHocLuc toàn trường, phạm vi tích lũy.
///   n+1. $group   – theo maLop: siSo, soSVCoGpa ($cond), gpaTB ($avg).   [barem: $group, $cond]
///   n+2. $project – làm tròn gpaTrungBinh (null nếu cả lớp chưa có GPA). [barem: $project, $cond]
///   n+3. $sort    – theo maLop.
/// </summary>
public static class GpaTheoLopPipeline
{
    public const string Json = """
        [
          "@baoCaoHocLuc",
          { "$group": {
              "_id": "$maLop",
              "siSo": { "$sum": 1 },
              "soSVCoGpa": { "$sum": { "$cond": [ { "$ne": [ "$gpa", null ] }, 1, 0 ] } },
              "gpaTB": { "$avg": "$gpa" } } },
          { "$project": {
              "_id": 0,
              "maLop": "$_id",
              "siSo": 1,
              "soSVCoGpa": 1,
              "gpaTrungBinh": { "$cond": [ { "$eq": [ "$gpaTB", null ] }, null,
                  { "$divide": [ { "$floor": { "$add": [ { "$multiply": [ "$gpaTB", 100 ] }, 0.5000001 ] } }, 100 ] } ] } } },
          { "$sort": { "maLop": 1 } }
        ]
        """;

    public static BsonDocument[] Tao()
        => PipelineJson.Tao(Json, new Dictionary<string, BsonValue>
        {
            ["baoCaoHocLuc"] = new BsonArray(BaoCaoHocLucPipeline.Tao(BoLocSinhVien.TatCa(), PhamViBaoCao.TichLuy()))
        });
}
