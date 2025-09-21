using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

public class ActionPlanTable
{
    public int Id { get; set; }
    public string PageKey { get; set; } = "";
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    
    public virtual ICollection<ActionPlanItem> Items { get; set; } = new List<ActionPlanItem>();
}

public class ActionPlanItem
{
    public int Id { get; set; }
    public int ActionPlanTableId { get; set; }
    public int Priority { get; set; }
    public string Module { get; set; } = "";
    public string Activity { get; set; } = "";
    public string DetailedDescription { get; set; } = string.Empty;
    public string PlannedDelivery { get; set; } = "";
    public string Completed { get; set; } = "";
    public int SortOrder { get; set; }
    
    public virtual ActionPlanTable ActionPlanTable { get; set; } = null!;
}