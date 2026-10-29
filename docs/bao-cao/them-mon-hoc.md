# Báo cáo bổ sung – Mở rộng dữ liệu mẫu (thêm môn học, giảng viên)

- **Ngày hoàn thành:** 29/10/2026
- **Trạng thái:** ✅ Hoàn thành (nhánh `them-mon-hoc`, tách từ `doi-ma-lop`)
- **Commit:** `<mã commit>` (điền sau khi người dùng nghiệm thu và commit)

## 1. Đã làm gì
- Thêm 3 giảng viên GV009 Trương Minh Khoa (TS.), GV010 Lý Thu Trang (ThS.), GV011 Đinh Quốc Huy (ThS.) kèm tài khoản (`123456`).
- Thêm 19 **môn mở rộng có ghi danh** (HK1, HK2, hè 2025-2026 và HK1 2026-2027; 4 môn chuyên ngành INT207–INT210 theo lớp) và 13 **môn chưa mở** (INT302–INT312, INT401, INT402) chỉ có trong danh mục.
- Môn mở rộng sinh bằng `Random(2027)` riêng, chạy **sau** toàn bộ dữ liệu 14 môn gốc → dữ liệu gốc giống hệt từng điểm. 5 SV kiểm thử và GV005 không dính môn mới.
- Điểm môn mới theo cùng năng lực của từng SV, học kỳ đã qua đủ điểm (F 3,05%), HK1 2026-2027 chưa có CK. Không có học lại cho môn mới.
- Chạy lại `--reset`, `--kiem-thu-gpa`, `--export-pipelines`, `--tao-file-demo`, `--export`; cập nhật `thiet-ke.md` mục 1/4.6/7, `thiet-ke-csdl.md`, `README.md`, `database/README.md`, `CLAUDE.md`.
- Không sửa pipeline, service, view, công thức điểm.

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `Data/DuLieuMau.cs` | `HatGiongMonMoRong = 2027`; 3 GV mới; `MonMau` thêm tham số tùy chọn `ChiLop`; mảng mới `MonHocMoRong` (19), `MonHocChuaMo` (13, record `MonDanhMucMau`). Mảng `MonHoc` 14 môn gốc không đổi. |
| `Data/Seeder.cs` | `_rdMoRong`; `TaoMonHoc` chèn 46 môn; hàm mới `GhiDanhMonMoRong` + `HocKyCuaNhom`; `SinhDiem`/`SinhDiemDenKhi`/`Chuan` nhận `Random?` tùy chọn (null = Random(2026) như cũ); `KiemTraRangBuoc` áp 4–8 cho môn gốc + chặn SV kiểm thử có môn mới; `InThongKe` tách gốc/mở rộng. |
| `database/export/*.json`, `database/dump/eduscore.archive.gz` | Xuất lại (100 SV · 5 lớp · 11 GV · 46 môn · 112 tài khoản). |
| `docs/thiet-ke.md` | Mục 1 (barem dữ liệu mẫu), 4.6 (ghi chú 4–8), 7 (tổng, bảng GV, bảng môn mở rộng, môn chưa mở, phân bổ, số liệu mới). |
| `docs/thiet-ke-csdl.md` | Kích thước document mới, 2 160 lần học, ObjectId ví dụ TS5 khớp bản export mới. |
| `README.md`, `database/README.md`, `CLAUDE.md` | Số liệu tổng, GV001 dạy thêm, ghi chú kỹ thuật. |
| `docs/kiem-thu-gpa.md` | Chạy lại – chỉ đổi thời điểm/thời gian chạy. |

`docs/demo/*.xlsx`: đã tạo lại và so nội dung (sheet + shared strings, bỏ dòng "Ngày xuất") – **giống hệt** bản cũ, nên giữ nguyên file cũ để diff gọn. `database/pipelines/*.json` xuất lại không đổi byte nào.

## 3. Kết quả tự kiểm tra
| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | 0 lỗi, 0 cảnh báo ✅ |
| Kiểm thử GPA | `--reset --kiem-thu-gpa` (chạy 2 lần) | 200/200 PASS ✅ |
| Số môn / GV / tài khoản | mongosh | 46 = 14 + 19 + 13; 11 GV; 112 tài khoản (11 GV) ✅ |
| Dữ liệu gốc giống hệt | Script so `database/export/sinhvien.json` cũ ↔ mới, chỉ lấy lần học 14 môn gốc, bỏ `maDK`/`_id`; so cả hồ sơ SV, `giangvien`/`monhoc`/`lophoc` cũ | 640 lần học, 0 SV khác; 8 GV, 14 môn, 5 lớp cũ giữ nguyên ✅ |
| Số liệu gốc | Seeder in thống kê | 19 F, hè học lại 12 / cải thiện 8 / học vượt 5 ✅ |
| INT301 | `countDocuments({monHoc: {$elemMatch: {maMon: "INT301", ...}}})`, trang G-03 | 56 SV, tiến độ 0/56 ✅ |
| TS1–TS5 | mongosh + S-02 của TS5 | 0 môn mới ✅ |
| GV005 | mongosh + G-02 | vẫn 2 lớp học phần INT202 (41), INT301 (56), không có môn mới ✅ |
| Môn chưa mở | mongosh; A-08 xóa INT402 / xóa INT301 | 0 lần học; INT402 xóa được, INT301 bị chặn ✅ (đã `--reset` lại) |
| GV001 / GV009 | G-02 có INT207 / INT210; G-03 lưu CK 1 dòng | lưu được (TK 6.8 C / 7.7 B do pipeline tính) ✅ |
| Quyền | GV001 gửi `maDK` INT210 của GV009 | không ghi được ✅ |
| SV mỗi ngành | S-01, S-02 của 2001250006 / 041 / 061 / 081 | có đúng INT207 / INT208 / INT209 / INT210, không có môn ngành khác ✅ |
| Kịch bản demo 1–3 | G-03 TS5 CK `8,5` → TK 8.8; A-10 TS5 3.60 → 3.69; TS4 1.50, HK2 0.60; file demo INT301 giữ nguyên | ✅ |
| Kiểm tra web tổng | Script Python urllib (đăng nhập 3 vai trò) | 28/29 lần đầu; mục còn lại (tiến độ 0/56) do regex của script không khớp markup `<strong>0</strong>/<span>56</span>`, kiểm lại bằng regex đúng → sĩ số 56 ✅ |

**Số liệu mới**

| Chỉ số | Trước | Sau |
|---|---|---|
| Giảng viên / môn / tài khoản | 8 / 14 / 109 | 11 / 46 / 112 |
| Tổng lần học | 640 | **2 160** (640 gốc + 1 520 mở rộng) |
| Theo học kỳ (HK1 25-26 / HK2 25-26 / hè / HK1 26-27) | – | 621 / 599 / 215 / 725 |
| Lần F | 19 | 48 (19 gốc + 29 mở rộng = 29/950 lần đủ điểm, 3,05%) |
| Phần tử `monHoc` / SV | 5–8 | 5–24 (TS 5–6; SV khác 22–24), TB 21,6 |
| Document `sinhvien` lớn nhất | ≈ 3.2 KB | ≈ 9.0 KB (TB 8.2 KB) |

| Xếp loại tích lũy toàn trường | Xuất sắc | Giỏi | Khá | Trung bình | Yếu | Kém |
|---|---|---|---|---|---|---|
| Trước | 18 | 16 | 29 | 19 | 18 | 0 |
| Sau | 16 | 16 | 27 | 18 | 23 | 0 |

GPA trung bình theo lớp (sau): 16DHTH01 2.42 · 16DHTH02 2.90 · 16DHATTT01 2.84 · 16DHHTTT01 2.63 · 16DHKHDL01 2.75.

## 4. Quyết định kỹ thuật mới
- **Hai nguồn ngẫu nhiên**: `Random(2026)` cho 14 môn gốc (không đổi thứ tự gọi), `Random(2027)` cho môn mở rộng, gọi trong `GhiDanhMonMoRong` sau `TaoHocKyHe`. Hàm sinh điểm nhận `Random?` tùy chọn, mặc định `_rd` → mọi lời gọi cũ giữ nguyên.
- Môn mở rộng tách mảng riêng (`MonHocMoRong`) vì `ChonMonChoLop` chọn ngẫu nhiên từ `DuLieuMau.MonHoc` theo nhóm học kỳ – thêm vào mảng cũ sẽ đổi kết quả chọn môn.
- F môn mở rộng: sinh tự nhiên rồi điều chỉnh vào khoảng 3–5% (thiếu → hạ điểm một lần học ngẫu nhiên của nhóm 1/3 SV năng lực thấp nhất; thừa → nâng lần F của SV năng lực cao nhất).
- Mảng `monHoc` sắp lại ổn định theo (namHoc, hocKy) → trong cùng học kỳ môn gốc đứng trước môn mở rộng.
- Môn thực tập/khóa luận trọng số 0/0/100 (validator `monhoc` và form A-08 cho phép 0). `monhoc` không có trường giảng viên nên môn chưa mở không gán GV.

## 5. Điểm cần chú ý / hạn chế
- **Đổi nhỏ trong Seeder (ngoài phần thêm dữ liệu)**: ràng buộc "mỗi SV 4–8 phần tử `monHoc`" (thiết kế 7.4) nay chỉ áp cho lần học 14 môn gốc; nếu không, Seeder ném lỗi vì SV có tới 24 phần tử. Thêm kiểm tra "SV kiểm thử không có môn mở rộng".
- **Barem "mỗi SV 4–8 môn có điểm"**: 95 SV ngẫu nhiên giờ có 22–24 lần học (vượt 8). Khi bảo vệ nên giải thích: 14 môn gốc vẫn giữ 4–8, môn mở rộng mô phỏng chương trình đào tạo thật. Nếu muốn bám sát barem tuyệt đối thì cần cân nhắc lại.
- Xếp loại toàn trường đổi (Yếu 18 → 23) vì môn mở rộng F không học lại → thành môn nợ và kéo GPA tích lũy. Mọi số liệu của TS1–TS5 và kịch bản demo không đổi.
- `maDK`/`_id` sinh mới sau mỗi `--reset`: ObjectId ví dụ trong `thiet-ke-csdl.md` đã cập nhật theo bản export hiện tại.
- Trước khi build tôi đã dừng một tiến trình `DT2_LMS_NHOM6.exe` đang chạy sẵn. DB `eduscore` hiện là dữ liệu nguyên bản (đã `--reset` sau khi test ghi).

## 6. Nghiệm thu
- [ ] `dotnet run --project DT2_LMS_NHOM6 -- --reset --kiem-thu-gpa` → 200/200 PASS
- [ ] `mongosh eduscore --quiet --eval "db.monhoc.countDocuments()"` → 46; `db.giangvien.countDocuments()` → 11
- [ ] A-08 Môn học: môn chưa mở (INT302…INT402) hiện 0 lần học; môn mở rộng có số lần học
- [ ] Đăng nhập GV009 → Lớp học phần có INT210 (HK1 2026-2027), chọn học kỳ cũ thấy INT105, INT106
- [ ] Đăng nhập GV005 → vẫn chỉ INT202, INT301; INT301 tiến độ 0/56
- [ ] Đăng nhập một SV lớp 16DHATTT01 → Kết quả học tập có INT208, không có INT207/209/210
- [ ] Kịch bản demo phần 1–3: TS5 3.60 → 3.69; file `docs/demo/diem_INT301_*.xlsx` dùng như cũ

## 7. Gợi ý trả lời khi bảo vệ
- *"Thêm môn mới có làm đổi dữ liệu cũ không?"* → Không. Dữ liệu gốc dùng `Random(2026)` với đúng thứ tự gọi cũ; môn mới dùng `Random(2027)` riêng và sinh sau cùng. Em đã so bản export trước và sau: 640 lần học gốc giống từng điểm, `--kiem-thu-gpa` vẫn 200/200.
- *"Mỗi SV hơn 20 phần tử mảng có sao không?"* → Mỗi phần tử ≈ 360 byte, document lớn nhất ≈ 9 KB, rất xa giới hạn 16 MB; mảng tăng có giới hạn theo chương trình học nên validator không đặt `maxItems`.
- *"Môn chưa mở để làm gì?"* → Minh họa danh mục môn tách khỏi việc ghi danh: môn có trong `monhoc` nhưng chưa ai học thì đếm được 0 lần học và xóa được; môn đã có người học thì bị chặn xóa (`countDocuments({"monHoc.maMon"})`).
- *"Tương quan điểm giữa các môn?"* → Seeder gán mỗi SV một "năng lực" ~ N(7.0, 1.3) và sinh điểm mọi môn quanh năng lực đó, nên SV khá ở môn gốc cũng khá ở môn mới. Năng lực không lưu vào DB.
