namespace DT2_LMS_NHOM6.Data;

/// <summary>
/// Bảng dữ liệu tĩnh cho Seeder – chép đúng thiết kế mục 7.1, 7.2, 7.3
/// cùng danh sách họ / tên đệm / tên / địa chỉ để sinh hồ sơ tự nhiên.
/// </summary>
public static class DuLieuMau
{
    public const int HatGiongNgauNhien = 2026;
    public const string Khoa = "Khoa Công nghệ thông tin";
    public const string NienKhoa = "2025-2029";
    public const int SoSinhVienMoiLop = 20;

    // ---------- Học kỳ dùng trong dữ liệu mẫu (thiết kế 7.4: ngày capNhatLuc) ----------
    public record HocKyMau(string NamHoc, int HocKy, DateTime CapNhatLuc);

    public static readonly HocKyMau HK1_2526 = new("2025-2026", 1, new DateTime(2026, 1, 15, 3, 0, 0, DateTimeKind.Utc));
    public static readonly HocKyMau HK2_2526 = new("2025-2026", 2, new DateTime(2026, 6, 10, 3, 0, 0, DateTimeKind.Utc));
    public static readonly HocKyMau He_2526 = new("2025-2026", 3, new DateTime(2026, 8, 20, 3, 0, 0, DateTimeKind.Utc));
    public static readonly HocKyMau HK1_2627 = new("2026-2027", 1, new DateTime(2026, 10, 20, 3, 0, 0, DateTimeKind.Utc));

    // ---------- 7.1 Lớp ----------
    // maLop là mã nội bộ (khóa liên kết sinhvien.maLop), tenLop là tên hiển thị cho người dùng.
    // Giữ nguyên thứ tự mảng: Seeder duyệt lớp theo thứ tự này nên dữ liệu ngẫu nhiên không đổi.
    public record LopMau(string MaLop, string TenLop, string Nganh, string MaCVHT);

    public static readonly LopMau[] Lop =
    {
        new("CNTT2501", "16DHTH01",   "Công nghệ thông tin", "GV001"),
        new("CNTT2502", "16DHTH02",   "Công nghệ thông tin", "GV002"),
        new("ATTT2501", "16DHATTT01", "An toàn thông tin",   "GV005"),
        new("HTTT2501", "16DHHTTT01", "Hệ thống thông tin",  "GV007"),
        new("KHDL2501", "16DHKHDL01", "Khoa học dữ liệu",    "GV003"),
    };

    /// <summary>Lớp bắt buộc có INT301 ở HK1 2026-2027 (demo nhập điểm của GV005) – lớp 16DHTH01, 16DHTH02.</summary>
    public static readonly string[] LopBatBuocINT301 = { "CNTT2501", "CNTT2502" };

    /// <summary>Lớp của 5 SV kiểm thử (mục 7.5) – lớp 16DHTH01.</summary>
    public const string LopKiemThu = "CNTT2501";

    // ---------- 7.2 Giảng viên ----------
    public record GiangVienMau(string MaGV, string HoTen, string HocVi, string Email, string SoDienThoai);

    public static readonly GiangVienMau[] GiangVien =
    {
        new("GV001", "Nguyễn Văn An",  "ThS.", "annv@eduscore.vn",   "0987654321"),
        new("GV002", "Lê Quốc Cường",  "TS.",  "cuonglq@eduscore.vn", "0903112233"),
        new("GV003", "Phạm Thị Hồng",  "TS.",  "hongpt@eduscore.vn",  "0912445566"),
        new("GV004", "Võ Minh Tuấn",   "ThS.", "tuanvm@eduscore.vn",  "0938778899"),
        new("GV005", "Trần Văn Bình",  "ThS.", "binhtv@eduscore.vn",  "0909223344"),
        new("GV006", "Đặng Thu Hà",    "ThS.", "hadt@eduscore.vn",    "0976556677"),
        new("GV007", "Huỳnh Gia Bảo",  "ThS.", "baohg@eduscore.vn",   "0918990011"),
        new("GV008", "Ngô Thị Lan",    "CN.",  "lannt@eduscore.vn",   "0965334455"),
    };

    // ---------- 7.3 Môn học (kèm giảng viên dạy theo 7.2 và nhóm học kỳ dùng) ----------
    public enum NhomHocKy { HK1_2526, HK2_2526, He_2526, HK1_2627 }

    public record MonMau(string MaMon, string TenMon, int SoTinChi, int Cc, int Gk, int Ck, string MaGV, NhomHocKy Nhom);

    public static readonly MonMau[] MonHoc =
    {
        new("INT101", "Nhập môn lập trình",             3, 10, 30, 60, "GV001", NhomHocKy.HK1_2526),
        new("MAT101", "Toán cao cấp A1",                3, 10, 30, 60, "GV003", NhomHocKy.HK1_2526),
        new("POL101", "Triết học Mác - Lênin",          3, 10, 30, 60, "GV004", NhomHocKy.HK1_2526),
        new("ENG101", "Tiếng Anh 1",                    2, 20, 30, 50, "GV006", NhomHocKy.HK1_2526),
        new("INT102", "Kỹ thuật lập trình",             3, 10, 30, 60, "GV001", NhomHocKy.HK2_2526),
        new("MAT102", "Toán rời rạc",                   3, 10, 30, 60, "GV002", NhomHocKy.HK2_2526),
        new("POL102", "Kinh tế chính trị Mác - Lênin",  2, 10, 30, 60, "GV004", NhomHocKy.HK2_2526),
        new("ENG102", "Tiếng Anh 2",                    2, 20, 30, 50, "GV006", NhomHocKy.HK2_2526),
        new("LAW101", "Pháp luật đại cương",            2, 10, 30, 60, "GV004", NhomHocKy.He_2526),
        new("SKL101", "Kỹ năng mềm",                    2, 20, 30, 50, "GV008", NhomHocKy.He_2526),
        new("INT201", "Cấu trúc dữ liệu và giải thuật", 4, 10, 30, 60, "GV002", NhomHocKy.HK1_2627),
        new("INT202", "Cơ sở dữ liệu",                  3, 10, 40, 50, "GV005", NhomHocKy.HK1_2627),
        new("INT203", "Mạng máy tính",                  3, 10, 30, 60, "GV007", NhomHocKy.HK1_2627),
        new("INT301", "Cơ sở dữ liệu NoSQL",            3, 10, 30, 60, "GV005", NhomHocKy.HK1_2627),
    };

    // ---------- Họ tên tiếng Việt ----------
    /// <summary>Họ – lặp lại họ phổ biến để tần suất tự nhiên hơn.</summary>
    public static readonly string[] Ho =
    {
        "Nguyễn", "Nguyễn", "Nguyễn", "Nguyễn", "Trần", "Trần", "Lê", "Lê", "Phạm", "Phạm",
        "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô",
        "Dương", "Lý", "Trương", "Đinh", "Lâm", "Mai", "Tô", "Cao", "Châu", "Tạ"
    };

    public static readonly string[] TenDemNam =
    {
        "Văn", "Văn", "Hoàng", "Minh", "Quốc", "Đức", "Gia", "Thanh", "Hữu", "Công",
        "Anh", "Tuấn", "Nhật", "Trọng", "Đình", "Bảo", "Thành", "Minh Đức", "Hoàng Gia", "Quang"
    };

    public static readonly string[] TenDemNu =
    {
        "Thị", "Thị", "Ngọc", "Thu", "Thanh", "Minh", "Bảo", "Khánh", "Phương", "Mai",
        "Kim", "Thị Thu", "Thị Minh", "Ngọc Bảo", "Hoàng", "Diệu", "Thảo", "Quỳnh", "Thị Ngọc", "Hà"
    };

    public static readonly string[] TenNam =
    {
        "An", "Bảo", "Cường", "Dũng", "Duy", "Đạt", "Hải", "Hiếu", "Hùng", "Huy",
        "Khang", "Khoa", "Kiên", "Long", "Lộc", "Nam", "Nghĩa", "Nhân", "Phát", "Phong",
        "Phúc", "Quân", "Sơn", "Tài", "Thắng", "Thịnh", "Toàn", "Trí", "Trung", "Tuấn",
        "Việt", "Vinh", "Vũ", "Khải", "Hưng", "Tâm", "Thái", "Danh", "Kiệt", "Lâm"
    };

    public static readonly string[] TenNu =
    {
        "Anh", "Châu", "Diệp", "Dung", "Giang", "Hà", "Hân", "Hằng", "Hoa", "Hương",
        "Lan", "Linh", "Ly", "Mai", "My", "Nga", "Ngân", "Nhi", "Nhung", "Oanh",
        "Phương", "Quyên", "Tâm", "Thảo", "Thư", "Trang", "Trinh", "Uyên", "Vy", "Yến"
    };

    public static readonly string[] DiaChi =
    {
        "Quận 1, TP.HCM", "Quận 3, TP.HCM", "Quận 5, TP.HCM", "Quận 7, TP.HCM", "Quận 10, TP.HCM",
        "Quận 12, TP.HCM", "Tân Phú, TP.HCM", "Tân Bình, TP.HCM", "Bình Tân, TP.HCM", "Bình Thạnh, TP.HCM",
        "Gò Vấp, TP.HCM", "Phú Nhuận, TP.HCM", "Thủ Đức, TP.HCM", "Hóc Môn, TP.HCM", "Củ Chi, TP.HCM",
        "Bình Chánh, TP.HCM", "Nhà Bè, TP.HCM", "Dĩ An, Bình Dương", "Thủ Dầu Một, Bình Dương",
        "Biên Hòa, Đồng Nai", "Long Thành, Đồng Nai", "Tân An, Long An", "Bến Lức, Long An",
        "Vũng Tàu, Bà Rịa - Vũng Tàu", "Tây Ninh, Tây Ninh"
    };

    public static readonly string[] DauSoDienThoai =
    {
        "090", "091", "093", "094", "097", "098", "032", "033", "035", "038", "070", "077", "079", "086", "089"
    };
}
