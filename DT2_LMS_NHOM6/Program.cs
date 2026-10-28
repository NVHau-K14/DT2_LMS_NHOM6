using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;

// Cờ --reset: xóa database rồi tạo lại validator, index, dữ liệu mẫu.
// Tách khỏi args trước khi đưa vào builder để không bị hiểu nhầm là tham số cấu hình.
var reset = args.Contains("--reset");
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--reset").ToArray());

// Cấu hình
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.TenMuc));
builder.Services.Configure<HocKyHienTaiOptions>(builder.Configuration.GetSection(HocKyHienTaiOptions.TenMuc));

// MongoDB: một MongoClient dùng chung toàn ứng dụng
builder.Services.AddSingleton<MongoContext>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Khởi tạo database (validator, index, seed) trước khi nhận request
await DbInitializer.KhoiTaoAsync(app.Services, reset);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
