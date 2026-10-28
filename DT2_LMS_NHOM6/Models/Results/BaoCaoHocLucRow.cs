using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>Một dòng của pipeline BaoCaoHocLuc: học lực của một sinh viên (tích lũy hoặc một học kỳ).</summary>
[BsonIgnoreExtraElements]
public class BaoCaoHocLucRow
{
    [BsonElement("maSV")] public string MaSV { get; set; } = "";
    [BsonElement("hoTen")] public string HoTen { get; set; } = "";
    [BsonElement("maLop")] public string MaLop { get; set; } = "";

    /// <summary>null → "Chưa xếp loại".</summary>
    [BsonElement("gpa")] public double? Gpa { get; set; }
    [BsonElement("xepLoai")] public string XepLoai { get; set; } = "";
    [BsonElement("tinChiTichLuy")] public int TinChiTichLuy { get; set; }
    [BsonElement("soMonNo")] public int SoMonNo { get; set; }
}
