using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>Một dòng của pipeline SiSoTheoLop.</summary>
[BsonIgnoreExtraElements]
public class SiSoLopRow
{
    [BsonElement("maLop")] public string MaLop { get; set; } = "";
    [BsonElement("siSo")] public int SiSo { get; set; }
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
