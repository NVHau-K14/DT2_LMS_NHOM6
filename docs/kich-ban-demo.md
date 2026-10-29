# Kịch bản demo – EduScore (1 người trình bày, ~10 phút)

Ba phần theo barem "Demo sống":
1. **Nhập điểm trên web → Compass thấy điểm nằm đúng phần tử mảng** (~3 phút 30)
2. **Báo cáo học lực từ Aggregation Pipeline** (~3 phút)
3. **Import / Export bảng điểm, giải thích dữ liệu trong Compass** (~3 phút)

Mở đầu 30 giây, kết 30 giây. Câu in nghiêng là gợi ý lời nói.

---

## 0. Chuẩn bị (làm trước giờ demo 15 phút)

| Việc | Cách làm |
|---|---|
| Dữ liệu chuẩn | `dotnet run --project DT2_LMS_NHOM6 -- --reset` (xóa và seed lại, ~20 giây). Để nguyên cửa sổ console chạy web. |
| Trình duyệt | **Cửa sổ 1** (thường): đăng nhập `GV005` / `123456`, mở sẵn trang Nhập điểm INT301. **Cửa sổ 2** (ẩn danh – Ctrl+Shift+N): đăng nhập `admin` / `Admin@123`. Zoom 110–125 %. |
| Compass | Kết nối `mongodb://localhost:27017` → database `eduscore` → collection `sinhvien`. Lưu sẵn 4 truy vấn ở bảng dưới (**Save → Save query**: đặt tên Q1…Q4) và pipeline Q5 (tab Aggregations → **Save**). |
| File Excel | Mở sẵn thư mục `docs/demo/` trong Explorer: `diem_INT301_loi.xlsx`, `diem_INT301_hop_le.xlsx`, `sinhvien_hop_le.xlsx`. |
| Dự phòng | Mở sẵn một terminal mongosh: `mongosh eduscore`. |

**Truy vấn Compass chuẩn bị sẵn (collection `sinhvien`):**

| Tên | Filter | Project |
|---|---|---|
| Q1 – TS5 môn INT301 | `{ maSV: "2001250005" }` | `{ maSV: 1, hoTen: 1, monHoc: { $elemMatch: { maMon: "INT301" } } }` |
| Q2 – Lớp INT301 đã có CK | `{ monHoc: { $elemMatch: { maMon: "INT301", namHoc: "2026-2027", hocKy: 1, "diem.ck": { $ne: null } } } }` | `{ maSV: 1, hoTen: 1, "monHoc.$": 1 }` |
| Q3 – Do admin nhập | `{ "monHoc.capNhatBoi": "admin" }` | `{ maSV: 1, hoTen: 1 }` |
| Q4 – SV mới nhập từ Excel | `{ maSV: { $gte: "2001250101" } }` | – |
| Q5 – Pipeline | Tab **Aggregations** → `</>` Text → dán nội dung `database/pipelines/BaoCaoHocLuc.json` | – |

Q1–Q5 không lọc theo lớp. Muốn lọc lớp trong Compass thì dùng **mã lớp** (`{ maLop: "CNTT2501" }` = lớp 16DHTH01), không dùng tên lớp. Bảng quy đổi: CNTT2501 = 16DHTH01, CNTT2502 = 16DHTH02, ATTT2501 = 16DHATTT01, HTTT2501 = 16DHHTTT01, KHDL2501 = 16DHKHDL01.

---

## Mở đầu (0:00 – 0:30)

*"EduScore là cổng quản lý kết quả học tập. Mỗi sinh viên là một document trong collection `sinhvien`, các lần học nằm trong mảng nhúng `monHoc`. Mọi điểm tổng kết, GPA, xếp loại không lưu trong database mà do Aggregation Pipeline tính lại mỗi lần xem. Em sẽ demo ba phần: nhập điểm, báo cáo học lực và nhập/xuất bảng điểm."*

Trong Compass, mở nhanh một document bất kỳ, bấm mở mảng `monHoc` cho thấy cấu trúc lồng. Chỉ vào trường `maLop: "CNTT2501"`:

*"`maLop` là mã nội bộ, ví dụ CNTT2501. Tên lớp hiển thị 16DHTH01 nằm trong collection `lophoc` (trường `tenLop`). Trên web mọi nơi đều hiện 16DHTH01, chỉ trang Lớp học có thêm cột mã lớp."*

---

## Phần 1 – Nhập điểm → Compass (0:30 – 4:00)

| # | Thao tác | Lời nói gợi ý |
|---|---|---|
| 1 | **Compass**: chạy **Q1**. Document có `maLop: "CNTT2501"` (lớp 16DHTH01). Mở phần tử `monHoc` → chỉ ra `diem: { cc: 10, gk: 9, ck: null }`, `maDK`, `giangVien.maGV: "GV005"`. | *"Sinh viên Võ Ngọc Bảo Anh, môn NoSQL học kỳ này đã có chuyên cần và giữa kỳ, chưa có cuối kỳ. Mỗi lần học có `maDK` riêng để cập nhật đúng phần tử."* |
| 2 | **Cửa sổ 1 (GV005)**: trang Tổng quan → thẻ lớp học phần **INT301** → **Nhập điểm** (hoặc menu Lớp học phần). Chỉ thanh tiến độ 0/56. | *"Giảng viên chỉ thấy các lớp học phần mình dạy; lớp học phần gồm sinh viên của nhiều lớp sinh hoạt."* |
| 3 | Ô lọc lớp chọn **16DHTH01**. Dòng **2001250005**: gõ CK `11` → báo lỗi đỏ. Sửa thành `8,5` (dấu phẩy). Bấm **Lưu** cuối dòng. | *"Ô nhập kiểm tra 0–10, tối đa một chữ số thập phân, nhận cả dấu phẩy."* |
| 4 | Toast hiện → bấm **Lệnh MongoDB đã chạy** để mở lệnh. Chỉ vào `$set: { "monHoc.$[e].diem.ck": 8.5 ... }` và `arrayFilters: [{ "e.maDK": ..., "e.giangVien.maGV": "GV005" }]`. Cột TK/Chữ/Hệ 4 hiện **8.8 / A / 4**. | *"Lệnh dùng `$set` với toán tử `$[e]` và `arrayFilters`: chỉ phần tử có đúng `maDK` **và** do chính GV005 dạy mới bị sửa. Quyền của giảng viên nằm ngay trong câu lệnh – giảng viên khác gửi cùng `maDK` sẽ ra `modifiedCount = 0`. TK 8.8 là do pipeline tính lại sau khi lưu: (10×10 + 9×30 + 8.5×60)/100 = 8.8."* |
| 5 | **Compass**: bấm **Refresh** (hoặc chạy lại Q1) → `ck: 8.5`, `capNhatLuc` mới, `capNhatBoi: "GV005"`. | *"Điểm nằm đúng trong phần tử INT301 của mảng, các phần tử khác không đổi. Không có trường điểm tổng kết nào được lưu."* |
| 6 | (Nếu còn thời gian) Nhập CK cho 2–3 dòng khác rồi bấm **Lưu tất cả** → toast "bulkWrite … tổng cộng N lệnh". | *"Lưu tất cả gom nhiều `updateOne` vào một `bulkWrite`."* |

---

## Phần 2 – Báo cáo học lực từ pipeline (4:00 – 7:00)

| # | Thao tác | Lời nói gợi ý |
|---|---|---|
| 1 | **Cửa sổ 2 (admin)**: menu **Báo cáo học lực**. Chỉ 7 thẻ xếp loại, biểu đồ, bảng. Bấm tiêu đề cột **GPA** để sắp xếp. | *"Báo cáo tích lũy toàn trường 100 sinh viên: GPA hệ 4, tín chỉ tích lũy, số môn nợ, xếp loại."* |
| 2 | Lọc lớp **16DHTH01**. Chỉ dòng **2001250005**: GPA **3.69 Xuất sắc** (trước phần 1 là 3.60). | *"GPA của Bảo Anh vừa đổi từ 3.60 lên 3.69 vì môn NoSQL 3 tín chỉ vừa đủ điểm, được A: (36 + 4×3)/13 = 3.69. Không có gì lưu sẵn – mỗi lần mở trang pipeline tính lại."* |
| 3 | Chỉ dòng **2001250004** (TS4): 1.50 Yếu, nợ 2 môn. Đổi **Phạm vi** → **HK2 2025-2026**: TS4 **0.60 Kém**. Đổi lại **Tích lũy**. | *"Cùng một pipeline, hai phạm vi: tích lũy lấy lần học tốt nhất của mỗi môn và chỉ tính môn đạt; theo học kỳ thì tính cả môn F."* |
| 4 | Bấm **Xem pipeline**: chỉ số stage, thời gian chạy (ms). Kéo qua các stage: `$match` → `$filter` → `$unwind` → 4 stage quy đổi (`$cond`, `$switch`) → `$sort` + `$group` lấy lần tốt nhất → `$group` theo SV → GPA + `$switch` xếp loại → `$project`. Bấm **Sao chép**. | *"Điểm tổng kết làm tròn nửa lên bằng `floor(x + 0.5000001)`. Em không dùng `$round` vì `$round` làm tròn kiểu banker's: 84.5 thành 84, tức 8.4 điểm B thay vì 8.5 điểm A – sinh viên TS1 trong dữ liệu kiểm thử chính là trường hợp này."* |
| 5 | **Compass**: tab **Aggregations** → `</>` Text → dán (hoặc mở Q5) → bảng kết quả bên phải giống trên web. Mở một stage ở chế độ xem từng bước (**Stage** view) để thấy dữ liệu sau `$unwind`. | *"Pipeline chạy thẳng trong Compass cho cùng kết quả – toàn bộ tính toán nằm ở tầng cơ sở dữ liệu."* |

Câu hỏi hay gặp: *"Sao biết pipeline đúng?"* → mở `docs/kiem-thu-gpa.md`: 5 sinh viên tính tay (làm tròn biên, học lại, cải thiện thấp hơn, nợ môn, biên Xuất sắc), lệnh `--kiem-thu-gpa` so 200/200 tiêu chí PASS.

---

## Phần 3 – Import / Export + giải thích dữ liệu trong Compass (7:00 – 9:30)

| # | Thao tác | Lời nói gợi ý |
|---|---|---|
| 1 | **Cửa sổ 2 (admin)**: menu **Nhập/Xuất dữ liệu** → thẻ *Xuất bảng điểm lớp học phần* chọn **INT301 – … · ThS. Trần Văn Bình** → **Xuất Excel**. Mở file vừa tải: phần đầu (mã môn, học kỳ, giảng viên), bảng STT/MSSV/Họ tên/Lớp/CC/GK/CK. | *"File xuất cũng chính là file mẫu để nhập điểm: thông tin lớp học phần ở phần đầu, ô MSSV định dạng Text."* |
| 2 | Thẻ *Nhập bảng điểm* → **Chọn file** `docs/demo/diem_INT301_loi.xlsx` → **Kiểm tra file**. Các dòng lỗi đỏ: CK = 11, chữ "abc", GK 8.25, MSSV không thuộc lớp học phần. | *"Hệ thống kiểm tra toàn bộ file trước; chỉ cần một dòng lỗi là không ghi gì."* |
| 3 | **Tải file khác** → chọn `diem_INT301_hop_le.xlsx` → **Kiểm tra file**. Chỉ cột điểm cũ/mới (dòng vàng = có thay đổi); dòng 2001250005 CK **8.5 → 4** (vì file mẫu được tạo trước phần 1). Bấm **Xác nhận ghi điểm (… sinh viên)**. | *"Ô trống trong file nghĩa là giữ nguyên điểm cũ; ô có giá trị khác thì ghi đè."* |
| 4 | Toast: mở **Lệnh MongoDB đã chạy** → `bulkWrite` các `updateOne` `$set` + `arrayFilters`, `capNhatBoi: "admin"`, "tổng cộng N lệnh". | *"Admin nhập thì arrayFilters chỉ lọc theo `maDK`, không kèm điều kiện giảng viên."* |
| 5 | **Compass**: chạy **Q2** → 56 document (đủ cả lớp học phần); **Q3** → các SV do admin nhập. Mở 1 document: `capNhatBoi: "admin"`; trường `maLop` là mã mới (`CNTT2501`, `CNTT2502` hoặc `KHDL2501`), trên web vẫn hiện 16DHTH01… | *"Toàn bộ 56 sinh viên của lớp học phần đã có điểm cuối kỳ, ghi vào đúng phần tử INT301 trong mảng của từng người."* |
| 6 | Quay lại **Nhập/Xuất dữ liệu** → *Nhập danh sách sinh viên* → `sinhvien_hop_le.xlsx` → **Kiểm tra file** → **Xác nhận nhập 5 sinh viên**. Toast: `insertMany`. **Compass**: **Q4** → 5 document mới với `monHoc: []` và `maLop: "KHDL2501"` (file ghi lớp 16DHKHDL01, khi nhập được đổi sang mã lớp); collection `taikhoan` có 5 tài khoản mới (mật khẩu = ngày sinh). | *"File Excel ghi tên lớp như người dùng thấy, hệ thống tra collection `lophoc` để đổi sang mã lớp trước khi ghi. Thêm sinh viên ghi vào hai collection; MongoDB standalone không có transaction nên em ghi tuần tự, bước tài khoản lỗi thì xóa lại các sinh viên vừa thêm."* |
| 7 | **Compass**: collection `sinhvien` → tab **Validation** (JSON Schema) và tab **Indexes** (multikey `ix_monHoc_*`). Thẻ *Xuất báo cáo học lực* → **Xuất Excel** (nếu còn thời gian). | *"Schema linh hoạt nhưng có validator `$jsonSchema` strict: tín chỉ phải là số nguyên, điểm 0–10 hoặc null. Index multikey trên mảng `monHoc` phục vụ tìm lớp học phần của giảng viên."* |

---

## Kết (9:30 – 10:00)

*"Tóm lại: hồ sơ sinh viên là một document nhúng mảng môn học nên đọc một lần là đủ, ghi trên mảng bằng `$push`, `$pull`, `$set` + `arrayFilters` đều nguyên tử trên một document; còn GPA và xếp loại tính hoàn toàn bằng Aggregation Pipeline. Em xin hết phần demo."*

Sau buổi demo: `dotnet run --project DT2_LMS_NHOM6 -- --reset` để trả dữ liệu về ban đầu.

---

## Phương án dự phòng

| Sự cố | Xử lý |
|---|---|
| Ứng dụng không chạy, console báo "Không kết nối được MongoDB" | MongoDB service chưa chạy: `net start MongoDB` (PowerShell quyền Admin) hoặc services.msc → MongoDB Server → Start. Chạy lại `dotnet run`. |
| Cổng 5207 đang bị chiếm (app cũ chưa tắt) | `Stop-Process -Name DT2_LMS_NHOM6` rồi chạy lại. |
| Dữ liệu đã bị sửa lung tung, số liệu không khớp kịch bản | `dotnet run --project DT2_LMS_NHOM6 -- --reset` (~20 giây). |
| Không đăng nhập được (sai mật khẩu, tài khoản bị khóa khi thử trước đó) | Admin → Giảng viên / Sinh viên → ⋮ → Reset mật khẩu / Mở khóa; hoặc `--reset`. |
| Toast đóng trước khi kịp mở lệnh | Toast có lệnh giữ 15 giây; làm lại thao tác (lưu lại cùng điểm vẫn chạy lệnh). Hoặc chiếu lệnh mẫu trong `docs/thiet-ke-csdl.md` mục 5. |
| Compass treo / không dán được pipeline | Dùng mongosh: `db.sinhvien.aggregate(EJSON.parse(fs.readFileSync("database/pipelines/BaoCaoHocLuc.json","utf8")))` (chạy mongosh từ thư mục gốc repo). Xem điểm: `db.sinhvien.findOne({maSV:"2001250005"},{monHoc:{$elemMatch:{maMon:"INT301"}}})`. |
| File Excel mẫu bị lỗi / đã dùng | Tạo lại: dừng app, `dotnet run --project DT2_LMS_NHOM6 -- --reset --tao-file-demo`, rồi chạy app. Hoặc dùng file vừa xuất ở bước 3.1 (sửa vài ô CK). |
| File hợp lệ báo "không có thay đổi" | Đã nhập rồi → bỏ qua bước, chiếu kết quả trong Compass (Q2, Q3). |
| Máy chiếu chậm, thiếu thời gian | Bỏ bước 1.6, 2.5, 3.6, 3.7; giữ 1.1–1.5, 2.1–2.4, 3.2–3.5. |
| Mất mạng | Không ảnh hưởng: Bootstrap, icon, Chart.js để cục bộ trong `wwwroot/lib`. |
