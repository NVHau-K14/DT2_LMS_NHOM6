# Giai đoạn 6 – Báo cáo học lực, tổng quan, cổng sinh viên

Trọng tâm demo 2.

## Prompt
```
Đọc CLAUDE.md, docs/thiet-ke.md (mục 3.6, 5.2 A-01/A-03/A-10, 5.3 G-04, 5.4) và docs/quy-trinh/GD6.md. Thực hiện Giai đoạn 6 đúng theo GD6.md, dùng lại pipeline GĐ3 và partial _BangDiem GĐ4. Lập todo trước khi làm. Khi xong, tự kiểm tra, cập nhật tiến độ trong CLAUDE.md, báo cáo kèm hướng dẫn nghiệm thu, rồi chờ tôi xác nhận mới commit.
```

## Việc cần làm
1. **A-10 Báo cáo học lực** `/Admin/BaoCao`: lọc lớp (tất cả/từng lớp), phạm vi (Tích lũy / từng học kỳ có dữ liệu); hàng thẻ số theo 7 mức xếp loại (kể cả Chưa xếp loại) + biểu đồ cột (Chart.js); bảng MSSV, họ tên, lớp, TC tích lũy, GPA, xếp loại (badge), số môn nợ – sắp xếp được theo cột (phía client là đủ), link sang hồ sơ SV. Nút **Xem pipeline**: modal hiện JSON pipeline vừa chạy (đúng tham số), thời gian thực thi (ms), số dòng, nút sao chép. Nút Xuất Excel để sẵn (GĐ7).
2. **G-04** `/GiangVien/BaoCao`: dùng lại view/partial của A-10; dữ liệu giới hạn SV thuộc các lớp học phần của GV (mọi học kỳ), bộ lọc theo lớp học phần.
3. **A-01 Tổng quan Admin** `/Admin`: 4 thẻ số; biểu đồ tròn/donut phân bố xếp loại toàn trường (`ThongKeXepLoai`); biểu đồ cột GPA TB theo lớp (`GpaTheoLop`); cụm Truy cập nhanh (icon + nhãn: Sinh viên, Ghi danh, Báo cáo, Nhập/Xuất dữ liệu, Lớp học).
4. **A-03**: hoàn thiện thống kê xếp loại của lớp (thẻ nhỏ) trên trang chi tiết lớp.
5. **S-01 Tổng quan SV** `/SinhVien`: lời chào; 4 thẻ (GPA tích lũy, TC tích lũy, xếp loại, số môn nợ); biểu đồ đường GPA theo học kỳ (`GpaHocKy`); bảng môn học kỳ hiện tại (điểm thành phần đã có, trạng thái "Chưa có điểm").
6. **S-02 Kết quả học tập** `/SinhVien/KetQua`: dùng partial `_BangDiem`: mỗi học kỳ một khối (accordion, mở học kỳ mới nhất), cột đúng thiết kế, cột "Tính tích lũy ✓", chân khối GPA học kỳ + TC đăng ký + TC đạt; cuối trang GPA tích lũy, TC tích lũy, xếp loại. Chú thích nhỏ giải thích quy tắc (lần học tốt nhất, F không tính tích lũy). Nút Xuất Excel để sẵn (GĐ7).
7. **S-03 Hồ sơ** `/SinhVien/HoSo`: thông tin chỉ đọc + link đổi mật khẩu.
8. MSSV của SV luôn lấy từ claim.

## Tự kiểm tra
- Build 0 lỗi; curl các trang 200 với đúng vai trò.
- Tổng số SV trên thẻ xếp loại A-10 (phạm vi tích lũy, tất cả lớp) = 100.
- Đăng nhập TS1 → S-02 có GPA HK2 3.60 Xuất sắc, tích lũy 3.27 Giỏi; TS3 → MAT101 lần 1 ✓, lần 2 không ✓.
- SV không xem được dữ liệu SV khác (thử thêm query `?maSV=` → bị bỏ qua).
- Modal Xem pipeline hiện JSON hợp lệ (parse được).

## Nghiệm thu (bạn làm) – tập luôn demo 2
- [ ] Admin → Báo cáo học lực → lọc lớp 16DHTH01 → bảng + biểu đồ đúng; bấm Xem pipeline → thấy `$unwind`, `$group`, `$project`, `$switch`, thời gian ms.
- [ ] Sao chép pipeline → dán vào Compass Aggregations → kết quả trùng với giao diện.
- [ ] Đổi phạm vi sang "HK1 2025-2026" → số liệu thay đổi hợp lý.
- [ ] Nhập 1 điểm làm SV đổi xếp loại (GV) → báo cáo cập nhật ngay.
- [ ] Đăng nhập SV `2001250002 / 22072007` → tổng quan + kết quả học tập hiển thị đẹp, đúng.
- [ ] Dashboard Admin có biểu đồ.
- [ ] Commit.
