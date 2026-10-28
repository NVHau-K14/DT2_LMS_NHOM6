using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.ViewModels;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Điểm: ĐỌC qua pipeline (TK, chữ, hệ 4, đạt, GPA, xếp loại đều do pipeline tính) và
/// GHI điểm thành phần bằng $set + arrayFilters (G3, thiết kế 4.8).
/// Dùng cho: bảng điểm SV, GPA theo học kỳ, lớp học phần của GV, danh sách nhập điểm, lưu điểm.
/// </summary>
public class DiemService
{
    private readonly MongoContext _db;

    public DiemService(MongoContext db)
    {
        _db = db;
    }

    /// <summary>Mọi lần học của một SV (pipeline BangDiem), sắp theo năm học, học kỳ, mã môn.</summary>
    public Task<KetQuaPipeline<KetQuaMon>> BangDiemAsync(string maSV)
        => PipelineRunner.ChayAsync<KetQuaMon>(_db, BangDiemPipeline.Tao(maSV));

    /// <summary>GPA từng học kỳ của một SV (pipeline GpaHocKy).</summary>
    public Task<KetQuaPipeline<GpaHocKyResult>> GpaHocKyAsync(string maSV)
        => GpaHocKyAsync(BoLocSinhVien.TheoSinhVien(maSV));

    /// <summary>GPA từng học kỳ của nhóm SV theo bộ lọc.</summary>
    public Task<KetQuaPipeline<GpaHocKyResult>> GpaHocKyAsync(BoLocSinhVien boLoc)
        => PipelineRunner.ChayAsync<GpaHocKyResult>(_db, GpaHocKyPipeline.Tao(boLoc));

    /// <summary>Các lớp học phần của giảng viên; namHoc/hocKy null → mọi học kỳ.</summary>
    public Task<KetQuaPipeline<LopHocPhanRow>> LopHocPhanCuaGVAsync(string maGV, string? namHoc = null, int? hocKy = null)
        => PipelineRunner.ChayAsync<LopHocPhanRow>(_db, LopHocPhanCuaGVPipeline.Tao(maGV, namHoc, hocKy));

    /// <summary>Một lớp học phần của GV (null nếu GV không dạy lớp học phần này) – dùng để kiểm tra quyền vào G-03.</summary>
    public async Task<LopHocPhanRow?> LayLopHocPhanAsync(string maGV, string maMon, string namHoc, int hocKy)
        => (await LopHocPhanCuaGVAsync(maGV, namHoc, hocKy)).DuLieu.FirstOrDefault(l => l.MaMon == maMon);

    /// <summary>SV của một lớp học phần kèm điểm; maGV = null khi Admin xem.</summary>
    public Task<KetQuaPipeline<DongNhapDiem>> DanhSachNhapDiemAsync(string? maGV, string maMon, string namHoc, int hocKy)
        => PipelineRunner.ChayAsync<DongNhapDiem>(_db, DanhSachNhapDiemPipeline.Tao(maGV, maMon, namHoc, hocKy));

    /// <summary>
    /// Lưu một dòng điểm (nút Lưu từng dòng). maGV = null khi Admin nhập (GĐ7, capNhatBoi "admin").
    /// Sau khi ghi, chạy lại pipeline BangDiem cho đúng SV đó để lấy TK / chữ / hệ 4 mới.
    /// </summary>
    public async Task<KetQuaLuuDiemJson> CapNhatDiemAsync(string? maGV, DongLuuDiem dong)
    {
        if (!TryTaoLenhNhapDiem(maGV, dong, DateTime.UtcNow, out var lenh, out var loi))
            return new KetQuaLuuDiemJson { ThongBao = loi! };

        var kq = await _db.Database.GetCollection<BsonDocument>(MongoContext.TenSinhVien).UpdateOneAsync(
            lenh!.Filter, lenh.Update,
            new UpdateOptions { ArrayFilters = lenh.TaoModel().ArrayFilters });
        var chuoiLenh = MongoCommandFormatter.UpdateOne(MongoContext.TenSinhVien, lenh.Filter, lenh.Update, lenh.ArrayFilters, kq);

        // capNhatLuc luôn đổi nên lần học khớp quyền thì modifiedCount = 1; = 0 nghĩa là không có quyền / không tìm thấy
        if (kq.ModifiedCount == 0)
            return new KetQuaLuuDiemJson { ThongBao = "Không có quyền hoặc không tìm thấy lần học.", MongoCommand = chuoiLenh };

        var maDK = ObjectId.Parse(dong.MaDK);
        var mon = (await BangDiemAsync(dong.MaSV)).DuLieu.First(m => m.MaDK == maDK);
        return new KetQuaLuuDiemJson
        {
            ThanhCong = true,
            ThongBao = $"Đã lưu điểm {mon.MaMon} của {dong.MaSV}: TK {HienThi.Diem(mon.DiemTK)}" + (mon.DiemChu != null ? $" ({mon.DiemChu})." : "."),
            MongoCommand = chuoiLenh,
            Dong = [DongDiemJson.Tao(mon)]
        };
    }

    /// <summary>
    /// "Lưu tất cả": kiểm tra toàn bộ các dòng (một dòng sai → không ghi gì), bulkWrite các updateOne,
    /// rồi chạy lại pipeline DanhSachNhapDiem cho cả lớp học phần để trình duyệt cập nhật bảng.
    /// </summary>
    public async Task<KetQuaLuuDiemJson> CapNhatNhieuDiemAsync(string? maGV, YeuCauLuuNhieuDiem yc)
    {
        if (yc.Dong.Count == 0)
            return new KetQuaLuuDiemJson { ThongBao = "Không có dòng nào thay đổi." };

        var bayGio = DateTime.UtcNow;
        var cacLenh = new List<LenhUpdateOne>();
        foreach (var dong in yc.Dong)
        {
            if (!TryTaoLenhNhapDiem(maGV, dong, bayGio, out var lenh, out var loi))
                return new KetQuaLuuDiemJson { ThongBao = $"Dòng {dong.MaSV}: {loi} Chưa lưu dòng nào." };
            cacLenh.Add(lenh!);
        }

        BulkWriteResult kq;
        try
        {
            kq = await _db.Database.GetCollection<BsonDocument>(MongoContext.TenSinhVien)
                .BulkWriteAsync(cacLenh.Select(l => l.TaoModel()), new BulkWriteOptions { IsOrdered = false });
        }
        catch (MongoBulkWriteException<BsonDocument> ex)
        {
            kq = ex.Result;
        }
        var chuoiLenh = MongoCommandFormatter.BulkWriteUpdateOne(MongoContext.TenSinhVien, cacLenh, false, kq);

        var dsMoi = await DanhSachNhapDiemAsync(maGV, yc.MaMon, yc.NamHoc, yc.HocKy);
        var soLoi = cacLenh.Count - kq.ModifiedCount;
        return new KetQuaLuuDiemJson
        {
            ThanhCong = kq.ModifiedCount > 0,
            ThongBao = $"Đã lưu {kq.ModifiedCount}/{cacLenh.Count} dòng điểm."
                     + (soLoi > 0 ? $" {soLoi} dòng không có quyền hoặc không tìm thấy lần học." : ""),
            MongoCommand = chuoiLenh,
            Dong = dsMoi.DuLieu.Select(DongDiemJson.Tao).ToList()
        };
    }

    /// <summary>
    /// Dựng lệnh nhập điểm (thiết kế 4.8) từ một dòng người dùng gửi:
    ///   db.sinhvien.updateOne(
    ///     { maSV, monHoc: { $elemMatch: { maDK, "giangVien.maGV": maGV } } },
    ///     { $set: { "monHoc.$[e].diem.cc": .., "monHoc.$[e].diem.gk": .., "monHoc.$[e].diem.ck": ..,
    ///               "monHoc.$[e].capNhatLuc": now, "monHoc.$[e].capNhatBoi": maGV, ngayCapNhat: now } },
    ///     { arrayFilters: [ { "e.maDK": maDK, "e.giangVien.maGV": maGV } ] })
    /// - arrayFilters: $[e] chỉ trỏ tới phần tử có ĐÚNG maDK VÀ do chính GV này dạy → quyền nằm trong câu lệnh.
    /// - Cùng điều kiện được lặp ở filter ($elemMatch): nếu không có quyền thì document không khớp,
    ///   nên cả ngayCapNhat (trường ngoài mảng) cũng không bị ghi → modifiedCount = 0.
    /// - Admin (maGV = null): bỏ điều kiện giangVien.maGV, capNhatBoi = "admin".
    /// </summary>
    private static bool TryTaoLenhNhapDiem(string? maGV, DongLuuDiem dong, DateTime bayGio,
        out LenhUpdateOne? lenh, out string? loi)
    {
        lenh = null;
        if (string.IsNullOrWhiteSpace(dong.MaSV) || !ObjectId.TryParse(dong.MaDK, out var maDK))
        {
            loi = "Thiếu MSSV hoặc mã lần học không hợp lệ.";
            return false;
        }
        if (!DiemNhap.TryParse(dong.Cc, out var cc, out loi) ||
            !DiemNhap.TryParse(dong.Gk, out var gk, out loi) ||
            !DiemNhap.TryParse(dong.Ck, out var ck, out loi))
            return false;

        var dieuKien = new BsonDocument("maDK", maDK);
        if (maGV != null) dieuKien.Add("giangVien.maGV", maGV);

        var filter = new BsonDocument
        {
            { "maSV", dong.MaSV },
            { "monHoc", new BsonDocument("$elemMatch", dieuKien) }
        };
        var update = new BsonDocument("$set", new BsonDocument
        {
            { "monHoc.$[e].diem.cc", GiaTriDiem(cc) },
            { "monHoc.$[e].diem.gk", GiaTriDiem(gk) },
            { "monHoc.$[e].diem.ck", GiaTriDiem(ck) },
            { "monHoc.$[e].capNhatLuc", bayGio },
            { "monHoc.$[e].capNhatBoi", maGV ?? HangSo.MaAdmin },
            { "ngayCapNhat", bayGio }
        });
        // arrayFilters: { "e.maDK": ..., "e.giangVien.maGV": ... } – cùng điều kiện, viết theo biến e
        var locPhanTu = new BsonDocument();
        foreach (var phanTu in dieuKien)
            locPhanTu.Add("e." + phanTu.Name, phanTu.Value);

        lenh = new LenhUpdateOne(filter, update, [locPhanTu]);
        return true;
    }

    /// <summary>Điểm lưu kiểu double (validator cho phép double / int / null); ô trống → null.</summary>
    private static BsonValue GiaTriDiem(double? diem) => diem.HasValue ? new BsonDouble(diem.Value) : BsonNull.Value;
}
