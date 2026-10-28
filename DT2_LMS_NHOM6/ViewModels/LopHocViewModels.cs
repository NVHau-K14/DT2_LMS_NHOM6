using System.ComponentModel.DataAnnotations;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>Modal thêm / sửa lớp (A-02). Mã lớp khóa khi sửa.</summary>
public class LopHocFormViewModel
{
    public bool LaSua { get; set; }

    [Display(Name = "Mã lớp")]
    [Required(ErrorMessage = "Vui lòng nhập mã lớp.")]
    [RegularExpression(@"^[A-Za-z0-9]{3,20}$", ErrorMessage = "Mã lớp gồm 3–20 chữ cái hoặc chữ số, không dấu, không khoảng trắng.")]
    public string MaLop { get; set; } = "";

    [Display(Name = "Tên lớp")]
    [Required(ErrorMessage = "Vui lòng nhập tên lớp.")]
    [StringLength(150, ErrorMessage = "Tên lớp tối đa {1} ký tự.")]
    public string TenLop { get; set; } = "";

    [Display(Name = "Niên khóa")]
    [Required(ErrorMessage = "Vui lòng nhập niên khóa.")]
    [RegularExpression(@"^\d{4}-\d{4}$", ErrorMessage = "Niên khóa có dạng 2025-2029.")]
    public string NienKhoa { get; set; } = "";

    [Display(Name = "Ngành")]
    [Required(ErrorMessage = "Vui lòng nhập ngành.")]
    [StringLength(100, ErrorMessage = "Ngành tối đa {1} ký tự.")]
    public string Nganh { get; set; } = "";

    [Display(Name = "Khoa")]
    [Required(ErrorMessage = "Vui lòng nhập khoa.")]
    [StringLength(100, ErrorMessage = "Khoa tối đa {1} ký tự.")]
    public string Khoa { get; set; } = "Khoa Công nghệ thông tin";

    /// <summary>Mã GV làm cố vấn học tập; để trống = chưa phân công.</summary>
    [Display(Name = "Cố vấn học tập")]
    public string? MaGVCoVan { get; set; }
}

/// <summary>Một dòng bảng lớp: document lớp + sĩ số đếm từ sinhvien.</summary>
public record DongLopHoc(LopHoc Lop, int SiSo);

/// <summary>Trang danh sách lớp (A-02).</summary>
public class LopHocIndexViewModel
{
    public List<DongLopHoc> DanhSach { get; set; } = new();

    // Bộ lọc
    public string? Nganh { get; set; }
    public string? NienKhoa { get; set; }
    public string? Q { get; set; }
    public List<string> DsNganh { get; set; } = new();
    public List<string> DsNienKhoa { get; set; } = new();

    // Modal
    public List<GiangVien> DsGiangVien { get; set; } = new();
    public LopHocFormViewModel Form { get; set; } = new();
    /// <summary>true khi POST lỗi validation → mở lại modal kèm lỗi.</summary>
    public bool MoModal { get; set; }
}

/// <summary>Chi tiết lớp (A-03): thông tin lớp + học lực tích lũy từng SV (pipeline BaoCaoHocLuc lọc theo lớp).</summary>
public class LopHocChiTietViewModel
{
    public LopHoc Lop { get; set; } = new();
    public List<BaoCaoHocLucRow> DanhSachSV { get; set; } = new();
}
