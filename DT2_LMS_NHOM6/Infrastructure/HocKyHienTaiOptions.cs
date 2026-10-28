namespace DT2_LMS_NHOM6.Infrastructure;

/// <summary>Học kỳ hiện tại – đọc từ mục "HocKyHienTai" trong appsettings.json.</summary>
public class HocKyHienTaiOptions
{
    public const string TenMuc = "HocKyHienTai";

    public string NamHoc { get; set; } = "2026-2027";

    /// <summary>1, 2 (học kỳ chính) hoặc 3 (học kỳ hè).</summary>
    public int HocKy { get; set; } = 1;
}
