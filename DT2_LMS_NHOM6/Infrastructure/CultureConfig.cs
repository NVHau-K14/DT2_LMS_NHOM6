using System.Globalization;

namespace DT2_LMS_NHOM6.Infrastructure;

/// <summary>
/// Culture dùng cho toàn ứng dụng: vi-VN (ngày dd/MM/yyyy, tên tháng tiếng Việt)
/// nhưng số dùng dấu CHẤM thập phân (thiết kế mục 8: "8.5", GPA "3.25").
/// </summary>
public static class CultureConfig
{
    public static CultureInfo TaoCultureViVn()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("vi-VN").Clone();

        culture.NumberFormat.NumberDecimalSeparator = ".";
        // Nhóm hàng nghìn dùng khoảng trắng (không dùng dấu phẩy) để chuỗi "8,5"
        // không bị model binder hiểu nhầm thành 85.
        culture.NumberFormat.NumberGroupSeparator = " ";
        culture.NumberFormat.CurrencyDecimalSeparator = ".";
        culture.NumberFormat.CurrencyGroupSeparator = " ";
        culture.NumberFormat.PercentDecimalSeparator = ".";
        culture.NumberFormat.PercentGroupSeparator = " ";

        culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
        return culture;
    }
}
