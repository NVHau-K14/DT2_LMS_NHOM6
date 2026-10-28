using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>Danh mục môn học (A-08): danh sách, thêm/sửa bằng modal (đổi tên → lan truyền), xóa.</summary>
public class MonHocController : AdminControllerBase
{
    private const string TienToForm = nameof(MonHocIndexViewModel.Form) + ".";

    private readonly MonHocService _monHoc;

    public MonHocController(MonHocService monHoc)
    {
        _monHoc = monHoc;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new MonHocIndexViewModel { DanhSach = await _monHoc.DanhSachAsync() });
    }

    [HttpPost]
    public async Task<IActionResult> Luu([Bind(Prefix = nameof(MonHocIndexViewModel.Form))] MonHocFormViewModel form)
    {
        if (ModelState.IsValid)
        {
            var ketQua = form.LaSua
                ? await _monHoc.SuaAsync(form.MaMon, form)
                : await _monHoc.ThemAsync(form);
            if (ketQua.ThanhCong)
            {
                ThongBaoKetQua(ketQua);
                return RedirectToAction(nameof(Index));
            }
            GanLoi(ketQua, TienToForm);
        }

        var danhSach = await _monHoc.DanhSachAsync();
        return View(nameof(Index), new MonHocIndexViewModel
        {
            DanhSach = danhSach,
            Form = form,
            MoModal = true,
            SoLanHocDangSua = form.LaSua
                ? danhSach.FirstOrDefault(d => d.MonHoc.MaMon == form.MaMon)?.SoLanHoc ?? 0
                : 0
        });
    }

    [HttpPost]
    public async Task<IActionResult> Xoa(string id)
    {
        ThongBaoKetQua(await _monHoc.XoaAsync(id));
        return RedirectToAction(nameof(Index));
    }
}
