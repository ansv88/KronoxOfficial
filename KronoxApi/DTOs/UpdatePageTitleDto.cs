using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class UpdatePageTitleDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;
}