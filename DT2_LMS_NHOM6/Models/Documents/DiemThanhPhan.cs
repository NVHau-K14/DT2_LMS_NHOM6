using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Documents;

/// <summary>Điểm thành phần thang 10; null = chưa nhập (trường vẫn được lưu với giá trị null).</summary>
[BsonIgnoreExtraElements]
public class DiemThanhPhan
{
    [BsonElement("cc")] public double? Cc { get; set; }
    [BsonElement("gk")] public double? Gk { get; set; }
    [BsonElement("ck")] public double? Ck { get; set; }
}
