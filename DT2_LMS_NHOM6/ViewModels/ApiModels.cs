using System.ComponentModel.DataAnnotations;
using DT2_LMS_NHOM6.Models.Results;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>
/// Khung trả về chung của API: thông báo + lệnh MongoDB đã chạy (API ghi) + dữ liệu.
/// Lỗi kiểm tra dữ liệu: thanhCong = false, loi = { tênTrường: [thông báo...] }.
/// </summary>
public class KetQuaApi
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = "";

    /// <summary>Chuỗi lệnh dạng mongosh đã thực thi (chỉ có ở API ghi).</summary>
    public string? MongoCommand { get; set; }

    public object? DuLieu { get; set; }

    /// <summary>Lỗi theo từng trường (khi dữ liệu gửi lên không hợp lệ).</summary>
    public Dictionary<string, string[]>? Loi { get; set; }
}

/// <summary>Một dòng danh sách sinh viên: hồ sơ (không kèm mảng monHoc) + học lực tích lũy từ pipeline BaoCaoHocLuc.</summary>
public class SinhVienTomTatApi
{
    public string MaSV { get; set; } = "";
    public string HoTen { get; set; } = "";
    public DateTime NgaySinh { get; set; }
    public string GioiTinh { get; set; } = "";
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    /// <summary>Mã lớp nội bộ, ví dụ CNTT2501.</summary>
    public string MaLop { get; set; } = "";
    /// <summary>Tên lớp hiển thị, ví dụ 16DHTH01.</summary>
    public string TenLop { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public double? Gpa { get; set; }
    public string? XepLoai { get; set; }
    public int TinChiTichLuy { get; set; }
    public int SoMonNo { get; set; }
}

/// <summary>Một trang danh sách sinh viên (20 dòng/trang như A-04).</summary>
public class TrangSinhVienApi
{
    public int Trang { get; set; }
    public int KichThuoc { get; set; }
    public long TongSo { get; set; }
    public int TongSoTrang { get; set; }
    public List<SinhVienTomTatApi> DuLieu { get; set; } = new();
}

/// <summary>Bảng điểm một sinh viên – toàn bộ từ pipeline: BangDiem (từng lần học), GpaHocKy, BaoCaoHocLuc (tổng kết).</summary>
public class BangDiemApi
{
    public string MaSV { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string MaLop { get; set; } = "";
    public string TenLop { get; set; } = "";
    public List<KetQuaMon> LanHoc { get; set; } = new();
    public List<GpaHocKyResult> GpaHocKy { get; set; } = new();
    public BaoCaoHocLucRow? TongKet { get; set; }
}

/// <summary>Báo cáo học lực tích lũy (pipeline BaoCaoHocLuc).</summary>
public class BaoCaoHocLucApi
{
    /// <summary>null = toàn bộ phạm vi được phép (Admin: toàn trường; GV: SV của mình).</summary>
    public string? MaLop { get; set; }
    /// <summary>Tên lớp của MaLop; null khi không lọc lớp.</summary>
    public string? TenLop { get; set; }
    public int SoSinhVien { get; set; }
    /// <summary>Thời gian chạy pipeline (ms).</summary>
    public double ThoiGianMs { get; set; }
    public List<BaoCaoHocLucRow> DuLieu { get; set; } = new();
}

/// <summary>Ghi danh một sinh viên vào một lớp học phần (body của POST /api/sinhvien/{maSV}/monhoc).</summary>
public class GhiDanhApiForm
{
    /// <summary>Năm học dạng "2026-2027".</summary>
    [Required(ErrorMessage = "Vui lòng nhập năm học.")]
    public string NamHoc { get; set; } = "";

    /// <summary>1, 2 hoặc 3 (học kỳ hè).</summary>
    [Range(1, 3, ErrorMessage = "Học kỳ phải là 1, 2 hoặc 3.")]
    public int HocKy { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã môn.")]
    public string MaMon { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng nhập mã giảng viên.")]
    public string MaGV { get; set; } = "";
}

/// <summary>
/// Điểm thành phần của một lần học (body của PUT .../diem). Gửi đủ 3 thành phần như "Lưu từng dòng" ở G-03:
/// null = xóa điểm thành phần đó. Mỗi điểm 0–10, tối đa 1 chữ số thập phân.
/// </summary>
public class DiemApiForm
{
    public double? Cc { get; set; }
    public double? Gk { get; set; }
    public double? Ck { get; set; }
}
