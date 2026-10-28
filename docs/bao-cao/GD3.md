# Báo cáo Giai đoạn 3 – Pipeline điểm & GPA + kiểm thử 5 ca

- **Ngày hoàn thành:** 28/10/2026
- **Trạng thái:** ✅ Hoàn thành
- **Commit:** `746df3b` (điền sau khi người dùng nghiệm thu và commit)

## 1. Đã làm gì
- **Bộ stage quy đổi dùng chung** (`Pipelines/QuyDoiDiemStages.cs`): 4 stage `$addFields` đúng mục 3.5 (duDiem → diemTK → diemChu → diemHe4 + dat) và cụm 2 stage tính GPA + xếp loại (`floor(x*100 + 0.5000001)/100`, `$switch`, nhánh "Chưa xếp loại"). Mỗi stage là một chuỗi JSON riêng, có comment tiếng Việt. Không pipeline nào tự viết lại công thức: tất cả chèn hai cụm này qua chỗ giữ `"@quyDoi"` và `"@gpaXepLoai"`.
- **7 pipeline** đúng bảng mục 3.6: `BangDiem`, `GpaHocKy`, `BaoCaoHocLuc` (tích lũy hoặc một học kỳ), `ThongKeXepLoai`, `GpaTheoLop`, `LopHocPhanCuaGV`, `DanhSachNhapDiem`. Mỗi pipeline là một chuỗi JSON (C# raw string). Tham số dạng `"@ten"` được thay bằng `BsonValue`, không ghép chuỗi. Comment đầu mỗi file đánh số từng stage và ghi rõ stage nào đáp ứng barem `$unwind`/`$group`/`$project`/`$cond`/`$switch`.
- **Models/Results**: `KetQuaMon`, `GpaHocKyResult`, `BaoCaoHocLucRow`, `ThongKeXepLoaiRow`, `GpaTheoLopRow`, `LopHocPhanRow`, `DongNhapDiem`, cùng lớp bao `KetQuaPipeline<T>` (dữ liệu + JSON pipeline đã chạy + thời gian thực thi ms).
- **Services** `DiemService` (bảng điểm, GPA học kỳ, lớp học phần, danh sách nhập điểm) và `BaoCaoService` (báo cáo học lực, thống kê xếp loại, GPA theo lớp). Tham số được gói thành `BoLocSinhVien` (tất cả / lớp / SV / danh sách / GV / lớp học phần) và `PhamViBaoCao` (tích lũy / học kỳ) để GĐ 4–7 gọi lại.
- **Lệnh `--kiem-thu-gpa`**: chạy pipeline thật cho TS1–TS5, so với giá trị viết cứng theo bảng 7.5, rồi thêm 11 kiểm tra tổng thể toàn trường. Kết quả **200/200 PASS**, ghi ra `docs/kiem-thu-gpa.md`.
- **Lệnh `--export-pipelines`**: ghi 8 file vào `database/pipelines/*.json`, gồm `BaoCaoHocLuc` thêm một bản phạm vi học kỳ. Có kèm `database/pipelines/README.md` hướng dẫn dùng với Compass và mongosh.

## 2. File chính tạo / sửa
| File | Vai trò |
|---|---|
| `Pipelines/QuyDoiDiemStages.cs` | **Nguồn duy nhất** của công thức: 4 stage quy đổi + 2 stage GPA/xếp loại |
| `Pipelines/PipelineJson.cs` | Parse chuỗi JSON → `BsonDocument[]`, thay tham số `"@ten"`, chèn cụm stage; `ToJson()` thụt lề cho "Xem pipeline" |
| `Pipelines/ThamSoPipeline.cs` | `BoLocSinhVien`, `PhamViBaoCao`, `DieuKienLanHoc` (điều kiện trước/sau `$unwind`) |
| `Pipelines/BangDiemPipeline.cs` | Lần học đã quy đổi + `tinhTichLuy` (`$setWindowFields`) |
| `Pipelines/GpaHocKyPipeline.cs` | GPA từng học kỳ (kể cả F) |
| `Pipelines/BaoCaoHocLucPipeline.cs` | GPA tích lũy / học kỳ, xếp loại, TC tích lũy, số môn nợ – mỗi SV một dòng |
| `Pipelines/ThongKeXepLoaiPipeline.cs`, `GpaTheoLopPipeline.cs` | `BaoCaoHocLuc` + `$group` |
| `Pipelines/LopHocPhanCuaGVPipeline.cs`, `DanhSachNhapDiemPipeline.cs` | Lớp học phần của GV; danh sách nhập điểm |
| `Models/Results/*.cs` | Class nhận kết quả pipeline + `KetQuaPipeline<T>` |
| `Services/PipelineRunner.cs` | Chạy `aggregate` trên `sinhvien`, đo Stopwatch, kèm JSON |
| `Services/DiemService.cs`, `Services/BaoCaoService.cs` | API nghiệp vụ cho các giai đoạn sau |
| `Data/KiemThuGpa.cs` | Lệnh `--kiem-thu-gpa` (giá trị mong đợi viết cứng theo 7.5) |
| `Data/XuatPipeline.cs` | Lệnh `--export-pipelines` |
| `Program.cs` | Đăng ký 2 service; tách 2 cờ mới khỏi `args`, chạy công cụ rồi thoát |
| `database/pipelines/*.json`, `database/pipelines/README.md` | Pipeline để dán vào Compass / mongosh |
| `docs/kiem-thu-gpa.md` | Kết quả kiểm thử (sinh tự động) |

## 3. Kết quả tự kiểm tra
| Hạng mục | Cách kiểm tra | Kết quả |
|---|---|---|
| Build | `dotnet build DT2_LMS_NHOM6.sln` | 0 lỗi, 0 cảnh báo ✅ |
| Kiểm thử GPA | `dotnet run --project DT2_LMS_NHOM6 -- --kiem-thu-gpa` | **200/200 PASS**, mã thoát 0 ✅ |
| Xuất pipeline | `-- --export-pipelines` | 8 file `.json` hợp lệ ✅ |
| File JSON chạy được | mongosh `db.sinhvien.aggregate(EJSON.parse(fs.readFileSync(...)))` cho cả 8 file | `BangDiem` TS1: 8.5 A / 6.4 C / 7.2 B / 9 A / INT201 null; GPA 3 / 3.6 / null – giống lệnh kiểm thử ✅ |
| BaoCaoHocLuc toàn trường | mongosh + lệnh kiểm thử | 100 dòng ✅ |
| ThongKeXepLoai | mongosh | Xuất sắc 18, Giỏi 16, Khá 29, Trung bình 19, Yếu 18 → tổng 100 ✅ |
| GpaTheoLop | mongosh | 5 lớp × 20 SV; GPA TB 2.54–2.96 ✅ |
| SV không có lần học trong phạm vi | `BaoCaoHocLuc` phạm vi HK1 2026-2027 (CK chưa nhập) | 100 dòng, tất cả "Chưa xếp loại", gpa null ✅ |
| Không dùng `$round` | `grep -rn '\$round' DT2_LMS_NHOM6/Pipelines database/pipelines` | Không có ✅ |
| Web vẫn chạy | `dotnet run`, curl `/Account/Login` | 200 ✅ |

## 4. Quyết định kỹ thuật mới
- **Tham số pipeline bằng chỗ giữ `"@ten"`**: JSON được parse thành Bson trước, sau đó mới thay giá trị vào cây document, nên không có ghép chuỗi và không có lỗi chèn mã. Nếu `"@ten"` đứng làm phần tử của mảng pipeline thì cả cụm stage được chèn vào đó; `ThongKeXepLoai` và `GpaTheoLop` tái dùng `BaoCaoHocLuc` theo cách này.
- **`tinhTichLuy` dùng `$first` thay cho `$documentNumber`**: MongoDB báo lỗi *"$documentNumber must be specified with a top level sortBy expression with exactly one element"*, trong khi tiêu chí "lần tốt nhất" cần sắp theo 4 khóa (hệ 4 ↓, TK ↓, năm học ↓, học kỳ ↓). Vì vậy `$setWindowFields` giữ nguyên `partitionBy` + `sortBy` 4 khóa và lấy `maDKTotNhat = $first` trên cả partition (window `["unbounded","unbounded"]`). `tinhTichLuy = (maDK == maDKTotNhat) && dat`. Kết quả tương đương "số thứ tự = 1"; chỉ cú pháp khác thiết kế.
- **Một pipeline `BaoCaoHocLuc` cho cả hai phạm vi**: stage `$filter` giữ các lần học thuộc phạm vi (tích lũy giữ hết). Điều kiện cộng vào GPA là `"@dieuKienTinhGpa"`: tích lũy dùng `dat = true` (lần tốt nhất phải đạt), học kỳ dùng `duDiem = true` (tính cả F). Trong một học kỳ mỗi môn chỉ có một lần học, nên bước "lần tốt nhất" không làm đổi kết quả.
- **Phạm vi học kỳ trong `BaoCaoHocLuc`** (thiết kế chưa nói rõ): `gpa` = GPA học kỳ, `tinChiTichLuy` = TC đạt trong kỳ, `soMonNo` = số môn F trong kỳ.
- **`GpaTheoLop`**: GPA trung bình của lớp = `$avg` GPA tích lũy (đã làm tròn) của các SV có GPA (`$avg` bỏ qua null), sau đó làm tròn lại bằng công thức floor. Có thêm cột `soSVCoGpa`.
- **`ThongKeXepLoai`** có trường `thuTu` (dùng `$switch`) để sắp Xuất sắc → Kém → Chưa xếp loại, và mảng `theoLop`.
- **JSON hiển thị**: driver ghi số thực 17 chữ số (`0.50000009999999995`), nên `PipelineJson.ToJson` ghi lại ở dạng ngắn nhất mà vẫn đúng giá trị (`0.5000001`, `7.0`). Thụt lề bằng System.Text.Json, giữ nguyên tiếng Việt.
- **Kiểm thử so sánh số thực bằng `==`** (không dung sai): phép chia trong MongoDB làm tròn đúng chuẩn IEEE, nên `327/100` cho cùng số double với hằng `3.27`. Lệch dù rất nhỏ cũng bị tính là FAIL.
- Hai lệnh công cụ chạy `DbInitializer` trước (DB trống thì tự seed), làm xong việc thì thoát, không khởi động web. Có thể truyền cả hai cờ cùng lúc.
- Service là class cụ thể, không viết interface C#, theo cùng kiểu `AccountService`. Phương thức public đặt tên rõ ràng để GĐ 4–7 gọi lại.

## 5. Điểm cần chú ý / hạn chế
- `docs/kiem-thu-gpa.md` và `database/pipelines/*.json` là file **sinh tự động**. Sửa pipeline thì chạy lại hai lệnh rồi commit kèm.
- Kiểm thử dựa vào dữ liệu mẫu chuẩn. Nếu đã nhập điểm thử cho TS1–TS5 hoặc cho lớp học phần INT301 (GĐ5), chạy `-- --reset` trước rồi mới kiểm thử.
- Bộ quy đổi xuất hiện lặp lại trong từng file `.json` vì Compass cần pipeline đầy đủ. Trong code C# nó chỉ được định nghĩa ở một nơi.
- Kiểm thử có 15 tiêu chí ghi "(suy ra từ 3.3)": GPA học kỳ HK1 2026-2027 là null / "Chưa xếp loại". Bảng 7.5 không ghi trực tiếp các giá trị này; chúng suy ra từ quy tắc mục 3.3–3.4.
- Chưa có giao diện. Các màn hình dùng pipeline sẽ làm ở GĐ 4–6.

## 6. Nghiệm thu
- [ ] Chạy `dotnet run --project DT2_LMS_NHOM6 -- --kiem-thu-gpa` → toàn PASS (dòng cuối: `TỔNG KẾT: 200/200 PASS – TẤT CẢ ĐẠT`).
- [ ] Mở `docs/kiem-thu-gpa.md` đọc được, đối chiếu vài số với bảng tính tay (gợi ý: TS1 INT101 = 8.5 A; TS4 GPA HK2 = 0.60 Kém, nợ 2; TS5 tích lũy = 3.60 Xuất sắc).
- [ ] Compass → `eduscore` → `sinhvien` → Aggregations → nút `</>` (Text) → dán `database/pipelines/BaoCaoHocLuc.json` → ra danh sách GPA + xếp loại (100 SV). Lưu pipeline trong Compass (Save → Save pipeline as… "Báo cáo học lực") để dùng khi demo.
- [ ] Đọc lướt `DT2_LMS_NHOM6/Pipelines/QuyDoiDiemStages.cs`: comment đủ để giải thích từng stage.
- [ ] Commit.

## 7. Gợi ý trả lời khi bảo vệ
- **Vì sao không dùng `$round`?** `$round` của MongoDB làm tròn kiểu banker's (half-to-even): 84.5 → 84 → TK 8.4 (B), trong khi quy chế yêu cầu làm tròn nửa lên → 8.5 (A). Nhóm dùng `floor(tong/10 + 0.5000001)/10`; phần `0.0000001` để bù sai số dấu phẩy động. Ca TS1 INT101 trong kiểm thử chứng minh điều này.
- **GPA tích lũy chọn lần học nào khi học lại / cải thiện?** Mỗi môn: `$sort` theo hệ 4 ↓, TK ↓, học kỳ mới nhất, rồi `$group` theo (maSV, maMon) lấy `$first`, tức là lần tốt nhất. Chỉ cộng môn mà lần tốt nhất đạt. TS3 cải thiện MAT101 bị điểm thấp hơn (D) nên GPA vẫn giữ C của lần 1.
- **Tính hoàn toàn ở tầng CSDL nghĩa là sao?** Database chỉ lưu điểm thành phần CC/GK/CK và trọng số. TK, chữ, hệ 4, GPA, xếp loại do pipeline tính mỗi lần truy vấn, C# chỉ hiển thị. Nhờ đó không có dữ liệu dư thừa có thể lệch nhau, và sửa một điểm thì báo cáo đúng ngay.
- **Các stage barem nằm ở đâu?** Ví dụ `BaoCaoHocLuc`: `$unwind` (preserveNullAndEmptyArrays để SV chưa có điểm vẫn có dòng), `$group` hai tầng (lần tốt nhất → theo SV), `$cond` (chỉ cộng TC môn đạt, tránh chia 0), `$switch` (điểm chữ, xếp loại), `$project` (định dạng đầu ra). Comment đầu mỗi file pipeline có đánh số từng stage.
