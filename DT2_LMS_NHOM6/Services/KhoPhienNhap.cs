using Microsoft.Extensions.Caching.Memory;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Giữ tạm kết quả kiểm tra file Excel giữa bước "xem trước" và bước "xác nhận ghi" (thiết kế mục 6:
/// "lưu kết quả kiểm tra tạm theo phiên"). Lưu trong bộ nhớ máy chủ (IMemoryCache) 30 phút,
/// khóa = mã phiên ngẫu nhiên; mỗi phiên gắn với người tải lên nên người khác không dùng lại được.
/// Không lưu vào cookie TempData vì danh sách có thể hàng trăm dòng (vượt giới hạn cookie).
/// </summary>
public class KhoPhienNhap
{
    private static readonly TimeSpan ThoiHan = TimeSpan.FromMinutes(30);

    private readonly IMemoryCache _cache;

    public KhoPhienNhap(IMemoryCache cache)
    {
        _cache = cache;
    }

    private record MucPhien(string ChuSoHuu, object DuLieu);

    /// <returns>Mã phiên (32 ký tự hex) để đưa vào URL / form xác nhận.</returns>
    public string Luu(string chuSoHuu, object duLieu)
    {
        var ma = Guid.NewGuid().ToString("N");
        _cache.Set(ma, new MucPhien(chuSoHuu, duLieu), ThoiHan);
        return ma;
    }

    /// <summary>null khi không có / hết hạn / không phải của người này / sai kiểu.</summary>
    public T? Lay<T>(string? ma, string chuSoHuu) where T : class
    {
        if (string.IsNullOrEmpty(ma) || !_cache.TryGetValue(ma, out MucPhien? muc) || muc == null) return null;
        return muc.ChuSoHuu == chuSoHuu ? muc.DuLieu as T : null;
    }

    /// <summary>Xóa phiên sau khi đã ghi (bấm Xác nhận lần 2 không ghi lại).</summary>
    public void Xoa(string ma) => _cache.Remove(ma);
}
