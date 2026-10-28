using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Documents;

/// <summary>Tham chiếu giảng viên nhúng trong document khác; hoTen lưu kèm học vị ("ThS. Nguyễn Văn An").</summary>
[BsonIgnoreExtraElements]
public class GiangVienRef
{
    [BsonElement("maGV")] public string MaGV { get; set; } = "";
    [BsonElement("hoTen")] public string HoTen { get; set; } = "";
}
