using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>
/// A-11 Nhập/Xuất dữ liệu (A7, thiết kế mục 6).
/// Luồng nhập: chọn file (POST) → kiểm tra toàn bộ, lưu kết quả vào KhoPhienNhap → chuyển sang trang xem trước (GET)
/// → không có lỗi: Xác nhận (POST) → ghi hàng loạt → toast. Có lỗi: không ghi gì, cho tải file khác lên.
/// </summary>
public class DuLieuController : AdminControllerBase
{
    private const string DuongDanTrang = "/Admin/DuLieu";

    private readonly NhapExcelService _nhap;
    private readonly XuatExcelService _xuat;
    private readonly KhoPhienNhap _kho;
    private readonly DiemService _diem;
    private readonly LopHocService _lopHoc;
    private readonly BaoCaoService _baoCao;

    public DuLieuController(NhapExcelService nhap, XuatExcelService xuat, KhoPhienNhap kho,
        DiemService diem, LopHocService lopHoc, BaoCaoService baoCao)
    {
        _nhap = nhap;
        _xuat = xuat;
        _kho = kho;
        _diem = diem;
        _lopHoc = lopHoc;
        _baoCao = baoCao;
    }

    /// <summary>Mỗi phiên nhập gắn với tài khoản đã tải file lên.</summary>
    private string ChuPhien => User.TenDangNhap();

    [HttpGet]
    public async Task<IActionResult> Index()
        => View(new DuLieuViewModel
        {
            DsLopHocPhan = await _diem.LopHocPhanToanTruongAsync(),
            DsLop = await _lopHoc.TatCaAsync(),
            DsHocKy = await _baoCao.HocKyCoDuLieuAsync()
        });

    // ---------------- Danh sách sinh viên ----------------

    [HttpGet]
    public async Task<IActionResult> MauSinhVien()
    {
        var tep = await _xuat.MauSinhVienAsync();
        return File(tep.NoiDung, ExcelService.LoaiNoiDung, tep.TenFile);
    }

    /// <param name="quayLai">Trang gửi file (A-04 hoặc A-11) – về lại đó nếu file không đọc được.</param>
    [HttpPost]
    [RequestSizeLimit(ExcelService.KichThuocToiDa + 1024 * 1024)]
    public async Task<IActionResult> NhapSinhVien(IFormFile? tep, string? quayLai)
    {
        var loiTep = NhapExcelService.KiemTraTep(tep);
        if (loiTep != null)
        {
            this.ThongBao(loiTep, "danger");
            return QuayLai(quayLai, DuongDanTrang);
        }

        PhienNhapSinhVien phien;
        try
        {
            await using var stream = tep!.OpenReadStream();
            phien = await _nhap.KiemTraSinhVienAsync(stream, Path.GetFileName(tep.FileName));
        }
        catch (LoiFileExcelException ex)
        {
            this.ThongBao(ex.Message, "danger");
            return QuayLai(quayLai, DuongDanTrang);
        }
        return RedirectToAction(nameof(XemTruocSinhVien), new { ma = _kho.Luu(ChuPhien, phien) });
    }

    [HttpGet]
    public IActionResult XemTruocSinhVien(string? ma)
    {
        var phien = _kho.Lay<PhienNhapSinhVien>(ma, ChuPhien);
        if (phien == null) return PhienHetHan();
        return View(new XemTruocNhapSinhVienViewModel { MaPhien = ma!, Phien = phien });
    }

    [HttpPost]
    public async Task<IActionResult> XacNhanSinhVien(string? ma)
    {
        var phien = _kho.Lay<PhienNhapSinhVien>(ma, ChuPhien);
        if (phien == null) return PhienHetHan();

        var ketQua = await _nhap.GhiSinhVienAsync(phien);
        ThongBaoKetQua(ketQua);
        if (!ketQua.ThanhCong) return RedirectToAction(nameof(XemTruocSinhVien), new { ma });

        _kho.Xoa(ma!);
        return Redirect("/Admin/SinhVien");
    }

    // ---------------- Bảng điểm lớp học phần ----------------

    /// <summary>Xuất bảng điểm một lớp học phần, <paramref name="lhp"/> dạng "INT301_2026-2027_1_GV005".</summary>
    [HttpGet]
    public async Task<IActionResult> XuatBangDiem(string? lhp)
    {
        var phan = lhp?.Split('_');
        if (phan is not { Length: 4 } || !int.TryParse(phan[2], out var hocKy)) return NotFound();

        var tep = await _xuat.BangDiemLopHocPhanAsync(phan[3], phan[0], phan[1], hocKy);
        if (tep == null) return NotFound();
        return File(tep.NoiDung, ExcelService.LoaiNoiDung, tep.TenFile);
    }

    /// <summary>Admin nhập file bảng điểm của BẤT KỲ lớp học phần nào (lớp học phần đọc từ phần đầu file).</summary>
    [HttpPost]
    [RequestSizeLimit(ExcelService.KichThuocToiDa + 1024 * 1024)]
    public async Task<IActionResult> NhapDiem(IFormFile? tep)
    {
        var loiTep = NhapExcelService.KiemTraTep(tep);
        if (loiTep != null)
        {
            this.ThongBao(loiTep, "danger");
            return Redirect(DuongDanTrang);
        }

        PhienNhapDiem phien;
        try
        {
            await using var stream = tep!.OpenReadStream();
            phien = await _nhap.KiemTraBangDiemAsync(stream, Path.GetFileName(tep.FileName), maGVDangNhap: null);
        }
        catch (LoiFileExcelException ex)
        {
            this.ThongBao(ex.Message, "danger");
            return Redirect(DuongDanTrang);
        }
        return RedirectToAction(nameof(XemTruocDiem), new { ma = _kho.Luu(ChuPhien, phien) });
    }

    [HttpGet]
    public IActionResult XemTruocDiem(string? ma)
    {
        var phien = _kho.Lay<PhienNhapDiem>(ma, ChuPhien);
        if (phien == null) return PhienHetHan();
        return View(new XemTruocNhapDiemViewModel
        {
            MaPhien = ma!,
            Phien = phien,
            UrlXacNhan = Url.Action(nameof(XacNhanDiem))!,
            UrlTaiLen = Url.Action(nameof(NhapDiem))!,
            UrlQuayLai = DuongDanTrang
        });
    }

    /// <summary>Ghi điểm: bulkWrite $set + arrayFilters (không điều kiện maGV, capNhatBoi "admin").</summary>
    [HttpPost]
    public async Task<IActionResult> XacNhanDiem(string? ma)
    {
        var phien = _kho.Lay<PhienNhapDiem>(ma, ChuPhien);
        if (phien == null) return PhienHetHan();

        var ketQua = await _nhap.GhiBangDiemAsync(phien, maGVDangNhap: null);
        ThongBaoKetQua(ketQua);
        if (!ketQua.ThanhCong) return RedirectToAction(nameof(XemTruocDiem), new { ma });

        _kho.Xoa(ma!);
        return Redirect(DuongDanTrang);
    }

    private IActionResult PhienHetHan()
    {
        this.ThongBao("Phiên nhập không còn (đã ghi xong hoặc quá 30 phút). Vui lòng tải file lên lại.", "warning");
        return Redirect(DuongDanTrang);
    }
}
