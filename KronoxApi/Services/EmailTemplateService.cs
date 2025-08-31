using Microsoft.Extensions.Options;
using KronoxApi.Models;

namespace KronoxApi.Services;

public class EmailTemplateService
{
    private readonly EmailTemplates _templates;

    public EmailTemplateService(IOptions<EmailTemplates> templates)
    {
        _templates = templates.Value;
    }

    public string GetDevelopmentSuggestionNotification(DevelopmentSuggestion suggestion)
    {
        return _templates.DevelopmentSuggestionNotificationBody
            .Replace("{Organization}", suggestion.Organization)
            .Replace("{Name}", suggestion.Name)
            .Replace("{Email}", suggestion.Email)
            .Replace("{Requirement}", suggestion.Requirement)
            .Replace("{ExpectedBenefit}", suggestion.ExpectedBenefit)
            .Replace("{AdditionalInfo}", suggestion.AdditionalInfo)
            .Replace("{SubmittedAt}", suggestion.SubmittedAt.ToString("yyyy-MM-dd HH:mm"));
    }

    public string GetDevelopmentSuggestionExport(DevelopmentSuggestion suggestion)
    {
        return _templates.DevelopmentSuggestionExportTemplate
            .Replace("{Organization}", suggestion.Organization)
            .Replace("{Name}", suggestion.Name)
            .Replace("{Email}", suggestion.Email)
            .Replace("{Requirement}", suggestion.Requirement)
            .Replace("{ExpectedBenefit}", suggestion.ExpectedBenefit)
            .Replace("{AdditionalInfo}", suggestion.AdditionalInfo)
            .Replace("{SubmittedAt}", suggestion.SubmittedAt.ToString("yyyy-MM-dd HH:mm"))
            .Replace("{Status}", suggestion.IsProcessed ? "Behandlad" : "Obehandlad");
    }

    public string GetDevelopmentSuggestionReply(DevelopmentSuggestion suggestion)
    {
        var requirementPreview = suggestion.Requirement.Length > 50 
            ? suggestion.Requirement.Substring(0, 50) 
            : suggestion.Requirement;

        return Uri.EscapeDataString(_templates.DevelopmentSuggestionReplyTemplate
            .Replace("{Name}", suggestion.Name)
            .Replace("{SubmittedAt}", suggestion.SubmittedAt.ToString("yyyy-MM-dd"))
            .Replace("{RequirementPreview}", requirementPreview));
    }
}