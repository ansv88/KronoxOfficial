namespace KronoxApi.Models;

public class EmailTemplates
{
    public string AccountApprovedSubject { get; set; } = "";
    public string AccountApprovedBody { get; set; } = "";
    public string DevelopmentSuggestionNotificationSubject { get; set; } = "";
    public string DevelopmentSuggestionNotificationBody { get; set; } = "";
    public string DevelopmentSuggestionExportTemplate { get; set; } = "";
    public string DevelopmentSuggestionReplyTemplate { get; set; } = "";
}