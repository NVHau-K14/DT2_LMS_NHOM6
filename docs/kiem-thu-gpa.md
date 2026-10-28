# Kết quả kiểm thử GPA (5 sinh viên kiểm thử – thiết kế mục 7.5)

> File sinh tự động bởi `dotnet run --project DT2_LMS_NHOM6 -- --kiem-thu-gpa`. Không sửa tay.

- **Thời điểm chạy:** 28/10/2026 20:05:52
- **Kết quả:** 200/200 tiêu chí PASS ✅
- **Thời gian chạy:** 0.37 giây
- **Cách kiểm thử:** chạy các pipeline thật (`BangDiem`, `GpaHocKy`, `BaoCaoHocLuc`, `ThongKeXepLoai`, `GpaTheoLop`, `LopHocPhanCuaGV`, `DanhSachNhapDiem`) qua `DiemService`/`BaoCaoService` trên database `eduscore`, so với giá trị mong đợi viết cứng theo bảng tính tay ở `docs/thiet-ke.md` mục 7.5. Số thực so sánh bằng `==` tuyệt đối (không dung sai). Dòng ghi "suy ra từ 3.3" không có trong bảng 7.5 mà suy ra trực tiếp từ quy tắc: học kỳ chưa có lần học nào đủ điểm → GPA null, "Chưa xếp loại".
- **Công thức (mục 3):** `diemTK = floor(tong/10 + 0.5000001)/10`; `GPA = floor(x*100 + 0.5000001)/100`; không dùng `$round` (làm tròn kiểu banker's, 84.5 → 84).

## TS1 – 2001250001 – Làm tròn nửa lên

INT101: tong = 8×10 + 8.5×30 + 8.5×60 = 845 → 84.5 + 0.5000001 → floor 85 → **8.5 (A)**. Nếu dùng `$round` (làm tròn kiểu banker's) 84.5 → 84 → 8.4 (B) là sai. GPA HK2 = (4×3 + 3×2)/5 = 3.60 đúng **biên Xuất sắc**. INT201 HK1 2026-2027 chưa có CK → không tính.

| Tiêu chí | Mong đợi | Thực tế | Kết quả |
|---|---|---|---|
| Số lần học | 5 | 5 | ✅ PASS |
| INT101 HK1 2025-2026 – TK | 8.5 | 8.5 | ✅ PASS |
| INT101 HK1 2025-2026 – Chữ | A | A | ✅ PASS |
| INT101 HK1 2025-2026 – Hệ 4 | 4 | 4 | ✅ PASS |
| INT101 HK1 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| MAT101 HK1 2025-2026 – TK | 6.4 | 6.4 | ✅ PASS |
| MAT101 HK1 2025-2026 – Chữ | C | C | ✅ PASS |
| MAT101 HK1 2025-2026 – Hệ 4 | 2 | 2 | ✅ PASS |
| MAT101 HK1 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| INT102 HK2 2025-2026 – TK | 9.0 | 9.0 | ✅ PASS |
| INT102 HK2 2025-2026 – Chữ | A | A | ✅ PASS |
| INT102 HK2 2025-2026 – Hệ 4 | 4 | 4 | ✅ PASS |
| INT102 HK2 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| ENG102 HK2 2025-2026 – TK | 7.2 | 7.2 | ✅ PASS |
| ENG102 HK2 2025-2026 – Chữ | B | B | ✅ PASS |
| ENG102 HK2 2025-2026 – Hệ 4 | 3 | 3 | ✅ PASS |
| ENG102 HK2 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| INT201 HK1 2026-2027 – TK | null | null | ✅ PASS |
| INT201 HK1 2026-2027 – Chữ | null | null | ✅ PASS |
| INT201 HK1 2026-2027 – Hệ 4 | null | null | ✅ PASS |
| INT201 HK1 2026-2027 – Tính tích lũy | – | – | ✅ PASS |
| Số học kỳ có lần học | 3 | 3 | ✅ PASS |
| GPA HK1 2025-2026 | 3.00 | 3.00 | ✅ PASS |
| GPA HK1 2025-2026 – xếp loại | Khá | Khá | ✅ PASS |
| GPA HK1 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 3.00 | 3.00 | ✅ PASS |
| GPA HK2 2025-2026 | 3.60 | 3.60 | ✅ PASS |
| GPA HK2 2025-2026 – xếp loại | Xuất sắc | Xuất sắc | ✅ PASS |
| GPA HK2 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 3.60 | 3.60 | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) | null | null | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – xếp loại | Chưa xếp loại | Chưa xếp loại | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – qua BaoCaoHocLuc phạm vi học kỳ | null | null | ✅ PASS |
| GPA tích lũy | 3.27 | 3.27 | ✅ PASS |
| Xếp loại tích lũy | Giỏi | Giỏi | ✅ PASS |
| Tín chỉ tích lũy | 11 | 11 | ✅ PASS |
| Số môn nợ | 0 | 0 | ✅ PASS |

## TS2 – 2001250002 – Học lại

INT101 lần 1 được F (2.6) → học lại ở học kỳ hè, lần 2 được B (8.4). GPA HK1 tính cả F: (0×3 + 3×3)/6 = 1.50. GPA tích lũy chỉ lấy lần tốt nhất của INT101 (lần 2, B): 30/11 = 2.727… → 2.73. Lần 1 không ✓, lần 2 ✓.

| Tiêu chí | Mong đợi | Thực tế | Kết quả |
|---|---|---|---|
| Số lần học | 6 | 6 | ✅ PASS |
| INT101 HK1 2025-2026 – TK | 2.6 | 2.6 | ✅ PASS |
| INT101 HK1 2025-2026 – Chữ | F | F | ✅ PASS |
| INT101 HK1 2025-2026 – Hệ 4 | 0 | 0 | ✅ PASS |
| INT101 HK1 2025-2026 – Tính tích lũy | – | – | ✅ PASS |
| MAT101 HK1 2025-2026 – TK | 7.4 | 7.4 | ✅ PASS |
| MAT101 HK1 2025-2026 – Chữ | B | B | ✅ PASS |
| MAT101 HK1 2025-2026 – Hệ 4 | 3 | 3 | ✅ PASS |
| MAT101 HK1 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| INT102 HK2 2025-2026 – TK | 6.1 | 6.1 | ✅ PASS |
| INT102 HK2 2025-2026 – Chữ | C | C | ✅ PASS |
| INT102 HK2 2025-2026 – Hệ 4 | 2 | 2 | ✅ PASS |
| INT102 HK2 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| POL102 HK2 2025-2026 – TK | 8.1 | 8.1 | ✅ PASS |
| POL102 HK2 2025-2026 – Chữ | B | B | ✅ PASS |
| POL102 HK2 2025-2026 – Hệ 4 | 3 | 3 | ✅ PASS |
| POL102 HK2 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| INT101 HK hè 2025-2026 – TK | 8.4 | 8.4 | ✅ PASS |
| INT101 HK hè 2025-2026 – Chữ | B | B | ✅ PASS |
| INT101 HK hè 2025-2026 – Hệ 4 | 3 | 3 | ✅ PASS |
| INT101 HK hè 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| INT202 HK1 2026-2027 – TK | null | null | ✅ PASS |
| INT202 HK1 2026-2027 – Chữ | null | null | ✅ PASS |
| INT202 HK1 2026-2027 – Hệ 4 | null | null | ✅ PASS |
| INT202 HK1 2026-2027 – Tính tích lũy | – | – | ✅ PASS |
| Số học kỳ có lần học | 4 | 4 | ✅ PASS |
| GPA HK1 2025-2026 | 1.50 | 1.50 | ✅ PASS |
| GPA HK1 2025-2026 – xếp loại | Yếu | Yếu | ✅ PASS |
| GPA HK1 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 1.50 | 1.50 | ✅ PASS |
| GPA HK2 2025-2026 | 2.40 | 2.40 | ✅ PASS |
| GPA HK2 2025-2026 – xếp loại | Trung bình | Trung bình | ✅ PASS |
| GPA HK2 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 2.40 | 2.40 | ✅ PASS |
| GPA HK hè 2025-2026 | 3.00 | 3.00 | ✅ PASS |
| GPA HK hè 2025-2026 – xếp loại | Khá | Khá | ✅ PASS |
| GPA HK hè 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 3.00 | 3.00 | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) | null | null | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – xếp loại | Chưa xếp loại | Chưa xếp loại | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – qua BaoCaoHocLuc phạm vi học kỳ | null | null | ✅ PASS |
| GPA tích lũy | 2.73 | 2.73 | ✅ PASS |
| Xếp loại tích lũy | Khá | Khá | ✅ PASS |
| Tín chỉ tích lũy | 11 | 11 | ✅ PASS |
| Số môn nợ | 0 | 0 | ✅ PASS |

## TS3 – 2001250003 – Cải thiện bị điểm thấp hơn

MAT101 lần 1 được C (5.7), học cải thiện ở học kỳ hè chỉ được D (4.7). Lần tốt nhất là lần 1 (hệ 4 cao hơn) → GPA tích lũy giữ C: (2×3 + 3×2 + 3×3 + 4×3)/11 = 3.00. Lần 1 ✓, lần 2 không ✓. GPA học kỳ hè vẫn tính lần 2: 1×3/3 = 1.00. MAT102 7.0 là biên B.

| Tiêu chí | Mong đợi | Thực tế | Kết quả |
|---|---|---|---|
| Số lần học | 6 | 6 | ✅ PASS |
| MAT101 HK1 2025-2026 – TK | 5.7 | 5.7 | ✅ PASS |
| MAT101 HK1 2025-2026 – Chữ | C | C | ✅ PASS |
| MAT101 HK1 2025-2026 – Hệ 4 | 2 | 2 | ✅ PASS |
| MAT101 HK1 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| ENG101 HK1 2025-2026 – TK | 8.2 | 8.2 | ✅ PASS |
| ENG101 HK1 2025-2026 – Chữ | B | B | ✅ PASS |
| ENG101 HK1 2025-2026 – Hệ 4 | 3 | 3 | ✅ PASS |
| ENG101 HK1 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| MAT102 HK2 2025-2026 – TK | 7.0 | 7.0 | ✅ PASS |
| MAT102 HK2 2025-2026 – Chữ | B | B | ✅ PASS |
| MAT102 HK2 2025-2026 – Hệ 4 | 3 | 3 | ✅ PASS |
| MAT102 HK2 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| INT102 HK2 2025-2026 – TK | 8.6 | 8.6 | ✅ PASS |
| INT102 HK2 2025-2026 – Chữ | A | A | ✅ PASS |
| INT102 HK2 2025-2026 – Hệ 4 | 4 | 4 | ✅ PASS |
| INT102 HK2 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| MAT101 HK hè 2025-2026 – TK | 4.7 | 4.7 | ✅ PASS |
| MAT101 HK hè 2025-2026 – Chữ | D | D | ✅ PASS |
| MAT101 HK hè 2025-2026 – Hệ 4 | 1 | 1 | ✅ PASS |
| MAT101 HK hè 2025-2026 – Tính tích lũy | – | – | ✅ PASS |
| INT203 HK1 2026-2027 – TK | null | null | ✅ PASS |
| INT203 HK1 2026-2027 – Chữ | null | null | ✅ PASS |
| INT203 HK1 2026-2027 – Hệ 4 | null | null | ✅ PASS |
| INT203 HK1 2026-2027 – Tính tích lũy | – | – | ✅ PASS |
| Số học kỳ có lần học | 4 | 4 | ✅ PASS |
| GPA HK1 2025-2026 | 2.40 | 2.40 | ✅ PASS |
| GPA HK1 2025-2026 – xếp loại | Trung bình | Trung bình | ✅ PASS |
| GPA HK1 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 2.40 | 2.40 | ✅ PASS |
| GPA HK2 2025-2026 | 3.50 | 3.50 | ✅ PASS |
| GPA HK2 2025-2026 – xếp loại | Giỏi | Giỏi | ✅ PASS |
| GPA HK2 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 3.50 | 3.50 | ✅ PASS |
| GPA HK hè 2025-2026 | 1.00 | 1.00 | ✅ PASS |
| GPA HK hè 2025-2026 – xếp loại | Yếu | Yếu | ✅ PASS |
| GPA HK hè 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 1.00 | 1.00 | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) | null | null | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – xếp loại | Chưa xếp loại | Chưa xếp loại | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – qua BaoCaoHocLuc phạm vi học kỳ | null | null | ✅ PASS |
| GPA tích lũy | 3.00 | 3.00 | ✅ PASS |
| Xếp loại tích lũy | Khá | Khá | ✅ PASS |
| Tín chỉ tích lũy | 11 | 11 | ✅ PASS |
| Số môn nợ | 0 | 0 | ✅ PASS |

## TS4 – 2001250004 – Nợ môn

POL101 (3.1) và ENG102 (3.4) bị F, chưa học lại → **nợ 2 môn**, không tính vào GPA tích lũy và tín chỉ tích lũy. Tích lũy = (2×3 + 1×3)/6 = 1.50 Yếu, TC 6. GPA HK2 tính cả F: (1×3 + 0×2)/5 = 0.60 → **Kém** ("Kém" chỉ có thể xuất hiện ở GPA học kỳ).

| Tiêu chí | Mong đợi | Thực tế | Kết quả |
|---|---|---|---|
| Số lần học | 5 | 5 | ✅ PASS |
| POL101 HK1 2025-2026 – TK | 3.1 | 3.1 | ✅ PASS |
| POL101 HK1 2025-2026 – Chữ | F | F | ✅ PASS |
| POL101 HK1 2025-2026 – Hệ 4 | 0 | 0 | ✅ PASS |
| POL101 HK1 2025-2026 – Tính tích lũy | – | – | ✅ PASS |
| INT101 HK1 2025-2026 – TK | 5.8 | 5.8 | ✅ PASS |
| INT101 HK1 2025-2026 – Chữ | C | C | ✅ PASS |
| INT101 HK1 2025-2026 – Hệ 4 | 2 | 2 | ✅ PASS |
| INT101 HK1 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| INT102 HK2 2025-2026 – TK | 4.2 | 4.2 | ✅ PASS |
| INT102 HK2 2025-2026 – Chữ | D | D | ✅ PASS |
| INT102 HK2 2025-2026 – Hệ 4 | 1 | 1 | ✅ PASS |
| INT102 HK2 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| ENG102 HK2 2025-2026 – TK | 3.4 | 3.4 | ✅ PASS |
| ENG102 HK2 2025-2026 – Chữ | F | F | ✅ PASS |
| ENG102 HK2 2025-2026 – Hệ 4 | 0 | 0 | ✅ PASS |
| ENG102 HK2 2025-2026 – Tính tích lũy | – | – | ✅ PASS |
| INT201 HK1 2026-2027 – TK | null | null | ✅ PASS |
| INT201 HK1 2026-2027 – Chữ | null | null | ✅ PASS |
| INT201 HK1 2026-2027 – Hệ 4 | null | null | ✅ PASS |
| INT201 HK1 2026-2027 – Tính tích lũy | – | – | ✅ PASS |
| Số học kỳ có lần học | 3 | 3 | ✅ PASS |
| GPA HK1 2025-2026 | 1.00 | 1.00 | ✅ PASS |
| GPA HK1 2025-2026 – xếp loại | Yếu | Yếu | ✅ PASS |
| GPA HK1 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 1.00 | 1.00 | ✅ PASS |
| GPA HK2 2025-2026 | 0.60 | 0.60 | ✅ PASS |
| GPA HK2 2025-2026 – xếp loại | Kém | Kém | ✅ PASS |
| GPA HK2 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 0.60 | 0.60 | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) | null | null | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – xếp loại | Chưa xếp loại | Chưa xếp loại | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – qua BaoCaoHocLuc phạm vi học kỳ | null | null | ✅ PASS |
| GPA tích lũy | 1.50 | 1.50 | ✅ PASS |
| Xếp loại tích lũy | Yếu | Yếu | ✅ PASS |
| Tín chỉ tích lũy | 6 | 6 | ✅ PASS |
| Số môn nợ | 2 | 2 | ✅ PASS |

## TS5 – 2001250005 – Biên Xuất sắc

MAT101: tong 885 → 8.9 (A). GPA tích lũy = (4×3 + 4×3 + 3×2 + 3×2)/10 = 36/10 = 3.60 đúng **biên Xuất sắc** (so sánh trên GPA đã làm tròn, `$gte: 3.6`). INT301 HK1 2026-2027 chưa có CK; TS5 cũng có mặt trong lớp học phần INT301 của GV005.

| Tiêu chí | Mong đợi | Thực tế | Kết quả |
|---|---|---|---|
| Số lần học | 5 | 5 | ✅ PASS |
| INT101 HK1 2025-2026 – TK | 9.1 | 9.1 | ✅ PASS |
| INT101 HK1 2025-2026 – Chữ | A | A | ✅ PASS |
| INT101 HK1 2025-2026 – Hệ 4 | 4 | 4 | ✅ PASS |
| INT101 HK1 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| MAT101 HK1 2025-2026 – TK | 8.9 | 8.9 | ✅ PASS |
| MAT101 HK1 2025-2026 – Chữ | A | A | ✅ PASS |
| MAT101 HK1 2025-2026 – Hệ 4 | 4 | 4 | ✅ PASS |
| MAT101 HK1 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| ENG101 HK1 2025-2026 – TK | 7.6 | 7.6 | ✅ PASS |
| ENG101 HK1 2025-2026 – Chữ | B | B | ✅ PASS |
| ENG101 HK1 2025-2026 – Hệ 4 | 3 | 3 | ✅ PASS |
| ENG101 HK1 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| POL102 HK2 2025-2026 – TK | 7.8 | 7.8 | ✅ PASS |
| POL102 HK2 2025-2026 – Chữ | B | B | ✅ PASS |
| POL102 HK2 2025-2026 – Hệ 4 | 3 | 3 | ✅ PASS |
| POL102 HK2 2025-2026 – Tính tích lũy | ✓ | ✓ | ✅ PASS |
| INT301 HK1 2026-2027 – TK | null | null | ✅ PASS |
| INT301 HK1 2026-2027 – Chữ | null | null | ✅ PASS |
| INT301 HK1 2026-2027 – Hệ 4 | null | null | ✅ PASS |
| INT301 HK1 2026-2027 – Tính tích lũy | – | – | ✅ PASS |
| Số học kỳ có lần học | 3 | 3 | ✅ PASS |
| GPA HK1 2025-2026 | 3.75 | 3.75 | ✅ PASS |
| GPA HK1 2025-2026 – xếp loại | Xuất sắc | Xuất sắc | ✅ PASS |
| GPA HK1 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 3.75 | 3.75 | ✅ PASS |
| GPA HK2 2025-2026 | 3.00 | 3.00 | ✅ PASS |
| GPA HK2 2025-2026 – xếp loại | Khá | Khá | ✅ PASS |
| GPA HK2 2025-2026 – qua BaoCaoHocLuc phạm vi học kỳ | 3.00 | 3.00 | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) | null | null | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – xếp loại | Chưa xếp loại | Chưa xếp loại | ✅ PASS |
| GPA HK1 2026-2027 (suy ra từ 3.3) – qua BaoCaoHocLuc phạm vi học kỳ | null | null | ✅ PASS |
| GPA tích lũy | 3.60 | 3.60 | ✅ PASS |
| Xếp loại tích lũy | Xuất sắc | Xuất sắc | ✅ PASS |
| Tín chỉ tích lũy | 10 | 10 | ✅ PASS |
| Số môn nợ | 0 | 0 | ✅ PASS |

## Tổng thể toàn trường

Mọi SV đều có dòng trong báo cáo (kể cả chưa có điểm → "Chưa xếp loại"); các pipeline thống kê cộng lại khớp sĩ số.

| Tiêu chí | Mong đợi | Thực tế | Kết quả |
|---|---|---|---|
| BaoCaoHocLuc toàn trường – số dòng | 100 | 100 | ✅ PASS |
| ThongKeXepLoai – tổng số SV | 100 | 100 | ✅ PASS |
| GpaTheoLop – số lớp | 5 | 5 | ✅ PASS |
| GpaTheoLop – tổng sĩ số | 100 | 100 | ✅ PASS |
| BaoCaoHocLuc HK1 2026-2027 (chưa có CK) – số dòng | 100 | 100 | ✅ PASS |
| BaoCaoHocLuc HK1 2026-2027 – số SV "Chưa xếp loại" | 100 | 100 | ✅ PASS |
| LopHocPhanCuaGV GV005 HK1 2026-2027 – có INT301 | có | có | ✅ PASS |
| LopHocPhanCuaGV INT301 – số SV đủ điểm (CK chưa nhập) | 0 | 0 | ✅ PASS |
| DanhSachNhapDiem INT301 – sĩ số khớp LopHocPhanCuaGV | 56 | 56 | ✅ PASS |
| DanhSachNhapDiem INT301 – có TS5 (2001250005) | có | có | ✅ PASS |
| DanhSachNhapDiem INT301 – TS5 điểm CC/GK/CK | 10/9/null | 10/9/null | ✅ PASS |
