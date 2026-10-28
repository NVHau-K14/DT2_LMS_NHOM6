using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>Một dòng của pipeline SiSoTheoLop.</summary>
[BsonIgnoreExtraElements]
public class SiSoLopRow
{
    [BsonElement("maLop")] public string MaLop { get; set; } = "";
    [BsonElement("siSo")] public int SiSo { get; set; }
}

/// <summary>Một dòng của pipeline HocKyCoDuLieu.</summary>
[BsonIgnoreExtraElements]
public class HocKyRow
{
    [BsonElement("namHoc")] public string NamHoc { get; set; } = "";
    [BsonElement("hocKy")] public int HocKy { get; set; }
}

/// <summary>Một dòng của pipeline SoLanHocTheoMon.</summary>
[BsonIgnoreExtraElements]
public class SoLanHocMonRow
{
    [BsonElement("maMon")] public string MaMon { get; set; } = "";

    /// <summary>Số phần tử monHoc (mọi SV, mọi học kỳ) có maMon này.</summary>
    [BsonElement("soLanHoc")] public int SoLanHoc { get; set; }

    /// <summary>Số SV khác nhau đã từng học môn này.</summary>
    [BsonElement("soSV")] public int SoSV { get; set; }
}

/// <summary>Một dòng của pipeline LopHocPhanToanTruong: một lớp học phần (maMon, namHoc, hocKy, maGV).</summary>
[BsonIgnoreExtraElements]
public class LopHocPhanToanTruongRow
{
    [BsonElement("maMon")] public string MaMon { get; set; } = "";
    [BsonElement("tenMon")] public string TenMon { get; set; } = "";
    [BsonElement("namHoc")] public string NamHoc { get; set; } = "";
    [BsonElement("hocKy")] public int HocKy { get; set; }
    [BsonElement("maGV")] public string MaGV { get; set; } = "";
    /// <summary>Họ tên kèm học vị (ảnh chụp trong monHoc.giangVien.hoTen).</summary>
    [BsonElement("tenGV")] public string TenGV { get; set; } = "";
    [BsonElement("siSo")] public int SiSo { get; set; }

    /// <summary>Mã dùng trong ô chọn / query string: "INT301_2026-2027_1_GV005".</summary>
    public string Ma => $"{MaMon}_{NamHoc}_{HocKy}_{MaGV}";
}
