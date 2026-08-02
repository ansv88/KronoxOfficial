using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

public class UpdateMetadataDto
{
    [Required(ErrorMessage = "Metadata är obligatoriskt.")]
    public string Metadata { get; set; } = "{}";
}