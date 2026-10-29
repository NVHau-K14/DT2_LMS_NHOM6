using System.Text.RegularExpressions;
using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.ViewModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// CRUD lớp sinh hoạt (A2). Sĩ số không lưu – đếm từ sinhvien bằng pipeline SiSoTheoLop.
/// Không xóa lớp còn sinh viên.
/// </summary>
public class LopHocService
{
    private readonly MongoContext _db;

    public LopHocService(MongoContext db)
    {
        _db = db;
    }

    /// <summary>Mọi lớp, sắp theo tên lớp – tên hiển thị (dùng cho ô chọn lớp).</summary>
    public Task<List<LopHoc>> TatCaAsync()
        => _db.LopHoc.Find(FilterDefinition<LopHoc>.Empty).SortBy(l => l.TenLop).ToListAsync();

    public Task<LopHoc?> LayAsync(string maLop)
        => _db.LopHoc.Find(l => l.MaLop == maLop).FirstOrDefaultAsync()!;

    /// <summary>
    /// Danh sách lớp theo bộ lọc ngành, niên khóa và từ khóa (mã hoặc tên lớp, không phân biệt hoa thường), kèm sĩ số.
    /// Tương đương: db.lophoc.find({ nganh, nienKhoa, $or: [{ maLop: /q/i }, { tenLop: /q/i }] }).sort({ tenLop: 1 })
    /// </summary>
    public async Task<List<DongLopHoc>> DanhSachAsync(string? nganh, string? nienKhoa, string? q)
    {
        var b = Builders<LopHoc>.Filter;
        var filter = b.Empty;
        if (!string.IsNullOrWhiteSpace(nganh)) filter &= b.Eq(l => l.Nganh, nganh);
        if (!string.IsNullOrWhiteSpace(nienKhoa)) filter &= b.Eq(l => l.NienKhoa, nienKhoa);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var regex = new BsonRegularExpression(Regex.Escape(q.Trim()), "i");
            filter &= b.Or(b.Regex(l => l.MaLop, regex), b.Regex(l => l.TenLop, regex));
        }

        var dsLop = await _db.LopHoc.Find(filter).SortBy(l => l.TenLop).ToListAsync();
        var siSo = await SiSoTheoLopAsync();
        return dsLop.Select(l => new DongLopHoc(l, siSo.GetValueOrDefault(l.MaLop))).ToList();
    }

    /// <summary>Sĩ số mọi lớp (pipeline SiSoTheoLop) → từ điển maLop → siSo.</summary>
    public async Task<Dictionary<string, int>> SiSoTheoLopAsync()
    {
        var kq = await PipelineRunner.ChayAsync<SiSoLopRow>(_db, ThongKeDanhMucPipeline.SiSoTheoLop());
        return kq.DuLieu.ToDictionary(r => r.MaLop, r => r.SiSo);
    }

    /// <summary>Giá trị khác nhau của ngành và niên khóa – cho ô lọc. db.lophoc.distinct("nganh")</summary>
    public async Task<(List<string> DsNganh, List<string> DsNienKhoa)> GiaTriLocAsync()
    {
        var dsNganh = await (await _db.LopHoc.DistinctAsync(l => l.Nganh, FilterDefinition<LopHoc>.Empty)).ToListAsync();
        var dsNienKhoa = await (await _db.LopHoc.DistinctAsync(l => l.NienKhoa, FilterDefinition<LopHoc>.Empty)).ToListAsync();
        return (dsNganh.Order().ToList(), dsNienKhoa.Order().ToList());
    }

    public async Task<KetQuaGhi> ThemAsync(LopHocFormViewModel form)
    {
        var maLop = form.MaLop.Trim().ToUpperInvariant();
        var tenLop = form.TenLop.Trim();
        if (await _db.LopHoc.Find(l => l.MaLop == maLop).AnyAsync())
            return KetQuaGhi.Loi($"Mã lớp {maLop} đã tồn tại.", nameof(form.MaLop));
        if (await _db.LopHoc.Find(l => l.TenLop == maLop).AnyAsync())
            return KetQuaGhi.Loi($"Mã lớp {maLop} trùng với tên của một lớp khác.", nameof(form.MaLop));
        var loiTen = await KiemTraTenLopAsync(tenLop, null);
        if (loiTen != null) return loiTen;

        var (coVan, loi) = await TimCoVanAsync(form.MaGVCoVan);
        if (loi != null) return loi;

        var lop = new LopHoc
        {
            MaLop = maLop,
            TenLop = tenLop,
            NienKhoa = form.NienKhoa.Trim(),
            Nganh = form.Nganh.Trim(),
            Khoa = form.Khoa.Trim(),
            CoVanHocTap = coVan,
            NgayTao = DateTime.UtcNow
        };
        try
        {
            await _db.LopHoc.InsertOneAsync(lop);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Bấm đồng thời: index ux_maLop hoặc ux_tenLop chặn
            return LoiTrungKhoa(ex, maLop, tenLop);
        }
        return KetQuaGhi.Thanh($"Đã thêm lớp {tenLop} (mã {maLop}).");
    }

    /// <summary>Sửa lớp: mã lớp KHÔNG đổi (lấy từ route, bỏ qua giá trị trên form).</summary>
    public async Task<KetQuaGhi> SuaAsync(string maLop, LopHocFormViewModel form)
    {
        var tenLop = form.TenLop.Trim();
        var loiTen = await KiemTraTenLopAsync(tenLop, maLop);
        if (loiTen != null) return loiTen;

        var (coVan, loi) = await TimCoVanAsync(form.MaGVCoVan);
        if (loi != null) return loi;

        // db.lophoc.updateOne({ maLop }, { $set: { tenLop, nienKhoa, nganh, khoa, coVanHocTap } })
        UpdateResult kq;
        try
        {
            kq = await _db.LopHoc.UpdateOneAsync(
                l => l.MaLop == maLop,
                Builders<LopHoc>.Update
                    .Set(l => l.TenLop, tenLop)
                    .Set(l => l.NienKhoa, form.NienKhoa.Trim())
                    .Set(l => l.Nganh, form.Nganh.Trim())
                    .Set(l => l.Khoa, form.Khoa.Trim())
                    .Set(l => l.CoVanHocTap, coVan));
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return LoiTrungKhoa(ex, maLop, tenLop);
        }
        if (kq.MatchedCount == 0)
            return KetQuaGhi.Loi($"Không tìm thấy lớp {maLop}.");
        return KetQuaGhi.Thanh($"Đã cập nhật lớp {tenLop} (mã {maLop}).");
    }

    /// <summary>
    /// Tên lớp là tên hiển thị (16DHTH01) nên không được trùng – không phân biệt hoa thường – và không được trùng
    /// mã của lớp khác (nhập Excel / API nhận cả mã lẫn tên lớp). maLopDangSua = lớp đang sửa (bỏ qua chính nó).
    /// </summary>
    private async Task<KetQuaGhi?> KiemTraTenLopAsync(string tenLop, string? maLopDangSua)
    {
        var b = Builders<LopHoc>.Filter;
        var khacLop = maLopDangSua == null ? b.Empty : b.Ne(l => l.MaLop, maLopDangSua);
        var regex = new BsonRegularExpression($"^{Regex.Escape(tenLop)}$", "i");

        // db.lophoc.findOne({ tenLop: /^ten$/i, maLop: { $ne: maLopDangSua } })
        if (await _db.LopHoc.Find(khacLop & b.Regex(l => l.TenLop, regex)).AnyAsync())
            return KetQuaGhi.Loi($"Tên lớp {tenLop} đã tồn tại.", nameof(LopHocFormViewModel.TenLop));
        if (await _db.LopHoc.Find(khacLop & b.Eq(l => l.MaLop, tenLop.ToUpperInvariant())).AnyAsync())
            return KetQuaGhi.Loi($"Tên lớp {tenLop} trùng với mã của một lớp khác.", nameof(LopHocFormViewModel.TenLop));
        return null;
    }

    private static KetQuaGhi LoiTrungKhoa(MongoWriteException ex, string maLop, string tenLop)
        => ex.WriteError.Message.Contains("ux_tenLop")
            ? KetQuaGhi.Loi($"Tên lớp {tenLop} đã tồn tại.", nameof(LopHocFormViewModel.TenLop))
            : KetQuaGhi.Loi($"Mã lớp {maLop} đã tồn tại.", nameof(LopHocFormViewModel.MaLop));

    /// <summary>Xóa lớp – chặn khi còn sinh viên (A2).</summary>
    public async Task<KetQuaGhi> XoaAsync(string maLop)
    {
        // db.sinhvien.countDocuments({ maLop })
        var siSo = await _db.SinhVien.CountDocumentsAsync(sv => sv.MaLop == maLop);
        if (siSo > 0)
            return KetQuaGhi.Loi($"Không thể xóa lớp {maLop} vì lớp còn {siSo} sinh viên. Hãy chuyển hoặc xóa sinh viên trước.");

        var kq = await _db.LopHoc.DeleteOneAsync(l => l.MaLop == maLop);
        if (kq.DeletedCount == 0)
            return KetQuaGhi.Loi($"Không tìm thấy lớp {maLop}.");
        return KetQuaGhi.Thanh($"Đã xóa lớp {maLop}.");
    }

    /// <summary>CVHT nhúng vào lớp: { maGV, hoTen kèm học vị }. Mã trống → null (chưa phân công).</summary>
    private async Task<(GiangVienRef? CoVan, KetQuaGhi? Loi)> TimCoVanAsync(string? maGV)
    {
        if (string.IsNullOrWhiteSpace(maGV)) return (null, null);

        var gv = await _db.GiangVien.Find(g => g.MaGV == maGV).FirstOrDefaultAsync();
        if (gv == null)
            return (null, KetQuaGhi.Loi("Giảng viên được chọn làm cố vấn không tồn tại.", nameof(LopHocFormViewModel.MaGVCoVan)));
        return (new GiangVienRef { MaGV = gv.MaGV, HoTen = gv.HoTenKemHocVi }, null);
    }
}
