using System.Text.RegularExpressions;
using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.ViewModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// CRUD hồ sơ sinh viên (A3). Thêm SV tự tạo tài khoản; không sửa MSSV; xóa SV xóa luôn tài khoản.
/// MongoDB standalone không có transaction → ghi tuần tự 2 collection, bước sau lỗi thì hoàn tác bước trước (mục 4.8).
/// </summary>
public class SinhVienService
{
    private readonly MongoContext _db;
    private readonly TaiKhoanService _taiKhoan;

    public SinhVienService(MongoContext db, TaiKhoanService taiKhoan)
    {
        _db = db;
        _taiKhoan = taiKhoan;
    }

    public Task<SinhVien?> LayAsync(string maSV)
        => _db.SinhVien.Find(sv => sv.MaSV == maSV).FirstOrDefaultAsync()!;

    /// <summary>
    /// Một trang danh sách SV + tổng số dòng khớp bộ lọc (phân trang phía server).
    /// Tương đương:
    ///   db.sinhvien.find({ maLop, trangThai, $or: [{ maSV: /q/i }, { hoTen: /q/i }] }, { monHoc: 0 })
    ///              .sort({ maSV: 1 }).skip((trang-1)*20).limit(20)
    ///   db.sinhvien.countDocuments({ ...cùng bộ lọc })
    /// Bỏ mảng monHoc khỏi kết quả (projection) vì bảng danh sách không cần – GPA lấy từ pipeline.
    /// </summary>
    public async Task<(List<SinhVien> DanhSach, long TongSo)> DanhSachAsync(
        string? maLop, string? trangThai, string? q, int trang, int kichThuoc)
    {
        var b = Builders<SinhVien>.Filter;
        var filter = b.Empty;
        if (!string.IsNullOrWhiteSpace(maLop)) filter &= b.Eq(sv => sv.MaLop, maLop);
        if (!string.IsNullOrWhiteSpace(trangThai)) filter &= b.Eq(sv => sv.TrangThai, trangThai);
        if (!string.IsNullOrWhiteSpace(q))
        {
            // Regex.Escape: ký tự đặc biệt người dùng gõ (. * ( ...) được hiểu theo nghĩa đen; "i" = không phân biệt hoa thường
            var regex = new BsonRegularExpression(Regex.Escape(q.Trim()), "i");
            filter &= b.Or(b.Regex(sv => sv.MaSV, regex), b.Regex(sv => sv.HoTen, regex));
        }

        var tongSo = await _db.SinhVien.CountDocumentsAsync(filter);
        var danhSach = await _db.SinhVien.Find(filter)
            .Project<SinhVien>(Builders<SinhVien>.Projection.Exclude(sv => sv.MonHoc))
            .SortBy(sv => sv.MaSV)
            .Skip((trang - 1) * kichThuoc)
            .Limit(kichThuoc)
            .ToListAsync();
        return (danhSach, tongSo);
    }

    /// <summary>Thêm SV (monHoc: []) rồi tạo tài khoản (mật khẩu = ngày sinh ddMMyyyy). Lỗi bước 2 → xóa SV vừa tạo.</summary>
    public async Task<KetQuaGhi> ThemAsync(SinhVienFormViewModel form)
    {
        var maSV = form.MaSV.Trim();
        if (await _db.SinhVien.Find(sv => sv.MaSV == maSV).AnyAsync())
            return KetQuaGhi.Loi($"MSSV {maSV} đã tồn tại.", nameof(form.MaSV));
        if (await _taiKhoan.TonTaiAsync(maSV))
            return KetQuaGhi.Loi($"Tên đăng nhập {maSV} đã được dùng cho một tài khoản khác.", nameof(form.MaSV));

        var loiLop = await KiemTraLopAsync(form.MaLop);
        if (loiLop != null) return loiLop;

        var bayGio = DateTime.UtcNow;
        var sinhVien = new SinhVien
        {
            MaSV = maSV,
            NgayTao = bayGio,
            NgayCapNhat = bayGio,
            MonHoc = new() // monHoc: [] – môn học được thêm ở chức năng Ghi danh ($push)
        };
        GanThongTin(sinhVien, form);

        // Bước 1: db.sinhvien.insertOne({...})
        try
        {
            await _db.SinhVien.InsertOneAsync(sinhVien);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return KetQuaGhi.Loi($"MSSV {maSV} đã tồn tại.", nameof(form.MaSV));
        }

        // Bước 2: db.taikhoan.insertOne({...}); lỗi → hoàn tác bước 1
        var matKhau = TaiKhoanService.MatKhauMacDinhSinhVien(sinhVien.NgaySinh);
        try
        {
            await _taiKhoan.TaoAsync(HangSo.VaiTro.SinhVien, maSV, sinhVien.HoTen, matKhau);
        }
        catch (MongoException)
        {
            await _db.SinhVien.DeleteOneAsync(sv => sv.MaSV == maSV);
            return KetQuaGhi.Loi("Không tạo được tài khoản cho sinh viên nên đã hủy thao tác thêm. Vui lòng thử lại.");
        }

        return KetQuaGhi.Thanh($"Đã thêm sinh viên {maSV} – {sinhVien.HoTen}. Tài khoản: {maSV} / mật khẩu {matKhau}.");
    }

    /// <summary>Sửa hồ sơ (MSSV không đổi). Đổi họ tên → cập nhật luôn taikhoan.hoTen.</summary>
    public async Task<KetQuaGhi> SuaAsync(string maSV, SinhVienFormViewModel form)
    {
        var cu = await LayAsync(maSV);
        if (cu == null) return KetQuaGhi.Loi($"Không tìm thấy sinh viên {maSV}.");

        var loiLop = await KiemTraLopAsync(form.MaLop);
        if (loiLop != null) return loiLop;

        var moi = new SinhVien();
        GanThongTin(moi, form);

        // db.sinhvien.updateOne({ maSV }, { $set: { hoTen, ngaySinh, gioiTinh, email, soDienThoai, diaChi, maLop, trangThai, ngayCapNhat } })
        // Chỉ $set các trường hồ sơ – KHÔNG ghi đè mảng monHoc.
        await _db.SinhVien.UpdateOneAsync(
            sv => sv.MaSV == maSV,
            Builders<SinhVien>.Update
                .Set(sv => sv.HoTen, moi.HoTen)
                .Set(sv => sv.NgaySinh, moi.NgaySinh)
                .Set(sv => sv.GioiTinh, moi.GioiTinh)
                .Set(sv => sv.Email, moi.Email)
                .Set(sv => sv.SoDienThoai, moi.SoDienThoai)
                .Set(sv => sv.DiaChi, moi.DiaChi)
                .Set(sv => sv.MaLop, moi.MaLop)
                .Set(sv => sv.TrangThai, moi.TrangThai)
                .Set(sv => sv.NgayCapNhat, DateTime.UtcNow));

        if (moi.HoTen != cu.HoTen)
        {
            var lenh = await _taiKhoan.CapNhatHoTenAsync(HangSo.VaiTro.SinhVien, maSV, moi.HoTen);
            return KetQuaGhi.Thanh($"Đã cập nhật sinh viên {maSV}. Họ tên tài khoản đăng nhập cũng được đổi theo.", lenh);
        }
        return KetQuaGhi.Thanh($"Đã cập nhật sinh viên {maSV}.");
    }

    /// <summary>Xóa SV rồi xóa tài khoản. Xóa tài khoản lỗi → chèn lại hồ sơ SV vừa xóa.</summary>
    public async Task<KetQuaGhi> XoaAsync(string maSV)
    {
        var sinhVien = await LayAsync(maSV);
        if (sinhVien == null) return KetQuaGhi.Loi($"Không tìm thấy sinh viên {maSV}.");

        // Bước 1: db.sinhvien.deleteOne({ maSV })
        await _db.SinhVien.DeleteOneAsync(sv => sv.MaSV == maSV);

        // Bước 2: db.taikhoan.deleteOne({ vaiTro: "SinhVien", maThamChieu: maSV }); lỗi → hoàn tác bước 1
        try
        {
            await _taiKhoan.XoaAsync(HangSo.VaiTro.SinhVien, maSV);
        }
        catch (MongoException)
        {
            await _db.SinhVien.InsertOneAsync(sinhVien);
            return KetQuaGhi.Loi("Không xóa được tài khoản của sinh viên nên đã khôi phục hồ sơ. Vui lòng thử lại.");
        }
        return KetQuaGhi.Thanh($"Đã xóa sinh viên {maSV} – {sinhVien.HoTen} và tài khoản đăng nhập.");
    }

    /// <summary>Reset mật khẩu về ngày sinh ddMMyyyy (A9).</summary>
    public async Task<KetQuaGhi> ResetMatKhauAsync(string maSV)
    {
        var sinhVien = await LayAsync(maSV);
        if (sinhVien == null) return KetQuaGhi.Loi($"Không tìm thấy sinh viên {maSV}.");

        var matKhau = TaiKhoanService.MatKhauMacDinhSinhVien(sinhVien.NgaySinh);
        if (!await _taiKhoan.ResetMatKhauAsync(HangSo.VaiTro.SinhVien, maSV, matKhau))
            return KetQuaGhi.Loi($"Sinh viên {maSV} chưa có tài khoản.");
        return KetQuaGhi.Thanh($"Đã đặt lại mật khẩu của {maSV} về mặc định ({matKhau}).");
    }

    /// <summary>Khóa / mở khóa tài khoản SV (A9).</summary>
    public async Task<KetQuaGhi> DoiTrangThaiTaiKhoanAsync(string maSV)
    {
        var trangThai = await _taiKhoan.DoiTrangThaiAsync(HangSo.VaiTro.SinhVien, maSV);
        if (trangThai == null) return KetQuaGhi.Loi($"Sinh viên {maSV} chưa có tài khoản.");
        return KetQuaGhi.Thanh(trangThai == HangSo.TrangThaiTaiKhoan.Khoa
            ? $"Đã khóa tài khoản {maSV}."
            : $"Đã mở khóa tài khoản {maSV}.");
    }

    private async Task<KetQuaGhi?> KiemTraLopAsync(string maLop)
    {
        if (!await _db.LopHoc.Find(l => l.MaLop == maLop).AnyAsync())
            return KetQuaGhi.Loi("Lớp được chọn không tồn tại.", nameof(SinhVienFormViewModel.MaLop));
        return null;
    }

    /// <summary>Chép dữ liệu form vào document; chuỗi rỗng → null; ngày sinh lưu 00:00 UTC.</summary>
    private static void GanThongTin(SinhVien sv, SinhVienFormViewModel form)
    {
        static string? RongThanhNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        sv.HoTen = form.HoTen.Trim();
        sv.NgaySinh = DateTime.SpecifyKind(form.NgaySinh!.Value.Date, DateTimeKind.Utc);
        sv.GioiTinh = form.GioiTinh;
        sv.Email = RongThanhNull(form.Email);
        sv.SoDienThoai = RongThanhNull(form.SoDienThoai);
        sv.DiaChi = RongThanhNull(form.DiaChi);
        sv.MaLop = form.MaLop;
        sv.TrangThai = form.TrangThai;
    }
}
