namespace KronoxApi.Services;

// Innehåller e-postmallar för olika kontohändelser; godkännande eller avslag
public class EmailTemplates
{
    public string AccountApprovedSubject { get; set; }
    public string AccountApprovedBody { get; set; }
    public string AccountRejectedSubject { get; set; } = "";
    public string AccountRejectedBody { get; set; } = "";
}