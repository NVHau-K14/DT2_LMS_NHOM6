using Microsoft.AspNetCore.WebUtilities;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>
/// Dữ liệu cho partial _PhanTrang (phân trang phía server).
/// ThamSo = các tham số lọc hiện tại, được giữ nguyên khi chuyển trang.
/// </summary>
public class PhanTrangViewModel
{
    public const int KichThuocMacDinh = 20;

    public int Trang { get; init; } = 1;
    public int KichThuoc { get; init; } = KichThuocMacDinh;
    public long TongSo { get; init; }
    public string DuongDan { get; init; } = "";
    public Dictionary<string, string?> ThamSo { get; init; } = new();

    public int TongSoTrang => (int)Math.Max(1, (TongSo + KichThuoc - 1) / KichThuoc);
    public long TuDong => TongSo == 0 ? 0 : (long)(Trang - 1) * KichThuoc + 1;
    public long DenDong => Math.Min(TongSo, (long)Trang * KichThuoc);

    /// <summary>URL tới trang số <paramref name="trang"/>, giữ nguyên bộ lọc (bỏ tham số rỗng).</summary>
    public string UrlTrang(int trang)
    {
        var thamSo = ThamSo
            .Where(t => !string.IsNullOrEmpty(t.Value))
            .ToDictionary(t => t.Key, t => t.Value);
        thamSo["page"] = trang.ToString();
        return QueryHelpers.AddQueryString(DuongDan, thamSo);
    }
}
