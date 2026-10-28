using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.SinhVien.Controllers;

/// <summary>
/// Controller cơ sở của Area SinhVien – cổng Sinh viên.
/// Mọi controller trong Area kế thừa lớp này nên tự động yêu cầu đăng nhập đúng vai trò.
/// </summary>
[Area("SinhVien")]
[Authorize(Roles = HangSo.VaiTro.SinhVien)]
public abstract class SinhVienControllerBase : Controller
{
    /// <summary>
    /// MSSV đang đăng nhập – lấy từ claim, KHÔNG BAO GIỜ lấy từ URL/query/form.
    /// Các action của Area không có tham số maSV nên "?maSV=..." trên URL bị bỏ qua.
    /// </summary>
    protected string MaSV => User.MaThamChieu();
}
