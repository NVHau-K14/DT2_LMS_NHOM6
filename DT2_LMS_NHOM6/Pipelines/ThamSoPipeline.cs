using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Pipelines;

/// <summary>
/// Bộ lọc sinh viên – trở thành nội dung của stage { $match: ... } đầu pipeline.
/// Lọc ở đầu pipeline để MongoDB dùng index (maSV, maLop, monHoc.giangVien.maGV).
/// </summary>
public class BoLocSinhVien
{
    public BsonDocument DieuKien { get; }

    private BoLocSinhVien(BsonDocument dieuKien) => DieuKien = dieuKien;

    /// <summary>Toàn trường: { }</summary>
    public static BoLocSinhVien TatCa() => new(new BsonDocument());

    /// <summary>Một lớp sinh hoạt: { maLop: "16DHTH01" }</summary>
    public static BoLocSinhVien TheoLop(string maLop) => new(new BsonDocument("maLop", maLop));

    /// <summary>Một sinh viên: { maSV: "2001250001" }</summary>
    public static BoLocSinhVien TheoSinhVien(string maSV) => new(new BsonDocument("maSV", maSV));

    /// <summary>Danh sách MSSV: { maSV: { $in: [...] } }</summary>
    public static BoLocSinhVien TheoDanhSach(IEnumerable<string> dsMaSV)
        => new(new BsonDocument("maSV", new BsonDocument("$in", new BsonArray(dsMaSV))));

    /// <summary>SV có ít nhất một lần học do giảng viên này dạy: { "monHoc.giangVien.maGV": "GV005" }</summary>
    public static BoLocSinhVien TheoGiangVien(string maGV) => new(new BsonDocument("monHoc.giangVien.maGV", maGV));

    /// <summary>SV thuộc một lớp học phần: { monHoc: { $elemMatch: { maMon, namHoc, hocKy, giangVien.maGV } } }</summary>
    public static BoLocSinhVien TheoLopHocPhan(string maGV, string maMon, string namHoc, int hocKy)
        => new(new BsonDocument("monHoc", new BsonDocument("$elemMatch",
            DieuKienLanHoc.Tao(maGV, maMon, namHoc, hocKy).TrongPhanTu)));
}

/// <summary>Phạm vi báo cáo học lực: tích lũy (mọi học kỳ) hoặc một học kỳ.</summary>
public class PhamViBaoCao
{
    public string? NamHoc { get; }
    public int? HocKy { get; }
    public bool LaTichLuy => NamHoc == null;

    private PhamViBaoCao(string? namHoc, int? hocKy)
    {
        NamHoc = namHoc;
        HocKy = hocKy;
    }

    public static PhamViBaoCao TichLuy() => new(null, null);

    public static PhamViBaoCao TheoHocKy(string namHoc, int hocKy) => new(namHoc, hocKy);
}

/// <summary>
/// Điều kiện chọn lần học (phần tử monHoc) theo giảng viên / môn / học kỳ – tham số nào null thì bỏ qua.
/// Cùng một điều kiện được viết 2 dạng:
///   TrongPhanTu – dùng trong $elemMatch trước $unwind:  { "giangVien.maGV": "GV005", "namHoc": "2026-2027" }
///   SauUnwind   – dùng trong $match sau $unwind:        { "monHoc.giangVien.maGV": "GV005", "monHoc.namHoc": "2026-2027" }
/// </summary>
public record DieuKienLanHoc(BsonDocument TrongPhanTu, BsonDocument SauUnwind)
{
    public static DieuKienLanHoc Tao(string? maGV, string? maMon, string? namHoc, int? hocKy)
    {
        var trong = new BsonDocument();
        if (maGV != null) trong.Add("giangVien.maGV", maGV);
        if (maMon != null) trong.Add("maMon", maMon);
        if (namHoc != null) trong.Add("namHoc", namHoc);
        if (hocKy != null) trong.Add("hocKy", hocKy.Value);

        var sau = new BsonDocument();
        foreach (var phanTu in trong)
            sau.Add("monHoc." + phanTu.Name, phanTu.Value);

        return new DieuKienLanHoc(trong, sau);
    }
}
