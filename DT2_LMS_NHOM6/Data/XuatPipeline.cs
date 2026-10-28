using System.Text;
using DT2_LMS_NHOM6.Pipelines;
using MongoDB.Bson;

namespace DT2_LMS_NHOM6.Data;

/// <summary>
/// Lệnh: dotnet run --project DT2_LMS_NHOM6 -- --export-pipelines
/// Ghi mọi pipeline (đã thay tham số mẫu) ra database/pipelines/&lt;Ten&gt;.json để dán vào Compass
/// (tab Aggregations → nút "&lt;/&gt;" Text) hoặc chạy bằng mongosh.
/// Dùng đúng các hàm Tao(...) mà service dùng → file luôn khớp code.
/// </summary>
public static class XuatPipeline
{
    // Tham số mẫu (GD3.md mục 6)
    private const string MaSVMau = "2001250001";
    private const string MaGVMau = "GV005";
    private const string MaMonMau = "INT301";
    private const string NamHocMau = "2026-2027";
    private const int HocKyMau = 1;
    private const string MaLopMau = "16DHTH01";

    public static async Task ChayAsync(string thuMucGoc)
    {
        var thuMuc = Path.Combine(thuMucGoc, "database", "pipelines");
        Directory.CreateDirectory(thuMuc);

        var danhSach = new (string Ten, BsonDocument[] Pipeline)[]
        {
            ("BangDiem", BangDiemPipeline.Tao(MaSVMau)),
            ("GpaHocKy", GpaHocKyPipeline.Tao(BoLocSinhVien.TheoSinhVien(MaSVMau))),
            ("BaoCaoHocLuc", BaoCaoHocLucPipeline.Tao(BoLocSinhVien.TatCa(), PhamViBaoCao.TichLuy())),
            // Phạm vi một học kỳ: dùng HK2 2025-2026 vì HK1 2026-2027 chưa có điểm CK (mọi SV sẽ "Chưa xếp loại")
            ("BaoCaoHocLuc_HocKy", BaoCaoHocLucPipeline.Tao(BoLocSinhVien.TheoLop(MaLopMau), PhamViBaoCao.TheoHocKy("2025-2026", 2))),
            ("ThongKeXepLoai", ThongKeXepLoaiPipeline.Tao(BoLocSinhVien.TatCa(), PhamViBaoCao.TichLuy())),
            ("GpaTheoLop", GpaTheoLopPipeline.Tao()),
            ("LopHocPhanCuaGV", LopHocPhanCuaGVPipeline.Tao(MaGVMau, NamHocMau, HocKyMau)),
            ("DanhSachNhapDiem", DanhSachNhapDiemPipeline.Tao(MaGVMau, MaMonMau, NamHocMau, HocKyMau)),
        };

        foreach (var (ten, pipeline) in danhSach)
        {
            var duongDan = Path.Combine(thuMuc, ten + ".json");
            await File.WriteAllTextAsync(duongDan, PipelineJson.ToJson(pipeline) + "\n", new UTF8Encoding(false));
            Console.WriteLine($"  Đã ghi {Path.GetRelativePath(thuMucGoc, duongDan)} ({pipeline.Length} stage)");
        }
        Console.WriteLine($"Xuất xong {danhSach.Length} pipeline.");
    }
}
