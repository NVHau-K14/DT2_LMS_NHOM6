using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Documents;

/// <summary>Giảng viên (collection giangvien). hoTen KHÔNG kèm học vị; học vị lưu riêng ở hocVi.</summary>
[BsonIgnoreExtraElements]
public class GiangVien
{
    [BsonId] public ObjectId Id { get; set; }
    [BsonElement("maGV")] public string MaGV { get; set; } = "";
    [BsonElement("hoTen")] public string HoTen { get; set; } = "";
    [BsonElement("hocVi")] public string HocVi { get; set; } = "";
    [BsonElement("email")] public string? Email { get; set; }
    [BsonElement("soDienThoai")] public string? SoDienThoai { get; set; }
    [BsonElement("khoa")] public string Khoa { get; set; } = "";

    [BsonElement("ngayTao")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NgayTao { get; set; }

    /// <summary>Họ tên kèm học vị ("ThS. Nguyễn Văn An"), dùng khi nhúng vào document khác. Không lưu DB.</summary>
    [BsonIgnore] public string HoTenKemHocVi => $"{HocVi} {HoTen}";
}
