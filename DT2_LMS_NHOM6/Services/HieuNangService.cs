using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Trang hiệu năng A-12 (tùy chọn):
///   1. explain("executionStats") 3 truy vấn chính, mỗi truy vấn 2 lần – để planner tự chọn index
///      và ép quét toàn bộ collection bằng hint({ $natural: 1 }) – rồi so sánh stage, số khóa / document đã đọc.
///   2. Chạy pipeline BaoCaoHocLuc toàn trường 20 lần, lấy min / trung bình / max thời gian.
/// Chỉ ĐỌC, không ghi gì vào database.
/// </summary>
public class HieuNangService
{
    public const int SoLanChayPipeline = 20;

    private readonly MongoContext _db;
    private readonly BaoCaoService _baoCao;
    private readonly HocKyHienTaiOptions _hocKyHienTai;

    public HieuNangService(MongoContext db, BaoCaoService baoCao, IOptions<HocKyHienTaiOptions> hocKyHienTai)
    {
        _db = db;
        _baoCao = baoCao;
        _hocKyHienTai = hocKyHienTai.Value;
    }

    public Task<long> DemSinhVienAsync()
        => _db.SinhVien.CountDocumentsAsync(FilterDefinition<SinhVien>.Empty);

    /// <summary>
    /// Giá trị mặc định lấy từ dữ liệu thật: SV đầu tiên (theo MSSV) có lần học trong học kỳ hiện tại,
    /// lấy MSSV, giảng viên, môn của lần học đó.
    ///   db.sinhvien.find({ monHoc: { $elemMatch: { namHoc, hocKy } } }).sort({ maSV: 1 }).limit(1)
    /// </summary>
    public async Task<ThamSoHieuNang> ThamSoMacDinhAsync()
    {
        var hk = _hocKyHienTai;
        var locHocKy = new BsonDocument("monHoc", new BsonDocument("$elemMatch",
            new BsonDocument { { "namHoc", hk.NamHoc }, { "hocKy", hk.HocKy } }));
        var sv = await _db.SinhVien.Find(locHocKy).SortBy(s => s.MaSV).FirstOrDefaultAsync()
              ?? await _db.SinhVien.Find(FilterDefinition<SinhVien>.Empty).SortBy(s => s.MaSV).FirstOrDefaultAsync();

        var lanHoc = sv?.MonHoc.FirstOrDefault(m => m.NamHoc == hk.NamHoc && m.HocKy == hk.HocKy)
                  ?? sv?.MonHoc.FirstOrDefault();
        return new ThamSoHieuNang
        {
            MaSV = sv?.MaSV ?? "",
            MaGV = lanHoc?.GiangVien.MaGV ?? "",
            NamHoc = lanHoc?.NamHoc ?? hk.NamHoc,
            HocKy = lanHoc?.HocKy ?? hk.HocKy,
            MaMon = lanHoc?.MaMon ?? ""
        };
    }

    /// <summary>Ba truy vấn chính, mỗi truy vấn explain 2 lần (có index / ép COLLSCAN).</summary>
    public async Task<List<SoSanhTruyVan>> SoSanhTruyVanAsync(ThamSoHieuNang ts)
    {
        var dsTruyVan = new (string Ten, BsonDocument Filter, string GiaiThich)[]
        {
            ("(a) Tìm sinh viên theo MSSV",
                // { maSV: "2001250001" }
                new BsonDocument("maSV", ts.MaSV),
                "Index unique ux_maSV { maSV: 1 }: có index thì chỉ đọc 1 khóa và 1 document "
                + "(MongoDB 8 còn dùng EXPRESS_IXSCAN – đường tắt cho phép so sánh bằng trên index unique). "
                + "Không có index (COLLSCAN) phải đọc toàn bộ document của collection để trả về đúng 1 document."),

            ("(b) Danh sách nhập điểm: lần học do giảng viên dạy trong một học kỳ",
                // { monHoc: { $elemMatch: { "giangVien.maGV": "GV005", namHoc: "2026-2027", hocKy: 1 } } } – giống bộ lọc lớp học phần
                new BsonDocument("monHoc", new BsonDocument("$elemMatch",
                    DieuKienLanHoc.Tao(ts.MaGV, null, ts.NamHoc, ts.HocKy).TrongPhanTu)),
                "Index ghép ix_monHoc_maGV_namHoc_hocKy trên 3 trường của MẢNG NHÚNG monHoc là index multikey: "
                + "mỗi phần tử của mảng sinh một khóa index riêng, nên một sinh viên có nhiều khóa (mỗi lần học một khóa). "
                + "Dùng $elemMatch để 3 điều kiện cùng áp lên MỘT phần tử, MongoDB ghép được giới hạn quét của cả 3 trường "
                + "→ chỉ đọc khóa của đúng lớp học phần. Không index thì phải mở từng hồ sơ và duyệt cả mảng monHoc."),

            ("(c) Tìm sinh viên đã học một môn",
                // { "monHoc.maMon": "INT301" }
                new BsonDocument("monHoc.maMon", ts.MaMon),
                "Index multikey ix_monHoc_maMon { \"monHoc.maMon\": 1 }: tìm thẳng các khóa bằng mã môn. "
                + "keysExamined có thể lớn hơn nReturned khi một sinh viên học môn đó nhiều lần (học lại / cải thiện) "
                + "– mỗi lần học là một khóa, MongoDB tự loại document trùng. Đây cũng là truy vấn của lệnh lan truyền tên môn (updateMany).")
        };

        var ketQua = new List<SoSanhTruyVan>();
        foreach (var (ten, filter, giaiThich) in dsTruyVan)
        {
            ketQua.Add(new SoSanhTruyVan
            {
                Ten = ten,
                Filter = MongoCommandFormatter.DinhDang(filter),
                GiaiThich = giaiThich,
                CoIndex = await ExplainAsync(filter, epQuetToanBo: false),
                KhongIndex = await ExplainAsync(filter, epQuetToanBo: true)
            });
        }
        return ketQua;
    }

    /// <summary>
    /// Chạy lệnh explain cho find trên sinhvien:
    ///   db.runCommand({ explain: { find: "sinhvien", filter, [hint: { $natural: 1 }] }, verbosity: "executionStats" })
    /// tương đương mongosh: db.sinhvien.find(filter)[.hint({ $natural: 1 })].explain("executionStats")
    /// </summary>
    private async Task<KetQuaExplain> ExplainAsync(BsonDocument filter, bool epQuetToanBo)
    {
        var lenhFind = new BsonDocument { { "find", MongoContext.TenSinhVien }, { "filter", filter } };
        if (epQuetToanBo) lenhFind.Add("hint", new BsonDocument("$natural", 1)); // $natural = đọc theo thứ tự lưu trữ, bỏ qua mọi index

        var explain = await _db.Database.RunCommandAsync<BsonDocument>(new BsonDocument
        {
            { "explain", lenhFind },
            { "verbosity", "executionStats" }
        });

        // queryPlanner.winningPlan: cây stage (FETCH → IXSCAN, hoặc COLLSCAN); duyệt để lấy tên stage và index
        var cacStage = new List<string>();
        string? tenIndex = null;
        var laMultikey = false;
        DuyetCayStage(explain["queryPlanner"]["winningPlan"].AsBsonDocument, cacStage, ref tenIndex, ref laMultikey);

        var thongKe = explain["executionStats"].AsBsonDocument;
        return new KetQuaExplain
        {
            LenhMongosh = $"db.sinhvien.find({MongoCommandFormatter.DinhDang(filter)})"
                        + (epQuetToanBo ? ".hint({ $natural: 1 })" : "")
                        + ".explain(\"executionStats\")",
            StageThang = cacStage.FirstOrDefault(s => s.Contains("IXSCAN"))
                      ?? cacStage.FirstOrDefault(s => s == "COLLSCAN")
                      ?? cacStage.LastOrDefault() ?? "",
            ChuoiStage = string.Join(" → ", cacStage),
            TenIndex = tenIndex,
            LaMultikey = laMultikey,
            NReturned = thongKe["nReturned"].ToInt64(),
            ExecutionTimeMillis = thongKe["executionTimeMillis"].ToInt64(),
            KeysExamined = thongKe["totalKeysExamined"].ToInt64(),
            DocsExamined = thongKe["totalDocsExamined"].ToInt64()
        };
    }

    /// <summary>
    /// Duyệt cây kế hoạch: mỗi nút có "stage" và stage con ở "inputStage" / "inputStages".
    /// Khi server chạy bằng engine SBE, kế hoạch nằm trong "queryPlan".
    /// </summary>
    private static void DuyetCayStage(BsonDocument nut, List<string> cacStage, ref string? tenIndex, ref bool laMultikey)
    {
        if (nut.TryGetValue("queryPlan", out var queryPlan))
        {
            DuyetCayStage(queryPlan.AsBsonDocument, cacStage, ref tenIndex, ref laMultikey);
            return;
        }
        if (nut.TryGetValue("stage", out var stage)) cacStage.Add(stage.AsString);
        if (nut.TryGetValue("indexName", out var ten)) tenIndex = ten.AsString;
        if (nut.TryGetValue("isMultiKey", out var multikey) && multikey.IsBoolean) laMultikey |= multikey.AsBoolean;

        if (nut.TryGetValue("inputStage", out var con))
            DuyetCayStage(con.AsBsonDocument, cacStage, ref tenIndex, ref laMultikey);
        if (nut.TryGetValue("inputStages", out var dsCon))
            foreach (var c in dsCon.AsBsonArray)
                DuyetCayStage(c.AsBsonDocument, cacStage, ref tenIndex, ref laMultikey);
    }

    /// <summary>Chạy pipeline BaoCaoHocLuc toàn trường (tích lũy) 20 lần liên tiếp, ghi thời gian từng lần (Stopwatch ở PipelineRunner).</summary>
    public async Task<KetQuaDoPipeline> DoPipelineAsync()
    {
        var ketQua = new KetQuaDoPipeline { SoLan = SoLanChayPipeline };
        for (var i = 0; i < SoLanChayPipeline; i++)
        {
            var lan = await _baoCao.BaoCaoHocLucAsync(BoLocSinhVien.TatCa(), PhamViBaoCao.TichLuy());
            ketQua.CacLanMs.Add(lan.ThoiGianMs);
            ketQua.SoDong = lan.DuLieu.Count;
        }
        return ketQua;
    }
}
