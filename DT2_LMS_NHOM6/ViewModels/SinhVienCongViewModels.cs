using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>S-01: tổng quan của sinh viên đang đăng nhập.</summary>
public class TongQuanSinhVienViewModel
{
    /// <summary>Học lực tích lũy (pipeline BaoCaoHocLuc của chính SV); null nếu không có.</summary>
    public BaoCaoHocLucRow? TongKet { get; set; }

    /// <summary>GPA từng học kỳ (pipeline GpaHocKy), cũ → mới.</summary>
    public List<GpaHocKyResult> GpaHocKy { get; set; } = new();

    /// <summary>Các lần học thuộc học kỳ hiện tại (lọc từ kết quả pipeline BangDiem).</summary>
    public List<KetQuaMon> MonHocKyHienTai { get; set; } = new();
    public string TenHocKyHienTai { get; set; } = "";

    /// <summary>Biểu đồ đường: chỉ các học kỳ đã có GPA (học kỳ chưa đủ điểm không có điểm để vẽ).</summary>
    public CauHinhBieuDo BieuDoGpa
    {
        get
        {
            var coGpa = GpaHocKy.Where(g => g.Gpa.HasValue).ToList();
            return new CauHinhBieuDo
            {
                Loai = "line",
                Nhan = coGpa.Select(g => HienThi.HocKy(g.NamHoc, g.HocKy)).ToList(),
                DuLieu = coGpa.Select(g => g.Gpa).ToList(),
                Mau = ["#2563EB"],
                TenChuoi = "GPA học kỳ",
                TrucYToiDa = 4,
                SoLe = 2
            };
        }
    }
}

/// <summary>S-03: hồ sơ cá nhân (chỉ đọc).</summary>
public class HoSoCaNhanViewModel
{
    public SinhVien SinhVien { get; set; } = new();
    public LopHoc? Lop { get; set; }
}
