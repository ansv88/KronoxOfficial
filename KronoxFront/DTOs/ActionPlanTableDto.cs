namespace KronoxFront.DTOs;

public class ActionPlanTableDto
{
    public int Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public List<ActionPlanItemDto> Items { get; set; } = new();
    public DateTime LastModified { get; set; }
}

public class ActionPlanItemDto
{
    public int Id { get; set; }
    public int Priority { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Activity { get; set; } = string.Empty;
    public string PlannedDelivery { get; set; } = string.Empty;
    public string Completed { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}