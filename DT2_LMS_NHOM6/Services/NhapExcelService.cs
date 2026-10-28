using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.ViewModels;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Nhập Excel (A7, G4 – thiết kế mục 6). Nguyên tắc: KIỂM TRA TOÀN BỘ FILE trước, có dòng lỗi → không ghi gì;
/// sạch → người dùng bấm Xác nhận → ghi bằng thao tác hàng loạt (insertMany / bulkWrite).
/// Đọc file do ExcelService làm; lớp này đối chiếu với dữ liệu trong MongoDB và gọi service ghi.
/// </summary>
public class NhapExcelService
{
    private static readonly Regex DangMaSV = new(@"^\d{10}$", RegexOptions.Compiled);
    private static readonly Regex DangSoDienThoai = new(@"^0\d{9}$", RegexOptions.Compiled);
    private static readonly string[] DinhDangNgay = ["dd/MM/yyyy", "d/M/yyyy"];

    private readonly MongoContext _db;
    private readonly DiemService _diem;
    private readonly SinhVienService _sinhVien;
    private readonly TaiKhoanService _taiKhoan;

    public NhapExcelService(MongoContext db, DiemService diem, SinhVienService sinhVien, TaiKhoanService taiKhoan)
    {
        _db = db;
        _diem = diem;
        _sinhVien = sinhVien;
        _taiKhoan = taiKhoan;
    }

    /// <summary>Kiểm tra file tải lên ở mức tệp: có chọn file, đuôi .xlsx, ≤ 5 MB. null = hợp lệ.</summary>
    public static string? KiemTraTep(IFormFile? tep)
    {
        if (tep == null || tep.Length == 0) return "Vui lòng chọn file Excel cần nhập.";
        if (!string.Equals(Path.GetExtension(tep.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return "Chỉ nhận file Excel .xlsx.";
        if (tep.Length > ExcelService.KichThuocToiDa) return "File vượt quá 5 MB.";
        return null;
    }

    // =======================================================================
    // DANH SÁCH SINH VIÊN
    // =======================================================================

    /// <summary>
    /// Đọc và kiểm tra từng dòng file danh sách SV. Lỗi: MSSV sai định dạng / đã tồn tại / trùng trong file;
    /// lớp không tồn tại; ngày sinh sai; giới tính sai; (thêm) họ tên, email, SĐT sai giống form A-05.
    /// </summary>
    /// <exception cref="LoiFileExcelException">File không đọc được / sai mẫu.</exception>
    public async Task<PhienNhapSinhVien> KiemTraSinhVienAsync(Stream stream, string tenFile)
    {
        var cacDong = ExcelService.DocDanhSachSinhVien(stream);

        // Tra cứu DB một lần cho cả file: { maSV: { $in: [...] } }, { tenDangNhap: { $in: [...] } }, danh sách lớp
        var dsMa = cacDong.Select(d => d.MaSV).Where(m => m.Length > 0).Distinct().ToList();
        var maDaCo = (await _db.SinhVien.Find(Builders<SinhVien>.Filter.In(sv => sv.MaSV, dsMa))
            .Project(sv => sv.MaSV).ToListAsync()).ToHashSet();
        var tenDangNhapDaDung = await _taiKhoan.TenDangNhapDaDungAsync(dsMa);
        var dsLop = (await _db.LopHoc.Find(FilterDefinition<LopHoc>.Empty).Project(l => l.MaLop).ToListAsync()).ToHashSet();

        // MSSV xuất hiện nhiều lần trong file → số các dòng chứa nó
        var dongTheoMa = cacDong.Where(d => d.MaSV.Length > 0)
            .GroupBy(d => d.MaSV).Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(d => d.SoDong).ToList());

        var emailHopLe = new EmailAddressAttribute();
        var namToiThieu = DateTime.Today.AddYears(-15);
        foreach (var d in cacDong)
        {
            // MSSV
            if (!DangMaSV.IsMatch(d.MaSV))
                d.Loi.Add(d.MaSV.Length == 0 ? "Thiếu MSSV." : "MSSV phải gồm đúng 10 chữ số.");
            else if (maDaCo.Contains(d.MaSV))
                d.Loi.Add("MSSV đã tồn tại trong hệ thống.");
            else if (tenDangNhapDaDung.Contains(d.MaSV))
                d.Loi.Add("MSSV đã được dùng làm tên đăng nhập của tài khoản khác.");
            if (dongTheoMa.TryGetValue(d.MaSV, out var cacDongTrung))
                d.Loi.Add($"MSSV trùng trong file (dòng {string.Join(", ", cacDongTrung)}).");

            // Họ tên
            if (d.HoTen.Length < 2 || d.HoTen.Length > 100)
                d.Loi.Add("Họ tên dài từ 2 đến 100 ký tự.");

            // Ngày sinh dd/MM/yyyy, từ năm 1950 và đủ 15 tuổi (cùng quy tắc form A-05)
            if (DateTime.TryParseExact(d.NgaySinh, DinhDangNgay, CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngaySinh))
            {
                if (ngaySinh.Year < 1950 || ngaySinh > namToiThieu)
                    d.Loi.Add("Ngày sinh phải từ năm 1950 và sinh viên đủ 15 tuổi.");
                else
                    d.NgaySinhDaDoc = DateTime.SpecifyKind(ngaySinh.Date, DateTimeKind.Utc);
            }
            else
            {
                d.Loi.Add(d.NgaySinh.Length == 0 ? "Thiếu ngày sinh." : $"Ngày sinh \"{d.NgaySinh}\" sai (cần dạng dd/MM/yyyy, ngày có thật).");
            }

            // Giới tính: chấp nhận "nam" / "NỮ"... rồi lưu đúng chuỗi enum
            var gioiTinh = SinhVienFormViewModel.DsGioiTinh.FirstOrDefault(g => string.Equals(g, d.GioiTinh, StringComparison.OrdinalIgnoreCase));
            if (gioiTinh == null) d.Loi.Add("Giới tính phải là Nam hoặc Nữ.");
            else d.GioiTinh = gioiTinh;

            // Email, SĐT, địa chỉ: không bắt buộc
            if (d.Email.Length > 0 && (d.Email.Length > 100 || !emailHopLe.IsValid(d.Email)))
                d.Loi.Add("Email không hợp lệ.");
            if (d.SoDienThoai.Length > 0 && !DangSoDienThoai.IsMatch(d.SoDienThoai))
                d.Loi.Add("SĐT gồm 10 chữ số, bắt đầu bằng 0.");
            if (d.DiaChi.Length > 200)
                d.Loi.Add("Địa chỉ tối đa 200 ký tự.");

            // Mã lớp
            d.MaLop = d.MaLop.ToUpperInvariant();
            if (d.MaLop.Length == 0) d.Loi.Add("Thiếu mã lớp.");
            else if (!dsLop.Contains(d.MaLop)) d.Loi.Add($"Lớp {d.MaLop} không tồn tại.");
        }

        return new PhienNhapSinhVien { TenFile = tenFile, Dong = cacDong };
    }

    /// <summary>Ghi các SV của một phiên đã kiểm tra sạch: insertMany sinhvien + taikhoan.</summary>
    public Task<KetQuaGhi> GhiSinhVienAsync(PhienNhapSinhVien phien)
    {
        if (!phien.CoTheGhi)
            return Task.FromResult(KetQuaGhi.Loi("File còn dòng lỗi nên không thể ghi. Hãy sửa file rồi tải lên lại."));

        static string? RongThanhNull(string s) => s.Length == 0 ? null : s;
        var ds = phien.Dong.Select(d => new SinhVien
        {
            MaSV = d.MaSV,
            HoTen = d.HoTen,
            NgaySinh = d.NgaySinhDaDoc!.Value,
            GioiTinh = d.GioiTinh,
            Email = RongThanhNull(d.Email),
            SoDienThoai = RongThanhNull(d.SoDienThoai),
            DiaChi = RongThanhNull(d.DiaChi),
            MaLop = d.MaLop,
            TrangThai = HangSo.TrangThaiSinhVien.DangHoc
        }).ToList();
        return _sinhVien.ThemNhieuAsync(ds);
    }

    // =======================================================================
    // BẢNG ĐIỂM LỚP HỌC PHẦN
    // =======================================================================

    /// <summary>
    /// Đọc file bảng điểm, xác định lớp học phần từ phần đầu file, đối chiếu từng dòng với danh sách lớp học phần
    /// (pipeline DanhSachNhapDiem). <paramref name="maGVDangNhap"/> = null khi Admin nhập (mọi lớp học phần);
    /// GV chỉ nhập được file của lớp học phần mình dạy.
    /// Lỗi dòng: MSSV không thuộc lớp học phần / trùng trong file; điểm không phải số, ngoài 0–10, quá 1 chữ số thập phân.
    /// Ô trống = giữ nguyên điểm cũ.
    /// </summary>
    /// <exception cref="LoiFileExcelException">File không đọc được / sai mẫu / lớp học phần không hợp lệ.</exception>
    public async Task<PhienNhapDiem> KiemTraBangDiemAsync(Stream stream, string tenFile, string? maGVDangNhap)
    {
        var (lhp, cacDong) = ExcelService.DocBangDiem(stream);

        if (maGVDangNhap != null && lhp.MaGV != maGVDangNhap)
            throw new LoiFileExcelException($"File là bảng điểm lớp học phần {lhp.MaMon} của giảng viên {lhp.MaGV}. Bạn chỉ được nhập điểm cho lớp học phần mình dạy.");

        // Lớp học phần = (maMon, namHoc, hocKy, maGV) – luôn lọc theo maGV ghi trong file
        var lopHocPhan = await _diem.LayLopHocPhanAsync(lhp.MaGV, lhp.MaMon, lhp.NamHoc, lhp.HocKy)
            ?? throw new LoiFileExcelException($"Không tìm thấy lớp học phần {lhp.MaMon} – {HienThi.HocKy(lhp.NamHoc, lhp.HocKy)} của giảng viên {lhp.MaGV}"
                + (maGVDangNhap != null ? " mà bạn dạy." : "."));
        var dsLop = (await _diem.DanhSachNhapDiemAsync(lhp.MaGV, lhp.MaMon, lhp.NamHoc, lhp.HocKy)).DuLieu;

        lhp.TenMon = lopHocPhan.TenMon;
        lhp.TrongSo = lopHocPhan.TrongSo;
        lhp.TenGV = dsLop.FirstOrDefault()?.GiangVien.HoTen ?? lhp.MaGV;

        var theoMa = dsLop.ToDictionary(d => d.MaSV);
        var dongTheoMa = cacDong.Where(d => d.MaSV.Length > 0)
            .GroupBy(d => d.MaSV).Where(g => g.Count() > 1)
            .ToDictionary(g => g.Key, g => g.Select(d => d.SoDong).ToList());

        foreach (var d in cacDong)
        {
            if (d.MaSV.Length == 0)
                d.Loi.Add("Thiếu MSSV.");
            else if (!theoMa.TryGetValue(d.MaSV, out var trongLop))
                d.Loi.Add("MSSV không thuộc lớp học phần này.");
            else
            {
                // Họ tên, lớp, điểm cũ lấy từ DB (không tin cột Họ tên / Lớp trong file)
                d.HoTen = trongLop.HoTen;
                d.MaLop = trongLop.MaLop;
                d.MaDK = trongLop.MaDK;
                d.DiemCu = trongLop.Diem;
            }
            if (dongTheoMa.TryGetValue(d.MaSV, out var cacDongTrung))
                d.Loi.Add($"MSSV trùng trong file (dòng {string.Join(", ", cacDongTrung)}).");

            // Cùng hàm kiểm tra với ô nhập G-03: chấp nhận "8,5" / "8.5", 0–10, ≤ 1 chữ số thập phân, trống = null
            if (DiemNhap.TryParse(d.CcNhap, out var cc, out var loi)) d.CcMoi = cc; else d.Loi.Add("CC: " + loi);
            if (DiemNhap.TryParse(d.GkNhap, out var gk, out loi)) d.GkMoi = gk; else d.Loi.Add("GK: " + loi);
            if (DiemNhap.TryParse(d.CkNhap, out var ck, out loi)) d.CkMoi = ck; else d.Loi.Add("CK: " + loi);
        }

        return new PhienNhapDiem { TenFile = tenFile, LopHocPhan = lhp, Dong = cacDong, SiSo = dsLop.Count };
    }

    /// <summary>
    /// Ghi các dòng có thay đổi bằng bulkWrite $set + arrayFilters.
    /// GV: arrayFilters kèm "e.giangVien.maGV"; Admin (maGV null): capNhatBoi "admin".
    /// </summary>
    public Task<KetQuaGhi> GhiBangDiemAsync(PhienNhapDiem phien, string? maGVDangNhap)
    {
        if (phien.SoLoi > 0)
            return Task.FromResult(KetQuaGhi.Loi("File còn dòng lỗi nên không thể ghi. Hãy sửa file rồi tải lên lại."));
        var cacDong = phien.Dong.Where(d => d.CoThayDoi).Select(d => d.TaoDongGhi()).ToList();
        return _diem.GhiDiemTuExcelAsync(maGVDangNhap, cacDong);
    }
}
