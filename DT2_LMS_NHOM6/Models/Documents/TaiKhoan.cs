using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Documents;

/// <summary>Tài khoản đăng nhập (collection taikhoan).</summary>
[BsonIgnoreExtraElements]
public class TaiKhoan
{
    [BsonId] public ObjectId Id { get; set; }

    /// <summary>SV: maSV; GV: maGV; Admin: "admin".</summary>
    [BsonElement("tenDangNhap")] public string TenDangNhap { get; set; } = "";
    /// <summary>Chuỗi băm BCrypt.</summary>
    [BsonElement("matKhauHash")] public string MatKhauHash { get; set; } = "";
    [BsonElement("vaiTro")] public string VaiTro { get; set; } = "";
    /// <summary>maSV / maGV / "admin".</summary>
    [BsonElement("maThamChieu")] public string MaThamChieu { get; set; } = "";
    /// <summary>GV: kèm học vị.</summary>
    [BsonElement("hoTen")] public string HoTen { get; set; } = "";
    [BsonElement("trangThai")] public string TrangThai { get; set; } = "";

    [BsonElement("lanDangNhapCuoi")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? LanDangNhapCuoi { get; set; }

    [BsonElement("ngayTao")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NgayTao { get; set; }
}
