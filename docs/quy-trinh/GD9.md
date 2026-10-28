# Giai đoạn 9 – Hoàn thiện, export database, README, tài liệu

Không được bỏ giai đoạn này: README và database là thứ phải nộp.

## Prompt
```
Đọc CLAUDE.md, docs/thiet-ke.md và docs/quy-trinh/GD9.md. Thực hiện Giai đoạn 9 đúng theo GD9.md. Lập todo trước khi làm. Khi xong, tự kiểm tra (kể cả thử clone sạch), cập nhật tiến độ trong CLAUDE.md, báo cáo kèm hướng dẫn nghiệm thu, rồi chờ tôi xác nhận mới commit.
```

## Việc cần làm

> Nếu skill `frontend-design` đã được cài: dùng nó cho phần giao diện, nhưng **bắt buộc giữ đúng phong cách ở thiết kế mục 8** (tông xanh dương kiểu hoclieu, sidebar trái, nền sáng, Bootstrap 5, không font/thư viện từ CDN).
1. **Rà soát toàn bộ màn hình** theo thiết kế mục 5 (đăng nhập từng vai trò, đi hết menu): thống nhất tiêu đề/breadcrumb, khoảng cách, màu badge, empty state, thông báo lỗi; không còn trang mặc định của template, không còn chữ tiếng Anh, không còn link chết; responsive cơ bản; không có lỗi console JS. Sửa lỗi phát hiện được (không thêm chức năng mới).
2. **Xử lý lỗi**: kết nối MongoDB thất bại khi khởi động → thông báo rõ trên console ("Không kết nối được MongoDB tại ..."); exception trong request → trang lỗi thân thiện + log.
3. **Export database** – lệnh `-- --export`:
   - Ghi `database/export/<collection>.json` cho 5 collection (Extended JSON relaxed, dạng mảng, import được bằng Compass "Import JSON" và `mongoimport --jsonArray`), giữ nguyên `_id`, ObjectId, Date.
   - Nếu máy có `mongodump` (kiểm tra `mongodump --version`): tạo `database/dump/eduscore.archive.gz` (`--archive --gzip`). Không có thì bỏ qua và ghi chú trong README cách cài MongoDB Database Tools.
   - Viết `database/khoi-phuc.ps1`: ưu tiên `mongorestore --archive --gzip --drop`; nếu không có archive/tool → hướng dẫn import JSON. Viết `database/README.md` (3 cách khôi phục: chạy app tự seed, `--reset`, import JSON/Compass, mongorestore).
   - Chạy export trên dữ liệu chuẩn (sau `--reset`).
4. **README.md** (gốc repo, tiếng Việt, đẹp trên GitHub): giới thiệu, ảnh chụp (để chỗ `docs/images/*.png` – người dùng bổ sung), công nghệ, yêu cầu cài đặt (.NET 9, MongoDB 8+, tùy chọn Compass/mongosh), các bước chạy (clone → `dotnet run` → mở http://localhost:5207), **bảng tài khoản demo** (admin, GV005, GV001, TS1–TS5 với mật khẩu), các lệnh `--reset` / `--kiem-thu-gpa` / `--export-pipelines` / `--export` / `--tao-file-demo`, cấu trúc thư mục, mô hình dữ liệu tóm tắt + link tài liệu, chức năng theo vai trò, mapping barem → chức năng, thông tin nhóm (Nhóm 6 – Đề tài 02; để chỗ trống họ tên/MSSV cho người dùng điền).
5. **Tài liệu báo cáo trong `docs/`**:
   - `thiet-ke-csdl.md`: (a) **Giải trình ưu điểm Document Store** cho hồ sơ học tập lồng nhau: đọc trọn hồ sơ một lần, khớp cấu trúc nghiệp vụ, cập nhật nguyên tử trên một document, schema linh hoạt có kiểm soát bằng validator, so sánh với mô hình quan hệ (bảng SinhVien–DangKy–MonHoc phải JOIN), phân tích nhúng vs tham chiếu cho từng quan hệ, denormalization có chủ đích + cách lan truyền, giới hạn 16MB và tại sao không vấn đề, nhược điểm và cách xử lý (không transaction, trùng lặp dữ liệu). (b) **JSON Schema** đầy đủ của `sinhvien` + sơ đồ cấu trúc document dạng Mermaid (`classDiagram` hoặc `flowchart`) + sơ đồ quan hệ 5 collection. (c) Bảng index và lý do. (d) Giải thích từng pipeline và từng toán tử mảng (`$push`, `$pull`, `$set` + `arrayFilters`, `updateMany` lan truyền) kèm câu lệnh mẫu.
   - `kich-ban-demo.md`: kịch bản demo **cho 1 người trình bày** theo 3 phần của đề (nhập điểm → Compass; báo cáo học lực → pipeline; import/export → giải thích dữ liệu trong Compass): từng bước bấm gì, tài khoản nào, câu lệnh/filter Compass chuẩn bị sẵn, câu nói gợi ý, thời lượng ~10 phút, phương án dự phòng khi lỗi.
   - Cập nhật `kiem-thu-gpa.md` (chạy lại `--kiem-thu-gpa`).
6. **Kiểm tra clone sạch** (sau khi người dùng đã commit các phần trước): clone repo ra thư mục tạm ngoài repo, đặt biến môi trường `MongoDB__DatabaseName=eduscore_clonetest`, `dotnet run` → tự seed, mở được trang login; xóa DB `eduscore_clonetest` và thư mục tạm sau khi xong.

## Tự kiểm tra
- Build 0 lỗi 0 cảnh báo đáng kể; `--kiem-thu-gpa` PASS; clone sạch chạy được.
- `database/export/*.json` đủ 5 file, import thử 1 file vào DB tạm bằng mongoimport (nếu có) hoặc kiểm tra JSON hợp lệ.
- Mermaid trong docs đúng cú pháp.
- `git status` không có file rác (cookie, file tạm, bin/obj).

## Nghiệm thu (bạn làm)
- [ ] Đọc README như người lạ: làm theo được.
- [ ] Đọc `docs/thiet-ke-csdl.md` và `docs/kich-ban-demo.md` – đủ để bạn trình bày.
- [ ] Đi một vòng 3 vai trò, không thấy lỗi giao diện.
- [ ] Commit. (GĐ 10: xem `docs/quy-trinh/README.md`.)
