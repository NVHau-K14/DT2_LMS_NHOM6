using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.GiangVien.Controllers;

/// <summary>
/// G-03 Nhập điểm một lớp học phần: /GiangVien/NhapDiem?maMon=INT301&amp;namHoc=2026-2027&amp;hocKy=1
/// - Trang: chỉ mở được lớp học phần GV đang dạy (không → 403).
/// - Lưu từng dòng / Lưu tất cả: fetch POST JSON (antiforgery qua header RequestVerificationToken).
///   Quyền sửa điểm nằm trong arrayFilters của lệnh update (e.giangVien.maGV = GV đăng nhập).
/// - Excel (G4): Xuất Excel = file mẫu nhập điểm; Nhập Excel → kiểm tra toàn bộ → xem trước → Xác nhận (bulkWrite).
/// </summary>
public class NhapDiemController : GiangVienControllerBase
{
    private readonly DiemService _diem;
    private readonly XuatExcelService _xuat;
    private readonly NhapExcelService _nhap;
    private readonly KhoPhienNhap _kho;

    public NhapDiemController(DiemService diem, XuatExcelService xuat, NhapExcelService nhap, KhoPhienNhap kho)
    {
        _diem = diem;
        _xuat = xuat;
        _nhap = nhap;
        _kho = kho;
    }

    /// <summary>Mỗi phiên nhập gắn với tài khoản đã tải file lên.</summary>
    private string ChuPhien => User.TenDangNhap();

    private static string UrlTrang(string maMon, string namHoc, int hocKy)
        => $"/GiangVien/NhapDiem?maMon={Uri.EscapeDataString(maMon)}&namHoc={Uri.EscapeDataString(namHoc)}&hocKy={hocKy}";

    [HttpGet]
    public async Task<IActionResult> Index(string maMon, string namHoc, int hocKy, string? lop)
    {
        if (string.IsNullOrEmpty(maMon) || string.IsNullOrEmpty(namHoc)) return NotFound();

        var lopHocPhan = await _diem.LayLopHocPhanAsync(MaGV, maMon, namHoc, hocKy);
        if (lopHocPhan == null) return StatusCode(StatusCodes.Status403Forbidden);

        return View(new NhapDiemViewModel
        {
            LopHocPhan = lopHocPhan,
            DanhSach = (await _diem.DanhSachNhapDiemAsync(MaGV, maMon, namHoc, hocKy)).DuLieu,
            LocLop = lop
        });
    }

    /// <summary>Lưu một dòng: updateOne + $set + arrayFilters, trả TK / chữ / hệ 4 mới (pipeline) và lệnh đã chạy.</summary>
    [HttpPost]
    public async Task<IActionResult> LuuDong([FromBody] DongLuuDiem? dong)
    {
        if (dong == null) return BadRequest(new KetQuaLuuDiemJson { ThongBao = "Dữ liệu gửi lên không hợp lệ." });
        return Json(await _diem.CapNhatDiemAsync(MaGV, dong));
    }

    /// <summary>Lưu tất cả dòng đã sửa: bulkWrite, rồi trả lại cả bảng từ pipeline DanhSachNhapDiem.</summary>
    [HttpPost]
    public async Task<IActionResult> LuuTatCa([FromBody] YeuCauLuuNhieuDiem? yc)
    {
        if (yc == null) return BadRequest(new KetQuaLuuDiemJson { ThongBao = "Dữ liệu gửi lên không hợp lệ." });
        return Json(await _diem.CapNhatNhieuDiemAsync(MaGV, yc));
    }

    /// <summary>Tải mẫu / Xuất bảng điểm: danh sách lớp học phần kèm điểm hiện có (chỉ lớp học phần GV dạy).</summary>
    [HttpGet]
    public async Task<IActionResult> XuatExcel(string maMon, string namHoc, int hocKy)
    {
        if (string.IsNullOrEmpty(maMon) || string.IsNullOrEmpty(namHoc)) return NotFound();
        var tep = await _xuat.BangDiemLopHocPhanAsync(MaGV, maMon, namHoc, hocKy);
        if (tep == null) return StatusCode(StatusCodes.Status403Forbidden);
        return File(tep.NoiDung, ExcelService.LoaiNoiDung, tep.TenFile);
    }

    /// <summary>
    /// Nhận file bảng điểm. Lớp học phần xác định từ PHẦN ĐẦU FILE (không từ trang đang mở);
    /// file của lớp học phần GV khác → báo lỗi, không ghi gì. maMon/namHoc/hocKy chỉ để quay lại G-03 khi lỗi.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(ExcelService.KichThuocToiDa + 1024 * 1024)]
    public async Task<IActionResult> NhapExcel(IFormFile? tep, string maMon, string namHoc, int hocKy)
    {
        var urlTrang = UrlTrang(maMon ?? "", namHoc ?? "", hocKy);
        var loiTep = NhapExcelService.KiemTraTep(tep);
        if (loiTep != null)
        {
            this.ThongBao(loiTep, "danger");
            return Redirect(urlTrang);
        }

        PhienNhapDiem phien;
        try
        {
            await using var stream = tep!.OpenReadStream();
            phien = await _nhap.KiemTraBangDiemAsync(stream, Path.GetFileName(tep.FileName), MaGV);
        }
        catch (LoiFileExcelException ex)
        {
            this.ThongBao(ex.Message, "danger");
            return Redirect(urlTrang);
        }
        return RedirectToAction(nameof(XemTruocExcel), new { ma = _kho.Luu(ChuPhien, phien) });
    }

    [HttpGet]
    public IActionResult XemTruocExcel(string? ma)
    {
        var phien = _kho.Lay<PhienNhapDiem>(ma, ChuPhien);
        if (phien == null) return PhienHetHan();
        var lhp = phien.LopHocPhan;
        return View(new XemTruocNhapDiemViewModel
        {
            MaPhien = ma!,
            Phien = phien,
            UrlXacNhan = Url.Action(nameof(XacNhanExcel))!,
            UrlTaiLen = Url.Action(nameof(NhapExcel), new { maMon = lhp.MaMon, namHoc = lhp.NamHoc, hocKy = lhp.HocKy })!,
            UrlQuayLai = UrlTrang(lhp.MaMon, lhp.NamHoc, lhp.HocKy)
        });
    }

    /// <summary>Ghi điểm: bulkWrite $set + arrayFilters có điều kiện e.giangVien.maGV = GV đăng nhập.</summary>
    [HttpPost]
    public async Task<IActionResult> XacNhanExcel(string? ma)
    {
        var phien = _kho.Lay<PhienNhapDiem>(ma, ChuPhien);
        if (phien == null) return PhienHetHan();

        var ketQua = await _nhap.GhiBangDiemAsync(phien, MaGV);
        this.ThongBao(ketQua.ThongBao, ketQua.ThanhCong ? "success" : "danger", ketQua.MongoCommand);
        if (!ketQua.ThanhCong) return RedirectToAction(nameof(XemTruocExcel), new { ma });

        _kho.Xoa(ma!);
        var lhp = phien.LopHocPhan;
        return Redirect(UrlTrang(lhp.MaMon, lhp.NamHoc, lhp.HocKy));
    }

    private IActionResult PhienHetHan()
    {
        this.ThongBao("Phiên nhập không còn (đã ghi xong hoặc quá 30 phút). Vui lòng tải file lên lại.", "warning");
        return Redirect("/GiangVien/LopHocPhan");
    }
}
