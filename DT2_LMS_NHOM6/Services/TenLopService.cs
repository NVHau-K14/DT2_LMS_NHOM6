using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Models.Documents;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Bảng tra lớp sinh hoạt: maLop (mã nội bộ, ví dụ CNTT2501) ↔ tenLop (tên hiển thị, ví dụ 16DHTH01).
/// Đây chỉ là tra tên để hiển thị / sắp xếp – không tính điểm nên làm bằng C#.
/// </summary>
public class BangTenLop
{
    private readonly Dictionary<string, string> _tenTheoMa;
    private readonly Dictionary<string, string> _maTheoTen;

    public BangTenLop(IEnumerable<(string MaLop, string TenLop)> dsLop)
    {
        var ds = dsLop.ToList();
        _tenTheoMa = ds.ToDictionary(l => l.MaLop, l => l.TenLop);
        _maTheoTen = ds.GroupBy(l => l.TenLop, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().MaLop, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Tên hiển thị của một lớp; mã không có trong bảng → trả lại chính mã đó.</summary>
    public string Ten(string? maLop)
        => string.IsNullOrEmpty(maLop) ? "" : _tenTheoMa.GetValueOrDefault(maLop, maLop);

    /// <summary>
    /// Quy đổi chữ người dùng nhập (Excel, API) về maLop: nhận mã lớp (CNTT2501) hoặc tên lớp (16DHTH01),
    /// không phân biệt hoa thường. Không tìm thấy → null.
    /// </summary>
    public string? TimMaLop(string? maHoacTen)
    {
        if (string.IsNullOrWhiteSpace(maHoacTen)) return null;
        var chuoi = maHoacTen.Trim();
        var ma = chuoi.ToUpperInvariant();
        if (_tenTheoMa.ContainsKey(ma)) return ma;
        return _maTheoTen.GetValueOrDefault(chuoi);
    }

    /// <summary>Sắp xếp theo tên lớp (ổn định: các phần tử cùng lớp giữ nguyên thứ tự cũ, ví dụ theo MSSV).</summary>
    public IEnumerable<T> SapXep<T>(IEnumerable<T> ds, Func<T, string> layMaLop)
        => ds.OrderBy(x => Ten(layMaLop(x)), StringComparer.Ordinal);
}

/// <summary>
/// Nạp bảng tên lớp một lần mỗi request (scoped): db.lophoc.find({}, { maLop: 1, tenLop: 1 }) – chỉ vài document.
/// Dùng ở view (@inject TenLop trong _ViewImports), service Excel, API.
/// </summary>
public class TenLopService
{
    private readonly MongoContext _db;
    private BangTenLop? _bang;

    public TenLopService(MongoContext db)
    {
        _db = db;
    }

    public async Task<BangTenLop> LayAsync()
    {
        if (_bang != null) return _bang;
        var dsLop = await _db.LopHoc.Find(FilterDefinition<LopHoc>.Empty)
            .Project(l => new { l.MaLop, l.TenLop })
            .ToListAsync();
        _bang = new BangTenLop(dsLop.Select(l => (l.MaLop, l.TenLop)));
        return _bang;
    }
}
