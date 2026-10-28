using DT2_LMS_NHOM6.Models.Results;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>
/// Dữ liệu cho partial _BangDiem (A-06, S-02): bảng điểm chia theo học kỳ.
/// Mọi con số lấy nguyên từ pipeline BangDiem, GpaHocKy, BaoCaoHocLuc – ở đây chỉ GOM NHÓM để hiển thị.
/// </summary>
public class BangDiemViewModel
{
    /// <summary>Các khối học kỳ, mới nhất trước (khối đầu mở sẵn).</summary>
    public List<KhoiHocKy> CacHocKy { get; set; } = new();

    /// <summary>Tổng kết tích lũy (BaoCaoHocLuc của SV); null nếu không có.</summary>
    public BaoCaoHocLucRow? TongKet { get; set; }

    /// <summary>Hiện dòng tổng kết tích lũy ở cuối bảng (S-02). A-06 tắt vì đã có thẻ tóm tắt đầu trang.</summary>
    public bool HienTongKetCuoi { get; set; } = true;

    public static BangDiemViewModel Tao(IEnumerable<KetQuaMon> bangDiem, IEnumerable<GpaHocKyResult> gpaHocKy, BaoCaoHocLucRow? tongKet)
    {
        var gpaTheoKy = gpaHocKy.ToDictionary(g => (g.NamHoc, g.HocKy));
        var cacKhoi = bangDiem
            .GroupBy(m => (m.NamHoc, m.HocKy))
            .OrderByDescending(g => g.Key.NamHoc).ThenByDescending(g => g.Key.HocKy)
            .Select(g => new KhoiHocKy
            {
                NamHoc = g.Key.NamHoc,
                HocKy = g.Key.HocKy,
                CacMon = g.ToList(), // pipeline đã sắp theo maMon
                Gpa = gpaTheoKy.GetValueOrDefault(g.Key)
            })
            .ToList();
        return new BangDiemViewModel { CacHocKy = cacKhoi, TongKet = tongKet };
    }
}

public class KhoiHocKy
{
    public string NamHoc { get; set; } = "";
    public int HocKy { get; set; }
    public List<KetQuaMon> CacMon { get; set; } = new();
    public GpaHocKyResult? Gpa { get; set; }

    /// <summary>Id HTML cho khối thu gọn, ví dụ "hk-2025-2026-1".</summary>
    public string IdHtml => $"hk-{NamHoc}-{HocKy}";
}
