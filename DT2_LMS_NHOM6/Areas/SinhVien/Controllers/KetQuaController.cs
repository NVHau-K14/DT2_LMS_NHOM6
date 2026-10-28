using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.SinhVien.Controllers;

/// <summary>
/// S-02 Kết quả học tập (S2): bảng điểm theo học kỳ – dùng chung partial _BangDiem với A-06.
/// Dữ liệu: pipeline BangDiem (mọi lần học + tinhTichLuy), GpaHocKy (GPA từng kỳ), BaoCaoHocLuc (tổng kết tích lũy).
/// </summary>
public class KetQuaController : SinhVienControllerBase
{
    private readonly DiemService _diem;
    private readonly BaoCaoService _baoCao;

    public KetQuaController(DiemService diem, BaoCaoService baoCao)
    {
        _diem = diem;
        _baoCao = baoCao;
    }

    public async Task<IActionResult> Index()
    {
        var bangDiem = await _diem.BangDiemAsync(MaSV);
        var gpaHocKy = await _diem.GpaHocKyAsync(MaSV);
        var tongKet = (await _baoCao.BaoCaoHocLucAsync(BoLocSinhVien.TheoSinhVien(MaSV), PhamViBaoCao.TichLuy()))
            .DuLieu.FirstOrDefault();

        // HienTongKetCuoi = true (mặc định): dòng GPA tích lũy / TC tích lũy / xếp loại ở cuối trang;
        // QuayLaiHuyGhiDanh = null: SV không có cột hủy ghi danh
        return View(BangDiemViewModel.Tao(bangDiem.DuLieu, gpaHocKy.DuLieu, tongKet));
    }
}
