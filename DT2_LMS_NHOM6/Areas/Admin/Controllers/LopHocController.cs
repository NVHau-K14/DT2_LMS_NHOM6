using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>Lớp sinh hoạt: danh sách (A-02), chi tiết (A-03), thêm/sửa bằng modal, xóa.</summary>
public class LopHocController : AdminControllerBase
{
    private const string TienToForm = nameof(LopHocIndexViewModel.Form) + ".";

    private readonly LopHocService _lopHoc;
    private readonly GiangVienService _giangVien;
    private readonly BaoCaoService _baoCao;

    public LopHocController(LopHocService lopHoc, GiangVienService giangVien, BaoCaoService baoCao)
    {
        _lopHoc = lopHoc;
        _giangVien = giangVien;
        _baoCao = baoCao;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? nganh, string? nienKhoa, string? q)
    {
        return View(await TaoIndexAsync(nganh, nienKhoa, q));
    }

    /// <summary>Lưu từ modal: Form.LaSua = false → thêm, true → sửa. Lỗi → hiện lại trang với modal mở sẵn.</summary>
    [HttpPost]
    public async Task<IActionResult> Luu([Bind(Prefix = nameof(LopHocIndexViewModel.Form))] LopHocFormViewModel form)
    {
        if (ModelState.IsValid)
        {
            var ketQua = form.LaSua
                ? await _lopHoc.SuaAsync(form.MaLop, form)
                : await _lopHoc.ThemAsync(form);
            if (ketQua.ThanhCong)
            {
                ThongBaoKetQua(ketQua);
                return RedirectToAction(nameof(Index));
            }
            GanLoi(ketQua, TienToForm);
        }

        var vm = await TaoIndexAsync(null, null, null);
        vm.Form = form;
        vm.MoModal = true;
        return View(nameof(Index), vm);
    }

    [HttpPost]
    public async Task<IActionResult> Xoa(string id)
    {
        ThongBaoKetQua(await _lopHoc.XoaAsync(id));
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// A-03: thông tin lớp + GPA tích lũy, xếp loại từng SV (pipeline BaoCaoHocLuc lọc theo lớp)
    /// + thẻ thống kê xếp loại của lớp (pipeline ThongKeXepLoai cùng bộ lọc).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ChiTiet(string id)
    {
        var lop = await _lopHoc.LayAsync(id);
        if (lop == null) return NotFound();

        var boLoc = BoLocSinhVien.TheoLop(id);
        var baoCao = await _baoCao.BaoCaoHocLucAsync(boLoc, PhamViBaoCao.TichLuy());
        var thongKe = await _baoCao.ThongKeXepLoaiAsync(boLoc, PhamViBaoCao.TichLuy());
        return View(new LopHocChiTietViewModel
        {
            Lop = lop,
            DanhSachSV = baoCao.DuLieu,
            ThongKeXepLoai = BaoCaoHocLucViewModel.DuBayMuc(thongKe.DuLieu)
        });
    }

    private async Task<LopHocIndexViewModel> TaoIndexAsync(string? nganh, string? nienKhoa, string? q)
    {
        var (dsNganh, dsNienKhoa) = await _lopHoc.GiaTriLocAsync();
        return new LopHocIndexViewModel
        {
            DanhSach = await _lopHoc.DanhSachAsync(nganh, nienKhoa, q),
            Nganh = nganh,
            NienKhoa = nienKhoa,
            Q = q,
            DsNganh = dsNganh,
            DsNienKhoa = dsNienKhoa,
            DsGiangVien = await _giangVien.TatCaAsync()
        };
    }
}
