# Báo cáo Giai đoạn 5 – Ghi danh ($push / $pull) + Nhập điểm ($set + arrayFilters)

- **Ngày hoàn thành:** 28/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Commit:** `ca04d76` (điền sau khi người dùng nghiệm thu và commit)

## 1. Đã làm gì
- **`GhiDanhService`**
  - **Xem trước** (`XemTruocAsync`): với mỗi SV, lấy các lần học trước cùng `maMon` từ **kết quả pipeline `BangDiem`** rồi xác định `lanHoc` và `hinhThuc`: chưa từng học → Học mới, đã có lần đạt → Cải thiện, còn lại → Học lại. Các trường hợp bị chặn đều kèm lý do: đã ghi danh môn này trong học kỳ này, lần học trước chưa có kết quả, SV không ở trạng thái "Đang học", không tìm thấy SV, môn/GV không tồn tại. C# chỉ đọc `duDiem`/`dat` do pipeline trả về, không tự tính điểm.
  - **Xác nhận** (`XacNhanAsync`): chạy lại bước xem trước ở server, sau đó `bulkWrite` (ordered: false) gồm mỗi dòng hợp lệ một `updateOne` với `$push` phần tử mới và `$set` `ngayCapNhat`. Phần tử mới được dựng bằng `BsonDocument` tường minh: `maDK` mới; tên môn, TC và trọng số là ảnh chụp từ `monhoc`; `giangVien.hoTen` kèm học vị; 3 điểm và `capNhatLuc/Boi` đều `null`.
  - **Hủy ghi danh** (`HuyGhiDanhAsync`): `$pull`, trong filter có `$elemMatch` yêu cầu 3 điểm đều `null`. Nếu matchedCount = 0 thì báo "Đã có điểm, không thể hủy".
- **A-09 `/Admin/GhiDanh`:** một trang 4 bước. B1 chọn năm học, học kỳ (1/2/hè), môn, GV. B2 chọn **cả lớp** hoặc **từng SV**: ô tìm theo MSSV/tên/lớp, tick chọn nhiều, khung "Đã chọn" bấm × để bỏ. B3 là bảng xem trước, cột hình thức có badge màu, dòng bị chặn tô đỏ kèm lý do. B4 "Xác nhận ghi danh N sinh viên" chỉ ghi các dòng hợp lệ, toast hiện lệnh `bulkWrite`. Trang nhận `?maLop=` (từ chi tiết lớp) và `?maSV=` (từ hồ sơ SV) để điền sẵn.
- **A-06 Hồ sơ SV:** nút "Ghi danh môn" đã bật (chuyển sang A-09 với `?maSV=`). Dòng chưa có điểm nào có nút "Hủy ghi danh"; bấm sẽ mở modal xác nhận, xong hiện toast kèm lệnh `$pull`.
- **G-01 `/GiangVien`:** lời chào kèm học vị, chọn học kỳ (mặc định học kỳ hiện tại, danh sách gồm các học kỳ GV có dạy), 3 thẻ số (lớp học phần, tổng sĩ số, SV chưa đủ điểm), mỗi lớp học phần một thẻ có thanh tiến độ `soDuDiem/siSo` và nút Nhập điểm. Dữ liệu lấy từ pipeline `LopHocPhanCuaGV`.
- **G-02 `/GiangVien/LopHocPhan`:** bảng tất cả lớp học phần của GV, lọc được theo học kỳ.
- **G-03 `/GiangVien/NhapDiem?maMon=&namHoc=&hocKy=`:** GV không dạy lớp học phần đó sẽ nhận **403**. Đầu trang hiện môn, TC, trọng số, học kỳ, các lớp, sĩ số và tiến độ. Bảng lấy từ pipeline `DanhSachNhapDiem`, lọc được theo lớp và có ô "Chỉ hiện chưa đủ điểm".
  - **Lưu từng dòng:** `fetch` gửi JSON kèm header antiforgery → `updateOne` + `$set` + `arrayFilters [{e.maDK, e.giangVien.maGV}]` → chạy lại `BangDiem` của đúng SV đó → cập nhật TK/Chữ/Hệ 4 trên dòng, toast hiện lệnh. Phím Enter trong ô cũng lưu dòng.
  - **Lưu tất cả:** gửi các dòng đã sửa → `bulkWrite` → chạy lại `DanhSachNhapDiem` rồi cập nhật cả bảng → toast hiện số dòng và lệnh đầu.
  - **Ô nhập:** nhận `8,5` và `8.5`, trong khoảng 0–10, tối đa 1 chữ số thập phân, để trống là `null`. Ô sai viền đỏ, dòng đã sửa nhưng chưa lưu tô vàng, rời trang khi còn dòng chưa lưu sẽ có cảnh báo. Server kiểm tra lại tất cả. Các nút Tải mẫu, Xuất Excel, Nhập Excel đã đặt sẵn và đang tắt (GĐ7).

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `Services/GhiDanhService.cs` (mới) | Xem trước, xác nhận (bulkWrite `$push`), hủy ghi danh (`$pull`) |
| `Services/DiemService.cs` | Thêm `CapNhatDiemAsync`, `CapNhatNhieuDiemAsync`, `LayLopHocPhanAsync`; dựng lệnh `$set` + arrayFilters |
| `Infrastructure/DiemNhap.cs` (mới) | Chuẩn hóa/kiểm tra ô điểm ở server |
| `Infrastructure/MongoCommandFormatter.cs` | Thêm `BulkWriteUpdateOne` (lệnh đầu + tổng số lệnh) và record `LenhUpdateOne` (dùng chung để chạy và in) |
| `Infrastructure/HienThi.cs` | `DiemNhap` (giá trị ô nhập), `LopBadgeHinhThuc` |
| `Areas/Admin/Controllers/GhiDanhController.cs` + `Views/GhiDanh/Index.cshtml` (mới) | A-09 và action `Huy` |
| `Areas/Admin/Controllers/SinhVienController.cs`, `Views/SinhVien/HoSo.cshtml`, `Views/Shared/_BangDiem.cshtml`, `ViewModels/BangDiemViewModel.cs` | Nút Ghi danh môn / Hủy ghi danh trên A-06 |
| `Areas/GiangVien/Controllers/{Home,LopHocPhan,NhapDiem}Controller.cs` + Views | G-01, G-02, G-03 (JSON `LuuDong`, `LuuTatCa`) |
| `Areas/GiangVien/Controllers/GiangVienControllerBase.cs` | `MaGV` lấy từ claim |
| `ViewModels/GhiDanhViewModels.cs`, `NhapDiemViewModels.cs` (mới) | Yêu cầu/kết quả ghi danh, DTO lưu điểm, JSON trả về |
| `Services/SinhVienService.cs` | `TatCaRutGonAsync` (danh sách SV không kèm `monHoc`) |
| `wwwroot/js/ghi-danh.js`, `nhap-diem.js` (mới); `eduscore.js` | Chọn nhiều SV; ô nhập điểm, lưu, lọc, cảnh báo rời trang; `<select data-tu-gui>` |
| `wwwroot/css/site.css` | Style bước, chọn SV, badge hình thức, thẻ lớp học phần, dòng đã sửa |

Không pipeline nào bị sửa nên `database/pipelines/*.json` giữ nguyên.

## 3. Kết quả tự kiểm tra
Kiểm tra bằng script Python (urllib, cookie, antiforgery) trên dữ liệu sau `--reset`, đối chiếu bằng mongosh.

| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | 0 lỗi, 0 cảnh báo |
| Trang | `/Admin/GhiDanh`, `?maLop=16DHTH02` (chọn sẵn lớp), `?maSV=2001250004` (tick sẵn), hồ sơ, `/GiangVien`, `/GiangVien/LopHocPhan`, G-03 INT301 | Tất cả 200 |
| Ghi danh LAW101 (GV004, HK1 2026-2027) cho `2001250010` | Xem trước → Xác nhận → mongosh | "Học mới", lần 1; phần tử mới nằm **cuối mảng**, `diem` 3 null, `capNhatLuc/Boi` null; toast có `bulkWrite ... $push`, matched/modified 1 ✅ |
| Ghi danh lại | Xem trước + POST xác nhận lần 2 | Bị chặn "Đã ghi danh LAW101 trong HK1 2026-2027"; server báo "Không có sinh viên hợp lệ" ✅ |
| Hủy | `$pull` phần tử LAW101 / phần tử đã có điểm | Phần tử biến mất / bị chặn "Đã có điểm, không thể hủy" (toast kèm lệnh, matchedCount 0) ✅ |
| Hình thức TS4 | INT101 (đã đạt C) / POL101 (F) | "Cải thiện" lần 2 / "Học lại" lần 2 ✅ |
| Chặn khác | INT201 cho TS1 (HK1 26-27 chưa có CK); MSSV `2009999999`; thiếu môn | "Lần học trước chưa có kết quả" / "Không tìm thấy sinh viên." / "Vui lòng chọn môn học." ✅ |
| Cả lớp | INT203 (GV007) HK1 2026-2027 cho 16DHTH02 | 20 hợp lệ, toast hiện lệnh đầu + "tổng cộng 20 lệnh updateOne", mongosh đếm 20 ✅ |
| G-01 GV005 | Trang tổng quan | "ThS. Trần Văn Bình"; INT202 (41 SV) và INT301 (56 SV) đều 0%; thẻ số 2 / 97 / 97 ✅ |
| Nhập CK = 9 cho TS5 INT301 | POST `LuuDong` | TK **9.1**, A, hệ 4 = 4; mongosh `diem.ck` = 9, `capNhatBoi` "GV005"; toast có `updateOne ... arrayFilters` ✅ |
| Dấu phẩy / giá trị sai | `8,5` → lưu 8.5 (TK 8.8); `10.5`, `8.55`, `abc`, `-1`, `11` | Đúng / bị từ chối kèm lý do ✅ |
| Sửa điểm môn của GV khác | GV005 gửi `maDK` INT101 của TS5 (GV001 dạy) | "Không có quyền...", matchedCount 0; mongosh: điểm, `capNhatBoi` và **`ngayCapNhat` không đổi** ✅ |
| Lưu tất cả | 1 dòng có CK=12 → không ghi dòng nào; 2 dòng hợp lệ + 1 dòng không quyền | "Đã lưu 2/3 dòng"; TK 6.7 (C) khớp tính tay; GK để trống → `null`; tiến độ G-01 2/56 ✅ |
| Phân quyền | SV vào G-03 / A-09 / POST LuuDong; GV vào A-09; G-03 lớp không dạy | 403 ✅; thiếu tham số → 404 |
| JS | `node --check` 3 file | Cú pháp hợp lệ |
| Pipeline | `--reset --kiem-thu-gpa` sau khi test | 200/200 PASS; DB về nguyên bản |

**Chưa tự kiểm tra được trong trình duyệt** (không có trình duyệt tự động): hiệu ứng tô màu dòng, cảnh báo rời trang, khung "Đã chọn" và việc cập nhật dòng sau khi bấm Lưu. Các phần này nằm trong checklist nghiệm thu bên dưới.

## 4. Quyết định kỹ thuật mới
- **Xem trước dùng GET, xác nhận dùng POST rồi redirect về trang xem trước** (Post-Redirect-Get). Sau khi ghi, bảng xem trước tự hiện các dòng "Đã ghi danh", người dùng thấy ngay kết quả và F5 không ghi lại lần nữa.
- **Server không tin bảng xem trước của trình duyệt:** `XacNhanAsync` chạy lại toàn bộ bước xem trước rồi mới ghi.
- **Chống ghi danh trùng ngay trong lệnh ghi:** filter của `$push` có thêm `monHoc: {$not: {$elemMatch: {maMon, namHoc, hocKy}}}`. Kiểm tra và ghi nằm trong một thao tác nguyên tử trên một document, nên hai người bấm xác nhận cùng lúc cũng không tạo bản trùng. Thiết kế 4.8 chỉ ghi `{maSV}`; phần thêm này là bổ sung an toàn, không đổi nghiệp vụ.
- **Nhập điểm: filter lặp lại điều kiện của arrayFilters** (`monHoc: {$elemMatch: {maDK, "giangVien.maGV"}}`). Nếu chỉ có arrayFilters, lệnh của GV không có quyền vẫn khớp document theo `maSV` và **vẫn ghi `ngayCapNhat`**, khiến modifiedCount = 1, tức là báo thành công sai. Khi có thêm filter, không có quyền thì matched = modified = 0 và DB hoàn toàn không đổi. Quyền vẫn nằm trong arrayFilters đúng như thiết kế.
- **modifiedCount = 0 ⇔ không có quyền / không tìm thấy:** `capNhatLuc` luôn đổi theo thời gian, nên lưu lại cùng một điểm vẫn cho modified = 1.
- **bulkWrite `ordered: false`:** các SV độc lập nhau, một lệnh lỗi không làm dừng các lệnh còn lại; toast báo "đã lưu X/Y".
- **Lưu tất cả: một dòng sai thì không ghi dòng nào** (kiểm tra hết trước khi gửi bulkWrite), tránh lưu nửa vời.
- **Điểm gửi lên dạng chuỗi** đúng như người dùng gõ; server tự chuẩn hóa (`DiemNhap`), JS chỉ kiểm tra để báo sớm.
- **JS không tính điểm:** server trả TK, chữ, hệ 4 đã định dạng kèm sẵn lớp CSS badge (`DongDiemJson`). Thanh tiến độ chỉ đếm cờ `duDiem` do pipeline trả về.
- **Hình thức học: chạy `BangDiem` mỗi SV một lần** (cả lớp là 20 lần). Cách này giữ nguyên pipeline đã kiểm thử, mỗi lần chạy chỉ vài ms.

## 5. Điểm cần chú ý / hạn chế
- **Thẻ "Sinh viên" ở G-01 là tổng sĩ số các lớp học phần.** Một SV học 2 môn của cùng GV trong kỳ sẽ được đếm 2 lần (nhãn ghi rõ "tổng sĩ số").
- Danh sách GV ở A-09 gồm mọi GV, không lọc theo môn (thiết kế không có bảng phân công môn–GV). Người dùng tự chọn GV phù hợp (bảng 7.2).
- Ghi danh không kiểm tra thứ tự thời gian, ví dụ ghi danh vào một học kỳ cũ hơn lần học đã có. Thiết kế không quy định nên giữ quy tắc của mục 2.4 ("số lần học trước" = mọi lần học cùng môn).
- Hủy ghi danh chỉ được khi **cả 3 điểm đều null**. Lần học HK1 2026-2027 trong dữ liệu mẫu đã có CC/GK nên không hủy được; muốn thử thì dùng môn vừa ghi danh.
- G-03 vẫn cho sửa điểm học kỳ cũ (thiết kế: "Học kỳ không phải hiện tại vẫn sửa được").
- `docs/kiem-thu-gpa.md` chỉ đổi thời điểm chạy (đã chạy lại sau khi test).
- Lúc bắt đầu build có một tiến trình `DT2_LMS_NHOM6.exe` đang chạy sẵn; tôi đã dừng nó để build được.

## 6. Nghiệm thu
Chạy `dotnet run --project DT2_LMS_NHOM6` (DB đã ở trạng thái `--reset`).
- [ ] Đăng nhập `GV005 / 123456` → Tổng quan thấy lớp học phần INT301 HK1 2026-2027 với tiến độ 0%.
- [ ] Bấm "Nhập điểm" INT301 → gõ CK `9` cho `2001250005` (dòng tô vàng) → bấm Lưu (hoặc Enter) → toast hiện lệnh `updateOne ... arrayFilters` (bấm "Lệnh MongoDB đã chạy"); TK **9.1**, Chữ **A**, Hệ 4 **4** cập nhật ngay; tiến độ 1/56. Thử gõ `8,5` → rời ô thành `8.5`; gõ `11` → viền đỏ, nút Lưu bị tắt. Sửa 2–3 dòng → "Lưu tất cả (n)". Sửa một ô rồi bấm sang trang khác → trình duyệt hỏi xác nhận rời trang.
- [ ] Mở Compass, tìm `{maSV: "2001250005"}` → thấy `diem.ck` mới nằm đúng phần tử INT301 trong mảng `monHoc`, `capNhatBoi: "GV005"`.
- [ ] Admin (`admin / Admin@123`): Lớp học → 16DHTH02 → "Ghi danh cả lớp" → chọn HK1 2026-2027, môn (ví dụ INT203), GV (GV007) → Xem trước (20 dòng "Học mới") → Xác nhận → toast hiện `bulkWrite` + "tổng cộng 20 lệnh" → hồ sơ một SV lớp 16DHTH02 có môn mới ở HK1 2026-2027 → bấm "Hủy ghi danh" ở dòng đó → toast hiện lệnh `$pull`, môn biến mất.
- [ ] Hồ sơ `2001250004` → "Ghi danh môn" (SV đã được tick sẵn) → chọn POL101, GV004 → Xem trước → hình thức **"Học lại"**, lần 2. (Đổi sang INT101 → "Cải thiện".)
- [ ] `dotnet run --project DT2_LMS_NHOM6 -- --reset` rồi commit.

## 7. Gợi ý trả lời khi bảo vệ
- **`$[e]` + arrayFilters khác gì `$` (positional)?** `$` chỉ trỏ tới phần tử **đầu tiên** khớp với điều kiện ở filter, và chỉ dùng được khi filter có điều kiện trên mảng. `$[e]` trỏ tới **mọi** phần tử thỏa điều kiện của `e` trong `arrayFilters`, đặt riêng khỏi filter. Ở đây điều kiện `e.maDK` + `e.giangVien.maGV` bảo đảm chỉ đúng lần học đó, và chỉ khi do chính GV này dạy, mới bị sửa. Quyền được kiểm tra ngay trong câu lệnh của MongoDB chứ không chỉ ở C#.
- **GV sửa trộm điểm môn khác bằng cách gửi `maDK` khác thì sao?** `maGV` lấy từ cookie đăng nhập (claim), không lấy từ request. Phần tử không thỏa `e.giangVien.maGV` thì không bị sửa; filter cũng có `$elemMatch` cùng điều kiện nên document không khớp, matchedCount = 0 và giao diện báo "Không có quyền". Đã thử: DB không đổi, kể cả `ngayCapNhat`.
- **Vì sao hủy ghi danh đặt điều kiện "chưa có điểm" vào filter thay vì kiểm tra trước bằng C#?** Nếu đọc rồi mới ghi, giữa hai bước GV có thể vừa nhập điểm, và lúc đó ta sẽ xóa mất điểm. Đặt `$elemMatch {maDK, diem.cc: null, ...}` trong filter thì kiểm tra và `$pull` là một thao tác nguyên tử trên một document (MongoDB bảo đảm nguyên tử ở mức document, không cần transaction).
- **Hình thức "Học lại/Cải thiện" được xác định thế nào, có tính điểm bằng C# không?** Không. Service chạy pipeline `BangDiem` của SV; pipeline tính `duDiem` và `dat` bằng bộ stage quy đổi dùng chung. C# chỉ đếm số lần học trước cùng môn (→ `lanHoc`) và xem có lần nào `dat = true` (→ Cải thiện) hay không.
- **Ghi danh nhiều SV có nguyên tử không?** Không có transaction (standalone). `bulkWrite` gửi nhiều `updateOne` trong một lượt; mỗi lệnh nguyên tử trên một SV. `ordered: false` để một SV lỗi không chặn SV khác, kết quả matched/modified được báo trong toast. Chạy lại cũng an toàn vì filter chặn ghi trùng.
