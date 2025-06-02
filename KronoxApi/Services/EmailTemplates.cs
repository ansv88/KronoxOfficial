namespace KronoxApi.Services;

// Inneh�ller e-postmallar f�r olika kontoh�ndelser; godk�nnande eller avslag
public class EmailTemplates
{
    public string AccountApprovedSubject { get; set; }
    public string AccountApprovedBody { get; set; }
    public string AccountRejectedSubject { get; set; } = "";
    public string AccountRejectedBody { get; set; } = "";
}