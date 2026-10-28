using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>Form thêm / sửa sinh viên (A-05). MSSV khóa khi sửa.</summary>
public class SinhVienFormViewModel : IValidatableObject
{
    /// <summary>Do server đặt (không bind từ JSON của API).</summary>
    [JsonIgnore] public bool LaSua { get; set; }

    [Display(Name = "MSSV")]
    [Required(ErrorMessage = "Vui lòng nhập MSSV.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "MSSV phải gồm đúng 10 chữ số.")]
    public string MaSV { get; set; } = "";

    [Display(Name = "Họ tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ tên dài từ {2} đến {1} ký tự.")]
    public string HoTen { get; set; } = "";

    [Display(Name = "Ngày sinh")]
    [Required(ErrorMessage = "Vui lòng nhập ngày sinh hợp lệ.")]
    [DataType(DataType.Date)]
    public DateTime? NgaySinh { get; set; }

    [Display(Name = "Giới tính")]
    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    public string GioiTinh { get; set; } = HangSo.GioiTinh.Nam;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(100, ErrorMessage = "Email tối đa {1} ký tự.")]
    public string? Email { get; set; }

    [Display(Name = "Số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0.")]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Địa chỉ")]
    [StringLength(200, ErrorMessage = "Địa chỉ tối đa {1} ký tự.")]
    public string? DiaChi { get; set; }

    [Display(Name = "Lớp")]
    [Required(ErrorMessage = "Vui lòng chọn lớp.")]
    public string MaLop { get; set; } = "";

    [Display(Name = "Trạng thái")]
    [Required(ErrorMessage = "Vui lòng chọn trạng thái.")]
    public string TrangThai { get; set; } = HangSo.TrangThaiSinhVien.DangHoc;

    /// <summary>Danh sách lớp cho ô chọn – không bind từ form.</summary>
    [ValidateNever, JsonIgnore] public List<LopHoc> DsLop { get; set; } = new();

    public static readonly string[] DsGioiTinh = [HangSo.GioiTinh.Nam, HangSo.GioiTinh.Nu];
    public static readonly string[] DsTrangThai =
        [HangSo.TrangThaiSinhVien.DangHoc, HangSo.TrangThaiSinhVien.BaoLuu, HangSo.TrangThaiSinhVien.ThoiHoc];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Ngày sinh hợp lệ: năm 1950 trở đi và đủ 15 tuổi (chọn đơn giản, ghi trong báo cáo GĐ4)
        if (NgaySinh.HasValue && (NgaySinh.Value.Year < 1950 || NgaySinh.Value.Date > DateTime.Today.AddYears(-15)))
            yield return new ValidationResult("Ngày sinh phải từ năm 1950 và sinh viên đủ 15 tuổi.", [nameof(NgaySinh)]);
        if (!DsGioiTinh.Contains(GioiTinh))
            yield return new ValidationResult("Giới tính không hợp lệ.", [nameof(GioiTinh)]);
        if (!DsTrangThai.Contains(TrangThai))
            yield return new ValidationResult("Trạng thái không hợp lệ.", [nameof(TrangThai)]);
    }
}

/// <summary>Một dòng bảng sinh viên: hồ sơ + học lực tích lũy (pipeline) + tài khoản.</summary>
public record DongSinhVien(SinhVien SinhVien, BaoCaoHocLucRow? HocLuc, TaiKhoan? TaiKhoan);

/// <summary>Danh sách sinh viên (A-04), phân trang phía server.</summary>
public class SinhVienIndexViewModel
{
    public List<DongSinhVien> DanhSach { get; set; } = new();
    public string? MaLop { get; set; }
    public string? TrangThai { get; set; }
    public string? Q { get; set; }
    public List<LopHoc> DsLop { get; set; } = new();
    public PhanTrangViewModel PhanTrang { get; set; } = new();
    /// <summary>URL trang hiện tại (kèm bộ lọc) – để các thao tác POST quay lại đúng chỗ.</summary>
    public string UrlHienTai { get; set; } = "/Admin/SinhVien";
}

/// <summary>Hồ sơ sinh viên (A-06).</summary>
public class HoSoSinhVienViewModel
{
    public SinhVien SinhVien { get; set; } = new();
    public string? TenLop { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }
    public BangDiemViewModel BangDiem { get; set; } = new();
}
