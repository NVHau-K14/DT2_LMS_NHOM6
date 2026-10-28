using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Documents;

/// <summary>Trọng số điểm thành phần, số nguyên phần trăm (cc + gk + ck = 100).</summary>
[BsonIgnoreExtraElements]
public class TrongSo
{
    [BsonElement("cc")] public int Cc { get; set; }
    [BsonElement("gk")] public int Gk { get; set; }
    [BsonElement("ck")] public int Ck { get; set; }
}
