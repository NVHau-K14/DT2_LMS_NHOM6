# Báo cáo Giai đoạn 9 – Hoàn thiện, export database, README, tài liệu

- **Ngày hoàn thành:** 29/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Commit:** `<mã commit>` (điền sau khi người dùng nghiệm thu và commit)

## 1. Đã làm gì
- **Rà soát toàn bộ màn hình** 3 vai trò bằng 2 cách:
  - Script tự đăng nhập `admin`, `GV005`, `2001250001`, `2001250004` và đi theo mọi link nội bộ (Admin 441 trang, GV 9, SV 5 + 5). Kết quả: không có link chết hay trang lỗi, tài nguyên tĩnh đều 200.
  - Chrome headless mở 30 trang chính ở 1366px và 375px. Kết quả: 0 lỗi JavaScript, 0 request lỗi; tiêu đề và breadcrumb thống nhất.
  - Đã sửa:
    - (a) Ô chọn lớp học phần ở A-11 làm trang tràn ngang khi màn hình hẹp.
    - (b) Ô chọn file hiện chữ tiếng Anh "Choose File / No file chosen" của trình duyệt → partial `_ChonFile` với nhãn "Chọn file / Chưa chọn file" (A-11 và modal Nhập Excel ở A-04, G-03).
    - (c) `aria-label="breadcrumb"` → tiếng Việt.
    - (d) Comment cũ "mục chưa làm tạm thời 404" ở sidebar và "giai đoạn sau" ở `eduscore.js`.
- **Xử lý lỗi**:
  - Khi khởi động, ứng dụng ping MongoDB, chờ tối đa 5 giây. Không kết nối được thì console in `Không kết nối được MongoDB tại localhost:27017 (database "eduscore")` kèm cách khắc phục rồi thoát (mã 1).
  - MongoDB mất kết nối giữa chừng: trang 500 báo "Không kết nối được cơ sở dữ liệu", lỗi được ghi log, không lộ stack trace.
- **Export database** – lệnh `-- --export`:
  - Ghi `database/export/*.json` cho 5 collection: Extended JSON relaxed, dạng mảng, giữ `_id`/ObjectId/Date.
  - Có `mongodump` thì ghi thêm `database/dump/eduscore.archive.gz`.
  - Script `database/khoi-phuc.ps1` thử lần lượt: mongorestore → mongoimport → mongosh → in hướng dẫn. Kèm `database/nhap-json.mongosh.js` và `database/README.md` (4 cách khôi phục).
- **README.md** gốc repo, gồm: giới thiệu, chỗ đặt ảnh (`docs/images/README.md` liệt kê 6 file cần chụp), công nghệ, yêu cầu cài đặt, các bước chạy, bảng tài khoản demo, các lệnh, cấu trúc thư mục, mô hình dữ liệu, chức năng theo vai trò + API, đối chiếu barem, bảng thông tin nhóm để trống.
- **Tài liệu**:
  - `docs/thiet-ke-csdl.md`: giải trình Document Store, so sánh mô hình quan hệ, nhúng/tham chiếu từng quan hệ, denormalization + lan truyền, giới hạn 16 MB (số đo thật), nhược điểm và cách xử lý, JSON Schema đầy đủ, 2 sơ đồ Mermaid, bảng index, giải thích pipeline và từng toán tử mảng kèm lệnh mẫu.
  - `docs/kich-ban-demo.md`: kịch bản ~10 phút cho 1 người, chia 3 phần, có truy vấn Compass Q1–Q5, lời nói gợi ý và bảng dự phòng.
  - Chạy lại `--kiem-thu-gpa` và `--export-pipelines`.

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `DT2_LMS_NHOM6/Data/XuatDuLieu.cs` (mới) | Lệnh `--export`: 5 file JSON + mongodump nếu có |
| `DT2_LMS_NHOM6/Data/MongoContext.cs` | `ServerSelectionTimeout` 5 giây, `KiemTraKetNoiAsync` (ping), `DiaChiMayChu`, `ConnectionString` |
| `DT2_LMS_NHOM6/Program.cs` | Cờ `--export`; kiểm tra kết nối trước `DbInitializer`; console UTF-8 |
| `DT2_LMS_NHOM6/Controllers/HomeController.cs`, `Views/Home/StatusCode.cshtml` | Trang 500 nhận diện lỗi mất kết nối MongoDB |
| `Views/Shared/_ChonFile.cshtml`, `ViewModels/ChonFileViewModel.cs` (mới) | Ô chọn file nhãn tiếng Việt dùng chung |
| `Areas/Admin/Views/DuLieu/Index.cshtml`, `Views/Shared/_ModalNhapExcel.cshtml` | Dùng `_ChonFile`; sửa ô chọn lớp học phần tràn ngang |
| `wwwroot/js/eduscore.js`, `wwwroot/css/site.css` | `khoiTaoChonFile`; CSS `.chon-file` |
| `Views/Shared/_Layout.cshtml`, `_Sidebar.cshtml` | aria-label tiếng Việt, comment cập nhật |
| `database/export/*.json` (mới) | Dữ liệu chuẩn sau `--reset`: 100 SV, 5 lớp, 8 GV, 14 môn, 109 tài khoản |
| `database/khoi-phuc.ps1`, `database/nhap-json.mongosh.js`, `database/README.md` (mới) | Khôi phục database |
| `README.md` (mới) | Giới thiệu và hướng dẫn chạy |
| `docs/thiet-ke-csdl.md`, `docs/kich-ban-demo.md`, `docs/images/README.md` (mới) | Tài liệu báo cáo, kịch bản demo, danh sách ảnh cần chụp |
| `docs/kiem-thu-gpa.md` | Chạy lại (200/200 PASS) |

## 3. Kết quả tự kiểm tra
| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | ✅ 0 cảnh báo, 0 lỗi |
| Kiểm thử GPA | `--kiem-thu-gpa` trên dữ liệu chuẩn | ✅ 200/200 PASS |
| Pipeline JSON | `--export-pipelines` | ✅ 12 file, không thay đổi so với GĐ8 |
| MongoDB không chạy | `MongoDB__ConnectionString=mongodb://localhost:27999 dotnet run ...` | ✅ Sau ~6 giây in "Không kết nối được MongoDB tại localhost:27999 …", mã thoát 1 |
| MongoDB mất giữa chừng | Chạy `mongod` tạm cổng 27555, đăng nhập admin, tắt `mongod`, mở `/Admin/SinhVien` | ✅ 500 "Không kết nối được cơ sở dữ liệu", không lộ stack trace; log `fail: ExceptionHandlerMiddleware` |
| Link chết / trang lỗi | Script đi theo mọi link của 3 vai trò | ✅ 0 trang lỗi (ngoài 403/404 cố ý thử) |
| Lỗi JS console, responsive | Chrome headless, 30 trang × 2 kích thước (1366 / 375px) | ✅ 0 lỗi JS; 375px không tràn ngang (trừ Swagger UI – trang của thư viện) |
| Chữ tiếng Anh | Lọc từ ASCII trên mọi trang + xem ảnh chụp | ✅ Chỉ còn thuật ngữ kỹ thuật (Excel, Compass, Swagger, Email, toán tử pipeline) |
| Ô chọn file | Chrome headless: chọn file → nhãn hiện tên file; chưa chọn → không gửi được; gửi → trang xem trước | ✅ |
| Export | `--reset --export` | ✅ 5 file JSON (sinhvien 100, lophoc 5, giangvien 8, monhoc 14, taikhoan 109) + `database/dump/eduscore.archive.gz` (24 KB, mongodump 100.20.0). Trước khi cài Database Tools: bỏ qua dump, có thông báo |
| Khôi phục bằng mongorestore | `khoi-phuc.ps1 -TenDb eduscore_restoretest` (đường mongorestore, `--nsFrom/--nsTo`) | ✅ 236 document; validator + index (sinhvien 6, taikhoan 3, còn lại 2) được khôi phục; `_id` khớp; điểm `double`, TC `int`; `--kiem-thu-gpa` 200/200 PASS |
| Khôi phục bằng mongoimport | 5 lệnh `mongoimport --jsonArray --drop` (như script / README) vào `eduscore_importtest` | ✅ Đủ document, điểm giữ `double`, ngày `date`; 200/200 PASS |
| Khôi phục bằng mongosh | `khoi-phuc.ps1 -TenDb eduscore_restoretest` (đường mongosh, chạy khi chưa có Database Tools) → chạy app `--kiem-thu-gpa` trên DB đó | ✅ Đủ số document, `_id` trùng bản gốc, điểm `double`, TC `int`; validator + 6 index tạo lại; 200/200 PASS; đã xóa DB tạm |
| Mermaid | Nạp mermaid 11.4.1 vào Chrome, `parse` + `render` 2 sơ đồ, xem ảnh | ✅ Cả 2 hợp lệ (đã sửa 1 thuộc tính bị hiểu nhầm thành phương thức) |
| Clone sạch | `git clone` ra thư mục tạm + chép đúng các file sắp commit, `MongoDB__DatabaseName=eduscore_clonetest dotnet run` | ✅ Build 0 lỗi; DB trống → tự seed 100 SV / 640 lần học / 109 tài khoản; `/Account/Login` 200, `/` → Login |
| Kịch bản demo | Chạy trên bản clone bằng Chrome headless | ✅ CK "11" báo lỗi, "8,5" lưu → 8.8/A/4, toast có lệnh `$set`+`arrayFilters`; báo cáo TS5 3.69 Xuất sắc, TS4 1.50 Yếu / HK2 0.60 Kém; file lỗi không có nút xác nhận; file hợp lệ "Xác nhận ghi điểm (56 sinh viên)", TS5 "8.5 → 4.0"; Q2 = 56, Q3 = 56; nhập 5 SV + 5 tài khoản (Q4 = 5) |
| Dọn dẹp | `git status` | ✅ Không có file rác; đã xóa DB `eduscore_clonetest`, thư mục clone, file tạm |

## 4. Quyết định kỹ thuật mới
- Chờ chọn máy chủ MongoDB 5 giây thay vì 30 giây mặc định. Khởi động thì ping trước để báo lỗi rõ, không để lỗi timeout dài khó hiểu.
- Lệnh `--export` tự phát hiện `mongodump` (chạy `mongodump --version`); có thì ghi thêm dump `--archive --gzip` – bản khôi phục trọn vẹn nhất (gồm validator, index).
- File export dạng relaxed + indent, sắp theo `_id` → xuất lại cùng dữ liệu ra cùng file (dễ xem `git diff`).
- Script mongosh nhập JSON tự đổi điểm về `double`, vì JSON mất phân biệt `8.0` với `8`. Validator vẫn cho phép `int` nên nhập bằng Compass cũng không làm sai GPA (đã kiểm thử).
- Thay ô chọn file gốc của trình duyệt bằng nhãn tiếng Việt (input ẩn kiểu `visually-hidden`, vẫn Tab tới được, vẫn `required`).
- Không tự tạo ảnh `docs/images/*.png`. GĐ9 ghi rõ người dùng bổ sung; tên file cần chụp có ở `docs/images/README.md`.

## 5. Điểm cần chú ý / hạn chế
- `database/export/*.json` và `database/dump/eduscore.archive.gz` là cùng một lần xuất, dữ liệu chuẩn ngay sau `--reset`.
- `_id` và `maDK` sinh mới sau mỗi `--reset` (nội dung giống hệt). Ví dụ ObjectId của TS5 trong `docs/thiet-ke-csdl.md` khớp bản xuất đang nộp; khi demo, lấy `maDK` thật trong Compass hoặc toast.
- Terminal mở **trước** khi cài Database Tools sẽ không thấy `mongodump`; mở terminal mới (PATH máy đã có `C:\Program Files\MongoDB\Tools\100\bin`). Nếu sau demo muốn nộp dữ liệu đã thay đổi, chạy `-- --export` (không `--reset`).
- README có ảnh chưa tồn tại (sẽ hiện ảnh vỡ trên GitHub tới khi bạn chụp) và bảng thành viên nhóm để trống – điền trong GĐ10.
- `docs/bao-cao/GD8.md` đang có thay đổi của bạn (điền mã commit `5e007dc`), sẽ đi cùng commit GĐ9.
- Lỗi kiểm tra dữ liệu dạng `required` của trình duyệt ("Please select a file…") vẫn theo ngôn ngữ trình duyệt; server luôn kiểm tra lại và báo tiếng Việt.
- Clone sạch được kiểm tra trên trạng thái **sắp commit** (clone HEAD rồi chép các file thay đổi), vì GĐ9 chưa commit. Sau khi commit, có thể clone lại để chắc chắn (mục 6).
- Sau khi tôi tự kiểm tra, database `eduscore` vẫn là dữ liệu chuẩn (chỉ đăng nhập, không ghi điểm). App đã dừng.

## 6. Nghiệm thu

Hướng dẫn nhanh:
1. `dotnet run --project DT2_LMS_NHOM6 -- --reset` → mở http://localhost:5207.
2. Đọc `README.md` (xem trước Markdown trong VS Code: Ctrl+Shift+V) như người lạ: các bước chạy, bảng tài khoản, lệnh.
3. Đọc `docs/thiet-ke-csdl.md` (xem trước Markdown để thấy sơ đồ Mermaid) và `docs/kich-ban-demo.md`; thử chạy theo kịch bản một lần (khoảng 10 phút) – sau đó `--reset`.
4. Đi một vòng 3 vai trò: `admin`/`Admin@123`, `GV005`/`123456`, `2001250001`/`15032007`. Chú ý trang Nhập/Xuất dữ liệu (ô "Chọn file" mới), thu hẹp cửa sổ để xem bản điện thoại.
5. Thử lỗi kết nối (tùy chọn): dừng app, chạy `$env:MongoDB__ConnectionString="mongodb://localhost:27999"; dotnet run --project DT2_LMS_NHOM6` trong PowerShell → thông báo đỏ; sau đó `Remove-Item Env:MongoDB__ConnectionString`.
6. Thử khôi phục (tùy chọn): `powershell -ExecutionPolicy Bypass -File database\khoi-phuc.ps1 -TenDb eduscore_thu` → rồi `mongosh --eval "db.getSiblingDB('eduscore_thu').dropDatabase()"`.

- [ ] Đọc README như người lạ: làm theo được.
- [ ] Đọc `docs/thiet-ke-csdl.md` và `docs/kich-ban-demo.md` – đủ để bạn trình bày.
- [ ] Đi một vòng 3 vai trò, không thấy lỗi giao diện.
- [ ] Commit. (GĐ 10: xem `docs/quy-trinh/README.md`.)

## 7. Gợi ý trả lời khi bảo vệ
- **"Vì sao nhúng mảng môn học vào sinh viên mà không tách collection đăng ký?"** – Bảng điểm luôn đọc cùng hồ sơ → 1 document, không JOIN. Ghi danh và nhập điểm chỉ đụng 1 document nên nguyên tử, không cần transaction. Mảng có giới hạn tự nhiên: ~350 B/lần học, 60–70 lần học ≈ 25 KB, xa giới hạn 16 MB.
- **"Không có transaction thì tạo SV + tài khoản xử lý thế nào?"** – Ghi tuần tự: chèn SV trước, chèn tài khoản sau; bước sau lỗi thì xóa SV vừa chèn. Các thao tác nghiệp vụ chính chỉ trên 1 document.
- **"Dữ liệu trùng lặp (tên môn, tên GV) thì sao?"** – Trùng lặp có chủ đích để đọc nhanh. Khi nguồn đổi thì lan truyền bằng `updateMany` + `arrayFilters` (toast hiện lệnh và số bản ghi). Riêng tín chỉ/trọng số là ảnh chụp lúc ghi danh, cố ý không lan truyền.
- **"Khôi phục dữ liệu nộp kèm thế nào?"** – Chạy app là tự seed (cùng nội dung, seed 2026); hoặc `database\khoi-phuc.ps1` nhập lại đúng các file `export/*.json` (giữ `_id`); hoặc Compass Import JSON; có Database Tools thì `mongorestore`.
