# Kế hoạch đổi mã lớp sinh hoạt

Nhánh: `doi-ma-lop` (tạo từ `thu-impeccable`, working tree sạch). Chưa sửa mã nguồn, chưa commit.

## Bảng ánh xạ

| maLop cũ | maLop mới | tenLop mới |
|---|---|---|
| 16DHTH01 | CNTT2501 | 16DHTH01 |
| 16DHTH02 | CNTT2502 | 16DHTH02 |
| 16DHATTT01 | ATTT2501 | 16DHATTT01 |
| 16DHHTTT01 | HTTT2501 | 16DHHTTT01 |
| 16DHKHDL01 | KHDL2501 | 16DHKHDL01 |

Quy ước: **maLop là mã nội bộ, tenLop là tên hiển thị**. Người dùng chỉ thấy 16DHTH01… ở mọi nơi, trừ trang Lớp học (cột "Mã lớp" hiện mã mới).

## Nhóm A – Dữ liệu mẫu và ví dụ

| File | Thay đổi |
|---|---|
| `Data/DuLieuMau.cs` | `LopMau` thêm `TenLop`; `LopBatBuocINT301 = {CNTT2501, CNTT2502}` |
| `Data/Seeder.cs` | Ghi `tenLop` = mã cũ; SV kiểm thử `lop = "CNTT2501"`; sửa comment |
| `Data/TaoFileDemo.cs` | Cột Lớp trong file mẫu ghi tenLop (16DHKHDL01, 16DHTH01…); các dòng lỗi giữ nguyên |
| `Data/XuatPipeline.cs`, `Pipelines/ThamSoPipeline.cs` | Ví dụ `MaLopMau = "CNTT2501"`, sửa comment |
| `Controllers/Api/SinhVienApiController.cs`, `BaoCaoApiController.cs`, `Areas/Admin/Controllers/GhiDanhController.cs` | Ví dụ trong `///` |
| `Views/Shared/_XacNhanXoa.cshtml` | Chữ ví dụ |
| `Services/ExcelService.cs` (dòng 325) | Dòng ví dụ sheet hướng dẫn giữ "16DHTH01" (tenLop) |

## Nhóm B – Ràng buộc

- `DbInitializer`: thêm index unique `ux_lophoc_tenLop`.
- `LopHocService.ThemAsync/SuaAsync`: kiểm tra trùng tên lớp (khi sửa bỏ qua chính lớp đó), lỗi gắn vào `Form.TenLop`, thông báo thân thiện giống lỗi trùng mã lớp.

## Nhóm C – Hiển thị

**Cách làm:** service scoped `TenLopService` (`Services/`): lần gọi đầu mỗi request đọc `db.lophoc.find({}, {maLop:1, tenLop:1})` một lần (5 document) và giữ trong field. Cung cấp `Ten(maLop)` (không thấy thì trả lại maLop) và hàm sắp xếp theo tenLop.

- Trong view: TagHelper `<ten-lop ma="@sv.MaLop" />` (cùng kiểu `<diem-chu>`, `<xep-loai>`).
- Chỗ cần chuỗi (`<option>`, nhãn biểu đồ, Excel, sắp xếp): controller/service lấy bảng tên rồi gán vào ViewModel.
- **Không sửa pipeline tính điểm/GPA.** Pipeline vẫn trả `maLop`; các pipeline đang `$sort` theo maLop (`DanhSachNhapDiem`, `GpaTheoLop`, `cacLop` của `LopHocPhanCuaGV`) được sắp lại bằng C# theo tenLop (OrderBy ổn định nên thứ tự MSSV trong lớp giữ nguyên).

| Màn hình | File |
|---|---|
| A-02 Lớp học (trang duy nhất hiện mã mới) | `LopHocService` đổi `SortBy(TenLop)`; `LopHoc/Index` giữ 2 cột; placeholder form `CNTT2503` / `16DHTH03`; tiêu đề `ChiTiet`/Sửa/Xóa hiện tenLop |
| A-04 DS SV + lọc | `SinhVien/Index.cshtml`, `Form.cshtml`, `HoSo.cshtml` |
| S-01, S-03 | `SinhVien/Views/Home/Index`, `HoSo/Index` |
| A-09 Ghi danh | `GhiDanh/Index.cshtml`, `GhiDanhService` (sắp theo tenLop) |
| G-01/02/03 | `GiangVien/Views/Home`, `LopHocPhan`, `NhapDiem/Index` (option `value`=maLop, chữ=tenLop, `data-lop` giữ maLop), `_XemTruocNhapDiem` |
| A-10/G-04 | `_BaoCaoHocLuc` (cột Lớp, nhãn "Lớp …"), `PhanTrangTheoLop` (sắp theo tenLop), `BaoCao/Index`, 2 `BaoCaoController` |
| A-01 | `TongQuanAdminViewModel.BieuDoGpaLop`: nhãn tenLop, sắp theo tenLop |
| A-03 | Dùng partial chung nên tự đúng; tiêu đề hiện tenLop |
| A-11 | `DuLieu/Index`, `XemTruocSinhVien` |

## Nhóm D – Excel

- **Xuất** (`ExcelService`, `XuatExcelService`, `BaoCaoController`): cột Lớp ghi tenLop; dòng "Lớp:" chỉ ghi tenLop; tên file giữ dạng cũ theo tenLop.
- **Nhập SV** (`NhapExcelService`): cột Lớp nhận tenLop, nhận cả maLop mới, quy đổi về maLop trước khi ghi. Lớp không tồn tại → báo lỗi dòng như cũ. Sheet hướng dẫn liệt kê tenLop.
- **Nhập điểm:** hiện không đọc cột Lớp (lấy từ DB) → chỉ đổi hiển thị ở trang xem trước.
- Chạy `--reset --tao-file-demo`; các file `*_loi.xlsx` phải giữ đúng các lỗi cũ.

## Nhóm E – API

- Thêm `TenLop` cạnh `MaLop` ở `SinhVienTomTatApi`, `BangDiemApi`, `BaoCaoHocLucApi`; dòng `BaoCaoHocLucRow` thêm `[BsonIgnore] TenLop` gán bằng C#. Không đổi tên trường cũ.
- Tham số query `maLop` giữ tên; đề xuất nhận cả tenLop.

## Nhóm F – Tài liệu và dữ liệu

- Cập nhật: `README.md`, `docs/thiet-ke.md` (dữ liệu mẫu + ghi chú quy ước), `docs/thiet-ke-csdl.md`, `docs/kich-ban-demo.md` (Q1–Q5 dùng maLop mới nếu lọc theo lớp; lời nói vẫn "lớp 16DHTH01"), `database/pipelines/README.md` + `BaoCaoHocLuc_HocKy.json` (qua `--export-pipelines`), mục "Ghi chú kỹ thuật" trong `CLAUDE.md`, `docs/kiem-thu-gpa.md` (sinh lại).
- **Không sửa** `docs/bao-cao/GD*.md`, `docs/quy-trinh/GD*.md`.
- Dữ liệu: `--reset`, rồi `--export` (5 JSON + dump).

## Điểm cần xác nhận

1. Đổi tiêu đề cột file mẫu nhập SV từ "Mã lớp" → "Lớp" (vẫn đọc được "Mã lớp")? *Đề xuất: đổi.*
2. Tham số `maLop` của API nhận cả tenLop? *Đề xuất: có.*
3. S-03 và hồ sơ Admin: gộp "Mã lớp" + "Tên lớp" thành một dòng "Lớp" hiện 16DHTH01?
4. Tên file xuất báo cáo theo lớp dùng tenLop (như cũ)?

## Tự kiểm tra sau khi làm

1. Dừng app, `dotnet build DT2_LMS_NHOM6.sln` 0 lỗi; `--kiem-thu-gpa` 200/200 PASS.
2. mongosh: `db.lophoc.find(...)` đúng bảng ánh xạ; `db.sinhvien.distinct('maLop')` chỉ còn 5 mã mới; không còn mã cũ trong `maLop` của collection nào.
3. Đăng nhập admin, GV005, một SV: đi hết menu, mọi nơi hiện 16DHTH01…, chỉ trang Lớp học có cột mã mới; thử lọc lớp, phân trang báo cáo, nhập điểm, ghi danh, thêm/sửa lớp trùng tên.
4. Nhập `sinhvien_hop_le.xlsx`, `diem_INT301_hop_le.xlsx` thành công; các file `_loi` báo đúng lỗi; xuất Excel hiện tenLop.
5. Kịch bản `docs/kich-ban-demo.md` phần 1–3: số liệu (GPA 3.60 → 3.69 của 2001250005, 56 SV INT301…) vẫn đúng.
6. `grep` mã cũ: chỉ còn trong tenLop, chữ hiển thị, báo cáo/quy trình lịch sử.
7. Không commit.
