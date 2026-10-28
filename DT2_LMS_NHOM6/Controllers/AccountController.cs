using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Controllers;

/// <summary>Đăng nhập, đăng xuất, đổi mật khẩu, trang từ chối truy cập (C-01, C-02, C-03).</summary>
public class AccountController : Controller
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Đã đăng nhập thì đưa thẳng về cổng của mình
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(User.TrangChuTheoVaiTro());

        return View(new DangNhapViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(DangNhapViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (ketQua, taiKhoan) = await _accountService.DangNhapAsync(model.TenDangNhap, model.MatKhau);
        if (ketQua == KetQuaDangNhap.SaiThongTin)
        {
            // Thông báo chung chung: không cho biết sai tên đăng nhập hay sai mật khẩu
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }
        if (ketQua == KetQuaDangNhap.BiKhoa)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ phòng đào tạo.");
            return View(model);
        }

        var principal = _accountService.TaoClaimsPrincipal(taiKhoan!);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Chỉ quay lại returnUrl nội bộ (chống open redirect); mặc định về cổng theo vai trò
        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl != "/")
            return Redirect(model.ReturnUrl);
        return Redirect(principal.TrangChuTheoVaiTro());
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public IActionResult DoiMatKhau()
    {
        return View(new DoiMatKhauViewModel());
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> DoiMatKhau(DoiMatKhauViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // Tài khoản cần đổi lấy từ claim, không lấy từ form
        var thanhCong = await _accountService.DoiMatKhauAsync(User.TenDangNhap(), model.MatKhauCu, model.MatKhauMoi);
        if (!thanhCong)
        {
            ModelState.AddModelError(nameof(model.MatKhauCu), "Mật khẩu hiện tại không đúng.");
            return View(model);
        }

        this.ThongBao("Đổi mật khẩu thành công.");
        return Redirect(User.TrangChuTheoVaiTro());
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Views/Home/StatusCode.cshtml", StatusCodes.Status403Forbidden);
    }
}
