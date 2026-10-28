using System.ComponentModel.DataAnnotations;
using DT2_LMS_NHOM6.Models.Documents;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>Modal thêm / sửa môn học (A-08). Mã môn khóa khi sửa; tổng trọng số phải bằng 100.</summary>
public class MonHocFormViewModel : IValidatableObject
{
    public bool LaSua { get; set; }

    [Display(Name = "Mã môn")]
    [Required(ErrorMessage = "Vui lòng nhập mã môn.")]
    [RegularExpression(@"^[A-Za-z0-9]{3,20}$", ErrorMessage = "Mã môn gồm 3–20 chữ cái hoặc chữ số, không dấu, không khoảng trắng.")]
    public string MaMon { get; set; } = "";

    [Display(Name = "Tên môn")]
    [Required(ErrorMessage = "Vui lòng nhập tên môn.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Tên môn dài từ {2} đến {1} ký tự.")]
    public string TenMon { get; set; } = "";

    [Display(Name = "Số tín chỉ")]
    [Required(ErrorMessage = "Vui lòng nhập số tín chỉ.")]
    [Range(1, 10, ErrorMessage = "Số tín chỉ từ {1} đến {2}.")]
    public int? SoTinChi { get; set; } = 3;

    [Display(Name = "Chuyên cần (%)")]
    [Required(ErrorMessage = "Nhập trọng số CC.")]
    [Range(0, 100, ErrorMessage = "Trọng số từ {1} đến {2}.")]
    public int? TrongSoCC { get; set; } = 10;

    [Display(Name = "Giữa kỳ (%)")]
    [Required(ErrorMessage = "Nhập trọng số GK.")]
    [Range(0, 100, ErrorMessage = "Trọng số từ {1} đến {2}.")]
    public int? TrongSoGK { get; set; } = 30;

    [Display(Name = "Cuối kỳ (%)")]
    [Required(ErrorMessage = "Nhập trọng số CK.")]
    [Range(0, 100, ErrorMessage = "Trọng số từ {1} đến {2}.")]
    public int? TrongSoCK { get; set; } = 60;

    [Display(Name = "Khoa")]
    [Required(ErrorMessage = "Vui lòng nhập khoa.")]
    [StringLength(100, ErrorMessage = "Khoa tối đa {1} ký tự.")]
    public string Khoa { get; set; } = "Khoa Công nghệ thông tin";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TrongSoCC.HasValue && TrongSoGK.HasValue && TrongSoCK.HasValue
            && TrongSoCC + TrongSoGK + TrongSoCK != 100)
            yield return new ValidationResult(
                $"Tổng trọng số CC + GK + CK phải bằng 100 (hiện tại là {TrongSoCC + TrongSoGK + TrongSoCK}).",
                [nameof(TrongSoCK)]);
    }
}

/// <summary>Một dòng bảng môn: document + số lần học / số SV đã học (pipeline SoLanHocTheoMon).</summary>
public record DongMonHoc(MonHoc MonHoc, int SoLanHoc, int SoSV);

/// <summary>Danh mục môn học (A-08).</summary>
public class MonHocIndexViewModel
{
    public List<DongMonHoc> DanhSach { get; set; } = new();
    public MonHocFormViewModel Form { get; set; } = new();
    public bool MoModal { get; set; }
    /// <summary>Số lần học của môn đang sửa (khi mở lại modal sau lỗi) – cho cảnh báo đổi tên.</summary>
    public int SoLanHocDangSua { get; set; }
}
