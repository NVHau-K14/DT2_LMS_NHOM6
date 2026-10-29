# EduScore – Tài liệu thiết kế hệ thống

> Nguồn sự thật duy nhất của dự án. Mọi quyết định nghiệp vụ, dữ liệu, giao diện đều theo tài liệu này.
> Không tự ý thêm chức năng ngoài tài liệu. Chỗ nào chưa rõ: chọn cách đơn giản nhất, ghi lại trong báo cáo cuối giai đoạn.

**Tên hiển thị:** Cổng Quản lý Học tập – EduScore
**Đề tài:** Đồ án NoSQL – Đề tài 02: Hệ thống quản lý và chấm điểm học tập trực tuyến
**Bản chất hệ thống:** cổng quản lý kết quả học tập bậc đại học (giống cổng sinh viên của trường), KHÔNG phải LMS dạy học (không bài giảng, bài tập, đề thi).

---

## 1. Barem đề tài cần đáp ứng

| Barem | Đáp ứng bằng |
|---|---|
| Báo cáo & thiết kế CSDL (2.5đ): ưu điểm Document Store; JSON Schema hồ sơ SV chứa mảng môn học, tín chỉ, điểm | Collection `sinhvien` nhúng mảng `monHoc`; `$jsonSchema` validator ở mục 4.6 |
| CRUD nền tảng (2.5đ): lớp học, hồ sơ SV, cập nhật điểm môn | Màn hình Admin + Giảng viên; API (tùy chọn) |
| Nâng cao (2.5đ): `$push`, `arrayFilters`, `$set` trên mảng lồng; Aggregation (`$unwind`, `$group`, `$project`, `$cond`/`$switch`) tính GPA có trọng số + xếp loại **hoàn toàn ở tầng CSDL** | Ghi danh (`$push`), hủy ghi danh (`$pull`), nhập điểm (`$set` + `arrayFilters`), lan truyền tên (`updateMany` + `arrayFilters`); pipeline mục 3 |
| Dữ liệu mẫu (1đ): ≥100 SV chia đều 4–6 lớp; mỗi SV 4–8 môn có điểm, tín chỉ, giảng viên | Seeder mục 7: 100 SV, 5 lớp × 20, mỗi SV 4–8 phần tử `monHoc` |
| Demo sống (1.5đ) | (1) Nhập điểm trên web → Compass thấy điểm trong đúng phần tử mảng; (2) Báo cáo học lực từ pipeline; (3) Import/Export bảng điểm |

---

## 2. Vai trò và nghiệp vụ

Ba vai trò: **Admin** (phòng đào tạo), **GiangVien**, **SinhVien**. Không có đăng ký tài khoản công khai.

### 2.1 Admin
| Mã | Nghiệp vụ |
|---|---|
| A1 | Tổng quan toàn trường: số lớp, SV, GV, môn; phân bố xếp loại; GPA trung bình theo lớp |
| A2 | CRUD lớp sinh hoạt. Không xóa lớp còn sinh viên |
| A3 | CRUD sinh viên. Thêm SV tự tạo tài khoản. Không sửa MSSV. Xóa SV xóa luôn tài khoản |
| A4 | CRUD giảng viên. Thêm GV tự tạo tài khoản. Đổi họ tên GV lan truyền sang mọi nơi lưu tên GV. Không xóa GV đang được phân công dạy hoặc đang làm CVHT |
| A5 | CRUD danh mục môn (mã, tên, tín chỉ, trọng số mặc định). Đổi tên môn lan truyền sang `sinhvien.monHoc[].tenMon`. Không đổi tín chỉ/trọng số của các lần học đã ghi danh (ảnh chụp). Không xóa môn đã có người học |
| A6 | Ghi danh môn: **cả lớp** (học kỳ chính) hoặc **từng sinh viên** (học lại, cải thiện, học kỳ hè, học vượt). Hủy ghi danh khi phần tử chưa có điểm nào |
| A7 | Nhập Excel danh sách SV, nhập Excel bảng điểm; xuất Excel bảng điểm, báo cáo |
| A8 | Báo cáo học lực toàn trường / theo lớp, phạm vi tích lũy hoặc theo học kỳ; xuất Excel; xem pipeline |
| A9 | Reset mật khẩu GV/SV về mặc định; khóa/mở tài khoản |

### 2.2 Giảng viên
| Mã | Nghiệp vụ |
|---|---|
| G1 | Tổng quan: lớp học phần của mình theo học kỳ, số SV, số SV chưa đủ điểm |
| G2 | Danh sách lớp học phần của mình. **Lớp học phần** = nhóm (maMon, namHoc, hocKy, maGV); có thể gồm SV nhiều lớp sinh hoạt |
| G3 | Nhập/sửa điểm CC, GK, CK cho SV trong lớp học phần của mình; xóa một điểm nhập nhầm (về `null`). Lưu từng dòng hoặc lưu tất cả |
| G4 | Nhập điểm từ Excel (file mẫu xuất từ chính màn hình nhập điểm), xuất bảng điểm lớp học phần |
| G5 | Báo cáo học lực (tích lũy) của sinh viên thuộc các lớp học phần của mình |
| G6 | Đổi mật khẩu |

**Giảng viên CHỈ sửa được object `diem` của phần tử `monHoc` có `giangVien.maGV` = chính mình.** Không thêm/xóa môn, không sửa tín chỉ, trọng số, học kỳ, giảng viên, thông tin SV. Điều kiện quyền nằm ngay trong `arrayFilters` của câu lệnh update (mục 4.8).

### 2.3 Sinh viên
| Mã | Nghiệp vụ |
|---|---|
| S1 | Tổng quan: GPA tích lũy, tín chỉ tích lũy, xếp loại, số môn nợ; biểu đồ GPA theo học kỳ; môn đang học kỳ này |
| S2 | Kết quả học tập theo học kỳ: mọi lần học, điểm thành phần, TK, chữ, hệ 4, kết quả, cột "Tính tích lũy"; GPA học kỳ; xuất Excel |
| S3 | Hồ sơ cá nhân (chỉ xem) |
| S4 | Đổi mật khẩu |

SV chỉ xem dữ liệu của chính mình; MSSV lấy từ claim đăng nhập, không bao giờ lấy từ URL/query.

### 2.4 Học kỳ và hình thức học
- `hocKy`: `1`, `2` (học kỳ chính), `3` (học kỳ hè / học kỳ phụ). `namHoc`: `"2025-2026"`.
- Hiển thị: "HK1 2025-2026", "HK2 2025-2026", "HK hè 2025-2026".
- Thứ tự thời gian: sắp theo `namHoc` rồi `hocKy` (3 sau 2).
- **Học kỳ hiện tại** cấu hình trong `appsettings.json` → `HocKyHienTai: { NamHoc: "2026-2027", HocKy: 1 }`.
- `hinhThuc` của mỗi lần học, xác định tự động khi ghi danh dựa trên các lần học trước cùng `maMon`:
  - Chưa từng học → `"Học mới"` (gồm cả học vượt ở học kỳ hè)
  - Đã từng học, chưa lần nào đạt → `"Học lại"`
  - Đã có lần đạt → `"Cải thiện"`
  - Lần học trước chưa có điểm đầy đủ → **chặn**, báo "Lần học trước chưa có kết quả"
- `lanHoc` = số lần học trước cùng `maMon` + 1.
- Chặn ghi danh trùng: cùng SV, cùng `maMon`, cùng `namHoc` + `hocKy`.
- Học lại / cải thiện có thể ở học kỳ chính hoặc học kỳ hè.

---

## 3. Quy tắc tính điểm (Thông tư 08/2021/TT-BGDĐT) – tính hoàn toàn trong Aggregation Pipeline

### 3.1 Điểm học phần
- Điểm thành phần CC (chuyên cần), GK (giữa kỳ), CK (cuối kỳ): thang 10, tối đa 1 chữ số thập phân, hoặc `null` (chưa nhập).
- Trọng số lưu dạng **số nguyên phần trăm** (`cc + gk + ck = 100`). Mặc định 10/30/60.
- `duDiem` = cả 3 thành phần khác `null`. Không đủ điểm → môn "Chưa có điểm", không tính vào bất kỳ GPA nào.
- `tong = cc*tsCC + gk*tsGK + ck*tsCK` (giá trị trong khoảng 0–1000)
- **Điểm tổng kết (TK)** làm tròn 1 chữ số, **làm tròn nửa lên**:
  `diemTK = floor(tong / 10 + 0.5000001) / 10`
  - **CẤM dùng `$round`**: MongoDB `$round` làm tròn kiểu banker's (half-to-even), 84.5 → 84 → 8.4 (sai, phải là 8.5).
  - Hằng số `0.0000001` chống sai số dấu phẩy động.

### 3.2 Quy đổi
| TK (hệ 10) | Chữ | Hệ 4 | Kết quả |
|---|---|---|---|
| 8.5 – 10.0 | A | 4 | Đạt |
| 7.0 – 8.4 | B | 3 | Đạt |
| 5.5 – 6.9 | C | 2 | Đạt |
| 4.0 – 5.4 | D | 1 | Đạt |
| < 4.0 | F | 0 | Không đạt |

Không có A+, B+, C+, D+.

### 3.3 Điểm trung bình (GPA hệ 4)
- Công thức: `GPA = Σ(he4 × soTinChi) / Σ soTinChi`, làm tròn 2 chữ số, làm tròn nửa lên: `floor(x*100 + 0.5000001)/100`. **Xếp loại theo GPA đã làm tròn.**
- **GPA học kỳ**: mọi lần học trong học kỳ đó có `duDiem`, **kể cả F** (F = 0).
- **GPA tích lũy**: với mỗi `maMon`, chọn **lần học tốt nhất** (he4 cao nhất, rồi TK cao nhất, rồi lần học sau cùng). Chỉ tính các môn mà lần tốt nhất **đạt** (A–D). Cải thiện bị điểm thấp hơn → giữ lần cũ.
- **Tín chỉ tích lũy** = tổng tín chỉ các môn đạt (theo lần tốt nhất).
- **Số môn nợ** = số `maMon` đã có ít nhất một lần đủ điểm nhưng chưa lần nào đạt.
- **Tính tích lũy (✓)** trên bảng điểm: đánh dấu đúng lần học được chọn làm "lần tốt nhất" và lần đó đạt.

### 3.4 Xếp loại học lực (theo GPA hệ 4 đã làm tròn)
| GPA | Xếp loại |
|---|---|
| 3.60 – 4.00 | Xuất sắc |
| 3.20 – 3.59 | Giỏi |
| 2.50 – 3.19 | Khá |
| 2.00 – 2.49 | Trung bình |
| 1.00 – 1.99 | Yếu |
| < 1.00 | Kém |
| không có tín chỉ nào được tính | Chưa xếp loại |

Áp dụng cho cả GPA tích lũy và GPA học kỳ.

### 3.5 Bộ stage quy đổi dùng chung (viết một lần, mọi pipeline dùng lại)
Đặt sau `{ $unwind: "$monHoc" }`. Viết đúng logic sau (có thể điều chỉnh cú pháp, KHÔNG đổi công thức):

```js
{ $addFields: { "monHoc.duDiem": { $and: [
    { $ne: [ { $ifNull: ["$monHoc.diem.cc", null] }, null ] },
    { $ne: [ { $ifNull: ["$monHoc.diem.gk", null] }, null ] },
    { $ne: [ { $ifNull: ["$monHoc.diem.ck", null] }, null ] } ] } } },
{ $addFields: { "monHoc.diemTK": { $cond: [ "$monHoc.duDiem",
    { $divide: [ { $floor: { $add: [
        { $divide: [ { $add: [
            { $multiply: ["$monHoc.diem.cc", "$monHoc.trongSo.cc"] },
            { $multiply: ["$monHoc.diem.gk", "$monHoc.trongSo.gk"] },
            { $multiply: ["$monHoc.diem.ck", "$monHoc.trongSo.ck"] } ] }, 10 ] },
        0.5000001 ] } }, 10 ] },
    null ] } } },
{ $addFields: { "monHoc.diemChu": { $switch: { branches: [
    { case: { $eq: ["$monHoc.diemTK", null] }, then: null },
    { case: { $gte: ["$monHoc.diemTK", 8.5] }, then: "A" },
    { case: { $gte: ["$monHoc.diemTK", 7.0] }, then: "B" },
    { case: { $gte: ["$monHoc.diemTK", 5.5] }, then: "C" },
    { case: { $gte: ["$monHoc.diemTK", 4.0] }, then: "D" } ], default: "F" } } } },
{ $addFields: {
    "monHoc.diemHe4": { $switch: { branches: [
        { case: { $eq: ["$monHoc.diemChu", "A"] }, then: 4 },
        { case: { $eq: ["$monHoc.diemChu", "B"] }, then: 3 },
        { case: { $eq: ["$monHoc.diemChu", "C"] }, then: 2 },
        { case: { $eq: ["$monHoc.diemChu", "D"] }, then: 1 },
        { case: { $eq: ["$monHoc.diemChu", "F"] }, then: 0 } ], default: null } },
    "monHoc.dat": { $cond: [ { $eq: ["$monHoc.diemTK", null] }, null, { $gte: ["$monHoc.diemTK", 4.0] } ] } } }
```

### 3.6 Danh sách pipeline
Mọi pipeline viết bằng **chuỗi JSON (C# raw string literal)** trong `Pipelines/`, có tham số thay vào bằng BsonDocument, và có bản `.json` tương ứng trong `database/pipelines/` để dán vào Compass. **Không dùng LINQ cho các pipeline này.**

| Tên | Đầu vào | Kết quả | Stage bắt buộc |
|---|---|---|---|
| `BangDiem` | maSV | Mọi lần học đã quy đổi + `tinhTichLuy` (bool), sắp theo namHoc, hocKy, maMon | `$match`, `$unwind`, quy đổi, `$setWindowFields` (partition maMon, sort diemHe4 ↓, diemTK ↓, namHoc ↓, hocKy ↓; `$documentNumber` = 1 và `dat` = true → tinhTichLuy), `$project` |
| `GpaHocKy` | maSV (hoặc bộ lọc SV) | Mỗi (maSV, namHoc, hocKy): gpa, xepLoai, tinChiDangKy (mọi lần học), tinChiDat | `$unwind`, quy đổi, `$group`, `$project`, `$switch` |
| `BaoCaoHocLuc` | bộ lọc (maLop / danh sách maSV / tất cả), phạm vi (`tichluy` hoặc `{namHoc,hocKy}`) | Mỗi SV: maSV, hoTen, maLop, gpa, xepLoai, tinChiTichLuy, soMonNo | `$match`, `$unwind` (preserveNullAndEmptyArrays: true), quy đổi, `$sort` + `$group` (lần tốt nhất theo maSV+maMon, `$first`), `$group` theo SV với `$cond`, `$project` + `$switch` |
| `ThongKeXepLoai` | như trên | Số SV theo từng xếp loại (và theo lớp) | `BaoCaoHocLuc` + `$group` |
| `GpaTheoLop` | – | Mỗi lớp: GPA tích lũy trung bình, sĩ số | `BaoCaoHocLuc` + `$group` theo maLop |
| `LopHocPhanCuaGV` | maGV, (namHoc, hocKy tùy chọn) | Mỗi (maMon, namHoc, hocKy): tenMon, soTinChi, siSo, soDuDiem, cacLop | `$match`, `$unwind`, `$match`, quy đổi, `$group` |
| `DanhSachNhapDiem` | maGV (null nếu Admin), maMon, namHoc, hocKy | Mỗi SV trong lớp học phần: maSV, hoTen, maLop, maDK, hinhThuc, lanHoc, diem, diemTK, diemChu, diemHe4, dat | `$match`, `$unwind`, `$match`, quy đổi, `$project` |

Sinh viên chưa có lần học nào đủ điểm vẫn phải xuất hiện trong báo cáo với "Chưa xếp loại".

---

## 4. Cơ sở dữ liệu MongoDB

- Database: `eduscore`. Connection string mặc định `mongodb://localhost:27017`. MongoDB Server 8.3 standalone (**không có transaction**).
- Tên trường tiếng Việt không dấu, camelCase. Ngày lưu kiểu `Date` (UTC). Giá trị enum là tiếng Việt có dấu.
- 5 collection: `sinhvien`, `lophoc`, `giangvien`, `monhoc`, `taikhoan`.
- **Không lưu** điểm TK, chữ, hệ 4, đạt, GPA, xếp loại, tín chỉ tích lũy, sĩ số: luôn tính bằng pipeline / đếm.

### 4.1 `sinhvien`
```jsonc
{
  "_id": ObjectId("..."),
  "maSV": "2001250006",                 // 10 chữ số, unique, không cho sửa
  "hoTen": "Nguyễn Thị Minh Anh",
  "ngaySinh": ISODate("2007-03-14T00:00:00Z"),
  "gioiTinh": "Nữ",                     // "Nam" | "Nữ"
  "email": "2001250006@sv.eduscore.vn",
  "soDienThoai": "0912345678",
  "diaChi": "Tân Phú, TP.HCM",
  "maLop": "CNTT2501",                  // mã nội bộ của lớp 16DHTH01 (xem 4.2)
  "trangThai": "Đang học",              // "Đang học" | "Bảo lưu" | "Thôi học"
  "monHoc": [
    {
      "maDK": ObjectId("..."),          // định danh lần học; arrayFilters lọc theo trường này
      "maMon": "INT101",
      "tenMon": "Nhập môn lập trình",
      "soTinChi": 3,                    // ảnh chụp lúc ghi danh
      "namHoc": "2025-2026",
      "hocKy": 1,                       // 1 | 2 | 3 (hè)
      "lanHoc": 1,
      "hinhThuc": "Học mới",            // "Học mới" | "Học lại" | "Cải thiện"
      "giangVien": { "maGV": "GV001", "hoTen": "ThS. Nguyễn Văn An" },
      "trongSo": { "cc": 10, "gk": 30, "ck": 60 },     // ảnh chụp lúc ghi danh
      "diem":    { "cc": 9.0, "gk": 7.5, "ck": 8.0 },  // từng thành phần có thể null
      "capNhatLuc": ISODate("2026-01-15T03:00:00Z"),   // null nếu chưa nhập
      "capNhatBoi": "GV001"                            // maGV hoặc "admin"; null nếu chưa nhập
    }
  ],
  "ngayTao": ISODate("..."),
  "ngayCapNhat": ISODate("...")
}
```
`giangVien.hoTen` lưu **kèm học vị** (ví dụ "ThS. Nguyễn Văn An") để hiển thị ngay.

### 4.2 `lophoc`
```jsonc
{ "_id": ObjectId("..."), "maLop": "CNTT2501", "tenLop": "16DHTH01",
  "nienKhoa": "2025-2029", "nganh": "Công nghệ thông tin", "khoa": "Khoa Công nghệ thông tin",
  "coVanHocTap": { "maGV": "GV001", "hoTen": "ThS. Nguyễn Văn An" }, "ngayTao": ISODate("...") }
```
**Quy ước: `maLop` là mã nội bộ, `tenLop` là tên hiển thị.**
- `maLop` (CNTT2501) là khóa liên kết: `sinhvien.maLop`, bộ lọc pipeline, URL, `value` của ô chọn lớp, lệnh MongoDB trong toast.
- `tenLop` (16DHTH01) là tên người dùng thấy ở mọi màn hình, file Excel (xuất và nhập) và lời nói khi demo. `tenLop` unique (`ux_tenLop`).
- Mã lớp chỉ hiện ở trang Lớp học (A-02, A-03). Pipeline vẫn trả `maLop`; ứng dụng tra `maLop → tenLop` bằng C# khi hiển thị. Việc này không phải tính điểm.
- Danh sách lớp, phân trang báo cáo theo lớp và các danh sách có nhóm theo lớp đều sắp theo `tenLop`.

### 4.3 `giangvien`
```jsonc
{ "_id": ObjectId("..."), "maGV": "GV001", "hoTen": "Nguyễn Văn An", "hocVi": "ThS.",   // "CN." | "ThS." | "TS." | "PGS.TS."
  "email": "annv@eduscore.vn", "soDienThoai": "0987654321", "khoa": "Khoa Công nghệ thông tin", "ngayTao": ISODate("...") }
```

### 4.4 `monhoc`
```jsonc
{ "_id": ObjectId("..."), "maMon": "INT101", "tenMon": "Nhập môn lập trình", "soTinChi": 3,
  "trongSoMacDinh": { "cc": 10, "gk": 30, "ck": 60 }, "khoa": "Khoa Công nghệ thông tin", "ngayTao": ISODate("...") }
```

### 4.5 `taikhoan`
```jsonc
{ "_id": ObjectId("..."), "tenDangNhap": "2001250006",   // SV: maSV; GV: maGV; Admin: "admin"
  "matKhauHash": "$2a$11$...",                            // BCrypt
  "vaiTro": "SinhVien",                                   // "Admin" | "GiangVien" | "SinhVien"
  "maThamChieu": "2001250006",                            // maSV / maGV / "admin"
  "hoTen": "Nguyễn Thị Minh Anh",                         // GV: kèm học vị
  "trangThai": "Hoạt động",                               // "Hoạt động" | "Khóa"
  "lanDangNhapCuoi": null, "ngayTao": ISODate("...") }
```
Mật khẩu mặc định (dùng khi tạo mới và khi reset):
- Admin: `admin` / `Admin@123`
- Giảng viên: mã GV / `123456`
- Sinh viên: MSSV / ngày sinh dạng `ddMMyyyy` (ví dụ `14032007`)

### 4.6 Validator `$jsonSchema` cho `sinhvien` (validationLevel strict, validationAction error)
```js
{ $jsonSchema: {
  bsonType: "object",
  required: ["maSV","hoTen","ngaySinh","gioiTinh","maLop","trangThai","monHoc"],
  properties: {
    maSV:      { bsonType: "string", pattern: "^[0-9]{10}$" },
    hoTen:     { bsonType: "string", minLength: 2, maxLength: 100 },
    ngaySinh:  { bsonType: "date" },
    gioiTinh:  { enum: ["Nam","Nữ"] },
    email:       { bsonType: ["string","null"] },
    soDienThoai: { bsonType: ["string","null"] },
    diaChi:      { bsonType: ["string","null"] },
    maLop:     { bsonType: "string" },
    trangThai: { enum: ["Đang học","Bảo lưu","Thôi học"] },
    monHoc: { bsonType: "array", items: {
      bsonType: "object",
      required: ["maDK","maMon","tenMon","soTinChi","namHoc","hocKy","lanHoc","hinhThuc","giangVien","trongSo","diem"],
      properties: {
        maDK:     { bsonType: "objectId" },
        maMon:    { bsonType: "string" },
        tenMon:   { bsonType: "string" },
        soTinChi: { bsonType: "int", minimum: 1, maximum: 10 },
        namHoc:   { bsonType: "string", pattern: "^[0-9]{4}-[0-9]{4}$" },
        hocKy:    { bsonType: "int", enum: [1,2,3] },
        lanHoc:   { bsonType: "int", minimum: 1 },
        hinhThuc: { enum: ["Học mới","Học lại","Cải thiện"] },
        giangVien: { bsonType: "object", required: ["maGV","hoTen"],
                     properties: { maGV: { bsonType: "string" }, hoTen: { bsonType: "string" } } },
        trongSo: { bsonType: "object", required: ["cc","gk","ck"], properties: {
                     cc: { bsonType: "int", minimum: 0, maximum: 100 },
                     gk: { bsonType: "int", minimum: 0, maximum: 100 },
                     ck: { bsonType: "int", minimum: 0, maximum: 100 } } },
        diem: { bsonType: "object", required: ["cc","gk","ck"], properties: {
                     cc: { bsonType: ["double","int","null"], minimum: 0, maximum: 10 },
                     gk: { bsonType: ["double","int","null"], minimum: 0, maximum: 10 },
                     ck: { bsonType: ["double","int","null"], minimum: 0, maximum: 10 } } },
        capNhatLuc: { bsonType: ["date","null"] },
        capNhatBoi: { bsonType: ["string","null"] }
      } } }
  } } }
```
Mảng `monHoc` **không giới hạn số phần tử** (giới hạn 4–8 chỉ áp dụng cho dữ liệu mẫu). Các collection khác có validator đơn giản (required + kiểu + enum). Khởi tạo: collection chưa có → `createCollection` kèm validator; đã có → `collMod` cập nhật validator.

### 4.7 Index
| Collection | Index |
|---|---|
| sinhvien | `{maSV:1}` unique; `{maLop:1}`; `{"monHoc.giangVien.maGV":1, "monHoc.namHoc":1, "monHoc.hocKy":1}`; `{"monHoc.maMon":1}`; `{hoTen:1}` |
| lophoc | `{maLop:1}` unique; `{tenLop:1}` unique |
| giangvien | `{maGV:1}` unique |
| monhoc | `{maMon:1}` unique |
| taikhoan | `{tenDangNhap:1}` unique; `{vaiTro:1, maThamChieu:1}` |

### 4.8 Các câu lệnh ghi trên mảng (trọng tâm demo)
- **Ghi danh** (A6): `updateOne({maSV}, {$push: {monHoc: <phần tử mới, diem toàn null>}, $set: {ngayCapNhat}})`. Ghi danh nhiều SV dùng `bulkWrite` gồm nhiều UpdateOne.
- **Hủy ghi danh**: `updateOne({maSV, monHoc: {$elemMatch: {maDK, "diem.cc": null, "diem.gk": null, "diem.ck": null}}}, {$pull: {monHoc: {maDK}}})`. MatchedCount = 0 → báo "Đã có điểm, không thể hủy".
- **Nhập điểm** (G3):
  ```js
  db.sinhvien.updateOne(
    { maSV: "2001250006" },
    { $set: { "monHoc.$[e].diem.cc": 9, "monHoc.$[e].diem.gk": 8, "monHoc.$[e].diem.ck": 8.5,
              "monHoc.$[e].capNhatLuc": ISODate(), "monHoc.$[e].capNhatBoi": "GV005", ngayCapNhat: ISODate() } },
    { arrayFilters: [ { "e.maDK": ObjectId("..."), "e.giangVien.maGV": "GV005" } ] })
  ```
  Admin nhập (qua import) thì bỏ điều kiện `e.giangVien.maGV`, `capNhatBoi: "admin"`. ModifiedCount = 0 → báo "Không có quyền hoặc không tìm thấy lần học". "Lưu tất cả" dùng `bulkWrite`.
- **Lan truyền tên môn**: `updateMany({"monHoc.maMon": ma}, {$set: {"monHoc.$[e].tenMon": ten}}, {arrayFilters: [{"e.maMon": ma}]})`.
- **Lan truyền tên GV**: tương tự cho `monHoc.$[e].giangVien.hoTen`, cộng `lophoc.coVanHocTap.hoTen` và `taikhoan.hoTen`.
- Không có transaction: thao tác nhiều collection (tạo SV + tài khoản, xóa SV + tài khoản) ghi tuần tự, bước sau lỗi thì hoàn tác bước trước.

### 4.9 Hiển thị lệnh MongoDB (phục vụ demo)
Mọi thao tác ghi trên mảng (ghi danh, hủy ghi danh, nhập điểm, lan truyền) trả về kèm **chuỗi lệnh dạng mongosh** đã thực thi (filter, update, arrayFilters dựng từ chính BsonDocument dùng để chạy) và kết quả (matched/modified). Toast hiển thị chuỗi này trong phần thu gọn "Lệnh MongoDB đã chạy" (có nút sao chép). Ghi nhiều dòng: hiện lệnh của dòng đầu + tổng số lệnh.

---

## 5. Màn hình

Khung chung: sidebar trái màu xanh dương (logo EduScore, menu theo vai trò, thu gọn được), header (tiêu đề + breadcrumb, nhãn học kỳ hiện tại, menu người dùng: họ tên, vai trò, Đổi mật khẩu, Đăng xuất). Thành phần dùng chung: bảng (tìm kiếm, lọc, sắp xếp, phân trang 20), modal thêm/sửa, hộp xác nhận xóa, toast (kèm lệnh MongoDB), badge điểm chữ (A xanh lá, B xanh dương, C vàng, D cam, F đỏ) và badge xếp loại, trạng thái trống.

### 5.1 Chung
| Mã | Route | Nội dung |
|---|---|---|
| C-01 | `/Account/Login` | Trái: giới thiệu hệ thống. Phải: form tên đăng nhập, mật khẩu. **Không hiện tài khoản demo.** Đăng nhập xong chuyển đến cổng theo vai trò. Tài khoản Khóa → báo lỗi |
| C-02 | `/Account/DoiMatKhau` | Mật khẩu cũ, mới (≥6 ký tự), xác nhận |
| C-03 | `/Account/AccessDenied`, `/Home/StatusCode/{code}` | 403, 404, lỗi chung |
| – | `/` | Chưa đăng nhập → Login; đã đăng nhập → cổng theo vai trò |

### 5.2 Admin (Area `Admin`) – Menu: Tổng quan · Lớp học · Sinh viên · Giảng viên · Môn học · Ghi danh · Báo cáo học lực · Nhập/Xuất dữ liệu · Hiệu năng
| Mã | Route | Nội dung |
|---|---|---|
| A-01 | `/Admin` | 4 thẻ số (lớp, SV, GV, môn); biểu đồ phân bố xếp loại toàn trường; biểu đồ GPA TB theo lớp; cụm Truy cập nhanh dạng icon |
| A-02 | `/Admin/LopHoc` | Bảng: mã, tên, niên khóa, ngành, CVHT, sĩ số. Lọc ngành, niên khóa; tìm. + Thêm lớp; ⋮ Xem/Sửa/Xóa. Thêm/sửa bằng modal (CVHT chọn từ danh sách GV) |
| A-03 | `/Admin/LopHoc/ChiTiet/{maLop}` | Thông tin lớp; DS SV kèm GPA tích lũy, xếp loại; nút Ghi danh cả lớp (sang A-09 điền sẵn lớp); Xuất báo cáo lớp |
| A-04 | `/Admin/SinhVien` | Bảng: MSSV, họ tên, lớp, ngày sinh, trạng thái, GPA, xếp loại. Lọc lớp, trạng thái; tìm MSSV/tên; phân trang phía server. Nút + Thêm, Nhập Excel, Tải mẫu, Xuất Excel; ⋮ Xem/Sửa/Reset mật khẩu/Xóa |
| A-05 | `/Admin/SinhVien/Them`, `/Admin/SinhVien/Sua/{maSV}` | Form đủ trường, validation; MSSV khóa khi sửa |
| A-06 | `/Admin/SinhVien/HoSo/{maSV}` | Thẻ thông tin; thẻ tóm tắt (GPA, TC tích lũy, xếp loại, số môn nợ); bảng điểm theo học kỳ (như S-02); nút Ghi danh môn (cho SV này); nút Hủy ghi danh ở dòng chưa có điểm; Xuất bảng điểm |
| A-07 | `/Admin/GiangVien` | Bảng: mã, họ tên, học vị, khoa, email, số lớp học phần đang dạy (học kỳ hiện tại). Modal thêm/sửa; ⋮ Reset mật khẩu/Khóa/Xóa |
| A-08 | `/Admin/MonHoc` | Bảng: mã, tên, tín chỉ, trọng số mặc định. Modal thêm/sửa (validation tổng trọng số = 100); đổi tên → cảnh báo "Cập nhật tên cho N lần học" |
| A-09 | `/Admin/GhiDanh` | B1: năm học, học kỳ, môn, giảng viên. B2: chọn cả lớp hoặc từng SV (tìm, chọn nhiều). B3: **xem trước** từng SV với lanHoc, hinhThuc tự nhận diện và dòng bị chặn kèm lý do. B4: xác nhận → bulkWrite `$push`, toast lệnh. Nhận diện hình thức dùng kết quả pipeline `BangDiem` (không tính lại điểm bằng C#) |
| A-10 | `/Admin/BaoCao` | Lọc: lớp, phạm vi (tích lũy / từng học kỳ). Thẻ số theo xếp loại + biểu đồ; bảng MSSV, tên, lớp, TC tích lũy, GPA, xếp loại, số môn nợ (sắp xếp được). Nút Xuất Excel, **Xem pipeline** (modal: JSON pipeline đang chạy + thời gian thực thi ms + nút sao chép) |
| A-11 | `/Admin/DuLieu` | Thẻ Nhập: danh sách SV, bảng điểm. Thẻ Xuất: bảng điểm lớp học phần (chọn môn/học kỳ), báo cáo học lực. Luồng nhập: chọn file → bảng kiểm tra (dòng hợp lệ xanh, lỗi đỏ + lý do) → Xác nhận ghi |
| A-12 | `/Admin/HieuNang` | (Tùy chọn) explain executionStats các truy vấn chính, có/không index (hint `$natural`), thời gian pipeline trung bình 20 lần |

### 5.3 Giảng viên (Area `GiangVien`) – Menu: Tổng quan · Lớp học phần · Báo cáo học lực
| Mã | Route | Nội dung |
|---|---|---|
| G-01 | `/GiangVien` | Lời chào; chọn học kỳ (mặc định hiện tại); thẻ số: số lớp học phần, số SV, số SV chưa đủ điểm; thẻ lớp học phần với thanh tiến độ nhập điểm |
| G-02 | `/GiangVien/LopHocPhan` | Bảng mọi lớp học phần của GV, lọc học kỳ; nút Nhập điểm |
| G-03 | `/GiangVien/NhapDiem?maMon=&namHoc=&hocKy=` | Đầu trang: môn, tín chỉ, trọng số, học kỳ, sĩ số, tiến độ. Bảng: MSSV, họ tên, lớp, hình thức, ô nhập CC/GK/CK, TK, Chữ, Hệ 4 (lấy lại từ pipeline sau khi lưu). Lọc lớp, "chỉ chưa đủ điểm". Lưu từng dòng (fetch, `$set`+arrayFilters) và Lưu tất cả (bulkWrite). Ô nhập nhận `8,5` và `8.5`, kiểm tra 0–10, tối đa 1 chữ số thập phân; ô trống = null. Nút Tải mẫu/Xuất Excel, Nhập Excel. Toast hiện lệnh MongoDB. Học kỳ không phải hiện tại vẫn sửa được |
| G-04 | `/GiangVien/BaoCao` | Như A-10, giới hạn SV thuộc lớp học phần của GV, lọc theo lớp học phần |

### 5.4 Sinh viên (Area `SinhVien`) – Menu: Tổng quan · Kết quả học tập · Hồ sơ cá nhân
| Mã | Route | Nội dung |
|---|---|---|
| S-01 | `/SinhVien` | Lời chào; 4 thẻ: GPA tích lũy, TC tích lũy, xếp loại, số môn nợ; biểu đồ đường GPA theo học kỳ; môn học kỳ hiện tại (điểm đã có) |
| S-02 | `/SinhVien/KetQua` | Mỗi học kỳ một khối (mở sẵn học kỳ mới nhất): mã môn, tên, TC, lần/hình thức, CC, GK, CK, TK, Chữ, Hệ 4, Kết quả, Tính tích lũy ✓. Cuối khối: GPA học kỳ, TC đăng ký, TC đạt. Cuối trang: GPA tích lũy, TC tích lũy, xếp loại. Xuất Excel |
| S-03 | `/SinhVien/HoSo` | Thông tin cá nhân chỉ đọc; liên kết đổi mật khẩu |

---

## 6. Nhập / Xuất Excel (ClosedXML)

- **Nguyên tắc nhập: kiểm tra toàn bộ file trước; có dòng lỗi → hiện danh sách lỗi theo dòng, KHÔNG ghi gì. Sạch → xác nhận → ghi bằng bulkWrite.** Lưu kết quả kiểm tra tạm (TempData/cache theo phiên) giữa bước xem trước và bước xác nhận.
- Ô MSSV, ngày sinh định dạng Text trong file mẫu. Ngày sinh `dd/MM/yyyy`. Tối đa 5MB, chỉ `.xlsx`.
- **Mẫu danh sách SV**: MSSV | Họ tên | Ngày sinh | Giới tính | Email | SĐT | Địa chỉ | Lớp. Cột Lớp ghi **tên lớp** (16DHTH01); vẫn nhận mã lớp (CNTT2501) và file mẫu cũ có cột "Mã lớp". Trước khi ghi được quy đổi về `maLop`. Mọi file xuất ghi cột Lớp là tên lớp. Lỗi: MSSV sai định dạng / đã tồn tại / trùng trong file; lớp không tồn tại; ngày sinh sai; giới tính sai. Ghi: insert SV (monHoc rỗng) + tài khoản.
- **Mẫu bảng điểm (theo lớp học phần)**: phần đầu file ghi Mã môn, Tên môn, Năm học, Học kỳ, Giảng viên; bảng: STT | MSSV | Họ tên | Lớp | CC | GK | CK. "Tải mẫu" = xuất danh sách lớp học phần hiện tại kèm điểm đang có. Lỗi: MSSV không thuộc lớp học phần; điểm không phải số / ngoài 0–10 / quá 1 chữ số thập phân. **Ô trống = giữ nguyên điểm cũ.** Ghi: bulkWrite `$set`+arrayFilters (GV: kèm điều kiện maGV).
- **Xuất**: bảng điểm lớp học phần; bảng điểm cá nhân (S-02, A-06); báo cáo học lực (A-10, G-04, A-03). Có tiêu đề, ngày xuất, định dạng cột, tên file không dấu (ví dụ `BangDiem_INT301_2026-2027_HK1.xlsx`).

---

## 7. Dữ liệu mẫu (Seeder)

- Chạy tự động khi DB trống; `--reset` xóa database rồi tạo lại. **Random seed cố định 2026** → chạy lại ra đúng một bộ dữ liệu.
- Họ tên tiếng Việt tự nhiên (danh sách họ, tên đệm theo giới tính, tên), **không đánh số**. Ngày sinh năm 2007. Địa chỉ quận/huyện TP.HCM và các tỉnh lân cận.

### 7.1 Lớp (niên khóa 2025-2029, Khoa Công nghệ thông tin), mỗi lớp 20 SV
| maLop | tenLop | Ngành | CVHT |
|---|---|---|---|
| CNTT2501 | 16DHTH01 | Công nghệ thông tin | GV001 |
| CNTT2502 | 16DHTH02 | Công nghệ thông tin | GV002 |
| ATTT2501 | 16DHATTT01 | An toàn thông tin | GV005 |
| HTTT2501 | 16DHHTTT01 | Hệ thống thông tin | GV007 |
| KHDL2501 | 16DHKHDL01 | Khoa học dữ liệu | GV003 |
MSSV liên tiếp `2001250001` … `2001250100` (lớp 1: 001–020, lớp 2: 021–040, …).

### 7.2 Giảng viên
| maGV | Họ tên | Học vị | Dạy |
|---|---|---|---|
| GV001 | Nguyễn Văn An | ThS. | INT101, INT102 |
| GV002 | Lê Quốc Cường | TS. | INT201, MAT102 |
| GV003 | Phạm Thị Hồng | TS. | MAT101 |
| GV004 | Võ Minh Tuấn | ThS. | POL101, POL102, LAW101 |
| GV005 | Trần Văn Bình | ThS. | INT301, INT202 |
| GV006 | Đặng Thu Hà | ThS. | ENG101, ENG102 |
| GV007 | Huỳnh Gia Bảo | ThS. | INT203 |
| GV008 | Ngô Thị Lan | CN. | SKL101 |

### 7.3 Môn học
| maMon | Tên | TC | Trọng số | Học kỳ dùng |
|---|---|---|---|---|
| INT101 | Nhập môn lập trình | 3 | 10/30/60 | HK1 25-26 |
| MAT101 | Toán cao cấp A1 | 3 | 10/30/60 | HK1 25-26 |
| POL101 | Triết học Mác - Lênin | 3 | 10/30/60 | HK1 25-26 |
| ENG101 | Tiếng Anh 1 | 2 | 20/30/50 | HK1 25-26 |
| INT102 | Kỹ thuật lập trình | 3 | 10/30/60 | HK2 25-26 |
| MAT102 | Toán rời rạc | 3 | 10/30/60 | HK2 25-26 |
| POL102 | Kinh tế chính trị Mác - Lênin | 2 | 10/30/60 | HK2 25-26 |
| ENG102 | Tiếng Anh 2 | 2 | 20/30/50 | HK2 25-26 |
| LAW101 | Pháp luật đại cương | 2 | 10/30/60 | HK hè 25-26 (học vượt) |
| SKL101 | Kỹ năng mềm | 2 | 20/30/50 | HK hè 25-26 (học vượt) |
| INT201 | Cấu trúc dữ liệu và giải thuật | 4 | 10/30/60 | HK1 26-27 |
| INT202 | Cơ sở dữ liệu | 3 | 10/40/50 | HK1 26-27 |
| INT203 | Mạng máy tính | 3 | 10/30/60 | HK1 26-27 |
| INT301 | Cơ sở dữ liệu NoSQL | 3 | 10/30/60 | HK1 26-27 |

### 7.4 Phân bổ học kỳ (trừ 5 SV kiểm thử)
- Ghi danh theo lớp: mỗi lớp chọn **2–3 môn HK1 25-26**, **2–3 môn HK2 25-26**, **1–2 môn HK1 26-27** từ đúng nhóm học kỳ ở bảng trên. Bắt buộc lớp 16DHTH01 và 16DHTH02 (CNTT2501, CNTT2502) có INT301 ở HK1 26-27 (GV005 – dùng cho demo nhập điểm).
- HK1, HK2 25-26: đủ điểm. Năng lực mỗi SV ~ N(7.0, 1.3); điểm thành phần = năng lực + nhiễu N(0, 1.0), kẹp 0–10; CC làm tròn số nguyên (thường cao hơn), GK/CK làm tròn 0.5. Bảo đảm toàn bộ dữ liệu có khoảng 15–20 lần F và có SV ở mọi mức xếp loại.
- HK hè 25-26 (~25 SV, đủ điểm): ~12 học lại môn F; ~8 cải thiện môn C/D (khoảng 3 trường hợp điểm mới **thấp hơn**); ~5 học vượt LAW101/SKL101. Vẫn còn vài SV nợ môn không học lại.
- HK1 26-27 (học kỳ hiện tại): **CC và GK đã có, CK = null.**
- capNhatLuc: HK1 → 2026-01-15, HK2 → 2026-06-10, hè → 2026-08-20, HK1 26-27 → 2026-10-20; capNhatBoi = maGV của lần học.
- **Ràng buộc: mỗi SV có tổng 4–8 phần tử `monHoc`.** Seeder tự kiểm tra và báo số liệu cuối cùng (số SV/lớp, min/max số môn, số F, số học lại/cải thiện/học vượt, phân bố xếp loại).

### 7.5 Năm sinh viên kiểm thử (lớp 16DHTH01 – maLop CNTT2501, MSSV 2001250001–2001250005, dữ liệu cố định, tên tự nhiên)
Giảng viên theo bảng 7.2. Kết quả mong đợi đã tính tay – pipeline phải ra **đúng y hệt**.

| SV | MSSV | Họ tên | Giới tính | Ngày sinh | Mật khẩu |
|---|---|---|---|---|---|
| TS1 | 2001250001 | Nguyễn Hoàng Minh | Nam | 15/03/2007 | 15032007 |
| TS2 | 2001250002 | Trần Thị Thu Trang | Nữ | 22/07/2007 | 22072007 |
| TS3 | 2001250003 | Lê Văn Phúc | Nam | 09/11/2007 | 09112007 |
| TS4 | 2001250004 | Phạm Gia Huy | Nam | 01/01/2007 | 01012007 |
| TS5 | 2001250005 | Võ Ngọc Bảo Anh | Nữ | 28/05/2007 | 28052007 |

**TS1 – 2001250001 – làm tròn nửa lên**
| Môn | Kỳ | CC | GK | CK | TK | Chữ | Hệ 4 |
|---|---|---|---|---|---|---|---|
| INT101 (3) | HK1 25-26 | 8 | 8.5 | 8.5 | **8.5** (tong 845; nếu dùng $round sẽ sai thành 8.4/B) | A | 4 |
| MAT101 (3) | HK1 25-26 | 7 | 6 | 6.5 | 6.4 | C | 2 |
| INT102 (3) | HK2 25-26 | 9 | 9 | 9 | 9.0 | A | 4 |
| ENG102 (2, 20/30/50) | HK2 25-26 | 8 | 7 | 7 | 7.2 | B | 3 |
| INT201 (4) | HK1 26-27 | 8 | 7 | null | – | – | – |
GPA HK1 = 18/6 = **3.00 Khá**; GPA HK2 = 18/5 = **3.60 Xuất sắc** (biên); Tích lũy = 36/11 = **3.27 Giỏi**, TC 11, nợ 0.

**TS2 – 2001250002 – học lại**
| Môn | Kỳ | Lần/HT | CC | GK | CK | TK | Chữ |
|---|---|---|---|---|---|---|---|
| INT101 (3) | HK1 25-26 | 1/Học mới | 5 | 2 | 2.5 | 2.6 | F |
| MAT101 (3) | HK1 25-26 | 1 | 8 | 7 | 7.5 | 7.4 | B |
| INT102 (3) | HK2 25-26 | 1 | 7 | 6 | 6 | 6.1 | C |
| POL102 (2) | HK2 25-26 | 1 | 9 | 8 | 8 | 8.1 | B |
| INT101 (3) | Hè 25-26 | 2/Học lại | 9 | 8 | 8.5 | 8.4 | B |
| INT202 (3, 10/40/50) | HK1 26-27 | 1 | 9 | 8 | null | – | – |
GPA HK1 = 9/6 = **1.50 Yếu**; HK2 = 12/5 = **2.40 Trung bình**; Hè = **3.00 Khá**; Tích lũy = 30/11 = **2.73 Khá**, TC 11, nợ 0. INT101 lần 1 không ✓, lần 2 ✓.

**TS3 – 2001250003 – cải thiện bị thấp hơn**
| Môn | Kỳ | Lần/HT | CC | GK | CK | TK | Chữ |
|---|---|---|---|---|---|---|---|
| MAT101 (3) | HK1 25-26 | 1 | 6 | 5 | 6 | 5.7 | C |
| ENG101 (2, 20/30/50) | HK1 25-26 | 1 | 9 | 8 | 8 | 8.2 | B |
| MAT102 (3) | HK2 25-26 | 1 | 7 | 7 | 7 | 7.0 | B (biên) |
| INT102 (3) | HK2 25-26 | 1 | 8 | 8 | 9 | 8.6 | A |
| MAT101 (3) | Hè 25-26 | 2/Cải thiện | 5 | 5 | 4.5 | 4.7 | D |
| INT203 (3) | HK1 26-27 | 1 | 7 | 6.5 | null | – | – |
GPA HK1 = 12/5 = **2.40 Trung bình**; HK2 = 21/6 = **3.50 Giỏi**; Hè = 3/3 = **1.00 Yếu**; Tích lũy = 33/11 = **3.00 Khá**, TC 11, nợ 0. MAT101 lần 1 ✓, lần 2 không ✓.

**TS4 – 2001250004 – nợ môn**
| Môn | Kỳ | CC | GK | CK | TK | Chữ |
|---|---|---|---|---|---|---|
| POL101 (3) | HK1 25-26 | 4 | 3 | 3 | 3.1 | F |
| INT101 (3) | HK1 25-26 | 7 | 6 | 5.5 | 5.8 | C |
| INT102 (3) | HK2 25-26 | 6 | 4 | 4 | 4.2 | D |
| ENG102 (2, 20/30/50) | HK2 25-26 | 5 | 3 | 3 | 3.4 | F |
| INT201 (4) | HK1 26-27 | 6 | 5 | null | – | – |
GPA HK1 = 6/6 = **1.00 Yếu**; HK2 = 3/5 = **0.60 Kém**; Tích lũy = 9/6 = **1.50 Yếu**, TC 6, **nợ 2**.

**TS5 – 2001250005 – biên Xuất sắc**
| Môn | Kỳ | CC | GK | CK | TK | Chữ |
|---|---|---|---|---|---|---|
| INT101 (3) | HK1 25-26 | 10 | 9 | 9 | 9.1 | A |
| MAT101 (3) | HK1 25-26 | 9 | 8.5 | 9 | 8.9 (tong 885) | A |
| ENG101 (2, 20/30/50) | HK1 25-26 | 8 | 7.5 | 7.5 | 7.6 | B |
| POL102 (2) | HK2 25-26 | 9 | 8 | 7.5 | 7.8 | B |
| INT301 (3) | HK1 26-27 | 10 | 9 | null | – | – |
GPA HK1 = 30/8 = **3.75 Xuất sắc**; HK2 = **3.00 Khá**; Tích lũy = 36/10 = **3.60 Xuất sắc** (biên), TC 10, nợ 0.

Lưu ý: TS5 có INT301 nên cũng xuất hiện trong lớp học phần INT301 của GV005.

---

## 8. Giao diện

- Bootstrap 5 (có sẵn), Bootstrap Icons, Chart.js – **tất cả nằm trong `wwwroot/lib`, không dùng CDN** (demo có thể mất mạng). Font: system font stack (Segoe UI…), hỗ trợ tiếng Việt.
- Phong cách tham khảo lms.hoclieu.vn: sidebar xanh dương đậm (gradient `#1E40AF` → `#2563EB`), chữ trắng, mục active nền sáng; nội dung nền `#F5F7FB`, thẻ trắng bo góc 12px, bóng nhẹ; nút chính `#2563EB`. Không dùng logo/tên thương hiệu của hệ thống khác.
- Razor render phía server (đúng MVC). JavaScript vanilla chỉ cho: lưu điểm từng dòng, toast, biểu đồ, modal, chọn nhiều SV. Không dựng trang bằng chuỗi HTML trong JS.
- Văn bản giao diện 100% tiếng Việt có dấu. Ngày `dd/MM/yyyy`. Số dùng dấu chấm thập phân; GPA 2 chữ số, điểm 1 chữ số.
- Responsive cơ bản (sidebar thu gọn ở màn hình hẹp).

---

## 9. Ngoài phạm vi – KHÔNG làm
Đăng ký tài khoản công khai, quên mật khẩu qua email, đăng ký học phần trực tuyến, thời khóa biểu, học phí, điểm rèn luyện, bài giảng, bài tập, đề thi, chấm bài, bảng tin/thông báo, chat, upload file ngoài Excel, đa ngôn ngữ, xếp loại tốt nghiệp, khóa điểm, nhật ký hệ thống ngoài `capNhatLuc/capNhatBoi`.
