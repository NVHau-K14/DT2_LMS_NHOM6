using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>Giảng viên (A-07): danh sách, thêm/sửa bằng modal (lan truyền tên), xóa, reset mật khẩu, khóa/mở.</summary>
public class GiangVienController : AdminControllerBase
{
    private const string TienToForm = nameof(GiangVienIndexViewModel.Form) + ".";

    private readonly GiangVienService _giangVien;
    private readonly HocKyHienTaiOptions _hocKyHienTai;

    public GiangVienController(GiangVienService giangVien, IOptions<HocKyHienTaiOptions> hocKyHienTai)
    {
        _giangVien = giangVien;
        _hocKyHienTai = hocKyHienTai.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await TaoIndexAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Luu([Bind(Prefix = nameof(GiangVienIndexViewModel.Form))] GiangVienFormViewModel form)
    {
        if (ModelState.IsValid)
        {
            var ketQua = form.LaSua
                ? await _giangVien.SuaAsync(form.MaGV, form)
                : await _giangVien.ThemAsync(form);
            if (ketQua.ThanhCong)
            {
                ThongBaoKetQua(ketQua);
                return RedirectToAction(nameof(Index));
            }
            GanLoi(ketQua, TienToForm);
        }

        var vm = await TaoIndexAsync();
        vm.Form = form;
        vm.MoModal = true;
        return View(nameof(Index), vm);
    }

    [HttpPost]
    public async Task<IActionResult> Xoa(string id)
    {
        ThongBaoKetQua(await _giangVien.XoaAsync(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ResetMatKhau(string id)
    {
        ThongBaoKetQua(await _giangVien.ResetMatKhauAsync(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> KhoaTaiKhoan(string id)
    {
        ThongBaoKetQua(await _giangVien.DoiTrangThaiTaiKhoanAsync(id));
        return RedirectToAction(nameof(Index));
    }

    private async Task<GiangVienIndexViewModel> TaoIndexAsync() => new()
    {
        DanhSach = await _giangVien.DanhSachAsync(),
        NhanHocKyHienTai = HienThi.HocKy(_hocKyHienTai.NamHoc, _hocKyHienTai.HocKy)
    };
}
