# Giai đoạn 8 (TÙY CHỌN) – Hiệu năng + API/Swagger

Chỉ làm nếu đến chiều 29/10 vẫn đúng tiến độ. Bỏ qua được mà không ảnh hưởng các phần khác.

## Prompt
```
Đọc CLAUDE.md, docs/thiet-ke.md (mục 4.7, 5.2 A-12) và docs/quy-trinh/GD8.md. Thực hiện Giai đoạn 8 đúng theo GD8.md. Lập todo trước khi làm. Khi xong, tự kiểm tra, cập nhật tiến độ trong CLAUDE.md, báo cáo kèm hướng dẫn nghiệm thu, rồi chờ tôi xác nhận mới commit.
```

## Việc cần làm
1. **A-12 Hiệu năng** `/Admin/HieuNang`: với 3 truy vấn – (a) tìm SV theo `maSV`, (b) danh sách nhập điểm theo `monHoc.giangVien.maGV` + học kỳ, (c) tìm SV theo `monHoc.maMon` – chạy `explain` mức `executionStats` hai lần: dùng index và ép quét toàn bộ (`hint: {$natural: 1}`). Bảng so sánh: stage thắng (IXSCAN/COLLSCAN), keysExamined, docsExamined, nReturned, executionTimeMillis. Thêm đo pipeline `BaoCaoHocLuc` toàn trường chạy 20 lần: min/trung bình/max ms. Nút "Chạy đo". Giải thích ngắn dưới mỗi bảng (index multikey trên trường của mảng nhúng).
2. **API** (`/api/...`, `[ApiController]`, JSON, dùng chung service – không lặp logic; xác thực bằng cookie hiện có, Admin cho ghi, đọc theo quyền):
   - `GET /api/sinhvien?maLop=&page=`; `GET /api/sinhvien/{maSV}`; `POST /api/sinhvien`; `PUT /api/sinhvien/{maSV}`; `DELETE /api/sinhvien/{maSV}`
   - `POST /api/sinhvien/{maSV}/monhoc` (ghi danh – `$push`)
   - `PUT /api/sinhvien/{maSV}/monhoc/{maDK}/diem` (`$set` + arrayFilters; GV/Admin)
   - `GET /api/sinhvien/{maSV}/bangdiem`; `GET /api/baocao/hocluc?maLop=`
   - Các API ghi trả kèm trường `mongoCommand`.
3. **Swagger** (Swashbuckle.AspNetCore) tại `/swagger`, chỉ bật ở Development, mô tả tiếng Việt cho từng endpoint. Link "API" ở sidebar Admin.

## Tự kiểm tra
- Build 0 lỗi; `/swagger` mở được; gọi thử GET/PUT điểm qua curl với cookie.
- Trang hiệu năng hiện IXSCAN vs COLLSCAN đúng, số liệu hợp lý.

## Nghiệm thu (bạn làm)
- [ ] `/Admin/HieuNang` → Chạy đo → thấy khác biệt có/không index.
- [ ] `/swagger` → thử `GET /api/baocao/hocluc` ra JSON.
- [ ] Commit.
