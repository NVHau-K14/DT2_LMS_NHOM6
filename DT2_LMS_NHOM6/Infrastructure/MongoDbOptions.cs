namespace DT2_LMS_NHOM6.Infrastructure;

/// <summary>Cấu hình kết nối MongoDB – đọc từ mục "MongoDB" trong appsettings.json.</summary>
public class MongoDbOptions
{
    public const string TenMuc = "MongoDB";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "eduscore";
}
