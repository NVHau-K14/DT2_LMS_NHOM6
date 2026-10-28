using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Đọc điểm đã quy đổi – mọi con số (TK, chữ, hệ 4, đạt, GPA, xếp loại) đều do pipeline tính.
/// Dùng cho: bảng điểm SV, GPA theo học kỳ, lớp học phần của GV, danh sách nhập điểm.
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

    /// <summary>SV của một lớp học phần kèm điểm; maGV = null khi Admin xem.</summary>
    public Task<KetQuaPipeline<DongNhapDiem>> DanhSachNhapDiemAsync(string? maGV, string maMon, string namHoc, int hocKy)
        => PipelineRunner.ChayAsync<DongNhapDiem>(_db, DanhSachNhapDiemPipeline.Tao(maGV, maMon, namHoc, hocKy));
}
