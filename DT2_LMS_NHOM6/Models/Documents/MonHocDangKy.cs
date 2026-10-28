using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Documents;

/// <summary>
/// Một phần tử của mảng sinhvien.monHoc = một lần học một môn.
/// soTinChi và trongSo là ảnh chụp lúc ghi danh (không đổi khi danh mục môn đổi).
/// Không lưu điểm TK / chữ / hệ 4 – các giá trị đó luôn tính bằng pipeline.
/// </summary>
[BsonIgnoreExtraElements]
public class MonHocDangKy
{
    /// <summary>Định danh lần học; arrayFilters lọc theo trường này.</summary>
    [BsonElement("maDK")] public ObjectId MaDK { get; set; }
    [BsonElement("maMon")] public string MaMon { get; set; } = "";
    [BsonElement("tenMon")] public string TenMon { get; set; } = "";
    [BsonElement("soTinChi")] public int SoTinChi { get; set; }
    [BsonElement("namHoc")] public string NamHoc { get; set; } = "";
    /// <summary>1, 2 (học kỳ chính) hoặc 3 (học kỳ hè).</summary>
    [BsonElement("hocKy")] public int HocKy { get; set; }
    [BsonElement("lanHoc")] public int LanHoc { get; set; }
    [BsonElement("hinhThuc")] public string HinhThuc { get; set; } = "";
    [BsonElement("giangVien")] public GiangVienRef GiangVien { get; set; } = new();
    [BsonElement("trongSo")] public TrongSo TrongSo { get; set; } = new();
    [BsonElement("diem")] public DiemThanhPhan Diem { get; set; } = new();

    [BsonElement("capNhatLuc")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CapNhatLuc { get; set; }

    /// <summary>maGV hoặc "admin"; null nếu chưa nhập điểm.</summary>
    [BsonElement("capNhatBoi")] public string? CapNhatBoi { get; set; }
}
