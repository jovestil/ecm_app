using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Mathy.ELM.Infrastructure.Services;

/// <summary>
/// Service for building email content from EmailTemplate and field data
/// </summary>
public class EmailTemplateBuilderService : IEmailTemplateBuilderService
{
    private readonly ILogger<EmailTemplateBuilderService> _logger;

    public EmailTemplateBuilderService(ILogger<EmailTemplateBuilderService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Builds an HTML-formatted email body from a template's Body field (comma-delimited ContentCodes)
    /// Maps ContentCodes to ContentFields, retrieves values from field data, and generates HTML
    ///
    /// Process:
    /// 1. Parse Body field to get ContentCodes (e.g., "START-DATE,EMPLOYEE-NAME")
    /// 2. Use contentFieldMappings to map each ContentCode to its ContentField (e.g., "START-DATE" → "startdate")
    /// 3. Look up ContentField value in fieldData dictionary
    /// 4. Use contentLabelMappings to get user-friendly labels (e.g., "START-DATE" → "Start Date")
    /// 5. Generate HTML table with field labels and values
    /// </summary>
    public string BuildEmailBodyFromTemplate(
        EmailTemplate template,
        Dictionary<string, string> fieldData,
        Dictionary<string, string> contentFieldMappings,
        Dictionary<string, string> contentLabelMappings)
    {
        // Parse the Body field to get the list of ContentCodes
        var contentCodes = template.Body
            .Split(',')
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList();

        if (!contentCodes.Any())
        {
            _logger.LogWarning("Template {TemplateName} has empty Body field", template.TemplateName);
            return "<p>No fields defined in template.</p>";
        }

        // Build HTML formatted email body
        var htmlBuilder = new StringBuilder();

        // Email header
        htmlBuilder.AppendLine("<div style='font-family: Arial, sans-serif; font-size: 14px; color: #333;'>");

        // Greeting (optional, can be customized)
        htmlBuilder.AppendLine("<p>Hello,</p>");
        htmlBuilder.AppendLine("<p>Please find the details below:</p>");
        htmlBuilder.AppendLine("<br />");

        // Field list
        htmlBuilder.AppendLine("<table style='border-collapse: collapse; width: 100%; max-width: 600px;'>");

        foreach (var contentCode in contentCodes)
        {
            var normalizedContentCode = contentCode.Trim();
            var value = "N/A";

            // Step 1: Map ContentCode to ContentField using contentFieldMappings
            if (contentFieldMappings.TryGetValue(normalizedContentCode, out var contentFieldKey))
            {
                // Step 2: Try to find the value in fieldData using the ContentField name
                if (fieldData.TryGetValue(contentFieldKey, out var fieldValue))
                {
                    value = fieldValue ?? "N/A";
                    _logger.LogDebug("[EMAIL BUILDER] ContentCode '{ContentCode}' mapped to ContentField '{ContentField}' with value '{Value}'",
                        normalizedContentCode, contentFieldKey, value);
                }
                else
                {
                    _logger.LogWarning("[EMAIL BUILDER] ContentField '{ContentField}' (from ContentCode '{ContentCode}') not found in field data for template {TemplateName}",
                        contentFieldKey, normalizedContentCode, template.TemplateName);
                }
            }
            else
            {
                _logger.LogWarning("[EMAIL BUILDER] ContentCode '{ContentCode}' not found in ContentField mappings for template {TemplateName}",
                    normalizedContentCode, template.TemplateName);
            }

            // Get the user-friendly label from contentLabelMappings, fallback to ContentCode if not found
            var displayLabel = contentLabelMappings.TryGetValue(normalizedContentCode, out var label) && !string.IsNullOrEmpty(label)
                ? label
                : normalizedContentCode;

            // Check if this is a field that contains HTML (bullet lists, etc.)
            var isHtmlContent = value.Contains("<ul>") || value.Contains("<ol>") || value.Contains("<li>");

            if (isHtmlContent)
            {
                // Close the table, add the HTML content, then reopen the table
                htmlBuilder.AppendLine("</table>");
                htmlBuilder.AppendLine($"<p style='font-weight: bold; margin-top: 12px;'>{displayLabel}:</p>");
                htmlBuilder.AppendLine(value);
                htmlBuilder.AppendLine("<table style='border-collapse: collapse; width: 100%; max-width: 600px;'>");
            }
            else
            {
                // Add row to table for regular field values
                htmlBuilder.AppendLine("<tr>");
                htmlBuilder.AppendLine($"  <td style='padding: 8px; border-bottom: 1px solid #ddd; font-weight: bold; width: 40%;'>{displayLabel}:</td>");
                htmlBuilder.AppendLine($"  <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{value}</td>");
                htmlBuilder.AppendLine("</tr>");
            }
        }

        htmlBuilder.AppendLine("</table>");
        htmlBuilder.AppendLine("<br />");

        // Footer
        htmlBuilder.AppendLine("<p>Thank you.</p>");
        htmlBuilder.AppendLine("</div>");

        return htmlBuilder.ToString();
    }

    /// <summary>
    /// DEPRECATED: Use BuildEmailBodyFromTemplate(EmailTemplate, Dictionary, Dictionary) instead
    /// This overload is kept for backward compatibility only
    /// </summary>
    [Obsolete("Use BuildEmailBodyFromTemplate with contentFieldMappings parameter instead", false)]
    public string BuildEmailBodyFromTemplate(EmailTemplate template, Dictionary<string, string> fieldData)
    {
        // Fallback: try case-insensitive matching as before
        _logger.LogWarning("[EMAIL BUILDER] Using deprecated BuildEmailBodyFromTemplate overload without contentFieldMappings for template {TemplateName}",
            template.TemplateName);

        var contentCodes = template.Body
            .Split(',')
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList();

        if (!contentCodes.Any())
        {
            _logger.LogWarning("Template {TemplateName} has empty Body field", template.TemplateName);
            return "<p>No fields defined in template.</p>";
        }

        var htmlBuilder = new StringBuilder();
        htmlBuilder.AppendLine("<div style='font-family: Arial, sans-serif; font-size: 14px; color: #333;'>");
        htmlBuilder.AppendLine("<p>Hello,</p>");
        htmlBuilder.AppendLine("<p>Please find the details below:</p>");
        htmlBuilder.AppendLine("<br />");
        htmlBuilder.AppendLine("<table style='border-collapse: collapse; width: 100%; max-width: 600px;'>");

        foreach (var fieldName in contentCodes)
        {
            var normalizedFieldName = fieldName.Trim();
            var value = "N/A";

            // Try case-insensitive match as fallback
            var matchingKey = fieldData.Keys.FirstOrDefault(k =>
                k.Equals(normalizedFieldName, StringComparison.OrdinalIgnoreCase));

            if (matchingKey != null)
            {
                value = fieldData[matchingKey];
            }
            else
            {
                _logger.LogWarning("Field {FieldName} not found in field data for template {TemplateName}",
                    normalizedFieldName, template.TemplateName);
            }

            var isHtmlContent = value.Contains("<ul>") || value.Contains("<ol>") || value.Contains("<li>");

            if (isHtmlContent)
            {
                htmlBuilder.AppendLine("</table>");
                htmlBuilder.AppendLine($"<p style='font-weight: bold; margin-top: 12px;'>{normalizedFieldName}:</p>");
                htmlBuilder.AppendLine(value);
                htmlBuilder.AppendLine("<table style='border-collapse: collapse; width: 100%; max-width: 600px;'>");
            }
            else
            {
                htmlBuilder.AppendLine("<tr>");
                htmlBuilder.AppendLine($"  <td style='padding: 8px; border-bottom: 1px solid #ddd; font-weight: bold; width: 40%;'>{normalizedFieldName}:</td>");
                htmlBuilder.AppendLine($"  <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{value}</td>");
                htmlBuilder.AppendLine("</tr>");
            }
        }

        htmlBuilder.AppendLine("</table>");
        htmlBuilder.AppendLine("<br />");
        htmlBuilder.AppendLine("<p>Thank you.</p>");
        htmlBuilder.AppendLine("</div>");

        return htmlBuilder.ToString();
    }

    /// <summary>
    /// Replaces placeholders in the email subject with actual values
    /// Supports placeholders like {{EmployeeName}}, {{StartDate}}, etc.
    /// </summary>
    public string ReplaceSubjectPlaceholders(string subject, Dictionary<string, string> fieldData)
    {
        if (string.IsNullOrEmpty(subject))
        {
            return subject;
        }

        var result = subject;

        // Common placeholder mappings (these can be expanded based on needs)
        var placeholderMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "EmployeeName", GetFieldValue(fieldData, "New Employee", "Employee Name", "Full Name") },
            { "FirstName", GetFieldValue(fieldData, "First Name") },
            { "LastName", GetFieldValue(fieldData, "Last Name") },
            { "StartDate", GetFieldValue(fieldData, "Start Date", "First Day Employment") },
            { "Company", GetFieldValue(fieldData, "Company", "Company Name") },
            { "Position", GetFieldValue(fieldData, "Position", "Job Title") },
            { "Department", GetFieldValue(fieldData, "Department", "Division") },
            { "Supervisor", GetFieldValue(fieldData, "Supervisor", "Manager") }
        };

        // Replace all {{Placeholder}} patterns
        foreach (var mapping in placeholderMappings)
        {
            var placeholder = $"{{{{{mapping.Key}}}}}";
            if (result.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
            {
                result = result.Replace(placeholder, mapping.Value, StringComparison.OrdinalIgnoreCase);
                _logger.LogDebug("Replaced placeholder {Placeholder} with value {Value}", placeholder, mapping.Value);
            }
        }

        // Check for any remaining unreplaced placeholders and log warning
        if (result.Contains("{{") && result.Contains("}}"))
        {
            _logger.LogWarning("Subject still contains unreplaced placeholders: {Subject}", result);
        }

        return result;
    }

    /// <summary>
    /// Helper method to get field value from dictionary with multiple possible field name variants
    /// </summary>
    private string GetFieldValue(Dictionary<string, string> fieldData, params string[] possibleFieldNames)
    {
        foreach (var fieldName in possibleFieldNames)
        {
            var matchingKey = fieldData.Keys.FirstOrDefault(k =>
                k.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

            if (matchingKey != null)
            {
                return fieldData[matchingKey];
            }
        }

        return "N/A";
    }
}
