namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>
/// Dữ liệu cho partial _TrangTrong (trạng thái trống).
/// Cách dùng: &lt;partial name="_TrangTrong" model='new TrangTrongViewModel("Chưa có lớp học")' /&gt;
/// </summary>
public record TrangTrongViewModel(string TieuDe, string? MoTa = null, string BieuTuong = "bi-inbox");
