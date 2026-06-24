using Mathy.ELM.Core.Entities;

namespace Mathy.ELM.Core.Services;

/// <summary>
/// Service for building email content from EmailTemplate and field data
/// </summary>
public interface IEmailTemplateBuilderService
{
    /// <summary>
    /// Builds an HTML-formatted email body from a template's Body field (comma-delimited ContentCodes)
    /// Maps ContentCodes to ContentFields using the provided mapping, then retrieves values from fieldData
    /// </summary>
    /// <param name="template">The email template containing the Body field with comma-delimited ContentCodes</param>
    /// <param name="fieldData">Dictionary mapping ContentField names (like "startdate") to their values</param>
    /// <param name="contentFieldMappings">Dictionary mapping ContentCodes (like "START-DATE") to ContentField names (like "startdate")</param>
    /// <param name="contentLabelMappings">Dictionary mapping ContentCodes (like "START-DATE") to ContentLabels (like "Start Date")</param>
    /// <returns>HTML-formatted email body with actual values populated</returns>
    string BuildEmailBodyFromTemplate(
        EmailTemplate template,
        Dictionary<string, string> fieldData,
        Dictionary<string, string> contentFieldMappings,
        Dictionary<string, string> contentLabelMappings);

    /// <summary>
    /// DEPRECATED: Use BuildEmailBodyFromTemplate(EmailTemplate, Dictionary, Dictionary) instead
    /// Builds an HTML-formatted email body from a template's Body field using case-insensitive matching
    /// This overload is kept for backward compatibility only and should not be used in new code
    /// </summary>
    /// <param name="template">The email template containing the Body field</param>
    /// <param name="fieldData">Dictionary mapping field names to their values</param>
    /// <returns>HTML-formatted email body (may have N/A values if matching fails)</returns>
    [Obsolete("Use BuildEmailBodyFromTemplate with contentFieldMappings parameter instead", false)]
    string BuildEmailBodyFromTemplate(EmailTemplate template, Dictionary<string, string> fieldData);

    /// <summary>
    /// Replaces placeholders in the email subject with actual values
    /// Supports placeholders like {{EmployeeName}}, {{StartDate}}, etc.
    /// </summary>
    /// <param name="subject">The subject line with placeholders</param>
    /// <param name="fieldData">Dictionary mapping field names to their values</param>
    /// <returns>Subject with placeholders replaced by actual values</returns>
    string ReplaceSubjectPlaceholders(string subject, Dictionary<string, string> fieldData);
}
