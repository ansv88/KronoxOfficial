using Microsoft.AspNetCore.Components.Forms;

namespace KronoxFront.ViewModels.Features;

public class ImageUploadInfo
{
    public IBrowserFile? File { get; set; }
    public int SectionIndex { get; set; }
    public FeatureSectionViewModel? Section { get; set; }
    public Action? CloseModal { get; set; }
}