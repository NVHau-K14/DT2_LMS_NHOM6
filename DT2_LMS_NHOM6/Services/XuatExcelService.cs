using DT2_LMS_NHOM6.Models.Results;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.ViewModels;

namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Xuất Excel (thiết kế mục 6): lấy dữ liệu bằng ĐÚNG các pipeline / truy vấn mà màn hình đang dùng
/// rồi giao cho ExcelService dựng file. Nhờ vậy số liệu trong file luôn khớp số liệu trên web.
/// </summary>
public class XuatExcelService
{
    private readonly DiemService _diem;
    private readonly BaoCaoService _baoCao;
    private readonly SinhVienService _sinhVien;
    private readonly LopHocService _lopHoc;

    public XuatExcelService(DiemService diem, BaoCaoService baoCao, SinhVienService sinhVien, LopHocService lopHoc)
    {
        _diem = diem;
        _baoCao = baoCao;
        _sinhVien = sinhVien;
        _lopHoc = lopHoc;
    }

    /// <summary>"HK1", "HK2", "HKhe" – phần học kỳ trong tên file.</summary>
    public static string HocKyTrongTenFile(int hocKy) => hocKy == 3 ? "HKhe" : $"HK{hocKy}";

    /// <summary>
    /// Bảng điểm một lớp học phần (pipeline DanhSachNhapDiem) – vừa là file xuất, vừa là file mẫu nhập điểm.
    /// null nếu lớp học phần không tồn tại (hoặc không phải của GV này).
    /// </summary>
    public async Task<TepExcel?> BangDiemLopHocPhanAsync(string maGV, string maMon, string namHoc, int hocKy)
    {
        var lhp = await _diem.LayLopHocPhanAsync(maGV, maMon, namHoc, hocKy);
        if (lhp == null) return null;
        var ds = (await _diem.DanhSachNhapDiemAsync(maGV, maMon, namHoc, hocKy)).DuLieu;

        var thongTin = new ThongTinLopHocPhanExcel
        {
            MaMon = lhp.MaMon,
            TenMon = lhp.TenMon,
            NamHoc = lhp.NamHoc,
            HocKy = lhp.HocKy,
            MaGV = maGV,
            TenGV = ds.FirstOrDefault()?.GiangVien.HoTen ?? maGV,
            TrongSo = lhp.TrongSo
        };
        return new TepExcel(
            ExcelService.TenFile("BangDiem", maMon, namHoc, HocKyTrongTenFile(hocKy), maGV),
            ExcelService.XuatBangDiemLopHocPhan(thongTin, ds));
    }

    /// <summary>Báo cáo học lực theo bộ lọc + phạm vi đang chọn (pipeline BaoCaoHocLuc + ThongKeXepLoai).</summary>
    public async Task<TepExcel> BaoCaoHocLucAsync(BoLocSinhVien boLoc, PhamViBaoCao phamVi,
        List<(string Nhan, string GiaTri)> thongTinLoc, params string?[] phanTenFile)
    {
        var ds = (await _baoCao.BaoCaoHocLucAsync(boLoc, phamVi)).DuLieu;
        var thongKe = BaoCaoHocLucViewModel.DuBayMuc((await _baoCao.ThongKeXepLoaiAsync(boLoc, phamVi)).DuLieu);
        return new TepExcel(
            ExcelService.TenFile(["BaoCaoHocLuc", .. phanTenFile]),
            ExcelService.XuatBaoCaoHocLuc("BÁO CÁO HỌC LỰC", thongTinLoc, phamVi.LaTichLuy, ds, thongKe));
    }

    /// <summary>Bảng điểm cá nhân (pipeline BangDiem, GpaHocKy, BaoCaoHocLuc – giống S-02 / A-06). null nếu không có SV.</summary>
    public async Task<TepExcel?> BangDiemCaNhanAsync(string maSV)
    {
        var sv = await _sinhVien.LayAsync(maSV);
        if (sv == null) return null;

        var bangDiem = await _diem.BangDiemAsync(maSV);
        var gpaHocKy = await _diem.GpaHocKyAsync(maSV);
        var tongKet = (await _baoCao.BaoCaoHocLucAsync(BoLocSinhVien.TheoSinhVien(maSV), PhamViBaoCao.TichLuy()))
            .DuLieu.FirstOrDefault();
        var lop = await _lopHoc.LayAsync(sv.MaLop);

        return new TepExcel(
            ExcelService.TenFile("BangDiem", maSV),
            ExcelService.XuatBangDiemCaNhan(sv, lop?.TenLop, BangDiemViewModel.Tao(bangDiem.DuLieu, gpaHocKy.DuLieu, tongKet)));
    }

    /// <summary>Danh sách SV theo bộ lọc A-04 (không phân trang), kèm GPA / xếp loại tích lũy (pipeline BaoCaoHocLuc).</summary>
    public async Task<TepExcel> DanhSachSinhVienAsync(string? maLop, string? trangThai, string? q)
    {
        var ds = await _sinhVien.DanhSachTatCaAsync(maLop, trangThai, q);
        var hocLuc = ds.Count == 0
            ? new Dictionary<string, BaoCaoHocLucRow>()
            : (await _baoCao.BaoCaoHocLucAsync(BoLocSinhVien.TheoDanhSach(ds.Select(sv => sv.MaSV)), PhamViBaoCao.TichLuy()))
                .DuLieu.ToDictionary(r => r.MaSV);

        var thongTinLoc = new List<(string, string)>
        {
            ("Lớp", string.IsNullOrEmpty(maLop) ? "Tất cả lớp" : maLop),
            ("Trạng thái", string.IsNullOrEmpty(trangThai) ? "Tất cả" : trangThai)
        };
        if (!string.IsNullOrWhiteSpace(q)) thongTinLoc.Add(("Từ khóa tìm", q.Trim()));

        return new TepExcel(
            ExcelService.TenFile("DanhSachSinhVien", string.IsNullOrEmpty(maLop) ? "TatCa" : maLop),
            ExcelService.XuatDanhSachSinhVien(thongTinLoc, ds.Select(sv => (sv, hocLuc.GetValueOrDefault(sv.MaSV))).ToList()));
    }

    /// <summary>File mẫu nhập danh sách SV (kèm mã lớp hợp lệ ở sheet hướng dẫn).</summary>
    public async Task<TepExcel> MauSinhVienAsync()
        => new(ExcelService.TenFile("MauNhapSinhVien"), ExcelService.MauDanhSachSinhVien(await _lopHoc.TatCaAsync()));
}
