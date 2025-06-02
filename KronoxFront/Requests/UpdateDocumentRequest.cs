namespace KronoxFront.Requests;

public class UpdateDocumentRequest
{
    public int MainCategoryId { get; set; }
    public List<int>? SubCategoryIds { get; set; }
}
