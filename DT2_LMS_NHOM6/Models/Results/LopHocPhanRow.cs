using DT2_LMS_NHOM6.Models.Documents;
using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>Một dòng của pipeline LopHocPhanCuaGV: một lớp học phần (maMon, namHoc, hocKy) của giảng viên.</summary>
[BsonIgnoreExtraElements]
public class LopHocPhanRow
{
    [BsonElement("maMon")] public string MaMon { get; set; } = "";
    [BsonElement("tenMon")] public string TenMon { get; set; } = "";
    [BsonElement("soTinChi")] public int SoTinChi { get; set; }
    [BsonElement("trongSo")] public TrongSo TrongSo { get; set; } = new();
    [BsonElement("namHoc")] public string NamHoc { get; set; } = "";
    [BsonElement("hocKy")] public int HocKy { get; set; }
    [BsonElement("siSo")] public int SiSo { get; set; }

    /// <summary>Số SV đã có đủ 3 điểm thành phần.</summary>
    [BsonElement("soDuDiem")] public int SoDuDiem { get; set; }

    /// <summary>Các lớp sinh hoạt có SV trong lớp học phần.</summary>
    [BsonElement("cacLop")] public List<string> CacLop { get; set; } = new();
}
