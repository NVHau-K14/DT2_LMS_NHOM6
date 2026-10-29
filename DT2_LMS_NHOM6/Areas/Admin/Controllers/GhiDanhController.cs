using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Areas.Admin.Controllers;

/// <summary>
/// Ghi danh (A-09): B1 chọn học phần → B2 chọn cả lớp / từng SV → B3 xem trước → B4 xác nhận (bulkWrite $push).
/// Xem trước là thao tác chỉ đọc nên dùng GET (form B1+B2 gửi bằng query string); xác nhận là POST.
/// Hủy ghi danh ($pull) gọi từ hồ sơ SV (A-06).
/// </summary>
public class GhiDanhController : AdminControllerBase
{
    private readonly GhiDanhService _ghiDanh;
    private readonly MonHocService _monHoc;
    private readonly GiangVienService _giangVien;
    private readonly LopHocService _lopHoc;
    private readonly SinhVienService _sinhVien;
    private readonly HocKyHienTaiOptions _hocKyHienTai;

    public GhiDanhController(GhiDanhService ghiDanh, MonHocService monHoc, GiangVienService giangVien,
        LopHocService lopHoc, SinhVienService sinhVien, IOptions<HocKyHienTaiOptions> hocKyHienTai)
    {
        _ghiDanh = ghiDanh;
        _monHoc = monHoc;
        _giangVien = giangVien;
        _lopHoc = lopHoc;
        _sinhVien = sinhVien;
        _hocKyHienTai = hocKyHienTai.Value;
    }

    /// <summary>
    /// /Admin/GhiDanh?maLop=CNTT2502 (từ chi tiết lớp 16DHTH02) hoặc ?maSV=2001250004 (từ hồ sơ SV) để điền sẵn.
    /// xemTruoc=true → chạy bước xem trước (không ghi DB).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] YeuCauGhiDanh yc, string? maSV, bool xemTruoc = false)
    {
        // Mặc định học kỳ hiện tại (appsettings.json → HocKyHienTai)
        if (string.IsNullOrEmpty(yc.NamHoc)) yc.NamHoc = _hocKyHienTai.NamHoc;
        if (yc.HocKy == 0) yc.HocKy = _hocKyHienTai.HocKy;
        if (!string.IsNullOrEmpty(maSV) && yc.DsMaSV.Count == 0)
        {
            yc.CheDo = YeuCauGhiDanh.CheDoTungSV;
            yc.DsMaSV.Add(maSV.Trim());
        }
        yc.DsMaSV = yc.DsMaSV.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct().ToList();

        return View(new GhiDanhViewModel
        {
            YeuCau = yc,
            DsMon = await _monHoc.TatCaAsync(),
            DsGV = await _giangVien.TatCaAsync(),
            DsLop = await _lopHoc.TatCaAsync(),
            DsSinhVien = await _sinhVien.TatCaRutGonAsync(),
            XemTruoc = xemTruoc ? await _ghiDanh.XemTruocAsync(yc) : null
        });
    }

    /// <summary>B4: ghi các dòng hợp lệ (service tự chạy lại xem trước), rồi quay về trang xem trước của cùng lựa chọn.</summary>
    [HttpPost]
    public async Task<IActionResult> XacNhan(YeuCauGhiDanh yc)
    {
        ThongBaoKetQua(await _ghiDanh.XacNhanAsync(yc));
        return Redirect("/Admin/GhiDanh" + TaoQuery(yc));
    }

    /// <summary>Hủy ghi danh một lần học chưa có điểm ($pull) – gọi từ hồ sơ SV.</summary>
    [HttpPost]
    public async Task<IActionResult> Huy(string maSV, string maDK, string? quayLai)
    {
        var ketQua = ObjectId.TryParse(maDK, out var oid)
            ? await _ghiDanh.HuyGhiDanhAsync(maSV, oid)
            : KetQuaGhi.Loi("Mã lần học không hợp lệ.");
        ThongBaoKetQua(ketQua);
        return QuayLai(quayLai, $"/Admin/SinhVien/HoSo/{Uri.EscapeDataString(maSV)}");
    }

    /// <summary>Query string giữ nguyên lựa chọn B1 + B2, kèm xemTruoc=true (DsMaSV lặp nhiều lần).</summary>
    private static QueryString TaoQuery(YeuCauGhiDanh yc)
    {
        var thamSo = new List<KeyValuePair<string, string?>>
        {
            new(nameof(yc.NamHoc), yc.NamHoc),
            new(nameof(yc.HocKy), yc.HocKy.ToString()),
            new(nameof(yc.MaMon), yc.MaMon),
            new(nameof(yc.MaGV), yc.MaGV),
            new(nameof(yc.CheDo), yc.CheDo)
        };
        if (yc.CheDo == YeuCauGhiDanh.CheDoCaLop)
            thamSo.Add(new(nameof(yc.MaLop), yc.MaLop));
        else
            thamSo.AddRange(yc.DsMaSV.Select(m => new KeyValuePair<string, string?>(nameof(yc.DsMaSV), m)));
        thamSo.Add(new("xemTruoc", "true"));
        return QueryString.Create(thamSo);
    }
}
