using DT2_LMS_NHOM6.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.GiangVien.Controllers;

/// <summary>
/// Controller cơ sở của Area GiangVien – cổng Giảng viên.
/// Mọi controller trong Area kế thừa lớp này nên tự động yêu cầu đăng nhập đúng vai trò.
/// </summary>
[Area("GiangVien")]
[Authorize(Roles = HangSo.VaiTro.GiangVien)]
public abstract class GiangVienControllerBase : Controller
{
}
