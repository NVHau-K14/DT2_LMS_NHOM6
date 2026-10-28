using Microsoft.AspNetCore.Razor.TagHelpers;

namespace DT2_LMS_NHOM6.Infrastructure.TagHelpers;

/// <summary>
/// Badge điểm chữ. Cách dùng: &lt;diem-chu gia-tri="@dong.DiemChu" /&gt;
/// Giá trị null (chưa đủ điểm) hiển thị "–".
/// </summary>
[HtmlTargetElement("diem-chu", TagStructure = TagStructure.WithoutEndTag)]
public class DiemChuTagHelper : TagHelper
{
    public string? GiaTri { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"badge badge-diem {HienThi.LopBadgeDiemChu(GiaTri)}");
        output.Content.SetContent(string.IsNullOrEmpty(GiaTri) ? "–" : GiaTri);
    }
}

/// <summary>
/// Badge xếp loại học lực. Cách dùng: &lt;xep-loai gia-tri="@dong.XepLoai" /&gt;
/// </summary>
[HtmlTargetElement("xep-loai", TagStructure = TagStructure.WithoutEndTag)]
public class XepLoaiTagHelper : TagHelper
{
    public string? GiaTri { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"badge badge-xeploai {HienThi.LopBadgeXepLoai(GiaTri)}");
        output.Content.SetContent(string.IsNullOrEmpty(GiaTri) ? "Chưa xếp loại" : GiaTri);
    }
}
