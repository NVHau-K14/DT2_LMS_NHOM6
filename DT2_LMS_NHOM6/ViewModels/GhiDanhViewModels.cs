using DT2_LMS_NHOM6.Models.Documents;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>
/// Yêu cầu ghi danh (B1 + B2 của A-09). Bind từ query string (xem trước, GET) và từ form (xác nhận, POST).
/// </summary>
public class YeuCauGhiDanh
{
    public const string CheDoCaLop = "lop";
    public const string CheDoTungSV = "sv";

    public string NamHoc { get; set; } = "";
    /// <summary>1, 2 (học kỳ chính) hoặc 3 (học kỳ hè).</summary>
    public int HocKy { get; set; }
    public string MaMon { get; set; } = "";
    public string MaGV { get; set; } = "";

    /// <summary>"lop" = cả lớp sinh hoạt; "sv" = từng sinh viên.</summary>
    public string CheDo { get; set; } = CheDoCaLop;
    public string? MaLop { get; set; }
    public List<string> DsMaSV { get; set; } = new();
}

/// <summary>Một dòng của bảng xem trước (B3).</summary>
public class DongXemTruoc
{
    public string MaSV { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string MaLop { get; set; } = "";
    public int LanHoc { get; set; }
    public string HinhThuc { get; set; } = "";
    public bool HopLe { get; set; }
    /// <summary>Lý do bị chặn (khi HopLe = false).</summary>
    public string? LyDo { get; set; }

    public static DongXemTruoc Chan(string maSV, string hoTen, string maLop, string lyDo)
        => new() { MaSV = maSV, HoTen = hoTen, MaLop = maLop, HopLe = false, LyDo = lyDo };
}

/// <summary>Kết quả xem trước: lỗi chung (thiếu tham số) hoặc danh sách dòng.</summary>
public class KetQuaXemTruoc
{
    public string? Loi { get; set; }
    public MonHoc? MonHoc { get; set; }
    public GiangVien? GiangVien { get; set; }
    public List<DongXemTruoc> Dong { get; set; } = new();
    public int SoHopLe => Dong.Count(d => d.HopLe);
}

/// <summary>Màn hình A-09.</summary>
public class GhiDanhViewModel
{
    /// <summary>Năm học cho ô chọn (GD5: 2025-2026, 2026-2027).</summary>
    public static readonly string[] DsNamHoc = ["2025-2026", "2026-2027"];

    public YeuCauGhiDanh YeuCau { get; set; } = new();
    public List<MonHoc> DsMon { get; set; } = new();
    public List<GiangVien> DsGV { get; set; } = new();
    public List<LopHoc> DsLop { get; set; } = new();
    /// <summary>Mọi SV (không kèm monHoc) cho ô chọn từng SV.</summary>
    public List<SinhVien> DsSinhVien { get; set; } = new();

    /// <summary>null = chưa bấm "Xem trước".</summary>
    public KetQuaXemTruoc? XemTruoc { get; set; }
}
