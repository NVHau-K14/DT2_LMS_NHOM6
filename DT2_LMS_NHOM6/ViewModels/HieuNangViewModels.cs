using DT2_LMS_NHOM6.Models.Results;

namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>Giá trị dùng trong 3 truy vấn đo (điền sẵn từ dữ liệu thật, Admin sửa được).</summary>
public class ThamSoHieuNang
{
    public string MaSV { get; set; } = "";
    public string MaGV { get; set; } = "";
    public string NamHoc { get; set; } = "";
    public int HocKy { get; set; }
    public string MaMon { get; set; } = "";
}

/// <summary>Số liệu rút ra từ explain("executionStats") của một lần chạy truy vấn.</summary>
public class KetQuaExplain
{
    /// <summary>Lệnh mongosh tương đương, ví dụ db.sinhvien.find({...}).hint({ $natural: 1 }).explain("executionStats").</summary>
    public string LenhMongosh { get; set; } = "";

    /// <summary>Stage quét dữ liệu của kế hoạch thắng: IXSCAN / EXPRESS_IXSCAN / COLLSCAN.</summary>
    public string StageThang { get; set; } = "";

    /// <summary>Chuỗi stage từ ngoài vào trong, ví dụ "FETCH → IXSCAN".</summary>
    public string ChuoiStage { get; set; } = "";

    public string? TenIndex { get; set; }
    public bool LaMultikey { get; set; }
    public long KeysExamined { get; set; }
    public long DocsExamined { get; set; }
    public long NReturned { get; set; }
    public long ExecutionTimeMillis { get; set; }

    public bool DungIndex => StageThang.Contains("IXSCAN");
}

/// <summary>Một truy vấn chạy 2 lần: để planner chọn index và ép quét toàn bộ (hint $natural).</summary>
public class SoSanhTruyVan
{
    public string Ten { get; set; } = "";
    public string Filter { get; set; } = "";
    public string GiaiThich { get; set; } = "";
    public KetQuaExplain CoIndex { get; set; } = new();
    public KetQuaExplain KhongIndex { get; set; } = new();
}

/// <summary>Kết quả chạy pipeline BaoCaoHocLuc toàn trường nhiều lần.</summary>
public class KetQuaDoPipeline
{
    public int SoLan { get; set; }
    public int SoDong { get; set; }
    public List<double> CacLanMs { get; set; } = new();
    public double MinMs => CacLanMs.Min();
    public double TrungBinhMs => CacLanMs.Average();
    public double MaxMs => CacLanMs.Max();
}

/// <summary>Màn hình A-12.</summary>
public class HieuNangViewModel
{
    public ThamSoHieuNang ThamSo { get; set; } = new();
    public List<HocKyRow> DsHocKy { get; set; } = new();
    public long SoSinhVien { get; set; }

    /// <summary>false = chưa bấm "Chạy đo".</summary>
    public bool DaChay { get; set; }
    public string? Loi { get; set; }
    public List<SoSanhTruyVan> TruyVan { get; set; } = new();
    public KetQuaDoPipeline? Pipeline { get; set; }
}
