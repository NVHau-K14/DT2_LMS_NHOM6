using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Documents;

/// <summary>Lớp sinh hoạt (collection lophoc). Sĩ số không lưu – đếm từ sinhvien.</summary>
[BsonIgnoreExtraElements]
public class LopHoc
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("maLop")] public string MaLop { get; set; } = "";
    [BsonElement("tenLop")] public string TenLop { get; set; } = "";
    [BsonElement("nienKhoa")] public string NienKhoa { get; set; } = "";
    [BsonElement("nganh")] public string Nganh { get; set; } = "";
    [BsonElement("khoa")] public string Khoa { get; set; } = "";
    [BsonElement("coVanHocTap")] public GiangVienRef? CoVanHocTap { get; set; }

    [BsonElement("ngayTao")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NgayTao { get; set; }
}
