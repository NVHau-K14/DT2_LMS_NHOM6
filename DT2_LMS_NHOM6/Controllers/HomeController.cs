using DT2_LMS_NHOM6.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Controllers;

public class HomeController : Controller
{
    /// <summary>"/" : chưa đăng nhập → [Authorize] chuyển về Login; đã đăng nhập → cổng theo vai trò.</summary>
    [Authorize]
    public IActionResult Index()
    {
        return Redirect(User.TrangChuTheoVaiTro());
    }

    /// <summary>
    /// Trang lỗi thân thiện cho 404, 403, 500... Được gọi lại (re-execute) bởi
    /// UseStatusCodePagesWithReExecute và UseExceptionHandler, kể cả khi request gốc là POST
    /// nên không giới hạn phương thức và bỏ qua kiểm tra antiforgery.
    /// </summary>
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("Home/StatusCode/{code:int}")]
    public IActionResult TrangLoi(int code)
    {
        if (code < 400 || code > 599) code = StatusCodes.Status404NotFound;
        Response.StatusCode = code;
        return View("StatusCode", code);
    }
}
