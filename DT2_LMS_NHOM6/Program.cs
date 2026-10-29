using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using DT2_LMS_NHOM6.Data;
using DT2_LMS_NHOM6.Infrastructure;
using DT2_LMS_NHOM6.Services;
using DT2_LMS_NHOM6.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.WebEncoders;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;

// Cờ dòng lệnh (tách khỏi args trước khi đưa vào builder để không bị hiểu nhầm là tham số cấu hình):
//   --reset            xóa database rồi tạo lại validator, index, dữ liệu mẫu
//   --kiem-thu-gpa     chạy pipeline cho 5 SV kiểm thử, so với bảng 7.5, ghi docs/kiem-thu-gpa.md rồi thoát
//   --export-pipelines ghi mọi pipeline ra database/pipelines/*.json rồi thoát
//   --tao-file-demo    ghi các file Excel demo nhập/xuất vào docs/demo/ rồi thoát
//   --export           ghi 5 collection ra database/export/*.json (+ database/dump/ nếu có mongodump) rồi thoát
string[] coDongLenh = ["--reset", "--kiem-thu-gpa", "--export-pipelines", "--tao-file-demo", "--export"];
var reset = args.Contains("--reset");
var kiemThuGpa = args.Contains("--kiem-thu-gpa");
var xuatPipeline = args.Contains("--export-pipelines");
var taoFileDemo = args.Contains("--tao-file-demo");
var xuatDuLieu = args.Contains("--export");
var builder = WebApplication.CreateBuilder(args.Where(a => !coDongLenh.Contains(a)).ToArray());

// Console in tiếng Việt có dấu (thông báo lỗi kết nối, kết quả các lệnh công cụ)
Console.OutputEncoding = System.Text.Encoding.UTF8;

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
builder.Services.AddScoped<TaiKhoanService>();
builder.Services.AddScoped<LopHocService>();
builder.Services.AddScoped<TenLopService>();
builder.Services.AddScoped<SinhVienService>();
builder.Services.AddScoped<GiangVienService>();
builder.Services.AddScoped<MonHocService>();
builder.Services.AddScoped<GhiDanhService>();
builder.Services.AddScoped<XuatExcelService>();
builder.Services.AddScoped<NhapExcelService>();
builder.Services.AddScoped<HieuNangService>();

// Kết quả kiểm tra file Excel giữ tạm trong bộ nhớ giữa bước xem trước và bước xác nhận (thiết kế mục 6)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<KhoPhienNhap>();

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

        // API (/api/...): không chuyển hướng sang trang đăng nhập mà trả 401 / 403 dạng JSON
        options.Events.OnRedirectToLogin = context => TraLoiApiHoacChuyenHuong(context,
            StatusCodes.Status401Unauthorized, "Chưa đăng nhập. Hãy đăng nhập ở /Account/Login rồi gọi lại API.");
        options.Events.OnRedirectToAccessDenied = context => TraLoiApiHoacChuyenHuong(context,
            StatusCodes.Status403Forbidden, "Vai trò của bạn không được phép gọi API này.");
    });
builder.Services.AddAuthorization();

// Razor giữ nguyên chữ tiếng Việt có dấu thay vì mã hóa thành &#x...; (vẫn mã hóa < > & " để chống XSS)
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddControllersWithViews(options =>
{
    // Mọi request POST/PUT/DELETE đều phải có antiforgery token hợp lệ (API bỏ qua – xem ApiControllerBase)
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
.AddJsonOptions(options =>
{
    // JSON trả về (API + fetch): ObjectId → chuỗi hex; giữ nguyên chữ tiếng Việt và dấu " trong mongoCommand
    // thay vì \uXXXX (JSON này chỉ trả qua HTTP, không nhúng vào HTML nên không cần mã hóa < > ")
    options.JsonSerializerOptions.Converters.Add(new ObjectIdJsonConverter());
    options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
})
.ConfigureApiBehaviorOptions(options =>
{
    // API: dữ liệu gửi lên sai → 400 với thông báo tiếng Việt theo khung KetQuaApi
    options.InvalidModelStateResponseFactory = context =>
        new BadRequestObjectResult(new KetQuaApi
        {
            ThanhCong = false,
            ThongBao = "Dữ liệu gửi lên không hợp lệ.",
            Loi = context.ModelState
                .Where(m => m.Value!.Errors.Count > 0)
                .ToDictionary(m => JsonNamingPolicy.CamelCase.ConvertName(m.Key),
                              m => m.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage)
                                  ? "Giá trị không hợp lệ." : e.ErrorMessage).ToArray())
        });
});

// Swagger (GĐ8): mô tả API lấy từ comment /// tiếng Việt; chỉ mở giao diện ở môi trường Development
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EduScore API",
        Version = "v1",
        Description = "API REST của Cổng Quản lý Học tập – EduScore. Xác thực bằng cookie: đăng nhập ở /Account/Login "
                    + "(cùng trình duyệt) rồi bấm \"Try it out\". Các API ghi trả kèm trường mongoCommand (lệnh MongoDB đã chạy)."
    });
    // Chỉ đưa các controller /api vào tài liệu (màn hình MVC không phải API)
    options.DocInclusionPredicate((_, api) => api.RelativePath?.StartsWith("api/") == true);
    options.MapType<ObjectId>(() => new OpenApiSchema { Type = "string", Example = new OpenApiString("6720f1a2b3c4d5e6f7a8b9c0") });
    var fileXml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    options.IncludeXmlComments(fileXml);
});

var app = builder.Build();

// Kiểm tra kết nối MongoDB trước: không kết nối được → báo rõ trên console rồi thoát (mã 1)
var mongo = app.Services.GetRequiredService<MongoContext>();
var loiKetNoi = await mongo.KiemTraKetNoiAsync();
if (loiKetNoi != null)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine($"Không kết nối được MongoDB tại {mongo.DiaChiMayChu} (database \"{mongo.Database.DatabaseNamespace.DatabaseName}\").");
    Console.ResetColor();
    Console.Error.WriteLine("  → Kiểm tra dịch vụ MongoDB đã chạy chưa (Windows: services.msc → MongoDB Server, hoặc lệnh: net start MongoDB).");
    Console.Error.WriteLine("  → Kiểm tra mục \"MongoDB:ConnectionString\" trong appsettings.json.");
    Console.Error.WriteLine($"  Chi tiết: {loiKetNoi}");
    Environment.ExitCode = 1;
    return;
}

// Khởi tạo database (validator, index, seed) trước khi nhận request
await DbInitializer.KhoiTaoAsync(app.Services, reset);

// Lệnh công cụ: chạy xong thì thoát, không khởi động web. Thư mục gốc repo = thư mục cha của project.
var thuMucGoc = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, ".."));
if (xuatPipeline)
{
    await XuatPipeline.ChayAsync(thuMucGoc);
    if (!kiemThuGpa) return;
}
if (xuatDuLieu)
{
    Environment.ExitCode = await XuatDuLieu.ChayAsync(mongo, thuMucGoc);
    return;
}
if (taoFileDemo)
{
    Environment.ExitCode = await TaoFileDemo.ChayAsync(app.Services, thuMucGoc);
    return;
}
if (kiemThuGpa)
{
    Environment.ExitCode = await KiemThuGpa.ChayAsync(app.Services, thuMucGoc);
    return;
}

// Lỗi chưa bắt → trang lỗi thân thiện (không lộ stack trace), kể cả môi trường Development
app.UseExceptionHandler("/Home/StatusCode/500");
// 404, 403... không có nội dung → hiển thị lại trang lỗi tiếng Việt, giữ nguyên mã trạng thái.
// Không áp dụng cho /api (API luôn trả JSON).
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"),
    nhanh => nhanh.UseStatusCodePagesWithReExecute("/Home/StatusCode/{0}"));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EduScore API v1");
        options.DocumentTitle = "EduScore API";
    });
}

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

// Request tới /api → trả mã lỗi + JSON; trang web → chuyển hướng như mặc định (Login / AccessDenied)
static Task TraLoiApiHoacChuyenHuong(RedirectContext<CookieAuthenticationOptions> context, int maTrangThai, string thongBao)
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }
    context.Response.StatusCode = maTrangThai;
    return context.Response.WriteAsJsonAsync(new KetQuaApi { ThanhCong = false, ThongBao = thongBao });
}
