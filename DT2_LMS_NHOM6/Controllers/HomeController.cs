using DT2_LMS_NHOM6.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Controllers;

public class HomeController : Controller
{
    /// <summary>"/" : chưa đăng nhập → trang giới thiệu (chỉ có nút Đăng nhập); đã đăng nhập → cổng theo vai trò.</summary>
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(User.TrangChuTheoVaiTro());

        return View();
    }

    /// <summary>
    /// Trang lỗi thân thiện cho 404, 403, 500... Được gọi lại (re-execute) bởi
    /// UseStatusCodePagesWithReExecute và UseExceptionHandler, kể cả khi request gốc là POST
    /// nên không giới hạn phương thức và bỏ qua kiểm tra antiforgery.
    /// Exception đã được ExceptionHandlerMiddleware ghi log (mức Error); ở đây chỉ nhận diện
    /// lỗi mất kết nối MongoDB để thông báo cụ thể hơn.
    /// </summary>
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("Home/StatusCode/{code:int}")]
    public IActionResult TrangLoi(int code)
    {
        if (code < 400 || code > 599) code = StatusCodes.Status404NotFound;
        Response.StatusCode = code;

        var loi = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        ViewData["LoiKetNoiCsdl"] = loi is TimeoutException or MongoConnectionException;
        return View("StatusCode", code);
    }
}
