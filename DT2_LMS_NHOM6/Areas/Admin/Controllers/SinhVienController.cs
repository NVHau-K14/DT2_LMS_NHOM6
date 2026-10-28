using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>
/// Sinh viên: danh sách phân trang (A-04), thêm / sửa (A-05), hồ sơ + bảng điểm (A-06), xóa,
/// reset mật khẩu, khóa / mở khóa tài khoản (A9).
/// </summary>
public class SinhVienController : AdminControllerBase
{
    private const string DuongDanDanhSach = "/Admin/SinhVien";

    private readonly SinhVienService _sinhVien;
    private readonly LopHocService _lopHoc;
    private readonly TaiKhoanService _taiKhoan;
    private readonly BaoCaoService _baoCao;
    private readonly DiemService _diem;
    private readonly XuatExcelService _xuat;

    public SinhVienController(SinhVienService sinhVien, LopHocService lopHoc, TaiKhoanService taiKhoan,
        BaoCaoService baoCao, DiemService diem, XuatExcelService xuat)
    {
        _xuat = xuat;
        _sinhVien = sinhVien;
        _lopHoc = lopHoc;
        _taiKhoan = taiKhoan;
        _baoCao = baoCao;
        _diem = diem;
    }

    /// <summary>A-04: lọc lớp / trạng thái, tìm MSSV hoặc tên, phân trang phía server 20 dòng.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? maLop, string? trangThai, string? q, int page = 1)
    {
        var kichThuoc = PhanTrangViewModel.KichThuocMacDinh;
        var (danhSach, tongSo) = await _sinhVien.DanhSachAsync(maLop, trangThai, q, Math.Max(1, page), kichThuoc);

        // Trang vượt quá số trang (ví dụ sau khi lọc) → về trang cuối
        var tongSoTrang = (int)Math.Max(1, (tongSo + kichThuoc - 1) / kichThuoc);
        if (page > tongSoTrang)
            (danhSach, _) = await _sinhVien.DanhSachAsync(maLop, trangThai, q, page = tongSoTrang, kichThuoc);
        page = Math.Max(1, page);

        // GPA / xếp loại chỉ cho đúng các SV của trang này: BaoCaoHocLuc với { maSV: { $in: [...] } }
        var dsMaSV = danhSach.Select(sv => sv.MaSV).ToList();
        var hocLuc = dsMaSV.Count == 0
            ? new Dictionary<string, Models.Results.BaoCaoHocLucRow>()
            : (await _baoCao.BaoCaoHocLucAsync(BoLocSinhVien.TheoDanhSach(dsMaSV), PhamViBaoCao.TichLuy()))
                .DuLieu.ToDictionary(r => r.MaSV);
        var taiKhoan = await _taiKhoan.LayTheoMaAsync(HangSo.VaiTro.SinhVien, dsMaSV);

        var thamSo = new Dictionary<string, string?> { ["maLop"] = maLop, ["trangThai"] = trangThai, ["q"] = q };
        var phanTrang = new PhanTrangViewModel
        {
            Trang = page,
            KichThuoc = kichThuoc,
            TongSo = tongSo,
            DuongDan = DuongDanDanhSach,
            ThamSo = thamSo
        };

        return View(new SinhVienIndexViewModel
        {
            DanhSach = danhSach.Select(sv => new DongSinhVien(
                sv, hocLuc.GetValueOrDefault(sv.MaSV), taiKhoan.GetValueOrDefault(sv.MaSV))).ToList(),
            MaLop = maLop,
            TrangThai = trangThai,
            Q = q,
            DsLop = await _lopHoc.TatCaAsync(),
            PhanTrang = phanTrang,
            UrlHienTai = phanTrang.UrlTrang(page)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Them(string? maLop)
    {
        var form = new SinhVienFormViewModel { MaLop = maLop ?? "", DsLop = await _lopHoc.TatCaAsync() };
        return View("Form", form);
    }

    [HttpPost]
    public async Task<IActionResult> Them(SinhVienFormViewModel form)
    {
        form.LaSua = false;
        if (ModelState.IsValid)
        {
            var ketQua = await _sinhVien.ThemAsync(form);
            if (ketQua.ThanhCong)
            {
                ThongBaoKetQua(ketQua);
                return RedirectToAction(nameof(HoSo), new { id = form.MaSV.Trim() });
            }
            GanLoi(ketQua);
        }
        form.DsLop = await _lopHoc.TatCaAsync();
        return View("Form", form);
    }

    [HttpGet]
    public async Task<IActionResult> Sua(string id)
    {
        var sv = await _sinhVien.LayAsync(id);
        if (sv == null) return NotFound();

        return View("Form", new SinhVienFormViewModel
        {
            LaSua = true,
            MaSV = sv.MaSV,
            HoTen = sv.HoTen,
            NgaySinh = sv.NgaySinh,
            GioiTinh = sv.GioiTinh,
            Email = sv.Email,
            SoDienThoai = sv.SoDienThoai,
            DiaChi = sv.DiaChi,
            MaLop = sv.MaLop,
            TrangThai = sv.TrangThai,
            DsLop = await _lopHoc.TatCaAsync()
        });
    }

    /// <summary>MSSV lấy từ route (id) – giá trị MSSV trên form bị bỏ qua, nên không thể đổi MSSV.</summary>
    [HttpPost]
    public async Task<IActionResult> Sua(string id, SinhVienFormViewModel form)
    {
        form.LaSua = true;
        form.MaSV = id;
        ModelState.Remove(nameof(form.MaSV));
        if (ModelState.IsValid)
        {
            var ketQua = await _sinhVien.SuaAsync(id, form);
            if (ketQua.ThanhCong)
            {
                ThongBaoKetQua(ketQua);
                return RedirectToAction(nameof(HoSo), new { id });
            }
            GanLoi(ketQua);
        }
        form.DsLop = await _lopHoc.TatCaAsync();
        return View("Form", form);
    }

    /// <summary>A-06: thông tin + tóm tắt học lực + bảng điểm theo học kỳ (pipeline BangDiem, GpaHocKy, BaoCaoHocLuc).</summary>
    [HttpGet]
    public async Task<IActionResult> HoSo(string id)
    {
        var sv = await _sinhVien.LayAsync(id);
        if (sv == null) return NotFound();

        var bangDiem = await _diem.BangDiemAsync(id);
        var gpaHocKy = await _diem.GpaHocKyAsync(id);
        var tongKet = (await _baoCao.BaoCaoHocLucAsync(BoLocSinhVien.TheoSinhVien(id), PhamViBaoCao.TichLuy()))
            .DuLieu.FirstOrDefault();
        var lop = await _lopHoc.LayAsync(sv.MaLop);

        var vmBangDiem = BangDiemViewModel.Tao(bangDiem.DuLieu, gpaHocKy.DuLieu, tongKet);
        vmBangDiem.HienTongKetCuoi = false; // A-06 đã có thẻ tóm tắt ở đầu trang
        vmBangDiem.QuayLaiHuyGhiDanh = $"/Admin/SinhVien/HoSo/{id}"; // hiện nút "Hủy ghi danh" ở dòng chưa có điểm
        return View(new HoSoSinhVienViewModel
        {
            SinhVien = sv,
            TenLop = lop?.TenLop,
            TaiKhoan = await _taiKhoan.LayAsync(HangSo.VaiTro.SinhVien, id),
            BangDiem = vmBangDiem
        });
    }

    [HttpPost]
    public async Task<IActionResult> Xoa(string id, string? quayLai)
    {
        var ketQua = await _sinhVien.XoaAsync(id);
        ThongBaoKetQua(ketQua);
        // Xóa thành công từ trang hồ sơ → không quay lại hồ sơ (đã mất) mà về danh sách
        if (ketQua.ThanhCong && quayLai != null && quayLai.Contains("/HoSo/", StringComparison.OrdinalIgnoreCase))
            quayLai = null;
        return QuayLai(quayLai, DuongDanDanhSach);
    }

    [HttpPost]
    public async Task<IActionResult> ResetMatKhau(string id, string? quayLai)
    {
        ThongBaoKetQua(await _sinhVien.ResetMatKhauAsync(id));
        return QuayLai(quayLai, DuongDanDanhSach);
    }

    [HttpPost]
    public async Task<IActionResult> KhoaTaiKhoan(string id, string? quayLai)
    {
        ThongBaoKetQua(await _sinhVien.DoiTrangThaiTaiKhoanAsync(id));
        return QuayLai(quayLai, DuongDanDanhSach);
    }

    /// <summary>A-04 "Xuất Excel": mọi SV khớp bộ lọc đang chọn (không phân trang).</summary>
    [HttpGet]
    public async Task<IActionResult> XuatExcel(string? maLop, string? trangThai, string? q)
    {
        var tep = await _xuat.DanhSachSinhVienAsync(maLop, trangThai, q);
        return File(tep.NoiDung, ExcelService.LoaiNoiDung, tep.TenFile);
    }

    /// <summary>A-06 "Xuất bảng điểm": bảng điểm cá nhân theo học kỳ + tổng kết.</summary>
    [HttpGet]
    public async Task<IActionResult> XuatBangDiem(string id)
    {
        var tep = await _xuat.BangDiemCaNhanAsync(id);
        if (tep == null) return NotFound();
        return File(tep.NoiDung, ExcelService.LoaiNoiDung, tep.TenFile);
    }
}
