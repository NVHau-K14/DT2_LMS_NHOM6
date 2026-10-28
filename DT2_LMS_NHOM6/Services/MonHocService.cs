using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.ViewModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Danh mục môn học (A5). Đổi tên môn → lan truyền sang sinhvien.monHoc[].tenMon (updateMany + arrayFilters).
/// Đổi tín chỉ / trọng số chỉ áp dụng cho các lần ghi danh MỚI: các lần học đã ghi danh giữ ảnh chụp cũ.
/// Không xóa môn đã có người học.
/// </summary>
public class MonHocService
{
    private readonly MongoContext _db;

    public MonHocService(MongoContext db)
    {
        _db = db;
    }

    public Task<List<MonHoc>> TatCaAsync()
        => _db.MonHoc.Find(FilterDefinition<MonHoc>.Empty).SortBy(m => m.MaMon).ToListAsync();

    /// <summary>Danh sách môn kèm số lần học / số SV đã học (pipeline SoLanHocTheoMon).</summary>
    public async Task<List<DongMonHoc>> DanhSachAsync()
    {
        var dsMon = await TatCaAsync();
        var thongKe = (await PipelineRunner.ChayAsync<SoLanHocMonRow>(_db, ThongKeDanhMucPipeline.SoLanHocTheoMon()))
            .DuLieu.ToDictionary(r => r.MaMon);
        return dsMon.Select(m =>
        {
            var tk = thongKe.GetValueOrDefault(m.MaMon);
            return new DongMonHoc(m, tk?.SoLanHoc ?? 0, tk?.SoSV ?? 0);
        }).ToList();
    }

    public async Task<KetQuaGhi> ThemAsync(MonHocFormViewModel form)
    {
        var maMon = form.MaMon.Trim().ToUpperInvariant();
        if (await _db.MonHoc.Find(m => m.MaMon == maMon).AnyAsync())
            return KetQuaGhi.Loi($"Mã môn {maMon} đã tồn tại.", nameof(form.MaMon));

        var mon = new MonHoc
        {
            MaMon = maMon,
            TenMon = form.TenMon.Trim(),
            SoTinChi = form.SoTinChi!.Value,
            TrongSoMacDinh = new TrongSo { Cc = form.TrongSoCC!.Value, Gk = form.TrongSoGK!.Value, Ck = form.TrongSoCK!.Value },
            Khoa = form.Khoa.Trim(),
            NgayTao = DateTime.UtcNow
        };
        try
        {
            await _db.MonHoc.InsertOneAsync(mon);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return KetQuaGhi.Loi($"Mã môn {maMon} đã tồn tại.", nameof(form.MaMon));
        }
        return KetQuaGhi.Thanh($"Đã thêm môn {maMon} – {mon.TenMon}.");
    }

    /// <summary>
    /// Sửa môn (mã không đổi). Tín chỉ / trọng số chỉ đổi trong danh mục (ghi danh mới).
    /// Tên đổi → lan truyền sang mọi lần học của môn trong hồ sơ SV, trả về lệnh đã chạy.
    /// </summary>
    public async Task<KetQuaGhi> SuaAsync(string maMon, MonHocFormViewModel form)
    {
        var cu = await _db.MonHoc.Find(m => m.MaMon == maMon).FirstOrDefaultAsync();
        if (cu == null) return KetQuaGhi.Loi($"Không tìm thấy môn {maMon}.");

        var tenMoi = form.TenMon.Trim();
        // db.monhoc.updateOne({ maMon }, { $set: { tenMon, soTinChi, trongSoMacDinh, khoa } })
        await _db.MonHoc.UpdateOneAsync(
            m => m.MaMon == maMon,
            Builders<MonHoc>.Update
                .Set(m => m.TenMon, tenMoi)
                .Set(m => m.SoTinChi, form.SoTinChi!.Value)
                .Set(m => m.TrongSoMacDinh, new TrongSo { Cc = form.TrongSoCC!.Value, Gk = form.TrongSoGK!.Value, Ck = form.TrongSoCK!.Value })
                .Set(m => m.Khoa, form.Khoa.Trim()));

        if (tenMoi == cu.TenMon)
            return KetQuaGhi.Thanh($"Đã cập nhật môn {maMon}. Thay đổi tín chỉ / trọng số chỉ áp dụng cho lần ghi danh mới.");

        // Lan truyền tên môn (thiết kế 4.8):
        //   db.sinhvien.updateMany(
        //     { "monHoc.maMon": maMon },                         ← SV có học môn này (dùng index ix_monHoc_maMon)
        //     { $set: { "monHoc.$[e].tenMon": tenMoi } },        ← đổi tên ở MỌI phần tử khớp e (kể cả học lại, cải thiện)
        //     { arrayFilters: [ { "e.maMon": maMon } ] })
        var filter = new BsonDocument("monHoc.maMon", maMon);
        var update = new BsonDocument("$set", new BsonDocument("monHoc.$[e].tenMon", tenMoi));
        var arrayFilters = new List<BsonDocument> { new("e.maMon", maMon) };
        var kq = await _db.Database.GetCollection<BsonDocument>(MongoContext.TenSinhVien).UpdateManyAsync(
            filter, update,
            new UpdateOptions { ArrayFilters = arrayFilters.Select(f => new BsonDocumentArrayFilterDefinition<BsonDocument>(f)) });

        return KetQuaGhi.Thanh(
            $"Đã đổi tên môn {maMon} thành \"{tenMoi}\" và cập nhật {kq.ModifiedCount} hồ sơ sinh viên.",
            MongoCommandFormatter.UpdateMany(MongoContext.TenSinhVien, filter, update, arrayFilters, kq));
    }

    /// <summary>Xóa môn – chặn nếu đã có người học (bất kỳ phần tử monHoc nào có maMon này).</summary>
    public async Task<KetQuaGhi> XoaAsync(string maMon)
    {
        // db.sinhvien.countDocuments({ "monHoc.maMon": maMon })
        var soSV = await _db.SinhVien.CountDocumentsAsync(new BsonDocument("monHoc.maMon", maMon));
        if (soSV > 0)
            return KetQuaGhi.Loi($"Không thể xóa môn {maMon} vì đã có {soSV} sinh viên học môn này.");

        var kq = await _db.MonHoc.DeleteOneAsync(m => m.MaMon == maMon);
        if (kq.DeletedCount == 0)
            return KetQuaGhi.Loi($"Không tìm thấy môn {maMon}.");
        return KetQuaGhi.Thanh($"Đã xóa môn {maMon}.");
    }
}
