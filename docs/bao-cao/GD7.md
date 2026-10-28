# Báo cáo Giai đoạn 7 – Nhập / Xuất Excel

- **Ngày hoàn thành:** 28/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Commit:** `<mã commit>` (điền sau khi người dùng nghiệm thu và commit)

## 1. Đã làm gì
- **ExcelService (ClosedXML)** đọc và ghi file theo thiết kế mục 6. Mọi file xuất có cùng bố cục:
  - dòng 1 "CỔNG QUẢN LÝ HỌC TẬP – EDUSCORE", dòng 2 tên báo cáo;
  - các dòng "Nhãn: giá trị" ghi thông tin lọc và ngày xuất;
  - header in đậm, nền xanh nhạt, có viền, cố định khi cuộn;
  - cột MSSV / ngày sinh / SĐT định dạng Text, điểm `0.0`, GPA `0.00`, độ rộng cột đặt sẵn, trang in khổ ngang;
  - tên file không dấu, ví dụ `BangDiem_INT301_2026-2027_HK1_GV005.xlsx`.
- **Nhập danh sách SV** (A-04 nút "Nhập Excel"/"Tải mẫu", A-11):
  - File mẫu có sheet `DanhSach` (chỉ dòng tiêu đề cột, các cột cần nhập dạng Text) và sheet `HuongDan` (quy tắc, 1 dòng ví dụ, danh sách mã lớp hợp lệ).
  - Upload → **kiểm tra toàn bộ file** → trang kết quả: mỗi dòng xanh (hợp lệ) hoặc đỏ kèm lý do.
  - Lỗi được bắt: MSSV sai định dạng / đã tồn tại / trùng trong file; lớp không tồn tại; ngày sinh sai; giới tính sai; thêm họ tên, email, SĐT sai (cùng quy tắc với form A-05).
  - Không có lỗi → nút **Xác nhận** → `insertMany` sinhvien rồi `insertMany` taikhoan (bước sau lỗi thì hoàn tác bước trước) → toast số lượng kèm lệnh. Có lỗi → không ghi gì, cho tải file khác.
- **Nhập bảng điểm** (G-03 nút "Nhập Excel"; A-11 cho Admin nhập bất kỳ lớp học phần):
  - **Mẫu = bảng điểm xuất ra** (nút "Tải mẫu / Xuất Excel"). Phần đầu ghi Mã môn, Tên môn, Năm học, Học kỳ, Giảng viên, trọng số, ngày xuất, ghi chú; bảng gồm STT | MSSV | Họ tên | Lớp | CC | GK | CK kèm điểm đang có.
  - Upload → đọc phần đầu để xác định lớp học phần → kiểm tra từng dòng: MSSV thuộc lớp học phần, không trùng; điểm 0–10, tối đa 1 chữ số thập phân, chấp nhận dấu phẩy; **ô trống = giữ nguyên**.
  - Trang xem trước: mỗi ô hiện `cũ → mới` (gạch ngang điểm cũ), dòng thay đổi tô vàng, dòng lỗi tô đỏ.
  - **Xác nhận** → `bulkWrite` các `updateOne` dùng `$set` + `arrayFilters`. Mỗi dòng chỉ `$set` thành phần thật sự đổi. GV có thêm điều kiện `e.giangVien.maGV`; Admin ghi `capNhatBoi: "admin"`. Toast kèm lệnh đầu + tổng số lệnh.
  - GV tải file của lớp học phần GV khác → báo lỗi, không ghi.
- **Gắn nút xuất** vào màn hình đã có:
  - A-04: Xuất Excel danh sách SV theo bộ lọc đang chọn, không phân trang, kèm GPA và xếp loại.
  - A-06 và S-02: xuất bảng điểm cá nhân (thông tin SV, mỗi học kỳ một khối kèm GPA học kỳ, cuối file là tổng kết tích lũy).
  - A-10, G-04: xuất báo cáo học lực đúng bộ lọc và phạm vi đang chọn, kèm bảng thống kê theo xếp loại. A-03 dùng cùng chức năng này với lớp đang xem, phạm vi tích lũy.
  - G-03: Tải mẫu / Xuất bảng điểm.
- **A-11 `/Admin/DuLieu`** có 4 thẻ:
  - Nhập danh sách SV;
  - Nhập bảng điểm;
  - Xuất bảng điểm lớp học phần: ô chọn nhóm theo học kỳ, lấy từ pipeline nhỏ mới `LopHocPhanToanTruong`;
  - Xuất báo cáo học lực theo lớp và phạm vi, kèm link xuất danh sách toàn bộ SV.
- **Lệnh `-- --tao-file-demo`** ghi 4 file vào `docs/demo/`, dựng bằng chính `ExcelService` từ dữ liệu mẫu, rồi đọc lại toàn bộ các loại file xuất để kiểm tra.

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `Services/ExcelService.cs` (mới) | ClosedXML thuần: bố cục chung, 4 loại file xuất + mẫu SV, đọc file SV / bảng điểm, `TenFile` bỏ dấu |
| `Services/XuatExcelService.cs` (mới) | Lấy dữ liệu bằng đúng pipeline của màn hình (BangDiem, GpaHocKy, BaoCaoHocLuc, ThongKeXepLoai, DanhSachNhapDiem) rồi gọi ExcelService |
| `Services/NhapExcelService.cs` (mới) | Kiểm tra toàn bộ file SV / bảng điểm, đối chiếu MongoDB, gọi service ghi |
| `Services/KhoPhienNhap.cs` (mới) | Giữ kết quả kiểm tra giữa bước xem trước và xác nhận (IMemoryCache 30 phút, gắn người tải lên) |
| `Services/DiemService.cs` | Tách `TaoLenhSetDiem` (dùng chung lưu từng dòng và Excel); thêm `GhiDiemTuExcelAsync` (bulkWrite), `LopHocPhanToanTruongAsync` |
| `Services/SinhVienService.cs`, `Services/TaiKhoanService.cs` | `ThemNhieuAsync` (insertMany + hoàn tác theo `_id`), `DanhSachTatCaAsync`; `TaoNhieuAsync`, `TenDangNhapDaDungAsync` |
| `Infrastructure/MongoCommandFormatter.cs` | Thêm `InsertMany` (document đầu + tổng số) cho toast nhập SV |
| `Pipelines/ThongKeDanhMucPipeline.cs`, `Models/Results/ThongKeDanhMucRow.cs`, `database/pipelines/LopHocPhanToanTruong.json` (mới) | Pipeline nhỏ liệt kê lớp học phần toàn trường |
| `Areas/Admin/Controllers/DuLieuController.cs` + `Views/DuLieu/{Index,XemTruocSinhVien,XemTruocDiem}.cshtml` (mới) | A-11 và luồng nhập của Admin |
| `Areas/GiangVien/Controllers/NhapDiemController.cs` + `Views/NhapDiem/{Index,XemTruocExcel}.cshtml` | G-03: Xuất Excel, Nhập Excel → xem trước → xác nhận |
| `Areas/Admin/Controllers/{BaoCao,SinhVien}Controller.cs`, `Areas/GiangVien/Controllers/BaoCaoController.cs`, `Areas/SinhVien/Controllers/KetQuaController.cs` | Action `XuatExcel` / `XuatBangDiem`; A-10, G-04 tách hàm đọc bộ lọc dùng chung cho trang và file xuất |
| `Views/Shared/_XemTruocNhapDiem.cshtml`, `_ModalNhapExcel.cshtml` (mới) | Xem trước nhập điểm (Admin + GV), modal chọn file |
| `Views/Shared/_BaoCaoHocLuc.cshtml`, `ViewModels/BaoCaoViewModels.cs` | Nút Xuất Excel theo `UrlXuatExcel` |
| Views A-03, A-04, A-06, S-02 | Bật các nút Excel (trước đây đặt sẵn và đang tắt) |
| `ViewModels/NhapExcelViewModels.cs` (mới) | Dòng nhập SV / điểm, phiên nhập, view model xem trước, A-11 |
| `Data/TaoFileDemo.cs` (mới), `Program.cs` | Lệnh `--tao-file-demo`; đăng ký service + `AddMemoryCache` |
| `docs/demo/*.xlsx` (mới) | 4 file demo |
| `wwwroot/css/site.css` | Dòng hợp lệ / thay đổi / lỗi, badge kết quả, điểm cũ → mới |

Các pipeline tính điểm **không sửa**: chạy lại `--export-pipelines` thì 11 file cũ không đổi, chỉ có thêm `LopHocPhanToanTruong.json`.

## 3. Kết quả tự kiểm tra
Kiểm tra bằng script Python urllib (đăng nhập bằng cookie, gửi antiforgery token, upload multipart). Đếm số liệu bằng mongosh.

| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | 0 lỗi, 0 cảnh báo |
| Kiểm thử GPA | `--reset --export-pipelines --kiem-thu-gpa` | 200/200 PASS |
| Sinh file demo | `--reset --tao-file-demo` | ✅ 4 file; đọc lại: `diem_*` 68/69 dòng, `sinhvien_*` 6/8 dòng + sheet HuongDan |
| Đọc lại file xuất bằng ClosedXML | Lệnh tạo demo đọc lại 5 loại file; giải nén xlsx xem chuỗi, ô gộp, cố định header | ✅ mở được; tiêu đề, thông tin lọc, ngày xuất đúng; header cố định (`A9` ở báo cáo) |
| `sinhvien_loi.xlsx` | Upload ở A-11 | ✅ 6 dòng đỏ, 1 dòng xanh; báo đủ: đã tồn tại, lớp 16DHXX99 không tồn tại, ngày 31/02/2007 sai, trùng trong file (dòng 6, 7), MSSV 9 số, giới tính, email, SĐT; không có nút Xác nhận; **SV 100 → 100, TK 109 → 109** |
| `sinhvien_hop_le.xlsx` | Upload → Xác nhận | ✅ 5 dòng xanh → toast "Đã nhập 5 sinh viên và tạo 5 tài khoản…"; **SV 105, TK 114**; SV mới `monHoc: []`, ngày sinh 00:00 UTC |
| Bấm Xác nhận lần 2 | POST lại cùng mã phiên | ✅ "Phiên nhập không còn…", không ghi thêm |
| Tải lại file hợp lệ | Upload lần 2 | ✅ 5 dòng lỗi "MSSV đã tồn tại" |
| File sai đuôi | Upload `.csv` từ A-04 | ✅ toast "Chỉ nhận file Excel .xlsx.", quay lại A-04 |
| `diem_INT301_loi.xlsx` (GV005) | Upload ở G-03 | ✅ 4 dòng lỗi: "CK: Điểm 11 vượt quá 10", "CK: "abc" không phải điểm…", "GK: "8.25" không phải điểm…", "MSSV không thuộc lớp học phần này"; **số CK đã có vẫn 0/56** |
| `diem_INT301_hop_le.xlsx` (GV005) | Upload → xem trước → Xác nhận | ✅ 56 dòng thay đổi → "Đã nhập điểm từ Excel cho 56/56 sinh viên"; mongosh **CK đã có 56/56**, `capNhatBoi: "GV005"`; toast có lệnh `bulkWrite`, chỉ `$set` `diem.ck`, `arrayFilters` có `e.giangVien.maGV` |
| GV tải file lớp học phần GV khác | Admin xuất `INT201_2026-2027_1_GV002`, GV005 tải lên | ✅ "File là bảng điểm lớp học phần INT201 của giảng viên GV002…", không ghi |
| Admin nhập điểm (sau `--reset`) | A-11: file lỗi rồi file hợp lệ | ✅ file lỗi không ghi; file hợp lệ 56/56, `capNhatBoi: "admin"`, `arrayFilters` chỉ có `e.maDK` |
| File không có thay đổi | Admin tải lại file GV002 vừa xuất | ✅ "không có điểm nào khác…", không có nút Xác nhận |
| Các file xuất | Tải về: kiểm tra Content-Type, tên file, chữ ký `PK` | ✅ `MauNhapSinhVien`, `DanhSachSinhVien_16DHTH01`, `BangDiem_2001250001`, `BaoCaoHocLuc_TatCa_TichLuy`, `BaoCaoHocLuc_16DHTH01_2025-2026_HK2`, `BangDiem_INT301_2026-2027_HK1_GV005`, `BaoCaoHocLuc_GV005_…` |
| Tham số sai / phân quyền | Lớp học phần không tồn tại; MSSV không có; GV xuất `INT101 2025-2026` (không dạy); GV → `/Admin/DuLieu`; SV → `/Admin/SinhVien/XuatBangDiem/...` | ✅ 404 / 404 / 403 / 403 / 403 |
| Nút trên giao diện | Tìm link xuất ở A-03, A-04, A-06, A-10, G-03, S-02; tìm chữ "Chức năng Excel sẽ…" | ✅ đủ link, không còn nút tắt |
| Log ứng dụng | Tìm "exception/fail" | ✅ không có |
| Dọn dẹp | `--reset` sau khi kiểm tra; xóa file tạm | ✅ 100 SV, 109 TK |

## 4. Quyết định kỹ thuật mới
- **Tách 3 lớp:**
  - `ExcelService` chỉ làm việc với file: static, không DB, không tính điểm.
  - `XuatExcelService` lấy dữ liệu bằng đúng pipeline của màn hình, nên số trong file luôn trùng số trên web.
  - `NhapExcelService` kiểm tra và gọi service ghi.
- **Giữ kết quả kiểm tra trong bộ nhớ máy chủ** (`IMemoryCache`, 30 phút, khóa Guid ngẫu nhiên, gắn tên đăng nhập người tải lên), không để trong TempData. TempData lưu bằng cookie, nên vài chục dòng đã vượt giới hạn 4 KB. Luồng PRG: POST file → redirect GET trang xem trước `?ma=` → POST Xác nhận. Ghi xong thì xóa phiên, nên bấm lại hoặc F5 không ghi lần 2.
- **Xác định lớp học phần từ phần đầu file, không từ trang đang mở.** Admin nhập được mọi lớp học phần. GV bị chặn nếu mã GV trong file khác mã GV trong claim. Lệnh ghi vẫn giữ điều kiện quyền trong `arrayFilters`, đây là lớp bảo vệ thứ hai.
- **Họ tên, lớp, điểm cũ lấy từ DB**, không tin cột Họ tên / Lớp trong file. Ô điểm số đọc theo `InvariantCulture` rồi kiểm tra bằng **cùng hàm `DiemNhap.TryParse` với ô nhập G-03**.
- **Chỉ `$set` thành phần thật sự đổi** (ô có giá trị khác điểm cũ). Dòng không đổi không sinh lệnh, nên `modifiedCount` phản ánh đúng số SV có thay đổi. `TaoLenhSetDiem` dùng chung cho lưu từng dòng, lưu tất cả và Excel.
- **Nhập SV:** kiểm tra lại trùng ngay lúc Xác nhận (có thể đã có người thêm trong lúc xem trước). `_id` được sinh ở client, nên khi hoàn tác chỉ xóa đúng document vừa chèn.
- **Phần đầu file:** nhãn đặt ở ô A:B gộp, giá trị ở ô C:… gộp, dạng Text. Khi đọc thì tra theo nhãn ("Mã môn", "Năm học", "Học kỳ", "Giảng viên"), không phụ thuộc số dòng. Giá trị "Giảng viên" có dạng `GV005 - ThS. Trần Văn Bình`; mã GV là phần trước " - ".
- **Tên file bảng điểm lớp học phần thêm mã GV** (`…_HK1_GV005.xlsx`). Lý do: cùng môn, cùng học kỳ có thể có nhiều GV.
- **Báo cáo học lực xuất kèm bảng "Thống kê theo xếp loại"**, số liệu lấy từ `ThongKeXepLoai` giống các thẻ trên web.
- File mẫu bảng điểm giữ **đúng 7 cột** theo thiết kế (không thêm TK/Chữ); TK/Chữ/Hệ 4 xem trên web hoặc trong báo cáo.

## 5. Điểm cần chú ý / hạn chế
- **Thử nhập điểm sẽ làm đổi dữ liệu kiểm thử:** INT301 có CK, TS5 có GPA HK1 2026-2027. Demo xong chạy `dotnet run --project DT2_LMS_NHOM6 -- --reset`. Các file trong `docs/demo/` vẫn dùng được sau `--reset` vì dữ liệu mẫu cố định (seed 2026).
- `diem_INT301_loi.xlsx`: 3 dòng đầu (2001250081, 082, 083) bị sửa thành CK = 11, CK = "abc", GK = 8.25; dòng cuối thêm `2001250001` (TS1 không học INT301). Ở trang xem trước, các dòng hợp lệ còn lại vẫn tô vàng để thấy trước thay đổi, nhưng vì còn lỗi nên **không có nút Xác nhận**.
- `sinhvien_hop_le.xlsx` thêm MSSV `2001250101`–`2001250105` vào lớp 16DHKHDL01. Mật khẩu của họ là ngày sinh, ví dụ `2001250101 / 05012007`.
- Nhập nhiều SV hơi chậm vì mỗi tài khoản phải băm BCrypt (khoảng 0,1 giây/tài khoản). 5 SV gần như tức thì; 100 SV mất khoảng 10 giây.
- Phiên xem trước giữ 30 phút trong bộ nhớ ứng dụng. Nếu khởi động lại app giữa chừng thì phải tải file lên lại (có thông báo).
- Mỗi lần chạy `--tao-file-demo` các file khác nhau về nhị phân (do dòng "Ngày xuất"); nội dung dữ liệu giống nhau.
- `docs/kiem-thu-gpa.md` đổi 2 dòng (thời điểm chạy). `docs/bao-cao/GD6.md` đã được sửa từ trước (điền mã commit `b7ec3c7`), tôi không đụng tới; file này sẽ đi cùng commit GĐ7 nếu dùng `git add -A`.

## 6. Nghiệm thu
- [ ] GV005 → Nhập điểm INT301 → Tải mẫu → mở Excel thấy danh sách, tiêu đề, định dạng ổn.
- [ ] Nhập `docs/demo/diem_INT301_loi.xlsx` → thấy bảng lỗi đỏ, không ghi.
- [ ] Nhập `docs/demo/diem_INT301_hop_le.xlsx` → xem trước điểm cũ → mới → xác nhận → Compass thấy điểm.
- [ ] Admin → Nhập/Xuất dữ liệu → nhập SV hợp lệ/lỗi; xuất báo cáo học lực mở bằng Excel đẹp.
- [ ] SV → Kết quả học tập → Xuất Excel.
- [ ] `--reset`, commit.

**Hướng dẫn nghiệm thu chi tiết** (chạy `dotnet run --project DT2_LMS_NHOM6`, mở http://localhost:5207). Đây cũng là kịch bản **demo 3**:
1. **GV tải mẫu:** đăng nhập `GV005 / 123456` → Lớp học phần → INT301 HK1 2026-2027 → **Nhập điểm** → bấm **Tải mẫu / Xuất Excel**. Mở file `BangDiem_INT301_2026-2027_HK1_GV005.xlsx` bằng Excel và kiểm tra:
   - dòng 1 là tiêu đề EduScore; các dòng Mã môn, Tên môn, Năm học, Học kỳ, Giảng viên, Trọng số, Ngày xuất, Ghi chú;
   - header STT…CK nền xanh nhạt, có viền, đứng yên khi cuộn;
   - 56 SV; cột MSSV là Text (căn trái, không bị đổi thành 2.00E+09); CC/GK dạng `8.0`, CK để trống.
2. **File lỗi:** bấm **Nhập Excel** → chọn `docs/demo/diem_INT301_loi.xlsx` → **Kiểm tra file**. Kết quả mong đợi:
   - Thẻ trên cùng báo "4 lỗi" và thông báo đỏ "chưa ghi điểm nào".
   - 4 dòng đỏ kèm lý do: CK 11 vượt quá 10; "abc" không phải điểm; "8.25" quá 1 chữ số thập phân; 2001250001 không thuộc lớp học phần.
   - Không có nút Xác nhận.
   - Trong Compass, `diem.ck` của các SV này vẫn `null`.
3. **File hợp lệ:** bấm **Tải file khác** → `docs/demo/diem_INT301_hop_le.xlsx` → xem trước có 56 dòng vàng; cột CK hiện `– → 5.0`, CC/GK hiện điểm cũ (không đổi). Bấm **Xác nhận ghi điểm (56 sinh viên)**:
   - Quay về G-03, toast "Đã nhập điểm từ Excel cho 56/56 sinh viên". Mở **Lệnh MongoDB đã chạy** để thấy `bulkWrite` / `$set` / `arrayFilters` có `"e.giangVien.maGV": "GV005"`.
   - Bảng G-03 đã có TK / Chữ / Hệ 4, tiến độ 56/56.
   - Trong Compass, lọc `{maSV: "2001250081"}`, mở phần tử `monHoc` INT301 → `diem.ck: 5`, `capNhatBoi: "GV005"`.
4. **Admin nhập SV:** đăng nhập `admin / Admin@123` → **Nhập/Xuất dữ liệu**.
   - Ở thẻ Nhập danh sách sinh viên, chọn `docs/demo/sinhvien_loi.xlsx`: 1 dòng xanh, 6 dòng đỏ (MSSV đã tồn tại, lớp 16DHXX99, ngày 31/02/2007, trùng dòng 6–7, MSSV 9 số + giới tính "Khác" + email + SĐT), không có nút Xác nhận. Trang Sinh viên vẫn 100.
   - Tải `docs/demo/sinhvien_hop_le.xlsx`: 5 dòng xanh → **Xác nhận nhập 5 sinh viên** → về A-04, toast số lượng kèm lệnh `insertMany`. Lọc lớp 16DHKHDL01 thấy 2001250101–105.
5. **Admin xuất:**
   - Ở A-11, thẻ Xuất báo cáo học lực, chọn lớp 16DHTH01, phạm vi Tích lũy → Xuất Excel. Mở file: tiêu đề, Lớp / Phạm vi / Số sinh viên / Ngày xuất, bảng 20 SV có GPA `0.00`, cuối file có bảng thống kê xếp loại.
   - Thử thẻ Xuất bảng điểm lớp học phần.
   - Ở A-04, bấm Xuất Excel theo bộ lọc; ở A-06, bấm Xuất bảng điểm; ở A-03, bấm Xuất báo cáo lớp; ở A-10, bấm Xuất Excel (đổi phạm vi HK2 2025-2026 để thấy tên file đổi).
6. **SV:** đăng nhập `2001250002 / 22072007` → Kết quả học tập → **Xuất Excel**. File `BangDiem_2001250002.xlsx` có thông tin SV, các khối học kỳ từ cũ đến mới, mỗi khối có dòng GPA học kỳ, cuối file là tổng kết tích lũy (GPA 2.73, Khá).
7. **Phân quyền (tùy chọn):** GV005 mở `/GiangVien/NhapDiem/XuatExcel?maMon=INT101&namHoc=2025-2026&hocKy=1` → trang 403. Ở A-11, Admin xuất bảng điểm một lớp học phần của GV002 rồi đăng nhập GV005 nhập file đó → toast đỏ "File là bảng điểm lớp học phần … của giảng viên GV002".
8. Dừng app, chạy `dotnet run --project DT2_LMS_NHOM6 -- --reset` để khôi phục dữ liệu, rồi commit.

## 7. Gợi ý trả lời khi bảo vệ
- **"Nếu file có 1 dòng sai thì sao, có ghi một phần không?"** – Không. Thiết kế mục 6: kiểm tra toàn bộ file trước; còn dòng lỗi thì trang xem trước không có nút Xác nhận và server cũng từ chối ghi (`GhiBangDiemAsync` / `GhiSinhVienAsync` kiểm tra lại `SoLoi`). Chỉ khi file sạch mới ghi một lần bằng `bulkWrite` / `insertMany`.
- **"Nhập điểm Excel ghi vào MongoDB thế nào?"** – Mỗi SV có thay đổi là một `updateOne`: filter `{maSV, monHoc: {$elemMatch: {maDK, "giangVien.maGV"}}}`, update `$set` `"monHoc.$[e].diem.ck"` + `capNhatLuc/capNhatBoi`, `arrayFilters: [{"e.maDK", "e.giangVien.maGV"}]`. Tất cả gom vào một `bulkWrite` (`ordered: false`). Đây chính là lệnh của chức năng nhập tay G-03, khác ở chỗ chỉ `$set` các ô có giá trị (ô trống = giữ nguyên).
- **"GV sửa phần đầu file thành lớp học phần của GV khác thì sao?"** – Có 2 lớp chặn. (1) Khi kiểm tra: mã GV trong file khác mã GV trong claim thì báo lỗi. (2) Kể cả khi lọt qua, lệnh ghi vẫn có `e.giangVien.maGV` = GV đăng nhập trong `arrayFilters`, nên không phần tử nào khớp và `modifiedCount = 0`.
- **"Không có transaction, nhập SV + tài khoản lỗi giữa chừng thì sao?"** – Ghi tuần tự: `insertMany` sinhvien, rồi `insertMany` taikhoan. Bước 2 lỗi thì xóa các tài khoản đã chèn và xóa các SV vừa chèn theo đúng `_id` sinh ở bước 1 (thiết kế 4.8). Ngay trước khi ghi còn kiểm tra lại trùng MSSV.
- **"Số trong file Excel có tính lại bằng C# không?"** – Không. `XuatExcelService` gọi đúng các pipeline mà màn hình dùng (`BangDiem`, `GpaHocKy`, `BaoCaoHocLuc`, `ThongKeXepLoai`, `DanhSachNhapDiem`). `ExcelService` chỉ ghi giá trị ra ô và đặt định dạng `0.0` / `0.00`.
