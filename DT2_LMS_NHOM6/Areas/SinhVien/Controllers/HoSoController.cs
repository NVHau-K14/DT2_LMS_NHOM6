using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Areas.SinhVien.Controllers;

/// <summary>S-03 Hồ sơ cá nhân (S3): thông tin chỉ đọc + liên kết đổi mật khẩu.</summary>
public class HoSoController : SinhVienControllerBase
{
    private readonly SinhVienService _sinhVien;
    private readonly LopHocService _lopHoc;

    public HoSoController(SinhVienService sinhVien, LopHocService lopHoc)
    {
        _sinhVien = sinhVien;
        _lopHoc = lopHoc;
    }

    public async Task<IActionResult> Index()
    {
        var sv = await _sinhVien.LayAsync(MaSV);
        if (sv == null) return NotFound(); // tài khoản còn nhưng hồ sơ đã bị xóa

        return View(new HoSoCaNhanViewModel { SinhVien = sv, Lop = await _lopHoc.LayAsync(sv.MaLop) });
    }
}
