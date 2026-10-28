using DT2_LMS_NHOM6.Models.Results;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>
/// Một dòng điểm gửi lên khi lưu (fetch JSON từ G-03). Điểm để dạng CHUỖI đúng như người dùng gõ
/// ("8,5", "8.5", "" = xóa điểm) – server tự chuẩn hóa và kiểm tra lại (DiemNhap).
/// </summary>
public class DongLuuDiem
{
    public string MaSV { get; set; } = "";
    /// <summary>ObjectId dạng chuỗi 24 ký tự hex.</summary>
    public string MaDK { get; set; } = "";
    public string? Cc { get; set; }
    public string? Gk { get; set; }
    public string? Ck { get; set; }
}

/// <summary>"Lưu tất cả": các dòng đã sửa của một lớp học phần.</summary>
public class YeuCauLuuNhieuDiem
{
    public string MaMon { get; set; } = "";
    public string NamHoc { get; set; } = "";
    public int HocKy { get; set; }
    public List<DongLuuDiem> Dong { get; set; } = new();
}

/// <summary>Kết quả lưu điểm trả về trình duyệt (JSON).</summary>
public class KetQuaLuuDiemJson
{
    public bool ThanhCong { get; set; }
    public string ThongBao { get; set; } = "";
    public string? MongoCommand { get; set; }
    /// <summary>Các dòng sau khi lưu, lấy lại từ pipeline (TK, chữ, hệ 4 mới).</summary>
    public List<DongDiemJson> Dong { get; set; } = new();
}

/// <summary>Một dòng điểm đã quy đổi (từ pipeline) gửi về để JS cập nhật bảng – JS không tính điểm.</summary>
public class DongDiemJson
{
    public string MaDK { get; set; } = "";
    public double? Cc { get; set; }
    public double? Gk { get; set; }
    public double? Ck { get; set; }
    public bool DuDiem { get; set; }
    public string DiemTK { get; set; } = "";
    public string? DiemChu { get; set; }
    public string DiemHe4 { get; set; } = "";
    public string LopBadgeChu { get; set; } = "";

    public static DongDiemJson Tao(string maDK, Models.Documents.DiemThanhPhan diem, bool duDiem,
        double? diemTK, string? diemChu, int? diemHe4) => new()
    {
        MaDK = maDK,
        Cc = diem.Cc,
        Gk = diem.Gk,
        Ck = diem.Ck,
        DuDiem = duDiem,
        DiemTK = Infrastructure.HienThi.Diem(diemTK),
        DiemChu = diemChu,
        DiemHe4 = diemHe4?.ToString() ?? "–",
        LopBadgeChu = Infrastructure.HienThi.LopBadgeDiemChu(diemChu)
    };

    public static DongDiemJson Tao(DongNhapDiem d)
        => Tao(d.MaDK.ToString(), d.Diem, d.DuDiem, d.DiemTK, d.DiemChu, d.DiemHe4);

    public static DongDiemJson Tao(KetQuaMon m)
        => Tao(m.MaDK.ToString(), m.Diem, m.DuDiem, m.DiemTK, m.DiemChu, m.DiemHe4);
}

/// <summary>Màn hình nhập điểm G-03.</summary>
public class NhapDiemViewModel
{
    public LopHocPhanRow LopHocPhan { get; set; } = new();
    public List<DongNhapDiem> DanhSach { get; set; } = new();
    public string? LocLop { get; set; }
}

/// <summary>Tổng quan GV (G-01) và danh sách lớp học phần (G-02).</summary>
public class LopHocPhanViewModel
{
    /// <summary>Học kỳ đang chọn dạng "2026-2027_1"; null / rỗng = mọi học kỳ (chỉ G-02).</summary>
    public string? KyChon { get; set; }
    /// <summary>Các học kỳ GV có dạy, mới nhất trước.</summary>
    public List<(string NamHoc, int HocKy)> DsHocKy { get; set; } = new();
    public List<LopHocPhanRow> DanhSach { get; set; } = new();

    public int TongSiSo => DanhSach.Sum(l => l.SiSo);
    public int SoChuaDuDiem => DanhSach.Sum(l => l.SiSo - l.SoDuDiem);

    public static string MaKy(string namHoc, int hocKy) => $"{namHoc}_{hocKy}";

    /// <summary>"2026-2027_1" → ("2026-2027", 1).</summary>
    public static bool TryParseKy(string? ma, out string namHoc, out int hocKy)
    {
        namHoc = "";
        hocKy = 0;
        var phan = ma?.Split('_');
        if (phan is not { Length: 2 } || !int.TryParse(phan[1], out hocKy)) return false;
        namHoc = phan[0];
        return true;
    }

    /// <summary>Tiến độ nhập điểm (%) của một lớp học phần – chỉ là tỉ lệ đếm để vẽ thanh tiến độ.</summary>
    public static int PhanTramTienDo(LopHocPhanRow l) => l.SiSo == 0 ? 0 : l.SoDuDiem * 100 / l.SiSo;
}
