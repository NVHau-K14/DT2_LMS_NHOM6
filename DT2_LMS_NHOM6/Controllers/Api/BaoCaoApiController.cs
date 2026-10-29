using DT2_LMS_NHOM6.Models;
using DT2_LMS_NHOM6.Pipelines;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DT2_LMS_NHOM6.Controllers.Api;

/// <summary>API báo cáo học lực – gọi lại BaoCaoService (cùng pipeline BaoCaoHocLuc với A-10 / G-04).</summary>
[Route("api/baocao")]
[Tags("Báo cáo")]
[Authorize(Roles = HangSo.VaiTro.Admin + "," + HangSo.VaiTro.GiangVien)]
public class BaoCaoApiController : ApiControllerBase
{
    private readonly BaoCaoService _baoCao;
    private readonly TenLopService _tenLop;

    public BaoCaoApiController(BaoCaoService baoCao, TenLopService tenLop)
    {
        _baoCao = baoCao;
        _tenLop = tenLop;
    }

    /// <summary>Báo cáo học lực tích lũy: GPA hệ 4, xếp loại, tín chỉ tích lũy, số môn nợ của từng sinh viên.</summary>
    /// <remarks>
    /// Tính hoàn toàn bằng Aggregation Pipeline BaoCaoHocLuc ($unwind, $group, $project, $switch...).
    /// Admin: toàn trường hoặc một lớp. Giảng viên: chỉ SV có học mình (bộ lọc { "monHoc.giangVien.maGV": maGV } lấy từ claim), có thể lọc thêm lớp.
    /// </remarks>
    /// <param name="maLop">Lớp sinh hoạt: mã lớp (CNTT2501) hoặc tên lớp (16DHTH01); bỏ trống = tất cả.</param>
    /// <response code="200">Danh sách học lực + thời gian chạy pipeline (ms).</response>
    [HttpGet("hocluc")]
    [ProducesResponseType(typeof(BaoCaoHocLucApi), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaoCaoHocLucApi>> HocLuc(string? maLop)
    {
        // Nhận mã lớp (CNTT2501) hoặc tên lớp (16DHTH01) → luôn lọc pipeline theo maLop (lớp không có → danh sách rỗng)
        var bangLop = await _tenLop.LayAsync();
        maLop = string.IsNullOrWhiteSpace(maLop) ? null : bangLop.TimMaLop(maLop) ?? maLop.Trim().ToUpperInvariant();

        var boLoc = VaiTro == HangSo.VaiTro.GiangVien
            ? BoLocSinhVien.TheoGiangVien(MaThamChieu)
            : BoLocSinhVien.TatCa();
        if (maLop != null) boLoc = boLoc.VaLop(maLop);

        var kq = await _baoCao.BaoCaoHocLucAsync(boLoc, PhamViBaoCao.TichLuy());
        foreach (var dong in kq.DuLieu) dong.TenLop = bangLop.Ten(dong.MaLop);
        return new BaoCaoHocLucApi
        {
            MaLop = maLop,
            TenLop = maLop == null ? null : bangLop.Ten(maLop),
            SoSinhVien = kq.DuLieu.Count,
            ThoiGianMs = Math.Round(kq.ThoiGianMs, 1),
            DuLieu = kq.DuLieu
        };
    }
}
