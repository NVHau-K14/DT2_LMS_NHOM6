# Giai đoạn 2 – Đăng nhập, phân quyền, layout, đổi mật khẩu, trang lỗi

## Prompt
```
Đọc CLAUDE.md, docs/thiet-ke.md (mục 2, 4.5, 5, 8) và docs/quy-trinh/GD2.md. Thực hiện Giai đoạn 2 đúng theo GD2.md. Lập todo trước khi làm. Khi xong, tự kiểm tra, cập nhật tiến độ trong CLAUDE.md, báo cáo kèm hướng dẫn nghiệm thu, rồi chờ tôi xác nhận mới commit.
```

## Việc cần làm

1. **Thư viện cục bộ**: tải về `wwwroot/lib/` – Bootstrap Icons 1.11.3 (css + thư mục fonts) và Chart.js 4.4.x (file UMD). Bootstrap 5 và jQuery validation đã có sẵn.
2. **Xác thực**: Cookie Authentication (LoginPath `/Account/Login`, AccessDeniedPath `/Account/AccessDenied`, hết hạn 8 giờ, sliding). `AccountService`: kiểm tra BCrypt, chặn tài khoản "Khóa", cập nhật `lanDangNhapCuoi`, đổi mật khẩu. Claims: `Name` = hoTen, `NameIdentifier` = tenDangNhap, `Role` = vaiTro, `MaThamChieu`. Helper đọc claim (`User.MaThamChieu()`).
3. **AccountController**: Login (GET/POST, ViewModel + validation, thông báo sai tài khoản chung chung), Logout (POST), DoiMatKhau (GET/POST, mọi vai trò), AccessDenied. **Không hiện tài khoản demo.**
4. **HomeController**: `/` chuyển hướng theo vai trò (Admin → `/Admin`, GiangVien → `/GiangVien`, SinhVien → `/SinhVien`); `StatusCode/{code}` cho 404/403/500 (`UseStatusCodePagesWithReExecute`, `UseExceptionHandler`). Xóa trang Privacy mặc định.
5. **Areas** `Admin`, `GiangVien`, `SinhVien`: route `{area:exists}/{controller=Home}/{action=Index}/{id?}`; mỗi area một controller cơ sở có `[Area]` + `[Authorize(Roles)]`; trang `Home/Index` tạm (lời chào + tên). Menu đủ mục theo thiết kế mục 5; mục chưa làm trỏ tới route tương lai (404 tạm chấp nhận được).
6. **Layout**:
   - `_LayoutAuth` cho trang đăng nhập (2 cột: trái giới thiệu EduScore với icon/hình minh họa bằng CSS + Bootstrap Icons, phải form).
   - `_Layout` chính: sidebar theo vai trò (partial theo Role), header (tiêu đề + breadcrumb qua ViewData, nhãn "HK1 2026-2027" từ `HocKyHienTai`, dropdown người dùng: họ tên, vai trò, Đổi mật khẩu, Đăng xuất), vùng nội dung, khu vực toast. Sidebar thu gọn được và ẩn/hiện ở màn hình hẹp.
   - CSS theme theo thiết kế mục 8 trong `wwwroot/css/site.css`.
7. **Thành phần dùng chung** (`wwwroot/js/eduscore.js` + partial):
   - `toast(message, type, mongoCommand?)`: toast Bootstrap; nếu có `mongoCommand` hiện khối thu gọn "Lệnh MongoDB đã chạy" (`<pre>`) + nút sao chép. Hỗ trợ hiển thị toast từ `TempData` sau redirect (TempData["Toast"], TempData["MongoCommand"]).
   - Hộp xác nhận (modal) dùng chung cho thao tác xóa (form POST).
   - Partial/TagHelper: badge điểm chữ (A/B/C/D/F, màu theo mục 5), badge xếp loại, định dạng điểm (1 chữ số), GPA (2 chữ số), hiển thị học kỳ ("HK1 2025-2026", "HK hè 2025-2026").
   - Trang trống/empty state.
8. **Culture**: vi-VN, `NumberDecimalSeparator = "."`, áp dụng cho request localization và thread mặc định.

## Tự kiểm tra
- Build 0 lỗi. Chạy app, dùng curl (cookie + antiforgery):
  - `/` chưa đăng nhập → 302 về Login.
  - Đăng nhập `admin/Admin@123` → `/Admin` 200; `GV005/123456` → `/GiangVien`; `2001250001/15032007` → `/SinhVien`.
  - Đăng nhập SV rồi gọi `/Admin` → 403 (trang AccessDenied).
  - `/khong-ton-tai` → trang 404 tiếng Việt.
  - Sai mật khẩu → thông báo lỗi, không lộ thông tin.
- File trong `wwwroot/lib/bootstrap-icons` và `chart.js` tồn tại; layout không tham chiếu URL ngoài (grep `http` trong Views).

## Nghiệm thu (bạn làm)
- [ ] Trang đăng nhập đẹp, tiếng Việt, không hiện tài khoản demo.
- [ ] Đăng nhập 3 vai trò → đúng cổng, sidebar đúng menu, header hiện họ tên + "HK1 2026-2027".
- [ ] Đăng nhập SV, gõ `localhost:5207/Admin` → bị chặn.
- [ ] Đổi mật khẩu SV thành công, đăng nhập lại bằng mật khẩu mới (sau đó có thể `--reset`).
- [ ] Thu gọn sidebar, thu nhỏ cửa sổ → vẫn dùng được.
- [ ] Commit.
