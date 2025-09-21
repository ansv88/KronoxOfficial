using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

public class ActionPlanTableDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = "";
    public List<ActionPlanItemDto> Items { get; set; } = new();
    public DateTime LastModified { get; set; }
}

public class ActionPlanItemDto
{
    public int Id { get; set; }
    public int Priority { get; set; }
    public string Module { get; set; } = "";
    public string Activity { get; set; } = "";
    public string DetailedDescription { get; set; } = "";
    public string PlannedDelivery { get; set; } = "";
    public string Completed { get; set; } = "";
    public int SortOrder { get; set; }
}

public class CreateActionPlanItemDto
{
    [Required(ErrorMessage = "Modul/del �r obligatorisk")]
    [StringLength(200, ErrorMessage = "Modul/del f�r vara max 200 tecken")]
    public string Module { get; set; } = "";

    [Required(ErrorMessage = "Aktivitet �r obligatorisk")]
    [StringLength(1000, ErrorMessage = "Aktivitet f�r vara max 1000 tecken")]
    public string Activity { get; set; } = "";

    [StringLength(1000, ErrorMessage = "Detaljerad beskrivning f�r vara max 1000 tecken")]
    public string DetailedDescription { get; set; } = "";

    [StringLength(100, ErrorMessage = "Planerad leverans f�r vara max 100 tecken")]
    public string PlannedDelivery { get; set; } = "";

    [StringLength(100, ErrorMessage = "Avklarad f�r vara max 100 tecken")]
    public string Completed { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Prioritet m�ste vara st�rre �n 0")]
    public int Priority { get; set; }
}