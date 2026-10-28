# Báo cáo Giai đoạn 2 – Đăng nhập, phân quyền, layout, đổi mật khẩu, trang lỗi

- **Ngày hoàn thành:** 28/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Commit:** `5946d74` (điền sau khi người dùng nghiệm thu và commit)

## 1. Đã làm gì
- **Thư viện cục bộ:** Bootstrap Icons 1.11.3 (css + fonts woff/woff2) và Chart.js 4.4.7 (`chart.umd.js`) tải từ npm registry vào `wwwroot/lib/`, kèm LICENSE. Không có URL ngoài trong Views/Areas.
- **Xác thực cookie** (LoginPath `/Account/Login`, AccessDeniedPath `/Account/AccessDenied`, hết hạn 8 giờ, sliding). `AccountService`: kiểm tra BCrypt, chặn tài khoản "Khóa", cập nhật `lanDangNhapCuoi`, đổi mật khẩu. Claims: `Name`=hoTen, `NameIdentifier`=tenDangNhap, `Role`=vaiTro, `MaThamChieu`; đọc bằng `User.MaThamChieu()`, `User.HoTen()`...
- **AccountController** (Login GET/POST, Logout POST, DoiMatKhau GET/POST, AccessDenied 403) và **HomeController** (`/` chuyển hướng theo vai trò, `/Home/StatusCode/{code}` cho 403/404/500). Đã xóa Privacy, Error mặc định.
- **3 Area** `Admin`, `GiangVien`, `SinhVien`: mỗi Area có controller cơ sở `[Area] + [Authorize(Roles)]`, trang `Home/Index` tạm (lời chào + họ tên). Menu đủ mục theo thiết kế mục 5; mục chưa làm trả 404 tạm.
- **Layout:** `_LayoutAuth` (2 cột: giới thiệu + minh họa CSS / form), `_Layout` chính (sidebar xanh dương thu gọn được + trượt ở màn hình hẹp, header có tiêu đề, breadcrumb, nhãn "HK1 2026-2027", dropdown người dùng), theme mục 8 trong `site.css`.
- **Thành phần dùng chung:** `toast(message, type, mongoCommand?)` có khối thu gọn "Lệnh MongoDB đã chạy" + nút sao chép, toast từ TempData sau redirect; modal xác nhận xóa (form POST có token); TagHelper badge điểm chữ / xếp loại; `HienThi` định dạng điểm (1 chữ số), GPA (2 chữ số), học kỳ, ngày; partial trạng thái trống. Culture vi-VN với dấu chấm thập phân.

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `Program.cs` | Cookie auth, antiforgery toàn cục, culture, `UseExceptionHandler` + `UseStatusCodePagesWithReExecute`, route Area |
| `Services/AccountService.cs` | Đăng nhập BCrypt, chặn "Khóa", `lanDangNhapCuoi`, dựng claims, đổi mật khẩu |
| `Controllers/AccountController.cs` | Login, Logout, DoiMatKhau, AccessDenied |
| `Controllers/HomeController.cs` | `/` theo vai trò; trang lỗi `Home/StatusCode/{code}` |
| `Areas/*/Controllers/*ControllerBase.cs` | Controller cơ sở: `[Area]` + `[Authorize(Roles = ...)]` |
| `Areas/*/Controllers/HomeController.cs`, `Areas/*/Views/Home/Index.cshtml` | Trang tổng quan tạm của từng cổng |
| `Infrastructure/ClaimsPrincipalExtensions.cs` | `User.MaThamChieu()`, `TenDangNhap()`, `HoTen()`, `VaiTro()`, `TrangChuTheoVaiTro()` |
| `Infrastructure/CultureConfig.cs` | vi-VN + dấu chấm thập phân |
| `Infrastructure/HienThi.cs` | Định dạng điểm, GPA, học kỳ, ngày, tên vai trò, lớp CSS badge |
| `Infrastructure/ThongBaoExtensions.cs` | `this.ThongBao(...)` ghi toast vào TempData |
| `Infrastructure/TagHelpers/BadgeTagHelpers.cs` | `<diem-chu gia-tri="A" />`, `<xep-loai gia-tri="Giỏi" />` |
| `ViewModels/DangNhapViewModel.cs`, `DoiMatKhauViewModel.cs`, `MucBreadcrumb.cs`, `TrangTrongViewModel.cs` | ViewModel + validation |
| `Views/Shared/_Layout.cshtml`, `_LayoutAuth.cshtml`, `_Sidebar.cshtml` | Khung giao diện |
| `Views/Shared/_Toast.cshtml`, `_XacNhanXoa.cshtml`, `_TrangTrong.cshtml` | Toast, modal xóa, trạng thái trống |
| `Views/Account/Login.cshtml`, `DoiMatKhau.cshtml`, `Views/Home/StatusCode.cshtml` | Màn hình C-01, C-02, C-03 |
| `wwwroot/css/site.css`, `wwwroot/js/eduscore.js` | Theme mục 8; JS dùng chung (toast, modal, sidebar) |
| `wwwroot/lib/bootstrap-icons/`, `wwwroot/lib/chart.js/` | Thư viện cục bộ |

## 3. Kết quả tự kiểm tra
| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | 0 lỗi, 0 cảnh báo |
| `/` chưa đăng nhập | curl | 302 → `/Account/Login?ReturnUrl=%2F` ✅ |
| Admin | `admin/Admin@123` | 302 → `/Admin`, `/Admin` 200, 9 mục menu, "HK1 2026-2027", "Xin chào, Quản trị hệ thống!" ✅ |
| Giảng viên | `GV005/123456` | 302 → `/GiangVien`, 200, 3 mục menu, "Xin chào, ThS. Trần Văn Bình!" ✅ |
| Sinh viên | `2001250001/15032007` | 302 → `/SinhVien`, 200, 3 mục menu, "Xin chào, Nguyễn Hoàng Minh!" ✅ |
| SV vào `/Admin` | curl | 302 → AccessDenied → **403** "Bạn không có quyền truy cập" ✅ (GV vào `/SinhVien` cũng bị chặn) |
| `/khong-ton-tai` | curl | **404** "Không tìm thấy trang" ✅ |
| Sai mật khẩu / sai tên | curl | Cùng một thông báo "Tên đăng nhập hoặc mật khẩu không đúng." ✅ |
| Tài khoản Khóa | mongosh khóa GV008 rồi đăng nhập | Đúng MK → "Tài khoản đã bị khóa..."; sai MK → thông báo chung. Đã mở lại ✅ |
| POST thiếu antiforgery token | curl | 400 ✅ |
| Đổi mật khẩu | SV 2001250001: sai MK cũ / MK ngắn / không khớp → lỗi đúng ô; đổi đúng → toast "Đổi mật khẩu thành công.", đăng nhập MK cũ thất bại, MK mới thành công; đã đổi lại `15032007` | ✅ |
| `lanDangNhapCuoi` | mongosh | Được cập nhật sau đăng nhập ✅ |
| Đăng xuất | POST `/Account/Logout` | 302 → Login, `/Admin` sau đó → 302 Login ✅ |
| returnUrl ngoài | `ReturnUrl=https://evil.com` | Bỏ qua, về `/Admin` ✅ |
| Không lộ tài khoản demo | grep trang Login | Không có ✅ |
| Thư viện cục bộ | GET css/font/chart.umd.js | 200 ✅; grep `http` trong Views/Areas: không có ✅ |
| Trang 500 | Cấu hình `UseExceptionHandler("/Home/StatusCode/500")` | Chưa có chỗ gây lỗi để kích hoạt thật; view dùng chung với 403/404 |

## 4. Quyết định kỹ thuật mới
- **Antiforgery toàn cục** (`AutoValidateAntiforgeryTokenAttribute`): mọi POST đều phải có token, kể cả fetch ở GĐ5 (gửi header `RequestVerificationToken`, lấy bằng `EduScore.layAntiforgeryToken()`). Action trang lỗi có `[IgnoreAntiforgeryToken]` vì được re-execute cả với POST.
- **Culture**: vi-VN, dấu thập phân ".", dấu nhóm nghìn là khoảng trắng (không dùng "," để "8,5" không bị model binder hiểu thành 85). Không đa ngôn ngữ nên `RequestCultureProviders` rỗng.
- **WebEncoderOptions = UnicodeRanges.All**: Razor xuất tiếng Việt nguyên dạng thay vì `&#x...;` (vẫn mã hóa `< > & "`).
- **Trạng thái "Khóa" chỉ báo khi mật khẩu đúng** – người đoán mò không biết tài khoản có tồn tại.
- **Menu dùng URL cứng** (`/Admin/LopHoc`...) vì anchor tag helper không sinh link tới controller chưa tồn tại; active theo area + controller.
- **Toast không dựng HTML bằng chuỗi**: JS nhân bản `<template id="mau-toast">` và gán `textContent`.
- Trang lỗi dùng `_Layout` (có sidebar) khi đã đăng nhập, `_LayoutAuth` khi chưa.

## 5. Điểm cần chú ý / hạn chế
- Các mục menu của giai đoạn sau (Lớp học, Sinh viên, Nhập điểm...) hiện trả **404** – đúng như GD2 cho phép.
- Khóa một tài khoản đang đăng nhập không đăng xuất phiên hiện tại (cookie còn hiệu lực tới khi hết hạn/đăng xuất); lần đăng nhập sau sẽ bị chặn.
- Khi tự kiểm tra đã đổi rồi trả lại mật khẩu của SV `2001250001` (vẫn là `15032007`, hash nay là `$2a$11$`) và khóa/mở lại `GV008`; `lanDangNhapCuoi` của vài tài khoản đã có giá trị. Muốn dữ liệu sạch: `dotnet run --project DT2_LMS_NHOM6 -- --reset`.
- `docs/bao-cao/GD1.md` đã có thay đổi chưa commit từ trước giai đoạn này; tôi không động vào.
- Trang 500 chưa được kích hoạt thật (chưa có chức năng nào ném lỗi để thử).

## 6. Nghiệm thu
Hướng dẫn: dừng app cũ nếu có → `dotnet run --project DT2_LMS_NHOM6` → mở http://localhost:5207.
Tài khoản: `admin / Admin@123`, `GV005 / 123456`, `2001250001 / 15032007`.

- [x] Trang đăng nhập đẹp, tiếng Việt, không hiện tài khoản demo.
- [x] Đăng nhập 3 vai trò → đúng cổng, sidebar đúng menu, header hiện họ tên + "HK1 2026-2027".
- [x] Đăng nhập SV, gõ `localhost:5207/Admin` → bị chặn.
- [x] Đổi mật khẩu SV thành công, đăng nhập lại bằng mật khẩu mới (sau đó có thể `--reset`).
- [x] Thu gọn sidebar, thu nhỏ cửa sổ → vẫn dùng được.
- [x] Commit.

Gợi ý thêm khi thử: nhập sai mật khẩu (thông báo chung), gõ `localhost:5207/abc` (404 tiếng Việt), nút ☰ ở header (màn hình rộng: thu gọn còn icon và được ghi nhớ; màn hình hẹp: sidebar trượt ra, bấm nền tối để đóng).

## 7. Gợi ý trả lời khi bảo vệ
- **Phân quyền làm thế nào?** Mỗi Area có controller cơ sở gắn `[Area("Admin")]` + `[Authorize(Roles = "Admin")]`; mọi controller trong Area kế thừa nên không thể quên phân quyền. Vai trò đọc từ claim `Role` trong cookie; sai vai trò → 403.
- **Mật khẩu lưu thế nào?** Chỉ lưu chuỗi băm BCrypt (`matKhauHash`, có salt, work factor 10–11), đăng nhập dùng `BCrypt.Verify`. Thông báo lỗi không phân biệt sai tên hay sai mật khẩu để tránh dò tài khoản.
- **Vì sao mã SV lấy từ claim?** Nếu lấy từ URL thì SV đổi `?maSV=` để xem điểm người khác. Claim nằm trong cookie đã ký/mã hóa bởi ASP.NET Core nên người dùng không sửa được.
- **Chống CSRF?** Filter toàn cục `AutoValidateAntiforgeryToken`: mọi POST thiếu token trả 400; form Razor tự chèn token.
