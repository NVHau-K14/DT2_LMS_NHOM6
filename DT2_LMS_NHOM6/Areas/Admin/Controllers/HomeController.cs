using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>
/// A-01 Tổng quan toàn trường (A1): 4 thẻ số (countDocuments), phân bố xếp loại tích lũy
/// (pipeline ThongKeXepLoai), GPA tích lũy trung bình theo lớp (pipeline GpaTheoLop).
/// </summary>
public class HomeController : AdminControllerBase
{
    private readonly BaoCaoService _baoCao;

    public HomeController(BaoCaoService baoCao)
    {
        _baoCao = baoCao;
    }

    public async Task<IActionResult> Index()
    {
        var (soLop, soSV, soGV, soMon) = await _baoCao.DemDanhMucAsync();
        var thongKe = await _baoCao.ThongKeXepLoaiAsync(BoLocSinhVien.TatCa(), PhamViBaoCao.TichLuy());
        var gpaTheoLop = await _baoCao.GpaTheoLopAsync();

        return View(new TongQuanAdminViewModel
        {
            SoLop = soLop,
            SoSV = soSV,
            SoGV = soGV,
            SoMon = soMon,
            PhanBoXepLoai = BaoCaoHocLucViewModel.DuBayMuc(thongKe.DuLieu),
            GpaTheoLop = gpaTheoLop.DuLieu
        });
    }
}
