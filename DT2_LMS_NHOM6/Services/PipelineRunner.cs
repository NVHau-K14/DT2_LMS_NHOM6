using System.Diagnostics;
using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Chạy pipeline trên collection sinhvien, đo thời gian và kèm chuỗi JSON pipeline.
/// Tương đương mongosh: db.sinhvien.aggregate([...]).
/// </summary>
public static class PipelineRunner
{
    public static async Task<KetQuaPipeline<T>> ChayAsync<T>(MongoContext db, BsonDocument[] pipeline)
    {
        var collection = db.Database.GetCollection<BsonDocument>(MongoContext.TenSinhVien);

        var dongHo = Stopwatch.StartNew();
        var cursor = await collection.AggregateAsync(PipelineDefinition<BsonDocument, T>.Create(pipeline));
        var duLieu = await cursor.ToListAsync();
        dongHo.Stop();

        return new KetQuaPipeline<T>
        {
            DuLieu = duLieu,
            PipelineJson = PipelineJson.ToJson(pipeline),
            ThoiGianMs = dongHo.Elapsed.TotalMilliseconds
        };
    }
}
