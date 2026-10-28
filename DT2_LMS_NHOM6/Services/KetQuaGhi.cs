namespace DT2_LMS_NHOM6.Services;

/// <summary>
/// Kết quả một thao tác ghi (thêm / sửa / xóa / reset...) trả về cho controller:
/// controller chỉ việc hiện toast (ThongBao + MongoCommand) hoặc gắn lỗi vào đúng ô nhập (TruongLoi).
/// </summary>
public class KetQuaGhi
{
    public bool ThanhCong { get; init; }
    public string ThongBao { get; init; } = "";

    /// <summary>Tên thuộc tính của form bị lỗi (ví dụ "MaSV"); null = lỗi chung.</summary>
    public string? TruongLoi { get; init; }

    /// <summary>Chuỗi lệnh dạng mongosh đã chạy (thiết kế mục 4.9) – chỉ có ở thao tác ghi trên mảng / lan truyền.</summary>
    public string? MongoCommand { get; init; }

    public static KetQuaGhi Thanh(string thongBao, string? mongoCommand = null)
        => new() { ThanhCong = true, ThongBao = thongBao, MongoCommand = mongoCommand };

    public static KetQuaGhi Loi(string thongBao, string? truongLoi = null)
        => new() { ThanhCong = false, ThongBao = thongBao, TruongLoi = truongLoi };
}
