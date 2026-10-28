using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.WebEncoders;

// Cờ dòng lệnh (tách khỏi args trước khi đưa vào builder để không bị hiểu nhầm là tham số cấu hình):
//   --reset            xóa database rồi tạo lại validator, index, dữ liệu mẫu
//   --kiem-thu-gpa     chạy pipeline cho 5 SV kiểm thử, so với bảng 7.5, ghi docs/kiem-thu-gpa.md rồi thoát
//   --export-pipelines ghi mọi pipeline ra database/pipelines/*.json rồi thoát
string[] coDongLenh = ["--reset", "--kiem-thu-gpa", "--export-pipelines"];
var reset = args.Contains("--reset");
var kiemThuGpa = args.Contains("--kiem-thu-gpa");
var xuatPipeline = args.Contains("--export-pipelines");
var builder = WebApplication.CreateBuilder(args.Where(a => !coDongLenh.Contains(a)).ToArray());

// Culture vi-VN nhưng dấu thập phân là dấu chấm – áp dụng cho thread mặc định và mọi request
var cultureViVn = CultureConfig.TaoCultureViVn();
CultureInfo.DefaultThreadCurrentCulture = cultureViVn;
CultureInfo.DefaultThreadCurrentUICulture = cultureViVn;

// Cấu hình
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.TenMuc));
builder.Services.Configure<HocKyHienTaiOptions>(builder.Configuration.GetSection(HocKyHienTaiOptions.TenMuc));

// MongoDB: một MongoClient dùng chung toàn ứng dụng
builder.Services.AddSingleton<MongoContext>();

// Nghiệp vụ
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<DiemService>();
builder.Services.AddScoped<BaoCaoService>();

// Xác thực bằng cookie: hết hạn 8 giờ, gia hạn trượt khi còn hoạt động
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "EduScore.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();

// Razor giữ nguyên chữ tiếng Việt có dấu thay vì mã hóa thành &#x...; (vẫn mã hóa < > & " để chống XSS)
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddControllersWithViews(options =>
{
    // Mọi request POST/PUT/DELETE đều phải có antiforgery token hợp lệ
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// Khởi tạo database (validator, index, seed) trước khi nhận request
await DbInitializer.KhoiTaoAsync(app.Services, reset);

// Lệnh công cụ: chạy xong thì thoát, không khởi động web. Thư mục gốc repo = thư mục cha của project.
var thuMucGoc = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, ".."));
if (xuatPipeline)
{
    await XuatPipeline.ChayAsync(thuMucGoc);
    if (!kiemThuGpa) return;
}
if (kiemThuGpa)
{
    Environment.ExitCode = await KiemThuGpa.ChayAsync(app.Services, thuMucGoc);
    return;
}

// Lỗi chưa bắt → trang lỗi thân thiện (không lộ stack trace), kể cả môi trường Development
app.UseExceptionHandler("/Home/StatusCode/500");
// 404, 403... không có nội dung → hiển thị lại trang lỗi tiếng Việt, giữ nguyên mã trạng thái
app.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}");

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultureViVn),
    SupportedCultures = [cultureViVn],
    SupportedUICultures = [cultureViVn],
    RequestCultureProviders = [] // Không đa ngôn ngữ: luôn dùng vi-VN
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Route cho Areas (Admin, GiangVien, SinhVien) – đặt trước route mặc định
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
