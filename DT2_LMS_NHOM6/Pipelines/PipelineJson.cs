using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// Chuyển pipeline viết dạng chuỗi JSON (C# raw string) thành BsonDocument[] để chạy.
///
/// Quy ước tham số: trong chuỗi JSON, giá trị dạng chuỗi "@ten" là chỗ giữ tham số
/// (ví dụ { "$match": { "maSV": "@maSV" } }) – được thay bằng BsonValue truyền vào,
/// KHÔNG ghép chuỗi nên không có lỗi chèn mã (injection).
///
/// Nếu "@ten" đứng trực tiếp làm một phần tử của mảng pipeline và tham số là BsonArray,
/// cả cụm stage được chèn vào đúng vị trí đó (dùng cho "@quyDoi", "@gpaXepLoai", "@baoCaoHocLuc").
/// Hai cụm "@quyDoi" và "@gpaXepLoai" luôn có sẵn – định nghĩa duy nhất ở QuyDoiDiemStages.
/// </summary>
public static class PipelineJson
{
    // Driver xuất JSON chuẩn (relaxed extended JSON: số in thẳng, không bọc NumberInt...)
    private static readonly JsonWriterSettings CaiDatJsonDriver = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    // System.Text.Json chỉ dùng để thụt lề cho dễ đọc; giữ nguyên chữ tiếng Việt và cách viết số (7.0 vẫn là 7.0)
    private static readonly JsonWriterOptions CaiDatThutLe = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Parse chuỗi JSON pipeline và thay tham số.</summary>
    public static BsonDocument[] Tao(string json, IDictionary<string, BsonValue>? thamSo = null)
    {
        var tatCaThamSo = new Dictionary<string, BsonValue>
        {
            ["quyDoi"] = new BsonArray(QuyDoiDiemStages.QuyDoi()),
            ["gpaXepLoai"] = new BsonArray(QuyDoiDiemStages.GpaXepLoai())
        };
        if (thamSo != null)
            foreach (var (ten, giaTri) in thamSo)
                tatCaThamSo[ten] = giaTri;

        var mangStage = BsonSerializer.Deserialize<BsonArray>(json);
        var pipeline = new List<BsonDocument>();
        foreach (var phanTu in mangStage)
        {
            if (LaThamSo(phanTu, out var ten))
            {
                // Chèn cả cụm stage dùng chung vào vị trí này
                foreach (var stage in LayThamSo(ten, tatCaThamSo).AsBsonArray)
                    pipeline.Add(ThayThamSo(stage, tatCaThamSo).AsBsonDocument);
            }
            else
            {
                pipeline.Add(ThayThamSo(phanTu, tatCaThamSo).AsBsonDocument);
            }
        }
        return pipeline.ToArray();
    }

    /// <summary>Chuỗi JSON đẹp (thụt lề) của pipeline – dùng cho "Xem pipeline" và file database/pipelines/*.json.</summary>
    public static string ToJson(IEnumerable<BsonDocument> pipeline)
    {
        var jsonGon = new BsonArray(pipeline).ToJson(CaiDatJsonDriver);

        using var tai = JsonDocument.Parse(jsonGon);
        using var bo = new MemoryStream();
        using (var ghi = new Utf8JsonWriter(bo, CaiDatThutLe))
            GhiPhanTu(tai.RootElement, ghi);
        return Encoding.UTF8.GetString(bo.ToArray());
    }

    /// <summary>
    /// Chép lại JSON, riêng số thực ghi dạng ngắn nhất mà vẫn đúng giá trị:
    /// driver in 0.5000001 thành 0.50000009999999995 (17 chữ số) – cùng một số double nhưng khó đọc.
    /// </summary>
    private static void GhiPhanTu(JsonElement phanTu, Utf8JsonWriter ghi)
    {
        switch (phanTu.ValueKind)
        {
            case JsonValueKind.Object:
                ghi.WriteStartObject();
                foreach (var thuocTinh in phanTu.EnumerateObject())
                {
                    ghi.WritePropertyName(thuocTinh.Name);
                    GhiPhanTu(thuocTinh.Value, ghi);
                }
                ghi.WriteEndObject();
                break;
            case JsonValueKind.Array:
                ghi.WriteStartArray();
                foreach (var con in phanTu.EnumerateArray())
                    GhiPhanTu(con, ghi);
                ghi.WriteEndArray();
                break;
            case JsonValueKind.Number when phanTu.GetRawText().IndexOfAny(['.', 'e', 'E']) >= 0:
                var soThuc = phanTu.GetDouble().ToString("R", CultureInfo.InvariantCulture);
                // Giữ dấu chấm để vẫn nhìn ra là số thực (7.0 chứ không phải 7);
                // ghi qua decimal để writer tự thụt lề và giữ nguyên số chữ số thập phân
                if (soThuc.IndexOfAny(['.', 'E']) < 0) soThuc += ".0";
                ghi.WriteNumberValue(decimal.Parse(soThuc, NumberStyles.Float, CultureInfo.InvariantCulture));
                break;
            default:
                phanTu.WriteTo(ghi);
                break;
        }
    }

    /// <summary>Duyệt đệ quy document/mảng, thay mọi chuỗi "@ten" bằng giá trị tham số.</summary>
    private static BsonValue ThayThamSo(BsonValue giaTri, IDictionary<string, BsonValue> thamSo)
    {
        if (LaThamSo(giaTri, out var ten))
            return LayThamSo(ten, thamSo).DeepClone();

        if (giaTri is BsonDocument doc)
        {
            var moi = new BsonDocument();
            foreach (var phanTu in doc)
                moi.Add(phanTu.Name, ThayThamSo(phanTu.Value, thamSo));
            return moi;
        }

        if (giaTri is BsonArray mang)
            return new BsonArray(mang.Select(x => ThayThamSo(x, thamSo)));

        return giaTri;
    }

    private static bool LaThamSo(BsonValue giaTri, out string ten)
    {
        if (giaTri is BsonString s && s.Value.StartsWith('@'))
        {
            ten = s.Value[1..];
            return true;
        }
        ten = "";
        return false;
    }

    private static BsonValue LayThamSo(string ten, IDictionary<string, BsonValue> thamSo)
        => thamSo.TryGetValue(ten, out var giaTri)
            ? giaTri
            : throw new InvalidOperationException($"Pipeline thiếu tham số \"@{ten}\".");
}
