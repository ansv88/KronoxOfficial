using Microsoft.AspNetCore.Components.Forms;
using KronoxFront.ViewModels;

namespace KronoxFront.ViewModels.Faq;

public class FaqImageUploadInfo
{
    public IBrowserFile? File { get; set; }
    public int SectionIndex { get; set; }
    public int ItemIndex { get; set; }
    public FaqItemViewModel? Item { get; set; }
}