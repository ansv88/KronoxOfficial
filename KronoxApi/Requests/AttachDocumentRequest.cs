namespace KronoxApi.Requests;

public class AttachDocumentRequest
{
    public int DocumentId { get; set; }
    public string? DisplayName { get; set; }
    public int SortOrder { get; set; } = 0;
}