using KronoxApi.Models;
using Microsoft.Extensions.Options;

namespace KronoxApi.Services;

/// <summary>
/// Hjälptjänst för att hämta e‑postmallar och bygga ämne/brödtext
/// för olika utvecklingsförslagsflöden (notifiering, export, svar).
/// </summary>
public class EmailTemplateService
{
    private readonly EmailTemplates _templates;

    public EmailTemplateService(IOptions<EmailTemplates> templates)
    {
        _templates = templates.Value ?? throw new ArgumentNullException(nameof(templates));
    }

    // Hämtar ämne för notifiering om nytt utvecklingsförslag.
    public string GetDevelopmentSuggestionNotificationSubject()
        => _templates.DevelopmentSuggestionNotificationSubject;

    // Bygger brödtext för notifiering om nytt utvecklingsförslag.
    public string GetDevelopmentSuggestionNotification(DevelopmentSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        return _templates.DevelopmentSuggestionNotificationBody
            .Replace("{Organization}", suggestion.Organization ?? "")
            .Replace("{Name}", suggestion.Name ?? "")
            .Replace("{Email}", suggestion.Email ?? "")
            .Replace("{Requirement}", suggestion.Requirement ?? "")
            .Replace("{ExpectedBenefit}", suggestion.ExpectedBenefit ?? "")
            .Replace("{AdditionalInfo}", suggestion.AdditionalInfo ?? "")
            .Replace("{SubmittedAt}", suggestion.SubmittedAt.ToString("yyyy-MM-dd HH:mm"));
    }

    // Bekvämlighetsmetod som returnerar både ämne och brödtext.
    public (string Subject, string Body) BuildDevelopmentSuggestionNotification(DevelopmentSuggestion suggestion)
        => (GetDevelopmentSuggestionNotificationSubject(), GetDevelopmentSuggestionNotification(suggestion));

    // Bygger text för export av utvecklingsförslag (CSV/Markdown/Plaintext beroende på mall).
    public string GetDevelopmentSuggestionExport(DevelopmentSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        return _templates.DevelopmentSuggestionExportTemplate
            .Replace("{Organization}", suggestion.Organization ?? "")
            .Replace("{Name}", suggestion.Name ?? "")
            .Replace("{Email}", suggestion.Email ?? "")
            .Replace("{Requirement}", suggestion.Requirement ?? "")
            .Replace("{ExpectedBenefit}", suggestion.ExpectedBenefit ?? "")
            .Replace("{AdditionalInfo}", suggestion.AdditionalInfo ?? "")
            .Replace("{SubmittedAt}", suggestion.SubmittedAt.ToString("yyyy-MM-dd HH:mm"))
            .Replace("{Status}", suggestion.IsProcessed ? "Behandlad" : "Obehandlad");
    }

    // Bygger URL‑escaped text (t.ex. för mailto) baserat på svarsmallen.
    public string GetDevelopmentSuggestionReply(DevelopmentSuggestion suggestion)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        var requirement = suggestion.Requirement ?? "";
        var requirementPreview = requirement.Length > 50 ? requirement[..50] : requirement;

        return Uri.EscapeDataString(_templates.DevelopmentSuggestionReplyTemplate
            .Replace("{Name}", suggestion.Name ?? "")
            .Replace("{SubmittedAt}", suggestion.SubmittedAt.ToString("yyyy-MM-dd"))
            .Replace("{RequirementPreview}", requirementPreview));
    }
}