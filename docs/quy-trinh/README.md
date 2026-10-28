# Quy trình làm việc với Claude Code

## Chuẩn bị (một lần)
1. Mở VS Code tại thư mục gốc `D:\Documents\BaiTap\NoSQL\DT2_LMS_NHOM6` (thư mục chứa `CLAUDE.md`).
2. Mở Claude Code, gõ `/model` → chọn **Sonnet** (tiết kiệm hạn mức gói Pro). Chỉ đổi sang Opus nếu GĐ 3 làm sai pipeline.
3. Khi Claude Code xin quyền chạy lệnh `dotnet`, `mongosh`, `git`, `curl`, `taskkill`: chọn **"Yes, and don't ask again"** cho các lệnh này để nó chạy liên tục.
4. Giữ MongoDB service đang chạy (cổng 27017).

## Mỗi giai đoạn
1. Gõ `/clear` (bắt đầu phiên sạch).
2. Mở file `GD<N>.md`, sao chép khối **Prompt** dán vào Claude Code.
3. Để Claude Code làm đến khi nó báo cáo xong (thường 30–90 phút). Không cần ngồi canh; quay lại khi có thông báo.
4. Đọc báo cáo `docs/bao-cao/GD<N>.md` Claude Code vừa viết, rồi làm theo mục **Nghiệm thu** (5–10 phút) và tick vào checklist trong báo cáo.
5. Ổn → gõ: `OK, commit giai đoạn N.`
   Chưa ổn → mô tả lỗi cụ thể: `Lỗi: <màn hình/thao tác> – <kết quả thấy> – <kết quả mong đợi>. Sửa trong phạm vi GĐ N rồi báo lại.`
6. Sang giai đoạn kế tiếp (quay lại bước 1).

## Khi có sự cố
- **Hết hạn mức (limit)**: chờ khung 5 giờ mới. Khi tiếp tục: `/clear` rồi dán: `Tiếp tục Giai đoạn N theo docs/quy-trinh/GD<N>.md. Kiểm tra code hiện có và bảng tiến độ trong CLAUDE.md, làm nốt phần còn thiếu, không làm lại phần đã xong.`
- **Claude Code đi lạc / làm thêm chức năng**: `Dừng. Chỉ làm đúng phạm vi GD<N>.md và docs/thiet-ke.md. Hoàn tác phần thêm ngoài phạm vi.`
- **Hỏng nặng sau khi sửa**: `git restore .` (bỏ thay đổi chưa commit) hoặc `git reset --hard` về giai đoạn trước, rồi làm lại giai đoạn.
- **Lỗi khó hiểu**: dán lỗi sang Claude Desktop để bàn.

## Lịch
| Thời gian | Việc |
|---|---|
| Tối 28/10 | GĐ 1 → 3 |
| Sáng 29/10 | GĐ 4 → 5 |
| Chiều 29/10 | GĐ 6 → 7 (còn dư giờ thì GĐ 8) |
| Tối 29/10 | GĐ 9 · song song soạn SRS ở Claude Desktop |
| Sáng 30/10 | GĐ 10 (bạn): nghiệm thu tổng, chụp màn hình, slide, push GitHub trước 11h |

Bị trễ: bỏ GĐ 8 → bỏ biểu đồ ở GĐ 6. Không bỏ GĐ 9.

## GĐ 10 – Nghiệm thu tổng (bạn tự làm, sáng 30/10)
- [ ] `dotnet run --project DT2_LMS_NHOM6 -- --reset` chạy sạch.
- [ ] Đăng nhập đủ 3 vai trò, đi hết menu, không trang nào lỗi.
- [ ] Chạy thử trọn kịch bản trong `docs/kich-ban-demo.md` 2 lần.
- [ ] Compass: lưu sẵn filter và pipeline dùng cho demo.
- [ ] Chụp màn hình các trang chính (cho README, SRS, slide).
- [ ] Sao lưu DB trước giờ demo (`-- --export`).
- [ ] Tạo repo GitHub, `git remote add origin ...`, `git push -u origin main`; mở repo kiểm tra README hiển thị đúng, có thư mục `database/`.
- [ ] Gửi link.
