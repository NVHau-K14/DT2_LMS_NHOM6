using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Documents;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Data;

/// <summary>
/// Điểm truy cập MongoDB duy nhất của ứng dụng (đăng ký Singleton – MongoClient an toàn đa luồng).
/// Service dùng các collection có kiểu ở đây; controller không gọi MongoDB trực tiếp.
/// </summary>
public class MongoContext
{
    // Tên collection đúng thiết kế mục 4
    public const string TenSinhVien = "sinhvien";
    public const string TenLopHoc = "lophoc";
    public const string TenGiangVien = "giangvien";
    public const string TenMonHoc = "monhoc";
    public const string TenTaiKhoan = "taikhoan";

    /// <summary>Địa chỉ máy chủ (host:port, không kèm mật khẩu) – dùng cho thông báo lỗi kết nối.</summary>
    public string DiaChiMayChu { get; }

    /// <summary>Chuỗi kết nối gốc trong appsettings – lệnh --export truyền cho mongodump.</summary>
    public string ConnectionString { get; }

    public IMongoClient Client { get; }
    public IMongoDatabase Database { get; }

    public IMongoCollection<SinhVien> SinhVien => Database.GetCollection<SinhVien>(TenSinhVien);
    public IMongoCollection<LopHoc> LopHoc => Database.GetCollection<LopHoc>(TenLopHoc);
    public IMongoCollection<GiangVien> GiangVien => Database.GetCollection<GiangVien>(TenGiangVien);
    public IMongoCollection<MonHoc> MonHoc => Database.GetCollection<MonHoc>(TenMonHoc);
    public IMongoCollection<TaiKhoan> TaiKhoan => Database.GetCollection<TaiKhoan>(TenTaiKhoan);

    public MongoContext(IOptions<MongoDbOptions> options)
    {
        var cauHinh = options.Value;
        ConnectionString = cauHinh.ConnectionString;
        var url = MongoUrl.Create(cauHinh.ConnectionString);
        DiaChiMayChu = string.Join(", ", url.Servers);

        // Chờ chọn máy chủ tối đa 5 giây (mặc định 30 giây) → MongoDB chưa chạy thì báo lỗi nhanh
        var settings = MongoClientSettings.FromUrl(url);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        Client = new MongoClient(settings);
        Database = Client.GetDatabase(cauHinh.DatabaseName);
    }

    /// <summary>Gửi lệnh ping. Trả null nếu kết nối được, ngược lại trả thông báo lỗi ngắn gọn.</summary>
    public async Task<string?> KiemTraKetNoiAsync()
    {
        try
        {
            await Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));
            return null;
        }
        catch (TimeoutException)
        {
            // Thông báo gốc của driver rất dài (kèm cả stack trace của lần thử kết nối) → rút gọn
            return "hết 5 giây chờ mà không có máy chủ MongoDB nào phản hồi.";
        }
        catch (MongoException ex)
        {
            return ex.Message.Split('\n')[0];
        }
    }
}
