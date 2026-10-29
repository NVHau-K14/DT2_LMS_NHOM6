using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.ViewModels;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Đọc / ghi file Excel bằng ClosedXML (thiết kế mục 6). Lớp này CHỈ làm việc với file:
/// không truy cập MongoDB, không tính điểm – mọi con số (TK, chữ, GPA, xếp loại) truyền vào đã do pipeline tính.
/// Cột "Lớp" luôn ghi tên lớp (16DHTH01) – tra từ BangTenLop truyền vào; maLop (CNTT2501) chỉ dùng nội bộ.
///
/// Bố cục chung của file xuất:
///   dòng 1  CỔNG QUẢN LÝ HỌC TẬP – EDUSCORE
///   dòng 2  TÊN BÁO CÁO
///   dòng 3… "Nhãn:" | giá trị   (thông tin lọc, ngày xuất)
///   (trống)
///   bảng: header in đậm nền xanh nhạt, viền mảnh; MSSV / ngày sinh dạng Text; điểm 0.0; GPA 0.00
/// </summary>
public static class ExcelService
{
    public const string LoaiNoiDung = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const long KichThuocToiDa = 5 * 1024 * 1024;

    private const string TieuDeHeThong = "CỔNG QUẢN LÝ HỌC TẬP – EDUSCORE";
    private const string DinhDangText = "@";
    private const string DinhDangDiem = "0.0";
    private const string DinhDangGpa = "0.00";

    private static readonly XLColor MauTieuDe = XLColor.FromHtml("#1E40AF");
    private static readonly XLColor MauHeader = XLColor.FromHtml("#DBEAFE");
    private static readonly XLColor MauNhomHocKy = XLColor.FromHtml("#EFF6FF");

    // Nhãn phần đầu file bảng điểm – dùng cả khi GHI và khi ĐỌC lại để xác định lớp học phần
    public const string NhanMaMon = "Mã môn";
    public const string NhanTenMon = "Tên môn";
    public const string NhanNamHoc = "Năm học";
    public const string NhanHocKy = "Học kỳ";
    public const string NhanGiangVien = "Giảng viên";

    /// <summary>Cột của file danh sách sinh viên (mẫu nhập, thiết kế mục 6). Cột "Lớp" ghi tên lớp, ví dụ 16DHTH01.</summary>
    public static readonly string[] CotSinhVien = ["MSSV", "Họ tên", "Ngày sinh", "Giới tính", "Email", "SĐT", "Địa chỉ", "Lớp"];

    /// <summary>Tiêu đề cột lớp của mẫu cũ – vẫn đọc được như cột "Lớp".</summary>
    private const string CotLopMauCu = "Mã lớp";

    /// <summary>Cột của bảng điểm lớp học phần (mẫu nhập = file xuất).</summary>
    public static readonly string[] CotBangDiem = ["STT", "MSSV", "Họ tên", "Lớp", "CC", "GK", "CK"];

    // =======================================================================
    // XUẤT
    // =======================================================================

    /// <summary>Bảng điểm lớp học phần (G-03 "Tải mẫu / Xuất bảng điểm", A-11). Cũng chính là file mẫu để nhập điểm.</summary>
    public static byte[] XuatBangDiemLopHocPhan(ThongTinLopHocPhanExcel lhp, IReadOnlyList<DongNhapDiem> ds, BangTenLop bangLop)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BangDiem");
        var cot = new[]
        {
            new CotExcel("STT", 6), new CotExcel("MSSV", 14, DinhDangText), new CotExcel("Họ tên", 28),
            new CotExcel("Lớp", 13), new CotExcel("CC", 9, DinhDangDiem), new CotExcel("GK", 9, DinhDangDiem),
            new CotExcel("CK", 9, DinhDangDiem)
        };
        var dong = GhiTieuDe(ws, "BẢNG ĐIỂM LỚP HỌC PHẦN", cot.Length,
        [
            (NhanMaMon, lhp.MaMon),
            (NhanTenMon, lhp.TenMon),
            (NhanNamHoc, lhp.NamHoc),
            (NhanHocKy, lhp.HocKy.ToString(CultureInfo.InvariantCulture)),
            (NhanGiangVien, $"{lhp.MaGV} - {lhp.TenGV}"),
            ("Trọng số CC/GK/CK", $"{lhp.TrongSo.Cc}/{lhp.TrongSo.Gk}/{lhp.TrongSo.Ck} (%)"),
            ("Ngày xuất", NgayXuat()),
            ("Ghi chú", "Nhập điểm thang 10, tối đa 1 chữ số thập phân (8.5 hoặc 8,5). Ô trống = giữ nguyên điểm cũ. Không sửa phần đầu file và cột MSSV.")
        ]);

        var dongHeader = GhiHeader(ws, dong, cot);
        for (var i = 0; i < ds.Count; i++)
        {
            var r = dongHeader + 1 + i;
            var d = ds[i];
            ws.Cell(r, 1).Value = i + 1;
            ws.Cell(r, 2).Value = d.MaSV;
            ws.Cell(r, 3).Value = d.HoTen;
            ws.Cell(r, 4).Value = bangLop.Ten(d.MaLop);
            GhiSo(ws.Cell(r, 5), d.Diem.Cc);
            GhiSo(ws.Cell(r, 6), d.Diem.Gk);
            GhiSo(ws.Cell(r, 7), d.Diem.Ck);
        }
        KeBang(ws, dongHeader, ds.Count, cot);
        ws.Range(dongHeader + 1, 5, dongHeader + Math.Max(ds.Count, 1), 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        return Luu(wb);
    }

    /// <summary>Báo cáo học lực (A-10, G-04, A-03): bảng từng SV + thống kê số SV theo xếp loại.</summary>
    public static byte[] XuatBaoCaoHocLuc(string tenBaoCao, IReadOnlyList<(string Nhan, string GiaTri)> thongTinLoc,
        bool laTichLuy, IReadOnlyList<BaoCaoHocLucRow> ds, IReadOnlyList<SoLuongXepLoai> thongKe, BangTenLop bangLop)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BaoCaoHocLuc");
        var cot = new[]
        {
            new CotExcel("STT", 6), new CotExcel("MSSV", 14, DinhDangText), new CotExcel("Họ tên", 28),
            new CotExcel("Lớp", 13), new CotExcel(laTichLuy ? "TC tích lũy" : "TC đạt", 11),
            new CotExcel("GPA", 9, DinhDangGpa), new CotExcel("Xếp loại", 16),
            new CotExcel(laTichLuy ? "Số môn nợ" : "Số môn F", 11)
        };
        var dong = GhiTieuDe(ws, tenBaoCao, cot.Length, [.. thongTinLoc, ("Số sinh viên", ds.Count.ToString()), ("Ngày xuất", NgayXuat())]);

        var dongHeader = GhiHeader(ws, dong, cot);
        for (var i = 0; i < ds.Count; i++)
        {
            var r = dongHeader + 1 + i;
            var d = ds[i];
            ws.Cell(r, 1).Value = i + 1;
            ws.Cell(r, 2).Value = d.MaSV;
            ws.Cell(r, 3).Value = d.HoTen;
            ws.Cell(r, 4).Value = bangLop.Ten(d.MaLop);
            ws.Cell(r, 5).Value = d.TinChiTichLuy;
            GhiSo(ws.Cell(r, 6), d.Gpa);
            ws.Cell(r, 7).Value = d.XepLoai;
            ws.Cell(r, 8).Value = d.SoMonNo;
        }
        KeBang(ws, dongHeader, ds.Count, cot);

        // Thống kê theo xếp loại (số đếm do pipeline ThongKeXepLoai trả về)
        var r0 = dongHeader + Math.Max(ds.Count, 1) + 2;
        ws.Cell(r0, 1).Value = "Thống kê theo xếp loại";
        ws.Cell(r0, 1).Style.Font.Bold = true;
        // Bảng thống kê đặt ở cột C–D (cột rộng) cho dễ đọc
        ws.Cell(r0 + 1, 3).Value = "Xếp loại";
        ws.Cell(r0 + 1, 4).Value = "Số SV";
        DinhDangHeader(ws.Range(r0 + 1, 3, r0 + 1, 4));
        for (var i = 0; i < thongKe.Count; i++)
        {
            ws.Cell(r0 + 2 + i, 3).Value = thongKe[i].XepLoai;
            ws.Cell(r0 + 2 + i, 4).Value = thongKe[i].SoSV;
        }
        KeVien(ws.Range(r0 + 1, 3, r0 + 1 + thongKe.Count, 4));
        return Luu(wb);
    }

    /// <summary>Danh sách sinh viên theo bộ lọc của A-04 (kèm GPA / xếp loại tích lũy từ pipeline).</summary>
    public static byte[] XuatDanhSachSinhVien(IReadOnlyList<(string Nhan, string GiaTri)> thongTinLoc,
        IReadOnlyList<(SinhVien SinhVien, BaoCaoHocLucRow? HocLuc)> ds, BangTenLop bangLop)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("DanhSachSinhVien");
        var cot = new[]
        {
            new CotExcel("STT", 6), new CotExcel("MSSV", 14, DinhDangText), new CotExcel("Họ tên", 28),
            new CotExcel("Ngày sinh", 12, DinhDangText), new CotExcel("Giới tính", 10), new CotExcel("Lớp", 13),
            new CotExcel("Email", 28), new CotExcel("SĐT", 13, DinhDangText), new CotExcel("Địa chỉ", 26),
            new CotExcel("Trạng thái", 12), new CotExcel("GPA", 8, DinhDangGpa), new CotExcel("Xếp loại", 15)
        };
        var dong = GhiTieuDe(ws, "DANH SÁCH SINH VIÊN", cot.Length, [.. thongTinLoc, ("Số sinh viên", ds.Count.ToString()), ("Ngày xuất", NgayXuat())]);

        var dongHeader = GhiHeader(ws, dong, cot);
        for (var i = 0; i < ds.Count; i++)
        {
            var r = dongHeader + 1 + i;
            var (sv, hocLuc) = ds[i];
            ws.Cell(r, 1).Value = i + 1;
            ws.Cell(r, 2).Value = sv.MaSV;
            ws.Cell(r, 3).Value = sv.HoTen;
            ws.Cell(r, 4).Value = HienThi.Ngay(sv.NgaySinh);
            ws.Cell(r, 5).Value = sv.GioiTinh;
            ws.Cell(r, 6).Value = bangLop.Ten(sv.MaLop);
            ws.Cell(r, 7).Value = sv.Email ?? "";
            ws.Cell(r, 8).Value = sv.SoDienThoai ?? "";
            ws.Cell(r, 9).Value = sv.DiaChi ?? "";
            ws.Cell(r, 10).Value = sv.TrangThai;
            GhiSo(ws.Cell(r, 11), hocLuc?.Gpa);
            ws.Cell(r, 12).Value = hocLuc?.XepLoai ?? "";
        }
        KeBang(ws, dongHeader, ds.Count, cot);
        return Luu(wb);
    }

    /// <summary>
    /// Bảng điểm cá nhân (S-02, A-06): thông tin SV, mỗi học kỳ một khối (môn + GPA học kỳ), cuối file tổng kết tích lũy.
    /// </summary>
    public static byte[] XuatBangDiemCaNhan(SinhVien sv, string? tenLop, BangDiemViewModel bangDiem)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BangDiem");
        var cot = new[]
        {
            new CotExcel("Mã môn", 10), new CotExcel("Tên môn", 32), new CotExcel("TC", 5),
            new CotExcel("Lần / Hình thức", 16), new CotExcel("CC", 7, DinhDangDiem), new CotExcel("GK", 7, DinhDangDiem),
            new CotExcel("CK", 7, DinhDangDiem), new CotExcel("TK", 7, DinhDangDiem), new CotExcel("Chữ", 6),
            new CotExcel("Hệ 4", 6), new CotExcel("Kết quả", 13), new CotExcel("Tính tích lũy", 12)
        };
        var dong = GhiTieuDe(ws, "BẢNG ĐIỂM CÁ NHÂN", cot.Length,
        [
            ("MSSV", sv.MaSV),
            ("Họ tên", sv.HoTen),
            ("Ngày sinh", HienThi.Ngay(sv.NgaySinh)),
            ("Lớp", tenLop ?? sv.MaLop),
            ("Ngày xuất", NgayXuat())
        ]);
        foreach (var c in Enumerable.Range(1, cot.Length))
            ws.Column(c).Width = cot[c - 1].DoRong;

        // Học kỳ theo thứ tự thời gian (cũ → mới) như bảng điểm giấy
        foreach (var khoi in bangDiem.CacHocKy.AsEnumerable().Reverse())
        {
            var oNhom = GopO(ws, dong, 1, cot.Length, HienThi.HocKy(khoi.NamHoc, khoi.HocKy));
            oNhom.Style.Font.Bold = true;
            oNhom.Style.Font.FontColor = MauTieuDe;
            oNhom.Style.Fill.BackgroundColor = MauNhomHocKy;
            dong++;

            var dongHeader = GhiHeader(ws, dong, cot, datDoRong: false);
            for (var i = 0; i < khoi.CacMon.Count; i++)
            {
                var r = dongHeader + 1 + i;
                var m = khoi.CacMon[i];
                ws.Cell(r, 1).Value = m.MaMon;
                ws.Cell(r, 2).Value = m.TenMon;
                ws.Cell(r, 3).Value = m.SoTinChi;
                ws.Cell(r, 4).Value = $"{m.LanHoc} / {m.HinhThuc}";
                GhiSo(ws.Cell(r, 5), m.Diem.Cc);
                GhiSo(ws.Cell(r, 6), m.Diem.Gk);
                GhiSo(ws.Cell(r, 7), m.Diem.Ck);
                GhiSo(ws.Cell(r, 8), m.DiemTK);
                ws.Cell(r, 9).Value = m.DiemChu ?? "";
                if (m.DiemHe4.HasValue) ws.Cell(r, 10).Value = m.DiemHe4.Value;
                ws.Cell(r, 11).Value = HienThi.KetQua(m.Dat);
                ws.Cell(r, 12).Value = m.TinhTichLuy ? "✓" : "";
            }
            KeBang(ws, dongHeader, khoi.CacMon.Count, cot, datDinhDangCot: false);
            ws.Range(dongHeader + 1, 3, dongHeader + khoi.CacMon.Count, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(dongHeader + 1, 5, dongHeader + khoi.CacMon.Count, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(dongHeader + 1, 12, dongHeader + khoi.CacMon.Count, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            dong = dongHeader + khoi.CacMon.Count + 1;

            // Dòng cuối khối: GPA học kỳ (pipeline GpaHocKy)
            var g = khoi.Gpa;
            var oGpa = GopO(ws, dong, 1, cot.Length, $"GPA học kỳ: {HienThi.Gpa(g?.Gpa)}   ·   TC đăng ký: {g?.TinChiDangKy ?? 0}   ·   TC đạt: {g?.TinChiDat ?? 0}"
                       + (g != null && !string.IsNullOrEmpty(g.XepLoai) ? $"   ·   Xếp loại học kỳ: {g.XepLoai}" : ""));
            oGpa.Style.Font.Italic = true;
            dong += 2;
        }

        // Tổng kết tích lũy (pipeline BaoCaoHocLuc)
        var tk = bangDiem.TongKet;
        ws.Cell(dong, 1).Value = "TỔNG KẾT TÍCH LŨY";
        ws.Cell(dong, 1).Style.Font.Bold = true;
        ws.Cell(dong, 1).Style.Font.FontColor = MauTieuDe;
        dong++;
        foreach (var (nhan, giaTri) in new[]
        {
            ("GPA tích lũy", HienThi.Gpa(tk?.Gpa)),
            ("Tín chỉ tích lũy", (tk?.TinChiTichLuy ?? 0).ToString()),
            ("Xếp loại", tk?.XepLoai ?? "Chưa xếp loại"),
            ("Số môn nợ", (tk?.SoMonNo ?? 0).ToString())
        })
        {
            GhiNhanGiaTri(ws, dong++, nhan, giaTri, cot.Length);
        }

        CaiDatTrangIn(ws, 1);
        return Luu(wb);
    }

    /// <summary>
    /// File mẫu nhập danh sách SV: sheet "DanhSach" chỉ có dòng tiêu đề cột (cột MSSV, ngày sinh, SĐT định dạng Text),
    /// sheet "HuongDan" ghi quy tắc, 1 dòng ví dụ và các lớp hợp lệ (tên lớp).
    /// </summary>
    public static byte[] MauDanhSachSinhVien(IEnumerable<LopHoc> dsLop)
        => XuatFileSinhVien([], dsLop);

    /// <summary>File danh sách SV theo đúng mẫu nhập, có sẵn dữ liệu (dùng cho lệnh tạo file demo).</summary>
    public static byte[] XuatFileSinhVien(IEnumerable<string[]> cacDong, IEnumerable<LopHoc> dsLop)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("DanhSach");
        var doRong = new[] { 14d, 28, 12, 10, 28, 13, 28, 13 };
        for (var c = 0; c < CotSinhVien.Length; c++)
        {
            ws.Cell(1, c + 1).Value = CotSinhVien[c];
            ws.Column(c + 1).Width = doRong[c];
        }
        // MSSV, Ngày sinh, SĐT nhập dạng Text: Excel không tự đổi thành số / ngày, không mất số 0 đầu
        foreach (var c in new[] { 1, 3, 6 })
            ws.Range(2, c, 1000, c).Style.NumberFormat.Format = DinhDangText;
        DinhDangHeader(ws.Range(1, 1, 1, CotSinhVien.Length));

        var r = 2;
        foreach (var dong in cacDong)
        {
            for (var c = 0; c < dong.Length && c < CotSinhVien.Length; c++)
                ws.Cell(r, c + 1).Value = dong[c];
            r++;
        }
        if (r > 2) KeVien(ws.Range(1, 1, r - 1, CotSinhVien.Length));
        ws.SheetView.FreezeRows(1);

        // Sheet hướng dẫn
        var hd = wb.Worksheets.Add("HuongDan");
        var dongHd = GhiTieuDe(hd, "MẪU NHẬP DANH SÁCH SINH VIÊN", CotSinhVien.Length, [("Ngày tạo", NgayXuat())]);
        string[] quyTac =
        [
            "Nhập dữ liệu ở sheet \"DanhSach\", mỗi sinh viên một dòng, giữ nguyên dòng tiêu đề cột.",
            "MSSV: đúng 10 chữ số, chưa có trong hệ thống, không trùng trong file.",
            "Ngày sinh: dạng dd/MM/yyyy (ví dụ 14/03/2007), từ năm 1950 và đủ 15 tuổi.",
            "Giới tính: Nam hoặc Nữ.  Email, SĐT (10 số, bắt đầu bằng 0), Địa chỉ: có thể để trống.",
            "Lớp: ghi tên lớp đã có, ví dụ 16DHTH01 (xem danh sách bên dưới).",
            "Tài khoản đăng nhập được tạo tự động: tên đăng nhập = MSSV, mật khẩu = ngày sinh ddMMyyyy.",
            "Hệ thống kiểm tra toàn bộ file trước: có dòng lỗi thì không ghi dòng nào. File .xlsx, tối đa 5 MB."
        ];
        foreach (var dongQuyTac in quyTac)
        {
            GopO(hd, dongHd, 1, CotSinhVien.Length, "• " + dongQuyTac);
            dongHd++;
        }
        dongHd++;
        hd.Cell(dongHd++, 1).Value = "Dòng ví dụ:";
        for (var c = 0; c < CotSinhVien.Length; c++)
        {
            hd.Cell(dongHd, c + 1).Value = CotSinhVien[c];
            hd.Column(c + 1).Width = doRong[c];
        }
        DinhDangHeader(hd.Range(dongHd, 1, dongHd, CotSinhVien.Length));
        string[] viDu = ["2001259999", "Nguyễn Văn Ví Dụ", "14/03/2007", "Nam", "2001259999@sv.eduscore.vn", "0912345678", "Tân Phú, TP.HCM", "16DHTH01"];
        for (var c = 0; c < viDu.Length; c++)
        {
            hd.Cell(dongHd + 1, c + 1).Style.NumberFormat.Format = DinhDangText;
            hd.Cell(dongHd + 1, c + 1).Value = viDu[c];
        }
        KeVien(hd.Range(dongHd, 1, dongHd + 1, CotSinhVien.Length));
        dongHd += 3;
        hd.Cell(dongHd++, 1).Value = "Lớp hợp lệ:";
        foreach (var lop in dsLop)
        {
            hd.Cell(dongHd, 1).Value = lop.TenLop;
            GopO(hd, dongHd, 2, CotSinhVien.Length, $"{lop.Nganh} – niên khóa {lop.NienKhoa}");
            dongHd++;
        }
        return Luu(wb);
    }

    // =======================================================================
    // ĐỌC
    // =======================================================================

    /// <summary>
    /// Đọc file danh sách SV: tìm dòng tiêu đề (ô "MSSV") trong 10 dòng đầu của sheet "DanhSach" (hoặc sheet đầu),
    /// mỗi dòng sau đó là một SV; dòng trống hoàn toàn bị bỏ qua. Chỉ đọc giá trị thô – kiểm tra ở NhapExcelService.
    /// </summary>
    /// <exception cref="LoiFileExcelException">File không mở được / không đúng mẫu.</exception>
    public static List<DongNhapSinhVien> DocDanhSachSinhVien(Stream stream)
    {
        using var wb = MoFile(stream);
        var ws = wb.Worksheets.FirstOrDefault(w => w.Name == "DanhSach") ?? wb.Worksheet(1);

        var (dongHeader, viTri) = TimHeader(ws, CotSinhVien, 10)
            ?? throw new LoiFileExcelException("Không tìm thấy dòng tiêu đề cột (MSSV, Họ tên, Ngày sinh, ...). Hãy dùng đúng file mẫu.");

        var ketQua = new List<DongNhapSinhVien>();
        var dongCuoi = ws.LastRowUsed()?.RowNumber() ?? dongHeader;
        for (var r = dongHeader + 1; r <= dongCuoi; r++)
        {
            string O(string tenCot) => DocChuoi(ws.Cell(r, viTri[tenCot]));
            var dong = new DongNhapSinhVien
            {
                SoDong = r,
                MaSV = O("MSSV"),
                HoTen = O("Họ tên"),
                NgaySinh = O("Ngày sinh"),
                GioiTinh = O("Giới tính"),
                Email = O("Email"),
                SoDienThoai = O("SĐT"),
                DiaChi = O("Địa chỉ"),
                MaLop = O("Lớp") // tên lớp (hoặc mã lớp) – NhapExcelService quy đổi về maLop
            };
            var trong = new[] { dong.MaSV, dong.HoTen, dong.NgaySinh, dong.GioiTinh, dong.Email, dong.SoDienThoai, dong.DiaChi, dong.MaLop }
                .All(string.IsNullOrEmpty);
            if (!trong) ketQua.Add(dong);
        }
        return ketQua;
    }

    /// <summary>
    /// Đọc file bảng điểm: phần đầu ("Mã môn:", "Năm học:", "Học kỳ:", "Giảng viên:") xác định lớp học phần,
    /// bảng từ dòng có ô "MSSV". Giá trị điểm trả về dạng chuỗi (số đọc theo dấu chấm) để kiểm tra bằng DiemNhap.
    /// </summary>
    /// <exception cref="LoiFileExcelException">File không mở được / thiếu phần đầu / thiếu cột.</exception>
    public static (ThongTinLopHocPhanExcel LopHocPhan, List<DongNhapDiemExcel> Dong) DocBangDiem(Stream stream)
    {
        using var wb = MoFile(stream);
        var ws = wb.Worksheet(1);

        var (dongHeader, viTri) = TimHeader(ws, ["MSSV", "CC", "GK", "CK"], 40)
            ?? throw new LoiFileExcelException("Không tìm thấy bảng điểm (dòng tiêu đề STT | MSSV | Họ tên | Lớp | CC | GK | CK). Hãy dùng file tải từ nút \"Tải mẫu\".");

        // Phần đầu: các dòng "Nhãn:" | giá trị phía trên bảng
        var thongTin = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var r = 1; r < dongHeader; r++)
        {
            var cacO = ws.Row(r).CellsUsed().ToList();
            if (cacO.Count < 2) continue;
            var nhan = DocChuoi(cacO[0]).TrimEnd(':').Trim();
            thongTin.TryAdd(nhan, DocChuoi(cacO[1]));
        }

        string Lay(string nhan) => thongTin.GetValueOrDefault(nhan, "").Trim();
        var maMon = Lay(NhanMaMon).ToUpperInvariant();
        var namHoc = Lay(NhanNamHoc);
        var giangVien = Lay(NhanGiangVien);
        // "GV005 - ThS. Trần Văn Bình" → mã GV là phần trước dấu " - "
        var maGV = giangVien.Split(" - ", 2)[0].Trim().ToUpperInvariant();
        if (maMon.Length == 0 || namHoc.Length == 0 || maGV.Length == 0
            || !int.TryParse(Lay(NhanHocKy), NumberStyles.Integer, CultureInfo.InvariantCulture, out var hocKy))
            throw new LoiFileExcelException("Phần đầu file thiếu hoặc sai thông tin lớp học phần (Mã môn, Năm học, Học kỳ, Giảng viên). Hãy dùng file tải từ nút \"Tải mẫu\" và không sửa phần đầu.");

        var lhp = new ThongTinLopHocPhanExcel { MaMon = maMon, TenMon = Lay(NhanTenMon), NamHoc = namHoc, HocKy = hocKy, MaGV = maGV };

        var dsDong = new List<DongNhapDiemExcel>();
        var dongCuoi = ws.LastRowUsed()?.RowNumber() ?? dongHeader;
        viTri.TryGetValue("Họ tên", out var cotHoTen);
        viTri.TryGetValue("Lớp", out var cotLop);
        for (var r = dongHeader + 1; r <= dongCuoi; r++)
        {
            var dong = new DongNhapDiemExcel
            {
                SoDong = r,
                MaSV = DocChuoi(ws.Cell(r, viTri["MSSV"])),
                HoTen = cotHoTen > 0 ? DocChuoi(ws.Cell(r, cotHoTen)) : "",
                MaLop = cotLop > 0 ? DocChuoi(ws.Cell(r, cotLop)) : "",
                CcNhap = DocChuoi(ws.Cell(r, viTri["CC"])),
                GkNhap = DocChuoi(ws.Cell(r, viTri["GK"])),
                CkNhap = DocChuoi(ws.Cell(r, viTri["CK"]))
            };
            if (dong.MaSV.Length == 0 && dong.CcNhap.Length == 0 && dong.GkNhap.Length == 0 && dong.CkNhap.Length == 0)
                continue; // dòng trống
            dsDong.Add(dong);
        }
        return (lhp, dsDong);
    }

    /// <summary>Đọc lại file vừa tạo để kiểm tra (lệnh tạo file demo): trả về tên sheet và số dòng đã dùng.</summary>
    public static List<(string Sheet, int SoDong)> DocTomTat(Stream stream)
    {
        using var wb = MoFile(stream);
        return wb.Worksheets.Select(w => (w.Name, w.LastRowUsed()?.RowNumber() ?? 0)).ToList();
    }

    // =======================================================================
    // TÊN FILE
    // =======================================================================

    /// <summary>
    /// Tên file không dấu, chỉ gồm chữ, số, '-', '_' (thiết kế mục 6), ví dụ
    /// TenFile("BangDiem", "INT301", "2026-2027", "HK1") → "BangDiem_INT301_2026-2027_HK1.xlsx".
    /// </summary>
    public static string TenFile(params string?[] cacPhan)
    {
        var phan = cacPhan.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => BoDau(p!));
        return string.Join("_", phan) + ".xlsx";
    }

    private static string BoDau(string s)
    {
        var chuan = s.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in chuan)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsAsciiLetterOrDigit(ch) || ch == '-') sb.Append(ch);
            else if (char.IsWhiteSpace(ch) || ch == '_' || ch == '.') sb.Append('-');
        }
        return sb.ToString().Trim('-');
    }

    // =======================================================================
    // HÀM PHỤ
    // =======================================================================

    /// <summary>Một cột của bảng xuất: tiêu đề, độ rộng, định dạng số / Text (null = mặc định).</summary>
    private record CotExcel(string Ten, double DoRong, string? DinhDang = null);

    private static string NgayXuat() => DateTime.Now.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Ghi tiêu đề hệ thống, tên báo cáo, các dòng thông tin; trả về số dòng để ghi tiếp (đã chừa 1 dòng trống).</summary>
    private static int GhiTieuDe(IXLWorksheet ws, string tenBaoCao, int soCot, IEnumerable<(string Nhan, string GiaTri)> thongTin)
    {
        var tieuDe = GopO(ws, 1, 1, soCot, TieuDeHeThong);
        tieuDe.Style.Font.Bold = true;
        tieuDe.Style.Font.FontSize = 14;
        tieuDe.Style.Font.FontColor = MauTieuDe;

        var ten = GopO(ws, 2, 1, soCot, tenBaoCao);
        ten.Style.Font.Bold = true;
        ten.Style.Font.FontSize = 12;

        var dong = 3;
        foreach (var (nhan, giaTri) in thongTin)
            GhiNhanGiaTri(ws, dong++, nhan, giaTri, soCot);
        return dong + 1;
    }

    /// <summary>"Nhãn:" ở cột A–B (gộp), giá trị dạng Text ở cột C đến cột cuối (gộp).</summary>
    private static void GhiNhanGiaTri(IXLWorksheet ws, int dong, string nhan, string giaTri, int soCot)
    {
        GopO(ws, dong, 1, 2, nhan + ":").Style.Font.Bold = true;
        ws.Cell(dong, 3).Style.NumberFormat.Format = DinhDangText;
        var oGiaTri = GopO(ws, dong, 3, Math.Max(3, soCot), giaTri);
        oGiaTri.Style.Alignment.WrapText = giaTri.Length > 60;
        if (giaTri.Length > 60) ws.Row(dong).Height = 30;
    }

    /// <summary>Ghi chữ vào ô đầu rồi gộp các ô trong dòng (chỉ ô đầu có giá trị – đọc lại không bị lặp).</summary>
    private static IXLRange GopO(IXLWorksheet ws, int dong, int cotDau, int cotCuoi, string giaTri)
    {
        ws.Cell(dong, cotDau).Value = giaTri;
        return ws.Range(dong, cotDau, dong, cotCuoi).Merge();
    }

    /// <summary>Ghi dòng tiêu đề cột; đặt độ rộng cột. Trả về số dòng header.</summary>
    private static int GhiHeader(IXLWorksheet ws, int dong, CotExcel[] cot, bool datDoRong = true)
    {
        for (var c = 0; c < cot.Length; c++)
        {
            ws.Cell(dong, c + 1).Value = cot[c].Ten;
            if (datDoRong) ws.Column(c + 1).Width = cot[c].DoRong;
        }
        DinhDangHeader(ws.Range(dong, 1, dong, cot.Length));
        return dong;
    }

    private static void DinhDangHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = MauHeader;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        KeVien(header);
    }

    /// <summary>Kẻ viền bảng, định dạng cột (Text / 0.0 / 0.00), cố định dòng header, cài đặt in ngang.</summary>
    private static void KeBang(IXLWorksheet ws, int dongHeader, int soDong, CotExcel[] cot, bool datDinhDangCot = true)
    {
        var dongCuoi = dongHeader + Math.Max(soDong, 0);
        KeVien(ws.Range(dongHeader, 1, dongCuoi, cot.Length));
        if (soDong > 0)
        {
            ws.Range(dongHeader + 1, 1, dongCuoi, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (datDinhDangCot)
                for (var c = 0; c < cot.Length; c++)
                    if (cot[c].DinhDang != null)
                        ws.Range(dongHeader + 1, c + 1, dongCuoi, c + 1).Style.NumberFormat.Format = cot[c].DinhDang;
        }
        if (datDinhDangCot)
        {
            ws.SheetView.FreezeRows(dongHeader);
            CaiDatTrangIn(ws, dongHeader);
        }
    }

    private static void KeVien(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static void CaiDatTrangIn(IXLWorksheet ws, int dongHeader)
    {
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.SetRowsToRepeatAtTop(dongHeader, dongHeader);
    }

    /// <summary>Điểm / GPA: có giá trị → ô số (định dạng đặt theo cột); null → ô trống.</summary>
    private static void GhiSo(IXLCell o, double? giaTri)
    {
        if (giaTri.HasValue) o.Value = giaTri.Value;
    }

    /// <summary>
    /// Giá trị ô dạng chuỗi: số đọc theo dấu chấm (8.5, 2001250001), ngày → dd/MM/yyyy, còn lại lấy chữ hiển thị.
    /// Nhờ vậy cả ô Text "8,5" lẫn ô số 8.5 đều được kiểm tra bằng cùng một hàm DiemNhap.
    /// </summary>
    private static string DocChuoi(IXLCell o)
    {
        var v = o.Value;
        if (v.IsBlank) return "";
        if (v.IsNumber) return v.GetNumber().ToString(CultureInfo.InvariantCulture);
        if (v.IsDateTime) return v.GetDateTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return o.GetFormattedString().Trim().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Tìm dòng tiêu đề chứa đủ các cột bắt buộc; trả về số dòng và vị trí (tên cột → số cột).</summary>
    private static (int Dong, Dictionary<string, int> ViTri)? TimHeader(IXLWorksheet ws, string[] cotBatBuoc, int soDongTim)
    {
        for (var r = 1; r <= soDongTim; r++)
        {
            var viTri = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var o in ws.Row(r).CellsUsed())
            {
                var ten = DocChuoi(o);
                // Chấp nhận tiêu đề có thêm trọng số, ví dụ "CC (10%)"
                var tenGoc = ten.Split(' ', '(')[0];
                viTri.TryAdd(ten, o.Address.ColumnNumber);
                viTri.TryAdd(tenGoc, o.Address.ColumnNumber);
            }
            // File mẫu cũ có cột "Mã lớp" → coi như cột "Lớp"
            if (!viTri.ContainsKey("Lớp") && viTri.TryGetValue(CotLopMauCu, out var cotLopCu)) viTri["Lớp"] = cotLopCu;
            if (cotBatBuoc.All(viTri.ContainsKey)) return (r, viTri);
        }
        return null;
    }

    private static XLWorkbook MoFile(Stream stream)
    {
        try
        {
            return new XLWorkbook(stream);
        }
        catch (Exception)
        {
            throw new LoiFileExcelException("Không đọc được file. Hãy chọn file Excel .xlsx (không phải .xls, .csv) và không đặt mật khẩu.");
        }
    }

    private static byte[] Luu(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}

/// <summary>Lỗi ở mức cả file (không mở được, sai mẫu) – thông báo đã thân thiện, hiện thẳng cho người dùng.</summary>
public class LoiFileExcelException(string message) : Exception(message);

/// <summary>Một file Excel trả về trình duyệt.</summary>
public record TepExcel(string TenFile, byte[] NoiDung);
