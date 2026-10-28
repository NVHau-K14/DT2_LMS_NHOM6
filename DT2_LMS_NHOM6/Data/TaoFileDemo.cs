using ClosedXML.Excel;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Data;

/// <summary>
/// Lệnh: dotnet run --project DT2_LMS_NHOM6 -- --tao-file-demo
/// Ghi 4 file Excel phục vụ demo 3 (nhập / xuất) vào docs/demo/:
///   sinhvien_hop_le.xlsx    – 5 SV mới hợp lệ, lớp 16DHKHDL01 (MSSV 2001250101–2001250105)
///   sinhvien_loi.xlsx       – MSSV đã tồn tại, MSSV trùng trong file, lớp không tồn tại, ngày sinh sai, ...
///   diem_INT301_hop_le.xlsx – điểm CK cho lớp học phần INT301 HK1 2026-2027 của GV005
///   diem_INT301_loi.xlsx    – điểm 11, chữ "abc", 8.25, MSSV không thuộc lớp học phần
/// File được dựng bằng chính ExcelService (cùng mẫu với nút "Tải mẫu") từ dữ liệu mẫu hiện tại, rồi đọc lại để kiểm tra.
/// Cần dữ liệu mẫu nguyên bản (chạy sau --reset) để file "hợp lệ" thật sự hợp lệ.
/// </summary>
public static class TaoFileDemo
{
    private const string MaGV = "GV005";
    private const string MaMon = "INT301";
    private const string NamHoc = "2026-2027";
    private const int HocKy = 1;

    public static async Task<int> ChayAsync(IServiceProvider services, string thuMucGoc)
    {
        var thuMuc = Path.Combine(thuMucGoc, "docs", "demo");
        Directory.CreateDirectory(thuMuc);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var diem = scope.ServiceProvider.GetRequiredService<DiemService>();
        var lopHoc = scope.ServiceProvider.GetRequiredService<LopHocService>();
        var xuat = scope.ServiceProvider.GetRequiredService<XuatExcelService>();
        var dsLop = await lopHoc.TatCaAsync();

        // ---------- Danh sách sinh viên ----------
        string[][] hopLe =
        [
            ["2001250101", "Lê Hoàng Phúc", "05/01/2007", "Nam", "2001250101@sv.eduscore.vn", "0901234561", "Quận 7, TP.HCM", "16DHKHDL01"],
            ["2001250102", "Trần Ngọc Bảo Trâm", "18/04/2007", "Nữ", "2001250102@sv.eduscore.vn", "0901234562", "Thủ Đức, TP.HCM", "16DHKHDL01"],
            ["2001250103", "Nguyễn Đức Minh", "22/07/2007", "Nam", "", "0901234563", "Biên Hòa, Đồng Nai", "16DHKHDL01"],
            ["2001250104", "Phạm Thị Thanh Hằng", "09/10/2007", "Nữ", "2001250104@sv.eduscore.vn", "", "Dĩ An, Bình Dương", "16DHKHDL01"],
            ["2001250105", "Võ Quốc Khánh", "30/12/2006", "Nam", "2001250105@sv.eduscore.vn", "0901234565", "", "16DHKHDL01"]
        ];
        string[][] coLoi =
        [
            ["2001250106", "Đặng Thu Hà", "11/02/2007", "Nữ", "", "0901234566", "Gò Vấp, TP.HCM", "16DHTH01"],          // hợp lệ
            ["2001250001", "Trùng Mã Có Sẵn", "01/01/2007", "Nam", "", "", "", "16DHTH01"],                           // MSSV đã tồn tại
            ["2001250107", "Lý Gia Bảo", "15/03/2007", "Nam", "", "", "", "16DHXX99"],                               // lớp không tồn tại
            ["2001250108", "Huỳnh Mai Anh", "31/02/2007", "Nữ", "", "", "", "16DHTH02"],                              // ngày sinh sai
            ["2001250109", "Bùi Tấn Lộc", "20/08/2007", "Nam", "", "", "", "16DHATTT01"],                             // trùng trong file
            ["2001250109", "Bùi Tấn Lộc", "20/08/2007", "Nam", "", "", "", "16DHATTT01"],                             // trùng trong file
            ["200125011", "Mã Thiếu Số", "01/06/2007", "Khác", "email-sai", "12345", "", "16DHHTTT01"]               // MSSV 9 số, giới tính, email, SĐT sai
        ];
        await GhiAsync(thuMuc, "sinhvien_hop_le.xlsx", ExcelService.XuatFileSinhVien(hopLe, dsLop));
        await GhiAsync(thuMuc, "sinhvien_loi.xlsx", ExcelService.XuatFileSinhVien(coLoi, dsLop));

        // ---------- Bảng điểm INT301 HK1 2026-2027 của GV005 ----------
        var lhp = await diem.LayLopHocPhanAsync(MaGV, MaMon, NamHoc, HocKy);
        if (lhp == null)
        {
            Console.WriteLine($"Không có lớp học phần {MaMon} {NamHoc} HK{HocKy} của {MaGV} – hãy chạy --reset trước.");
            return 1;
        }
        var ds = (await diem.DanhSachNhapDiemAsync(MaGV, MaMon, NamHoc, HocKy)).DuLieu;

        // Điểm CK cố định (seed 2026): 4.0 – 10.0, bước 0.5; CC, GK giữ nguyên
        var ngauNhien = new Random(2026);
        foreach (var d in ds)
            d.Diem.Ck = ngauNhien.Next(8, 21) / 2.0;
        var thongTin = new ThongTinLopHocPhanExcel
        {
            MaMon = lhp.MaMon,
            TenMon = lhp.TenMon,
            NamHoc = lhp.NamHoc,
            HocKy = lhp.HocKy,
            MaGV = MaGV,
            TenGV = ds[0].GiangVien.HoTen,
            TrongSo = lhp.TrongSo
        };
        var fileHopLe = ExcelService.XuatBangDiemLopHocPhan(thongTin, ds);
        await GhiAsync(thuMuc, "diem_INT301_hop_le.xlsx", fileHopLe);

        // File lỗi: sửa vài ô của file hợp lệ + thêm 1 SV không thuộc lớp học phần
        var dsMaTrongLop = ds.Select(d => d.MaSV).ToHashSet();
        var maNgoaiLop = (await db.SinhVien.Find(FilterDefinition<Models.Documents.SinhVien>.Empty)
            .SortBy(sv => sv.MaSV).Project(sv => sv.MaSV).ToListAsync())
            .First(ma => !dsMaTrongLop.Contains(ma));
        using (var wb = new XLWorkbook(new MemoryStream(fileHopLe)))
        {
            var ws = wb.Worksheet(1);
            var dongHeader = ws.Column(2).CellsUsed().First(o => o.GetString() == "MSSV").Address.RowNumber;
            ws.Cell(dongHeader + 1, 7).Value = 11;         // CK = 11 (ngoài 0–10)
            ws.Cell(dongHeader + 2, 7).Value = "abc";      // CK không phải số
            ws.Cell(dongHeader + 3, 6).Value = 8.25;       // GK quá 1 chữ số thập phân
            var dongMoi = dongHeader + ds.Count + 1;
            ws.Cell(dongMoi, 1).Value = ds.Count + 1;
            ws.Cell(dongMoi, 2).Value = maNgoaiLop;        // MSSV không thuộc lớp học phần
            ws.Cell(dongMoi, 3).Value = "Sinh viên ngoài lớp học phần";
            ws.Cell(dongMoi, 7).Value = 7.5;
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            await GhiAsync(thuMuc, "diem_INT301_loi.xlsx", ms.ToArray());
        }

        // ---------- Đọc lại các file xuất bằng ClosedXML để chắc chắn mở được ----------
        Console.WriteLine("Đọc lại file xuất (ClosedXML):");
        var cacFileXuat = new List<TepExcel?>
        {
            await xuat.BangDiemLopHocPhanAsync(MaGV, MaMon, NamHoc, HocKy),
            await xuat.BangDiemCaNhanAsync("2001250001"),
            await xuat.DanhSachSinhVienAsync(null, null, null),
            await xuat.MauSinhVienAsync(),
            await xuat.BaoCaoHocLucAsync(Pipelines.BoLocSinhVien.TatCa(), Pipelines.PhamViBaoCao.TichLuy(),
                [("Lớp", "Tất cả lớp"), ("Phạm vi", "Tích lũy")], "TatCa", "TichLuy")
        };
        foreach (var tep in cacFileXuat.OfType<TepExcel>())
        {
            var tomTat = ExcelService.DocTomTat(new MemoryStream(tep.NoiDung));
            Console.WriteLine($"  {tep.TenFile}: " + string.Join(", ", tomTat.Select(t => $"{t.Sheet} {t.SoDong} dòng")));
        }
        Console.WriteLine($"Đã tạo 4 file demo trong {Path.GetRelativePath(thuMucGoc, thuMuc)}.");
        return 0;
    }

    private static async Task GhiAsync(string thuMuc, string ten, byte[] noiDung)
    {
        var duongDan = Path.Combine(thuMuc, ten);
        await File.WriteAllBytesAsync(duongDan, noiDung);
        var tomTat = ExcelService.DocTomTat(new MemoryStream(noiDung));
        Console.WriteLine($"  Đã ghi docs/demo/{ten} ({noiDung.Length / 1024.0:0.0} KB; " + string.Join(", ", tomTat.Select(t => $"{t.Sheet} {t.SoDong} dòng")) + ")");
    }
}
