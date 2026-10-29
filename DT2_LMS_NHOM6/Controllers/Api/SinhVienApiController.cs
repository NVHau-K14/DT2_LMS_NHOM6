using System.Globalization;
using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Models.Documents;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Controllers.Api;

/// <summary>
/// API hồ sơ sinh viên, ghi danh, nhập điểm, bảng điểm. Gọi lại SinhVienService, GhiDanhService,
/// DiemService, BaoCaoService – cùng service với A-04/A-05/A-06/A-09/G-03.
/// Quyền: Admin đọc/ghi mọi SV; GV đọc SV có học mình và nhập điểm lần học mình dạy; SV chỉ đọc chính mình.
/// </summary>
[Route("api/sinhvien")]
[Tags("Sinh viên")]
public class SinhVienApiController : ApiControllerBase
{
    private const int KichThuocTrang = 20;

    private readonly SinhVienService _sinhVien;
    private readonly GhiDanhService _ghiDanh;
    private readonly DiemService _diem;
    private readonly BaoCaoService _baoCao;
    private readonly TenLopService _tenLop;

    public SinhVienApiController(SinhVienService sinhVien, GhiDanhService ghiDanh, DiemService diem, BaoCaoService baoCao,
        TenLopService tenLop)
    {
        _sinhVien = sinhVien;
        _ghiDanh = ghiDanh;
        _diem = diem;
        _baoCao = baoCao;
        _tenLop = tenLop;
    }

    /// <summary>maLop nhận mã lớp (CNTT2501) hoặc tên lớp (16DHTH01) → quy đổi về mã lớp; không tìm thấy → giữ nguyên.</summary>
    private async Task<string?> QuyDoiMaLopAsync(string? maHoacTen)
        => string.IsNullOrWhiteSpace(maHoacTen) ? maHoacTen
            : (await _tenLop.LayAsync()).TimMaLop(maHoacTen) ?? maHoacTen;

    /// <summary>Danh sách sinh viên (phân trang 20 dòng) kèm GPA tích lũy và xếp loại.</summary>
    /// <remarks>
    /// Chỉ Admin. Hồ sơ lấy bằng find (bỏ mảng monHoc), GPA / xếp loại lấy từ pipeline BaoCaoHocLuc
    /// với bộ lọc { maSV: { $in: [MSSV của trang] } }.
    /// </remarks>
    /// <param name="maLop">Lớp sinh hoạt: mã lớp (CNTT2501) hoặc tên lớp (16DHTH01); bỏ trống = mọi lớp.</param>
    /// <param name="page">Số trang, bắt đầu từ 1.</param>
    /// <response code="200">Một trang danh sách.</response>
    [HttpGet]
    [Authorize(Roles = HangSo.VaiTro.Admin)]
    [ProducesResponseType(typeof(TrangSinhVienApi), StatusCodes.Status200OK)]
    public async Task<ActionResult<TrangSinhVienApi>> DanhSach(string? maLop, int page = 1)
    {
        page = Math.Max(1, page);
        maLop = await QuyDoiMaLopAsync(maLop);
        var bangLop = await _tenLop.LayAsync();
        var (danhSach, tongSo) = await _sinhVien.DanhSachAsync(maLop, null, null, page, KichThuocTrang);

        var dsMaSV = danhSach.Select(sv => sv.MaSV).ToList();
        var hocLuc = dsMaSV.Count == 0
            ? new Dictionary<string, Models.Results.BaoCaoHocLucRow>()
            : (await _baoCao.BaoCaoHocLucAsync(BoLocSinhVien.TheoDanhSach(dsMaSV), PhamViBaoCao.TichLuy()))
                .DuLieu.ToDictionary(r => r.MaSV);

        return new TrangSinhVienApi
        {
            Trang = page,
            KichThuoc = KichThuocTrang,
            TongSo = tongSo,
            TongSoTrang = (int)Math.Max(1, (tongSo + KichThuocTrang - 1) / KichThuocTrang),
            DuLieu = danhSach.Select(sv =>
            {
                var hl = hocLuc.GetValueOrDefault(sv.MaSV);
                return new SinhVienTomTatApi
                {
                    MaSV = sv.MaSV, HoTen = sv.HoTen, NgaySinh = sv.NgaySinh, GioiTinh = sv.GioiTinh,
                    Email = sv.Email, SoDienThoai = sv.SoDienThoai, DiaChi = sv.DiaChi,
                    MaLop = sv.MaLop, TenLop = bangLop.Ten(sv.MaLop), TrangThai = sv.TrangThai,
                    Gpa = hl?.Gpa, XepLoai = hl?.XepLoai,
                    TinChiTichLuy = hl?.TinChiTichLuy ?? 0, SoMonNo = hl?.SoMonNo ?? 0
                };
            }).ToList()
        };
    }

    /// <summary>Hồ sơ một sinh viên – nguyên document trong collection sinhvien, kèm mảng monHoc nhúng.</summary>
    /// <remarks>Admin: mọi SV. Giảng viên: SV có ít nhất một lần học do mình dạy. Sinh viên: chỉ chính mình.</remarks>
    /// <param name="maSV">MSSV 10 chữ số, ví dụ 2001250001.</param>
    /// <response code="200">Document sinh viên.</response>
    /// <response code="403">Không có quyền xem sinh viên này.</response>
    /// <response code="404">Không tìm thấy sinh viên.</response>
    [HttpGet("{maSV}")]
    [ProducesResponseType(typeof(SinhVien), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChiTiet(string maSV)
    {
        if (!await DuocDocSinhVienAsync(maSV))
            return Loi(StatusCodes.Status403Forbidden, "Bạn không có quyền xem sinh viên này.");
        var sv = await _sinhVien.LayAsync(maSV);
        return sv == null ? Loi(StatusCodes.Status404NotFound, $"Không tìm thấy sinh viên {maSV}.") : Ok(sv);
    }

    /// <summary>Thêm sinh viên (monHoc rỗng) và tạo tài khoản đăng nhập (mật khẩu = ngày sinh ddMMyyyy).</summary>
    /// <remarks>
    /// Chỉ Admin. Ghi tuần tự insertOne sinhvien → insertOne taikhoan (không có transaction; bước 2 lỗi thì xóa SV vừa thêm).
    /// Ví dụ body:
    ///
    ///     { "maSV": "2001259999", "hoTen": "Trần Thị Thử", "ngaySinh": "2007-05-20", "gioiTinh": "Nữ",
    ///       "maLop": "CNTT2501", "trangThai": "Đang học" }
    ///
    /// maLop nhận mã lớp (CNTT2501) hoặc tên lớp (16DHTH01); luôn lưu mã lớp.
    /// </remarks>
    /// <response code="201">Đã thêm; duLieu = document vừa tạo, mongoCommand = các lệnh đã chạy.</response>
    /// <response code="400">Dữ liệu không hợp lệ hoặc MSSV đã tồn tại.</response>
    [HttpPost]
    [Authorize(Roles = HangSo.VaiTro.Admin)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Them(SinhVienFormViewModel form)
    {
        form.LaSua = false;
        form.MaLop = await QuyDoiMaLopAsync(form.MaLop) ?? "";
        var kq = await _sinhVien.ThemAsync(form);
        if (!kq.ThanhCong) return KetQua(kq);

        var maSV = form.MaSV.Trim();
        Response.Headers.Location = $"/api/sinhvien/{maSV}";
        return KetQua(kq, await _sinhVien.LayAsync(maSV), StatusCodes.Status201Created);
    }

    /// <summary>Sửa hồ sơ sinh viên (không đổi MSSV, không đụng mảng monHoc).</summary>
    /// <remarks>
    /// Chỉ Admin. Body giống POST; trường maSV phải trùng MSSV trên đường dẫn.
    /// Lệnh: db.sinhvien.updateOne({ maSV }, { $set: { hoTen, ngaySinh, ..., ngayCapNhat } }); đổi họ tên → cập nhật taikhoan.hoTen.
    /// </remarks>
    /// <response code="200">Đã cập nhật; mongoCommand = lệnh đã chạy.</response>
    /// <response code="400">Dữ liệu không hợp lệ.</response>
    /// <response code="404">Không tìm thấy sinh viên.</response>
    [HttpPut("{maSV}")]
    [Authorize(Roles = HangSo.VaiTro.Admin)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sua(string maSV, SinhVienFormViewModel form)
    {
        if (form.MaSV.Trim() != maSV)
            return Loi(StatusCodes.Status400BadRequest, "Không được đổi MSSV: trường maSV trong body phải trùng MSSV trên đường dẫn.");
        if (await _sinhVien.LayAsync(maSV) == null)
            return Loi(StatusCodes.Status404NotFound, $"Không tìm thấy sinh viên {maSV}.");

        form.LaSua = true;
        form.MaSV = maSV;
        form.MaLop = await QuyDoiMaLopAsync(form.MaLop) ?? "";
        var kq = await _sinhVien.SuaAsync(maSV, form);
        return KetQua(kq, kq.ThanhCong ? await _sinhVien.LayAsync(maSV) : null);
    }

    /// <summary>Xóa sinh viên và tài khoản đăng nhập của sinh viên đó.</summary>
    /// <remarks>Chỉ Admin. deleteOne sinhvien → deleteOne taikhoan (bước 2 lỗi thì chèn lại hồ sơ).</remarks>
    /// <response code="200">Đã xóa; mongoCommand = các lệnh đã chạy.</response>
    /// <response code="404">Không tìm thấy sinh viên.</response>
    [HttpDelete("{maSV}")]
    [Authorize(Roles = HangSo.VaiTro.Admin)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Xoa(string maSV)
    {
        if (await _sinhVien.LayAsync(maSV) == null)
            return Loi(StatusCodes.Status404NotFound, $"Không tìm thấy sinh viên {maSV}.");
        return KetQua(await _sinhVien.XoaAsync(maSV));
    }

    /// <summary>Ghi danh sinh viên vào một lớp học phần – $push một phần tử mới vào mảng monHoc.</summary>
    /// <remarks>
    /// Chỉ Admin. Cùng quy tắc với A-09: lanHoc và hinhThuc (Học mới / Học lại / Cải thiện) nhận diện từ pipeline BangDiem;
    /// bị chặn nếu đã ghi danh môn này trong học kỳ đó, lần học trước chưa có kết quả, hoặc SV không "Đang học".
    /// Ví dụ body:
    ///
    ///     { "namHoc": "2026-2027", "hocKy": 1, "maMon": "INT301", "maGV": "GV005" }
    /// </remarks>
    /// <param name="maSV">MSSV cần ghi danh.</param>
    /// <response code="201">Đã ghi danh; duLieu = lần học mới (có maDK để nhập điểm), mongoCommand = lệnh $push.</response>
    /// <response code="400">Thiếu tham số hoặc bị chặn (kèm lý do).</response>
    /// <response code="404">Không tìm thấy sinh viên.</response>
    [HttpPost("{maSV}/monhoc")]
    [Authorize(Roles = HangSo.VaiTro.Admin)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GhiDanh(string maSV, GhiDanhApiForm form)
    {
        if (await _sinhVien.LayAsync(maSV) == null)
            return Loi(StatusCodes.Status404NotFound, $"Không tìm thấy sinh viên {maSV}.");

        var yc = new YeuCauGhiDanh
        {
            NamHoc = form.NamHoc.Trim(), HocKy = form.HocKy,
            MaMon = form.MaMon.Trim().ToUpperInvariant(), MaGV = form.MaGV.Trim().ToUpperInvariant(),
            CheDo = YeuCauGhiDanh.CheDoTungSV, DsMaSV = [maSV]
        };

        // Xem trước để trả đúng lý do bị chặn; bước xác nhận vẫn tự chạy lại kiểm tra ở service
        var xemTruoc = await _ghiDanh.XemTruocAsync(yc);
        if (xemTruoc.Loi != null) return Loi(StatusCodes.Status400BadRequest, xemTruoc.Loi);
        var dong = xemTruoc.Dong.First();
        if (!dong.HopLe) return Loi(StatusCodes.Status400BadRequest, $"Không thể ghi danh: {dong.LyDo}");

        var kq = await _ghiDanh.XacNhanAsync(yc);
        var lanHocMoi = kq.ThanhCong
            ? (await _diem.BangDiemAsync(maSV)).DuLieu.FirstOrDefault(m =>
                m.MaMon == yc.MaMon && m.NamHoc == yc.NamHoc && m.HocKy == yc.HocKy)
            : null;
        return KetQua(kq, lanHocMoi, StatusCodes.Status201Created);
    }

    /// <summary>Nhập điểm một lần học – $set + arrayFilters vào đúng phần tử monHoc có maDK.</summary>
    /// <remarks>
    /// Giảng viên (chỉ lần học mình dạy – điều kiện "e.giangVien.maGV" nằm trong arrayFilters) hoặc Admin.
    /// Gửi đủ 3 thành phần; null = xóa điểm. Mỗi điểm 0–10, tối đa 1 chữ số thập phân. Ví dụ body:
    ///
    ///     { "cc": 9, "gk": 8, "ck": 8.5 }
    ///
    /// Sau khi ghi, chạy lại pipeline BangDiem để trả điểm TK / chữ / hệ 4 mới.
    /// </remarks>
    /// <param name="maSV">MSSV.</param>
    /// <param name="maDK">Mã lần học (ObjectId 24 ký tự hex – lấy từ GET .../bangdiem).</param>
    /// <response code="200">Đã lưu; duLieu = dòng điểm sau khi quy đổi, mongoCommand = lệnh updateOne.</response>
    /// <response code="400">Điểm không hợp lệ.</response>
    /// <response code="404">Không có quyền hoặc không tìm thấy lần học (modifiedCount = 0).</response>
    [HttpPut("{maSV}/monhoc/{maDK}/diem")]
    [Authorize(Roles = HangSo.VaiTro.Admin + "," + HangSo.VaiTro.GiangVien)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> NhapDiem(string maSV, string maDK, DiemApiForm form)
    {
        // Admin nhập: maGV = null → bỏ điều kiện giảng viên, capNhatBoi "admin" (giống nhập Excel của Admin)
        var maGV = VaiTro == HangSo.VaiTro.GiangVien ? MaThamChieu : null;

        // Đổi số về chuỗi để đi qua đúng bộ kiểm tra DiemNhap của G-03 (0–10, tối đa 1 chữ số thập phân)
        static string? Chuoi(double? d) => d?.ToString(CultureInfo.InvariantCulture);
        var kq = await _diem.CapNhatDiemAsync(maGV, new DongLuuDiem
        {
            MaSV = maSV, MaDK = maDK, Cc = Chuoi(form.Cc), Gk = Chuoi(form.Gk), Ck = Chuoi(form.Ck)
        });

        var body = new KetQuaApi
        {
            ThanhCong = kq.ThanhCong,
            ThongBao = kq.ThongBao,
            MongoCommand = kq.MongoCommand,
            DuLieu = kq.Dong.FirstOrDefault()
        };
        // Lệnh đã chạy mà không sửa được (modifiedCount = 0) → 404; chưa chạy (điểm sai) → 400
        var maTrangThai = kq.ThanhCong ? StatusCodes.Status200OK
                        : kq.MongoCommand != null ? StatusCodes.Status404NotFound
                        : StatusCodes.Status400BadRequest;
        return StatusCode(maTrangThai, body);
    }

    /// <summary>Bảng điểm một sinh viên: từng lần học đã quy đổi, GPA từng học kỳ, tổng kết tích lũy.</summary>
    /// <remarks>
    /// Toàn bộ điểm TK / chữ / hệ 4 / đạt / GPA / xếp loại do pipeline tính (BangDiem, GpaHocKy, BaoCaoHocLuc).
    /// Quyền đọc như GET /api/sinhvien/{maSV}.
    /// </remarks>
    /// <response code="200">Bảng điểm.</response>
    /// <response code="403">Không có quyền xem sinh viên này.</response>
    /// <response code="404">Không tìm thấy sinh viên.</response>
    [HttpGet("{maSV}/bangdiem")]
    [ProducesResponseType(typeof(BangDiemApi), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(KetQuaApi), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BangDiem(string maSV)
    {
        if (!await DuocDocSinhVienAsync(maSV))
            return Loi(StatusCodes.Status403Forbidden, "Bạn không có quyền xem sinh viên này.");
        var sv = await _sinhVien.LayAsync(maSV);
        if (sv == null) return Loi(StatusCodes.Status404NotFound, $"Không tìm thấy sinh viên {maSV}.");

        var tenLop = (await _tenLop.LayAsync()).Ten(sv.MaLop);
        var tongKet = (await _baoCao.BaoCaoHocLucAsync(BoLocSinhVien.TheoSinhVien(maSV), PhamViBaoCao.TichLuy()))
            .DuLieu.FirstOrDefault();
        if (tongKet != null) tongKet.TenLop = tenLop;

        return Ok(new BangDiemApi
        {
            MaSV = sv.MaSV,
            HoTen = sv.HoTen,
            MaLop = sv.MaLop,
            TenLop = tenLop,
            LanHoc = (await _diem.BangDiemAsync(maSV)).DuLieu,
            GpaHocKy = (await _diem.GpaHocKyAsync(maSV)).DuLieu,
            TongKet = tongKet
        });
    }

    /// <summary>Quyền đọc một SV: Admin mọi SV; SV chỉ chính mình (MSSV từ claim); GV chỉ SV có học mình.</summary>
    private async Task<bool> DuocDocSinhVienAsync(string maSV)
    {
        if (VaiTro == HangSo.VaiTro.Admin) return true;
        if (VaiTro == HangSo.VaiTro.SinhVien) return maSV == MaThamChieu;
        if (VaiTro == HangSo.VaiTro.GiangVien) return await _sinhVien.CoLanHocCuaGiangVienAsync(maSV, MaThamChieu);
        return false;
    }
}
