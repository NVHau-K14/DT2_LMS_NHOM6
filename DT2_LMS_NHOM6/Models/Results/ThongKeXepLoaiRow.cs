using MongoDB.Bson.Serialization.Attributes;

namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>Một dòng của pipeline ThongKeXepLoai: số SV của một xếp loại, kèm chi tiết theo lớp.</summary>
[BsonIgnoreExtraElements]
public class ThongKeXepLoaiRow
{
    [BsonElement("xepLoai")] public string XepLoai { get; set; } = "";
    [BsonElement("soSV")] public int SoSV { get; set; }

    /// <summary>1 = Xuất sắc … 6 = Kém, 7 = Chưa xếp loại.</summary>
    [BsonElement("thuTu")] public int ThuTu { get; set; }
    [BsonElement("theoLop")] public List<SoSVTheoLop> TheoLop { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class SoSVTheoLop
{
    [BsonElement("maLop")] public string MaLop { get; set; } = "";
    [BsonElement("soSV")] public int SoSV { get; set; }
}
