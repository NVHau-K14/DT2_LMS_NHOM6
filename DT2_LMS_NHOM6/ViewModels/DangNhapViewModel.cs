using System.ComponentModel.DataAnnotations;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>Form đăng nhập (C-01).</summary>
public class DangNhapViewModel
{
    [Display(Name = "Tên đăng nhập")]
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa {1} ký tự.")]
    public string TenDangNhap { get; set; } = "";

    [Display(Name = "Mật khẩu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = "";

    /// <summary>Trang người dùng định vào trước khi bị chuyển sang đăng nhập.</summary>
    public string? ReturnUrl { get; set; }
}
