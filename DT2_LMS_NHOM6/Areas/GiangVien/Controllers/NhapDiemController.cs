using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.GiangVien.Controllers;

/// <summary>
/// G-03 Nhập điểm một lớp học phần: /GiangVien/NhapDiem?maMon=INT301&amp;namHoc=2026-2027&amp;hocKy=1
/// - Trang: chỉ mở được lớp học phần GV đang dạy (không → 403).
/// - Lưu từng dòng / Lưu tất cả: fetch POST JSON (antiforgery qua header RequestVerificationToken).
///   Quyền sửa điểm nằm trong arrayFilters của lệnh update (e.giangVien.maGV = GV đăng nhập).
/// </summary>
public class NhapDiemController : GiangVienControllerBase
{
    private readonly DiemService _diem;

    public NhapDiemController(DiemService diem)
    {
        _diem = diem;
    }

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
}
