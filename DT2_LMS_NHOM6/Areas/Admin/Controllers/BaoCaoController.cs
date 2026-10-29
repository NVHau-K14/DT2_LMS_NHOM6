using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;
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
    private readonly XuatExcelService _xuat;

    public BaoCaoController(BaoCaoService baoCao, LopHocService lopHoc, XuatExcelService xuat)
    {
        _baoCao = baoCao;
        _lopHoc = lopHoc;
        _xuat = xuat;
    }

    /// <summary>Bộ lọc đã kiểm tra: lớp có thật (null = tất cả), phạm vi tích lũy hoặc một học kỳ có dữ liệu.</summary>
    private record BoLocBaoCao(string? MaLop, string PhamVi, PhamViBaoCao PhamViBaoCao, string TenPhamVi,
        List<LopHoc> DsLop, List<HocKyRow> DsHocKy)
    {
        public BoLocSinhVien BoLocSinhVien => MaLop == null ? BoLocSinhVien.TatCa() : BoLocSinhVien.TheoLop(MaLop);
    }

    /// <summary>Chỉ nhận lớp / học kỳ có trong danh sách; giá trị lạ → quay về mặc định (dùng chung Index và XuatExcel).</summary>
    private async Task<BoLocBaoCao> DocBoLocAsync(string? maLop, string? phamVi)
    {
        var dsLop = await _lopHoc.TatCaAsync();
        var dsHocKy = await _baoCao.HocKyCoDuLieuAsync();

        if (!dsLop.Any(l => l.MaLop == maLop)) maLop = null;
        if (LopHocPhanViewModel.TryParseKy(phamVi, out var namHoc, out var hocKy)
            && dsHocKy.Any(k => k.NamHoc == namHoc && k.HocKy == hocKy))
            return new BoLocBaoCao(maLop, phamVi!, PhamViBaoCao.TheoHocKy(namHoc, hocKy), HienThi.HocKy(namHoc, hocKy), dsLop, dsHocKy);

        return new BoLocBaoCao(maLop, BaoCaoAdminViewModel.PhamViTichLuy, PhamViBaoCao.TichLuy(), "Tích lũy", dsLop, dsHocKy);
    }

    /// <param name="maLop">Trống = tất cả lớp.</param>
    /// <param name="phamVi">"tichluy" (mặc định) hoặc học kỳ dạng "2025-2026_2".</param>
    /// <param name="trang">Trang bảng SV – mỗi trang một lớp sinh hoạt (mã lớp tăng dần); ngoài phạm vi → kẹp về 1 / trang cuối.</param>
    [HttpGet]
    public async Task<IActionResult> Index(string? maLop, string? phamVi, int? trang)
    {
        var loc = await DocBoLocAsync(maLop, phamVi);
        var baoCao = await _baoCao.BaoCaoHocLucAsync(loc.BoLocSinhVien, loc.PhamViBaoCao);
        var thongKe = await _baoCao.ThongKeXepLoaiAsync(loc.BoLocSinhVien, loc.PhamViBaoCao);

        return View(new BaoCaoAdminViewModel
        {
            MaLop = loc.MaLop,
            PhamVi = loc.PhamVi,
            DsLop = loc.DsLop,
            DsHocKy = loc.DsHocKy,
            BaoCao = new BaoCaoHocLucViewModel
            {
                DanhSach = baoCao.DuLieu,
                ThongKe = BaoCaoHocLucViewModel.DuBayMuc(thongKe.DuLieu),
                TenPhamVi = loc.TenPhamVi,
                LaTichLuy = loc.PhamViBaoCao.LaTichLuy,
                UrlHoSoSV = "/Admin/SinhVien/HoSo/",
                UrlXuatExcel = Url.Action(nameof(XuatExcel), new { maLop = loc.MaLop, phamVi = loc.PhamVi }),
                // Chỉ tách bảng theo lớp khi hiển thị; thẻ số, biểu đồ, Xuất Excel vẫn dùng toàn bộ danh sách
                PhanTrang = new PhanTrangTheoLop(baoCao.DuLieu, trang,
                    new() { ["maLop"] = loc.MaLop, ["phamVi"] = loc.PhamVi }),
                Pipelines =
                [
                    PipelineDaChay.Tu("BaoCaoHocLuc", "Bảng học lực: mỗi sinh viên một dòng (GPA, xếp loại, TC, số môn nợ).", baoCao),
                    PipelineDaChay.Tu("ThongKeXepLoai", "Thẻ số và biểu đồ: số sinh viên theo từng xếp loại (BaoCaoHocLuc + $group).", thongKe)
                ]
            }
        });
    }

    /// <summary>Xuất Excel báo cáo học lực theo đúng bộ lọc đang chọn (A-10; A-03 dùng maLop + phamVi=tichluy).</summary>
    [HttpGet]
    public async Task<IActionResult> XuatExcel(string? maLop, string? phamVi)
    {
        var loc = await DocBoLocAsync(maLop, phamVi);
        var lop = loc.DsLop.FirstOrDefault(l => l.MaLop == loc.MaLop);
        var pv = loc.PhamViBaoCao;
        var tep = await _xuat.BaoCaoHocLucAsync(loc.BoLocSinhVien, pv,
            [("Lớp", lop == null ? "Tất cả lớp" : $"{lop.MaLop} – {lop.TenLop}"), ("Phạm vi", loc.TenPhamVi)],
            loc.MaLop ?? "TatCa", pv.LaTichLuy ? "TichLuy" : pv.NamHoc,
            pv.HocKy is int hk ? XuatExcelService.HocKyTrongTenFile(hk) : null);
        return File(tep.NoiDung, ExcelService.LoaiNoiDung, tep.TenFile);
    }
}
