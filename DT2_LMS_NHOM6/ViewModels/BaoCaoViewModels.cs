using System.Text.Json;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>Số SV của một mức xếp loại (đếm do pipeline ThongKeXepLoai trả về).</summary>
public record SoLuongXepLoai(string XepLoai, int SoSV);

/// <summary>
/// Một pipeline vừa chạy – hiện trong modal "Xem pipeline" (A-10, G-04):
/// JSON đúng tham số đã chạy, thời gian thực thi, số dòng kết quả.
/// </summary>
public record PipelineDaChay(string Ten, string MoTa, string Json, double ThoiGianMs, int SoDong)
{
    /// <summary>Số stage – đếm từ chính chuỗi JSON (cũng là cách kiểm tra JSON parse được).</summary>
    public int SoStage
    {
        get
        {
            using var tai = JsonDocument.Parse(Json);
            return tai.RootElement.GetArrayLength();
        }
    }

    public static PipelineDaChay Tu<T>(string ten, string moTa, KetQuaPipeline<T> kq)
        => new(ten, moTa, kq.PipelineJson, kq.ThoiGianMs, kq.DuLieu.Count);
}

/// <summary>
/// Cấu hình một biểu đồ Chart.js, render vào thuộc tính data-bieu-do của &lt;canvas&gt;.
/// wwwroot/js/bieu-do.js đọc JSON này và vẽ – JS không tính toán số liệu, chỉ vẽ.
/// </summary>
public class CauHinhBieuDo
{
    private static readonly JsonSerializerOptions CaiDatJson = new(JsonSerializerDefaults.Web);

    /// <summary>Kiểu Chart.js: "bar" | "doughnut" | "line".</summary>
    public string Loai { get; init; } = "bar";
    public List<string> Nhan { get; init; } = new();
    public List<double?> DuLieu { get; init; } = new();
    /// <summary>Màu từng cột / lát (bar, doughnut) hoặc một màu cho cả đường (line).</summary>
    public List<string> Mau { get; init; } = new();
    /// <summary>Tên chuỗi số liệu (hiện ở tooltip).</summary>
    public string TenChuoi { get; init; } = "";
    /// <summary>Giá trị lớn nhất trục Y (GPA: 4); null = tự động.</summary>
    public double? TrucYToiDa { get; init; }
    /// <summary>Số chữ số thập phân khi hiện giá trị (GPA: 2, số SV: 0).</summary>
    public int SoLe { get; init; }

    public string ToJson() => JsonSerializer.Serialize(this, CaiDatJson);

    /// <summary>Biểu đồ phân bố xếp loại (cột hoặc donut) – đủ 7 mức, màu theo badge xếp loại.</summary>
    public static CauHinhBieuDo PhanBoXepLoai(IEnumerable<SoLuongXepLoai> thongKe, string loai) => new()
    {
        Loai = loai,
        Nhan = thongKe.Select(t => t.XepLoai).ToList(),
        DuLieu = thongKe.Select(t => (double?)t.SoSV).ToList(),
        Mau = thongKe.Select(t => HienThi.MauXepLoai(t.XepLoai)).ToList(),
        TenChuoi = "Số sinh viên"
    };
}

/// <summary>
/// Dữ liệu cho partial _BaoCaoHocLuc – dùng chung A-10 (Admin) và G-04 (Giảng viên).
/// Mọi số liệu lấy nguyên từ pipeline BaoCaoHocLuc / ThongKeXepLoai.
/// </summary>
public class BaoCaoHocLucViewModel
{
    public List<BaoCaoHocLucRow> DanhSach { get; set; } = new();

    /// <summary>Đủ 7 mức theo thứ tự Xuất sắc → Chưa xếp loại.</summary>
    public List<SoLuongXepLoai> ThongKe { get; set; } = new();

    /// <summary>Các pipeline đã chạy để dựng trang (modal "Xem pipeline").</summary>
    public List<PipelineDaChay> Pipelines { get; set; } = new();

    /// <summary>"Tích lũy" hoặc "HK1 2025-2026".</summary>
    public string TenPhamVi { get; set; } = "Tích lũy";
    public bool LaTichLuy { get; set; } = true;

    /// <summary>Tiền tố URL hồ sơ SV (Admin: "/Admin/SinhVien/HoSo/"); null = không có link (GV không xem hồ sơ).</summary>
    public string? UrlHoSoSV { get; set; }

    /// <summary>Nút "Xuất Excel" – URL xuất theo đúng bộ lọc đang chọn.</summary>
    public string? UrlXuatExcel { get; set; }

    /// <summary>G-04: phân trang theo lớp sinh hoạt (mỗi lớp một trang); null = không phân trang (A-10).</summary>
    public PhanTrangTheoLop? PhanTrang { get; set; }

    /// <summary>
    /// Các dòng hiện trong bảng: có phân trang → chỉ SV thuộc lớp của trang hiện tại
    /// (lọc hiển thị trên kết quả pipeline, không tính lại gì). Thẻ số và biểu đồ vẫn dùng toàn bộ.
    /// </summary>
    public List<BaoCaoHocLucRow> DongHienThi => PhanTrang?.LopHienTai is { } lop
        ? DanhSach.Where(sv => sv.MaLop == lop).ToList()
        : DanhSach;

    public CauHinhBieuDo BieuDo => CauHinhBieuDo.PhanBoXepLoai(ThongKe, "bar");

    /// <summary>Xếp kết quả ThongKeXepLoai vào đủ 7 mức (mức không có SV → 0).</summary>
    public static List<SoLuongXepLoai> DuBayMuc(IEnumerable<ThongKeXepLoaiRow> thongKe)
    {
        var theoMuc = thongKe.ToDictionary(t => t.XepLoai, t => t.SoSV);
        return HangSo.CacXepLoai.Select(xl => new SoLuongXepLoai(xl, theoMuc.GetValueOrDefault(xl))).ToList();
    }

    /// <summary>Khóa sắp xếp cột Xếp loại: 1 = Xuất sắc … 7 = Chưa xếp loại.</summary>
    public static int ThuTuXepLoai(string? xepLoai)
    {
        var i = Array.IndexOf(HangSo.CacXepLoai, xepLoai);
        return i < 0 ? HangSo.CacXepLoai.Length : i + 1;
    }
}

/// <summary>
/// Phân trang theo lớp sinh hoạt cho bảng báo cáo G-04: trang i = lớp thứ i (mã lớp sắp tăng dần).
/// Chuyển trang bằng form GET (?trang=n), giữ lại các tham số lọc trong ThamSoGiuLai.
/// </summary>
public class PhanTrangTheoLop
{
    /// <summary>Các mã lớp khác nhau trong danh sách, sắp tăng dần.</summary>
    public List<string> CacLop { get; }
    /// <summary>Trang hiện tại (1..TongSoTrang), đã kẹp vào phạm vi.</summary>
    public int Trang { get; }
    /// <summary>Tham số query giữ nguyên khi chuyển trang (ví dụ lhp).</summary>
    public Dictionary<string, string?> ThamSoGiuLai { get; }

    public PhanTrangTheoLop(IEnumerable<BaoCaoHocLucRow> danhSach, int? trang, Dictionary<string, string?> thamSoGiuLai)
    {
        CacLop = danhSach.Select(sv => sv.MaLop).Distinct().Order(StringComparer.Ordinal).ToList();
        Trang = Math.Clamp(trang ?? 1, 1, Math.Max(CacLop.Count, 1));
        ThamSoGiuLai = thamSoGiuLai;
    }

    public int TongSoTrang => CacLop.Count;
    public string? LopHienTai => CacLop.Count == 0 ? null : CacLop[Trang - 1];

    /// <summary>Query string của một trang, ví dụ "?lhp=INT301_2026-2027_1&amp;trang=2".</summary>
    public string UrlTrang(int trang) => QueryString.Create(
        ThamSoGiuLai.Where(t => !string.IsNullOrEmpty(t.Value))
            .Append(new KeyValuePair<string, string?>("trang", trang.ToString()))).ToString();
}

/// <summary>A-10: bộ lọc lớp + phạm vi, kèm báo cáo.</summary>
public class BaoCaoAdminViewModel
{
    /// <summary>null / rỗng = tất cả lớp.</summary>
    public string? MaLop { get; set; }
    /// <summary>"tichluy" hoặc mã học kỳ "2025-2026_2".</summary>
    public string PhamVi { get; set; } = PhamViTichLuy;
    public List<LopHoc> DsLop { get; set; } = new();
    public List<HocKyRow> DsHocKy { get; set; } = new();
    public BaoCaoHocLucViewModel BaoCao { get; set; } = new();

    public const string PhamViTichLuy = "tichluy";
}

/// <summary>G-04: bộ lọc lớp học phần của GV, kèm báo cáo (phạm vi tích lũy).</summary>
public class BaoCaoGiangVienViewModel
{
    /// <summary>Lớp học phần đang chọn dạng "INT301_2026-2027_1"; null = mọi lớp học phần.</summary>
    public string? Lhp { get; set; }
    public List<LopHocPhanRow> DsLopHocPhan { get; set; } = new();
    public BaoCaoHocLucViewModel BaoCao { get; set; } = new();

    public static string MaLhp(LopHocPhanRow l) => $"{l.MaMon}_{l.NamHoc}_{l.HocKy}";
}

/// <summary>A-01: tổng quan Admin.</summary>
public class TongQuanAdminViewModel
{
    public long SoLop { get; set; }
    public long SoSV { get; set; }
    public long SoGV { get; set; }
    public long SoMon { get; set; }
    public List<SoLuongXepLoai> PhanBoXepLoai { get; set; } = new();
    public List<GpaTheoLopRow> GpaTheoLop { get; set; } = new();

    public CauHinhBieuDo BieuDoXepLoai => CauHinhBieuDo.PhanBoXepLoai(PhanBoXepLoai, "doughnut");

    public CauHinhBieuDo BieuDoGpaLop => new()
    {
        Loai = "bar",
        Nhan = GpaTheoLop.Select(l => l.MaLop).ToList(),
        DuLieu = GpaTheoLop.Select(l => l.GpaTrungBinh).ToList(),
        Mau = GpaTheoLop.Select(_ => "#2563EB").ToList(),
        TenChuoi = "GPA tích lũy trung bình",
        TrucYToiDa = 4,
        SoLe = 2
    };
}
