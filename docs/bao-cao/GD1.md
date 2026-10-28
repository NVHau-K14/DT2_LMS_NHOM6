# Báo cáo Giai đoạn 1 – Nền móng: git, solution, package, models, khởi tạo DB, seeder

- **Ngày hoàn thành:** 28/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Commit:** `cd88ed8`

## 1. Đã làm gì
- Khởi tạo git (nhánh `main`), `.gitignore` (.NET + `*.user`, `c.txt`, `cookies*.txt`, `.vs/`), solution `DT2_LMS_NHOM6.sln` ở gốc; thêm MongoDB.Driver 3.13.0, BCrypt.Net-Next 4.2.0, ClosedXML 0.105.1.
- Cấu hình `MongoDB` và `HocKyHienTai` trong `appsettings.json` + class options đăng ký DI; chỉ chạy `http://localhost:5207` (bỏ profile https, `UseHttpsRedirection`).
- 9 class document (`SinhVien`, `MonHocDangKy`, `TrongSo`, `DiemThanhPhan`, `GiangVienRef`, `LopHoc`, `GiangVien`, `MonHoc`, `TaiKhoan`) đúng tên trường/kiểu ở thiết kế mục 4; hằng số enum chuỗi trong `HangSo`.
- `MongoContext` (singleton) + `DbInitializer`: cờ `--reset`, validator `$jsonSchema` cho 5 collection (create hoặc collMod), 11 index theo mục 4.7, tự seed khi `sinhvien` trống.
- `Seeder` theo mục 7: 5 lớp, 8 GV, 14 môn, 100 SV (5 SV kiểm thử cố định theo 7.5), 109 tài khoản BCrypt, `Random(2026)`, in bảng thống kê, tự `throw` khi vi phạm ràng buộc.
- Thư mục `database/export`, `database/dump`, `database/pipelines` (có `.gitkeep`).

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `DT2_LMS_NHOM6.sln`, `.gitignore` | Solution, bỏ qua file build/tạm |
| `DT2_LMS_NHOM6/Program.cs` | Đăng ký options + `MongoContext`, tách cờ `--reset`, gọi `DbInitializer` trước `app.Run` |
| `DT2_LMS_NHOM6/appsettings.json`, `Properties/launchSettings.json` | Chuỗi kết nối, học kỳ hiện tại; chỉ profile http |
| `DT2_LMS_NHOM6/Infrastructure/MongoDbOptions.cs`, `HocKyHienTaiOptions.cs` | Class options |
| `DT2_LMS_NHOM6/Models/HangSo.cs` | Hằng số vai trò, trạng thái, hình thức, giới tính, học vị |
| `DT2_LMS_NHOM6/Models/Documents/*.cs` | Map document MongoDB (`[BsonElement]` tên tiếng Việt không dấu) |
| `DT2_LMS_NHOM6/Data/MongoContext.cs` | Client, database, 5 collection có kiểu |
| `DT2_LMS_NHOM6/Data/DbInitializer.cs` | Validator `$jsonSchema`, index, gọi seeder |
| `DT2_LMS_NHOM6/Data/DuLieuMau.cs` | Bảng tĩnh 7.1–7.3, danh sách họ/tên đệm/tên/địa chỉ |
| `DT2_LMS_NHOM6/Data/Seeder.cs` | Sinh dữ liệu mẫu, kiểm tra ràng buộc, in thống kê |

## 3. Kết quả tự kiểm tra
| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | ✅ 0 lỗi, 0 cảnh báo |
| Seed | `dotnet run --project DT2_LMS_NHOM6 -- --reset` | ✅ In bảng thống kê (bên dưới), trang `/` trả 200 |
| Số SV | `db.sinhvien.countDocuments()` | ✅ 100 |
| SV mỗi lớp | `$group` theo `maLop` | ✅ 5 lớp × 20 |
| Số môn/SV | min/max `{$size:"$monHoc"}` | ✅ min 5, max 8 (TB 6.40) |
| TS1 | `findOne({maSV:"2001250001"})` | ✅ Khớp bảng 7.5 (tên, ngày sinh, 5 môn, điểm, trọng số, GV) |
| TS2 | INT101 hai lần | ✅ Lần 2: `hocKy: 3`, `lanHoc: 2`, `"Học lại"` |
| HK1 2026-2027 | phần tử có `diem.ck: null` | ✅ 155; học kỳ trước: 0 phần tử thiếu điểm |
| Kiểu dữ liệu | `$type` | ✅ soTinChi/hocKy `int`, điểm `double`, ngày `date` |
| Validator | `updateOne(... "monHoc.0.diem.cc": 11)` | ✅ Bị từ chối, mã 121 (DocumentValidationFailure) |
| Index | `getIndexes()` 5 collection | ✅ Đủ theo mục 4.7 |
| Tài khoản | `db.taikhoan.countDocuments()` | ✅ 109 (1 Admin, 8 GV, 100 SV) |
| Chạy lại | `dotnet run` (không `--reset`) | ✅ Log "Database đã có 100 sinh viên → bỏ qua seed", không lỗi |

Bảng thống kê seeder in ra:
```
Lớp 16DHTH01 / 16DHTH02 / 16DHATTT01 / 16DHHTTT01 / 16DHKHDL01 : 20 SV mỗi lớp
Số phần tử monHoc / SV : min 5, max 8, TB 6.40
Tổng lần học           : 640
Số lần F               : 19
HK hè 25-26            : học lại 12, cải thiện 8, học vượt 5
HK1 2026-2027 có ck null: 155
Tài khoản              : 109
```
Kiểm tra tạm bằng truy vấn mongosh (không nằm trong code, pipeline chính thức làm ở GĐ3): xếp loại tích lũy có đủ Xuất sắc 18, Giỏi 16, Khá 29, Trung bình 19, Yếu 18.

## 4. Quyết định kỹ thuật mới
- Cờ `--reset` được tách khỏi `args` trước `WebApplication.CreateBuilder` để không bị hiểu nhầm là tham số cấu hình.
- Validator viết dạng chuỗi JSON (raw string) trong `DbInitializer` – đọc giống hệt mục 4.6; dùng một lệnh `create`/`collMod` với cùng nội dung.
- Kế hoạch môn của mỗi lớp giữ tổng ≤ 7 để SV còn chỗ cho 1 môn học kỳ hè mà không vượt 8 phần tử. Lớp 16DHTH01, 16DHTH02 (và 16DHKHDL01 do ngẫu nhiên) có INT301 → lớp học phần INT301 của GV005 có 56 SV.
- Bước điều chỉnh F: bảo đảm ≥ 13 SV (ngoài SV kiểm thử) có môn F (đủ cho học lại + vài SV vẫn nợ) và tổng số F 16–19.
- Seeder có hàm `DiemTKMau` dùng đúng công thức floor ở mục 3.1, **chỉ** để chọn SV học lại/cải thiện và đếm số F khi sinh dữ liệu. Ứng dụng không dùng hàm này; điểm TK/GPA vẫn tính bằng pipeline (GĐ3).
- Khi `sinhvien` trống, Seeder xóa dữ liệu cũ ở 4 collection còn lại trước khi insert để tránh trùng khóa unique.

## 5. Điểm cần chú ý / hạn chế
- Xếp loại **"Kém" không thể xuất hiện ở GPA tích lũy**: GPA tích lũy chỉ tính các môn đạt (thấp nhất D = 1.0) nên luôn ≥ 1.00. "Kém" có ở GPA học kỳ (TS4 HK2 = 0.60).
- Phân bố xếp loại theo mục 7.4 chưa được in trong bảng thống kê seeder vì cần GPA (phải tính bằng pipeline, làm ở GĐ3).
- `maDK` và `_id` là ObjectId mới mỗi lần seed; mọi giá trị khác lặp lại y hệt nhờ `Random(2026)`.
- Mọi SV ở trạng thái "Đang học".
- Seed mất khoảng 5–10 giây do băm BCrypt 109 mật khẩu.

## 6. Nghiệm thu
- [ ] Mở Compass → database `eduscore` có 5 collection; `sinhvien` 100 document.
- [ ] Mở 1 sinh viên bất kỳ: họ tên tự nhiên, mảng `monHoc` 4–8 phần tử, mỗi phần tử có giảng viên, tín chỉ, trọng số, điểm; môn HK1 2026-2027 có `ck: null`.
- [ ] Tìm `{maSV: "2001250002"}`: có INT101 hai lần (lần 2 `hinhThuc: "Học lại"`, học kỳ 3).
- [ ] Tab Validation của `sinhvien` có schema; tab Indexes có đủ index.
- [ ] Commit.

## 7. Gợi ý trả lời khi bảo vệ
- **Vì sao nhúng mảng `monHoc` vào `sinhvien` thay vì tách collection?** Hồ sơ và bảng điểm luôn được đọc cùng nhau; một document chứa đủ dữ liệu để tính GPA bằng `$unwind` mà không cần `$lookup`; cập nhật điểm là cập nhật một phần tử mảng (`$set` + `arrayFilters`), nguyên tử trên một document.
- **Vì sao lưu `soTinChi`, `trongSo`, `giangVien.hoTen` trong từng phần tử?** Đó là ảnh chụp lúc ghi danh: đổi tín chỉ/trọng số trong danh mục không làm sai điểm cũ; tên GV nhúng để hiển thị ngay, khi đổi tên thì lan truyền bằng `updateMany` + `arrayFilters`.
- **Validator bảo vệ gì?** Bắt buộc trường, kiểu (`int` cho tín chỉ/học kỳ, số cho điểm), miền giá trị (điểm 0–10, trọng số 0–100, enum tiếng Việt). Ví dụ đặt điểm 11 bị MongoDB từ chối (lỗi 121) dù ứng dụng có lỗi.
- **Vì sao dữ liệu mẫu lặp lại được?** `Random(2026)` cố định; 5 SV kiểm thử có điểm viết tay để đối chiếu kết quả pipeline đã tính sẵn.
