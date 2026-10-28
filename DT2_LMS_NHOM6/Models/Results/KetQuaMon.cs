using DT2_LMS_NHOM6.Models.Documents;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>
/// Một dòng của pipeline BangDiem: một lần học đã quy đổi.
/// DiemTK, DiemChu, DiemHe4, Dat, TinhTichLuy do pipeline tính – C# chỉ hiển thị.
/// </summary>
[BsonIgnoreExtraElements]
public class KetQuaMon
{
    [BsonElement("maSV")] public string MaSV { get; set; } = "";
    [BsonElement("maDK")] public ObjectId MaDK { get; set; }
    [BsonElement("maMon")] public string MaMon { get; set; } = "";
    [BsonElement("tenMon")] public string TenMon { get; set; } = "";
    [BsonElement("soTinChi")] public int SoTinChi { get; set; }
    [BsonElement("namHoc")] public string NamHoc { get; set; } = "";
    [BsonElement("hocKy")] public int HocKy { get; set; }
    [BsonElement("lanHoc")] public int LanHoc { get; set; }
    [BsonElement("hinhThuc")] public string HinhThuc { get; set; } = "";
    [BsonElement("giangVien")] public GiangVienRef GiangVien { get; set; } = new();
    [BsonElement("trongSo")] public TrongSo TrongSo { get; set; } = new();
    [BsonElement("diem")] public DiemThanhPhan Diem { get; set; } = new();

    [BsonElement("capNhatLuc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CapNhatLuc { get; set; }

    [BsonElement("capNhatBoi")] public string? CapNhatBoi { get; set; }

    /// <summary>Cả 3 điểm thành phần đã có.</summary>
    [BsonElement("duDiem")] public bool DuDiem { get; set; }
    [BsonElement("diemTK")] public double? DiemTK { get; set; }
    [BsonElement("diemChu")] public string? DiemChu { get; set; }
    [BsonElement("diemHe4")] public int? DiemHe4 { get; set; }

    /// <summary>null = chưa có điểm; true = đạt (TK ≥ 4.0); false = F.</summary>
    [BsonElement("dat")] public bool? Dat { get; set; }

    /// <summary>Lần học được chọn làm lần tốt nhất của môn VÀ lần đó đạt (cột "Tính tích lũy" ✓).</summary>
    [BsonElement("tinhTichLuy")] public bool TinhTichLuy { get; set; }
}
