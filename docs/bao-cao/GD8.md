# Báo cáo Giai đoạn 8 – Hiệu năng + API/Swagger (tùy chọn)

- **Ngày hoàn thành:** 28/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Commit:** `5e007dc` (điền sau khi người dùng nghiệm thu và commit)

## 1. Đã làm gì
- **A-12 `/Admin/HieuNang`**: trang đo hiệu năng.
  - Có 3 truy vấn đo:
    - (a) tìm SV theo `maSV`;
    - (b) danh sách nhập điểm theo `monHoc.giangVien.maGV` + học kỳ, viết bằng `$elemMatch` giống bộ lọc lớp học phần;
    - (c) tìm SV theo `monHoc.maMon`.
  - Mỗi truy vấn chạy `explain("executionStats")` hai lần: một lần để MongoDB tự chọn index, một lần ép quét toàn bộ bằng `hint({ $natural: 1 })`.
  - Bảng so sánh gồm: stage thắng (badge xanh IXSCAN / đỏ COLLSCAN), cây stage, tên index (kèm nhãn *multikey*), keysExamined, docsExamined, nReturned, executionTimeMillis.
  - Dưới mỗi bảng có đoạn giải thích ngắn và lệnh mongosh tương đương (thu gọn).
  - Đo pipeline `BaoCaoHocLuc` toàn trường 20 lần: min / trung bình / max (ms), số dòng kết quả và thời gian từng lần.
  - Giá trị đo được điền sẵn từ dữ liệu thật (SV đầu tiên có lần học trong học kỳ hiện tại). Admin sửa được giá trị rồi bấm **Chạy đo**. Trang chỉ đọc, không ghi gì vào database.
- **API REST `/api/...`**: 9 endpoint, `[ApiController]`, trả JSON.
  - Mọi nghiệp vụ gọi lại đúng service của màn hình MVC (`SinhVienService`, `GhiDanhService`, `DiemService`, `BaoCaoService`), không lặp logic.
  - Xác thực bằng cookie đăng nhập hiện có.
  - Các API ghi trả kèm trường `mongoCommand`.
- **Swagger** (Swashbuckle.AspNetCore 9.0.6) tại `/swagger`:
  - chỉ bật ở môi trường Development;
  - mỗi endpoint có mô tả tiếng Việt lấy từ comment `///`: tóm tắt, ghi chú, ví dụ body, mã phản hồi;
  - sidebar Admin có thêm mục **API** (mở tab mới, cũng chỉ hiện ở Development).

| Phương thức | Đường dẫn | Quyền | Service dùng lại |
|---|---|---|---|
| GET | `/api/sinhvien?maLop=&page=` | Admin | `SinhVienService.DanhSachAsync` + pipeline `BaoCaoHocLuc` |
| GET | `/api/sinhvien/{maSV}` | Admin; GV (SV có học mình); SV (chính mình) | `SinhVienService.LayAsync` (document gốc, có mảng `monHoc`) |
| POST | `/api/sinhvien` | Admin | `SinhVienService.ThemAsync` (insertOne SV + tài khoản) |
| PUT | `/api/sinhvien/{maSV}` | Admin | `SinhVienService.SuaAsync` |
| DELETE | `/api/sinhvien/{maSV}` | Admin | `SinhVienService.XoaAsync` |
| POST | `/api/sinhvien/{maSV}/monhoc` | Admin | `GhiDanhService.XemTruocAsync` + `XacNhanAsync` (`$push`) |
| PUT | `/api/sinhvien/{maSV}/monhoc/{maDK}/diem` | GV (lần học mình dạy), Admin | `DiemService.CapNhatDiemAsync` (`$set` + `arrayFilters`) |
| GET | `/api/sinhvien/{maSV}/bangdiem` | như GET hồ sơ | pipeline `BangDiem`, `GpaHocKy`, `BaoCaoHocLuc` |
| GET | `/api/baocao/hocluc?maLop=` | Admin (toàn trường / lớp); GV (SV có học mình) | pipeline `BaoCaoHocLuc` |

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `Services/HieuNangService.cs` (mới) | Chạy `explain` qua `runCommand`, duyệt cây `winningPlan` để lấy stage / index / multikey; đo pipeline 20 lần |
| `Areas/Admin/Controllers/HieuNangController.cs`, `Areas/Admin/Views/HieuNang/Index.cshtml` (mới) | Màn hình A-12 |
| `ViewModels/HieuNangViewModels.cs` (mới) | `ThamSoHieuNang`, `KetQuaExplain`, `SoSanhTruyVan`, `KetQuaDoPipeline` |
| `Controllers/Api/ApiControllerBase.cs` (mới) | `[ApiController] [Authorize] [IgnoreAntiforgeryToken]`, đổi `KetQuaGhi` → JSON `KetQuaApi` |
| `Controllers/Api/SinhVienApiController.cs`, `BaoCaoApiController.cs` (mới) | 9 endpoint, kèm mô tả Swagger |
| `ViewModels/ApiModels.cs` (mới) | `KetQuaApi`, `TrangSinhVienApi`, `BangDiemApi`, `BaoCaoHocLucApi`, `GhiDanhApiForm`, `DiemApiForm` |
| `Infrastructure/ObjectIdJsonConverter.cs` (mới) | Đổi ObjectId sang chuỗi hex trong JSON |
| `Program.cs` | JSON options; lỗi validation API bằng tiếng Việt; cookie trả 401/403 JSON cho `/api`; Swagger chỉ ở Development; trang lỗi HTML chỉ áp dụng ngoài `/api` |
| `Services/SinhVienService.cs`, `TaiKhoanService.cs` | Thêm / sửa / xóa SV trả `MongoCommand`; `CoLanHocCuaGiangVienAsync` |
| `Infrastructure/MongoCommandFormatter.cs` | Thêm `InsertOne`, `DeleteOne` |
| `Pipelines/ThamSoPipeline.cs` | `BoLocSinhVien.VaLop` (GV lọc báo cáo thêm lớp) |
| `ViewModels/SinhVienViewModels.cs` | `[JsonIgnore]` cho `LaSua`, `DsLop` để API dùng lại form SV (cùng validation) |
| `Views/Shared/_Sidebar.cshtml` | Mục "API" cho Admin (Development) |
| `DT2_LMS_NHOM6.csproj` | Swashbuckle.AspNetCore 9.0.6, `GenerateDocumentationFile` |

## 3. Kết quả tự kiểm tra
| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | 0 lỗi, 0 cảnh báo ✅ |
| Swagger | GET `/swagger/index.html`, `/swagger/v1/swagger.json` | 200; đủ 9 endpoint, tóm tắt tiếng Việt; không lẫn route MVC ✅ |
| A-12 (a) `maSV` | Chạy đo | Có index: **EXPRESS_IXSCAN** `ux_maSV`, keys 1 / docs 1 / trả 1. Không index: **COLLSCAN**, docs 100 ✅ |
| A-12 (b) GV + học kỳ | GV002, HK1 2026-2027 | Có index: FETCH → **IXSCAN** `ix_monHoc_maGV_namHoc_hocKy` (multikey), keys 57 / docs 57 / trả 57. Không index: COLLSCAN, docs 100 ✅ |
| A-12 (c) `maMon` | INT201 | Có index: FETCH → **IXSCAN** `ix_monHoc_maMon` (multikey), docs 57. Không index: COLLSCAN, docs 100 ✅ |
| A-12 pipeline | 20 lần BaoCaoHocLuc toàn trường | min 9.06 / TB 18.69 / max 23.63 ms, 100 dòng (số đo thay đổi mỗi lần chạy) ✅ |
| Chưa đăng nhập | GET `/api/baocao/hocluc` | 401 + JSON tiếng Việt (không redirect) ✅ |
| curl + cookie (GV005) | đăng nhập rồi `GET /api/baocao/hocluc?maLop=16DHTH01` | 200, 17 SV của lớp có học GV005 ✅ |
| curl PUT điểm | `PUT .../monhoc/000000000000000000000000/diem` | 404 + `mongoCommand` có `matchedCount: 0` ✅ |
| GET danh sách / hồ sơ / bảng điểm | Admin | 200; `_id`, `maDK` là chuỗi hex; bảng điểm có TK/chữ/hệ 4 từ pipeline ✅ |
| POST SV sai | maSV "12", họ tên trống | 400, `loi: { maSV: [...], hoTen: [...] }` tiếng Việt ✅ |
| POST / PUT / DELETE SV | SV tạm 2001259999 | 201 + Location; trùng → 400; PUT đổi MSSV → 400; PUT trả 2 lệnh (sinhvien + taikhoan); DELETE trả 2 lệnh `deleteOne`; xóa lại → 404 ✅ |
| Ghi danh API | INT301 HK1 2026-2027 GV005 | 201, `mongoCommand` = bulkWrite `$push`, `duLieu.maDK` mới; ghi danh lại → 400 "Đã ghi danh INT301…" ✅ |
| Nhập điểm API (GV005) | `{cc: 9, gk: 8, ck: 8.5}` | 200, TK 8.4 (B), lệnh `$set` + `arrayFilters` có `e.giangVien.maGV` ✅; `ck: 8.25` → 400 |
| Nhập điểm API (GV001 không dạy) | cùng lần học | 404, `matchedCount: 0, modifiedCount: 0` – quyền nằm trong arrayFilters ✅ |
| Quyền đọc | GV001 xem SV không học mình; SV xem người khác; SV gọi báo cáo; GV POST SV | 403 JSON ✅ |
| Báo cáo GV | GV005 không lọc / lọc 16DHTH01 | 78 SV / 17 SV (chỉ SV có học GV005) ✅ |
| Hồi quy G-03 | Lưu từng dòng bằng fetch | Không token → 400; có token → 200, "8,5" được nhận ✅ |
| Hồi quy trang lỗi | `/Admin/KhongCo` | 404 trang HTML tiếng Việt như cũ ✅ |
| Dữ liệu sau kiểm tra | mongosh | 100 SV, 100 tài khoản SV, không còn SV tạm ✅ |

## 4. Quyết định kỹ thuật mới
- **API bỏ antiforgery token** (`[IgnoreAntiforgeryToken]` ở `ApiControllerBase`). Lý do:
  - API chỉ nhận body JSON, nên trang web khác muốn gửi phải qua CORS preflight (ứng dụng không bật CORS);
  - cookie `SameSite=Lax` không đi kèm request POST/PUT/DELETE từ trang khác.

  Nhờ vậy "Try it out" trên Swagger chạy được ngay bằng cookie đã đăng nhập. Mọi form MVC vẫn bắt buộc token như cũ.
- **Mã trạng thái**:
  - 401: chưa đăng nhập;
  - 403: sai vai trò hoặc không có quyền đọc SV;
  - 404: không tìm thấy SV, hoặc lệnh nhập điểm có `modifiedCount = 0` (không có quyền hoặc không tìm thấy lần học – không tiết lộ lần học có tồn tại hay không);
  - 400: dữ liệu sai hoặc bị chặn nghiệp vụ;
  - 201: thêm SV / ghi danh.
- **Thêm / sửa / xóa SV giờ có `MongoCommand`** (để API ghi trả được trường này, theo yêu cầu GD8). Vì dùng chung service, toast trên web ở A-04/A-05/A-06 cũng hiện thêm phần "Lệnh MongoDB đã chạy". Lệnh `insertOne taikhoan` in cả chuỗi băm BCrypt, giống lệnh `insertMany` khi nhập Excel ở GĐ7.
- **PUT điểm nhận số** (`8.5`, `null`). Server đổi số sang chuỗi rồi kiểm tra bằng đúng `DiemNhap.TryParse` của G-03. Không viết bộ kiểm tra thứ hai.
- **Ghi danh qua API** chạy `XemTruocAsync` để trả đúng lý do bị chặn, rồi `XacNhanAsync`. Hàm `XacNhanAsync` tự kiểm tra lại ở server như A-09. Trả về lần học mới (có `maDK`) lấy từ pipeline `BangDiem`.
- **JSON**:
  - ObjectId → chuỗi hex (converter toàn cục, Swagger cũng ghi kiểu `string`);
  - encoder `UnsafeRelaxedJsonEscaping`, để chữ tiếng Việt và dấu `"` trong `mongoCommand` đọc được (JSON chỉ trả qua HTTP, không nhúng vào HTML).
- **Không thêm pipeline mới, không sửa pipeline cũ.** Không cần chạy lại `--kiem-thu-gpa` / `--export-pipelines`.

## 5. Điểm cần chú ý / hạn chế
- Dữ liệu chỉ có 100 SV nên `executionTimeMillis` là 0 ms ở cả hai cách. Chênh lệch thể hiện ở **docsExamined** (1 hoặc 57 so với 100). Khi bảo vệ nên nhấn vào số document phải đọc, không nhấn vào thời gian.
- Truy vấn (a) trên MongoDB 8 hiện **EXPRESS_IXSCAN**, không phải IXSCAN. Đây là đường tắt của bản 8.0 cho phép so sánh bằng trên index unique, bản chất vẫn là quét index.
- Mặc định (b) và (c) có thể cho cùng số liệu nếu giảng viên đó dạy đúng môn đó (GV002 – INT201). Muốn demo phần nhập điểm thì nhập GV005, môn INT301.
- Thời gian pipeline đo cả đường truyền nội bộ và thời gian chuyển kết quả sang object C#. Mỗi lần bấm, số đo dao động vài ms.
- Swagger và link "API" chỉ có khi chạy `dotnet run` (profile `http` đặt `ASPNETCORE_ENVIRONMENT=Development`). Chạy ở Production thì `/swagger` trả 404.
- Đường dẫn `/api/...` không tồn tại trả 404 với body rỗng (không có trang HTML).
- API ghi thật vào database. Nếu thử POST / PUT / DELETE trên SV có sẵn thì nên `--reset` trước khi chạy kiểm thử GPA.

## 6. Nghiệm thu
Chạy `dotnet run --project DT2_LMS_NHOM6`, đăng nhập `admin / Admin@123`.
- [ ] `/Admin/HieuNang` → bấm **Chạy đo** → mỗi bảng: cột "Có index" badge xanh IXSCAN (hoặc EXPRESS_IXSCAN), docsExamined nhỏ; cột "Không index" badge đỏ COLLSCAN, docsExamined = 100. Cuối trang có min / TB / max của 20 lần chạy pipeline.
- [ ] Bấm mục **API** ở sidebar (hoặc mở `/swagger`) → mở nhóm "Báo cáo" → `GET /api/baocao/hocluc` → **Try it out** → (tùy chọn) nhập `maLop` = `16DHTH01` → **Execute** → thấy JSON có `soSinhVien`, `thoiGianMs`, `duLieu`.
- [ ] (Tùy chọn) Thử nhập điểm bằng Swagger:
  1. Đăng xuất, đăng nhập `GV005 / 123456`, rồi mở `/swagger`.
  2. Lấy một `maDK` môn INT301 bằng `GET /api/sinhvien/2001250006/bangdiem` (hoặc SV khác trong lớp học phần INT301 của GV005).
  3. Gọi `PUT /api/sinhvien/{maSV}/monhoc/{maDK}/diem` với body `{ "cc": 9, "gk": 8, "ck": 8.5 }` → thấy `mongoCommand` có `arrayFilters`.
  4. Mở Compass để thấy điểm nằm trong đúng phần tử mảng.
  5. Sau khi thử, nên chạy `--reset` nếu muốn dữ liệu nguyên bản.
- [ ] Commit.

## 7. Gợi ý trả lời khi bảo vệ
- **Index multikey là gì, sao dùng được cho mảng `monHoc`?** Khi trường được index nằm trong mảng, MongoDB tạo một khóa index cho **mỗi phần tử** của mảng. Một hồ sơ SV có 5 lần học thì có 5 khóa trỏ về cùng document. Nhờ vậy câu "SV nào học môn X / do GV Y dạy" tra index trực tiếp mà không phải mở từng hồ sơ. Explain cho thấy IXSCAN với `isMultiKey: true`, docsExamined bằng đúng số SV trả về; khi ép `$natural` thì phải đọc cả 100 document.
- **Sao truy vấn (b) dùng `$elemMatch`?** Với index ghép multikey, nếu viết 3 điều kiện rời (`"monHoc.giangVien.maGV"`, `"monHoc.namHoc"`, `"monHoc.hocKy"`), mỗi điều kiện có thể khớp ở **những phần tử khác nhau**, nên MongoDB không ghép được giới hạn quét. `$elemMatch` buộc cả 3 điều kiện khớp trên cùng một phần tử: đúng nghĩa "lần học của lớp học phần" và quét index chặt hơn.
- **API có lặp logic với web không?** Không. Controller API chỉ đổi request thành tham số service và đổi kết quả thành JSON. Pipeline tính điểm, quy tắc ghi danh, kiểm tra điểm, lệnh `$set` + `arrayFilters` đều nằm ở service mà màn hình MVC đang dùng. Ví dụ PUT điểm gọi `DiemService.CapNhatDiemAsync`, đúng hàm của nút "Lưu" ở G-03.
- **GV gọi API sửa điểm lớp người khác thì sao?** `maGV` lấy từ claim của cookie, không lấy từ request, và nằm trong cả filter lẫn `arrayFilters` (`e.giangVien.maGV`). Lệnh vẫn chạy nhưng không khớp phần tử nào (`modifiedCount: 0`), API trả 404 kèm lệnh đã chạy. Quyền được kiểm tra ngay trong câu lệnh MongoDB.
