using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DT2_LMS_NHOM6.Areas.SinhVien.Controllers;

/// <summary>
/// S-01 Tổng quan (S1): card thông tin sinh viên, thẻ học lực tích lũy (BaoCaoHocLuc), biểu đồ GPA theo học kỳ (GpaHocKy),
/// các môn của học kỳ hiện tại (BangDiem). Mọi pipeline lọc theo MSSV lấy từ claim.
/// </summary>
public class HomeController : SinhVienControllerBase
{
    private readonly DiemService _diem;
    private readonly BaoCaoService _baoCao;
    private readonly SinhVienService _sinhVien;
    private readonly LopHocService _lopHoc;
    private readonly HocKyHienTaiOptions _hocKyHienTai;

    public HomeController(DiemService diem, BaoCaoService baoCao, SinhVienService sinhVien, LopHocService lopHoc,
        IOptions<HocKyHienTaiOptions> hocKyHienTai)
    {
        _diem = diem;
        _baoCao = baoCao;
        _sinhVien = sinhVien;
        _lopHoc = lopHoc;
        _hocKyHienTai = hocKyHienTai.Value;
    }

    public async Task<IActionResult> Index()
    {
        // Card thông tin sinh viên: hồ sơ + lớp sinh hoạt (giống HoSoController)
        var sv = await _sinhVien.LayAsync(MaSV);
        var hoSo = sv == null ? null : new HoSoCaNhanViewModel { SinhVien = sv, Lop = await _lopHoc.LayAsync(sv.MaLop) };


        var tongKet = (await _baoCao.BaoCaoHocLucAsync(BoLocSinhVien.TheoSinhVien(MaSV), PhamViBaoCao.TichLuy()))
            .DuLieu.FirstOrDefault();
        var gpaHocKy = await _diem.GpaHocKyAsync(MaSV);
        var bangDiem = await _diem.BangDiemAsync(MaSV);

        return View(new TongQuanSinhVienViewModel
        {
            HoSo = hoSo,
            TongKet = tongKet,
            GpaHocKy = gpaHocKy.DuLieu,
            // Chỉ chọn các lần học thuộc học kỳ hiện tại để hiển thị – điểm TK/chữ vẫn là kết quả pipeline
            MonHocKyHienTai = bangDiem.DuLieu
                .Where(m => m.NamHoc == _hocKyHienTai.NamHoc && m.HocKy == _hocKyHienTai.HocKy)
                .ToList(),
            TenHocKyHienTai = HienThi.HocKy(_hocKyHienTai.NamHoc, _hocKyHienTai.HocKy)
        });
    }
}
