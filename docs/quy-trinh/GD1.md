# Giai đoạn 1 – Nền móng: git, solution, package, models, khởi tạo DB, seeder

## Prompt
```
Đọc CLAUDE.md, docs/thiet-ke.md (mục 4 và 7) và docs/quy-trinh/GD1.md. Thực hiện Giai đoạn 1 đúng theo GD1.md. Lập todo trước khi làm. Khi xong, tự kiểm tra theo mục "Tự kiểm tra", cập nhật tiến độ trong CLAUDE.md, báo cáo kèm hướng dẫn nghiệm thu, rồi chờ tôi xác nhận mới commit.
```

## Việc cần làm
1. **Git & solution** (tại thư mục gốc): `git init` (nhánh `main`) nếu chưa có; `dotnet new gitignore`; thêm vào .gitignore: `*.user`, `c.txt`, `cookies*.txt`, `.vs/`. Tạo `DT2_LMS_NHOM6.sln` ở gốc và add project.
2. **Package** (bản ổn định mới nhất tương thích net9.0): `MongoDB.Driver`, `BCrypt.Net-Next`, `ClosedXML`.
3. **Cấu hình** `appsettings.json`:
   ```json
   "MongoDB": { "ConnectionString": "mongodb://localhost:27017", "DatabaseName": "eduscore" },
   "HocKyHienTai": { "NamHoc": "2026-2027", "HocKy": 1 }
   ```
   Class options tương ứng, đăng ký DI. Bỏ profile https khỏi `launchSettings.json` (chỉ dùng http://localhost:5207) và bỏ `UseHttpsRedirection`.
4. **Models/Documents**: `SinhVien`, `MonHocDangKy` (phần tử mảng), `TrongSo`, `DiemThanhPhan`, `GiangVienRef`, `LopHoc`, `GiangVien`, `MonHoc`, `TaiKhoan` – đúng tên trường, kiểu dữ liệu ở thiết kế mục 4 (`int` cho tín chỉ/học kỳ/lần học/trọng số; `double?` cho điểm; `DateTime` UTC). Hằng số enum chuỗi (vai trò, trạng thái, hình thức, giới tính) đặt trong một class hằng.
5. **Data/MongoContext**: singleton, expose 5 `IMongoCollection<T>` + `IMongoDatabase`.
6. **Data/DbInitializer** (chạy lúc khởi động, trước `app.Run`):
   - Đọc cờ `--reset`: drop database `eduscore`.
   - Tạo collection kèm validator `$jsonSchema` (sinhvien đúng mục 4.6; 4 collection còn lại: required + kiểu + enum cơ bản). Collection đã có → `collMod`.
   - Tạo index đúng mục 4.7 (đặt tên index dễ đọc).
   - Nếu `sinhvien` trống → chạy Seeder.
7. **Data/Seeder** đúng mục 7: lớp, GV, môn, 100 SV (gồm 5 SV kiểm thử TS1–TS5 dữ liệu cố định y hệt bảng 7.5), tài khoản (admin, 8 GV, 100 SV; mật khẩu mặc định mục 4.5, BCrypt work factor 10). `Random(2026)`. Danh sách họ/tên đệm/tên tiếng Việt đủ đa dạng, tên đệm theo giới tính. `maDK` mỗi phần tử là ObjectId mới. Dùng `insertMany`.
   - Cuối seed in ra console bảng thống kê: số SV mỗi lớp; min/max/trung bình số phần tử monHoc; số lần F; số học lại/cải thiện/học vượt; số phần tử HK1 26-27 có ck null; số tài khoản.
   - Seeder tự `throw` nếu vi phạm: SV có <4 hoặc >8 môn; lớp không đủ 20 SV.
8. Tạo thư mục rỗng có `.gitkeep`: `database/export`, `database/dump`, `database/pipelines`.

## Tự kiểm tra (Claude Code chạy)
- `dotnet build DT2_LMS_NHOM6.sln` → 0 lỗi, 0 cảnh báo nullable quan trọng.
- `dotnet run --project DT2_LMS_NHOM6 -- --reset` → seed thành công, in bảng thống kê; dừng app.
- mongosh:
  - `db.sinhvien.countDocuments()` = 100; `db.sinhvien.aggregate([{$group:{_id:"$maLop",n:{$sum:1}}}])` → 5 lớp × 20.
  - min/max `{$size:"$monHoc"}` nằm trong 4–8.
  - `db.sinhvien.findOne({maSV:"2001250001"})` khớp TS1.
  - Đếm phần tử `namHoc:"2026-2027"` có `diem.ck: null` > 0 và mọi phần tử học kỳ trước có đủ điểm.
  - Thử `db.sinhvien.updateOne({maSV:"2001250001"},{$set:{"monHoc.0.diem.cc":11}})` → phải bị validator từ chối.
  - `db.sinhvien.getIndexes()` đủ index mục 4.7; `db.taikhoan.countDocuments()` = 109.
- Chạy lại `dotnet run` (không --reset) → không seed lại, không lỗi.

## Nghiệm thu (bạn làm)
- [ ] Mở Compass → database `eduscore` có 5 collection; `sinhvien` 100 document.
- [ ] Mở 1 sinh viên bất kỳ: họ tên tự nhiên, mảng `monHoc` 4–8 phần tử, mỗi phần tử có giảng viên, tín chỉ, trọng số, điểm; môn HK1 2026-2027 có `ck: null`.
- [ ] Tìm `{maSV: "2001250002"}`: có INT101 hai lần (lần 2 `hinhThuc: "Học lại"`, học kỳ 3).
- [ ] Tab Validation của `sinhvien` có schema; tab Indexes có đủ index.
- [ ] Commit.
