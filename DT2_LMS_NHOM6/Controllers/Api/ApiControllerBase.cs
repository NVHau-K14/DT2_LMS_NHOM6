using System.Text.Json;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Controllers.Api;

/// <summary>
/// Controller cơ sở của API REST (/api/...). Chỉ là lớp vỏ JSON: mọi nghiệp vụ gọi lại đúng service
/// mà màn hình MVC đang dùng (không lặp logic, không gọi MongoDB trực tiếp).
/// - Xác thực: dùng lại cookie đăng nhập của web (đăng nhập ở /Account/Login rồi gọi API).
///   Chưa đăng nhập → 401, sai vai trò → 403 (trả JSON, không chuyển hướng – cấu hình ở Program.cs).
/// - Bỏ kiểm tra antiforgery token: API nhận body JSON (Content-Type application/json) nên trình duyệt
///   của trang khác không gửi được nếu không qua CORS preflight, và cookie SameSite=Lax không đi kèm
///   request POST/PUT/DELETE từ trang khác → đủ chống CSRF cho phạm vi đồ án.
/// </summary>
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Vai trò người gọi (Admin / GiangVien / SinhVien) – lấy từ claim.</summary>
    protected string VaiTro => User.VaiTro();

    /// <summary>maSV / maGV / "admin" của người gọi – lấy từ claim, KHÔNG từ request.</summary>
    protected string MaThamChieu => User.MaThamChieu();

    /// <summary>Thông báo lỗi dạng JSON với mã trạng thái tùy chọn.</summary>
    protected ObjectResult Loi(int maTrangThai, string thongBao, string? mongoCommand = null)
        => StatusCode(maTrangThai, new KetQuaApi { ThanhCong = false, ThongBao = thongBao, MongoCommand = mongoCommand });

    /// <summary>Đổi KetQuaGhi của service thành JSON: thành công → maThanhCong (200/201), lỗi → 400 kèm trường lỗi.</summary>
    protected ObjectResult KetQua(KetQuaGhi kq, object? duLieu = null, int maThanhCong = StatusCodes.Status200OK)
    {
        var body = new KetQuaApi
        {
            ThanhCong = kq.ThanhCong,
            ThongBao = kq.ThongBao,
            MongoCommand = kq.MongoCommand,
            DuLieu = kq.ThanhCong ? duLieu : null,
            Loi = !kq.ThanhCong && kq.TruongLoi != null
                ? new Dictionary<string, string[]> { [JsonNamingPolicy.CamelCase.ConvertName(kq.TruongLoi)] = [kq.ThongBao] }
                : null
        };
        return StatusCode(kq.ThanhCong ? maThanhCong : StatusCodes.Status400BadRequest, body);
    }
}
