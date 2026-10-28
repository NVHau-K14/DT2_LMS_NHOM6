using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Quản lý tài khoản phía Admin (A3, A4, A9): tạo / xóa tài khoản đi kèm SV, GV;
/// reset mật khẩu về mặc định; khóa / mở khóa; đồng bộ họ tên.
/// Mật khẩu mặc định (thiết kế 4.5): GV = 123456, SV = ngày sinh ddMMyyyy.
/// </summary>
public class TaiKhoanService
{
    public const string MatKhauMacDinhGiangVien = "123456";

    private readonly MongoContext _db;

    public TaiKhoanService(MongoContext db)
    {
        _db = db;
    }

    /// <summary>Mật khẩu mặc định của SV: ngày sinh dạng ddMMyyyy (ví dụ 14032007).</summary>
    public static string MatKhauMacDinhSinhVien(DateTime ngaySinh) => ngaySinh.ToString("ddMMyyyy");

    public Task<bool> TonTaiAsync(string tenDangNhap)
        => _db.TaiKhoan.Find(tk => tk.TenDangNhap == tenDangNhap).AnyAsync();

    /// <summary>Tài khoản theo vai trò + danh sách mã (dùng index ix_vaiTro_maThamChieu), trả về từ điển mã → tài khoản.</summary>
    public async Task<Dictionary<string, TaiKhoan>> LayTheoMaAsync(string vaiTro, IEnumerable<string> dsMa)
    {
        // { vaiTro: "...", maThamChieu: { $in: [...] } }
        var filter = Builders<TaiKhoan>.Filter.Eq(tk => tk.VaiTro, vaiTro)
                   & Builders<TaiKhoan>.Filter.In(tk => tk.MaThamChieu, dsMa);
        var ds = await _db.TaiKhoan.Find(filter).ToListAsync();
        return ds.ToDictionary(tk => tk.MaThamChieu);
    }

    public Task<TaiKhoan?> LayAsync(string vaiTro, string ma)
        => _db.TaiKhoan.Find(tk => tk.VaiTro == vaiTro && tk.MaThamChieu == ma).FirstOrDefaultAsync()!;

    /// <summary>
    /// db.taikhoan.insertOne({ tenDangNhap: ma, vaiTro, maThamChieu: ma, matKhauHash, trangThai: "Hoạt động", ... })
    /// Trả về document đã chèn (để in lệnh).
    /// </summary>
    public async Task<TaiKhoan> TaoAsync(string vaiTro, string ma, string hoTen, string matKhau)
    {
        var taiKhoan = new TaiKhoan
        {
            TenDangNhap = ma,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(matKhau),
            VaiTro = vaiTro,
            MaThamChieu = ma,
            HoTen = hoTen,
            TrangThai = HangSo.TrangThaiTaiKhoan.HoatDong,
            LanDangNhapCuoi = null,
            NgayTao = DateTime.UtcNow
        };
        await _db.TaiKhoan.InsertOneAsync(taiKhoan);
        return taiKhoan;
    }

    /// <summary>Có tài khoản nào dùng một trong các tên đăng nhập này không: { tenDangNhap: { $in: [...] } }</summary>
    public async Task<HashSet<string>> TenDangNhapDaDungAsync(IEnumerable<string> dsTen)
    {
        var ds = await _db.TaiKhoan.Find(Builders<TaiKhoan>.Filter.In(tk => tk.TenDangNhap, dsTen))
            .Project(tk => tk.TenDangNhap).ToListAsync();
        return ds.ToHashSet();
    }

    /// <summary>
    /// Tạo nhiều tài khoản một lần (nhập Excel): db.taikhoan.insertMany([...]).
    /// Lỗi giữa chừng → xóa các tài khoản đã chèn (theo _id) rồi ném lại lỗi để bên gọi hoàn tác tiếp.
    /// Trả về các document đã chèn (để in lệnh cho toast).
    /// </summary>
    public async Task<List<TaiKhoan>> TaoNhieuAsync(string vaiTro, IEnumerable<(string Ma, string HoTen, string MatKhau)> ds)
    {
        var bayGio = DateTime.UtcNow;
        var cacTaiKhoan = ds.Select(t => new TaiKhoan
        {
            Id = ObjectId.GenerateNewId(),
            TenDangNhap = t.Ma,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(t.MatKhau),
            VaiTro = vaiTro,
            MaThamChieu = t.Ma,
            HoTen = t.HoTen,
            TrangThai = HangSo.TrangThaiTaiKhoan.HoatDong,
            LanDangNhapCuoi = null,
            NgayTao = bayGio
        }).ToList();
        try
        {
            await _db.TaiKhoan.InsertManyAsync(cacTaiKhoan, new InsertManyOptions { IsOrdered = true });
        }
        catch (MongoException)
        {
            var dsId = cacTaiKhoan.Select(tk => tk.Id).ToList();
            await _db.TaiKhoan.DeleteManyAsync(Builders<TaiKhoan>.Filter.In(tk => tk.Id, dsId));
            throw;
        }
        return cacTaiKhoan;
    }

    /// <summary>db.taikhoan.deleteOne({ vaiTro, maThamChieu: ma })</summary>
    public async Task<long> XoaAsync(string vaiTro, string ma)
    {
        var kq = await _db.TaiKhoan.DeleteOneAsync(tk => tk.VaiTro == vaiTro && tk.MaThamChieu == ma);
        return kq.DeletedCount;
    }

    /// <summary>
    /// Đồng bộ họ tên hiển thị của tài khoản (SV đổi họ tên, GV đổi họ tên/học vị).
    /// Trả về chuỗi lệnh mongosh để ghép vào toast lan truyền.
    /// </summary>
    public async Task<string> CapNhatHoTenAsync(string vaiTro, string ma, string hoTen)
    {
        // db.taikhoan.updateOne({ vaiTro: "...", maThamChieu: "..." }, { $set: { hoTen: "..." } })
        var filter = new BsonDocument { { "vaiTro", vaiTro }, { "maThamChieu", ma } };
        var update = new BsonDocument("$set", new BsonDocument("hoTen", hoTen));
        var kq = await _db.Database.GetCollection<BsonDocument>(MongoContext.TenTaiKhoan).UpdateOneAsync(filter, update);
        return MongoCommandFormatter.UpdateOne(MongoContext.TenTaiKhoan, filter, update, ketQua: kq);
    }

    /// <summary>Đặt lại mật khẩu về mặc định. false = không tìm thấy tài khoản.</summary>
    public async Task<bool> ResetMatKhauAsync(string vaiTro, string ma, string matKhauMacDinh)
    {
        // db.taikhoan.updateOne({ vaiTro, maThamChieu }, { $set: { matKhauHash: "$2a$11$..." } })
        var kq = await _db.TaiKhoan.UpdateOneAsync(
            tk => tk.VaiTro == vaiTro && tk.MaThamChieu == ma,
            Builders<TaiKhoan>.Update.Set(tk => tk.MatKhauHash, BCrypt.Net.BCrypt.HashPassword(matKhauMacDinh)));
        return kq.MatchedCount > 0;
    }

    /// <summary>Đảo trạng thái Hoạt động ↔ Khóa. Trả về trạng thái mới; null = không tìm thấy tài khoản.</summary>
    public async Task<string?> DoiTrangThaiAsync(string vaiTro, string ma)
    {
        var taiKhoan = await LayAsync(vaiTro, ma);
        if (taiKhoan == null) return null;

        var trangThaiMoi = taiKhoan.TrangThai == HangSo.TrangThaiTaiKhoan.Khoa
            ? HangSo.TrangThaiTaiKhoan.HoatDong
            : HangSo.TrangThaiTaiKhoan.Khoa;

        // db.taikhoan.updateOne({ _id }, { $set: { trangThai: "Khóa" | "Hoạt động" } })
        await _db.TaiKhoan.UpdateOneAsync(
            tk => tk.Id == taiKhoan.Id,
            Builders<TaiKhoan>.Update.Set(tk => tk.TrangThai, trangThaiMoi));
        return trangThaiMoi;
    }
}
