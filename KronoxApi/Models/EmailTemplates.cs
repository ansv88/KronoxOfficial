using System.ComponentModel.DataAnnotations;

namespace KronoxApi.Models;

// E‑postmallar för notifieringar och export/reply av utvecklingsförslag. Konfigureras via appsettings
public class EmailTemplates
{
    // Ämne när ett konto godkänts.
    // Platshållare: {RoleName}
    [Required(ErrorMessage = "AccountApprovedSubject måste anges i konfigurationen (EmailTemplates).")]
    public string AccountApprovedSubject { get; set; } = "";

    // Brödtext när ett konto godkänts.
    // Platshållare: {FirstName}, {RoleName}
    [Required(ErrorMessage = "AccountApprovedBody måste anges i konfigurationen (EmailTemplates).")]
    public string AccountApprovedBody { get; set; } = "";

    // Ämne för notifiering av nytt utvecklingsförslag
    [Required(ErrorMessage = "DevelopmentSuggestionNotificationSubject måste anges i konfigurationen (EmailTemplates).")]
    public string DevelopmentSuggestionNotificationSubject { get; set; } = "";

    // Brödtext för notifiering om nytt utvecklingsförslag.
    // Platshållare: {Organization}, {Name}, {Email}, {Requirement}, {ExpectedBenefit}, {AdditionalInfo}, {SubmittedAt}
    [Required(ErrorMessage = "DevelopmentSuggestionNotificationBody måste anges i konfigurationen (EmailTemplates).")]
    public string DevelopmentSuggestionNotificationBody { get; set; } = "";

    // Mall för export av utvecklingsförslag (t.ex. CSV/Markdown/Plaintext).
    // Platshållare: {Organization}, {Name}, {Email}, {Requirement}, {ExpectedBenefit}, {AdditionalInfo}, {SubmittedAt}, {Status}
    [Required(ErrorMessage = "DevelopmentSuggestionExportTemplate måste anges i konfigurationen (EmailTemplates).")]
    public string DevelopmentSuggestionExportTemplate { get; set; } = "";

    // Mall för svarslänk/ämne/brödtext (beroende på användning) vid svar på utvecklingsförslag.
    // Platshållare: {Name}, {SubmittedAt}, {RequirementPreview}
    [Required(ErrorMessage = "DevelopmentSuggestionReplyTemplate måste anges i konfigurationen (EmailTemplates).")]
    public string DevelopmentSuggestionReplyTemplate { get; set; } = "";
}