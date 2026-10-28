namespace DT2_LMS_NHOM6.ViewModels;

/// <summary>
/// Dữ liệu cho partial _ChonFile (ô chọn file Excel nhãn tiếng Việt).
/// Cách dùng: &lt;partial name="_ChonFile" model='new ChonFileViewModel("tep-sv", "File danh sách sinh viên")' /&gt;
/// </summary>
public record ChonFileViewModel(string Id, string MoTa);
