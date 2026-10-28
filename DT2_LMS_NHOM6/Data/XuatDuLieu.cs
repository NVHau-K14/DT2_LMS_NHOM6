using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Data;

/// <summary>
/// Lệnh: dotnet run --project DT2_LMS_NHOM6 -- --export   (thường chạy sau --reset để có dữ liệu chuẩn)
///   1. Ghi 5 collection ra database/export/&lt;collection&gt;.json: mảng JSON, Extended JSON dạng relaxed
///      (ObjectId → {"$oid": ...}, Date → {"$date": "...Z"}, số giữ nguyên) – giữ nguyên _id.
///      Nhập lại được bằng Compass (Add Data → Import JSON) hoặc mongoimport --jsonArray.
///   2. Máy có mongodump → ghi thêm database/dump/eduscore.archive.gz (--archive --gzip) để mongorestore
///      khôi phục trọn vẹn cả validator và index. Không có thì bỏ qua (xem database/README.md).
/// </summary>
public static class XuatDuLieu
{
    private static readonly string[] CacCollection =
    [
        MongoContext.TenSinhVien, MongoContext.TenLopHoc, MongoContext.TenGiangVien,
        MongoContext.TenMonHoc, MongoContext.TenTaiKhoan
    ];

    private const string TenFileArchive = "eduscore.archive.gz";

    public static async Task<int> ChayAsync(MongoContext context, string thuMucGoc)
    {
        var thuMucExport = Path.Combine(thuMucGoc, "database", "export");
        Directory.CreateDirectory(thuMucExport);

        var caiDat = new JsonWriterSettings { OutputMode = JsonOutputMode.RelaxedExtendedJson, Indent = true };
        foreach (var ten in CacCollection)
        {
            // Sắp theo _id để mỗi lần xuất cùng dữ liệu ra cùng một file (dễ so sánh bằng git diff)
            var danhSach = await context.Database.GetCollection<BsonDocument>(ten)
                .Find(FilterDefinition<BsonDocument>.Empty)
                .Sort(new BsonDocument("_id", 1))
                .ToListAsync();

            var duongDan = Path.Combine(thuMucExport, ten + ".json");
            var noiDung = "[\n" + string.Join(",\n", danhSach.Select(d => d.ToJson(caiDat))) + "\n]\n";
            await File.WriteAllTextAsync(duongDan, noiDung, new UTF8Encoding(false));
            Console.WriteLine($"  Đã ghi {Path.GetRelativePath(thuMucGoc, duongDan)} ({danhSach.Count} document)");
        }

        var thuMucDump = Path.Combine(thuMucGoc, "database", "dump");
        Directory.CreateDirectory(thuMucDump);
        var fileArchive = Path.Combine(thuMucDump, TenFileArchive);

        if (!await CoMongoDumpAsync())
        {
            Console.WriteLine("  Không tìm thấy mongodump → bỏ qua bản dump nhị phân (cài MongoDB Database Tools, xem database/README.md).");
            if (File.Exists(fileArchive))
                Console.WriteLine($"  Lưu ý: {Path.GetRelativePath(thuMucGoc, fileArchive)} là bản cũ, không khớp lần xuất này.");
            Console.WriteLine("Xuất xong 5 collection (JSON).");
            return 0;
        }

        // mongodump --uri=... --db=eduscore --archive=database/dump/eduscore.archive.gz --gzip
        var tenDb = context.Database.DatabaseNamespace.DatabaseName;
        var (maThoat, _, loi) = await ChayLenhAsync("mongodump",
            $"--uri=\"{context.ConnectionString}\" --db={tenDb} --archive=\"{fileArchive}\" --gzip");
        if (maThoat != 0)
        {
            Console.Error.WriteLine($"  mongodump lỗi (mã {maThoat}): {loi.Trim()}");
            return 1;
        }
        Console.WriteLine($"  Đã ghi {Path.GetRelativePath(thuMucGoc, fileArchive)} ({new FileInfo(fileArchive).Length / 1024} KB)");
        Console.WriteLine("Xuất xong 5 collection (JSON) + bản dump mongodump.");
        return 0;
    }

    /// <summary>Kiểm tra máy có mongodump trong PATH: chạy "mongodump --version".</summary>
    private static async Task<bool> CoMongoDumpAsync()
    {
        try
        {
            var (maThoat, _, _) = await ChayLenhAsync("mongodump", "--version");
            return maThoat == 0;
        }
        catch (Win32Exception)
        {
            return false; // không có file thực thi
        }
    }

    private static async Task<(int MaThoat, string Ra, string Loi)> ChayLenhAsync(string lenh, string thamSo)
    {
        using var tienTrinh = Process.Start(new ProcessStartInfo(lenh, thamSo)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        })!;
        var ra = tienTrinh.StandardOutput.ReadToEndAsync();
        var loi = tienTrinh.StandardError.ReadToEndAsync();
        await tienTrinh.WaitForExitAsync();
        return (tienTrinh.ExitCode, await ra, await loi);
    }
}
