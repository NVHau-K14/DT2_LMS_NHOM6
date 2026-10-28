using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Models.Documents;
using Microsoft.Extensions.Options;
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
        Client = new MongoClient(cauHinh.ConnectionString);
        Database = Client.GetDatabase(cauHinh.DatabaseName);
    }
}
