using DT2_LMS_NHOM6.Models;
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
}
