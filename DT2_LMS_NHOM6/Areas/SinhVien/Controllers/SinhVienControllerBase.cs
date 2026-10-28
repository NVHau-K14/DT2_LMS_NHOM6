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
}
