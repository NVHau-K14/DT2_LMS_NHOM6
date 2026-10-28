using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DT2_LMS_NHOM6.Areas.GiangVien.Controllers;

/// <summary>
/// G-01 Tổng quan: lớp học phần của GV trong một học kỳ (mặc định học kỳ hiện tại) – pipeline LopHocPhanCuaGV.
/// </summary>
public class HomeController : GiangVienControllerBase
{
    private readonly DiemService _diem;
    private readonly HocKyHienTaiOptions _hocKyHienTai;

    public HomeController(DiemService diem, IOptions<HocKyHienTaiOptions> hocKyHienTai)
    {
        _diem = diem;
        _hocKyHienTai = hocKyHienTai.Value;
    }

    /// <param name="ky">Học kỳ dạng "2026-2027_1"; trống → học kỳ hiện tại.</param>
    public async Task<IActionResult> Index(string? ky)
    {
        if (!LopHocPhanViewModel.TryParseKy(ky, out var namHoc, out var hocKy))
            (namHoc, hocKy) = (_hocKyHienTai.NamHoc, _hocKyHienTai.HocKy);

        return View(new LopHocPhanViewModel
        {
            KyChon = LopHocPhanViewModel.MaKy(namHoc, hocKy),
            DsHocKy = await DsHocKyAsync(),
            DanhSach = (await _diem.LopHocPhanCuaGVAsync(MaGV, namHoc, hocKy)).DuLieu
        });
    }

    /// <summary>Học kỳ GV có dạy (từ pipeline, mọi học kỳ) + học kỳ hiện tại, mới nhất trước.</summary>
    private async Task<List<(string NamHoc, int HocKy)>> DsHocKyAsync()
    {
        var tatCa = (await _diem.LopHocPhanCuaGVAsync(MaGV)).DuLieu;
        return tatCa.Select(l => (l.NamHoc, l.HocKy))
            .Append((_hocKyHienTai.NamHoc, _hocKyHienTai.HocKy))
            .Distinct()
            .OrderByDescending(k => k.Item1).ThenByDescending(k => k.Item2)
            .ToList();
    }
}
