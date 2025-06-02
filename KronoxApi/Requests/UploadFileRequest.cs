using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request för uppladdning av en fil med tillhörande kategoriinformation
public class UploadFileRequest
{
    [FromForm]
    [Required(ErrorMessage = "Fil måste väljas")]
    public IFormFile File { get; set; }

    [FromForm]
    [Required(ErrorMessage = "Huvudkategori måste väljas")]
    public int MainCategoryId { get; set; }
    [FromForm]
    public List<int>? SubCategoryIds { get; set; }
}