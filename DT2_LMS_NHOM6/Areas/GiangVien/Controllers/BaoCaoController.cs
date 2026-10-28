using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.GiangVien.Controllers;

/// <summary>
/// G-04 Báo cáo học lực (tích lũy, G5) của sinh viên thuộc các lớp học phần của GV (mọi học kỳ).
/// Dùng lại partial _BaoCaoHocLuc của A-10. Bộ lọc SV luôn chứa maGV lấy từ claim:
///   mọi lớp học phần:  { "monHoc.giangVien.maGV": maGV }
///   một lớp học phần: { monHoc: { $elemMatch: { "giangVien.maGV": maGV, maMon, namHoc, hocKy } } }
/// nên GV không thể xem SV ngoài lớp mình dạy dù sửa tham số trên URL.
/// </summary>
public class BaoCaoController : GiangVienControllerBase
{
    private readonly BaoCaoService _baoCao;
    private readonly DiemService _diem;
    private readonly XuatExcelService _xuat;

    public BaoCaoController(BaoCaoService baoCao, DiemService diem, XuatExcelService xuat)
    {
        _baoCao = baoCao;
        _diem = diem;
        _xuat = xuat;
    }

    /// <summary>
    /// Bộ lọc SV luôn chứa maGV từ claim; lhp không thuộc GV → bỏ qua (mọi lớp học phần).
    /// Dùng chung cho Index và XuatExcel.
    /// </summary>
    private async Task<(List<LopHocPhanRow> DsLhp, LopHocPhanRow? LhpChon, BoLocSinhVien BoLoc)> DocBoLocAsync(string? lhp)
    {
        var dsLhp = (await _diem.LopHocPhanCuaGVAsync(MaGV)).DuLieu
            .OrderByDescending(l => l.NamHoc).ThenByDescending(l => l.HocKy).ThenBy(l => l.MaMon)
            .ToList();
        var lhpChon = dsLhp.FirstOrDefault(l => BaoCaoGiangVienViewModel.MaLhp(l) == lhp);
        var boLoc = lhpChon == null
            ? BoLocSinhVien.TheoGiangVien(MaGV)
            : BoLocSinhVien.TheoLopHocPhan(MaGV, lhpChon.MaMon, lhpChon.NamHoc, lhpChon.HocKy);
        return (dsLhp, lhpChon, boLoc);
    }

    /// <param name="lhp">Lớp học phần dạng "INT301_2026-2027_1"; trống = mọi lớp học phần.</param>
    [HttpGet]
    public async Task<IActionResult> Index(string? lhp)
    {
        var (dsLhp, lhpChon, boLoc) = await DocBoLocAsync(lhp);
        var phamVi = PhamViBaoCao.TichLuy();

        var baoCao = await _baoCao.BaoCaoHocLucAsync(boLoc, phamVi);
        var thongKe = await _baoCao.ThongKeXepLoaiAsync(boLoc, phamVi);

        return View(new BaoCaoGiangVienViewModel
        {
            Lhp = lhpChon == null ? null : lhp,
            DsLopHocPhan = dsLhp,
            BaoCao = new BaoCaoHocLucViewModel
            {
                DanhSach = baoCao.DuLieu,
                ThongKe = BaoCaoHocLucViewModel.DuBayMuc(thongKe.DuLieu),
                TenPhamVi = "Tích lũy",
                LaTichLuy = true,
                UrlHoSoSV = null, // GV không có quyền xem hồ sơ SV
                UrlXuatExcel = Url.Action(nameof(XuatExcel), new { lhp = lhpChon == null ? null : lhp }),
                Pipelines =
                [
                    PipelineDaChay.Tu("BaoCaoHocLuc", "Bảng học lực: mỗi sinh viên một dòng (GPA, xếp loại, TC, số môn nợ).", baoCao),
                    PipelineDaChay.Tu("ThongKeXepLoai", "Thẻ số và biểu đồ: số sinh viên theo từng xếp loại (BaoCaoHocLuc + $group).", thongKe)
                ]
            }
        });
    }

    /// <summary>Xuất Excel báo cáo học lực (tích lũy) theo lớp học phần đang chọn.</summary>
    [HttpGet]
    public async Task<IActionResult> XuatExcel(string? lhp)
    {
        var (_, lhpChon, boLoc) = await DocBoLocAsync(lhp);
        var tenLhp = lhpChon == null
            ? "Tất cả lớp học phần của giảng viên"
            : $"{lhpChon.MaMon} – {lhpChon.TenMon} · {HienThi.HocKy(lhpChon.NamHoc, lhpChon.HocKy)}";
        var tep = await _xuat.BaoCaoHocLucAsync(boLoc, PhamViBaoCao.TichLuy(),
            [("Giảng viên", $"{MaGV} – {User.HoTen()}"), ("Lớp học phần", tenLhp), ("Phạm vi", "Tích lũy")],
            MaGV, lhpChon?.MaMon, lhpChon?.NamHoc,
            lhpChon == null ? null : XuatExcelService.HocKyTrongTenFile(lhpChon.HocKy), "TichLuy");
        return File(tep.NoiDung, ExcelService.LoaiNoiDung, tep.TenFile);
    }
}
