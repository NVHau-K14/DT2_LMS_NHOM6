<#
.SYNOPSIS
  Khôi phục database EduScore từ thư mục database/ (Windows PowerShell 5.1 trở lên).

.DESCRIPTION
  Thứ tự ưu tiên:
    1. Có dump/eduscore.archive.gz và mongorestore → mongorestore --archive --gzip --drop (đủ dữ liệu + validator + index).
    2. Có mongoimport → nhập từng file export/<collection>.json (--jsonArray --drop).
    3. Có mongosh     → chạy nhap-json.mongosh.js (đọc cùng các file JSON).
    4. Không có công cụ nào → in hướng dẫn nhập bằng Compass hoặc để ứng dụng tự tạo dữ liệu mẫu.
  Cách 2 và 3 không chép validator/index: chạy ứng dụng một lần (dotnet run --project DT2_LMS_NHOM6)
  để DbInitializer tạo lại (dữ liệu đã có nên không seed đè).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File database\khoi-phuc.ps1
  powershell -ExecutionPolicy Bypass -File database\khoi-phuc.ps1 -Uri mongodb://localhost:27017 -TenDb eduscore
#>
param(
    [string]$Uri = "mongodb://localhost:27017",
    [string]$TenDb = "eduscore"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$thuMuc = $PSScriptRoot
$archive = Join-Path $thuMuc "dump\eduscore.archive.gz"
$thuMucExport = Join-Path $thuMuc "export"
$cacCollection = @("lophoc", "giangvien", "monhoc", "taikhoan", "sinhvien")

function Co-Lenh([string]$ten) { return [bool](Get-Command $ten -ErrorAction SilentlyContinue) }

function Nhac-KhoiTao {
    Write-Host ""
    Write-Host "Tiếp theo: chạy ứng dụng một lần để tạo validator `$jsonSchema và index:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project DT2_LMS_NHOM6"
}

Write-Host "Khôi phục database '$TenDb' tại $Uri" -ForegroundColor Cyan

# 1. mongorestore từ bản dump nhị phân
if ((Test-Path $archive) -and (Co-Lenh "mongorestore")) {
    Write-Host "Dùng mongorestore với $archive"
    # nsFrom/nsTo: cho phép khôi phục vào database tên khác (mặc định vẫn là eduscore)
    & mongorestore --uri="$Uri" --archive="$archive" --gzip --drop `
        --nsFrom="eduscore.*" --nsTo="$TenDb.*"
    if ($LASTEXITCODE -ne 0) { throw "mongorestore lỗi (mã $LASTEXITCODE)." }
    Write-Host "Xong: đã khôi phục dữ liệu, validator và index." -ForegroundColor Green
    exit 0
}

if (-not (Test-Path (Join-Path $thuMucExport "sinhvien.json"))) {
    throw "Không tìm thấy $thuMucExport\sinhvien.json. Hãy chạy: dotnet run --project DT2_LMS_NHOM6 -- --export"
}

# 2. mongoimport từ các file JSON (giữ đúng kiểu double/int)
if (Co-Lenh "mongoimport") {
    Write-Host "Không có bản dump (hoặc thiếu mongorestore) → dùng mongoimport với export\*.json"
    foreach ($ten in $cacCollection) {
        & mongoimport --uri="$Uri" --db="$TenDb" --collection="$ten" `
            --file="$(Join-Path $thuMucExport "$ten.json")" --jsonArray --drop
        if ($LASTEXITCODE -ne 0) { throw "mongoimport $ten lỗi (mã $LASTEXITCODE)." }
    }
    Write-Host "Xong: đã nhập 5 collection." -ForegroundColor Green
    Nhac-KhoiTao
    exit 0
}

# 3. mongosh đọc các file JSON
if (Co-Lenh "mongosh") {
    Write-Host "Không có MongoDB Database Tools → dùng mongosh với export\*.json"
    $env:EDUSCORE_DB = $TenDb
    Push-Location $thuMuc
    try {
        & mongosh "$Uri" --quiet --file "nhap-json.mongosh.js"
        if ($LASTEXITCODE -ne 0) { throw "mongosh lỗi (mã $LASTEXITCODE)." }
    }
    finally { Pop-Location }
    Write-Host "Xong." -ForegroundColor Green
    Nhac-KhoiTao
    exit 0
}

# 4. Không có công cụ nào
Write-Host "Máy không có mongorestore, mongoimport hay mongosh. Chọn một trong các cách:" -ForegroundColor Yellow
Write-Host "  a) MongoDB Compass: tạo database '$TenDb', với mỗi collection lophoc, giangvien, monhoc, taikhoan, sinhvien:"
Write-Host "     Add Data → Import JSON or CSV file → chọn database\export\<collection>.json → Import."
Write-Host "  b) Để ứng dụng tự tạo lại dữ liệu mẫu (cùng nội dung, khác _id):"
Write-Host "     dotnet run --project DT2_LMS_NHOM6 -- --reset"
Write-Host "  c) Cài MongoDB Database Tools: https://www.mongodb.com/try/download/database-tools rồi chạy lại script này."
exit 1
