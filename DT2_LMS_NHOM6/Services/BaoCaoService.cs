using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;

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
}
