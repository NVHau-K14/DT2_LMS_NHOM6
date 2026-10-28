namespace DT2_LMS_NHOM6.Models;

/// <summary>
/// Các giá trị enum dạng chuỗi lưu trong MongoDB (tiếng Việt có dấu, thiết kế mục 4).
/// Dùng chung cho model, validator, seeder và giao diện để không gõ sai chuỗi.
/// </summary>
public static class HangSo
{
    public static class VaiTro
    {
        public const string Admin = "Admin";
        public const string GiangVien = "GiangVien";
        public const string SinhVien = "SinhVien";
    }

    public static class TrangThaiSinhVien
    {
        public const string DangHoc = "Đang học";
        public const string BaoLuu = "Bảo lưu";
        public const string ThoiHoc = "Thôi học";
    }

    public static class TrangThaiTaiKhoan
    {
        public const string HoatDong = "Hoạt động";
        public const string Khoa = "Khóa";
    }

    public static class HinhThuc
    {
        public const string HocMoi = "Học mới";
        public const string HocLai = "Học lại";
        public const string CaiThien = "Cải thiện";
    }

    public static class GioiTinh
    {
        public const string Nam = "Nam";
        public const string Nu = "Nữ";
    }

    public static class HocVi
    {
        public const string CuNhan = "CN.";
        public const string ThacSi = "ThS.";
        public const string TienSi = "TS.";
        public const string PhoGiaoSuTienSi = "PGS.TS.";
    }

    /// <summary>
    /// 7 mức xếp loại học lực theo thứ tự hiển thị (thiết kế 3.4). Chuỗi do pipeline trả về –
    /// danh sách này chỉ dùng để XẾP THẺ / CỘT BIỂU ĐỒ đủ 7 mức (mức không có SV vẫn hiện số 0).
    /// </summary>
    public static readonly string[] CacXepLoai = ["Xuất sắc", "Giỏi", "Khá", "Trung bình", "Yếu", "Kém", "Chưa xếp loại"];

    /// <summary>Tên đăng nhập / mã tham chiếu của tài khoản quản trị.</summary>
    public const string MaAdmin = "admin";
}
