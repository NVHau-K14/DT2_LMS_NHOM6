using System.Security.Claims;
using DT2_LMS_NHOM6.Models;

namespace DT2_LMS_NHOM6.Infrastructure;

/// <summary>
/// Đọc thông tin người dùng hiện tại từ claim của cookie đăng nhập.
/// Mã SV/GV luôn lấy ở đây – KHÔNG lấy từ URL/query/form.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Tên claim chứa maSV / maGV / "admin".</summary>
    public const string ClaimMaThamChieu = "MaThamChieu";

    public static string MaThamChieu(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimMaThamChieu) ?? "";

    public static string TenDangNhap(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public static string HoTen(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name) ?? "";

    public static string VaiTro(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Role) ?? "";

    /// <summary>Đường dẫn trang chủ của cổng theo vai trò.</summary>
    public static string TrangChuTheoVaiTro(this ClaimsPrincipal user) => user.VaiTro() switch
    {
        HangSo.VaiTro.Admin => "/Admin",
        HangSo.VaiTro.GiangVien => "/GiangVien",
        HangSo.VaiTro.SinhVien => "/SinhVien",
        _ => "/Account/Login"
    };
}
