﻿using System.ComponentModel.DataAnnotations;

namespace KronoxApi.DTOs;

// DTO för att representera en huvudkategori.
public class MainCategoryDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kategorinamn krävs")]
    public string Name { get; set; } = string.Empty;
}