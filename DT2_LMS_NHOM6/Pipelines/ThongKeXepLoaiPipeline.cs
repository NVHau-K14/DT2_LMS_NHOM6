using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// ThongKeXepLoai – số sinh viên theo từng xếp loại (kèm chi tiết theo lớp). Dùng cho biểu đồ A-01, thẻ số A-10.
///
///   1..n. @baoCaoHocLuc – toàn bộ pipeline BaoCaoHocLuc (mỗi SV một dòng có xepLoai).
///   n+1. $group  – đếm theo (xepLoai, maLop).                                        [barem: $group]
///   n+2. $sort   – theo maLop để mảng theoLop có thứ tự.
///   n+3. $group  – gom lại theo xepLoai: tổng soSV + mảng theoLop.
///   n+4. $project – thêm thuTu bằng $switch để sắp đúng thứ tự Xuất sắc → Chưa xếp loại. [barem: $project, $switch]
///   n+5. $sort   – theo thuTu.
/// </summary>
public static class ThongKeXepLoaiPipeline
{
    public const string Json = """
        [
          "@baoCaoHocLuc",
          { "$group": {
              "_id": { "xepLoai": "$xepLoai", "maLop": "$maLop" },
              "soSV": { "$sum": 1 } } },
          { "$sort": { "_id.maLop": 1 } },
          { "$group": {
              "_id": "$_id.xepLoai",
              "soSV": { "$sum": "$soSV" },
              "theoLop": { "$push": { "maLop": "$_id.maLop", "soSV": "$soSV" } } } },
          { "$project": {
              "_id": 0,
              "xepLoai": "$_id",
              "soSV": 1,
              "theoLop": 1,
              "thuTu": { "$switch": { "branches": [
                  { "case": { "$eq": [ "$_id", "Xuất sắc" ] }, "then": 1 },
                  { "case": { "$eq": [ "$_id", "Giỏi" ] }, "then": 2 },
                  { "case": { "$eq": [ "$_id", "Khá" ] }, "then": 3 },
                  { "case": { "$eq": [ "$_id", "Trung bình" ] }, "then": 4 },
                  { "case": { "$eq": [ "$_id", "Yếu" ] }, "then": 5 },
                  { "case": { "$eq": [ "$_id", "Kém" ] }, "then": 6 } ],
                  "default": 7 } } } },
          { "$sort": { "thuTu": 1 } }
        ]
        """;

    public static BsonDocument[] Tao(BoLocSinhVien boLoc, PhamViBaoCao phamVi)
        => PipelineJson.Tao(Json, new Dictionary<string, BsonValue>
        {
            ["baoCaoHocLuc"] = new BsonArray(BaoCaoHocLucPipeline.Tao(boLoc, phamVi))
        });
}
