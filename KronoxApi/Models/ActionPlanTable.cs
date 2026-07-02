using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// Tabell för handlingsplan kopplad till en sida (PageKey).
// Innehåller en sorterad lista av åtgärdsposter (Items).
public class ActionPlanTable
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string PageKey { get; set; } = "";

    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    // Om arkiverade (avslutade) poster ska visas publikt i en separat lista.
    public bool ShowArchivedPublicly { get; set; } = false;

    public virtual ICollection<ActionPlanItem> Items { get; set; } = new List<ActionPlanItem>();
}

// En åtgärdspost i handlingsplanen (rubrik, aktivitet, status, sorteringsordning).
public class ActionPlanItem
{
    public int Id { get; set; }
    public int ActionPlanTableId { get; set; }

    [Required]
    [StringLength(200)]
    public string Module { get; set; } = "";

    [Required]
    [StringLength(1000)]
    public string Activity { get; set; } = "";

    public string DetailedDescription { get; set; } = string.Empty;  // nvarchar(max) – ingen längdbegränsning

    [StringLength(100)]
    public string PlannedDelivery { get; set; } = "";

    [StringLength(100)]
    public string Completed { get; set; } = "";

    public int SortOrder { get; set; }

    // Arkivering (avslutad modul/del). Arkiverade poster visas separat och ingår inte i den aktiva numreringen.
    public bool IsArchived { get; set; } = false;

    public DateTime? ArchivedAt { get; set; }

    public virtual ActionPlanTable ActionPlanTable { get; set; } = null!;

    public virtual ICollection<ActionPlanSubgoal> Subgoals { get; set; } = new List<ActionPlanSubgoal>();     // Delmål kopplade till denna åtgärdspost (modul/del).
}

// Ett delmål under en åtgärdspost (modul/del) i handlingsplanen.
public class ActionPlanSubgoal
{
    public int Id { get; set; }
    public int ActionPlanItemId { get; set; }

    [Required]
    [StringLength(1000)]
    public string Activity { get; set; } = "";

    public string DetailedDescription { get; set; } = string.Empty;  // nvarchar(max) - rich-text via TinyMCE

    [StringLength(100)]
    public string PlannedDelivery { get; set; } = "";

    [StringLength(100)]
    public string Completed { get; set; } = "";

    public int SortOrder { get; set; }

    public virtual ActionPlanItem ActionPlanItem { get; set; } = null!;
}