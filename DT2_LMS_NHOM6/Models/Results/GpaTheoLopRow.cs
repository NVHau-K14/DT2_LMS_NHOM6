using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>Một dòng của pipeline GpaTheoLop.</summary>
[BsonIgnoreExtraElements]
public class GpaTheoLopRow
{
    [BsonElement("maLop")] public string MaLop { get; set; } = "";
    [BsonElement("siSo")] public int SiSo { get; set; }
    [BsonElement("soSVCoGpa")] public int SoSVCoGpa { get; set; }

    /// <summary>Trung bình GPA tích lũy của các SV đã có GPA; null nếu cả lớp chưa có.</summary>
    [BsonElement("gpaTrungBinh")] public double? GpaTrungBinh { get; set; }
}
