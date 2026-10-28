using System.Security.Claims;
using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

public enum KetQuaDangNhap
{
    ThanhCong,
    /// <summary>Sai tên đăng nhập HOẶC sai mật khẩu – không phân biệt để không lộ thông tin.</summary>
    SaiThongTin,
    /// <summary>Đúng mật khẩu nhưng tài khoản đang "Khóa".</summary>
    BiKhoa
}

/// <summary>
/// Nghiệp vụ tài khoản trên collection taikhoan: đăng nhập (BCrypt), đổi mật khẩu.
/// </summary>
public class AccountService
{
    private readonly MongoContext _db;

    public AccountService(MongoContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Kiểm tra tên đăng nhập + mật khẩu. Thành công thì cập nhật lanDangNhapCuoi.
    /// Trạng thái "Khóa" chỉ được báo khi mật khẩu đúng (người đoán mò không biết tài khoản có tồn tại).
    /// </summary>
    public async Task<(KetQuaDangNhap KetQua, TaiKhoan? TaiKhoan)> DangNhapAsync(string tenDangNhap, string matKhau)
    {
        var taiKhoan = await _db.TaiKhoan
            .Find(tk => tk.TenDangNhap == tenDangNhap.Trim())
            .FirstOrDefaultAsync();

        if (taiKhoan == null || !BCrypt.Net.BCrypt.Verify(matKhau, taiKhoan.MatKhauHash))
            return (KetQuaDangNhap.SaiThongTin, null);

        if (taiKhoan.TrangThai == HangSo.TrangThaiTaiKhoan.Khoa)
            return (KetQuaDangNhap.BiKhoa, null);

        // db.taikhoan.updateOne({ _id: ... }, { $set: { lanDangNhapCuoi: <now> } })
        var thoiDiem = DateTime.UtcNow;
        await _db.TaiKhoan.UpdateOneAsync(
            tk => tk.Id == taiKhoan.Id,
            Builders<TaiKhoan>.Update.Set(tk => tk.LanDangNhapCuoi, thoiDiem));
        taiKhoan.LanDangNhapCuoi = thoiDiem;

        return (KetQuaDangNhap.ThanhCong, taiKhoan);
    }

    /// <summary>
    /// Dựng ClaimsPrincipal lưu vào cookie: Name = hoTen, NameIdentifier = tenDangNhap,
    /// Role = vaiTro, MaThamChieu = maSV/maGV/"admin".
    /// </summary>
    public ClaimsPrincipal TaoClaimsPrincipal(TaiKhoan taiKhoan)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, taiKhoan.HoTen),
            new(ClaimTypes.NameIdentifier, taiKhoan.TenDangNhap),
            new(ClaimTypes.Role, taiKhoan.VaiTro),
            new(ClaimsPrincipalExtensions.ClaimMaThamChieu, taiKhoan.MaThamChieu)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Đổi mật khẩu của tài khoản đang đăng nhập. Trả về false nếu mật khẩu hiện tại sai.
    /// </summary>
    public async Task<bool> DoiMatKhauAsync(string tenDangNhap, string matKhauCu, string matKhauMoi)
    {
        var taiKhoan = await _db.TaiKhoan
            .Find(tk => tk.TenDangNhap == tenDangNhap)
            .FirstOrDefaultAsync();

        if (taiKhoan == null || !BCrypt.Net.BCrypt.Verify(matKhauCu, taiKhoan.MatKhauHash))
            return false;

        // db.taikhoan.updateOne({ tenDangNhap: ... }, { $set: { matKhauHash: "$2a$11$..." } })
        var hashMoi = BCrypt.Net.BCrypt.HashPassword(matKhauMoi); // work factor mặc định 11
        await _db.TaiKhoan.UpdateOneAsync(
            tk => tk.Id == taiKhoan.Id,
            Builders<TaiKhoan>.Update.Set(tk => tk.MatKhauHash, hashMoi));
        return true;
    }
}
