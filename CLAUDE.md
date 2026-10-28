# CLAUDE.md – EduScore (Đồ án NoSQL – Đề tài 02)

**Cổng Quản lý Học tập – EduScore**: ASP.NET Core MVC (.NET 9, C#) + MongoDB 8.3. Quản lý lớp, hồ sơ sinh viên nhúng mảng môn học, nhập điểm, tính GPA/xếp loại bằng Aggregation Pipeline.

**Đọc `docs/thiet-ke.md` trước khi làm bất cứ việc gì.** Đó là nguồn sự thật duy nhất về nghiệp vụ, quy tắc điểm, schema, màn hình, dữ liệu mẫu. Không tự sáng tạo nghiệp vụ.

## Bối cảnh
- Làm một mình, hạn nộp 12h30 ngày 30/10/2026. Ưu tiên: **đúng – chạy được – dễ giải thích khi bảo vệ**, rồi mới đến đẹp.
- Người dùng nghiệm thu thủ công sau mỗi giai đoạn và cần giải thích được code trước giảng viên → code rõ ràng, đặt tên có nghĩa, comment tiếng Việt ở chỗ quan trọng (pipeline, update mảng).
- Máy: Windows 11, .NET SDK 9.0.301, MongoDB Server 8.3.8 (standalone, cổng 27017, **không có transaction**), mongosh 2.14, Compass.

## Cấu trúc
```
DT2_LMS_NHOM6/                  ← gốc repo (CLAUDE.md, README.md, DT2_LMS_NHOM6.sln)
├── docs/                       thiet-ke.md, quy-trinh/GD*.md, bao-cao/GD*.md (báo cáo sau mỗi giai đoạn)
├── database/                   export/, dump/, pipelines/*.json
└── DT2_LMS_NHOM6/              ← project MVC (namespace DT2_LMS_NHOM6)
    ├── Areas/Admin|GiangVien|SinhVien/{Controllers,Views}
    ├── Controllers/            AccountController, HomeController
    ├── Models/Documents/       class map document MongoDB ([BsonElement] tên tiếng Việt không dấu)
    ├── Models/Results/         class nhận kết quả pipeline
    ├── ViewModels/
    ├── Services/               nghiệp vụ; controller KHÔNG gọi MongoDB trực tiếp
    ├── Pipelines/              pipeline dạng chuỗi JSON (raw string) – không dùng LINQ
    ├── Data/                   MongoContext, DbInitializer (validator, index), Seeder
    ├── Infrastructure/         culture, auth helpers, MongoCommandFormatter (chuỗi lệnh cho toast)
    └── wwwroot/lib/            bootstrap, bootstrap-icons, chart.js (cục bộ, không CDN)
```

## Lệnh
```bash
dotnet build DT2_LMS_NHOM6.sln
dotnet run --project DT2_LMS_NHOM6                 # http://localhost:5207 ; DB trống → tự seed
dotnet run --project DT2_LMS_NHOM6 -- --reset      # xóa DB eduscore, tạo lại validator/index/seed rồi chạy
mongosh eduscore --quiet --eval "db.sinhvien.countDocuments()"
```
- Windows khóa file exe khi app đang chạy: **dừng app trước khi build** (`taskkill //IM DT2_LMS_NHOM6.exe //F` trong Git Bash, hoặc `Stop-Process -Name DT2_LMS_NHOM6` trong PowerShell).
- Khi tự kiểm tra: chạy app nền, gọi bằng `curl` (đăng nhập cần cookie: `curl -c c.txt -b c.txt`, nhớ lấy antiforgery token), xong thì dừng app. Xóa file tạm sau khi kiểm tra.

## Quy ước bắt buộc
- Tên collection/trường đúng như `docs/thiet-ke.md` mục 4. Class C#: tên tiếng Việt không dấu PascalCase (`SinhVien`, `MonHocDangKy`), `[BsonIgnoreExtraElements]`.
- Kiểu dữ liệu: `soTinChi`, `hocKy`, `lanHoc`, trọng số → `int`; điểm → `double?`; ngày → `DateTime` UTC.
- **Mọi phép tính điểm TK, chữ, hệ 4, đạt, GPA, xếp loại nằm trong pipeline** (mục 3 thiết kế). C# không tính lại điểm. **Cấm `$round`** – dùng công thức floor ở mục 3.1/3.3. Bộ stage quy đổi viết một lần, dùng chung.
- Pipeline viết bằng chuỗi JSON đọc được (C# raw string), parse thành `BsonDocument[]`, có tham số. Mỗi pipeline có bản `.json` tương ứng trong `database/pipelines/` (cập nhật khi sửa).
- Thao tác ghi trên mảng (`$push`, `$pull`, `$set`+`arrayFilters`, `updateMany` lan truyền) dựng bằng `BsonDocument` tường minh và trả về chuỗi lệnh dạng mongosh cho toast (thiết kế mục 4.9).
- Phân quyền: mỗi Area có controller cơ sở `[Area(...)] [Authorize(Roles = ...)]`. Mã SV/GV hiện tại lấy từ claim, không lấy từ request. Quyền sửa điểm của GV nằm trong `arrayFilters`.
- Form POST có antiforgery token. Validation phía server luôn có; phía client là phụ.
- Culture vi-VN nhưng dấu thập phân là dấu chấm; input điểm chấp nhận `8,5` và `8.5`.
- Giao diện tiếng Việt có dấu, theo mục 5 và 8 của thiết kế. Thông báo lỗi thân thiện, không lộ stack trace.
- Không thêm package ngoài: MongoDB.Driver, BCrypt.Net-Next, ClosedXML (+ Swashbuckle nếu làm GĐ8). Cần thêm → hỏi người dùng.
- Không viết test project trừ khi giai đoạn yêu cầu. Kiểm tra bằng build + chạy thật + mongosh.

## Cấm
- Thêm chức năng ngoài `docs/thiet-ke.md` (xem mục 9 "Ngoài phạm vi").
- Dùng LINQ/`AsQueryable` cho pipeline tính điểm; tính GPA bằng C#.
- Dùng CDN; dựng trang bằng chuỗi HTML trong JavaScript.
- Lưu điểm TK/GPA/xếp loại vào database.
- Tự ý `git push`, đổi tên project/namespace, xóa file ngoài phạm vi giai đoạn.
- Giới hạn `maxItems` cho mảng `monHoc` trong validator.

## Quy trình mỗi giai đoạn
1. Đọc file `docs/quy-trinh/GD<N>.md` được giao + các mục thiết kế liên quan. Lập danh sách việc (todo) trước khi code.
2. Làm tuần tự, build thường xuyên. Không làm việc của giai đoạn sau.
3. Tự kiểm tra theo mục "Tự kiểm tra" của giai đoạn: `dotnet build` 0 lỗi, chạy app, gọi trang/chức năng, truy vấn mongosh.
4. Cập nhật bảng tiến độ bên dưới (✅ + ghi chú ngắn) và bổ sung "Ghi chú kỹ thuật" nếu có quyết định mới.
5. Báo cáo: **ghi ra file `docs/bao-cao/GD<N>.md` theo mẫu `docs/bao-cao/_mau.md`** (đã làm gì, file chính, kết quả tự kiểm tra, quyết định kỹ thuật, điểm cần chú ý, checklist nghiệm thu, gợi ý trả lời khi bảo vệ), cập nhật liên kết trong bảng `docs/bao-cao/README.md`, rồi tóm tắt ngắn trong khung chat kèm đường dẫn file.
6. **Chờ người dùng xác nhận rồi mới commit**: `git add -A && git commit -m "Giai đoạn N: <tóm tắt>"` (commit gồm cả file báo cáo; sau commit điền mã commit vào báo cáo nếu người dùng yêu cầu).

## Tiến độ
| GĐ | Nội dung | Trạng thái |
|---|---|---|
| 1 | Nền móng: git, sln, package, models, khởi tạo DB, seeder | ✅ 100 SV, 640 lần học, 19 F, hè 12/8/5, 109 TK; validator + index OK |
| 2 | Đăng nhập, phân quyền, layout, đổi mật khẩu, trang lỗi | ✅ Cookie auth 8h, 3 Area + base controller, layout sidebar/toast, 403/404/500 tiếng Việt |
| 3 | Pipeline điểm & GPA + kiểm thử 5 ca | ⬜ |
| 4 | CRUD Admin: lớp, sinh viên, giảng viên, môn học | ⬜ |
| 5 | Ghi danh ($push/$pull) + nhập điểm ($set+arrayFilters) | ⬜ |
| 6 | Báo cáo học lực, tổng quan, cổng sinh viên | ⬜ |
| 7 | Nhập/Xuất Excel | ⬜ |
| 8 | (Tùy chọn) Hiệu năng + API/Swagger | ⬜ |
| 9 | Hoàn thiện, export DB, README, tài liệu | ⬜ |

## Ghi chú kỹ thuật
(Claude Code bổ sung các quyết định phát sinh ở đây, ngắn gọn.)
- Package: MongoDB.Driver 3.13.0, BCrypt.Net-Next 4.2.0, ClosedXML 0.105.1.
- `Program.cs` tách cờ `--reset` khỏi `args` trước `CreateBuilder`; `DbInitializer.KhoiTaoAsync` chạy trước `app.Run`. Đã bỏ `UseHttpsRedirection`/`UseHsts`.
- Validator: lệnh `create` (chưa có) / `collMod` (đã có) với cùng JSON; validator để dạng raw string trong `DbInitializer`. Index đặt tên `ux_*` (unique) / `ix_*`.
- Seeder: kế hoạch môn mỗi lớp giữ tổng ≤ 7 để SV còn chỗ 1 môn hè (tối đa 8). Bước điều chỉnh F bảo đảm ≥ 13 SV ngẫu nhiên có F và tổng F 16–19. Seeder có hàm `DiemTKMau` (cùng công thức floor) **chỉ** để chọn SV học lại/cải thiện và in số F; ứng dụng không dùng hàm này.
- Seeder chạy khi `sinhvien` trống: xóa sạch 4 collection còn lại trước khi insert để tránh trùng khóa.
- "Kém" không thể có ở GPA tích lũy (chỉ tính môn đạt, D = 1.0) – chỉ xuất hiện ở GPA học kỳ (TS4 HK2).
- Antiforgery: filter toàn cục `AutoValidateAntiforgeryTokenAttribute` → mọi POST (kể cả fetch) phải gửi token; fetch dùng header `RequestVerificationToken` với `EduScore.layAntiforgeryToken()`. Action trang lỗi có `[IgnoreAntiforgeryToken]` vì được re-execute cả với POST.
- Culture: vi-VN, `NumberDecimalSeparator="."`, `NumberGroupSeparator=" "` (không dùng "," để "8,5" không bị bind thành 85). Input điểm vẫn phải tự chuẩn hóa "," → "." ở GĐ5.
- `WebEncoderOptions` = `UnicodeRanges.All`: Razor xuất tiếng Việt nguyên dạng (không `&#x..;`).
- Toast sau redirect: `this.ThongBao(noiDung, loai, mongoCommand)` (TempData `Toast`/`ToastLoai`/`MongoCommand`); JS `toast(msg, type, cmd)` nhân bản `<template id="mau-toast">`. Xóa: nút `data-xac-nhan-xoa data-url data-noi-dung` → modal POST dùng chung.
- Hiển thị: `HienThi.Diem/Gpa/HocKy/Ngay/TenVaiTro`; TagHelper `<diem-chu gia-tri="" />`, `<xep-loai gia-tri="" />`; partial `_TrangTrong` (TrangTrongViewModel); breadcrumb `ViewData["Breadcrumb"] = MucBreadcrumb[]`.
- Menu sidebar dùng URL cứng (`/Admin/LopHoc`...) vì anchor tag helper không sinh link tới controller chưa tồn tại.
- Trạng thái "Khóa" chỉ báo khi mật khẩu đúng; khóa tài khoản đang đăng nhập không đá phiên hiện tại (cookie còn hạn).
