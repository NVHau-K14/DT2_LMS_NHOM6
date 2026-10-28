# Báo cáo Giai đoạn 6 – Báo cáo học lực, tổng quan, cổng sinh viên

- **Ngày hoàn thành:** 28/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Commit:** `<mã commit>` (điền sau khi người dùng nghiệm thu và commit)

## 1. Đã làm gì
- **A-10 `/Admin/BaoCao`:**
  - Bộ lọc gồm **Lớp** (tất cả hoặc từng lớp) và **Phạm vi** (Tích lũy hoặc từng học kỳ đã có dữ liệu; danh sách học kỳ lấy từ pipeline mới `HocKyCoDuLieu`).
  - **7 thẻ số** theo xếp loại, có cả "Kém" và "Chưa xếp loại", và **biểu đồ cột** Chart.js. Số liệu của hai phần này lấy từ pipeline `ThongKeXepLoai`, chạy với cùng bộ lọc và phạm vi như bảng.
  - **Bảng** gồm MSSV, họ tên, lớp, TC tích lũy, GPA, xếp loại (badge), số môn nợ, nút "Hồ sơ". Bấm tiêu đề cột để sắp xếp, việc sắp xếp làm ở trình duyệt. Ở phạm vi học kỳ, hai cột đổi tên thành "TC đạt" và "Số môn F".
  - Nút **Xem pipeline** mở modal có 2 tab, `BaoCaoHocLuc` và `ThongKeXepLoai`. Mỗi tab hiện JSON pipeline vừa chạy (đúng tham số `$match`/`$filter`), thời gian thực thi (ms), số dòng, số stage và nút **Sao chép**. Nút **Xuất Excel** đã đặt sẵn và đang tắt (GĐ7).
- **G-04 `/GiangVien/BaoCao`:** dùng lại partial của A-10. Dữ liệu chỉ gồm SV thuộc các lớp học phần của GV ở mọi học kỳ. Bộ lọc **lớp học phần** có giá trị mặc định "Tất cả lớp học phần của tôi". Phạm vi luôn là tích lũy (theo G5) và không có link sang hồ sơ SV.
- **A-01 `/Admin`:**
  - 4 thẻ số (lớp, SV, GV, môn); bấm vào thẻ sẽ chuyển đến trang danh mục tương ứng.
  - **Biểu đồ donut** phân bố xếp loại toàn trường (`ThongKeXepLoai`) và **biểu đồ cột** GPA tích lũy trung bình theo lớp (`GpaTheoLop`).
  - Cụm **Truy cập nhanh** gồm 5 ô: Sinh viên, Ghi danh, Báo cáo, Nhập/Xuất dữ liệu, Lớp học.
- **A-03 Chi tiết lớp:** thêm hàng 7 thẻ thống kê xếp loại tích lũy của lớp, dùng `ThongKeXepLoai` lọc theo lớp.
- **Cổng sinh viên** (MSSV luôn lấy từ claim):
  - **S-01 `/SinhVien`:** lời chào; 4 thẻ (GPA tích lũy, TC tích lũy, xếp loại, số môn nợ); **biểu đồ đường** GPA theo học kỳ (`GpaHocKy`); bảng môn của học kỳ hiện tại với các điểm thành phần đã có và trạng thái "Chưa có điểm".
  - **S-02 `/SinhVien/KetQua`:** dùng lại partial `_BangDiem`. Mỗi học kỳ là một khối accordion, khối học kỳ mới nhất mở sẵn. Cột "Tính tích lũy ✓" được giữ. Cuối mỗi khối có GPA học kỳ, TC đăng ký, TC đạt; cuối trang có GPA tích lũy, TC tích lũy, xếp loại. Trang có chú thích quy tắc (lần học tốt nhất, F không tính tích lũy). Nút Xuất Excel đặt sẵn và đang tắt.
  - **S-03 `/SinhVien/HoSo`:** thông tin cá nhân và lớp sinh hoạt (chỉ đọc), kèm nút Đổi mật khẩu.

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `Areas/Admin/Controllers/BaoCaoController.cs` + `Views/BaoCao/Index.cshtml` (mới) | A-10: bộ lọc lớp + phạm vi, kiểm tra giá trị hợp lệ |
| `Areas/GiangVien/Controllers/BaoCaoController.cs` + `Views/BaoCao/Index.cshtml` (mới) | G-04: lọc lớp học phần, bộ lọc luôn chứa maGV từ claim |
| `Views/Shared/_BaoCaoHocLuc.cshtml` (mới) | Partial dùng chung A-10/G-04: thẻ, biểu đồ, bảng sắp xếp, modal Xem pipeline |
| `Views/Shared/_TheXepLoai.cshtml` (mới) | Hàng 7 thẻ xếp loại (A-10, G-04, A-03) |
| `ViewModels/BaoCaoViewModels.cs` (mới) | `BaoCaoHocLucViewModel`, `PipelineDaChay`, `CauHinhBieuDo`, `TongQuanAdminViewModel`, … |
| `ViewModels/SinhVienCongViewModels.cs` (mới) | `TongQuanSinhVienViewModel`, `HoSoCaNhanViewModel` |
| `Areas/Admin/Controllers/HomeController.cs` + `Views/Home/Index.cshtml` | A-01 |
| `Areas/Admin/Controllers/LopHocController.cs`, `Views/LopHoc/ChiTiet.cshtml`, `ViewModels/LopHocViewModels.cs` | A-03: thẻ thống kê xếp loại lớp |
| `Areas/SinhVien/Controllers/{Home,KetQua,HoSo}Controller.cs` + Views | S-01, S-02, S-03 |
| `Areas/SinhVien/Controllers/SinhVienControllerBase.cs` | `MaSV` lấy từ claim |
| `Services/BaoCaoService.cs` | Thêm `HocKyCoDuLieuAsync`, `DemDanhMucAsync` (countDocuments 4 collection) |
| `Pipelines/ThongKeDanhMucPipeline.cs`, `Models/Results/ThongKeDanhMucRow.cs` | Pipeline nhỏ `HocKyCoDuLieu` + class `HocKyRow` |
| `Data/XuatPipeline.cs`, `database/pipelines/HocKyCoDuLieu.json` (mới), `database/pipelines/README.md` | Xuất bản JSON của pipeline mới |
| `Models/HangSo.cs`, `Infrastructure/HienThi.cs` | `CacXepLoai` (7 mức theo thứ tự), `MauXepLoai`, `ThoiGian` |
| `wwwroot/js/bieu-do.js` (mới) | Vẽ biểu đồ Chart.js từ cấu hình JSON do server dựng |
| `wwwroot/js/eduscore.js` | Sắp xếp bảng phía client, nút `data-sao-chep` |
| `wwwroot/css/site.css` | Thẻ xếp loại, khung biểu đồ, cột sắp xếp, khối JSON pipeline, ô truy cập nhanh |

Các pipeline tính điểm của GĐ3 **không sửa**: chạy lại `--export-pipelines` thì 10 file cũ không đổi, chỉ có thêm `HocKyCoDuLieu.json`.

## 3. Kết quả tự kiểm tra
| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | 0 lỗi, 0 cảnh báo |
| Kiểm thử GPA | `--kiem-thu-gpa` | 200/200 PASS |
| Các trang trả 200 đúng vai trò | Script Python (cookie + antiforgery): `/Admin`, `/Admin/BaoCao` (4 biến thể), `/Admin/LopHoc/ChiTiet/16DHTH01`, `/GiangVien/BaoCao`, `/SinhVien`, `/SinhVien/KetQua`, `/SinhVien/HoSo` | ✅ |
| Chặn sai vai trò | Admin → `/SinhVien`; GV → `/Admin/BaoCao`; SV → `/Admin/BaoCao`, `/GiangVien/BaoCao` | ✅ đều bị chuyển hướng (302) |
| Tổng thẻ xếp loại A-10 (tích lũy, tất cả lớp) | Cộng 7 thẻ | ✅ 18 + 16 + 29 + 19 + 18 + 0 + 0 = **100**; bảng có 100 dòng |
| Đối chiếu mongosh | Chạy `database/pipelines/ThongKeXepLoai.json` bằng mongosh | ✅ Xuất sắc 18, Giỏi 16, Khá 29, Trung bình 19, Yếu 18, khớp giao diện |
| Lọc lớp 16DHTH01 | A-10 `?maLop=16DHTH01` | ✅ tổng 20; stage đầu `{"$match":{"maLop":"16DHTH01"}}`; TS1: TC 11, GPA 3.27 |
| Phạm vi học kỳ | A-10 `?maLop=16DHTH01&phamVi=2025-2026_2` | ✅ TS1 GPA 3.60, TS4 0.60 (Kém); cột đổi thành "TC đạt"/"Số môn F" |
| Phạm vi HK1 2026-2027 | Chưa có CK | ✅ 100 SV "Chưa xếp loại" |
| Tham số lạ | `?maLop=XYZ&phamVi=abc` | ✅ 200, quay về tất cả lớp + tích lũy |
| Modal Xem pipeline | Parse JSON 2 khối `<pre>` | ✅ cả hai parse được; có `$unwind`, `$group`, `$project`, `$switch`, `$cond`, thời gian ms |
| A-01 | Thẻ 5 lớp / 100 SV / 8 GV / 14 môn; 2 biểu đồ; 5 ô truy cập nhanh | ✅ |
| A-03 | 7 thẻ xếp loại 16DHTH01 | ✅ 1/2/9/5/3/0/0 = 20 |
| G-04 (GV005) | Mọi lớp học phần: 77 SV; lọc `INT301_2026-2027_1`: 56 SV, có TS5 | ✅ |
| G-04 lớp học phần của GV khác | `?lhp=INT101_2025-2026_1` | ✅ bị bỏ qua, `$match` vẫn là `{"monHoc.giangVien.maGV":"GV005"}` |
| S-02 TS1 | Khối HK2 2025-2026, cuối trang | ✅ GPA HK2 **3.60 Xuất sắc**; tích lũy **3.27 Giỏi** |
| S-02 TS3 | Dòng MAT101 | ✅ lần 1 (HK1) có ✓, lần 2 (hè) không ✓ |
| SV xem dữ liệu SV khác | TS1 gọi `/SinhVien/KetQua?maSV=2001250004`, `/SinhVien/HoSo?maSV=2001250004` | ✅ tham số bị bỏ qua, vẫn hiện dữ liệu TS1 |
| S-01 TS1 | Thẻ, biểu đồ, môn học kỳ hiện tại | ✅ GPA 3.27; INT201 "Chưa có điểm" |
| Log ứng dụng | Tìm "fail/exception/error" | ✅ không có |

## 4. Quyết định kỹ thuật mới
- **Thẻ số và biểu đồ** dùng pipeline `ThongKeXepLoai`, không đếm lại trong C#. `DuBayMuc` chỉ xếp kết quả vào đủ 7 mức theo `HangSo.CacXepLoai`; mức không có SV hiện số 0.
- **Biểu đồ:** server dựng `CauHinhBieuDo` (nhãn, số liệu, màu) và serialize thành JSON trong thuộc tính `data-bieu-do` của `<canvas>`. `bieu-do.js` chỉ đọc JSON này rồi vẽ, không tính gì và không dựng HTML. Chart.js nạp từ `wwwroot/lib`.
- **Modal Xem pipeline** hiện cả 2 pipeline thực sự đã chạy để dựng trang, đặt thành 2 tab. JSON lấy từ `KetQuaPipeline.PipelineJson`, tức là đúng chuỗi đã gửi lên server. Số stage được đếm bằng cách parse chính JSON đó.
- **Sắp xếp bảng làm ở client** (GD6 cho phép). JS chỉ đổi thứ tự các `<tr>` đã có. Ô GPA và xếp loại có `data-gia-tri` để sắp theo số; khi không có GPA thì giá trị là -1.
- **A-10:** chỉ nhận `maLop` có thật và học kỳ có trong `HocKyCoDuLieu`; giá trị khác bị đưa về mặc định.
- **G-04** chỉ có phạm vi tích lũy (nghiệp vụ G5 ghi "Báo cáo học lực (tích lũy)"). Bộ lọc luôn có `maGV` lấy từ claim.
- **Pipeline nhỏ mới `HocKyCoDuLieu`** (`$unwind` → `$group` (namHoc, hocKy) → `$sort`) chỉ liệt kê học kỳ, không tính điểm. Có bản JSON đi kèm.

## 5. Điểm cần chú ý / hạn chế
- Seeder **không có SV "Kém" ở phạm vi tích lũy**, vì GPA tích lũy chỉ tính các môn đạt và D = 1.0 (đã ghi ở GĐ1). Muốn thấy thẻ "Kém" khác 0, chọn phạm vi **HK2 2025-2026**, lớp 16DHTH01 (TS4 = 0.60).
- Phạm vi **HK1 2026-2027** cho 100% "Chưa xếp loại" vì điểm CK còn trống. Đây là kết quả đúng, dùng được để minh họa quy tắc "chưa đủ điểm thì không tính GPA".
- Biểu đồ GPA ở S-01 chỉ vẽ các học kỳ đã có GPA; học kỳ hiện tại chưa có CK nên không có điểm trên biểu đồ.
- "Báo cáo cập nhật ngay sau khi nhập điểm": không có cache; mỗi lần tải trang pipeline tính lại từ điểm thành phần, nên chỉ cần F5.
- Mục "Nhập/Xuất dữ liệu" trong Truy cập nhanh và sidebar vẫn trả 404 cho đến GĐ7. Các nút Xuất Excel đang tắt.
- `docs/kiem-thu-gpa.md` đổi 2 dòng (thời điểm chạy) do vừa chạy lại kiểm thử.

## 6. Nghiệm thu
- [ ] Admin → Báo cáo học lực → lọc lớp 16DHTH01 → bảng + biểu đồ đúng; bấm Xem pipeline → thấy `$unwind`, `$group`, `$project`, `$switch`, thời gian ms.
- [ ] Sao chép pipeline → dán vào Compass Aggregations → kết quả trùng với giao diện.
- [ ] Đổi phạm vi sang "HK1 2025-2026" → số liệu thay đổi hợp lý.
- [ ] Nhập 1 điểm làm SV đổi xếp loại (GV) → báo cáo cập nhật ngay.
- [ ] Đăng nhập SV `2001250002 / 22072007` → tổng quan + kết quả học tập hiển thị đẹp, đúng.
- [ ] Dashboard Admin có biểu đồ.
- [ ] Commit.

**Hướng dẫn nghiệm thu chi tiết** (chạy `dotnet run --project DT2_LMS_NHOM6`, mở http://localhost:5207):
1. Đăng nhập `admin / Admin@123`. Trang Tổng quan có 4 thẻ (5 / 100 / 8 / 14), biểu đồ donut xếp loại, biểu đồ cột GPA theo lớp và 5 ô Truy cập nhanh.
2. Mở **Báo cáo học lực**. Ở phạm vi Tích lũy, tất cả lớp, tổng 7 thẻ phải bằng 100. Chọn lớp **16DHTH01**: tổng còn 20; TS1 `2001250001` có GPA 3.27, TC 11. Bấm tiêu đề cột **GPA** hai lần để thấy sắp tăng rồi giảm, cột STT đánh số lại.
3. Bấm **Xem pipeline**. Tab `BaoCaoHocLuc` có stage đầu `{"$match": {"maLop": "16DHTH01"}}`, trong pipeline có `$unwind`, `$group`, `$project`, `$switch`; nhãn trên cùng hiện thời gian ms, số dòng, số stage. Bấm **Sao chép**.
4. Mở Compass → `eduscore.sinhvien` → Aggregations → `</>` Text → dán → xem trước được 20 document, GPA/xếp loại trùng bảng. (Có thể dán tab `ThongKeXepLoai` để so với thẻ số.)
5. Đổi phạm vi sang **HK1 2025-2026**: các thẻ thay đổi, cột đổi thành "TC đạt"/"Số môn F". Ví dụ TS1 có GPA HK1 = 3.00. Ở phạm vi **HK2 2025-2026**, TS4 có GPA 0.60, xếp loại **Kém**.
6. Đăng nhập GV có dạy SV cần đổi điểm, nhập điểm để xếp loại đổi (ví dụ GV005 → INT301 → nhập CK cho TS5). Quay lại báo cáo phạm vi **HK1 2026-2027** của admin rồi F5: TS5 đổi từ "Chưa xếp loại" sang có GPA. **Sau khi thử xong, chạy `--reset` để khôi phục dữ liệu kiểm thử.**
7. Ở cổng GV (GV005), mở **Báo cáo học lực**: chỉ có SV của các lớp học phần INT202/INT301 và không có nút Hồ sơ. Lọc INT301 HK1 2026-2027.
8. Đăng nhập `2001250002 / 22072007` (TS2):
   - Tổng quan có GPA 2.73, TC 11, xếp loại Khá, nợ 0; đường GPA đi qua 1.50 → 2.40 → 3.00; bảng INT202 "Chưa có điểm".
   - Kết quả học tập có khối HK1 2026-2027 mở sẵn. Ở khối hè, INT101 lần 2 có ✓; ở khối HK1 2025-2026, INT101 lần 1 (F) không có ✓.
   - Thêm `?maSV=2001250001` vào URL: dữ liệu vẫn là của TS2.
9. Mở Chi tiết lớp 16DHTH01 (Admin → Lớp học): có hàng 7 thẻ thống kê xếp loại, tổng 20.

## 7. Gợi ý trả lời khi bảo vệ
- **"Thẻ số và biểu đồ lấy dữ liệu từ đâu, có tính trong C# không?"** – Từ pipeline `ThongKeXepLoai`. Pipeline này chèn nguyên `BaoCaoHocLuc` rồi thêm `$group` theo xếp loại, sau đó `$project` + `$switch` để sắp thứ tự. C# chỉ xếp kết quả vào đủ 7 ô; mức không có SV hiện 0.
- **"Modal Xem pipeline có phải pipeline thật đã chạy không?"** – Đúng. `PipelineRunner` serialize chính mảng `BsonDocument[]` vừa gửi tới MongoDB và đo thời gian bằng `Stopwatch`, từ lúc gửi `aggregate` đến khi nhận đủ kết quả. Dán JSON đó vào Compass sẽ ra cùng kết quả.
- **"Sinh viên có xem được điểm người khác không?"** – Không. MSSV lấy từ claim của cookie đăng nhập (`SinhVienControllerBase.MaSV`), các action không có tham số `maSV`, nên `?maSV=` trên URL bị bỏ qua. GV cũng vậy: bộ lọc báo cáo luôn có `monHoc.giangVien.maGV` lấy từ claim.
- **"Vì sao phạm vi học kỳ cho GPA khác tích lũy?"** – Theo mục 3.3: GPA học kỳ tính mọi môn đủ điểm trong kỳ, kể cả F (= 0). GPA tích lũy lấy lần học tốt nhất của mỗi môn và chỉ tính môn đạt. Cùng một pipeline `BaoCaoHocLuc` xử lý cả hai phạm vi bằng tham số `$filter` theo học kỳ và `@dieuKienTinhGpa`.
