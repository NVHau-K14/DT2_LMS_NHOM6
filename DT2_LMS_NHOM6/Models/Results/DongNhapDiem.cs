using DT2_LMS_NHOM6.Models.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>Một dòng của pipeline DanhSachNhapDiem: một SV trong lớp học phần.</summary>
[BsonIgnoreExtraElements]
public class DongNhapDiem
{
    [BsonElement("maSV")] public string MaSV { get; set; } = "";
    [BsonElement("hoTen")] public string HoTen { get; set; } = "";
    [BsonElement("maLop")] public string MaLop { get; set; } = "";

    /// <summary>Định danh lần học – dùng trong arrayFilters khi lưu điểm.</summary>
    [BsonElement("maDK")] public ObjectId MaDK { get; set; }
    [BsonElement("hinhThuc")] public string HinhThuc { get; set; } = "";
    [BsonElement("lanHoc")] public int LanHoc { get; set; }
    [BsonElement("giangVien")] public GiangVienRef GiangVien { get; set; } = new();
    [BsonElement("diem")] public DiemThanhPhan Diem { get; set; } = new();

    [BsonElement("capNhatLuc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CapNhatLuc { get; set; }

    [BsonElement("capNhatBoi")] public string? CapNhatBoi { get; set; }
    [BsonElement("duDiem")] public bool DuDiem { get; set; }
    [BsonElement("diemTK")] public double? DiemTK { get; set; }
    [BsonElement("diemChu")] public string? DiemChu { get; set; }
    [BsonElement("diemHe4")] public int? DiemHe4 { get; set; }
    [BsonElement("dat")] public bool? Dat { get; set; }
}
