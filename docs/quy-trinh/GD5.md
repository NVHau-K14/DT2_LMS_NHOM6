# Giai đoạn 5 – Ghi danh ($push / $pull) + Nhập điểm ($set + arrayFilters)

Trọng tâm demo 1. Mọi câu lệnh ghi trên mảng phải trả chuỗi lệnh MongoDB hiển thị trên toast.

## Prompt
```
Đọc CLAUDE.md, docs/thiet-ke.md (mục 2.2, 2.4, 4.8, 4.9, 5.2 A-06/A-09, 5.3) và docs/quy-trinh/GD5.md. Thực hiện Giai đoạn 5 đúng theo GD5.md. Lập todo trước khi làm. Khi xong, tự kiểm tra, cập nhật tiến độ trong CLAUDE.md, báo cáo kèm hướng dẫn nghiệm thu, rồi chờ tôi xác nhận mới commit.
```

## Việc cần làm
1. **GhiDanhService**:
   - `XemTruoc(namHoc, hocKy, maMon, maGV, danhSachMaSV)`: với từng SV, lấy các lần học trước cùng maMon **từ kết quả pipeline `BangDiem`** (không tự tính điểm bằng C#) → xác định `lanHoc`, `hinhThuc` (Học mới / Học lại / Cải thiện) hoặc lý do chặn (đã ghi danh môn này trong học kỳ này; lần học trước chưa có điểm; SV không ở trạng thái "Đang học"; môn/GV không tồn tại).
   - `XacNhan(...)`: bulkWrite các `UpdateOne({maSV}, {$push: {monHoc: <phần tử>}, $set: {ngayCapNhat}})`; phần tử: `maDK` mới, ảnh chụp tenMon/soTinChi/trongSoMacDinh từ `monhoc`, `giangVien {maGV, hoTen kèm học vị}`, `diem` 3 thành phần null, `capNhatLuc/capNhatBoi` null. Trả về số ghi danh + chuỗi lệnh mongosh (dòng đầu + tổng).
   - `HuyGhiDanh(maSV, maDK)`: `$pull` với điều kiện `$elemMatch` 3 điểm null (thiết kế 4.8); không khớp → báo "Đã có điểm, không thể hủy".
2. **A-09 Ghi danh** `/Admin/GhiDanh` (nhận `?maLop=` hoặc `?maSV=` để điền sẵn): B1 chọn năm học (2025-2026, 2026-2027), học kỳ (1/2/hè), môn, GV; B2 chọn **cả lớp** hoặc **từng SV** (ô tìm MSSV/tên, chọn nhiều, danh sách đã chọn); B3 bảng xem trước (MSSV, họ tên, lớp, lần học, hình thức – badge màu, trạng thái hợp lệ/chặn + lý do); B4 Xác nhận (chỉ ghi các dòng hợp lệ) → toast kèm lệnh. Có thể làm 1 trang nhiều bước (submit form) – không cần SPA.
3. **A-06 Hồ sơ SV**: nút "Ghi danh môn" (sang A-09 với `?maSV=`), nút "Hủy ghi danh" ở dòng chưa có điểm (modal xác nhận) → toast kèm lệnh `$pull`.
4. **G-01 Tổng quan GV** `/GiangVien`: lời chào (họ tên kèm học vị), chọn học kỳ (mặc định `HocKyHienTai`, danh sách học kỳ GV có dạy), thẻ số (lớp học phần, SV, SV chưa đủ điểm), thẻ từng lớp học phần (môn, học kỳ, các lớp, sĩ số, thanh tiến độ soDuDiem/siSo, nút Nhập điểm) – từ pipeline `LopHocPhanCuaGV`.
5. **G-02** `/GiangVien/LopHocPhan`: bảng mọi lớp học phần của GV, lọc học kỳ.
6. **G-03 Nhập điểm** `/GiangVien/NhapDiem?maMon=&namHoc=&hocKy=`:
   - Kiểm tra GV có dạy lớp học phần này (không → 403).
   - Đầu trang: môn, TC, trọng số, học kỳ, sĩ số, tiến độ. Bảng từ pipeline `DanhSachNhapDiem` (GV hiện tại): MSSV, họ tên, lớp, hình thức, ô nhập CC/GK/CK, TK, Chữ (badge), Hệ 4. Lọc theo lớp, checkbox "Chỉ hiện chưa đủ điểm".
   - **Lưu từng dòng**: nút Lưu mỗi dòng → `fetch` POST JSON (antiforgery header) → `DiemService.CapNhatDiem(maGV, maSV, maDK, cc, gk, ck)` chạy `updateOne` + `$set` + `arrayFilters [{e.maDK, e.giangVien.maGV}]` (thiết kế 4.8), ghi `capNhatLuc`, `capNhatBoi` → chạy lại pipeline cho đúng SV đó lấy TK/Chữ/Hệ 4 mới → cập nhật dòng + toast kèm lệnh MongoDB. ModifiedCount = 0 → báo lỗi quyền.
   - **Lưu tất cả**: gửi các dòng đã thay đổi → bulkWrite → toast (số dòng + lệnh dòng đầu) → tải lại dữ liệu.
   - Ô nhập: chấp nhận `8,5`/`8.5`, chuẩn hóa về dấu chấm; 0–10; tối đa 1 chữ số thập phân; trống = null; dòng đã sửa chưa lưu được tô màu; cảnh báo khi rời trang còn dòng chưa lưu. Kiểm tra lại toàn bộ ở server.
   - Nút Tải mẫu/Xuất Excel/Nhập Excel để sẵn (GĐ7).

## Tự kiểm tra
- Build 0 lỗi; curl:
  - Admin ghi danh LAW101 (GV004, 2026-2027 HK1) cho `2001250010` → mongosh thấy phần tử mới cuối mảng, `hinhThuc:"Học mới"`, diem null. Ghi danh lại lần nữa → bị chặn trùng. Hủy → phần tử biến mất.
  - Xem trước ghi danh INT101 HK1 2026-2027 cho TS4 (`2001250004`, đã đạt C) → "Cải thiện"; POL101 cho TS4 (F) → "Học lại".
  - Đăng nhập GV005 → nhập CK cho TS5 môn INT301 = 9 → mongosh: `diem.ck` = 9, `capNhatBoi` "GV005"; pipeline trả TK 9.1 (10·10 + 9·30 + 9·60 = 910 → 9.1), chữ A.
  - GV005 gửi request sửa điểm một `maDK` của môn GV khác → bị từ chối, DB không đổi.
- Sau kiểm tra: `--reset` để trả dữ liệu về chuẩn.

## Nghiệm thu (bạn làm) – tập luôn demo 1
- [ ] Đăng nhập `GV005 / 123456` → Tổng quan thấy lớp học phần INT301 HK1 2026-2027 với tiến độ 0%.
- [ ] Nhập điểm CK cho 1 SV → toast hiện lệnh `updateOne ... arrayFilters`; TK/Chữ cập nhật ngay.
- [ ] Mở Compass, tìm `{maSV: "<MSSV vừa nhập>"}` → thấy `diem.ck` mới nằm đúng phần tử INT301 trong mảng `monHoc`.
- [ ] Admin: ghi danh cả lớp 16DHTH02 một môn học kỳ hiện tại → xem trước đúng → xác nhận → hồ sơ SV có môn mới. Hủy ghi danh 1 SV.
- [ ] Ghi danh học lại POL101 cho `2001250004` → hình thức "Học lại".
- [ ] `--reset` dữ liệu rồi commit.
