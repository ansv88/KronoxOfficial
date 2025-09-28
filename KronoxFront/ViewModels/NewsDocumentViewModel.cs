namespace KronoxFront.ViewModels;

public class NewsDocumentViewModel
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int SortOrder { get; set; }

    public string FormattedFileSize => FormatFileSize(FileSize);

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1073741824)
            return $"{bytes / 1073741824.0:F2} GB";
        if (bytes >= 1048576)
            return $"{bytes / 1048576.0:F2} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes} B";
    }
}