using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>
/// A-12 Hiệu năng (tùy chọn): explain có / không index cho 3 truy vấn chính và thời gian pipeline BaoCaoHocLuc 20 lần.
/// Chỉ đọc nên dùng GET (form "Chạy đo" gửi chay=true).
/// </summary>
public class HieuNangController : AdminControllerBase
{
    private readonly HieuNangService _hieuNang;
    private readonly BaoCaoService _baoCao;

    public HieuNangController(HieuNangService hieuNang, BaoCaoService baoCao)
    {
        _hieuNang = hieuNang;
        _baoCao = baoCao;
    }

    /// <param name="ky">Học kỳ dạng "2026-2027_1".</param>
    /// <param name="chay">true = bấm "Chạy đo"; false = chỉ hiện form với giá trị mặc định.</param>
    [HttpGet]
    public async Task<IActionResult> Index(string? maSV, string? maGV, string? ky, string? maMon, bool chay = false)
    {
        var vm = new HieuNangViewModel
        {
            DsHocKy = await _baoCao.HocKyCoDuLieuAsync(),
            SoSinhVien = await _hieuNang.DemSinhVienAsync()
        };

        var ts = await _hieuNang.ThamSoMacDinhAsync();
        if (chay)
        {
            // Giá trị người dùng sửa trên form (trống → giữ mặc định)
            if (!string.IsNullOrWhiteSpace(maSV)) ts.MaSV = maSV.Trim();
            if (!string.IsNullOrWhiteSpace(maGV)) ts.MaGV = maGV.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(maMon)) ts.MaMon = maMon.Trim().ToUpperInvariant();
            if (LopHocPhanViewModel.TryParseKy(ky, out var namHoc, out var hocKy))
            {
                ts.NamHoc = namHoc;
                ts.HocKy = hocKy;
            }
        }
        vm.ThamSo = ts;

        if (chay)
        {
            if (vm.SoSinhVien == 0)
                vm.Loi = "Collection sinhvien đang trống – hãy chạy lại dữ liệu mẫu (--reset) rồi đo.";
            else
            {
                vm.TruyVan = await _hieuNang.SoSanhTruyVanAsync(ts);
                vm.Pipeline = await _hieuNang.DoPipelineAsync();
                vm.DaChay = true;
            }
        }
        return View(vm);
    }
}
