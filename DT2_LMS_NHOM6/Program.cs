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

// Cờ --reset: xóa database rồi tạo lại validator, index, dữ liệu mẫu.
// Tách khỏi args trước khi đưa vào builder để không bị hiểu nhầm là tham số cấu hình.
var reset = args.Contains("--reset");
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--reset").ToArray());

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
