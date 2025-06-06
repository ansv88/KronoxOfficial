﻿using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Requests;

// Request-modell för att skapa en ny nyhet.
public class CreateNewsRequest
{
    [Required(ErrorMessage = "Titeln är obligatorisk.")]
    [StringLength(200, ErrorMessage = "Titeln får inte överstiga 200 tecken.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Innehållet är obligatoriskt.")]
    public string Content { get; set; } = string.Empty; // HTML-innehåll från TinyMCE

    public bool IsArchived { get; set; } = false;
}