using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>Một dòng của pipeline GpaHocKy: GPA của một sinh viên trong một học kỳ.</summary>
[BsonIgnoreExtraElements]
public class GpaHocKyResult
{
    [BsonElement("maSV")] public string MaSV { get; set; } = "";
    [BsonElement("namHoc")] public string NamHoc { get; set; } = "";
    [BsonElement("hocKy")] public int HocKy { get; set; }

    /// <summary>Tổng tín chỉ mọi lần học trong kỳ (kể cả chưa có điểm).</summary>
    [BsonElement("tinChiDangKy")] public int TinChiDangKy { get; set; }
    [BsonElement("tinChiDat")] public int TinChiDat { get; set; }

    /// <summary>null khi trong kỳ chưa có lần học nào đủ điểm.</summary>
    [BsonElement("gpa")] public double? Gpa { get; set; }
    [BsonElement("xepLoai")] public string XepLoai { get; set; } = "";
}
