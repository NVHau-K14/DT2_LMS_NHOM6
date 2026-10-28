using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Infrastructure;

/// <summary>
/// Dựng chuỗi lệnh dạng mongosh (thiết kế mục 4.9) từ CHÍNH các BsonDocument filter / update / arrayFilters
/// đã dùng để chạy lệnh, kèm kết quả matched/modified. Chuỗi này hiện trong toast "Lệnh MongoDB đã chạy".
///
/// Ví dụ:
///   db.sinhvien.updateMany(
///     { "monHoc.maMon": "INT301" },
///     { $set: { "monHoc.$[e].tenMon": "Cơ sở dữ liệu NoSQL" } },
///     { arrayFilters: [ { "e.maMon": "INT301" } ] }
///   )
///   // matchedCount: 40, modifiedCount: 40
///
/// Tự viết bộ định dạng (thay vì ToJson của driver) để in đúng cú pháp mongosh:
/// ObjectId("..."), ISODate("..."), toán tử $set không cần ngoặc kép.
/// </summary>
public static class MongoCommandFormatter
{
    // Tên khóa viết trần được trong JavaScript (maSV, $set...); còn lại ("monHoc.$[e].tenMon") phải đặt trong ngoặc kép
    private static readonly Regex KhoaTran = new(@"^[A-Za-z_$][A-Za-z0-9_$]*$", RegexOptions.Compiled);

    /// <summary>db.&lt;collection&gt;.updateOne(filter, update[, { arrayFilters }]) + kết quả.</summary>
    public static string UpdateOne(string collection, BsonDocument filter, BsonDocument update,
        IEnumerable<BsonDocument>? arrayFilters = null, UpdateResult? ketQua = null)
        => LenhUpdate(collection, "updateOne", filter, update, arrayFilters, ketQua);

    /// <summary>db.&lt;collection&gt;.updateMany(filter, update[, { arrayFilters }]) + kết quả.</summary>
    public static string UpdateMany(string collection, BsonDocument filter, BsonDocument update,
        IEnumerable<BsonDocument>? arrayFilters = null, UpdateResult? ketQua = null)
        => LenhUpdate(collection, "updateMany", filter, update, arrayFilters, ketQua);

    /// <summary>Ghép nhiều lệnh (ví dụ lan truyền tên GV sang 3 collection) thành một chuỗi, cách nhau một dòng trống.</summary>
    public static string GhepLenh(IEnumerable<string> cacLenh) => string.Join("\n\n", cacLenh);

    /// <summary>
    /// db.&lt;collection&gt;.bulkWrite([ { updateOne: {...} }, ... ]) – chỉ in lệnh ĐẦU TIÊN + tổng số lệnh (thiết kế 4.9).
    /// Ví dụ:
    ///   db.sinhvien.bulkWrite([
    ///     { updateOne: { filter: {...}, update: {...} } },
    ///     // ... cùng dạng, tổng cộng 20 lệnh updateOne
    ///   ], { ordered: false })
    ///   // matchedCount: 20, modifiedCount: 20
    /// </summary>
    public static string BulkWriteUpdateOne(string collection, IReadOnlyList<LenhUpdateOne> cacLenh, bool ordered,
        BulkWriteResult? ketQua = null)
    {
        var sb = new StringBuilder();
        sb.Append("db.").Append(collection).Append(".bulkWrite([\n");
        if (cacLenh.Count > 0)
        {
            var dau = cacLenh[0];
            var lenh = new BsonDocument { { "filter", dau.Filter }, { "update", dau.Update } };
            if (dau.ArrayFilters is { Count: > 0 })
                lenh.Add("arrayFilters", new BsonArray(dau.ArrayFilters));
            sb.Append("  ").Append(DinhDang(new BsonDocument("updateOne", lenh)));
            if (cacLenh.Count > 1)
                sb.Append(",\n  // ... cùng dạng, tổng cộng ").Append(cacLenh.Count).Append(" lệnh updateOne");
        }
        sb.Append("\n], { ordered: ").Append(ordered ? "true" : "false").Append(" })");

        if (ketQua != null && ketQua.IsAcknowledged)
            sb.Append("\n// matchedCount: ").Append(ketQua.MatchedCount)
              .Append(", modifiedCount: ").Append(ketQua.ModifiedCount);
        return sb.ToString();
    }

    /// <summary>
    /// db.&lt;collection&gt;.insertMany([ {...}, ... ]) – chỉ in document ĐẦU TIÊN + tổng số document.
    /// Ví dụ: db.sinhvien.insertMany([ { maSV: "2001250101", ... }, // ... tổng cộng 5 document ], { ordered: true })
    /// </summary>
    public static string InsertMany(string collection, IReadOnlyList<BsonDocument> cacDocument, bool ordered)
    {
        var sb = new StringBuilder();
        sb.Append("db.").Append(collection).Append(".insertMany([\n");
        if (cacDocument.Count > 0)
        {
            sb.Append("  ").Append(DinhDang(cacDocument[0]));
            if (cacDocument.Count > 1)
                sb.Append(",\n  // ... cùng dạng, tổng cộng ").Append(cacDocument.Count).Append(" document");
        }
        sb.Append("\n], { ordered: ").Append(ordered ? "true" : "false").Append(" })");
        sb.Append("\n// insertedCount: ").Append(cacDocument.Count);
        return sb.ToString();
    }

    private static string LenhUpdate(string collection, string phuongThuc, BsonDocument filter, BsonDocument update,
        IEnumerable<BsonDocument>? arrayFilters, UpdateResult? ketQua)
    {
        var sb = new StringBuilder();
        sb.Append("db.").Append(collection).Append('.').Append(phuongThuc).Append("(\n");
        sb.Append("  ").Append(DinhDang(filter)).Append(",\n");
        sb.Append("  ").Append(DinhDang(update));

        var dsLoc = arrayFilters?.ToList();
        if (dsLoc is { Count: > 0 })
        {
            sb.Append(",\n  { arrayFilters: ")
              .Append(DinhDang(new BsonArray(dsLoc)))
              .Append(" }");
        }
        sb.Append("\n)");

        if (ketQua != null && ketQua.IsAcknowledged)
            sb.Append("\n// matchedCount: ").Append(ketQua.MatchedCount)
              .Append(", modifiedCount: ").Append(ketQua.ModifiedCount);
        return sb.ToString();
    }

    /// <summary>In một BsonValue theo cú pháp mongosh, trên một dòng.</summary>
    public static string DinhDang(BsonValue giaTri)
    {
        switch (giaTri)
        {
            case BsonDocument doc:
                if (doc.ElementCount == 0) return "{}";
                return "{ " + string.Join(", ", doc.Select(e => $"{TenKhoa(e.Name)}: {DinhDang(e.Value)}")) + " }";
            case BsonArray mang:
                if (mang.Count == 0) return "[]";
                return "[ " + string.Join(", ", mang.Select(DinhDang)) + " ]";
            case BsonString s:
                return JsonSerializer.Serialize(s.Value, CaiDatChuoi);
            case BsonObjectId oid:
                return $"ObjectId(\"{oid.Value}\")";
            case BsonDateTime ngay:
                return $"ISODate(\"{ngay.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}\")";
            case BsonNull:
                return "null";
            case BsonBoolean b:
                return b.Value ? "true" : "false";
            case BsonDouble d:
                return d.Value.ToString("R", CultureInfo.InvariantCulture);
            case BsonInt32 or BsonInt64:
                return giaTri.ToString()!;
            default:
                return giaTri.ToString()!;
        }
    }

    // Giữ nguyên chữ tiếng Việt có dấu trong chuỗi (không đổi thành \uXXXX)
    private static readonly JsonSerializerOptions CaiDatChuoi = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static string TenKhoa(string ten)
        => KhoaTran.IsMatch(ten) ? ten : JsonSerializer.Serialize(ten, CaiDatChuoi);
}

/// <summary>Một lệnh updateOne trong bulkWrite: dùng chung để CHẠY (UpdateOneModel) và để IN chuỗi lệnh.</summary>
public record LenhUpdateOne(BsonDocument Filter, BsonDocument Update, List<BsonDocument>? ArrayFilters = null)
{
    public UpdateOneModel<BsonDocument> TaoModel()
    {
        var model = new UpdateOneModel<BsonDocument>(Filter, Update);
        if (ArrayFilters is { Count: > 0 })
            model.ArrayFilters = ArrayFilters.Select(f => (ArrayFilterDefinition)new BsonDocumentArrayFilterDefinition<BsonDocument>(f)).ToList();
        return model;
    }
}
