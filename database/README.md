# Thư mục `database/` – dữ liệu xuất và cách khôi phục

| Đường dẫn | Nội dung |
|---|---|
| `export/<collection>.json` | 5 collection `sinhvien`, `lophoc`, `giangvien`, `monhoc`, `taikhoan` – mảng JSON, **Extended JSON dạng relaxed** (`{"$oid": ...}`, `{"$date": "...Z"}`), giữ nguyên `_id`. Xuất từ dữ liệu mẫu chuẩn (ngay sau `--reset`). |
| `dump/eduscore.archive.gz` | Bản dump nhị phân của `mongodump --archive --gzip` (chỉ có khi máy xuất dữ liệu có cài MongoDB Database Tools). Chứa cả validator và index. |
| `pipelines/*.json` | Các Aggregation Pipeline để dán vào Compass / chạy bằng mongosh – xem [`pipelines/README.md`](pipelines/README.md). |
| `khoi-phuc.ps1` | Script PowerShell khôi phục database (tự chọn cách phù hợp với công cụ có trên máy). |
| `nhap-json.mongosh.js` | Script mongosh nhập các file `export/*.json` (được `khoi-phuc.ps1` gọi khi máy không có Database Tools). |

Số lượng document trong bản xuất (cả JSON lẫn dump, cùng một lần xuất): `sinhvien` 100 · `lophoc` 5 · `giangvien` 8 · `monhoc` 14 · `taikhoan` 109.

---

## Các cách khôi phục

### Cách 1 – Chạy ứng dụng, tự tạo dữ liệu mẫu (đơn giản nhất)
```bash
dotnet run --project DT2_LMS_NHOM6
```
Khi collection `sinhvien` trống, ứng dụng tự tạo validator, index và dữ liệu mẫu (seed cố định 2026 → lần nào cũng ra cùng một bộ dữ liệu, chỉ khác giá trị `_id`/`maDK`).

### Cách 2 – Xóa và tạo lại từ đầu (`--reset`)
```bash
dotnet run --project DT2_LMS_NHOM6 -- --reset
```
Xóa database `eduscore`, tạo lại validator, index, dữ liệu mẫu rồi chạy web. Dùng khi đã sửa nhiều dữ liệu lúc demo và muốn quay về trạng thái ban đầu.

### Cách 3 – Nhập các file JSON (giữ nguyên `_id` như bản nộp)

**Bằng script (khuyên dùng):**
```powershell
powershell -ExecutionPolicy Bypass -File database\khoi-phuc.ps1
```
Script tự chọn: `mongorestore` (nếu có bản dump) → `mongoimport` → `mongosh` → nếu không có công cụ nào thì in hướng dẫn. Tham số tùy chọn: `-Uri mongodb://localhost:27017 -TenDb eduscore`.

**Bằng MongoDB Compass:**
1. Kết nối `mongodb://localhost:27017`, tạo database `eduscore` (collection đầu tiên `sinhvien`).
2. Với từng collection `sinhvien`, `lophoc`, `giangvien`, `monhoc`, `taikhoan`: tạo collection (nếu chưa có) → **Add Data → Import JSON or CSV file** → chọn `database/export/<tên>.json` → **Import**.
3. Chạy ứng dụng một lần (`dotnet run --project DT2_LMS_NHOM6`) để tạo validator `$jsonSchema` và index (dữ liệu đã có nên ứng dụng không seed đè).

**Bằng mongoimport (MongoDB Database Tools):**
```bash
mongoimport --db eduscore --collection sinhvien --file database/export/sinhvien.json --jsonArray --drop
# lặp lại cho lophoc, giangvien, monhoc, taikhoan; sau đó chạy ứng dụng một lần để tạo validator + index
```

> Ghi chú về kiểu số: JSON không phân biệt `8.0` với `8`. `mongoimport` giữ đúng kiểu `double` cho điểm; Compass và mongosh có thể lưu điểm nguyên thành `int` (script `nhap-json.mongosh.js` tự đổi lại về `double`). Validator cho phép cả `int` lẫn `double` ở điểm và pipeline tính như nhau, nên kết quả GPA không đổi (đã kiểm thử 200/200 PASS trên dữ liệu nhập lại).

### Cách 4 – mongorestore từ bản dump (khi có `dump/eduscore.archive.gz`)
```bash
mongorestore --archive=database/dump/eduscore.archive.gz --gzip --drop
```
Khôi phục trọn vẹn dữ liệu **và** validator, index.

---

## Tạo lại bản xuất
```bash
dotnet run --project DT2_LMS_NHOM6 -- --reset --export
```
Ghi lại `export/*.json` từ dữ liệu chuẩn; nếu máy có `mongodump` thì ghi thêm `dump/eduscore.archive.gz`. Bỏ `--reset` để xuất đúng dữ liệu đang có (ví dụ sao lưu trước giờ demo).

### Cài MongoDB Database Tools (để có `mongodump` / `mongorestore` / `mongoimport`)
1. Tải bản Windows `.msi` tại https://www.mongodb.com/try/download/database-tools và cài đặt.
2. Thêm thư mục `C:\Program Files\MongoDB\Tools\100\bin` vào biến môi trường `PATH`, mở lại terminal.
3. Kiểm tra: `mongodump --version`.
