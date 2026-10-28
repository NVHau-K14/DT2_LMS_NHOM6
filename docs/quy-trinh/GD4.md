# Giai đoạn 4 – CRUD Admin: lớp, sinh viên, giảng viên, môn học

## Prompt
```
Đọc CLAUDE.md, docs/thiet-ke.md (mục 2.1, 4, 5.2) và docs/quy-trinh/GD4.md. Thực hiện Giai đoạn 4 đúng theo GD4.md, dùng lại layout/thành phần GĐ2 và service pipeline GĐ3. Lập todo trước khi làm. Khi xong, tự kiểm tra, cập nhật tiến độ trong CLAUDE.md, báo cáo kèm hướng dẫn nghiệm thu, rồi chờ tôi xác nhận mới commit.
```

## Việc cần làm
1. **A-02 Lớp học** `/Admin/LopHoc`: bảng (mã, tên, niên khóa, ngành, CVHT, sĩ số đếm từ sinhvien), lọc ngành/niên khóa, tìm kiếm; modal thêm/sửa (CVHT chọn từ GV, mã lớp khóa khi sửa); xóa (chặn khi còn SV, thông báo rõ).
2. **A-03 Chi tiết lớp** `/Admin/LopHoc/ChiTiet/{maLop}`: thông tin lớp + danh sách SV kèm GPA tích lũy, xếp loại (từ `BaoCaoHocLuc` lọc theo lớp). Nút "Ghi danh cả lớp" (link tới `/Admin/GhiDanh?maLop=` – GĐ5 làm trang) và "Xuất báo cáo" (GĐ7) để sẵn.
3. **A-04 Danh sách SV** `/Admin/SinhVien`: bảng (MSSV, họ tên, lớp, ngày sinh, trạng thái, GPA, xếp loại), lọc lớp/trạng thái, tìm MSSV/tên (regex không phân biệt hoa thường, escape ký tự đặc biệt), **phân trang phía server** 20 dòng (skip/limit + đếm tổng). GPA/xếp loại lấy từ `BaoCaoHocLuc` cho đúng các SV của trang. Nút Nhập Excel/Tải mẫu/Xuất Excel để sẵn (GĐ7). ⋮: Xem hồ sơ, Sửa, Reset mật khẩu, Xóa.
4. **A-05 Thêm/Sửa SV**: form đủ trường, validation (MSSV 10 chữ số, chưa tồn tại; email hợp lệ; ngày sinh hợp lệ; lớp tồn tại). Thêm → insert SV (`monHoc: []`) + tạo tài khoản (mật khẩu = ngày sinh ddMMyyyy); lỗi bước 2 → xóa SV vừa tạo. Sửa: MSSV khóa; đổi họ tên → cập nhật `taikhoan.hoTen`. Xóa SV → xóa tài khoản.
5. **A-06 Hồ sơ SV** `/Admin/SinhVien/HoSo/{maSV}`: thẻ thông tin, thẻ tóm tắt (GPA tích lũy, TC tích lũy, xếp loại, số môn nợ), **bảng điểm theo học kỳ** dùng pipeline `BangDiem` + `GpaHocKy`. Làm bảng điểm thành **partial `_BangDiem`** để S-02 dùng lại. Nút Ghi danh/Hủy ghi danh để GĐ5.
6. **A-07 Giảng viên**: bảng (mã, họ tên, học vị, khoa, email, số lớp học phần học kỳ hiện tại – từ `LopHocPhanCuaGV`), modal thêm/sửa; thêm → tạo tài khoản (mật khẩu 123456); **đổi họ tên hoặc học vị → lan truyền** `sinhvien.monHoc.$[e].giangVien.hoTen` (updateMany + arrayFilters), `lophoc.coVanHocTap.hoTen`, `taikhoan.hoTen`; toast kèm lệnh MongoDB (thiết kế 4.8, 4.9). Xóa: chặn nếu đang dạy hoặc làm CVHT.
7. **A-08 Môn học**: bảng (mã, tên, TC, trọng số), modal thêm/sửa (validation tổng trọng số = 100, TC 1–10). Đổi tên → cảnh báo số lần học bị ảnh hưởng → lan truyền `monHoc.$[e].tenMon` (updateMany + arrayFilters), toast kèm lệnh. Đổi TC/trọng số chỉ áp dụng cho ghi danh mới (ghi chú trên form). Xóa: chặn nếu đã có người học.
8. **A9**: Reset mật khẩu (về mặc định), Khóa/Mở khóa tài khoản – thao tác từ ⋮ của SV và GV.
9. **Infrastructure/MongoCommandFormatter**: từ filter/update/arrayFilters (BsonDocument) dựng chuỗi lệnh dạng mongosh (`db.sinhvien.updateMany({...}, {...}, { arrayFilters: [...] })`) + matched/modified. Dùng cho GĐ4–5.

## Tự kiểm tra
- Build 0 lỗi. Chạy app, curl với cookie admin: các trang trả 200; tạo lớp thử → sửa → xóa; tạo SV thử → đăng nhập được bằng tài khoản mới → xóa SV → tài khoản bị xóa.
- Đổi tên môn INT301 thử → mongosh kiểm tra mọi phần tử `monHoc.maMon:"INT301"` đã đổi tên → đổi lại như cũ.
- Đổi học vị GV005 → kiểm tra lan truyền 3 nơi → đổi lại.
- Xóa lớp 16DHTH01 → bị chặn. Xóa GV001 → bị chặn. Xóa môn INT101 → bị chặn.
- Phân trang `/Admin/SinhVien?page=5` đúng 20 dòng.

## Nghiệm thu (bạn làm)
- [ ] Thêm/sửa/xóa lớp, sinh viên, giảng viên, môn học trên giao diện đều chạy; thông báo dễ hiểu.
- [ ] Hồ sơ SV `2001250002`: bảng điểm theo học kỳ, INT101 lần 1 F, lần 2 B có ✓; GPA tích lũy 2.73 Khá.
- [ ] Đổi tên một môn → toast hiện lệnh `updateMany ... arrayFilters`; Compass thấy tên mới trong mảng `monHoc`. Đổi lại.
- [ ] Các thao tác xóa bị chặn đúng chỗ.
- [ ] Commit.
