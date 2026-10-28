using System.Text.RegularExpressions;
using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
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
    /// SV có ít nhất một lần học do giảng viên này dạy không (quyền đọc của GV ở API):
    /// db.sinhvien.countDocuments({ maSV, "monHoc.giangVien.maGV": maGV }, { limit: 1 })
    /// </summary>
    public Task<bool> CoLanHocCuaGiangVienAsync(string maSV, string maGV)
        => _db.SinhVien.Find(Builders<SinhVien>.Filter.Eq(sv => sv.MaSV, maSV)
                             & Builders<SinhVien>.Filter.Eq("monHoc.giangVien.maGV", maGV)).AnyAsync();

    /// <summary>Mọi SV, không kèm mảng monHoc (ô chọn SV ở A-09). db.sinhvien.find({}, { monHoc: 0 }).sort({ maSV: 1 })</summary>
    public Task<List<SinhVien>> TatCaRutGonAsync()
        => _db.SinhVien.Find(FilterDefinition<SinhVien>.Empty)
            .Project<SinhVien>(Builders<SinhVien>.Projection.Exclude(sv => sv.MonHoc))
            .SortBy(sv => sv.MaSV)
            .ToListAsync();

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
        var filter = TaoBoLoc(maLop, trangThai, q);
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
        TaiKhoan taiKhoan;
        try
        {
            taiKhoan = await _taiKhoan.TaoAsync(HangSo.VaiTro.SinhVien, maSV, sinhVien.HoTen, matKhau);
        }
        catch (MongoException)
        {
            await _db.SinhVien.DeleteOneAsync(sv => sv.MaSV == maSV);
            return KetQuaGhi.Loi("Không tạo được tài khoản cho sinh viên nên đã hủy thao tác thêm. Vui lòng thử lại.");
        }

        var lenh = MongoCommandFormatter.GhepLenh(
        [
            MongoCommandFormatter.InsertOne(MongoContext.TenSinhVien, sinhVien.ToBsonDocument()),
            MongoCommandFormatter.InsertOne(MongoContext.TenTaiKhoan, taiKhoan.ToBsonDocument())
        ]);
        return KetQuaGhi.Thanh($"Đã thêm sinh viên {maSV} – {sinhVien.HoTen}. Tài khoản: {maSV} / mật khẩu {matKhau}.", lenh);
    }

    /// <summary>
    /// Thêm nhiều SV từ file Excel đã kiểm tra (A7). Không có transaction nên ghi tuần tự:
    ///   Bước 1: db.sinhvien.insertMany([...])   (monHoc: [])
    ///   Bước 2: db.taikhoan.insertMany([...])   (mật khẩu = ngày sinh ddMMyyyy)
    /// Bước 1 lỗi → xóa các SV đã chèn; bước 2 lỗi → TaiKhoanService tự xóa tài khoản đã chèn, ở đây xóa SV.
    /// Xóa theo _id do chính lệnh này sinh ra nên không đụng tới hồ sơ có sẵn.
    /// </summary>
    public async Task<KetQuaGhi> ThemNhieuAsync(List<SinhVien> ds)
    {
        if (ds.Count == 0) return KetQuaGhi.Loi("Không có sinh viên nào để thêm.");

        // Kiểm tra lại ngay trước khi ghi: trong lúc xem trước có thể đã có người thêm trùng MSSV
        var dsMa = ds.Select(sv => sv.MaSV).ToList();
        if (await _db.SinhVien.Find(Builders<SinhVien>.Filter.In(sv => sv.MaSV, dsMa)).AnyAsync()
            || (await _taiKhoan.TenDangNhapDaDungAsync(dsMa)).Count > 0)
            return KetQuaGhi.Loi("Một số MSSV vừa được thêm bởi thao tác khác. Vui lòng tải file lên lại để kiểm tra.");

        var bayGio = DateTime.UtcNow;
        foreach (var sv in ds)
        {
            sv.Id = ObjectId.GenerateNewId();
            sv.NgayTao = bayGio;
            sv.NgayCapNhat = bayGio;
            sv.MonHoc = new();
        }
        var locTheoId = Builders<SinhVien>.Filter.In(sv => sv.Id, ds.Select(sv => sv.Id));

        // Bước 1
        try
        {
            await _db.SinhVien.InsertManyAsync(ds, new InsertManyOptions { IsOrdered = true });
        }
        catch (MongoException)
        {
            await _db.SinhVien.DeleteManyAsync(locTheoId);
            return KetQuaGhi.Loi("Không ghi được danh sách sinh viên nên đã hủy toàn bộ thao tác. Vui lòng thử lại.");
        }

        // Bước 2
        List<TaiKhoan> cacTaiKhoan;
        try
        {
            cacTaiKhoan = await _taiKhoan.TaoNhieuAsync(HangSo.VaiTro.SinhVien,
                ds.Select(sv => (sv.MaSV, sv.HoTen, TaiKhoanService.MatKhauMacDinhSinhVien(sv.NgaySinh))));
        }
        catch (MongoException)
        {
            await _db.SinhVien.DeleteManyAsync(locTheoId);
            return KetQuaGhi.Loi("Không tạo được tài khoản cho sinh viên nên đã hủy toàn bộ thao tác nhập. Vui lòng thử lại.");
        }

        var lenh = MongoCommandFormatter.GhepLenh(
        [
            MongoCommandFormatter.InsertMany(MongoContext.TenSinhVien, ds.Select(sv => sv.ToBsonDocument()).ToList(), true),
            MongoCommandFormatter.InsertMany(MongoContext.TenTaiKhoan, cacTaiKhoan.Select(tk => tk.ToBsonDocument()).ToList(), true)
        ]);
        return KetQuaGhi.Thanh($"Đã nhập {ds.Count} sinh viên và tạo {cacTaiKhoan.Count} tài khoản (tên đăng nhập = MSSV, mật khẩu = ngày sinh ddMMyyyy).", lenh);
    }

    /// <summary>Mọi SV khớp bộ lọc của A-04 (không phân trang, không kèm monHoc) – dùng cho Xuất Excel.</summary>
    public Task<List<SinhVien>> DanhSachTatCaAsync(string? maLop, string? trangThai, string? q)
        => _db.SinhVien.Find(TaoBoLoc(maLop, trangThai, q))
            .Project<SinhVien>(Builders<SinhVien>.Projection.Exclude(sv => sv.MonHoc))
            .SortBy(sv => sv.MaSV)
            .ToListAsync();

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
        static BsonValue ChuoiHoacNull(string? s) => s == null ? BsonNull.Value : new BsonString(s);
        var filter = new BsonDocument("maSV", maSV);
        var update = new BsonDocument("$set", new BsonDocument
        {
            { "hoTen", moi.HoTen },
            { "ngaySinh", moi.NgaySinh },
            { "gioiTinh", moi.GioiTinh },
            { "email", ChuoiHoacNull(moi.Email) },
            { "soDienThoai", ChuoiHoacNull(moi.SoDienThoai) },
            { "diaChi", ChuoiHoacNull(moi.DiaChi) },
            { "maLop", moi.MaLop },
            { "trangThai", moi.TrangThai },
            { "ngayCapNhat", DateTime.UtcNow }
        });
        var kq = await _db.Database.GetCollection<BsonDocument>(MongoContext.TenSinhVien).UpdateOneAsync(filter, update);
        var lenhSinhVien = MongoCommandFormatter.UpdateOne(MongoContext.TenSinhVien, filter, update, ketQua: kq);

        if (moi.HoTen != cu.HoTen)
        {
            var lenhTaiKhoan = await _taiKhoan.CapNhatHoTenAsync(HangSo.VaiTro.SinhVien, maSV, moi.HoTen);
            return KetQuaGhi.Thanh($"Đã cập nhật sinh viên {maSV}. Họ tên tài khoản đăng nhập cũng được đổi theo.",
                MongoCommandFormatter.GhepLenh([lenhSinhVien, lenhTaiKhoan]));
        }
        return KetQuaGhi.Thanh($"Đã cập nhật sinh viên {maSV}.", lenhSinhVien);
    }

    /// <summary>Xóa SV rồi xóa tài khoản. Xóa tài khoản lỗi → chèn lại hồ sơ SV vừa xóa.</summary>
    public async Task<KetQuaGhi> XoaAsync(string maSV)
    {
        var sinhVien = await LayAsync(maSV);
        if (sinhVien == null) return KetQuaGhi.Loi($"Không tìm thấy sinh viên {maSV}.");

        // Bước 1: db.sinhvien.deleteOne({ maSV })
        var kqXoaSV = await _db.SinhVien.DeleteOneAsync(sv => sv.MaSV == maSV);

        // Bước 2: db.taikhoan.deleteOne({ vaiTro: "SinhVien", maThamChieu: maSV }); lỗi → hoàn tác bước 1
        long soTaiKhoanXoa;
        try
        {
            soTaiKhoanXoa = await _taiKhoan.XoaAsync(HangSo.VaiTro.SinhVien, maSV);
        }
        catch (MongoException)
        {
            await _db.SinhVien.InsertOneAsync(sinhVien);
            return KetQuaGhi.Loi("Không xóa được tài khoản của sinh viên nên đã khôi phục hồ sơ. Vui lòng thử lại.");
        }
        var lenh = MongoCommandFormatter.GhepLenh(
        [
            MongoCommandFormatter.DeleteOne(MongoContext.TenSinhVien, new BsonDocument("maSV", maSV), kqXoaSV.DeletedCount),
            MongoCommandFormatter.DeleteOne(MongoContext.TenTaiKhoan,
                new BsonDocument { { "vaiTro", HangSo.VaiTro.SinhVien }, { "maThamChieu", maSV } }, soTaiKhoanXoa)
        ]);
        return KetQuaGhi.Thanh($"Đã xóa sinh viên {maSV} – {sinhVien.HoTen} và tài khoản đăng nhập.", lenh);
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

    /// <summary>{ maLop, trangThai, $or: [{ maSV: /q/i }, { hoTen: /q/i }] } – điều kiện nào trống thì bỏ.</summary>
    private static FilterDefinition<SinhVien> TaoBoLoc(string? maLop, string? trangThai, string? q)
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
        return filter;
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
