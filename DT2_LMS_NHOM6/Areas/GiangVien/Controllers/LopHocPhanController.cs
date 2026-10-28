using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.GiangVien.Controllers;

/// <summary>G-02: mọi lớp học phần của GV (pipeline LopHocPhanCuaGV), lọc theo học kỳ.</summary>
public class LopHocPhanController : GiangVienControllerBase
{
    private readonly DiemService _diem;

    public LopHocPhanController(DiemService diem)
    {
        _diem = diem;
    }

    /// <param name="ky">Học kỳ dạng "2026-2027_1"; trống → mọi học kỳ.</param>
    public async Task<IActionResult> Index(string? ky)
    {
        // Không lọc học kỳ: dùng luôn kết quả này để lập danh sách học kỳ cho ô lọc
        var tatCa = (await _diem.LopHocPhanCuaGVAsync(MaGV)).DuLieu;
        var dsHocKy = tatCa.Select(l => (l.NamHoc, l.HocKy)).Distinct().ToList(); // pipeline đã sắp mới nhất trước

        var danhSach = LopHocPhanViewModel.TryParseKy(ky, out var namHoc, out var hocKy)
            ? (await _diem.LopHocPhanCuaGVAsync(MaGV, namHoc, hocKy)).DuLieu
            : tatCa;

        return View(new LopHocPhanViewModel
        {
            KyChon = string.IsNullOrEmpty(ky) ? null : ky,
            DsHocKy = dsHocKy,
            DanhSach = danhSach
        });
    }
}
