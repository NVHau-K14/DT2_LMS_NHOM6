# Giai đoạn 3 – Pipeline điểm & GPA + kiểm thử 5 ca

Giai đoạn quan trọng nhất (barem 2.5đ nâng cao + kiểm thử độ chính xác). Nếu Sonnet làm sai công thức, đổi `/model` sang Opus và chạy lại.

## Prompt
```
Đọc CLAUDE.md, docs/thiet-ke.md (mục 3 và 7.5 thật kỹ) và docs/quy-trinh/GD3.md. Thực hiện Giai đoạn 3 đúng theo GD3.md. Đây là phần lõi tính điểm: bám sát tuyệt đối công thức trong thiết kế, cấm $round, cấm tính điểm bằng C#. Lập todo trước khi làm. Khi xong, tự kiểm tra (lệnh --kiem-thu-gpa phải PASS toàn bộ), cập nhật tiến độ trong CLAUDE.md, báo cáo kèm hướng dẫn nghiệm thu, rồi chờ tôi xác nhận mới commit.
```

## Việc cần làm
1. **Pipelines/QuyDoiDiemStages**: bộ 4 stage quy đổi đúng mục 3.5 (chuỗi JSON), một nguồn duy nhất, các pipeline khác ghép vào. Comment tiếng Việt giải thích từng stage.
2. **Các pipeline** đúng bảng mục 3.6: `BangDiem`, `GpaHocKy`, `BaoCaoHocLuc` (phạm vi tích lũy hoặc một học kỳ), `ThongKeXepLoai`, `GpaTheoLop`, `LopHocPhanCuaGV`, `DanhSachNhapDiem`.
   - Viết dạng chuỗi JSON có placeholder tham số, hàm builder trả về `BsonDocument[]`; hàm `ToJson(pipeline)` trả chuỗi đẹp (indent) để hiển thị "Xem pipeline".
   - GPA và xếp loại: làm tròn 2 chữ số bằng floor (mục 3.3), xếp loại theo GPA đã làm tròn, có nhánh "Chưa xếp loại".
   - `BaoCaoHocLuc`: SV không có môn nào đủ điểm vẫn có dòng ("Chưa xếp loại", GPA null, TC 0).
   - Bắt buộc có đủ `$unwind`, `$group`, `$project`, `$cond`, `$switch` (barem) – ghi rõ trong comment stage nào đáp ứng.
3. **Models/Results**: class nhận kết quả (`KetQuaMon`, `GpaHocKyResult`, `BaoCaoHocLucRow`, `ThongKeXepLoaiRow`, `GpaTheoLopRow`, `LopHocPhanRow`, `DongNhapDiem`…).
4. **Services**: `DiemService` / `BaoCaoService` gọi pipeline, trả kết quả + (khi cần) chuỗi pipeline JSON + thời gian thực thi (ms, Stopwatch). Interface rõ ràng để GĐ 4–7 dùng lại. Chưa cần giao diện.
5. **Lệnh `--kiem-thu-gpa`** (`dotnet run --project DT2_LMS_NHOM6 -- --kiem-thu-gpa`): chạy pipeline thật trên DB cho TS1–TS5, so với **giá trị mong đợi viết cứng theo bảng 7.5** (TK, chữ, hệ 4, tinhTichLuy từng lần học; GPA từng học kỳ + xếp loại; GPA tích lũy, xếp loại, TC tích lũy, số môn nợ). In bảng PASS/FAIL từng tiêu chí, tổng kết, rồi thoát (không chạy web). Ghi kết quả ra `docs/kiem-thu-gpa.md` (bảng: tiêu chí | mong đợi | thực tế | kết quả) kèm giải thích ngắn từng ca (làm tròn nửa lên 8.45→8.5, học lại, cải thiện thấp hơn, nợ môn, biên 3.60).
6. **Lệnh `--export-pipelines`**: ghi mọi pipeline ra `database/pipelines/<Ten>.json` (tham số mẫu: maSV `2001250001`, maGV `GV005`, maMon `INT301`, `2026-2027` HK1, lớp `16DHTH01`) dạng JSON hợp lệ dán được vào tab Aggregations của Compass; thêm `database/pipelines/README.md` hướng dẫn dùng.

## Tự kiểm tra
- Build 0 lỗi.
- `-- --kiem-thu-gpa` → **100% PASS**. Có FAIL thì sửa pipeline (không sửa giá trị mong đợi – bảng 7.5 đã tính tay và đúng).
- `-- --export-pipelines` → file sinh ra; chạy thử `database/pipelines/BangDiem.json` bằng mongosh (`db.sinhvien.aggregate(<nội dung file>)`) cho ra kết quả giống lệnh kiểm thử.
- `BaoCaoHocLuc` toàn trường trả đúng 100 dòng; `ThongKeXepLoai` tổng = 100.
- grep `\$round` trong Pipelines → không có.

## Nghiệm thu (bạn làm)
- [ ] Chạy `dotnet run --project DT2_LMS_NHOM6 -- --kiem-thu-gpa` → toàn PASS.
- [ ] Mở `docs/kiem-thu-gpa.md` đọc được, đối chiếu vài số với bảng tính tay.
- [ ] Compass → `sinhvien` → Aggregations → dán `database/pipelines/BaoCaoHocLuc.json` → ra danh sách GPA + xếp loại. Lưu pipeline này trong Compass (dùng khi demo).
- [ ] Đọc lướt file pipeline quy đổi: comment đủ để bạn giải thích từng stage.
- [ ] Commit.
