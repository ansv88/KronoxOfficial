using System.ComponentModel.DataAnnotations;

namespace KronoxFront.DTOs;

public class ActionPlanTableDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = "";
    public List<ActionPlanItemDto> Items { get; set; } = new();
    public List<ActionPlanItemDto> ArchivedItems { get; set; } = new();
    public bool ShowArchivedPublicly { get; set; }
    public DateTime LastModified { get; set; }
}

public class ActionPlanItemDto
{
    public int Id { get; set; }
    public string Module { get; set; } = "";
    public string Activity { get; set; } = "";
    public string DetailedDescription { get; set; } = "";
    public string PlannedDelivery { get; set; } = "";
    public string Completed { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public List<ActionPlanSubgoalDto> Subgoals { get; set; } = new();
}

public class ActionPlanSubgoalDto
{
    public int Id { get; set; }
    public string Activity { get; set; } = "";
    public string DetailedDescription { get; set; } = "";
    public string PlannedDelivery { get; set; } = "";
    public string Completed { get; set; } = "";
    public int SortOrder { get; set; }
}

public class CreateActionPlanItemDto
{
    [Required(ErrorMessage = "Modul/del är obligatorisk")]
    [StringLength(200, ErrorMessage = "Modul/del får vara max 200 tecken")]
    public string Module { get; set; } = "";

    [Required(ErrorMessage = "Aktivitet är obligatorisk")]
    [StringLength(1000, ErrorMessage = "Aktivitet får vara max 1000 tecken")]
    public string Activity { get; set; } = "";

    public string DetailedDescription { get; set; } = "";

    [StringLength(100, ErrorMessage = "Planerad leverans får vara max 100 tecken")]
    public string PlannedDelivery { get; set; } = "";

    [StringLength(100, ErrorMessage = "Avklarad får vara max 100 tecken")]
    public string Completed { get; set; } = "";
}

public class CreateActionPlanSubgoalDto
{
    [Required(ErrorMessage = "Aktivitet är obligatorisk")]
    [StringLength(1000, ErrorMessage = "Aktivitet får vara max 1000 tecken")]
    public string Activity { get; set; } = "";

    public string DetailedDescription { get; set; } = "";

    [StringLength(100, ErrorMessage = "Planerad leverans får vara max 100 tecken")]
    public string PlannedDelivery { get; set; } = "";

    [StringLength(100, ErrorMessage = "Avklarad får vara max 100 tecken")]
    public string Completed { get; set; } = "";
}

// Begäran för att sätta publik synlighet av arkiverade poster.
public class SetShowArchivedDto
{
    public bool ShowArchivedPublicly { get; set; }
}
