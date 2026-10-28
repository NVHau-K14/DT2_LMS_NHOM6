using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Báo cáo học lực: GPA + xếp loại từng SV, thống kê theo xếp loại, GPA trung bình theo lớp.
/// Toàn bộ tính ở tầng MongoDB (pipeline); service chỉ chọn tham số và trả kết quả.
/// </summary>
public class BaoCaoService
{
    private readonly MongoContext _db;

    public BaoCaoService(MongoContext db)
    {
        _db = db;
    }

    /// <summary>Mỗi SV một dòng (kể cả SV chưa có điểm → "Chưa xếp loại"), sắp theo MSSV.</summary>
    public Task<KetQuaPipeline<BaoCaoHocLucRow>> BaoCaoHocLucAsync(BoLocSinhVien boLoc, PhamViBaoCao phamVi)
        => PipelineRunner.ChayAsync<BaoCaoHocLucRow>(_db, BaoCaoHocLucPipeline.Tao(boLoc, phamVi));

    /// <summary>Số SV theo xếp loại (thứ tự Xuất sắc → Chưa xếp loại), kèm chi tiết theo lớp.</summary>
    public Task<KetQuaPipeline<ThongKeXepLoaiRow>> ThongKeXepLoaiAsync(BoLocSinhVien boLoc, PhamViBaoCao phamVi)
        => PipelineRunner.ChayAsync<ThongKeXepLoaiRow>(_db, ThongKeXepLoaiPipeline.Tao(boLoc, phamVi));

    /// <summary>GPA tích lũy trung bình và sĩ số mỗi lớp (toàn trường).</summary>
    public Task<KetQuaPipeline<GpaTheoLopRow>> GpaTheoLopAsync()
        => PipelineRunner.ChayAsync<GpaTheoLopRow>(_db, GpaTheoLopPipeline.Tao());

    /// <summary>Các học kỳ đã có lần học (pipeline HocKyCoDuLieu), mới nhất trước – cho ô chọn phạm vi A-10.</summary>
    public async Task<List<HocKyRow>> HocKyCoDuLieuAsync()
        => (await PipelineRunner.ChayAsync<HocKyRow>(_db, ThongKeDanhMucPipeline.HocKyCoDuLieu())).DuLieu;

    /// <summary>
    /// 4 thẻ số A-01: đếm document mỗi collection.
    /// Tương đương: db.lophoc.countDocuments({}), db.sinhvien.countDocuments({}), ...
    /// </summary>
    public async Task<(long SoLop, long SoSV, long SoGV, long SoMon)> DemDanhMucAsync()
    {
        var soLop = await _db.LopHoc.CountDocumentsAsync(FilterDefinition<LopHoc>.Empty);
        var soSV = await _db.SinhVien.CountDocumentsAsync(FilterDefinition<SinhVien>.Empty);
        var soGV = await _db.GiangVien.CountDocumentsAsync(FilterDefinition<GiangVien>.Empty);
        var soMon = await _db.MonHoc.CountDocumentsAsync(FilterDefinition<MonHoc>.Empty);
        return (soLop, soSV, soGV, soMon);
    }
}
