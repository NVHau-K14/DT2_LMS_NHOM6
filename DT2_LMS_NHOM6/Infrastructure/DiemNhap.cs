using System.Globalization;
using System.Text.RegularExpressions;

namespace DT2_LMS_NHOM6.Infrastructure;

/// <summary>
/// Chuẩn hóa và kiểm tra một ô điểm thành phần người dùng nhập (G-03, thiết kế 5.3):
///   - chấp nhận "8,5" và "8.5" (dấu phẩy đổi thành dấu chấm);
///   - thang 0–10, tối đa 1 chữ số thập phân;
///   - ô trống = null (xóa điểm nhập nhầm).
/// Đây là kiểm tra ĐẦU VÀO, không phải tính điểm – điểm TK / chữ / hệ 4 vẫn do pipeline tính.
/// </summary>
public static class DiemNhap
{
    // 0–2 chữ số phần nguyên, tùy chọn 1 chữ số thập phân: "9", "10", "8.5", ".5" không hợp lệ
    private static readonly Regex DangSo = new(@"^\d{1,2}(\.\d)?$", RegexOptions.Compiled);

    /// <returns>true nếu hợp lệ; <paramref name="diem"/> = null khi ô trống.</returns>
    public static bool TryParse(string? oNhap, out double? diem, out string? loi)
    {
        diem = null;
        loi = null;
        var s = oNhap?.Trim().Replace(',', '.') ?? "";
        if (s.Length == 0) return true;

        if (!DangSo.IsMatch(s))
        {
            loi = $"\"{oNhap}\" không phải điểm hợp lệ (số 0–10, tối đa 1 chữ số thập phân).";
            return false;
        }
        var giaTri = double.Parse(s, CultureInfo.InvariantCulture);
        if (giaTri > 10)
        {
            loi = $"Điểm {s} vượt quá 10.";
            return false;
        }
        diem = giaTri;
        return true;
    }
}
