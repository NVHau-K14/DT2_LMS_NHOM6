using System.Diagnostics;
using System.Globalization;
using System.Text;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;

namespace DT2_LMS_NHOM6.Data;

/// <summary>
/// Lệnh: dotnet run --project DT2_LMS_NHOM6 -- --kiem-thu-gpa
/// Chạy các pipeline THẬT (qua DiemService/BaoCaoService) trên DB cho 5 SV kiểm thử TS1–TS5,
/// so với giá trị mong đợi VIẾT CỨNG theo bảng tính tay ở thiết kế mục 7.5.
/// In PASS/FAIL từng tiêu chí, ghi docs/kiem-thu-gpa.md, trả mã thoát 0 (toàn PASS) / 1 (có FAIL).
///
/// Lớp này KHÔNG tính điểm: chỉ so sánh số pipeline trả về với hằng số mong đợi.
/// So sánh số thực bằng == (không dung sai): phép chia của MongoDB làm tròn đúng chuẩn IEEE,
/// nên 327/100 cho ra đúng cùng một số double với hằng 3.27 – lệch dù rất nhỏ cũng là FAIL.
/// </summary>
public static class KiemThuGpa
{
    // ─────────────── Giá trị mong đợi (bảng 7.5) ───────────────

    /// <summary>Một lần học: TK, chữ, hệ 4 (null = chưa đủ điểm) và có được ✓ "Tính tích lũy" không.</summary>
    private record LanHocMongDoi(string MaMon, string NamHoc, int HocKy, double? DiemTK, string? DiemChu, int? DiemHe4, bool TinhTichLuy);

    /// <summary>GPA một học kỳ. SuyRa = true: không ghi trong bảng 7.5 nhưng suy ra trực tiếp từ mục 3.3–3.4.</summary>
    private record HocKyMongDoi(string NamHoc, int HocKy, double? Gpa, string XepLoai, bool SuyRa = false);

    private record CaKiemThu(
        string Ma, string MaSV, string TieuDe, string GiaiThich,
        LanHocMongDoi[] LanHoc, HocKyMongDoi[] HocKy,
        double GpaTichLuy, string XepLoaiTichLuy, int TinChiTichLuy, int SoMonNo);

    private const string N1 = "2025-2026";
    private const string N2 = "2026-2027";

    private static readonly CaKiemThu[] DanhSachCa =
    [
        new("TS1", "2001250001", "Làm tròn nửa lên",
            "INT101: tong = 8×10 + 8.5×30 + 8.5×60 = 845 → 84.5 + 0.5000001 → floor 85 → **8.5 (A)**. " +
            "Nếu dùng `$round` (làm tròn kiểu banker's) 84.5 → 84 → 8.4 (B) là sai. " +
            "GPA HK2 = (4×3 + 3×2)/5 = 3.60 đúng **biên Xuất sắc**. INT201 HK1 2026-2027 chưa có CK → không tính.",
            [
                new("INT101", N1, 1, 8.5, "A", 4, true),
                new("MAT101", N1, 1, 6.4, "C", 2, true),
                new("INT102", N1, 2, 9.0, "A", 4, true),
                new("ENG102", N1, 2, 7.2, "B", 3, true),
                new("INT201", N2, 1, null, null, null, false),
            ],
            [
                new(N1, 1, 3.00, "Khá"),
                new(N1, 2, 3.60, "Xuất sắc"),
                new(N2, 1, null, "Chưa xếp loại", SuyRa: true),
            ],
            3.27, "Giỏi", 11, 0),

        new("TS2", "2001250002", "Học lại",
            "INT101 lần 1 được F (2.6) → học lại ở học kỳ hè, lần 2 được B (8.4). " +
            "GPA HK1 tính cả F: (0×3 + 3×3)/6 = 1.50. GPA tích lũy chỉ lấy lần tốt nhất của INT101 (lần 2, B): " +
            "30/11 = 2.727… → 2.73. Lần 1 không ✓, lần 2 ✓.",
            [
                new("INT101", N1, 1, 2.6, "F", 0, false),
                new("MAT101", N1, 1, 7.4, "B", 3, true),
                new("INT102", N1, 2, 6.1, "C", 2, true),
                new("POL102", N1, 2, 8.1, "B", 3, true),
                new("INT101", N1, 3, 8.4, "B", 3, true),
                new("INT202", N2, 1, null, null, null, false),
            ],
            [
                new(N1, 1, 1.50, "Yếu"),
                new(N1, 2, 2.40, "Trung bình"),
                new(N1, 3, 3.00, "Khá"),
                new(N2, 1, null, "Chưa xếp loại", SuyRa: true),
            ],
            2.73, "Khá", 11, 0),

        new("TS3", "2001250003", "Cải thiện bị điểm thấp hơn",
            "MAT101 lần 1 được C (5.7), học cải thiện ở học kỳ hè chỉ được D (4.7). " +
            "Lần tốt nhất là lần 1 (hệ 4 cao hơn) → GPA tích lũy giữ C: (2×3 + 3×2 + 3×3 + 4×3)/11 = 3.00. " +
            "Lần 1 ✓, lần 2 không ✓. GPA học kỳ hè vẫn tính lần 2: 1×3/3 = 1.00. MAT102 7.0 là biên B.",
            [
                new("MAT101", N1, 1, 5.7, "C", 2, true),
                new("ENG101", N1, 1, 8.2, "B", 3, true),
                new("MAT102", N1, 2, 7.0, "B", 3, true),
                new("INT102", N1, 2, 8.6, "A", 4, true),
                new("MAT101", N1, 3, 4.7, "D", 1, false),
                new("INT203", N2, 1, null, null, null, false),
            ],
            [
                new(N1, 1, 2.40, "Trung bình"),
                new(N1, 2, 3.50, "Giỏi"),
                new(N1, 3, 1.00, "Yếu"),
                new(N2, 1, null, "Chưa xếp loại", SuyRa: true),
            ],
            3.00, "Khá", 11, 0),

        new("TS4", "2001250004", "Nợ môn",
            "POL101 (3.1) và ENG102 (3.4) bị F, chưa học lại → **nợ 2 môn**, không tính vào GPA tích lũy và tín chỉ tích lũy. " +
            "Tích lũy = (2×3 + 1×3)/6 = 1.50 Yếu, TC 6. GPA HK2 tính cả F: (1×3 + 0×2)/5 = 0.60 → **Kém** " +
            "(\"Kém\" chỉ có thể xuất hiện ở GPA học kỳ).",
            [
                new("POL101", N1, 1, 3.1, "F", 0, false),
                new("INT101", N1, 1, 5.8, "C", 2, true),
                new("INT102", N1, 2, 4.2, "D", 1, true),
                new("ENG102", N1, 2, 3.4, "F", 0, false),
                new("INT201", N2, 1, null, null, null, false),
            ],
            [
                new(N1, 1, 1.00, "Yếu"),
                new(N1, 2, 0.60, "Kém"),
                new(N2, 1, null, "Chưa xếp loại", SuyRa: true),
            ],
            1.50, "Yếu", 6, 2),

        new("TS5", "2001250005", "Biên Xuất sắc",
            "MAT101: tong 885 → 8.9 (A). GPA tích lũy = (4×3 + 4×3 + 3×2 + 3×2)/10 = 36/10 = 3.60 đúng **biên Xuất sắc** " +
            "(so sánh trên GPA đã làm tròn, `$gte: 3.6`). INT301 HK1 2026-2027 chưa có CK; TS5 cũng có mặt trong " +
            "lớp học phần INT301 của GV005.",
            [
                new("INT101", N1, 1, 9.1, "A", 4, true),
                new("MAT101", N1, 1, 8.9, "A", 4, true),
                new("ENG101", N1, 1, 7.6, "B", 3, true),
                new("POL102", N1, 2, 7.8, "B", 3, true),
                new("INT301", N2, 1, null, null, null, false),
            ],
            [
                new(N1, 1, 3.75, "Xuất sắc"),
                new(N1, 2, 3.00, "Khá"),
                new(N2, 1, null, "Chưa xếp loại", SuyRa: true),
            ],
            3.60, "Xuất sắc", 10, 0),
    ];

    // Kiểm tra tổng thể trên dữ liệu mẫu (thiết kế mục 7.1: 100 SV, 5 lớp × 20)
    private const int TongSoSV = 100;
    private const int SoLop = 5;

    // ─────────────── Chạy kiểm thử ───────────────

    private record DongKetQua(string TieuChi, string MongDoi, string ThucTe, bool Dat);

    private class Nhom(string tieuDe, string? giaiThich = null)
    {
        public string TieuDe { get; } = tieuDe;
        public string? GiaiThich { get; } = giaiThich;
        public List<DongKetQua> Dong { get; } = new();
    }

    public static async Task<int> ChayAsync(IServiceProvider services, string thuMucGoc)
    {
        using var scope = services.CreateScope();
        var diemService = scope.ServiceProvider.GetRequiredService<DiemService>();
        var baoCaoService = scope.ServiceProvider.GetRequiredService<BaoCaoService>();
        var dongHo = Stopwatch.StartNew();
        var dsNhom = new List<Nhom>();

        // Báo cáo tích lũy của cả 5 SV kiểm thử – một lần gọi pipeline BaoCaoHocLuc
        var baoCaoTichLuy = (await baoCaoService.BaoCaoHocLucAsync(
            BoLocSinhVien.TheoDanhSach(DanhSachCa.Select(c => c.MaSV)), PhamViBaoCao.TichLuy())).DuLieu;

        foreach (var ca in DanhSachCa)
        {
            var nhom = new Nhom($"{ca.Ma} – {ca.MaSV} – {ca.TieuDe}", ca.GiaiThich);
            dsNhom.Add(nhom);

            // 1. Từng lần học (pipeline BangDiem)
            var bangDiem = (await diemService.BangDiemAsync(ca.MaSV)).DuLieu;
            nhom.Dong.Add(SoSanh("Số lần học", ca.LanHoc.Length, bangDiem.Count));
            foreach (var md in ca.LanHoc)
            {
                var ten = $"{md.MaMon} {HienThi.HocKy(md.NamHoc, md.HocKy)}";
                var tt = bangDiem.SingleOrDefault(x => x.MaMon == md.MaMon && x.NamHoc == md.NamHoc && x.HocKy == md.HocKy);
                if (tt == null)
                {
                    nhom.Dong.Add(new DongKetQua($"{ten} – có trong bảng điểm", "có", "không tìm thấy", false));
                    continue;
                }
                nhom.Dong.Add(SoSanhSo($"{ten} – TK", md.DiemTK, tt.DiemTK, "0.0"));
                nhom.Dong.Add(SoSanh($"{ten} – Chữ", md.DiemChu, tt.DiemChu));
                nhom.Dong.Add(SoSanh($"{ten} – Hệ 4", md.DiemHe4, tt.DiemHe4));
                nhom.Dong.Add(SoSanh($"{ten} – Tính tích lũy", md.TinhTichLuy ? "✓" : "–", tt.TinhTichLuy ? "✓" : "–"));
            }

            // 2. GPA từng học kỳ (pipeline GpaHocKy) + đối chiếu BaoCaoHocLuc phạm vi học kỳ
            var gpaHocKy = (await diemService.GpaHocKyAsync(ca.MaSV)).DuLieu;
            nhom.Dong.Add(SoSanh("Số học kỳ có lần học", ca.HocKy.Length, gpaHocKy.Count));
            foreach (var md in ca.HocKy)
            {
                var ten = $"GPA {HienThi.HocKy(md.NamHoc, md.HocKy)}" + (md.SuyRa ? " (suy ra từ 3.3)" : "");
                var tt = gpaHocKy.SingleOrDefault(x => x.NamHoc == md.NamHoc && x.HocKy == md.HocKy);
                nhom.Dong.Add(SoSanhSo(ten, md.Gpa, tt?.Gpa, "0.00"));
                nhom.Dong.Add(SoSanh($"{ten} – xếp loại", md.XepLoai, tt?.XepLoai));

                var baoCaoHocKy = (await baoCaoService.BaoCaoHocLucAsync(
                    BoLocSinhVien.TheoSinhVien(ca.MaSV), PhamViBaoCao.TheoHocKy(md.NamHoc, md.HocKy))).DuLieu.SingleOrDefault();
                nhom.Dong.Add(SoSanhSo($"{ten} – qua BaoCaoHocLuc phạm vi học kỳ", md.Gpa, baoCaoHocKy?.Gpa, "0.00"));
            }

            // 3. Tích lũy (pipeline BaoCaoHocLuc phạm vi tích lũy)
            var tichLuy = baoCaoTichLuy.SingleOrDefault(x => x.MaSV == ca.MaSV);
            nhom.Dong.Add(SoSanhSo("GPA tích lũy", ca.GpaTichLuy, tichLuy?.Gpa, "0.00"));
            nhom.Dong.Add(SoSanh("Xếp loại tích lũy", ca.XepLoaiTichLuy, tichLuy?.XepLoai));
            nhom.Dong.Add(SoSanh("Tín chỉ tích lũy", ca.TinChiTichLuy, tichLuy?.TinChiTichLuy));
            nhom.Dong.Add(SoSanh("Số môn nợ", ca.SoMonNo, tichLuy?.SoMonNo));
        }

        // 4. Tổng thể toàn trường
        var tongThe = new Nhom("Tổng thể toàn trường",
            "Mọi SV đều có dòng trong báo cáo (kể cả chưa có điểm → \"Chưa xếp loại\"); các pipeline thống kê cộng lại khớp sĩ số.");
        dsNhom.Add(tongThe);

        var baoCaoToanTruong = await baoCaoService.BaoCaoHocLucAsync(BoLocSinhVien.TatCa(), PhamViBaoCao.TichLuy());
        tongThe.Dong.Add(SoSanh("BaoCaoHocLuc toàn trường – số dòng", TongSoSV, baoCaoToanTruong.DuLieu.Count));

        var thongKe = (await baoCaoService.ThongKeXepLoaiAsync(BoLocSinhVien.TatCa(), PhamViBaoCao.TichLuy())).DuLieu;
        tongThe.Dong.Add(SoSanh("ThongKeXepLoai – tổng số SV", TongSoSV, thongKe.Sum(x => x.SoSV)));

        var gpaTheoLop = (await baoCaoService.GpaTheoLopAsync()).DuLieu;
        tongThe.Dong.Add(SoSanh("GpaTheoLop – số lớp", SoLop, gpaTheoLop.Count));
        tongThe.Dong.Add(SoSanh("GpaTheoLop – tổng sĩ số", TongSoSV, gpaTheoLop.Sum(x => x.SiSo)));

        var hocKyTrong = (await baoCaoService.BaoCaoHocLucAsync(BoLocSinhVien.TatCa(), PhamViBaoCao.TheoHocKy(N2, 1))).DuLieu;
        tongThe.Dong.Add(SoSanh("BaoCaoHocLuc HK1 2026-2027 (chưa có CK) – số dòng", TongSoSV, hocKyTrong.Count));
        tongThe.Dong.Add(SoSanh("BaoCaoHocLuc HK1 2026-2027 – số SV \"Chưa xếp loại\"", TongSoSV,
            hocKyTrong.Count(x => x.XepLoai == "Chưa xếp loại" && x.Gpa == null)));

        var lopHocPhan = (await diemService.LopHocPhanCuaGVAsync("GV005", N2, 1)).DuLieu;
        var int301 = lopHocPhan.SingleOrDefault(x => x.MaMon == "INT301");
        tongThe.Dong.Add(SoSanh("LopHocPhanCuaGV GV005 HK1 2026-2027 – có INT301", "có", int301 != null ? "có" : "không"));
        tongThe.Dong.Add(SoSanh("LopHocPhanCuaGV INT301 – số SV đủ điểm (CK chưa nhập)", 0, int301?.SoDuDiem));

        var nhapDiem = (await diemService.DanhSachNhapDiemAsync("GV005", "INT301", N2, 1)).DuLieu;
        tongThe.Dong.Add(SoSanh("DanhSachNhapDiem INT301 – sĩ số khớp LopHocPhanCuaGV", int301?.SiSo, nhapDiem.Count));
        var ts5 = nhapDiem.SingleOrDefault(x => x.MaSV == "2001250005");
        tongThe.Dong.Add(SoSanh("DanhSachNhapDiem INT301 – có TS5 (2001250005)", "có", ts5 != null ? "có" : "không"));
        tongThe.Dong.Add(SoSanh("DanhSachNhapDiem INT301 – TS5 điểm CC/GK/CK", "10/9/null",
            ts5 == null ? null : $"{ts5.Diem.Cc}/{ts5.Diem.Gk}/{ts5.Diem.Ck?.ToString() ?? "null"}"));

        dongHo.Stop();
        InRaConsole(dsNhom);
        await GhiMarkdownAsync(dsNhom, thuMucGoc, dongHo.Elapsed.TotalSeconds);

        return dsNhom.SelectMany(n => n.Dong).All(d => d.Dat) ? 0 : 1;
    }

    // ─────────────── So sánh ───────────────

    private static string HienThiGiaTri(object? giaTri) => giaTri switch
    {
        null => "null",
        double d => d.ToString("R", CultureInfo.InvariantCulture),
        _ => giaTri.ToString() ?? "null"
    };

    private static DongKetQua SoSanh<T>(string tieuChi, T? mongDoi, T? thucTe)
        => new(tieuChi, HienThiGiaTri(mongDoi), HienThiGiaTri(thucTe), Equals(mongDoi, thucTe));

    /// <summary>So sánh số thực bằng == tuyệt đối. Bằng nhau → hiển thị theo định dạng; khác → hiển thị giá trị thô.</summary>
    private static DongKetQua SoSanhSo(string tieuChi, double? mongDoi, double? thucTe, string dinhDang)
    {
        var dat = mongDoi == thucTe;
        string Fmt(double? x) => x?.ToString(dinhDang, CultureInfo.InvariantCulture) ?? "null";
        return new DongKetQua(tieuChi, Fmt(mongDoi), dat ? Fmt(thucTe) : HienThiGiaTri(thucTe), dat);
    }

    // ─────────────── Xuất kết quả ───────────────

    private static void InRaConsole(List<Nhom> dsNhom)
    {
        Console.OutputEncoding = Encoding.UTF8;
        foreach (var nhom in dsNhom)
        {
            Console.WriteLine();
            Console.WriteLine($"== {nhom.TieuDe} ==");
            foreach (var d in nhom.Dong)
                Console.WriteLine($"  [{(d.Dat ? "PASS" : "FAIL")}] {d.TieuChi,-62} mong đợi: {d.MongDoi,-14} thực tế: {d.ThucTe}");
        }

        var tatCa = dsNhom.SelectMany(n => n.Dong).ToList();
        var soDat = tatCa.Count(d => d.Dat);
        Console.WriteLine();
        Console.WriteLine($"TỔNG KẾT: {soDat}/{tatCa.Count} PASS" + (soDat == tatCa.Count ? " – TẤT CẢ ĐẠT" : $" – {tatCa.Count - soDat} FAIL"));
    }

    private static async Task GhiMarkdownAsync(List<Nhom> dsNhom, string thuMucGoc, double thoiGianGiay)
    {
        var tatCa = dsNhom.SelectMany(n => n.Dong).ToList();
        var soDat = tatCa.Count(d => d.Dat);
        var sb = new StringBuilder();

        sb.AppendLine("# Kết quả kiểm thử GPA (5 sinh viên kiểm thử – thiết kế mục 7.5)");
        sb.AppendLine();
        sb.AppendLine("> File sinh tự động bởi `dotnet run --project DT2_LMS_NHOM6 -- --kiem-thu-gpa`. Không sửa tay.");
        sb.AppendLine();
        sb.AppendLine($"- **Thời điểm chạy:** {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        sb.AppendLine($"- **Kết quả:** {soDat}/{tatCa.Count} tiêu chí PASS" + (soDat == tatCa.Count ? " ✅" : $" – {tatCa.Count - soDat} FAIL ❌"));
        sb.AppendLine($"- **Thời gian chạy:** {thoiGianGiay:0.00} giây");
        sb.AppendLine("- **Cách kiểm thử:** chạy các pipeline thật (`BangDiem`, `GpaHocKy`, `BaoCaoHocLuc`, `ThongKeXepLoai`, `GpaTheoLop`, " +
                      "`LopHocPhanCuaGV`, `DanhSachNhapDiem`) qua `DiemService`/`BaoCaoService` trên database `eduscore`, " +
                      "so với giá trị mong đợi viết cứng theo bảng tính tay ở `docs/thiet-ke.md` mục 7.5. " +
                      "Số thực so sánh bằng `==` tuyệt đối (không dung sai). Dòng ghi \"suy ra từ 3.3\" không có trong bảng 7.5 " +
                      "mà suy ra trực tiếp từ quy tắc: học kỳ chưa có lần học nào đủ điểm → GPA null, \"Chưa xếp loại\".");
        sb.AppendLine("- **Công thức (mục 3):** `diemTK = floor(tong/10 + 0.5000001)/10`; `GPA = floor(x*100 + 0.5000001)/100`; " +
                      "không dùng `$round` (làm tròn kiểu banker's, 84.5 → 84).");

        foreach (var nhom in dsNhom)
        {
            sb.AppendLine();
            sb.AppendLine($"## {nhom.TieuDe}");
            sb.AppendLine();
            if (nhom.GiaiThich != null)
            {
                sb.AppendLine(nhom.GiaiThich);
                sb.AppendLine();
            }
            sb.AppendLine("| Tiêu chí | Mong đợi | Thực tế | Kết quả |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var d in nhom.Dong)
                sb.AppendLine($"| {d.TieuChi} | {d.MongDoi} | {d.ThucTe} | {(d.Dat ? "✅ PASS" : "❌ FAIL")} |");
        }

        var duongDan = Path.Combine(thuMucGoc, "docs", "kiem-thu-gpa.md");
        await File.WriteAllTextAsync(duongDan, sb.ToString(), new UTF8Encoding(false));
        Console.WriteLine($"Đã ghi {Path.GetRelativePath(thuMucGoc, duongDan)}");
    }
}
