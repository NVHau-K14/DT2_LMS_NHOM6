using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// CRUD giảng viên (A4). Thêm GV tự tạo tài khoản (mật khẩu 123456).
/// Đổi họ tên / học vị → LAN TRUYỀN tên kèm học vị sang mọi nơi lưu bản sao (thiết kế 4.8):
///   sinhvien.monHoc[].giangVien.hoTen (updateMany + arrayFilters), lophoc.coVanHocTap.hoTen, taikhoan.hoTen.
/// Không xóa GV đang được phân công dạy hoặc đang làm CVHT.
/// </summary>
public class GiangVienService
{
    private readonly MongoContext _db;
    private readonly TaiKhoanService _taiKhoan;
    private readonly DiemService _diem;
    private readonly HocKyHienTaiOptions _hocKyHienTai;

    public GiangVienService(MongoContext db, TaiKhoanService taiKhoan, DiemService diem, IOptions<HocKyHienTaiOptions> hocKyHienTai)
    {
        _db = db;
        _taiKhoan = taiKhoan;
        _diem = diem;
        _hocKyHienTai = hocKyHienTai.Value;
    }

    public Task<List<GiangVien>> TatCaAsync()
        => _db.GiangVien.Find(FilterDefinition<GiangVien>.Empty).SortBy(g => g.MaGV).ToListAsync();

    public Task<GiangVien?> LayAsync(string maGV)
        => _db.GiangVien.Find(g => g.MaGV == maGV).FirstOrDefaultAsync()!;

    /// <summary>
    /// Danh sách GV kèm số lớp học phần đang dạy ở học kỳ hiện tại (pipeline LopHocPhanCuaGV) và tài khoản.
    /// </summary>
    public async Task<List<DongGiangVien>> DanhSachAsync()
    {
        var dsGV = await TatCaAsync();
        var taiKhoan = await _taiKhoan.LayTheoMaAsync(HangSo.VaiTro.GiangVien, dsGV.Select(g => g.MaGV));

        var ketQua = new List<DongGiangVien>();
        foreach (var gv in dsGV)
        {
            var lhp = await _diem.LopHocPhanCuaGVAsync(gv.MaGV, _hocKyHienTai.NamHoc, _hocKyHienTai.HocKy);
            ketQua.Add(new DongGiangVien(gv, lhp.DuLieu.Count, taiKhoan.GetValueOrDefault(gv.MaGV)));
        }
        return ketQua;
    }

    /// <summary>Thêm GV rồi tạo tài khoản (mật khẩu 123456). Lỗi bước 2 → xóa GV vừa tạo.</summary>
    public async Task<KetQuaGhi> ThemAsync(GiangVienFormViewModel form)
    {
        var maGV = form.MaGV.Trim().ToUpperInvariant();
        if (await _db.GiangVien.Find(g => g.MaGV == maGV).AnyAsync())
            return KetQuaGhi.Loi($"Mã giảng viên {maGV} đã tồn tại.", nameof(form.MaGV));
        if (await _taiKhoan.TonTaiAsync(maGV))
            return KetQuaGhi.Loi($"Tên đăng nhập {maGV} đã được dùng cho một tài khoản khác.", nameof(form.MaGV));

        var gv = new GiangVien
        {
            MaGV = maGV,
            HoTen = form.HoTen.Trim(),
            HocVi = form.HocVi,
            Email = RongThanhNull(form.Email),
            SoDienThoai = RongThanhNull(form.SoDienThoai),
            Khoa = form.Khoa.Trim(),
            NgayTao = DateTime.UtcNow
        };

        // Bước 1: db.giangvien.insertOne({...})
        try
        {
            await _db.GiangVien.InsertOneAsync(gv);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return KetQuaGhi.Loi($"Mã giảng viên {maGV} đã tồn tại.", nameof(form.MaGV));
        }

        // Bước 2: db.taikhoan.insertOne({...}); lỗi → hoàn tác bước 1
        try
        {
            await _taiKhoan.TaoAsync(HangSo.VaiTro.GiangVien, maGV, gv.HoTenKemHocVi, TaiKhoanService.MatKhauMacDinhGiangVien);
        }
        catch (MongoException)
        {
            await _db.GiangVien.DeleteOneAsync(g => g.MaGV == maGV);
            return KetQuaGhi.Loi("Không tạo được tài khoản cho giảng viên nên đã hủy thao tác thêm. Vui lòng thử lại.");
        }
        return KetQuaGhi.Thanh(
            $"Đã thêm giảng viên {gv.HoTenKemHocVi}. Tài khoản: {maGV} / mật khẩu {TaiKhoanService.MatKhauMacDinhGiangVien}.");
    }

    /// <summary>
    /// Sửa GV (mã không đổi). Nếu họ tên hoặc học vị đổi → lan truyền "học vị + họ tên" sang 3 collection,
    /// trả về chuỗi các lệnh đã chạy để hiện trong toast.
    /// </summary>
    public async Task<KetQuaGhi> SuaAsync(string maGV, GiangVienFormViewModel form)
    {
        var cu = await LayAsync(maGV);
        if (cu == null) return KetQuaGhi.Loi($"Không tìm thấy giảng viên {maGV}.");

        var hoTen = form.HoTen.Trim();
        // db.giangvien.updateOne({ maGV }, { $set: { hoTen, hocVi, email, soDienThoai, khoa } })
        await _db.GiangVien.UpdateOneAsync(
            g => g.MaGV == maGV,
            Builders<GiangVien>.Update
                .Set(g => g.HoTen, hoTen)
                .Set(g => g.HocVi, form.HocVi)
                .Set(g => g.Email, RongThanhNull(form.Email))
                .Set(g => g.SoDienThoai, RongThanhNull(form.SoDienThoai))
                .Set(g => g.Khoa, form.Khoa.Trim()));

        if (hoTen == cu.HoTen && form.HocVi == cu.HocVi)
            return KetQuaGhi.Thanh($"Đã cập nhật giảng viên {maGV}.");

        var tenMoi = new GiangVien { HoTen = hoTen, HocVi = form.HocVi }.HoTenKemHocVi;
        var cacLenh = await LanTruyenHoTenAsync(maGV, tenMoi);
        return KetQuaGhi.Thanh(
            $"Đã cập nhật giảng viên {maGV}. Tên \"{tenMoi}\" đã được lan truyền sang hồ sơ sinh viên, lớp (CVHT) và tài khoản.",
            MongoCommandFormatter.GhepLenh(cacLenh));
    }

    /// <summary>
    /// Lan truyền tên GV (kèm học vị) sang mọi bản sao – thiết kế 4.8. Không có transaction nên chạy tuần tự;
    /// mỗi lệnh idempotent (chạy lại cho cùng kết quả) nên nếu lỗi giữa chừng chỉ cần lưu lại lần nữa.
    /// </summary>
    private async Task<List<string>> LanTruyenHoTenAsync(string maGV, string tenMoi)
    {
        var cacLenh = new List<string>();

        // 1) Mọi lần học do GV dạy trong mảng monHoc của mọi SV:
        //    db.sinhvien.updateMany(
        //      { "monHoc.giangVien.maGV": maGV },                         ← chọn SV có ít nhất 1 lần học của GV (dùng index)
        //      { $set: { "monHoc.$[e].giangVien.hoTen": tenMoi } },       ← $[e] = MỌI phần tử thỏa điều kiện e
        //      { arrayFilters: [ { "e.giangVien.maGV": maGV } ] })        ← e là phần tử có giangVien.maGV = maGV
        var filterSV = new BsonDocument("monHoc.giangVien.maGV", maGV);
        var updateSV = new BsonDocument("$set", new BsonDocument("monHoc.$[e].giangVien.hoTen", tenMoi));
        var arrayFilters = new List<BsonDocument> { new("e.giangVien.maGV", maGV) };
        var kqSV = await _db.Database.GetCollection<BsonDocument>(MongoContext.TenSinhVien).UpdateManyAsync(
            filterSV, updateSV,
            new UpdateOptions { ArrayFilters = arrayFilters.Select(f => new BsonDocumentArrayFilterDefinition<BsonDocument>(f)) });
        cacLenh.Add(MongoCommandFormatter.UpdateMany(MongoContext.TenSinhVien, filterSV, updateSV, arrayFilters, kqSV));

        // 2) Các lớp mà GV làm cố vấn học tập:
        //    db.lophoc.updateMany({ "coVanHocTap.maGV": maGV }, { $set: { "coVanHocTap.hoTen": tenMoi } })
        var filterLop = new BsonDocument("coVanHocTap.maGV", maGV);
        var updateLop = new BsonDocument("$set", new BsonDocument("coVanHocTap.hoTen", tenMoi));
        var kqLop = await _db.Database.GetCollection<BsonDocument>(MongoContext.TenLopHoc).UpdateManyAsync(filterLop, updateLop);
        cacLenh.Add(MongoCommandFormatter.UpdateMany(MongoContext.TenLopHoc, filterLop, updateLop, ketQua: kqLop));

        // 3) Tài khoản: db.taikhoan.updateOne({ vaiTro: "GiangVien", maThamChieu: maGV }, { $set: { hoTen: tenMoi } })
        cacLenh.Add(await _taiKhoan.CapNhatHoTenAsync(HangSo.VaiTro.GiangVien, maGV, tenMoi));

        return cacLenh;
    }

    /// <summary>Xóa GV + tài khoản – chặn nếu GV đang được phân công dạy (có lần học trong monHoc) hoặc đang làm CVHT.</summary>
    public async Task<KetQuaGhi> XoaAsync(string maGV)
    {
        var gv = await LayAsync(maGV);
        if (gv == null) return KetQuaGhi.Loi($"Không tìm thấy giảng viên {maGV}.");

        var lyDo = new List<string>();
        // db.sinhvien.countDocuments({ "monHoc.giangVien.maGV": maGV })
        var soSV = await _db.SinhVien.CountDocumentsAsync(new BsonDocument("monHoc.giangVien.maGV", maGV));
        if (soSV > 0) lyDo.Add($"đang được phân công dạy ({soSV} sinh viên có lần học do GV này phụ trách)");
        // db.lophoc.find({ "coVanHocTap.maGV": maGV })
        var dsLop = await _db.LopHoc.Find(new BsonDocument("coVanHocTap.maGV", maGV)).ToListAsync();
        if (dsLop.Count > 0) lyDo.Add($"đang làm cố vấn học tập lớp {string.Join(", ", dsLop.Select(l => l.MaLop))}");
        if (lyDo.Count > 0)
            return KetQuaGhi.Loi($"Không thể xóa giảng viên {gv.HoTenKemHocVi} vì {string.Join(" và ", lyDo)}.");

        // Bước 1: db.giangvien.deleteOne({ maGV }); Bước 2: db.taikhoan.deleteOne(...), lỗi → chèn lại GV
        await _db.GiangVien.DeleteOneAsync(g => g.MaGV == maGV);
        try
        {
            await _taiKhoan.XoaAsync(HangSo.VaiTro.GiangVien, maGV);
        }
        catch (MongoException)
        {
            await _db.GiangVien.InsertOneAsync(gv);
            return KetQuaGhi.Loi("Không xóa được tài khoản của giảng viên nên đã khôi phục. Vui lòng thử lại.");
        }
        return KetQuaGhi.Thanh($"Đã xóa giảng viên {gv.HoTenKemHocVi} và tài khoản đăng nhập.");
    }

    /// <summary>Reset mật khẩu GV về 123456 (A9).</summary>
    public async Task<KetQuaGhi> ResetMatKhauAsync(string maGV)
    {
        if (!await _taiKhoan.ResetMatKhauAsync(HangSo.VaiTro.GiangVien, maGV, TaiKhoanService.MatKhauMacDinhGiangVien))
            return KetQuaGhi.Loi($"Giảng viên {maGV} chưa có tài khoản.");
        return KetQuaGhi.Thanh($"Đã đặt lại mật khẩu của {maGV} về mặc định ({TaiKhoanService.MatKhauMacDinhGiangVien}).");
    }

    /// <summary>Khóa / mở khóa tài khoản GV (A9).</summary>
    public async Task<KetQuaGhi> DoiTrangThaiTaiKhoanAsync(string maGV)
    {
        var trangThai = await _taiKhoan.DoiTrangThaiAsync(HangSo.VaiTro.GiangVien, maGV);
        if (trangThai == null) return KetQuaGhi.Loi($"Giảng viên {maGV} chưa có tài khoản.");
        return KetQuaGhi.Thanh(trangThai == HangSo.TrangThaiTaiKhoan.Khoa
            ? $"Đã khóa tài khoản {maGV}."
            : $"Đã mở khóa tài khoản {maGV}.");
    }

    private static string? RongThanhNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
