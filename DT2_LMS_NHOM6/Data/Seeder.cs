using System.Globalization;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;
using MongoDB.Bson;
using MongoDB.Driver;
using static DT2_LMS_NHOM6.Data.DuLieuMau;

namespace DT2_LMS_NHOM6.Data;

/// <summary>
/// Sinh dữ liệu mẫu theo thiết kế mục 7: 5 lớp, 8 GV, 14 môn, 100 SV (gồm 5 SV kiểm thử cố định), 109 tài khoản.
/// Dùng Random(2026) → mỗi lần chạy ra đúng một bộ dữ liệu (trừ maDK/_id là ObjectId mới).
/// </summary>
public class Seeder
{
    private const int BcryptWorkFactor = 10;

    // Mục tiêu tổng số lần F trên toàn bộ dữ liệu (thiết kế 7.4: khoảng 15–20)
    private const int SoFToiThieu = 16;
    private const int SoFToiDa = 19;
    // Số SV học kỳ hè ngẫu nhiên (cộng TS2 học lại + TS3 cải thiện = ~25 SV)
    private const int SoHocLai = 11;
    // Số SV ngẫu nhiên có ít nhất một môn F (= SoHocLai + 2 SV vẫn nợ môn, không học lại)
    private const int SoSvCoF = SoHocLai + 2;
    private const int SoCaiThien = 7;
    private const int SoCaiThienThapHon = 2;
    private const int SoHocVuot = 5;

    private readonly MongoContext _context;
    private readonly ILogger _logger;
    private readonly Random _rd = new(HatGiongNgauNhien);
    private readonly DateTime _bayGio = DateTime.UtcNow;

    private readonly Dictionary<string, MonHoc> _monTheoMa = new();
    private readonly Dictionary<string, GiangVien> _gvTheoMa = new();
    private readonly Dictionary<string, string> _gvDayMon = DuLieuMau.MonHoc.ToDictionary(m => m.MaMon, m => m.MaGV);

    /// <summary>Sinh viên đang dựng kèm "năng lực" (chỉ dùng khi sinh điểm, không lưu DB).</summary>
    private class SvTam
    {
        public required SinhVien Sv { get; init; }
        public double NangLuc { get; init; }
        public bool LaKiemThu { get; init; }
    }

    public Seeder(MongoContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ChayAsync()
    {
        var giangViens = TaoGiangVien();
        var monHocs = TaoMonHoc();
        var lopHocs = TaoLopHoc();
        var dsSv = TaoSinhVien(lopHocs);
        DieuChinhSoLanF(dsSv);
        TaoHocKyHe(dsSv);
        KiemTraRangBuoc(dsSv, lopHocs);

        _logger.LogInformation("Đang băm mật khẩu {So} tài khoản (BCrypt work factor {Wf})...", 1 + giangViens.Count + dsSv.Count, BcryptWorkFactor);
        var taiKhoans = TaoTaiKhoan(giangViens, dsSv);

        // sinhvien trống nhưng collection khác có thể còn dữ liệu cũ → xóa để tránh trùng khóa unique
        await _context.GiangVien.DeleteManyAsync(FilterDefinition<GiangVien>.Empty);
        await _context.MonHoc.DeleteManyAsync(FilterDefinition<MonHoc>.Empty);
        await _context.LopHoc.DeleteManyAsync(FilterDefinition<LopHoc>.Empty);
        await _context.TaiKhoan.DeleteManyAsync(FilterDefinition<TaiKhoan>.Empty);

        await _context.GiangVien.InsertManyAsync(giangViens);
        await _context.MonHoc.InsertManyAsync(monHocs);
        await _context.LopHoc.InsertManyAsync(lopHocs);
        await _context.SinhVien.InsertManyAsync(dsSv.Select(x => x.Sv));
        await _context.TaiKhoan.InsertManyAsync(taiKhoans);

        InThongKe(dsSv, lopHocs, taiKhoans.Count);
    }

    // =====================================================================
    // Danh mục: giảng viên, môn học, lớp
    // =====================================================================

    private List<GiangVien> TaoGiangVien()
    {
        foreach (var g in DuLieuMau.GiangVien)
        {
            _gvTheoMa[g.MaGV] = new GiangVien
            {
                MaGV = g.MaGV, HoTen = g.HoTen, HocVi = g.HocVi, Email = g.Email,
                SoDienThoai = g.SoDienThoai, Khoa = Khoa, NgayTao = _bayGio
            };
        }
        return _gvTheoMa.Values.ToList();
    }

    private List<MonHoc> TaoMonHoc()
    {
        foreach (var m in DuLieuMau.MonHoc)
        {
            _monTheoMa[m.MaMon] = new MonHoc
            {
                MaMon = m.MaMon, TenMon = m.TenMon, SoTinChi = m.SoTinChi,
                TrongSoMacDinh = new TrongSo { Cc = m.Cc, Gk = m.Gk, Ck = m.Ck },
                Khoa = Khoa, NgayTao = _bayGio
            };
        }
        return _monTheoMa.Values.ToList();
    }

    private List<LopHoc> TaoLopHoc() => Lop.Select(l => new LopHoc
    {
        MaLop = l.MaLop,
        TenLop = $"Đại học {l.Nganh} 16 - Lớp {l.SoThuTu:00}",
        NienKhoa = NienKhoa,
        Nganh = l.Nganh,
        Khoa = Khoa,
        CoVanHocTap = ThamChieuGV(l.MaCVHT),
        NgayTao = _bayGio
    }).ToList();

    private GiangVienRef ThamChieuGV(string maGV) =>
        new() { MaGV = maGV, HoTen = _gvTheoMa[maGV].HoTenKemHocVi };

    // =====================================================================
    // Sinh viên
    // =====================================================================

    private List<SvTam> TaoSinhVien(List<LopHoc> lopHocs)
    {
        var ds = new List<SvTam>();
        var hoTenDaDung = new HashSet<string>();

        // 5 SV kiểm thử (lớp 16DHTH01, MSSV 2001250001–2001250005) – dữ liệu cố định mục 7.5
        foreach (var sv in TaoSinhVienKiemThu())
        {
            hoTenDaDung.Add(sv.HoTen);
            ds.Add(new SvTam { Sv = sv, NangLuc = 0, LaKiemThu = true });
        }

        int stt = 0; // số thứ tự toàn trường 1..100 → MSSV 2001250001..2001250100
        foreach (var lop in lopHocs)
        {
            var keHoach = ChonMonChoLop(lop.MaLop);
            _logger.LogInformation("Lớp {Lop}: HK1 25-26 [{A}], HK2 25-26 [{B}], HK1 26-27 [{C}]", lop.MaLop,
                string.Join(", ", keHoach.HK1), string.Join(", ", keHoach.HK2), string.Join(", ", keHoach.HienTai));

            for (int i = 0; i < SoSinhVienMoiLop; i++)
            {
                stt++;
                if (stt <= 5) continue; // đã tạo ở phần SV kiểm thử

                var gioiTinh = _rd.NextDouble() < 0.6 ? HangSo.GioiTinh.Nam : HangSo.GioiTinh.Nu;
                string hoTen;
                do { hoTen = SinhHoTen(gioiTinh); } while (!hoTenDaDung.Add(hoTen));

                var maSV = $"2001250{stt:000}";
                var nangLuc = Math.Clamp(7.0 + 1.3 * Chuan(), 2.5, 9.8); // năng lực ~ N(7.0, 1.3)

                var sv = TaoHoSo(maSV, hoTen, gioiTinh,
                    new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(_rd.Next(365)),
                    SinhSoDienThoai(), DiaChi[_rd.Next(DiaChi.Length)], lop.MaLop);

                foreach (var maMon in keHoach.HK1)
                    sv.MonHoc.Add(TaoLanHoc(maMon, HK1_2526, 1, HangSo.HinhThuc.HocMoi, SinhDiem(nangLuc, coCuoiKy: true)));
                foreach (var maMon in keHoach.HK2)
                    sv.MonHoc.Add(TaoLanHoc(maMon, HK2_2526, 1, HangSo.HinhThuc.HocMoi, SinhDiem(nangLuc, coCuoiKy: true)));
                foreach (var maMon in keHoach.HienTai)
                    sv.MonHoc.Add(TaoLanHoc(maMon, HK1_2627, 1, HangSo.HinhThuc.HocMoi, SinhDiem(nangLuc, coCuoiKy: false)));

                ds.Add(new SvTam { Sv = sv, NangLuc = nangLuc, LaKiemThu = false });
            }
        }

        return ds.OrderBy(x => x.Sv.MaSV).ToList();
    }

    private record KeHoachLop(List<string> HK1, List<string> HK2, List<string> HienTai);

    /// <summary>
    /// Mỗi lớp chọn 2–3 môn HK1 25-26, 2–3 môn HK2 25-26, 1–2 môn HK1 26-27 (mục 7.4).
    /// Giữ tổng ≤ 7 để mỗi SV còn chỗ cho 1 môn học kỳ hè mà không vượt 8 phần tử.
    /// </summary>
    private KeHoachLop ChonMonChoLop(string maLop)
    {
        int soHK1 = _rd.Next(2, 4), soHK2 = _rd.Next(2, 4), soHienTai = _rd.Next(1, 3);
        if (soHK1 + soHK2 + soHienTai > 7) soHienTai = 1;

        var hienTai = new List<string>();
        if (LopBatBuocINT301.Contains(maLop)) hienTai.Add("INT301");
        hienTai.AddRange(ChonNgauNhien(MaMonTheoNhom(NhomHocKy.HK1_2627).Except(hienTai).ToList(), soHienTai - hienTai.Count));

        return new KeHoachLop(
            ChonNgauNhien(MaMonTheoNhom(NhomHocKy.HK1_2526), soHK1),
            ChonNgauNhien(MaMonTheoNhom(NhomHocKy.HK2_2526), soHK2),
            hienTai);
    }

    private static List<string> MaMonTheoNhom(NhomHocKy nhom) =>
        DuLieuMau.MonHoc.Where(m => m.Nhom == nhom).Select(m => m.MaMon).ToList();

    private SinhVien TaoHoSo(string maSV, string hoTen, string gioiTinh, DateTime ngaySinh,
                             string soDienThoai, string diaChi, string maLop) => new()
    {
        MaSV = maSV,
        HoTen = hoTen,
        NgaySinh = ngaySinh,
        GioiTinh = gioiTinh,
        Email = $"{maSV}@sv.eduscore.vn",
        SoDienThoai = soDienThoai,
        DiaChi = diaChi,
        MaLop = maLop,
        TrangThai = HangSo.TrangThaiSinhVien.DangHoc,
        MonHoc = new List<MonHocDangKy>(),
        NgayTao = _bayGio,
        NgayCapNhat = _bayGio
    };

    /// <summary>
    /// Tạo một phần tử monHoc. soTinChi, trongSo chụp từ danh mục môn; giảng viên theo bảng 7.2.
    /// capNhatLuc/capNhatBoi theo học kỳ (mục 7.4).
    /// </summary>
    private MonHocDangKy TaoLanHoc(string maMon, HocKyMau hk, int lanHoc, string hinhThuc, DiemThanhPhan diem)
    {
        var mon = _monTheoMa[maMon];
        var maGV = _gvDayMon[maMon];
        return new MonHocDangKy
        {
            MaDK = ObjectId.GenerateNewId(),
            MaMon = mon.MaMon,
            TenMon = mon.TenMon,
            SoTinChi = mon.SoTinChi,
            NamHoc = hk.NamHoc,
            HocKy = hk.HocKy,
            LanHoc = lanHoc,
            HinhThuc = hinhThuc,
            GiangVien = ThamChieuGV(maGV),
            TrongSo = new TrongSo { Cc = mon.TrongSoMacDinh.Cc, Gk = mon.TrongSoMacDinh.Gk, Ck = mon.TrongSoMacDinh.Ck },
            Diem = diem,
            CapNhatLuc = hk.CapNhatLuc,
            CapNhatBoi = maGV
        };
    }

    // =====================================================================
    // 5 SV kiểm thử – mục 7.5 (dữ liệu cố định, KHÔNG dùng Random)
    // =====================================================================

    private List<SinhVien> TaoSinhVienKiemThu()
    {
        const string lop = "16DHTH01";
        DiemThanhPhan D(double cc, double gk, double? ck) => new() { Cc = cc, Gk = gk, Ck = ck };
        MonHocDangKy Moi(string maMon, HocKyMau hk, DiemThanhPhan d) => TaoLanHoc(maMon, hk, 1, HangSo.HinhThuc.HocMoi, d);

        // TS1 – làm tròn nửa lên (INT101 tong 845 → 8.5/A)
        var ts1 = TaoHoSo("2001250001", "Nguyễn Hoàng Minh", HangSo.GioiTinh.Nam, NgayUtc(2007, 3, 15), "0901234561", "Tân Phú, TP.HCM", lop);
        ts1.MonHoc.AddRange(new[]
        {
            Moi("INT101", HK1_2526, D(8, 8.5, 8.5)),
            Moi("MAT101", HK1_2526, D(7, 6, 6.5)),
            Moi("INT102", HK2_2526, D(9, 9, 9)),
            Moi("ENG102", HK2_2526, D(8, 7, 7)),
            Moi("INT201", HK1_2627, D(8, 7, null)),
        });

        // TS2 – học lại INT101 ở học kỳ hè
        var ts2 = TaoHoSo("2001250002", "Trần Thị Thu Trang", HangSo.GioiTinh.Nu, NgayUtc(2007, 7, 22), "0912345672", "Gò Vấp, TP.HCM", lop);
        ts2.MonHoc.AddRange(new[]
        {
            Moi("INT101", HK1_2526, D(5, 2, 2.5)),
            Moi("MAT101", HK1_2526, D(8, 7, 7.5)),
            Moi("INT102", HK2_2526, D(7, 6, 6)),
            Moi("POL102", HK2_2526, D(9, 8, 8)),
            TaoLanHoc("INT101", He_2526, 2, HangSo.HinhThuc.HocLai, D(9, 8, 8.5)),
            Moi("INT202", HK1_2627, D(9, 8, null)),
        });

        // TS3 – cải thiện MAT101 nhưng điểm mới thấp hơn
        var ts3 = TaoHoSo("2001250003", "Lê Văn Phúc", HangSo.GioiTinh.Nam, NgayUtc(2007, 11, 9), "0933456783", "Biên Hòa, Đồng Nai", lop);
        ts3.MonHoc.AddRange(new[]
        {
            Moi("MAT101", HK1_2526, D(6, 5, 6)),
            Moi("ENG101", HK1_2526, D(9, 8, 8)),
            Moi("MAT102", HK2_2526, D(7, 7, 7)),
            Moi("INT102", HK2_2526, D(8, 8, 9)),
            TaoLanHoc("MAT101", He_2526, 2, HangSo.HinhThuc.CaiThien, D(5, 5, 4.5)),
            Moi("INT203", HK1_2627, D(7, 6.5, null)),
        });

        // TS4 – nợ 2 môn (POL101, ENG102)
        var ts4 = TaoHoSo("2001250004", "Phạm Gia Huy", HangSo.GioiTinh.Nam, NgayUtc(2007, 1, 1), "0976567894", "Bình Tân, TP.HCM", lop);
        ts4.MonHoc.AddRange(new[]
        {
            Moi("POL101", HK1_2526, D(4, 3, 3)),
            Moi("INT101", HK1_2526, D(7, 6, 5.5)),
            Moi("INT102", HK2_2526, D(6, 4, 4)),
            Moi("ENG102", HK2_2526, D(5, 3, 3)),
            Moi("INT201", HK1_2627, D(6, 5, null)),
        });

        // TS5 – biên Xuất sắc (tích lũy 3.60)
        var ts5 = TaoHoSo("2001250005", "Võ Ngọc Bảo Anh", HangSo.GioiTinh.Nu, NgayUtc(2007, 5, 28), "0988678905", "Thủ Đức, TP.HCM", lop);
        ts5.MonHoc.AddRange(new[]
        {
            Moi("INT101", HK1_2526, D(10, 9, 9)),
            Moi("MAT101", HK1_2526, D(9, 8.5, 9)),
            Moi("ENG101", HK1_2526, D(8, 7.5, 7.5)),
            Moi("POL102", HK2_2526, D(9, 8, 7.5)),
            Moi("INT301", HK1_2627, D(10, 9, null)),
        });

        return new List<SinhVien> { ts1, ts2, ts3, ts4, ts5 };
    }

    private static DateTime NgayUtc(int nam, int thang, int ngay) => new(nam, thang, ngay, 0, 0, 0, DateTimeKind.Utc);

    // =====================================================================
    // Điều chỉnh số lần F và tạo học kỳ hè (mục 7.4)
    // =====================================================================

    /// <summary>
    /// Điều chỉnh để có đủ SV nợ môn và tổng số lần F (HK1, HK2 2025-2026) nằm trong [SoFToiThieu, SoFToiDa]:
    ///   1. Số SV ngẫu nhiên có F ≥ SoSvCoF (đủ cho ~11 SV học lại + vài SV vẫn nợ môn):
    ///      hạ điểm một môn của SV năng lực thấp nhất chưa có F.
    ///   2. Thừa F → nâng điểm một môn F của SV đang có nhiều F nhất (không làm giảm số SV có F).
    ///   3. Thiếu F → lại hạ điểm như bước 1.
    /// Chỉ chạm vào SV ngẫu nhiên, không chạm SV kiểm thử.
    /// </summary>
    private void DieuChinhSoLanF(List<SvTam> ds)
    {
        var ngauNhien = ds.Where(x => !x.LaKiemThu).ToList();

        void ThemMotLanF()
        {
            var sv = ngauNhien.Where(x => !CoMonF(x.Sv)).OrderBy(x => x.NangLuc).First();
            var dsDuDiem = sv.Sv.MonHoc.Where(DuDiem).ToList();
            var lan = dsDuDiem[_rd.Next(dsDuDiem.Count)];
            SinhDiemDenKhi(lan, nangLuc: 2.8, dieuKien: tk => tk < 4.0);
        }

        while (ngauNhien.Count(x => CoMonF(x.Sv)) < SoSvCoF)
            ThemMotLanF();

        while (DemLanF(ds) > SoFToiDa)
        {
            var sv = ngauNhien.Where(x => CoMonF(x.Sv))
                .OrderByDescending(x => x.Sv.MonHoc.Count(LaF)).ThenByDescending(x => x.NangLuc).First();
            var lan = sv.Sv.MonHoc.First(LaF);
            SinhDiemDenKhi(lan, nangLuc: 5.5, dieuKien: tk => tk >= 4.0);
        }

        while (DemLanF(ds) < SoFToiThieu)
            ThemMotLanF();
    }

    /// <summary>
    /// Học kỳ hè 2025-2026 (đủ điểm): học lại môn F, cải thiện môn C/D (vài ca điểm mới thấp hơn), học vượt LAW101/SKL101.
    /// Mỗi SV tối đa 1 môn hè; chỉ chọn SV đang có ≤ 7 phần tử để không vượt 8.
    /// </summary>
    private void TaoHocKyHe(List<SvTam> ds)
    {
        var daChon = new HashSet<string>();
        bool ConCho(SvTam x) => !x.LaKiemThu && !daChon.Contains(x.Sv.MaSV) && x.Sv.MonHoc.Count <= 7;

        // 1) Học lại: SV có môn F → học lại môn F đầu tiên, lanHoc 2, bảo đảm đạt
        foreach (var x in TronNgauNhien(ds.Where(x => ConCho(x) && CoMonF(x.Sv)).ToList()).Take(SoHocLai))
        {
            var monF = x.Sv.MonHoc.First(LaF);
            var lan = TaoLanHoc(monF.MaMon, He_2526, 2, HangSo.HinhThuc.HocLai, new DiemThanhPhan());
            SinhDiemDenKhi(lan, Math.Max(x.NangLuc + 1.5, 6.0), tk => tk >= 4.0);
            x.Sv.MonHoc.Add(lan);
            daChon.Add(x.Sv.MaSV);
        }

        // 2) Cải thiện: SV không có F, có môn C/D → học lại môn đó
        var dsCaiThien = TronNgauNhien(ds.Where(x => ConCho(x) && !CoMonF(x.Sv) && x.Sv.MonHoc.Any(LaCHoacD)).ToList())
            .Take(SoCaiThien).ToList();
        for (int i = 0; i < dsCaiThien.Count; i++)
        {
            var x = dsCaiThien[i];
            bool thapHon = i < SoCaiThienThapHon;
            // ca thấp hơn cần điểm cũ ≥ 4.5 để điểm mới vẫn đạt (≥ 4.0) mà thấp hơn
            var monCu = x.Sv.MonHoc.Where(LaCHoacD).OrderByDescending(DiemTKMau).First();
            var tkCu = DiemTKMau(monCu);
            var lan = TaoLanHoc(monCu.MaMon, He_2526, 2, HangSo.HinhThuc.CaiThien, new DiemThanhPhan());
            if (thapHon && tkCu >= 4.5)
                SinhDiemDenKhi(lan, tkCu - 0.8, tk => tk >= 4.0 && tk < tkCu);
            else
                SinhDiemDenKhi(lan, tkCu + 1.8, tk => tk > tkCu);
            x.Sv.MonHoc.Add(lan);
            daChon.Add(x.Sv.MaSV);
        }

        // 3) Học vượt: SV khá giỏi học trước LAW101 hoặc SKL101 (Học mới, lanHoc 1)
        foreach (var x in TronNgauNhien(ds.Where(x => ConCho(x) && x.NangLuc >= 6.5).ToList()).Take(SoHocVuot))
        {
            var maMon = _rd.Next(2) == 0 ? "LAW101" : "SKL101";
            var lan = TaoLanHoc(maMon, He_2526, 1, HangSo.HinhThuc.HocMoi, new DiemThanhPhan());
            SinhDiemDenKhi(lan, x.NangLuc, tk => tk >= 4.0);
            x.Sv.MonHoc.Add(lan);
            daChon.Add(x.Sv.MaSV);
        }

        // Sắp mảng monHoc theo thời gian (namHoc, hocKy) cho dễ đọc trong Compass
        foreach (var x in ds.Where(x => !x.LaKiemThu))
            x.Sv.MonHoc = x.Sv.MonHoc.OrderBy(m => m.NamHoc).ThenBy(m => m.HocKy).ToList();
    }

    // =====================================================================
    // Tài khoản
    // =====================================================================

    private static List<TaiKhoan> TaoTaiKhoan(List<GiangVien> giangViens, List<SvTam> dsSv)
    {
        var ngayTao = DateTime.UtcNow;
        TaiKhoan Tao(string ten, string matKhau, string vaiTro, string hoTen) => new()
        {
            TenDangNhap = ten,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(matKhau, BcryptWorkFactor),
            VaiTro = vaiTro,
            MaThamChieu = ten,
            HoTen = hoTen,
            TrangThai = HangSo.TrangThaiTaiKhoan.HoatDong,
            LanDangNhapCuoi = null,
            NgayTao = ngayTao
        };

        // Mật khẩu mặc định (mục 4.5): admin/Admin@123; GV: 123456; SV: ngày sinh ddMMyyyy
        var ds = new List<TaiKhoan> { Tao(HangSo.MaAdmin, "Admin@123", HangSo.VaiTro.Admin, "Quản trị hệ thống") };
        ds.AddRange(giangViens.Select(g => Tao(g.MaGV, "123456", HangSo.VaiTro.GiangVien, g.HoTenKemHocVi)));
        ds.AddRange(dsSv.Select(x => Tao(x.Sv.MaSV, x.Sv.NgaySinh.ToString("ddMMyyyy"), HangSo.VaiTro.SinhVien, x.Sv.HoTen)));
        return ds;
    }

    // =====================================================================
    // Kiểm tra ràng buộc & thống kê
    // =====================================================================

    private static void KiemTraRangBuoc(List<SvTam> ds, List<LopHoc> lopHocs)
    {
        var viPham = ds.Where(x => x.Sv.MonHoc.Count < 4 || x.Sv.MonHoc.Count > 8).ToList();
        if (viPham.Count > 0)
            throw new InvalidOperationException("Seeder: SV có số môn ngoài 4–8: " +
                string.Join(", ", viPham.Select(x => $"{x.Sv.MaSV} ({x.Sv.MonHoc.Count})")));

        foreach (var lop in lopHocs)
        {
            var siSo = ds.Count(x => x.Sv.MaLop == lop.MaLop);
            if (siSo != SoSinhVienMoiLop)
                throw new InvalidOperationException($"Seeder: lớp {lop.MaLop} có {siSo} SV, yêu cầu {SoSinhVienMoiLop}.");
        }
    }

    private static void InThongKe(List<SvTam> ds, List<LopHoc> lopHocs, int soTaiKhoan)
    {
        var tatCaLanHoc = ds.SelectMany(x => x.Sv.MonHoc).ToList();
        var soMon = ds.Select(x => x.Sv.MonHoc.Count).ToList();
        var he = tatCaLanHoc.Where(m => m.HocKy == 3).ToList();

        Console.WriteLine();
        Console.WriteLine("================ THỐNG KÊ DỮ LIỆU MẪU ================");
        foreach (var lop in lopHocs)
            Console.WriteLine($"  Lớp {lop.MaLop,-12}: {ds.Count(x => x.Sv.MaLop == lop.MaLop),3} SV");
        Console.WriteLine($"  Tổng sinh viên         : {ds.Count}");
        Console.WriteLine($"  Số phần tử monHoc / SV : min {soMon.Min()}, max {soMon.Max()}, TB {soMon.Average().ToString("0.00", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  Tổng lần học           : {tatCaLanHoc.Count}");
        Console.WriteLine($"  Số lần F               : {tatCaLanHoc.Count(LaF)}");
        Console.WriteLine($"  HK hè 25-26            : học lại {he.Count(m => m.HinhThuc == HangSo.HinhThuc.HocLai)}, " +
                          $"cải thiện {he.Count(m => m.HinhThuc == HangSo.HinhThuc.CaiThien)}, " +
                          $"học vượt {he.Count(m => m.HinhThuc == HangSo.HinhThuc.HocMoi)}");
        Console.WriteLine($"  HK1 2026-2027 có ck null: {tatCaLanHoc.Count(m => m.NamHoc == HK1_2627.NamHoc && m.HocKy == HK1_2627.HocKy && m.Diem.Ck == null)}");
        Console.WriteLine($"  Tài khoản              : {soTaiKhoan}");
        Console.WriteLine("======================================================");
        Console.WriteLine();
    }

    // =====================================================================
    // Hàm phụ sinh điểm
    // =====================================================================

    /// <summary>
    /// Sinh điểm thành phần quanh năng lực: thành phần = năng lực + nhiễu N(0, 1), kẹp 0–10.
    /// CC làm tròn số nguyên (thường cao hơn), GK/CK làm tròn 0.5. coCuoiKy = false → ck = null (học kỳ hiện tại).
    /// </summary>
    private DiemThanhPhan SinhDiem(double nangLuc, bool coCuoiKy) => new()
    {
        Cc = Math.Round(Math.Clamp(nangLuc + 1.0 + Chuan(), 0, 10), MidpointRounding.AwayFromZero),
        Gk = LamTronNuaDiem(nangLuc + Chuan()),
        Ck = coCuoiKy ? LamTronNuaDiem(nangLuc + Chuan()) : null
    };

    /// <summary>Sinh lại điểm (đủ 3 thành phần) cho lần học đến khi điểm TK thỏa điều kiện.</summary>
    private void SinhDiemDenKhi(MonHocDangKy lan, double nangLuc, Func<double, bool> dieuKien)
    {
        for (int lanThu = 0; lanThu < 1000; lanThu++)
        {
            lan.Diem = SinhDiem(nangLuc, coCuoiKy: true);
            if (dieuKien(DiemTKMau(lan))) return;
        }
        throw new InvalidOperationException($"Seeder: không sinh được điểm phù hợp cho {lan.MaMon}.");
    }

    private static double LamTronNuaDiem(double x) => Math.Round(Math.Clamp(x, 0, 10) * 2, MidpointRounding.AwayFromZero) / 2;

    /// <summary>Số ngẫu nhiên phân phối chuẩn N(0, 1) (Box–Muller).</summary>
    private double Chuan()
    {
        double u1 = 1.0 - _rd.NextDouble(), u2 = _rd.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    /// <summary>
    /// Điểm TK ước tính – CHỈ dùng trong Seeder để chọn SV học lại / cải thiện và in thống kê số F.
    /// Ứng dụng KHÔNG dùng hàm này: điểm TK, chữ, GPA luôn tính bằng Aggregation Pipeline.
    /// Cùng công thức floor ở thiết kế mục 3.1 để kết quả khớp pipeline.
    /// </summary>
    private static double DiemTKMau(MonHocDangKy m)
    {
        if (!DuDiem(m)) return double.NaN;
        var tong = m.Diem.Cc!.Value * m.TrongSo.Cc + m.Diem.Gk!.Value * m.TrongSo.Gk + m.Diem.Ck!.Value * m.TrongSo.Ck;
        return Math.Floor(tong / 10 + 0.5000001) / 10;
    }

    private static bool DuDiem(MonHocDangKy m) => m.Diem.Cc != null && m.Diem.Gk != null && m.Diem.Ck != null;
    private static bool LaF(MonHocDangKy m) => DuDiem(m) && DiemTKMau(m) < 4.0;
    private static bool LaCHoacD(MonHocDangKy m) => DuDiem(m) && DiemTKMau(m) is >= 4.0 and < 7.0;
    private static bool CoMonF(SinhVien sv) => sv.MonHoc.Any(LaF);
    private static int DemLanF(List<SvTam> ds) => ds.Sum(x => x.Sv.MonHoc.Count(LaF));

    // =====================================================================
    // Hàm phụ ngẫu nhiên
    // =====================================================================

    private string SinhHoTen(string gioiTinh)
    {
        var laNam = gioiTinh == HangSo.GioiTinh.Nam;
        var tenDem = laNam ? TenDemNam : TenDemNu;
        var ten = laNam ? TenNam : TenNu;
        return $"{Ho[_rd.Next(Ho.Length)]} {tenDem[_rd.Next(tenDem.Length)]} {ten[_rd.Next(ten.Length)]}";
    }

    private string SinhSoDienThoai() =>
        DauSoDienThoai[_rd.Next(DauSoDienThoai.Length)] + _rd.Next(0, 10_000_000).ToString("0000000");

    private List<T> ChonNgauNhien<T>(List<T> nguon, int soLuong) => TronNgauNhien(nguon).Take(soLuong).ToList();

    /// <summary>Trộn Fisher–Yates bằng Random seed cố định (trả về danh sách mới).</summary>
    private List<T> TronNgauNhien<T>(List<T> nguon)
    {
        var ds = new List<T>(nguon);
        for (int i = ds.Count - 1; i > 0; i--)
        {
            int j = _rd.Next(i + 1);
            (ds[i], ds[j]) = (ds[j], ds[i]);
        }
        return ds;
    }
}
