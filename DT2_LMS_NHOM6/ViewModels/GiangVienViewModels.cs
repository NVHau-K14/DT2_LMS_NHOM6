using System.ComponentModel.DataAnnotations;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>Modal thêm / sửa giảng viên (A-07). Mã GV khóa khi sửa.</summary>
public class GiangVienFormViewModel : IValidatableObject
{
    public bool LaSua { get; set; }

    [Display(Name = "Mã giảng viên")]
    [Required(ErrorMessage = "Vui lòng nhập mã giảng viên.")]
    [RegularExpression(@"^[A-Za-z0-9]{3,20}$", ErrorMessage = "Mã GV gồm 3–20 chữ cái hoặc chữ số, không dấu, không khoảng trắng.")]
    public string MaGV { get; set; } = "";

    [Display(Name = "Họ tên (không kèm học vị)")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên dài từ {2} đến {1} ký tự.")]
    public string HoTen { get; set; } = "";

    [Display(Name = "Học vị")]
    [Required(ErrorMessage = "Vui lòng chọn học vị.")]
    public string HocVi { get; set; } = HangSo.HocVi.ThacSi;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100, ErrorMessage = "Email tối đa {1} ký tự.")]
    public string? Email { get; set; }

    [Display(Name = "Số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.")]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Khoa")]
    [Required(ErrorMessage = "Vui lòng nhập khoa.")]
    [StringLength(100, ErrorMessage = "Khoa tối đa {1} ký tự.")]
    public string Khoa { get; set; } = "Khoa Công nghệ thông tin";

    public static readonly string[] DsHocVi =
        [HangSo.HocVi.CuNhan, HangSo.HocVi.ThacSi, HangSo.HocVi.TienSi, HangSo.HocVi.PhoGiaoSuTienSi];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!DsHocVi.Contains(HocVi))
            yield return new ValidationResult("Học vị không hợp lệ.", [nameof(HocVi)]);
    }
}

/// <summary>Một dòng bảng giảng viên: document + số lớp học phần học kỳ hiện tại (pipeline LopHocPhanCuaGV) + tài khoản.</summary>
public record DongGiangVien(GiangVien GiangVien, int SoLopHocPhan, TaiKhoan? TaiKhoan);

/// <summary>Danh sách giảng viên (A-07).</summary>
public class GiangVienIndexViewModel
{
    public List<DongGiangVien> DanhSach { get; set; } = new();
    public string NhanHocKyHienTai { get; set; } = "";
    public GiangVienFormViewModel Form { get; set; } = new();
    public bool MoModal { get; set; }
}
