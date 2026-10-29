namespace DT2_LMS_NHOM6.Data;

/// <summary>
/// Bảng dữ liệu tĩnh cho Seeder – chép đúng thiết kế mục 7.1, 7.2, 7.3
/// cùng danh sách họ / tên đệm / tên / địa chỉ để sinh hồ sơ tự nhiên.
/// </summary>
public static class DuLieuMau
{
    public const int HatGiongNgauNhien = 2026;
    /// <summary>Hạt giống riêng cho môn mở rộng – chạy sau toàn bộ dữ liệu cũ nên không đổi chuỗi Random(2026).</summary>
    public const int HatGiongMonMoRong = 2027;
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
        new("GV009", "Trương Minh Khoa", "TS.",  "khoatm@eduscore.vn", "0907445566"),
        new("GV010", "Lý Thu Trang",     "ThS.", "tranglt@eduscore.vn", "0934667788"),
        new("GV011", "Đinh Quốc Huy",    "ThS.", "huydq@eduscore.vn",   "0971223344"),
    };

    // ---------- 7.3 Môn học (kèm giảng viên dạy theo 7.2 và nhóm học kỳ dùng) ----------
    public enum NhomHocKy { HK1_2526, HK2_2526, He_2526, HK1_2627 }

    /// <summary>ChiLop = null: mọi lớp học; khác null: chỉ các lớp (maLop) này học (môn chuyên ngành).</summary>
    public record MonMau(string MaMon, string TenMon, int SoTinChi, int Cc, int Gk, int Ck, string MaGV, NhomHocKy Nhom,
                         string[]? ChiLop = null);

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

    // ---------- 7.3 Môn mở rộng: mọi SV (trừ 5 SV kiểm thử) của lớp được chỉ định đều học ----------
    // Tách khỏi mảng MonHoc ở trên để việc chọn môn ngẫu nhiên của 14 môn cũ không đổi.
    public static readonly MonMau[] MonHocMoRong =
    {
        new("MAT103", "Đại số tuyến tính",                                     3, 10, 30, 60, "GV003", NhomHocKy.HK1_2526),
        new("INT104", "Kỹ năng ứng dụng công nghệ thông tin",                  2, 10, 30, 60, "GV008", NhomHocKy.HK1_2526),
        new("INT105", "Nguyên lý ngôn ngữ lập trình",                          3, 10, 30, 60, "GV009", NhomHocKy.HK1_2526),
        new("INT106", "Thực hành Nguyên lý ngôn ngữ lập trình",                1, 20, 30, 50, "GV009", NhomHocKy.HK1_2526),
        new("MAT104", "Xác suất và thống kê trong sản xuất, công nghệ, kỹ thuật", 3, 10, 30, 60, "GV003", NhomHocKy.HK2_2526),
        new("INT107", "Thực hành kỹ thuật lập trình",                          1, 20, 30, 50, "GV001", NhomHocKy.HK2_2526),
        new("INT108", "Kiến trúc máy tính",                                    3, 10, 30, 60, "GV010", NhomHocKy.HK2_2526),
        new("POL103", "Chủ nghĩa xã hội khoa học",                             2, 10, 30, 60, "GV004", NhomHocKy.HK2_2526),
        new("SKL102", "Đổi mới sáng tạo và khởi nghiệp",                       2, 20, 30, 50, "GV008", NhomHocKy.He_2526),
        new("SKL103", "Phương pháp nghiên cứu khoa học",                       2, 20, 30, 50, "GV011", NhomHocKy.He_2526),
        new("INT204", "Thực hành cấu trúc dữ liệu và giải thuật",              1, 20, 30, 50, "GV002", NhomHocKy.HK1_2627),
        new("INT205", "Thực hành mạng máy tính",                               1, 20, 30, 50, "GV007", NhomHocKy.HK1_2627),
        new("INT206", "Hệ điều hành",                                          3, 10, 30, 60, "GV010", NhomHocKy.HK1_2627),
        new("POL104", "Tư tưởng Hồ Chí Minh",                                  2, 10, 30, 60, "GV004", NhomHocKy.HK1_2627),
        new("ENG103", "Anh văn 3",                                             2, 20, 30, 50, "GV006", NhomHocKy.HK1_2627),
        new("INT207", "Lập trình hướng đối tượng",                             3, 10, 30, 60, "GV001", NhomHocKy.HK1_2627, new[] { "CNTT2501", "CNTT2502" }),
        new("INT208", "Bảo mật máy tính",                                      3, 10, 30, 60, "GV011", NhomHocKy.HK1_2627, new[] { "ATTT2501" }),
        new("INT209", "Phân tích thiết kế hệ thống",                           3, 10, 30, 60, "GV007", NhomHocKy.HK1_2627, new[] { "HTTT2501" }),
        new("INT210", "Nhập môn Big Data",                                     3, 10, 30, 60, "GV009", NhomHocKy.HK1_2627, new[] { "KHDL2501" }),
    };

    // ---------- 7.3 Môn chỉ có trong danh mục (chưa mở, không ai ghi danh) ----------
    public record MonDanhMucMau(string MaMon, string TenMon, int SoTinChi, int Cc, int Gk, int Ck);

    public static readonly MonDanhMucMau[] MonHocChuaMo =
    {
        new("INT302", "Hệ quản trị cơ sở dữ liệu",  3, 10, 30, 60),
        new("INT303", "Bảo mật cơ sở dữ liệu",      3, 10, 30, 60),
        new("INT304", "Công nghệ phần mềm",         3, 10, 30, 60),
        new("INT305", "Trí tuệ nhân tạo",           3, 10, 30, 60),
        new("INT306", "Lập trình web",              3, 10, 30, 60),
        new("INT307", "Lập trình mạng",             3, 10, 30, 60),
        new("INT308", "Lập trình di động",          3, 10, 30, 60),
        new("INT309", "Khai phá dữ liệu",           3, 10, 30, 60),
        new("INT310", "Deep learning",              3, 10, 30, 60),
        new("INT311", "Kiểm định phần mềm",         3, 10, 30, 60),
        new("INT312", "Internet of Things",         3, 10, 30, 60),
        new("INT401", "Thực tập nghề nghiệp",       3, 0, 0, 100),
        new("INT402", "Khóa luận cử nhân",          10, 0, 0, 100),
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
