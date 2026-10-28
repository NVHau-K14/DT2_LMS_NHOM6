using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Infrastructure;

/// <summary>
/// Gửi toast qua TempData để hiển thị sau khi redirect (mẫu Post-Redirect-Get).
/// Layout đọc các khóa này trong partial _Toast và gọi hàm JS toast().
/// </summary>
public static class ThongBaoExtensions
{
    public const string KhoaToast = "Toast";
    public const string KhoaLoaiToast = "ToastLoai";
    public const string KhoaMongoCommand = "MongoCommand";

    /// <param name="loai">success | danger | warning | info</param>
    /// <param name="mongoCommand">Chuỗi lệnh dạng mongosh đã chạy (thiết kế mục 4.9), có thể null.</param>
    public static void ThongBao(this Controller controller, string noiDung, string loai = "success", string? mongoCommand = null)
    {
        controller.TempData[KhoaToast] = noiDung;
        controller.TempData[KhoaLoaiToast] = loai;
        if (!string.IsNullOrEmpty(mongoCommand))
            controller.TempData[KhoaMongoCommand] = mongoCommand;
    }
}
