﻿using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Representerar en sektion med innehåll och eventuell bild på en sida (t.ex. startsidans feature-sektion).
public class FeatureSection
{
    public int Id { get; set; }

    [Required]
    public string PageKey { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ImageAltText { get; set; } = string.Empty;
    public bool HasImage { get; set; } = true;
    public int SortOrder { get; set; }
}