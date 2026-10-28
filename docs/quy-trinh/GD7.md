# Giai đoạn 7 – Nhập / Xuất Excel

Trọng tâm demo 3.

## Prompt
```
Đọc CLAUDE.md, docs/thiet-ke.md (mục 6, 5.2 A-11) và docs/quy-trinh/GD7.md. Thực hiện Giai đoạn 7 đúng theo GD7.md. Lập todo trước khi làm. Khi xong, tự kiểm tra, cập nhật tiến độ trong CLAUDE.md, báo cáo kèm hướng dẫn nghiệm thu, rồi chờ tôi xác nhận mới commit.
```

## Việc cần làm
1. **ExcelService** (ClosedXML): đọc/ghi theo thiết kế mục 6. File xuất có tiêu đề "CỔNG QUẢN LÝ HỌC TẬP – EDUSCORE", tên báo cáo, thông tin lọc, ngày xuất, header in đậm nền xanh nhạt, viền, độ rộng cột hợp lý, cột MSSV/ngày sinh định dạng Text, điểm 1 chữ số, GPA 2 chữ số. Tên file không dấu.
2. **Nhập danh sách SV**: tải mẫu (file trống đúng cột + 1 dòng ví dụ ở sheet hướng dẫn) → upload (.xlsx ≤5MB) → **kiểm tra toàn bộ** → trang kết quả: bảng từng dòng (hợp lệ xanh / lỗi đỏ + lý do) → nếu không có lỗi: nút Xác nhận → insert SV + tài khoản (bulk), toast số lượng. Có lỗi: không ghi gì, cho tải lên lại.
3. **Nhập bảng điểm**:
   - Mẫu = **xuất danh sách lớp học phần kèm điểm hiện có** (phần đầu: Mã môn, Tên môn, Năm học, Học kỳ, Giảng viên; bảng: STT, MSSV, Họ tên, Lớp, CC, GK, CK).
   - Upload → đọc phần đầu xác định lớp học phần → kiểm tra từng dòng (MSSV thuộc lớp học phần; điểm số hợp lệ 0–10, ≤1 chữ số thập phân, chấp nhận dấu phẩy; **ô trống = giữ nguyên**) → xem trước (cột điểm cũ → điểm mới, đánh dấu dòng thay đổi) → Xác nhận → bulkWrite `$set` + `arrayFilters` (GV: thêm điều kiện maGV; Admin: `capNhatBoi:"admin"`) → toast kèm lệnh dòng đầu + tổng.
   - GV chỉ nhập cho lớp học phần của mình (file của lớp học phần khác → lỗi).
4. **Gắn nút** vào các màn hình đã có: A-04 (Nhập Excel, Tải mẫu, Xuất danh sách), G-03 (Tải mẫu/Xuất bảng điểm, Nhập điểm Excel), A-10 & G-04 & A-03 (Xuất báo cáo học lực theo bộ lọc đang chọn), S-02 & A-06 (Xuất bảng điểm cá nhân: thông tin SV, từng học kỳ, GPA học kỳ, tổng kết).
5. **A-11 Nhập/Xuất dữ liệu** `/Admin/DuLieu`: trang tập trung – thẻ "Nhập danh sách sinh viên", "Nhập bảng điểm" (Admin chọn file bất kỳ lớp học phần), thẻ "Xuất bảng điểm lớp học phần" (chọn môn + học kỳ + GV từ danh sách lớp học phần có dữ liệu), "Xuất báo cáo học lực".
6. **File demo** (lệnh `-- --tao-file-demo`, ghi vào `docs/demo/`): `sinhvien_hop_le.xlsx` (5 SV mới hợp lệ, lớp 16DHKHDL01), `sinhvien_loi.xlsx` (có MSSV trùng, lớp không tồn tại, ngày sinh sai), `diem_INT301_hop_le.xlsx` (điểm CK cho lớp học phần INT301 HK1 2026-2027 của GV005), `diem_INT301_loi.xlsx` (điểm 11, chữ "abc", MSSV không thuộc lớp học phần).

## Tự kiểm tra
- Build 0 lỗi; sinh file demo; dùng curl upload từng file (multipart, có cookie + antiforgery):
  - `sinhvien_loi.xlsx` → báo đủ lỗi, DB không đổi (đếm SV trước/sau).
  - `sinhvien_hop_le.xlsx` → xác nhận → +5 SV, +5 tài khoản.
  - `diem_INT301_loi.xlsx` → báo lỗi, DB không đổi.
  - `diem_INT301_hop_le.xlsx` (GV005) → xác nhận → mongosh thấy `diem.ck` cập nhật.
- Mở thử file xuất bằng ClosedXML đọc lại không lỗi.
- `--reset` sau khi kiểm tra.

## Nghiệm thu (bạn làm) – tập luôn demo 3
- [ ] GV005 → Nhập điểm INT301 → Tải mẫu → mở Excel thấy danh sách, tiêu đề, định dạng ổn.
- [ ] Nhập `docs/demo/diem_INT301_loi.xlsx` → thấy bảng lỗi đỏ, không ghi.
- [ ] Nhập `docs/demo/diem_INT301_hop_le.xlsx` → xem trước điểm cũ → mới → xác nhận → Compass thấy điểm.
- [ ] Admin → Nhập/Xuất dữ liệu → nhập SV hợp lệ/lỗi; xuất báo cáo học lực mở bằng Excel đẹp.
- [ ] SV → Kết quả học tập → Xuất Excel.
- [ ] `--reset`, commit.
