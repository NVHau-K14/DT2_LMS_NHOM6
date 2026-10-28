using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Documents;

/// <summary>Danh mục môn học (collection monhoc).</summary>
[BsonIgnoreExtraElements]
public class MonHoc
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("maMon")] public string MaMon { get; set; } = "";
    [BsonElement("tenMon")] public string TenMon { get; set; } = "";
    [BsonElement("soTinChi")] public int SoTinChi { get; set; }
    [BsonElement("trongSoMacDinh")] public TrongSo TrongSoMacDinh { get; set; } = new();
    [BsonElement("khoa")] public string Khoa { get; set; } = "";

    [BsonElement("ngayTao")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NgayTao { get; set; }
}
