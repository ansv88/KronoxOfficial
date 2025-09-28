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

    public virtual ICollection<ActionPlanItem> Items { get; set; } = new List<ActionPlanItem>();
}

// En åtgärdspost i handlingsplanen (rubrik, aktivitet, status, sorteringsordning).
public class ActionPlanItem
{
    public int Id { get; set; }
    public int ActionPlanTableId { get; set; }
    public int Priority { get; set; }

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

    public virtual ActionPlanTable ActionPlanTable { get; set; } = null!;
}