namespace KronoxFront.Requests;

// Request för att uppdatera dokumentets kategorier

public class UpdateDocumentRequest
{
    public int MainCategoryId { get; set; }
    public List<int>? SubCategoryIds { get; set; }
}