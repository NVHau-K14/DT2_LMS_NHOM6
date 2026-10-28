namespace DT2_LMS_NHOM6.Models.Results;

/// <summary>
/// Kết quả chạy một pipeline: dữ liệu + chuỗi JSON pipeline đã chạy + thời gian thực thi.
/// PipelineJson và ThoiGianMs phục vụ nút "Xem pipeline" (A-10, G-04) và trang hiệu năng.
/// </summary>
public class KetQuaPipeline<T>
{
    public List<T> DuLieu { get; init; } = new();

    /// <summary>Pipeline dạng JSON thụt lề, dán thẳng được vào Compass / mongosh.</summary>
    public string PipelineJson { get; init; } = "";

    /// <summary>Thời gian từ lúc gửi aggregate đến lúc nhận đủ kết quả (ms, đo bằng Stopwatch).</summary>
    public double ThoiGianMs { get; init; }
}
