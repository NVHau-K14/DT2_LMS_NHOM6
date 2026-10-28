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

    public BaoCaoApiController(BaoCaoService baoCao)
    {
        _baoCao = baoCao;
    }

    /// <summary>Báo cáo học lực tích lũy: GPA hệ 4, xếp loại, tín chỉ tích lũy, số môn nợ của từng sinh viên.</summary>
    /// <remarks>
    /// Tính hoàn toàn bằng Aggregation Pipeline BaoCaoHocLuc ($unwind, $group, $project, $switch...).
    /// Admin: toàn trường hoặc một lớp. Giảng viên: chỉ SV có học mình (bộ lọc { "monHoc.giangVien.maGV": maGV } lấy từ claim), có thể lọc thêm lớp.
    /// </remarks>
    /// <param name="maLop">Mã lớp sinh hoạt, ví dụ 16DHTH01 (bỏ trống = tất cả).</param>
    /// <response code="200">Danh sách học lực + thời gian chạy pipeline (ms).</response>
    [HttpGet("hocluc")]
    [ProducesResponseType(typeof(BaoCaoHocLucApi), StatusCodes.Status200OK)]
    public async Task<ActionResult<BaoCaoHocLucApi>> HocLuc(string? maLop)
    {
        maLop = string.IsNullOrWhiteSpace(maLop) ? null : maLop.Trim().ToUpperInvariant();

        var boLoc = VaiTro == HangSo.VaiTro.GiangVien
            ? BoLocSinhVien.TheoGiangVien(MaThamChieu)
            : BoLocSinhVien.TatCa();
        if (maLop != null) boLoc = boLoc.VaLop(maLop);

        var kq = await _baoCao.BaoCaoHocLucAsync(boLoc, PhamViBaoCao.TichLuy());
        return new BaoCaoHocLucApi
        {
            MaLop = maLop,
            SoSinhVien = kq.DuLieu.Count,
            ThoiGianMs = Math.Round(kq.ThoiGianMs, 1),
            DuLieu = kq.DuLieu
        };
    }
}
