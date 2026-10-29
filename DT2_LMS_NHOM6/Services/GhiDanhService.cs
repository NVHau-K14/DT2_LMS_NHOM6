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
/// Ghi danh môn học (A6, màn hình A-09) và hủy ghi danh – các lệnh ghi trên mảng sinhvien.monHoc (thiết kế 4.8):
///   ghi danh      : bulkWrite nhiều updateOne { $push: { monHoc: phần tử mới } }
///   hủy ghi danh  : updateOne { $pull: { monHoc: { maDK } } } khi phần tử chưa có điểm nào
/// Hình thức học (Học mới / Học lại / Cải thiện) nhận diện từ kết quả pipeline BangDiem – C# không tính điểm.
/// </summary>
public class GhiDanhService
{
    private static readonly Regex DangNamHoc = new(@"^\d{4}-\d{4}$", RegexOptions.Compiled);

    private readonly MongoContext _db;
    private readonly DiemService _diem;
    private readonly TenLopService _tenLop;

    public GhiDanhService(MongoContext db, DiemService diem, TenLopService tenLop)
    {
        _db = db;
        _diem = diem;
        _tenLop = tenLop;
    }

    /// <summary>
    /// B3 – Xem trước: mỗi SV một dòng với lanHoc, hinhThuc hoặc lý do bị chặn. Không ghi gì vào DB.
    /// Quy tắc (thiết kế 2.4), dựa trên các lần học trước cùng maMon lấy từ pipeline BangDiem:
    ///   - đã có lần học cùng maMon trong đúng (namHoc, hocKy)  → chặn "đã ghi danh"
    ///   - có lần học trước chưa đủ điểm (duDiem = false)       → chặn "Lần học trước chưa có kết quả"
    ///   - SV không ở trạng thái "Đang học"                     → chặn
    ///   - chưa từng học → Học mới; đã có lần đạt (dat = true) → Cải thiện; còn lại → Học lại
    ///   - lanHoc = số lần học trước + 1
    /// </summary>
    public async Task<KetQuaXemTruoc> XemTruocAsync(YeuCauGhiDanh yc)
    {
        var loi = KiemTraYeuCau(yc);
        if (loi != null) return new KetQuaXemTruoc { Loi = loi };

        var mon = await _db.MonHoc.Find(m => m.MaMon == yc.MaMon).FirstOrDefaultAsync();
        var gv = await _db.GiangVien.Find(g => g.MaGV == yc.MaGV).FirstOrDefaultAsync();
        var lyDoChung = mon == null ? $"Môn {yc.MaMon} không tồn tại."
                      : gv == null ? $"Giảng viên {yc.MaGV} không tồn tại."
                      : null;

        var dsSV = await LaySinhVienAsync(yc);
        var ketQua = new KetQuaXemTruoc { MonHoc = mon, GiangVien = gv };

        // Từng SV được chọn mà không có trong DB → dòng bị chặn
        if (yc.CheDo == YeuCauGhiDanh.CheDoTungSV)
        {
            var coTrongDb = dsSV.Select(sv => sv.MaSV).ToHashSet();
            foreach (var ma in yc.DsMaSV.Where(m => !coTrongDb.Contains(m)))
                ketQua.Dong.Add(DongXemTruoc.Chan(ma, "", "", "Không tìm thấy sinh viên."));
        }

        foreach (var sv in dsSV)
        {
            if (lyDoChung != null)
            {
                ketQua.Dong.Add(DongXemTruoc.Chan(sv.MaSV, sv.HoTen, sv.MaLop, lyDoChung));
                continue;
            }
            if (sv.TrangThai != HangSo.TrangThaiSinhVien.DangHoc)
            {
                ketQua.Dong.Add(DongXemTruoc.Chan(sv.MaSV, sv.HoTen, sv.MaLop, $"Sinh viên đang ở trạng thái \"{sv.TrangThai}\"."));
                continue;
            }

            // Các lần học trước cùng môn – lấy từ pipeline BangDiem (đã có duDiem, dat do pipeline tính)
            var cacLanTruoc = (await _diem.BangDiemAsync(sv.MaSV)).DuLieu
                .Where(m => m.MaMon == yc.MaMon)
                .ToList();

            if (cacLanTruoc.Any(m => m.NamHoc == yc.NamHoc && m.HocKy == yc.HocKy))
                ketQua.Dong.Add(DongXemTruoc.Chan(sv.MaSV, sv.HoTen, sv.MaLop,
                    $"Đã ghi danh {yc.MaMon} trong {HienThi.HocKy(yc.NamHoc, yc.HocKy)}."));
            else if (cacLanTruoc.Any(m => !m.DuDiem))
                ketQua.Dong.Add(DongXemTruoc.Chan(sv.MaSV, sv.HoTen, sv.MaLop, "Lần học trước chưa có kết quả."));
            else
            {
                var hinhThuc = cacLanTruoc.Count == 0 ? HangSo.HinhThuc.HocMoi
                             : cacLanTruoc.Any(m => m.Dat == true) ? HangSo.HinhThuc.CaiThien
                             : HangSo.HinhThuc.HocLai;
                ketQua.Dong.Add(new DongXemTruoc
                {
                    MaSV = sv.MaSV, HoTen = sv.HoTen, MaLop = sv.MaLop,
                    LanHoc = cacLanTruoc.Count + 1, HinhThuc = hinhThuc, HopLe = true
                });
            }
        }

        // Sắp theo tên lớp hiển thị (16DHATTT01, ..., 16DHTH02) rồi MSSV
        var bangLop = await _tenLop.LayAsync();
        ketQua.Dong = bangLop.SapXep(ketQua.Dong.OrderBy(d => d.MaSV), d => d.MaLop).ToList();
        return ketQua;
    }

    /// <summary>
    /// B4 – Xác nhận: chạy lại bước xem trước ở server (không tin dữ liệu từ trình duyệt) rồi
    /// bulkWrite một updateOne $push cho mỗi dòng hợp lệ. Dòng bị chặn được bỏ qua.
    /// </summary>
    public async Task<KetQuaGhi> XacNhanAsync(YeuCauGhiDanh yc)
    {
        var xemTruoc = await XemTruocAsync(yc);
        if (xemTruoc.Loi != null) return KetQuaGhi.Loi(xemTruoc.Loi);

        var hopLe = xemTruoc.Dong.Where(d => d.HopLe).ToList();
        var soChan = xemTruoc.Dong.Count - hopLe.Count;
        if (hopLe.Count == 0)
            return KetQuaGhi.Loi("Không có sinh viên hợp lệ để ghi danh.");

        var mon = xemTruoc.MonHoc!;
        var gv = xemTruoc.GiangVien!;
        var bayGio = DateTime.UtcNow;

        var cacLenh = hopLe.Select(d => new LenhUpdateOne(
            // filter: đúng SV VÀ chưa có lần học cùng (maMon, namHoc, hocKy) – chống ghi trùng
            // nếu hai người bấm xác nhận cùng lúc (điều kiện kiểm tra và lệnh ghi là MỘT thao tác nguyên tử)
            new BsonDocument
            {
                { "maSV", d.MaSV },
                { "monHoc", new BsonDocument("$not", new BsonDocument("$elemMatch", new BsonDocument
                    {
                        { "maMon", mon.MaMon }, { "namHoc", yc.NamHoc }, { "hocKy", yc.HocKy }
                    })) }
            },
            // update: $push phần tử mới vào cuối mảng monHoc + $set ngayCapNhat
            new BsonDocument
            {
                { "$push", new BsonDocument("monHoc", TaoPhanTuMonHoc(mon, gv, yc.NamHoc, yc.HocKy, d.LanHoc, d.HinhThuc)) },
                { "$set", new BsonDocument("ngayCapNhat", bayGio) }
            })).ToList();

        // ordered: false – các SV độc lập nhau, một lệnh lỗi không chặn các lệnh còn lại
        var collection = _db.Database.GetCollection<BsonDocument>(MongoContext.TenSinhVien);
        BulkWriteResult ketQua;
        try
        {
            ketQua = await collection.BulkWriteAsync(cacLenh.Select(l => l.TaoModel()),
                new BulkWriteOptions { IsOrdered = false });
        }
        catch (MongoBulkWriteException<BsonDocument> ex)
        {
            ketQua = ex.Result;
        }

        var lenh = MongoCommandFormatter.BulkWriteUpdateOne(MongoContext.TenSinhVien, cacLenh, false, ketQua);
        var soGhi = ketQua.ModifiedCount;
        var thongBao = $"Đã ghi danh {soGhi} sinh viên môn {mon.MaMon} – {mon.TenMon} ({HienThi.HocKy(yc.NamHoc, yc.HocKy)}, {gv.HoTenKemHocVi}).";
        if (soChan > 0) thongBao += $" Bỏ qua {soChan} dòng bị chặn.";
        if (soGhi < cacLenh.Count) thongBao += $" {cacLenh.Count - soGhi} sinh viên không ghi được (vừa được ghi danh ở nơi khác).";

        return new KetQuaGhi { ThanhCong = soGhi > 0, ThongBao = thongBao, MongoCommand = lenh };
    }

    /// <summary>
    /// Hủy ghi danh – chỉ khi phần tử chưa có điểm nào (thiết kế 4.8):
    ///   db.sinhvien.updateOne(
    ///     { maSV, monHoc: { $elemMatch: { maDK, "diem.cc": null, "diem.gk": null, "diem.ck": null } } },
    ///     { $pull: { monHoc: { maDK } }, $set: { ngayCapNhat } })
    /// Điều kiện "chưa có điểm" nằm ngay trong filter → matchedCount = 0 nghĩa là đã có điểm (hoặc không tồn tại).
    /// </summary>
    public async Task<KetQuaGhi> HuyGhiDanhAsync(string maSV, ObjectId maDK)
    {
        var filter = new BsonDocument
        {
            { "maSV", maSV },
            { "monHoc", new BsonDocument("$elemMatch", new BsonDocument
                {
                    { "maDK", maDK }, { "diem.cc", BsonNull.Value }, { "diem.gk", BsonNull.Value }, { "diem.ck", BsonNull.Value }
                }) }
        };
        var update = new BsonDocument
        {
            { "$pull", new BsonDocument("monHoc", new BsonDocument("maDK", maDK)) },
            { "$set", new BsonDocument("ngayCapNhat", DateTime.UtcNow) }
        };

        var kq = await _db.Database.GetCollection<BsonDocument>(MongoContext.TenSinhVien).UpdateOneAsync(filter, update);
        var lenh = MongoCommandFormatter.UpdateOne(MongoContext.TenSinhVien, filter, update, null, kq);
        if (kq.MatchedCount == 0)
            return new KetQuaGhi { ThanhCong = false, ThongBao = "Đã có điểm, không thể hủy (hoặc lần học không còn tồn tại).", MongoCommand = lenh };
        return KetQuaGhi.Thanh($"Đã hủy ghi danh của sinh viên {maSV}.", lenh);
    }

    /// <summary>
    /// Phần tử monHoc mới – dựng BsonDocument tường minh, đúng thứ tự trường của thiết kế 4.1.
    /// tenMon / soTinChi / trongSo là ẢNH CHỤP danh mục môn lúc ghi danh; giangVien.hoTen kèm học vị;
    /// 3 điểm thành phần và capNhatLuc / capNhatBoi đều null.
    /// </summary>
    private static BsonDocument TaoPhanTuMonHoc(MonHoc mon, GiangVien gv, string namHoc, int hocKy, int lanHoc, string hinhThuc)
        => new()
        {
            { "maDK", ObjectId.GenerateNewId() },
            { "maMon", mon.MaMon },
            { "tenMon", mon.TenMon },
            { "soTinChi", mon.SoTinChi },
            { "namHoc", namHoc },
            { "hocKy", hocKy },
            { "lanHoc", lanHoc },
            { "hinhThuc", hinhThuc },
            { "giangVien", new BsonDocument { { "maGV", gv.MaGV }, { "hoTen", gv.HoTenKemHocVi } } },
            { "trongSo", new BsonDocument
                {
                    { "cc", mon.TrongSoMacDinh.Cc }, { "gk", mon.TrongSoMacDinh.Gk }, { "ck", mon.TrongSoMacDinh.Ck }
                } },
            { "diem", new BsonDocument { { "cc", BsonNull.Value }, { "gk", BsonNull.Value }, { "ck", BsonNull.Value } } },
            { "capNhatLuc", BsonNull.Value },
            { "capNhatBoi", BsonNull.Value }
        };

    /// <summary>SV được chọn: cả lớp (find { maLop }) hoặc danh sách MSSV (find { maSV: { $in } }). Không lấy mảng monHoc.</summary>
    private async Task<List<SinhVien>> LaySinhVienAsync(YeuCauGhiDanh yc)
    {
        var b = Builders<SinhVien>.Filter;
        var filter = yc.CheDo == YeuCauGhiDanh.CheDoCaLop
            ? b.Eq(sv => sv.MaLop, yc.MaLop)
            : b.In(sv => sv.MaSV, yc.DsMaSV);
        return await _db.SinhVien.Find(filter)
            .Project<SinhVien>(Builders<SinhVien>.Projection.Exclude(sv => sv.MonHoc))
            .SortBy(sv => sv.MaSV)
            .ToListAsync();
    }

    private static string? KiemTraYeuCau(YeuCauGhiDanh yc)
    {
        if (string.IsNullOrEmpty(yc.NamHoc) || !DangNamHoc.IsMatch(yc.NamHoc)) return "Vui lòng chọn năm học.";
        if (yc.HocKy is < 1 or > 3) return "Học kỳ không hợp lệ.";
        if (string.IsNullOrEmpty(yc.MaMon)) return "Vui lòng chọn môn học.";
        if (string.IsNullOrEmpty(yc.MaGV)) return "Vui lòng chọn giảng viên.";
        if (yc.CheDo == YeuCauGhiDanh.CheDoCaLop)
        {
            if (string.IsNullOrEmpty(yc.MaLop)) return "Vui lòng chọn lớp.";
        }
        else if (yc.CheDo == YeuCauGhiDanh.CheDoTungSV)
        {
            if (yc.DsMaSV.Count == 0) return "Vui lòng chọn ít nhất một sinh viên.";
        }
        else return "Vui lòng chọn cách ghi danh (cả lớp hoặc từng sinh viên).";
        return null;
    }
}
