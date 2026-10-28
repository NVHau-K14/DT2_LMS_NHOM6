using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Infrastructure;

/// <summary>
/// JSON (System.Text.Json) cho ObjectId: ghi ra chuỗi 24 ký tự hex ("6720f1...") thay vì object
/// { timestamp, creationTime... }. Nhờ vậy API trả thẳng được document / kết quả pipeline có _id, maDK.
/// </summary>
public class ObjectIdJsonConverter : JsonConverter<ObjectId>
{
    public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => ObjectId.TryParse(reader.GetString(), out var id)
            ? id
            : throw new JsonException("Mã ObjectId phải là chuỗi 24 ký tự hex.");

    public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
