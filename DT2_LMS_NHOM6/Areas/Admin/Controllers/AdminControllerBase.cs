using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>
/// Controller cơ sở của Area Admin – cổng Quản trị (phòng đào tạo).
/// Mọi controller trong Area kế thừa lớp này nên tự động yêu cầu đăng nhập đúng vai trò.
/// </summary>
[Area("Admin")]
[Authorize(Roles = HangSo.VaiTro.Admin)]
public abstract class AdminControllerBase : Controller
{
    /// <summary>Hiện kết quả thao tác ghi bằng toast (sau redirect): xanh nếu thành công, đỏ nếu bị chặn / lỗi.</summary>
    protected void ThongBaoKetQua(KetQuaGhi ketQua)
        => this.ThongBao(ketQua.ThongBao, ketQua.ThanhCong ? "success" : "danger", ketQua.MongoCommand);

    /// <summary>Gắn lỗi nghiệp vụ (mã trùng, lớp không tồn tại...) vào đúng ô nhập của form.</summary>
    protected void GanLoi(KetQuaGhi ketQua, string tienTo = "")
    {
        var khoa = ketQua.TruongLoi == null ? string.Empty : tienTo + ketQua.TruongLoi;
        ModelState.AddModelError(khoa, ketQua.ThongBao);
    }

    /// <summary>Quay lại trang trước (chỉ URL nội bộ – chống open redirect), mặc định về <paramref name="macDinh"/>.</summary>
    protected IActionResult QuayLai(string? quayLai, string macDinh)
        => Redirect(!string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai) ? quayLai : macDinh);
}
