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
dotnet run --project DT2_LMS_NHOM6 -- --reset --tao-file-demo   # tạo lại docs/demo/*.xlsx rồi thoát
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
| 3 | Pipeline điểm & GPA + kiểm thử 5 ca | ✅ 7 pipeline + bộ quy đổi dùng chung; `--kiem-thu-gpa` 200/200 PASS; `--export-pipelines` 8 file |
| 4 | CRUD Admin: lớp, sinh viên, giảng viên, môn học | ✅ A-02→A-08 + A9; lan truyền tên môn/GV có toast lệnh; chặn xóa đúng; phân trang SV 20; `_BangDiem` dùng chung |
| 5 | Ghi danh ($push/$pull) + nhập điểm ($set+arrayFilters) | ✅ A-09 (xem trước từ `BangDiem`, bulkWrite $push), hủy $pull ở A-06; G-01/02/03 lưu từng dòng + lưu tất cả, quyền trong arrayFilters; toast lệnh |
| 6 | Báo cáo học lực, tổng quan, cổng sinh viên | ✅ A-10/G-04 dùng chung `_BaoCaoHocLuc` (7 thẻ, biểu đồ, sắp xếp, Xem pipeline); A-01 donut + cột; A-03 thẻ xếp loại; S-01/02/03 MSSV từ claim |
| 7 | Nhập/Xuất Excel | ✅ A-11 + nút ở A-03/04/06/10, G-03/04, S-02; nhập SV (insertMany + TK, hoàn tác) và điểm (bulkWrite $set+arrayFilters, ô trống giữ nguyên) qua xem trước; `--tao-file-demo` 4 file |
| 8 | (Tùy chọn) Hiệu năng + API/Swagger | ✅ A-12 explain có/không index (IXSCAN·EXPRESS_IXSCAN vs COLLSCAN) + BaoCaoHocLuc 20 lần; 9 endpoint `/api` dùng chung service, API ghi trả `mongoCommand`; Swagger `/swagger` (Development) |
| 9 | Hoàn thiện, export DB, README, tài liệu | ⬜ |

## Ghi chú kỹ thuật
(Claude Code bổ sung các quyết định phát sinh ở đây, ngắn gọn.)
- Package: MongoDB.Driver 3.13.0, BCrypt.Net-Next 4.2.0, ClosedXML 0.105.1, Swashbuckle.AspNetCore 9.0.6 (GĐ8).
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
- Pipeline: chuỗi JSON có chỗ giữ `"@ten"` → `PipelineJson.Tao(json, thamSo)` thay bằng BsonValue; `"@ten"` làm phần tử mảng pipeline = chèn cả cụm stage. `"@quyDoi"`/`"@gpaXepLoai"` luôn có sẵn (định nghĩa duy nhất ở `QuyDoiDiemStages`). Cụm GPA cần trường `tongDiemTC`, `tongTinChiGpa` từ `$group`.
- `BangDiem.tinhTichLuy`: `$setWindowFields` + `$first` maDK (window unbounded) thay `$documentNumber` – server không cho `$documentNumber` với sortBy nhiều khóa.
- `BaoCaoHocLuc` dùng chung cho 2 phạm vi: `$filter` lần học theo phạm vi + `"@dieuKienTinhGpa"` (tích lũy: `dat=true`; học kỳ: `duDiem=true`, tính cả F). Phạm vi học kỳ: tinChiTichLuy = TC đạt trong kỳ, soMonNo = số F trong kỳ.
- Service: `DiemService` (BangDiem, GpaHocKy, LopHocPhanCuaGV, DanhSachNhapDiem), `BaoCaoService` (BaoCaoHocLuc, ThongKeXepLoai, GpaTheoLop) trả `KetQuaPipeline<T>` (DuLieu, PipelineJson, ThoiGianMs). Tham số: `BoLocSinhVien.*`, `PhamViBaoCao.TichLuy()/TheoHocKy()`.
- `--kiem-thu-gpa` / `--export-pipelines`: chạy sau `DbInitializer`, rồi thoát. Sửa pipeline → chạy lại cả hai, commit `docs/kiem-thu-gpa.md` + `database/pipelines/*.json`. Kiểm thử cần dữ liệu mẫu nguyên bản (đã sửa điểm TS → `--reset`).
- GĐ4: service CRUD trả `KetQuaGhi` (ThanhCong, ThongBao, TruongLoi, MongoCommand); controller dùng `ThongBaoKetQua`/`GanLoi`/`QuayLai` ở `AdminControllerBase`. `quayLai` chỉ nhận URL nội bộ.
- Modal thêm/sửa dùng chung: nút `data-mo-modal="#id" data-gia-tri='<JSON form>'`, ô `name="Form.X"`, ô khóa khi sửa `data-khoa-khi-sua`; POST lỗi → render lại Index với `MoModal=true` (`data-mo-san`). Hộp xác nhận nhận thêm `data-tieu-de/data-nut/data-ghi-chu` (dùng cho reset/khóa).
- `MongoCommandFormatter` tự in cú pháp mongosh (ObjectId(), ISODate(), khóa trần) – không dùng `ToJson` của driver (driver 3.x bỏ chế độ Shell).
- Sĩ số lớp, số lần học/môn: pipeline nhỏ `ThongKeDanhMuc` (`SiSoTheoLop`, `SoLanHocTheoMon`) – có bản JSON.
- "GV đang được phân công dạy" = còn bất kỳ phần tử `monHoc` nào có `giangVien.maGV` (mọi học kỳ) – xóa sẽ làm lịch sử mất tham chiếu.
- Ngày sinh SV hợp lệ: năm ≥ 1950 và đủ 15 tuổi. Mã lớp/GV/môn: 3–20 chữ/số, lưu chữ hoa.
- Test bằng curl trong Git Bash làm hỏng tiếng Việt (tham số theo code page ANSI) → dùng script Python urllib khi POST dữ liệu có dấu.
- GĐ5 ghi danh: xem trước là GET (`/Admin/GhiDanh?...&xemTruoc=true`), xác nhận POST rồi redirect về xem trước (PRG). `XacNhanAsync` chạy lại xem trước ở server. Filter `$push` có thêm `monHoc: {$not: {$elemMatch: {maMon, namHoc, hocKy}}}` chống ghi trùng khi bấm đồng thời. bulkWrite `ordered: false`. Hình thức/lanHoc từ `BangDiem` (chạy mỗi SV một lần).
- Nhập điểm: filter có `monHoc: {$elemMatch: {maDK, "giangVien.maGV"}}` lặp lại điều kiện arrayFilters – nếu chỉ có arrayFilters, lệnh không quyền vẫn ghi `ngayCapNhat` (modifiedCount = 1). `capNhatLuc` luôn đổi nên modifiedCount = 0 ⇔ không quyền. `maGV = null` = Admin (GĐ7).
- Điểm gửi lên dạng chuỗi; `Infrastructure/DiemNhap.TryParse` chuẩn hóa "," → ".", regex `^\d{1,2}(\.\d)?$`, ≤ 10, trống = null. Lưu tất cả: một dòng sai → không ghi dòng nào.
- `LenhUpdateOne` (record) dùng chung để chạy (`TaoModel()`) và in lệnh (`MongoCommandFormatter.BulkWriteUpdateOne`: lệnh đầu + "tổng cộng N lệnh").
- JSON trả về JS gồm TK/chữ/hệ 4 đã định dạng + lớp badge (`DongDiemJson`) – JS chỉ đặt vào ô. `<select data-tu-gui>` tự gửi form GET. `_BangDiem` có `QuayLaiHuyGhiDanh` (null = không hiện cột hủy).
- GV: `GiangVienControllerBase.MaGV` lấy từ claim. G-03 lớp học phần không dạy → `StatusCode(403)` (trang lỗi tiếng Việt).
- GĐ6: partial `_BaoCaoHocLuc` (A-10, G-04) + `_TheXepLoai` (A-10, G-04, A-03). Thẻ số/biểu đồ lấy từ `ThongKeXepLoai` (cùng bộ lọc + phạm vi với bảng); `BaoCaoHocLucViewModel.DuBayMuc` chỉ xếp vào đủ 7 mức (`HangSo.CacXepLoai`), mức không có SV = 0.
- Biểu đồ: server dựng `CauHinhBieuDo` → JSON trong `<canvas data-bieu-do>`; `wwwroot/js/bieu-do.js` chỉ vẽ. Trang có biểu đồ nạp `lib/chart.js/dist/chart.umd.js` + `bieu-do.js` ở section Scripts.
- Modal "Xem pipeline": `PipelineDaChay` (Json, ThoiGianMs, SoDong, SoStage đếm bằng `JsonDocument`) – 2 tab BaoCaoHocLuc + ThongKeXepLoai; nút `data-sao-chep="#id"` (eduscore.js).
- Sắp xếp bảng phía client: `table[data-sap-xep-bang]` + `th[data-sap-xep="so|chu"]`, ô có `data-gia-tri` sắp theo giá trị đó (GPA null = -1, xếp loại = thứ tự 1..7), ô `data-stt` đánh số lại.
- A-10 phạm vi: `tichluy` hoặc `namHoc_hocKy`; chỉ nhận học kỳ có trong pipeline `HocKyCoDuLieu` và lớp có thật, giá trị lạ → mặc định. Phạm vi học kỳ: cột đổi tên "TC đạt", "Số môn F".
- G-04 chỉ phạm vi tích lũy (G5). Bộ lọc luôn chứa maGV từ claim (`TheoGiangVien` / `TheoLopHocPhan`); `lhp` không thuộc GV → bỏ qua. Không có link hồ sơ SV (`UrlHoSoSV = null`).
- Cổng SV: `SinhVienControllerBase.MaSV` từ claim; action không có tham số maSV. S-01 lọc môn học kỳ hiện tại từ kết quả `BangDiem`; biểu đồ GPA chỉ vẽ học kỳ đã có GPA. S-02 = `_BangDiem` (HienTongKetCuoi, không cột hủy).
- GĐ7 Excel: `ExcelService` (static, chỉ ClosedXML – không DB, không tính điểm); `XuatExcelService` lấy dữ liệu bằng đúng pipeline của màn hình; `NhapExcelService` kiểm tra; `TepExcel(TenFile, NoiDung)`; lỗi mức file = `LoiFileExcelException` (thông báo thân thiện → toast).
- Phiên nhập: `KhoPhienNhap` (IMemoryCache 30 phút, khóa Guid, gắn `User.TenDangNhap()`), không dùng TempData cookie (quá lớn). PRG: POST file → kiểm tra → lưu phiên → redirect GET `XemTruoc...?ma=` → POST `XacNhan...` → xóa phiên.
- Bố cục file xuất: dòng 1 tiêu đề hệ thống, dòng 2 tên báo cáo, các dòng "Nhãn:" (A:B gộp) | giá trị Text (C:cuối gộp), 1 dòng trống, header. `GopO` chỉ ghi ô đầu rồi gộp (gán Value cho cả range sẽ lặp giá trị → đọc lại sai).
- Đọc bảng điểm: phần đầu tra theo nhãn ("Mã môn", "Năm học", "Học kỳ", "Giảng viên" = "GV005 - ThS. ..." lấy phần trước " - "), header tìm theo ô MSSV/CC/GK/CK. Ô số đọc theo InvariantCulture rồi qua `DiemNhap.TryParse` (giống G-03). Lớp học phần lấy từ file, không từ trang; GV khác maGV → lỗi file. Chỉ `$set` thành phần có giá trị mới khác điểm cũ (`DiemService.TaoLenhSetDiem` dùng chung với lưu từng dòng).
- Nhập SV: kiểm tra lại trùng MSSV/tên đăng nhập lúc xác nhận; insertMany sinhvien → `TaiKhoanService.TaoNhieuAsync` (insertMany taikhoan); lỗi → xóa theo `_id` vừa sinh. Toast in `MongoCommandFormatter.InsertMany` (document đầu + tổng).
- Pipeline nhỏ `LopHocPhanToanTruong` (ô chọn xuất bảng điểm A-11, mã `INT301_2026-2027_1_GV005`). Tên file bảng điểm lớp học phần thêm mã GV: `BangDiem_INT301_2026-2027_HK1_GV005.xlsx`.
- `--tao-file-demo`: chạy sau DbInitializer, cần dữ liệu nguyên bản (`--reset --tao-file-demo`). File có "Ngày xuất" nên mỗi lần tạo khác nhị phân.
- GĐ8 API: `Controllers/Api/` (`ApiControllerBase`: `[ApiController] [Authorize] [IgnoreAntiforgeryToken]`, `KetQua(KetQuaGhi)` → JSON `KetQuaApi {thanhCong, thongBao, mongoCommand, duLieu, loi}`). Bỏ antiforgery vì body JSON + cookie SameSite=Lax. Cookie events: `/api` → 401/403 JSON thay vì redirect; `UseStatusCodePagesWithReExecute` chỉ áp dụng ngoài `/api` (`UseWhen`). Lỗi model → `InvalidModelStateResponseFactory` tiếng Việt.
- Quyền API: Admin đọc/ghi; GV đọc SV có học mình (`SinhVienService.CoLanHocCuaGiangVienAsync`), nhập điểm qua `DiemService.CapNhatDiemAsync(maGV từ claim)`, báo cáo `TheoGiangVien(maGV).VaLop(maLop)`; SV chỉ đọc chính mình. PUT điểm: số → chuỗi invariant → `DiemNhap` (giống G-03); modifiedCount 0 → 404. Ghi danh API = `XemTruocAsync` (lấy lý do chặn) + `XacNhanAsync` (chế độ từng SV), trả lần học mới từ `BangDiem`.
- JSON MVC: `ObjectIdJsonConverter` (ObjectId ↔ chuỗi hex) + `UnsafeRelaxedJsonEscaping`. `SinhVienFormViewModel.LaSua/DsLop` có `[JsonIgnore]` để API dùng lại form. Thêm/sửa/xóa SV giờ trả `MongoCommand` (insertOne/updateOne/deleteOne sinhvien + taikhoan) → toast web cũng hiện lệnh; `TaiKhoanService.TaoAsync` trả document đã chèn.
- Swagger: `GenerateDocumentationFile` (NoWarn 1591;1573), mô tả từ `///` summary/remarks/response; `DocInclusionPredicate` chỉ `api/`; `MapType<ObjectId>` = string. Chỉ `UseSwagger` khi Development; link "API" ở sidebar Admin cũng chỉ hiện khi Development.
- A-12: `HieuNangService` chạy `db.runCommand({explain: {find, filter, [hint: {$natural: 1}]}, verbosity: "executionStats"})`, duyệt cây `winningPlan` (`queryPlan`/`inputStage(s)`) lấy stage, indexName, isMultiKey. Truy vấn (b) dùng `$elemMatch` từ `DieuKienLanHoc`. Tham số mặc định = SV đầu tiên có lần học trong học kỳ hiện tại. Form GET `chay=true`. Truy vấn (a) trên MongoDB 8 ra `EXPRESS_IXSCAN`.
