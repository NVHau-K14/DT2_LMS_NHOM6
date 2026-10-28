using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;
using MongoDB.Bson;

namespace DT2_LMS_NHOM6.ViewModels;

// ===========================================================================
// Nhập Excel (thiết kế mục 6): đọc file → KIỂM TRA TOÀN BỘ → xem trước (dòng hợp lệ xanh / lỗi đỏ)
// → không có lỗi mới cho Xác nhận ghi. Kết quả kiểm tra giữ tạm trong bộ nhớ (KhoPhienNhap) giữa 2 bước.
// ===========================================================================

/// <summary>Một dòng của file danh sách sinh viên (giá trị thô đọc từ ô + lỗi kiểm tra).</summary>
public class DongNhapSinhVien
{
    /// <summary>Số dòng trong Excel (để người dùng tìm đúng dòng cần sửa).</summary>
    public int SoDong { get; set; }
    public string MaSV { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string NgaySinh { get; set; } = "";
    public string GioiTinh { get; set; } = "";
    public string Email { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string DiaChi { get; set; } = "";
    public string MaLop { get; set; } = "";

    /// <summary>Ngày sinh đã đọc được (00:00 UTC); null nếu sai định dạng.</summary>
    public DateTime? NgaySinhDaDoc { get; set; }

    public List<string> Loi { get; set; } = new();
    public bool HopLe => Loi.Count == 0;
}

/// <summary>Kết quả kiểm tra file danh sách sinh viên.</summary>
public class PhienNhapSinhVien
{
    public string TenFile { get; set; } = "";
    public List<DongNhapSinhVien> Dong { get; set; } = new();
    public int SoLoi => Dong.Count(d => !d.HopLe);
    public bool CoTheGhi => Dong.Count > 0 && SoLoi == 0;
}

/// <summary>Trang xem trước nhập danh sách SV (A-11).</summary>
public class XemTruocNhapSinhVienViewModel
{
    public string MaPhien { get; set; } = "";
    public PhienNhapSinhVien Phien { get; set; } = new();
}

/// <summary>Thông tin lớp học phần ghi ở phần đầu file bảng điểm.</summary>
public class ThongTinLopHocPhanExcel
{
    public string MaMon { get; set; } = "";
    public string TenMon { get; set; } = "";
    public string NamHoc { get; set; } = "";
    public int HocKy { get; set; }
    public string MaGV { get; set; } = "";
    /// <summary>Họ tên kèm học vị.</summary>
    public string TenGV { get; set; } = "";
    public TrongSo TrongSo { get; set; } = new();
}

/// <summary>Một dòng của file bảng điểm: điểm mới (đã chuẩn hóa) so với điểm hiện có trong DB.</summary>
public class DongNhapDiemExcel
{
    public int SoDong { get; set; }
    public string MaSV { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string MaLop { get; set; } = "";

    /// <summary>Giá trị thô trong ô (để hiện lại khi lỗi).</summary>
    public string CcNhap { get; set; } = "";
    public string GkNhap { get; set; } = "";
    public string CkNhap { get; set; } = "";

    /// <summary>Điểm mới; null = ô trống (giữ nguyên điểm cũ).</summary>
    public double? CcMoi { get; set; }
    public double? GkMoi { get; set; }
    public double? CkMoi { get; set; }

    /// <summary>Lần học tương ứng trong DB (null nếu MSSV không thuộc lớp học phần).</summary>
    public ObjectId? MaDK { get; set; }
    public DiemThanhPhan DiemCu { get; set; } = new();

    public List<string> Loi { get; set; } = new();
    public bool HopLe => Loi.Count == 0;

    public bool CcThayDoi => CcMoi.HasValue && CcMoi != DiemCu.Cc;
    public bool GkThayDoi => GkMoi.HasValue && GkMoi != DiemCu.Gk;
    public bool CkThayDoi => CkMoi.HasValue && CkMoi != DiemCu.Ck;
    public bool CoThayDoi => HopLe && (CcThayDoi || GkThayDoi || CkThayDoi);

    /// <summary>Các thành phần cần $set khi ghi: chỉ ô có giá trị mới khác điểm cũ.</summary>
    public DongGhiDiemExcel TaoDongGhi()
    {
        var diemMoi = new List<(string, double)>();
        if (CcThayDoi) diemMoi.Add(("cc", CcMoi!.Value));
        if (GkThayDoi) diemMoi.Add(("gk", GkMoi!.Value));
        if (CkThayDoi) diemMoi.Add(("ck", CkMoi!.Value));
        return new DongGhiDiemExcel(MaSV, MaDK!.Value, diemMoi);
    }
}

/// <summary>Một lệnh ghi điểm từ Excel: SV, lần học, các thành phần điểm mới ("cc"/"gk"/"ck" → giá trị).</summary>
public record DongGhiDiemExcel(string MaSV, ObjectId MaDK, List<(string ThanhPhan, double GiaTri)> DiemMoi);

/// <summary>Kết quả kiểm tra file bảng điểm của một lớp học phần.</summary>
public class PhienNhapDiem
{
    public string TenFile { get; set; } = "";
    public ThongTinLopHocPhanExcel LopHocPhan { get; set; } = new();
    public List<DongNhapDiemExcel> Dong { get; set; } = new();
    /// <summary>Sĩ số lớp học phần trong DB (để báo số SV không có trong file).</summary>
    public int SiSo { get; set; }

    public int SoLoi => Dong.Count(d => !d.HopLe);
    public int SoThayDoi => Dong.Count(d => d.CoThayDoi);
    public bool CoTheGhi => SoLoi == 0 && SoThayDoi > 0;
}

/// <summary>
/// Trang xem trước nhập bảng điểm – dùng chung Admin (A-11) và Giảng viên (G-03).
/// Các URL khác nhau theo Area nên truyền vào từ controller.
/// </summary>
public class XemTruocNhapDiemViewModel
{
    public string MaPhien { get; set; } = "";
    public PhienNhapDiem Phien { get; set; } = new();
    public string UrlXacNhan { get; set; } = "";
    /// <summary>Nơi gửi file khi tải lên lại.</summary>
    public string UrlTaiLen { get; set; } = "";
    public string UrlQuayLai { get; set; } = "";

    public string TenLopHocPhan
        => $"{Phien.LopHocPhan.MaMon} – {Phien.LopHocPhan.TenMon} · {HienThi.HocKy(Phien.LopHocPhan.NamHoc, Phien.LopHocPhan.HocKy)} · {Phien.LopHocPhan.TenGV}";
}

/// <summary>Modal chọn file Excel để nhập (dùng chung A-04, G-03, trang xem trước).</summary>
public record ModalNhapExcelViewModel(string Id, string TieuDe, string UrlNhap, string GhiChu, string? UrlTaiMau = null);

/// <summary>A-11 Nhập/Xuất dữ liệu.</summary>
public class DuLieuViewModel
{
    public List<LopHocPhanToanTruongRow> DsLopHocPhan { get; set; } = new();
    public List<LopHoc> DsLop { get; set; } = new();
    public List<HocKyRow> DsHocKy { get; set; } = new();
}
