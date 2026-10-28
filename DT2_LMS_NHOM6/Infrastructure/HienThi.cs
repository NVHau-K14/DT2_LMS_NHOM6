using System.Globalization;
using DT2_LMS_NHOM6.Models;

namespace DT2_LMS_NHOM6.Infrastructure;

/// <summary>
/// Hàm định dạng dùng chung cho View (thiết kế mục 2.4, 5, 8).
/// Chỉ ĐỊNH DẠNG giá trị đã có (điểm do pipeline tính), không tính toán điểm.
/// </summary>
public static class HienThi
{
    private const string KhongCo = "–";

    /// <summary>Điểm hệ 10 với 1 chữ số thập phân, ví dụ "8.5"; null → "–".</summary>
    public static string Diem(double? diem)
        => diem.HasValue ? diem.Value.ToString("0.0", CultureInfo.InvariantCulture) : KhongCo;

    /// <summary>GPA với 2 chữ số thập phân, ví dụ "3.25"; null → "–".</summary>
    public static string Gpa(double? gpa)
        => gpa.HasValue ? gpa.Value.ToString("0.00", CultureInfo.InvariantCulture) : KhongCo;

    /// <summary>"HK1 2025-2026", "HK2 2025-2026", "HK hè 2025-2026".</summary>
    public static string HocKy(string namHoc, int hocKy)
        => hocKy == 3 ? $"HK hè {namHoc}" : $"HK{hocKy} {namHoc}";

    /// <summary>
    /// Ngày dạng dd/MM/yyyy. Ngày (ví dụ ngày sinh) lưu 00:00 UTC nên định dạng thẳng,
    /// không đổi múi giờ để tránh lệch sang ngày khác.
    /// </summary>
    public static string Ngay(DateTime? ngay)
        => ngay.HasValue ? ngay.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : KhongCo;

    /// <summary>Tên vai trò tiếng Việt để hiển thị.</summary>
    public static string TenVaiTro(string vaiTro) => vaiTro switch
    {
        HangSo.VaiTro.Admin => "Quản trị viên",
        HangSo.VaiTro.GiangVien => "Giảng viên",
        HangSo.VaiTro.SinhVien => "Sinh viên",
        _ => vaiTro
    };

    /// <summary>Lớp CSS badge cho điểm chữ: A xanh lá, B xanh dương, C vàng, D cam, F đỏ.</summary>
    public static string LopBadgeDiemChu(string? diemChu) => diemChu switch
    {
        "A" => "badge-diem-a",
        "B" => "badge-diem-b",
        "C" => "badge-diem-c",
        "D" => "badge-diem-d",
        "F" => "badge-diem-f",
        _ => "badge-trong"
    };

    /// <summary>Lớp CSS badge cho xếp loại học lực (thiết kế mục 3.4).</summary>
    public static string LopBadgeXepLoai(string? xepLoai) => xepLoai switch
    {
        "Xuất sắc" => "badge-xl-xuatsac",
        "Giỏi" => "badge-xl-gioi",
        "Khá" => "badge-xl-kha",
        "Trung bình" => "badge-xl-trungbinh",
        "Yếu" => "badge-xl-yeu",
        "Kém" => "badge-xl-kem",
        _ => "badge-trong"
    };
}
