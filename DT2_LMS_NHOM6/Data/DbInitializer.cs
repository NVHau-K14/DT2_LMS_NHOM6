using MongoDB.Bson;
using MongoDB.Driver;

namespace DT2_LMS_NHOM6.Data;

/// <summary>
/// Khởi tạo database lúc ứng dụng khởi động (gọi trước app.Run):
///   1. --reset → drop database.
///   2. Tạo collection kèm validator $jsonSchema (đã có → collMod cập nhật validator).
///   3. Tạo index (thiết kế mục 4.7).
///   4. sinhvien trống → chạy Seeder.
/// </summary>
public static class DbInitializer
{
    public static async Task KhoiTaoAsync(IServiceProvider services, bool reset)
    {
        var context = services.GetRequiredService<MongoContext>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));
        var db = context.Database;

        if (reset)
        {
            logger.LogWarning("--reset: xóa database {Ten}", db.DatabaseNamespace.DatabaseName);
            await context.Client.DropDatabaseAsync(db.DatabaseNamespace.DatabaseName);
        }

        await TaoValidatorAsync(db, MongoContext.TenSinhVien, ValidatorSinhVien);
        await TaoValidatorAsync(db, MongoContext.TenLopHoc, ValidatorLopHoc);
        await TaoValidatorAsync(db, MongoContext.TenGiangVien, ValidatorGiangVien);
        await TaoValidatorAsync(db, MongoContext.TenMonHoc, ValidatorMonHoc);
        await TaoValidatorAsync(db, MongoContext.TenTaiKhoan, ValidatorTaiKhoan);

        await TaoIndexAsync(db);

        var soSinhVien = await context.SinhVien.CountDocumentsAsync(FilterDefinition<Models.Documents.SinhVien>.Empty);
        if (soSinhVien == 0)
        {
            logger.LogInformation("Collection sinhvien trống → chạy Seeder");
            await new Seeder(context, logger).ChayAsync();
        }
        else
        {
            logger.LogInformation("Database đã có {SoSV} sinh viên → bỏ qua seed", soSinhVien);
        }
    }

    /// <summary>
    /// Collection chưa có → lệnh create kèm validator; đã có → lệnh collMod thay validator.
    /// Tương đương mongosh: db.runCommand({ create|collMod: ten, validator: {...}, validationLevel: "strict", validationAction: "error" })
    /// </summary>
    private static async Task TaoValidatorAsync(IMongoDatabase db, string tenCollection, string validatorJson)
    {
        var danhSach = await (await db.ListCollectionNamesAsync()).ToListAsync();
        var lenh = danhSach.Contains(tenCollection) ? "collMod" : "create";

        var command = new BsonDocument
        {
            { lenh, tenCollection },
            { "validator", BsonDocument.Parse(validatorJson) },
            { "validationLevel", "strict" },
            { "validationAction", "error" }
        };
        await db.RunCommandAsync<BsonDocument>(command);
    }

    /// <summary>Tạo index đúng thiết kế mục 4.7. createIndexes không lỗi nếu index đã tồn tại cùng tên + cùng khóa.</summary>
    private static async Task TaoIndexAsync(IMongoDatabase db)
    {
        static CreateIndexModel<BsonDocument> Index(BsonDocument khoa, string ten, bool unique = false) =>
            new(khoa, new CreateIndexOptions { Name = ten, Unique = unique });

        await db.GetCollection<BsonDocument>(MongoContext.TenSinhVien).Indexes.CreateManyAsync(new[]
        {
            Index(new BsonDocument("maSV", 1), "ux_maSV", unique: true),
            Index(new BsonDocument("maLop", 1), "ix_maLop"),
            // Phục vụ tìm lớp học phần của giảng viên theo học kỳ (multikey index trên mảng monHoc)
            Index(new BsonDocument { { "monHoc.giangVien.maGV", 1 }, { "monHoc.namHoc", 1 }, { "monHoc.hocKy", 1 } },
                  "ix_monHoc_maGV_namHoc_hocKy"),
            Index(new BsonDocument("monHoc.maMon", 1), "ix_monHoc_maMon"),
            Index(new BsonDocument("hoTen", 1), "ix_hoTen"),
        });

        await db.GetCollection<BsonDocument>(MongoContext.TenLopHoc).Indexes.CreateOneAsync(
            Index(new BsonDocument("maLop", 1), "ux_maLop", unique: true));

        await db.GetCollection<BsonDocument>(MongoContext.TenGiangVien).Indexes.CreateOneAsync(
            Index(new BsonDocument("maGV", 1), "ux_maGV", unique: true));

        await db.GetCollection<BsonDocument>(MongoContext.TenMonHoc).Indexes.CreateOneAsync(
            Index(new BsonDocument("maMon", 1), "ux_maMon", unique: true));

        await db.GetCollection<BsonDocument>(MongoContext.TenTaiKhoan).Indexes.CreateManyAsync(new[]
        {
            Index(new BsonDocument("tenDangNhap", 1), "ux_tenDangNhap", unique: true),
            Index(new BsonDocument { { "vaiTro", 1 }, { "maThamChieu", 1 } }, "ix_vaiTro_maThamChieu"),
        });
    }

    // ---------------- Validator $jsonSchema ----------------

    /// <summary>Validator sinhvien – đúng thiết kế mục 4.6. Mảng monHoc KHÔNG giới hạn số phần tử.</summary>
    private const string ValidatorSinhVien = """
    { "$jsonSchema": {
      "bsonType": "object",
      "required": ["maSV","hoTen","ngaySinh","gioiTinh","maLop","trangThai","monHoc"],
      "properties": {
        "maSV":        { "bsonType": "string", "pattern": "^[0-9]{10}$" },
        "hoTen":       { "bsonType": "string", "minLength": 2, "maxLength": 100 },
        "ngaySinh":    { "bsonType": "date" },
        "gioiTinh":    { "enum": ["Nam","Nữ"] },
        "email":       { "bsonType": ["string","null"] },
        "soDienThoai": { "bsonType": ["string","null"] },
        "diaChi":      { "bsonType": ["string","null"] },
        "maLop":       { "bsonType": "string" },
        "trangThai":   { "enum": ["Đang học","Bảo lưu","Thôi học"] },
        "monHoc": { "bsonType": "array", "items": {
          "bsonType": "object",
          "required": ["maDK","maMon","tenMon","soTinChi","namHoc","hocKy","lanHoc","hinhThuc","giangVien","trongSo","diem"],
          "properties": {
            "maDK":     { "bsonType": "objectId" },
            "maMon":    { "bsonType": "string" },
            "tenMon":   { "bsonType": "string" },
            "soTinChi": { "bsonType": "int", "minimum": 1, "maximum": 10 },
            "namHoc":   { "bsonType": "string", "pattern": "^[0-9]{4}-[0-9]{4}$" },
            "hocKy":    { "bsonType": "int", "enum": [1,2,3] },
            "lanHoc":   { "bsonType": "int", "minimum": 1 },
            "hinhThuc": { "enum": ["Học mới","Học lại","Cải thiện"] },
            "giangVien": { "bsonType": "object", "required": ["maGV","hoTen"],
                           "properties": { "maGV": { "bsonType": "string" }, "hoTen": { "bsonType": "string" } } },
            "trongSo": { "bsonType": "object", "required": ["cc","gk","ck"], "properties": {
                           "cc": { "bsonType": "int", "minimum": 0, "maximum": 100 },
                           "gk": { "bsonType": "int", "minimum": 0, "maximum": 100 },
                           "ck": { "bsonType": "int", "minimum": 0, "maximum": 100 } } },
            "diem": { "bsonType": "object", "required": ["cc","gk","ck"], "properties": {
                           "cc": { "bsonType": ["double","int","null"], "minimum": 0, "maximum": 10 },
                           "gk": { "bsonType": ["double","int","null"], "minimum": 0, "maximum": 10 },
                           "ck": { "bsonType": ["double","int","null"], "minimum": 0, "maximum": 10 } } },
            "capNhatLuc": { "bsonType": ["date","null"] },
            "capNhatBoi": { "bsonType": ["string","null"] }
          } } }
      } } }
    """;

    private const string ValidatorLopHoc = """
    { "$jsonSchema": {
      "bsonType": "object",
      "required": ["maLop","tenLop","nienKhoa","nganh","khoa"],
      "properties": {
        "maLop":    { "bsonType": "string" },
        "tenLop":   { "bsonType": "string" },
        "nienKhoa": { "bsonType": "string", "pattern": "^[0-9]{4}-[0-9]{4}$" },
        "nganh":    { "bsonType": "string" },
        "khoa":     { "bsonType": "string" },
        "coVanHocTap": { "bsonType": ["object","null"], "required": ["maGV","hoTen"],
                         "properties": { "maGV": { "bsonType": "string" }, "hoTen": { "bsonType": "string" } } },
        "ngayTao":  { "bsonType": "date" }
      } } }
    """;

    private const string ValidatorGiangVien = """
    { "$jsonSchema": {
      "bsonType": "object",
      "required": ["maGV","hoTen","hocVi","khoa"],
      "properties": {
        "maGV":        { "bsonType": "string" },
        "hoTen":       { "bsonType": "string", "minLength": 2, "maxLength": 100 },
        "hocVi":       { "enum": ["CN.","ThS.","TS.","PGS.TS."] },
        "email":       { "bsonType": ["string","null"] },
        "soDienThoai": { "bsonType": ["string","null"] },
        "khoa":        { "bsonType": "string" },
        "ngayTao":     { "bsonType": "date" }
      } } }
    """;

    private const string ValidatorMonHoc = """
    { "$jsonSchema": {
      "bsonType": "object",
      "required": ["maMon","tenMon","soTinChi","trongSoMacDinh"],
      "properties": {
        "maMon":    { "bsonType": "string" },
        "tenMon":   { "bsonType": "string" },
        "soTinChi": { "bsonType": "int", "minimum": 1, "maximum": 10 },
        "trongSoMacDinh": { "bsonType": "object", "required": ["cc","gk","ck"], "properties": {
                              "cc": { "bsonType": "int", "minimum": 0, "maximum": 100 },
                              "gk": { "bsonType": "int", "minimum": 0, "maximum": 100 },
                              "ck": { "bsonType": "int", "minimum": 0, "maximum": 100 } } },
        "khoa":     { "bsonType": "string" },
        "ngayTao":  { "bsonType": "date" }
      } } }
    """;

    private const string ValidatorTaiKhoan = """
    { "$jsonSchema": {
      "bsonType": "object",
      "required": ["tenDangNhap","matKhauHash","vaiTro","maThamChieu","hoTen","trangThai"],
      "properties": {
        "tenDangNhap":     { "bsonType": "string" },
        "matKhauHash":     { "bsonType": "string" },
        "vaiTro":          { "enum": ["Admin","GiangVien","SinhVien"] },
        "maThamChieu":     { "bsonType": "string" },
        "hoTen":           { "bsonType": "string" },
        "trangThai":       { "enum": ["Hoạt động","Khóa"] },
        "lanDangNhapCuoi": { "bsonType": ["date","null"] },
        "ngayTao":         { "bsonType": "date" }
      } } }
    """;
}
