using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>Trang tổng quan của cổng (tạm thời: lời chào).</summary>
public class HomeController : AdminControllerBase
{
    public IActionResult Index()
    {
        return View();
    }
}
