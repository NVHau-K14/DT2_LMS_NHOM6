using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Documents;

/// <summary>Hồ sơ sinh viên (collection sinhvien) – nhúng mảng monHoc gồm mọi lần học.</summary>
[BsonIgnoreExtraElements]
public class SinhVien
{
    [BsonId] public ObjectId Id { get; set; }

    /// <summary>10 chữ số, unique, không cho sửa.</summary>
    [BsonElement("maSV")] public string MaSV { get; set; } = "";
    [BsonElement("hoTen")] public string HoTen { get; set; } = "";

    [BsonElement("ngaySinh")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NgaySinh { get; set; }

    [BsonElement("gioiTinh")] public string GioiTinh { get; set; } = "";
    [BsonElement("email")] public string? Email { get; set; }
    [BsonElement("soDienThoai")] public string? SoDienThoai { get; set; }
    [BsonElement("diaChi")] public string? DiaChi { get; set; }
    [BsonElement("maLop")] public string MaLop { get; set; } = "";
    [BsonElement("trangThai")] public string TrangThai { get; set; } = "";
    [BsonElement("monHoc")] public List<MonHocDangKy> MonHoc { get; set; } = new();

    [BsonElement("ngayTao")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NgayTao { get; set; }

    [BsonElement("ngayCapNhat")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime NgayCapNhat { get; set; }
}
