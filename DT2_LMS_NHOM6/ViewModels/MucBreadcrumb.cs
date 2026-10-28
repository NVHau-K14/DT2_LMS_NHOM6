namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>
/// Một mục breadcrumb ở header. View đặt ViewData["Breadcrumb"] = new[] { new MucBreadcrumb("Lớp học", "/Admin/LopHoc") };
/// Layout tự thêm "Trang chủ" ở đầu và ViewData["Title"] ở cuối.
/// </summary>
public record MucBreadcrumb(string Ten, string? Url = null);
