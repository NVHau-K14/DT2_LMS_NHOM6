# Báo cáo Giai đoạn 4 – CRUD Admin: lớp, sinh viên, giảng viên, môn học

- **Ngày hoàn thành:** 28/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Commit:** `<mã commit>` (điền sau khi người dùng nghiệm thu và commit)

## 1. Đã làm gì
- **Lớp học (A-02, A-03):** bảng gồm mã, tên, niên khóa, ngành, CVHT và sĩ số. Sĩ số đếm từ `sinhvien` bằng pipeline `SiSoTheoLop`. Có lọc theo ngành, niên khóa và tìm theo mã/tên. Thêm và sửa bằng modal (CVHT chọn từ danh sách GV, mã lớp bị khóa khi sửa). Xóa lớp còn SV sẽ bị chặn kèm số SV. Trang chi tiết lớp hiển thị GPA tích lũy, xếp loại, TC tích lũy và số môn nợ của từng SV, lấy từ `BaoCaoHocLuc` lọc theo lớp. Đã đặt sẵn nút "Ghi danh cả lớp" (`/Admin/GhiDanh?maLop=`, trang làm ở GĐ5) và "Xuất báo cáo lớp" (đang tắt, làm ở GĐ7).
- **Sinh viên (A-04, A-05, A-06):** danh sách lọc được theo lớp và trạng thái. Ô tìm theo MSSV hoặc họ tên dùng regex không phân biệt hoa thường, ký tự đặc biệt được escape. Danh sách **phân trang phía server**, 20 dòng/trang (`countDocuments` + `skip/limit`, bỏ mảng `monHoc` bằng projection). GPA và xếp loại chỉ được tính cho đúng các SV của trang đang xem (`BaoCaoHocLuc` với `maSV: {$in: [...]}`). Form thêm/sửa có validation đầy đủ. Thêm SV sẽ insert `monHoc: []` rồi tạo tài khoản; nếu tạo tài khoản lỗi thì xóa SV vừa thêm. Khi sửa, MSSV lấy từ route nên không đổi được; đổi họ tên thì cập nhật luôn `taikhoan.hoTen`. Xóa SV xóa cả tài khoản. Trang hồ sơ có thẻ thông tin, thẻ tài khoản, 4 thẻ tóm tắt và bảng điểm theo học kỳ (partial **`_BangDiem`** dùng chung, S-02 sẽ dùng lại).
- **Giảng viên (A-07):** bảng có cột số lớp học phần ở học kỳ hiện tại, lấy từ pipeline `LopHocPhanCuaGV`. Thêm GV sẽ tạo tài khoản với mật khẩu 123456. **Đổi họ tên hoặc học vị sẽ lan truyền tên mới sang 3 nơi:** `sinhvien.monHoc.$[e].giangVien.hoTen` (updateMany + arrayFilters), `lophoc.coVanHocTap.hoTen` và `taikhoan.hoTen`. Toast hiển thị cả 3 lệnh kèm matched/modified. Xóa GV bị chặn nếu GV còn lần học được phân công hoặc đang làm CVHT.
- **Môn học (A-08):** modal kiểm tra tổng trọng số bằng 100 và TC trong khoảng 1–10, kèm ghi chú "tín chỉ / trọng số chỉ áp dụng cho ghi danh mới". Khi gõ tên mới, modal cảnh báo ngay "Đổi tên sẽ cập nhật tên cho N lần học". Lưu xong, tên được lan truyền sang `monHoc.$[e].tenMon` và toast hiện lệnh đã chạy. Môn đã có người học thì không xóa được.
- **A9:** reset mật khẩu về mặc định (SV: ngày sinh `ddMMyyyy`, GV: `123456`) và khóa/mở khóa tài khoản, thao tác từ menu ⋮ của SV và GV, cũng như từ trang hồ sơ SV.
- **`Infrastructure/MongoCommandFormatter`:** dựng chuỗi lệnh dạng mongosh (`db.x.updateMany({...}, {...}, { arrayFilters: [...] })` + `// matchedCount, modifiedCount`) từ **chính các BsonDocument** đã dùng để chạy lệnh. GĐ5 sẽ dùng tiếp cho ghi danh và nhập điểm.

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `Infrastructure/MongoCommandFormatter.cs` | In BsonValue theo cú pháp mongosh (ObjectId(), ISODate(), khóa trần), ghép nhiều lệnh |
| `Pipelines/ThongKeDanhMucPipeline.cs` + `database/pipelines/SiSoTheoLop.json`, `SoLanHocTheoMon.json` | Đếm sĩ số theo lớp; số lần học, số SV theo môn |
| `Services/KetQuaGhi.cs` | Kết quả thao tác ghi gồm thông báo, ô bị lỗi và lệnh MongoDB |
| `Services/TaiKhoanService.cs` | Tạo/xóa tài khoản đi kèm, reset mật khẩu, khóa/mở khóa, đồng bộ họ tên |
| `Services/LopHocService.cs`, `SinhVienService.cs`, `GiangVienService.cs`, `MonHocService.cs` | Nghiệp vụ CRUD, lan truyền tên, chặn xóa, hoàn tác khi ghi 2 collection |
| `Areas/Admin/Controllers/AdminControllerBase.cs` | Thêm helper `ThongBaoKetQua`, `GanLoi`, `QuayLai` (chỉ URL nội bộ) |
| `Areas/Admin/Controllers/{LopHoc,SinhVien,GiangVien,MonHoc}Controller.cs` + `Views/...` | Màn hình A-02 → A-08 |
| `ViewModels/*ViewModels.cs`, `PhanTrangViewModel.cs`, `BangDiemViewModel.cs` | Form có validation, dữ liệu trang, phân trang, bảng điểm |
| `Views/Shared/_BangDiem.cshtml`, `_PhanTrang.cshtml` | Partial dùng chung (bảng điểm theo học kỳ, phân trang) |
| `Views/Shared/_XacNhanXoa.cshtml`, `wwwroot/js/eduscore.js` | Hộp xác nhận tùy biến được tiêu đề/nút; JS mở modal sửa (điền giá trị từ `data-gia-tri`) |
| `wwwroot/css/site.css` | Style bảng, thanh lọc, phân trang, thẻ số, bảng điểm |

## 3. Kết quả tự kiểm tra
| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | 0 lỗi, 0 cảnh báo |
| Các trang | Đăng nhập admin, gọi 11 trang (danh sách, chi tiết, form, hồ sơ, lọc, tìm) | Tất cả 200; mã không tồn tại → 404 |
| Phân trang | `/Admin/SinhVien?page=5` | 20 dòng, "Hiển thị 81–100 / 100" |
| Tìm kiếm | `q=TRẦN` (chữ hoa) → 7 SV; `q=.*(` → trống, không lỗi regex | ✅ |
| Lớp | Tạo TEST02 → sửa (tiếng Việt lưu đúng) → xóa khi còn SV bị chặn → xóa SV → xóa lớp | ✅ |
| SV | Validation (MSSV, họ tên, email, ngày sinh 2030, lớp không tồn tại, MSSV trùng) → tạo 2001259999 → đăng nhập bằng `14032007` → sửa họ tên (taikhoan đổi theo) → đổi MK → reset → khóa (đăng nhập bị báo khóa) → mở → xóa → tài khoản mất, không đăng nhập được | ✅ |
| Đổi tên INT301 | mongosh trước/sau: 56 phần tử `monHoc.maMon:"INT301"` đổi tên hết; toast có lệnh `updateMany ... arrayFilters`, matched/modified 56; đã đổi lại tên cũ | ✅ |
| Đổi học vị GV005 | ThS. → TS.: 97 lần học (77 SV), lớp 16DHATTT01 (CVHT), taikhoan đều đổi; toast 3 lệnh; đã đổi lại ThS. | ✅ |
| Xóa bị chặn | 16DHTH01 (còn 20 SV), GV001 (đang dạy + CVHT 16DHTH01), INT101 (64 SV đã học) | ✅ báo rõ lý do |
| GV / Môn mới | Thêm GV099 → đăng nhập `123456` → reset → khóa → xóa; thêm/xóa môn TST101; trọng số 10/30/50 bị báo "tổng = 90" | ✅ |
| Hồ sơ 2001250002 | Bảng điểm theo học kỳ; INT101 lần 1 F (không ✓), lần 2 B (✓); GPA 2.73 Khá, TC 11, nợ 0 | ✅ |
| Pipeline | `--export-pipelines` (10 file), `--kiem-thu-gpa` | 200/200 PASS (dữ liệu mẫu còn nguyên) |

## 4. Quyết định kỹ thuật mới
- **Service trả `KetQuaGhi`** thay vì ném exception cho lỗi nghiệp vụ. Controller chỉ việc hiện toast hoặc gắn lỗi vào đúng ô nhập.
- **Modal thêm/sửa dùng chung:** nút có `data-mo-modal` + `data-gia-tri` (JSON của chính form ViewModel). JS chỉ điền giá trị vào ô có sẵn, không dựng HTML. Nếu POST lỗi thì server render lại trang và mở sẵn modal kèm lỗi validation (luôn có validation phía server).
- **MongoCommandFormatter tự định dạng** vì driver 3.x đã bỏ chế độ JSON "Shell". Cách này in ra đúng cú pháp dán được vào mongosh.
- **Hai pipeline đếm nhỏ** (`SiSoTheoLop`, `SoLanHocTheoMon`) viết bằng JSON như các pipeline khác và có bản `.json`, để tuân thủ quy ước "pipeline có bản JSON".
- **"GV đang được phân công dạy"** = còn bất kỳ lần học nào (mọi học kỳ) có `giangVien.maGV` của GV đó. Xóa GV trong trường hợp này sẽ làm lịch sử điểm trỏ tới một GV không còn tồn tại.
- **Ngày sinh hợp lệ:** từ năm 1950 trở đi và đủ 15 tuổi. Thiết kế không quy định cụ thể nên chọn cách đơn giản.
- **Mã lớp/GV/môn:** 3–20 chữ hoặc số, tự đổi sang chữ hoa khi lưu.
- **Không có transaction:** tạo/xóa SV và GV ghi tuần tự 2 collection; bước 2 lỗi thì hoàn tác bước 1 (xóa document vừa thêm, hoặc chèn lại document vừa xóa). Lan truyền tên gồm nhiều lệnh idempotent, nếu lỗi giữa chừng thì chỉ cần bấm lưu lại.

## 5. Điểm cần chú ý / hạn chế
- Các nút **Nhập Excel / Tải mẫu / Xuất Excel / Xuất báo cáo lớp / Ghi danh môn / Xuất bảng điểm** đang hiện ở trạng thái tắt (GĐ5, GĐ7). Nút "Ghi danh cả lớp" đã trỏ tới `/Admin/GhiDanh?maLop=`, hiện trang này còn 404 cho đến GĐ5.
- Cột "Lớp HP" ở trang Giảng viên tính theo **học kỳ hiện tại** (HK1 2026-2027). Vì vậy chỉ GV002, GV005 và GV007 có số khác 0, đúng với dữ liệu mẫu.
- Bảng điểm xếp học kỳ **mới nhất ở trên** và chỉ mở sẵn khối đó; các học kỳ cũ cần bấm để mở ra (đúng mô tả S-02).
- Khóa tài khoản đang đăng nhập không đẩy phiên hiện tại ra (quyết định từ GĐ2); lần đăng nhập sau mới bị chặn.
- GV tự đổi tên thì cookie của GV vẫn giữ tên cũ cho đến lần đăng nhập sau.
- `docs/kiem-thu-gpa.md` thay đổi vì đã chạy lại kiểm thử sau GĐ4; nội dung không đổi, chỉ khác thời điểm chạy.

## 6. Nghiệm thu
Chạy `dotnet run --project DT2_LMS_NHOM6`, đăng nhập `admin / Admin@123`.
- [ ] Thêm/sửa/xóa lớp, sinh viên, giảng viên, môn học trên giao diện đều chạy; thông báo dễ hiểu.
  - Lớp học → "Thêm lớp" → nhập `TEST01` → Lưu → ⋮ Sửa (mã lớp bị khóa) → ⋮ Xóa.
  - Sinh viên → "Thêm sinh viên" vào lớp TEST01 → đăng xuất, đăng nhập bằng MSSV + ngày sinh `ddMMyyyy` → quay lại admin, thử ⋮ Reset mật khẩu / Khóa → ⋮ Xóa → xóa lớp TEST01.
  - Giảng viên / Môn học: thêm, sửa, xóa một bản ghi mới (môn thử nhập trọng số 10/30/50 để thấy lỗi "tổng phải bằng 100").
- [ ] Hồ sơ SV `2001250002` (`/Admin/SinhVien/HoSo/2001250002`): bảng điểm theo học kỳ; mở "HK1 2025-2026" thấy INT101 lần 1 F không ✓; "HK hè 2025-2026" INT101 lần 2 B có ✓; thẻ GPA tích lũy **2.73**, xếp loại **Khá**.
- [ ] Môn học → ⋮ Sửa INT301 → sửa tên (modal cảnh báo "cập nhật tên cho 56 lần học") → Lưu → toast có mục "Lệnh MongoDB đã chạy" chứa `updateMany ... arrayFilters` → Compass: `sinhvien`, lọc `{"monHoc.maMon":"INT301"}`, thấy tên mới trong mảng `monHoc`. **Đổi lại** tên "Cơ sở dữ liệu NoSQL".
- [ ] (Tùy chọn) Giảng viên → ⋮ Sửa GV005 → học vị TS. → toast 3 lệnh → kiểm tra Compass → đổi lại ThS.
- [ ] Các thao tác xóa bị chặn đúng chỗ: lớp 16DHTH01, GV001, môn INT101 → toast đỏ nêu lý do.
- [ ] Commit.

Nếu đã sửa thử dữ liệu mẫu và muốn trở về nguyên bản: `dotnet run --project DT2_LMS_NHOM6 -- --reset`.

## 7. Gợi ý trả lời khi bảo vệ
- **Vì sao đổi tên môn phải "lan truyền"? Sao không chỉ lưu mã môn?** Vì `sinhvien` nhúng bản sao `tenMon` và `giangVien.hoTen` để đọc hồ sơ/bảng điểm bằng một truy vấn, không cần `$lookup`. Đây là đánh đổi của Document Store: đọc nhanh, nhưng khi đổi tên phải cập nhật bản sao. Lệnh `updateMany({"monHoc.maMon": ma}, {$set: {"monHoc.$[e].tenMon": ten}}, {arrayFilters: [{"e.maMon": ma}]})` dùng `$[e]` để sửa **mọi** phần tử khớp trong mảng (kể cả lần học lại), khác với `$` chỉ sửa phần tử đầu tiên.
- **Không có transaction thì tạo SV + tài khoản xử lý thế nào?** Ghi tuần tự: insert SV, rồi insert tài khoản; nếu bước 2 lỗi thì xóa SV vừa tạo (bù trừ thủ công). Xóa SV thì làm ngược lại: nếu xóa tài khoản lỗi thì chèn lại hồ sơ.
- **Phân trang làm ở đâu?** Ở MongoDB: `countDocuments` lấy tổng số, `find().sort({maSV:1}).skip().limit(20)` lấy một trang, có projection bỏ `monHoc`. GPA chỉ được tính bằng pipeline cho 20 MSSV của trang đó (`$in`), không tính cho cả trường.
- **Tìm kiếm có an toàn không?** Từ khóa được escape trước khi đưa vào regex (`Regex.Escape`) nên ký tự như `.*(` được hiểu theo nghĩa đen. Tùy chọn `i` giúp không phân biệt hoa thường, kể cả chữ có dấu.
