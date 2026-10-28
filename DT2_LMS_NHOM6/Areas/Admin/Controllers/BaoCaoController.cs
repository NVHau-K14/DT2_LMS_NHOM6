using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>
/// A-10 Báo cáo học lực: lọc lớp (tất cả / từng lớp) và phạm vi (tích lũy / từng học kỳ có dữ liệu).
/// Bảng = pipeline BaoCaoHocLuc; thẻ số + biểu đồ = pipeline ThongKeXepLoai (cùng bộ lọc, cùng phạm vi).
/// </summary>
public class BaoCaoController : AdminControllerBase
{
    private readonly BaoCaoService _baoCao;
    private readonly LopHocService _lopHoc;

    public BaoCaoController(BaoCaoService baoCao, LopHocService lopHoc)
    {
        _baoCao = baoCao;
        _lopHoc = lopHoc;
    }

    /// <param name="maLop">Trống = tất cả lớp.</param>
    /// <param name="phamVi">"tichluy" (mặc định) hoặc học kỳ dạng "2025-2026_2".</param>
    [HttpGet]
    public async Task<IActionResult> Index(string? maLop, string? phamVi)
    {
        var dsLop = await _lopHoc.TatCaAsync();
        var dsHocKy = await _baoCao.HocKyCoDuLieuAsync();

        // Chỉ nhận lớp / học kỳ có trong danh sách; giá trị lạ → quay về mặc định
        if (!dsLop.Any(l => l.MaLop == maLop)) maLop = null;
        var phamViBaoCao = PhamViBaoCao.TichLuy();
        var tenPhamVi = "Tích lũy";
        if (LopHocPhanViewModel.TryParseKy(phamVi, out var namHoc, out var hocKy)
            && dsHocKy.Any(k => k.NamHoc == namHoc && k.HocKy == hocKy))
        {
            phamViBaoCao = PhamViBaoCao.TheoHocKy(namHoc, hocKy);
            tenPhamVi = HienThi.HocKy(namHoc, hocKy);
        }
        else
        {
            phamVi = BaoCaoAdminViewModel.PhamViTichLuy;
        }

        var boLoc = maLop == null ? BoLocSinhVien.TatCa() : BoLocSinhVien.TheoLop(maLop);
        var baoCao = await _baoCao.BaoCaoHocLucAsync(boLoc, phamViBaoCao);
        var thongKe = await _baoCao.ThongKeXepLoaiAsync(boLoc, phamViBaoCao);

        return View(new BaoCaoAdminViewModel
        {
            MaLop = maLop,
            PhamVi = phamVi!,
            DsLop = dsLop,
            DsHocKy = dsHocKy,
            BaoCao = new BaoCaoHocLucViewModel
            {
                DanhSach = baoCao.DuLieu,
                ThongKe = BaoCaoHocLucViewModel.DuBayMuc(thongKe.DuLieu),
                TenPhamVi = tenPhamVi,
                LaTichLuy = phamViBaoCao.LaTichLuy,
                UrlHoSoSV = "/Admin/SinhVien/HoSo/",
                Pipelines =
                [
                    PipelineDaChay.Tu("BaoCaoHocLuc", "Bảng học lực: mỗi sinh viên một dòng (GPA, xếp loại, TC, số môn nợ).", baoCao),
                    PipelineDaChay.Tu("ThongKeXepLoai", "Thẻ số và biểu đồ: số sinh viên theo từng xếp loại (BaoCaoHocLuc + $group).", thongKe)
                ]
            }
        });
    }
}
