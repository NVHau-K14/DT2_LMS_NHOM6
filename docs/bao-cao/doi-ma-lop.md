# Báo cáo bổ sung – Đổi mã lớp sinh hoạt (mã nội bộ / tên hiển thị)

- **Ngày hoàn thành:** 29/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Nhánh:** `doi-ma-lop` (tạo từ `thu-impeccable`)
- **Commit:** `<mã commit>` (điền sau khi người dùng nghiệm thu và commit)

Bảng ánh xạ đã áp dụng:

| maLop cũ | maLop mới | tenLop mới |
|---|---|---|
| 16DHTH01 | CNTT2501 | 16DHTH01 |
| 16DHTH02 | CNTT2502 | 16DHTH02 |
| 16DHATTT01 | ATTT2501 | 16DHATTT01 |
| 16DHHTTT01 | HTTT2501 | 16DHHTTT01 |
| 16DHKHDL01 | KHDL2501 | 16DHKHDL01 |

## 1. Đã làm gì
- **Dữ liệu mẫu:** `maLop` mới, `tenLop` = mã cũ. Niên khóa, ngành, khoa, cố vấn, MSSV, điểm, ghi danh không đổi. Seed lại vẫn ra 640 lần học: thứ tự duyệt lớp giữ nguyên nên chuỗi ngẫu nhiên không đổi.
- **Ràng buộc:** index unique `ux_tenLop`. Form thêm/sửa lớp báo lỗi khi trùng tên lớp (không phân biệt hoa thường) và khi tên lớp trùng mã của một lớp khác.
- **Hiển thị:** `TenLopService` nạp bảng `maLop → tenLop` một lần mỗi request. Mọi màn hình hiện tên lớp. Riêng trang Lớp học (A-02, A-03) có thêm cột/dòng "Mã lớp". Các danh sách lớp, phân trang báo cáo, biểu đồ A-01, bảng nhập điểm và xem trước ghi danh sắp theo tên lớp, nên thứ tự giống hệt trước khi đổi.
- **Excel:** xuất ghi tên lớp, kể cả tên file (`BaoCaoHocLuc_16DHTH01_TichLuy.xlsx`). Cột nhập SV đổi thành "Lớp", nhận tên lớp hoặc mã lớp, file cũ có cột "Mã lớp" vẫn đọc được. Đã sinh lại 4 file `docs/demo/*.xlsx`.
- **API:** tham số/body `maLop` nhận cả tên lớp. Response có thêm `tenLop` cạnh `maLop`.
- **Tài liệu + dữ liệu:** README, thiết kế, thiết kế CSDL, kịch bản demo, README pipeline, CLAUDE.md. Đã chạy `--reset`, `--export-pipelines`, `--tao-file-demo`, `--export`.

## 2. File chính tạo / sửa
| Nhóm | File | Vai trò |
|---|---|---|
| A – Dữ liệu mẫu | `Data/DuLieuMau.cs`, `Data/Seeder.cs` | Bảng lớp mới (`LopMau(MaLop, TenLop, ...)`), `LopBatBuocINT301`, `LopKiemThu = "CNTT2501"` |
| | `Data/TaoFileDemo.cs`, `Data/XuatPipeline.cs`, `Pipelines/ThamSoPipeline.cs`, `Views/Shared/_XacNhanXoa.cshtml`, `GhiDanhController.cs`, 2 controller API | Ví dụ / comment / Swagger |
| B – Ràng buộc | `Data/DbInitializer.cs` | Index `ux_tenLop` |
| | `Services/LopHocService.cs` | `KiemTraTenLopAsync`, bắt DuplicateKey theo tên index, sắp theo `tenLop` |
| C – Hiển thị | `Services/TenLopService.cs` (mới), `Program.cs`, 4 `_ViewImports.cshtml` | `TenLopService` + `BangTenLop` (`Ten`, `TimMaLop`, `SapXep`), `@inject TenLop` |
| | `ViewModels/BaoCaoViewModels.cs`, 2 `BaoCaoController`, `Admin/HomeController.cs` | Phân trang theo lớp và biểu đồ GPA theo lớp sắp/ghi nhãn theo tên lớp |
| | `Services/DiemService.cs`, `Services/GhiDanhService.cs`, `Services/GiangVienService.cs` | Sắp lại kết quả pipeline theo tên lớp; lý do chặn xóa GV hiện tên lớp |
| | `_BaoCaoHocLuc`, `_XemTruocNhapDiem`, `Admin/{SinhVien,GhiDanh,BaoCao,DuLieu,LopHoc}/*`, `GiangVien/{Home,LopHocPhan,NhapDiem}`, `SinhVien/{Home,HoSo}` | Chữ hiển thị = tên lớp |
| D – Excel | `Services/ExcelService.cs`, `XuatExcelService.cs`, `NhapExcelService.cs`, `ViewModels/NhapExcelViewModels.cs`, `Admin/Views/DuLieu/XemTruocSinhVien.cshtml` | Cột Lớp ghi/đọc tên lớp, quy đổi về maLop |
| E – API | `ViewModels/ApiModels.cs`, `Models/Results/BaoCaoHocLucRow.cs` (`[BsonIgnore] TenLop`), `SinhVienApiController.cs`, `BaoCaoApiController.cs` | `tenLop` trong response, `maLop` nhận cả tên lớp |
| F – Tài liệu / dữ liệu | `README.md`, `docs/thiet-ke.md`, `docs/thiet-ke-csdl.md`, `docs/kich-ban-demo.md`, `database/pipelines/README.md`, `CLAUDE.md` | Quy ước "maLop nội bộ, tenLop hiển thị", bảng ánh xạ, ví dụ ObjectId mới |
| | `database/pipelines/BaoCaoHocLuc_HocKy.json`, `database/export/*.json`, `database/dump/eduscore.archive.gz`, `docs/demo/*.xlsx`, `docs/kiem-thu-gpa.md` | Sinh lại bằng lệnh dòng lệnh |
| | `docs/ke-hoach-doi-ma-lop.md` | Kế hoạch đã duyệt (chuyển từ gốc repo) – xóa được nếu không cần |

**Không sửa:** pipeline tính điểm/GPA (`Pipelines/*Pipeline.cs`), phân quyền, giao diện ngoài chữ hiển thị lớp, `docs/bao-cao/GD*.md`, `docs/quy-trinh/GD*.md`.

## 3. Kết quả tự kiểm tra
| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | Dừng app, `dotnet build DT2_LMS_NHOM6.sln` sau mỗi nhóm A→E | ✅ 0 lỗi, 0 cảnh báo |
| Kiểm thử GPA | `--reset --kiem-thu-gpa` và chạy lại sau khi khôi phục DB | ✅ 200/200 PASS (cả hai lần) |
| Dữ liệu lớp | `db.lophoc.find({},{maLop:1,tenLop:1,_id:0})` | ✅ đúng bảng ánh xạ; index `ux_maLop`, `ux_tenLop` |
| Không còn mã cũ | `db.sinhvien.distinct('maLop')`; đếm `maLop ∈ mã cũ` trong `sinhvien`, `lophoc` | ✅ chỉ 5 mã mới, 20 SV/lớp; 0 document mang mã cũ. TS1–TS5 thuộc `CNTT2501` |
| Duyệt 3 vai trò | Script Python đăng nhập admin, GV005, 2001250005, đi theo mọi link trong Area (Admin 400 trang, GV 8, SV 4). Bỏ thẻ/thuộc tính, chỉ xét chữ hiển thị | ✅ 0 trang lỗi; mã mới chỉ hiện ở `/Admin/LopHoc` và `/Admin/LopHoc/ChiTiet/...` |
| Ô lọc lớp | A-04, A-09, A-10, A-11, G-03 | ✅ `value` = maLop, chữ = tenLop; G-03 `data-lop` = maLop |
| Thứ tự lớp | A-10 trang 1→5; biểu đồ A-01; bảng G-03 INT301 | ✅ 16DHATTT01, 16DHHTTT01, 16DHKHDL01, 16DHTH01, 16DHTH02 (như cũ); G-03: 16DHKHDL01, 16DHTH01, 16DHTH02 |
| Form lớp | Thêm tên `16dhth01` / tên `CNTT2502`; sửa CNTT2501 thành `16DHTH02`; thêm CNTT2503/16DHTH03 rồi xóa | ✅ 3 lỗi thân thiện ở ô Tên lớp; thêm/xóa thành công |
| Excel xuất | A-04 lớp CNTT2501, A-10 lớp CNTT2501, bảng điểm cá nhân TS5 | ✅ cột/dòng Lớp = 16DHTH01; tên file `DanhSachSinhVien_16DHTH01.xlsx`, `BaoCaoHocLuc_16DHTH01_TichLuy.xlsx` |
| File demo | Đọc lại 4 file `docs/demo` | ✅ cột "Lớp" ghi tên lớp; sheet hướng dẫn liệt kê tên lớp; thứ tự dòng bảng điểm như cũ |
| Nhập `sinhvien_loi.xlsx` | A-11 | ✅ đủ các lỗi cũ: MSSV đã tồn tại, **Lớp 16DHXX99 không tồn tại**, ngày sinh 31/02, trùng trong file, MSSV 9 số, giới tính, email, SĐT |
| Nhập `diem_INT301_loi.xlsx` | A-11 | ✅ 11, "abc", 8.25, MSSV không thuộc lớp học phần |
| Nhập file hợp lệ | `diem_INT301_hop_le.xlsx` → xác nhận; `sinhvien_hop_le.xlsx` → xác nhận | ✅ mongosh: 56 SV có CK, `capNhatBoi: "admin"` 56; 5 SV mới `maLop: "KHDL2501"` + 5 tài khoản; xem trước hiện 16DHKHDL01 |
| Kịch bản demo 1–3 | GV005 lưu CK `8,5` cho 2001250005 → admin A-10 lọc CNTT2501 | ✅ tiến độ 0/56; "TK 8.8 (A)"; GPA 3.60 → **3.69**; lệnh toast chứa maLop thật, không chứa tên lớp; file hợp lệ đổi CK 8.5 → 4 như kịch bản ghi |
| API | `/api/sinhvien?maLop=16DHTH01` và `?maLop=cntt2501`; `/api/baocao/hocluc?maLop=16DHTH01`, `?maLop=KHONGCO`; `/api/sinhvien/2001250005/bangdiem` | ✅ 20 SV, có `maLop` + `tenLop`; lớp lạ → 200 rỗng (như cũ) |
| grep mã cũ | `grep -rnE "16DH(TH0[12]\|ATTT01\|HTTT01\|KHDL01)"` (trừ bin/obj/.git, báo cáo/quy trình cũ) | ✅ chỉ còn trong tenLop, chữ hiển thị, comment, tài liệu, dữ liệu file demo |
| Trạng thái DB cuối | Sau khi test đã ghi dữ liệu → `mongorestore --drop` từ `database/dump/eduscore.archive.gz` | ✅ 100 SV, TS5 CK = null, ObjectId khớp `database/export` và ví dụ trong `thiet-ke-csdl.md` |

Ghi chú: script kiểm tra có một lần báo sai ở bước "file điểm hợp lệ kiểm tra sạch". Nguyên nhân là chuỗi `dong-bi-chan` luôn có trong chú thích màu của trang xem trước. Kết quả trong DB (56/56 CK) xác nhận chức năng đúng.

## 4. Quyết định kỹ thuật mới
- **Tra tên lớp ở C#, không sửa pipeline.** Pipeline vẫn lọc/trả `maLop`. Tra `maLop → tenLop` chỉ phục vụ hiển thị, không phải tính điểm, nên không vi phạm quy tắc "mọi phép tính điểm trong pipeline".
- **`TenLopService` scoped + `@inject` trong `_ViewImports`.** View lấy bảng bằng `var bangLop = await TenLop.LayAsync();`. Mỗi request chỉ có một truy vấn `lophoc` (5 document). Kế hoạch ban đầu có nêu thêm TagHelper `<ten-lop>`, nhưng tôi bỏ: dùng một cách cho mọi chỗ (ô, option, data-tim) dễ giải thích hơn.
- **Sắp xếp ổn định theo tên lớp** (`BangTenLop.SapXep`, dùng OrderBy) đặt sau pipeline. Thứ tự MSSV trong từng lớp giữ nguyên.
- **Tên lớp không được trùng mã của lớp khác** (và ngược lại), vì Excel/API nhận cả hai. Thiếu ràng buộc này thì "CNTT2502" có thể chỉ hai lớp khác nhau.
- **Toast giữ lệnh MongoDB thật** (`maLop: "CNTT2501"`). Thông báo chữ ở trang Lớp học ghi "Đã thêm lớp 16DHTH03 (mã CNTT2503)".
- **API lớp không tồn tại** vẫn trả 200 + danh sách rỗng như trước, không đổi sang 404.

## 5. Điểm cần chú ý / hạn chế
- URL vẫn chứa mã mới (`/Admin/BaoCao?maLop=CNTT2501`, `/Admin/LopHoc/ChiTiet/CNTT2501`), vì `value` ô chọn và route dùng maLop như yêu cầu. Chữ trên trang thì không có mã mới.
- Modal "Xem pipeline" (A-10 lọc lớp) và toast lệnh MongoDB hiện `maLop: "CNTT2501"`. Đây là lệnh thật, đúng yêu cầu.
- `GET /api/sinhvien/{maSV}` trả nguyên document `sinhvien` nên không có `tenLop`. `tenLop` có ở danh sách, `/bangdiem` và `/api/baocao/hocluc`.
- `_id`/`maDK` sinh mới sau `--reset`. Ví dụ ObjectId trong `thiet-ke-csdl.md` đã cập nhật theo bản export hiện tại.
- File `docs/demo/*.xlsx` có "Ngày xuất" nên nhị phân khác mỗi lần sinh.
- Sau khi tự kiểm tra, DB đã được khôi phục từ dump (không `--reset` lại) để ID khớp bản export.

## 6. Nghiệm thu
- [ ] `dotnet build` 0 lỗi; `--kiem-thu-gpa` 200/200 PASS
- [ ] `db.lophoc.find({},{maLop:1,tenLop:1,_id:0})` đúng bảng ánh xạ; `db.sinhvien.distinct('maLop')` chỉ 5 mã mới
- [ ] Đăng nhập admin, GV005, một SV: mọi nơi hiện 16DHTH01…, chỉ trang Lớp học có cột mã mới
- [ ] Lọc lớp + phân trang báo cáo (A-10, G-04), lọc lớp + tiến độ ở nhập điểm (G-03), ghi danh cả lớp (A-09)
- [ ] Thêm/sửa lớp trùng tên lớp → báo lỗi thân thiện
- [ ] Nhập `sinhvien_hop_le.xlsx`, `diem_INT301_hop_le.xlsx` thành công; file `_loi` báo đúng lỗi; file xuất ghi tên lớp
- [ ] Kịch bản `docs/kich-ban-demo.md` phần 1–3: GPA 3.60 → 3.69 của 2001250005, 56 SV INT301
- [ ] Đọc lại `docs/thiet-ke.md` mục 4.2 (quy ước) và câu mới ở phần Mở đầu kịch bản demo

## 7. Gợi ý trả lời khi bảo vệ
- **Vì sao tách mã lớp và tên lớp?** *`maLop` là khóa ổn định để `sinhvien` tham chiếu và để pipeline lọc. `tenLop` là tên người dùng quen gọi. Đổi tên hiển thị chỉ sửa một document `lophoc`, không phải `updateMany` 20 sinh viên.*
- **Tra tên lớp bằng C# có trái quy tắc "tính toán trong pipeline" không?** *Không. Quy tắc áp cho điểm TK, GPA, xếp loại. Tra tên chỉ là hiển thị: nạp 5 document `lophoc` một lần mỗi request rồi tra từ điển. Pipeline không đổi một stage nào, `--kiem-thu-gpa` vẫn 200/200.*
- **Sao không dùng `$lookup` sang `lophoc` trong pipeline?** *Làm được nhưng phải sửa mọi pipeline báo cáo, thêm một stage chạy trên từng dòng. Với 5 lớp, tra ở tầng ứng dụng đơn giản hơn và giữ pipeline điểm nguyên vẹn đã kiểm thử.*
- **Làm sao chống trùng tên lớp?** *Index unique `ux_tenLop` ở tầng DB. Form kiểm tra trước để báo lỗi tiếng Việt. Nếu hai người bấm cùng lúc, DuplicateKey của index vẫn chặn và được dịch thành thông báo thân thiện.*
