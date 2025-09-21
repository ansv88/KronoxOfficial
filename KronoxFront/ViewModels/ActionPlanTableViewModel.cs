using System.ComponentModel.DataAnnotations;

namespace KronoxFront.ViewModels;

public class ActionPlanTableViewModel
{
    public int Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public List<ActionPlanItem> Items { get; set; } = new();
    public DateTime LastModified { get; set; } = DateTime.Now;
}

public class ActionPlanItem
{
    public int Id { get; set; }
    public int Priority { get; set; }
    
    [Required(ErrorMessage = "Modul/del �r obligatorisk")]
    [StringLength(200, ErrorMessage = "Modul/del f�r vara max 200 tecken")]
    public string Module { get; set; } = string.Empty;

    [Required(ErrorMessage = "Aktivitet �r obligatorisk")]
    [StringLength(1000, ErrorMessage = "Aktivitet f�r vara max 1000 tecken")]
    public string Activity { get; set; } = string.Empty;

    public string DetailedDescription { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Planerad leverans f�r vara max 100 tecken")]
    public string PlannedDelivery { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Avklarad f�r vara max 100 tecken")]
    public string Completed { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}