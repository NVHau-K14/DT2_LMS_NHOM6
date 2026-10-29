# Pipeline Aggregation – bản JSON để chạy trực tiếp

Các file `.json` ở đây **được sinh tự động** từ chính code ứng dụng (`DT2_LMS_NHOM6/Pipelines/*.cs`), đã thay sẵn tham số mẫu. Mọi file chạy trên collection **`sinhvien`**.

Sinh lại sau khi sửa pipeline:
```bash
dotnet run --project DT2_LMS_NHOM6 -- --export-pipelines
```

## Danh sách

| File | Kết quả | Tham số mẫu |
|---|---|---|
| `BangDiem.json` | Mọi lần học của 1 SV đã quy đổi (TK, chữ, hệ 4, đạt) + `tinhTichLuy` | maSV `2001250001` |
| `GpaHocKy.json` | GPA, xếp loại, TC đăng ký, TC đạt từng học kỳ | maSV `2001250001` |
| `BaoCaoHocLuc.json` | Mỗi SV: GPA tích lũy, xếp loại, TC tích lũy, số môn nợ | toàn trường, phạm vi tích lũy (100 dòng) |
| `BaoCaoHocLuc_HocKy.json` | Như trên nhưng phạm vi một học kỳ (GPA học kỳ, tính cả F) | lớp 16DHTH01 (`maLop: "CNTT2501"`), HK2 2025-2026 |
| `ThongKeXepLoai.json` | Số SV theo xếp loại (kèm theo lớp) | toàn trường, tích lũy |
| `GpaTheoLop.json` | Mỗi lớp: sĩ số, GPA tích lũy trung bình | – |
| `LopHocPhanCuaGV.json` | Lớp học phần của GV: sĩ số, số SV đủ điểm, các lớp | GV `GV005`, HK1 2026-2027 |
| `DanhSachNhapDiem.json` | SV của một lớp học phần kèm điểm đã quy đổi | GV `GV005`, môn `INT301`, HK1 2026-2027 |
| `SiSoTheoLop.json` | Sĩ số mỗi lớp sinh hoạt (A-02) | – |
| `SoLanHocTheoMon.json` | Số lần học, số SV của mỗi môn (cảnh báo đổi tên, chặn xóa môn – A-08) | – |
| `HocKyCoDuLieu.json` | Các học kỳ đã có lần học, mới nhất trước (ô chọn phạm vi A-10) | – |
| `LopHocPhanToanTruong.json` | Mọi lớp học phần (maMon, namHoc, hocKy, maGV) kèm tên môn, tên GV, sĩ số (ô chọn xuất bảng điểm A-11) | – |

Bốn stage `$addFields` đầu sau `$unwind` (duDiem → diemTK → diemChu → diemHe4 + dat) là **bộ quy đổi dùng chung**, giống hệt nhau ở mọi file. Bản có chú thích từng stage: `DT2_LMS_NHOM6/Pipelines/QuyDoiDiemStages.cs`.

## Chạy bằng MongoDB Compass
1. Mở database `eduscore` → collection `sinhvien` → tab **Aggregations**.
2. Bấm nút chuyển sang chế độ văn bản (**`</>` Text**).
3. Xóa nội dung mặc định, dán toàn bộ nội dung file `.json` (cả dấu `[` `]`).
4. Bảng xem trước bên phải hiện kết quả. Lưu lại bằng **Save → Save pipeline as…** (ví dụ "Báo cáo học lực") để dùng khi demo.

Muốn đổi tham số, sửa trực tiếp trong stage `$match` đầu tiên (ví dụ `"maSV": "2001250003"`, hoặc `{}` → `{"maLop": "CNTT2502"}` – lọc theo **mã lớp**, lớp 16DHTH02).

Pipeline luôn lọc và trả về `maLop` (mã nội bộ: CNTT2501, CNTT2502, ATTT2501, HTTT2501, KHDL2501). Tên lớp hiển thị (16DHTH01…) nằm ở `lophoc.tenLop`, ứng dụng tra thêm bằng C# khi hiển thị. Việc tra tên này không phải tính điểm.

## Chạy bằng mongosh
Chạy từ thư mục gốc repo:
```bash
mongosh eduscore --quiet --eval 'db.sinhvien.aggregate(EJSON.parse(fs.readFileSync("database/pipelines/BangDiem.json","utf8"))).forEach(r => printjson(r))'
```
Hoặc trong phiên mongosh đang mở:
```js
const p = EJSON.parse(fs.readFileSync("database/pipelines/BaoCaoHocLuc.json", "utf8"));
db.sinhvien.aggregate(p).toArray()
```

## Lưu ý
- Không dùng `$round` (làm tròn kiểu banker's: 84.5 → 84). Làm tròn nửa lên bằng `floor(x + 0.5000001)`.
- Không lưu điểm TK / GPA / xếp loại vào database – mỗi lần xem là một lần pipeline tính lại từ điểm thành phần.
