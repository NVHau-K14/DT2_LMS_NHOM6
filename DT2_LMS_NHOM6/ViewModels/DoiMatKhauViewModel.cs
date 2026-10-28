using System.ComponentModel.DataAnnotations;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>Form đổi mật khẩu (C-02) – dùng cho mọi vai trò.</summary>
public class DoiMatKhauViewModel
{
    [Display(Name = "Mật khẩu hiện tại")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
    [DataType(DataType.Password)]
    public string MatKhauCu { get; set; } = "";

    [Display(Name = "Mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có ít nhất {2} ký tự.")]
    [DataType(DataType.Password)]
    public string MatKhauMoi { get; set; } = "";

    [Display(Name = "Xác nhận mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu xác nhận không khớp.")]
    [DataType(DataType.Password)]
    public string XacNhanMatKhau { get; set; } = "";
}
