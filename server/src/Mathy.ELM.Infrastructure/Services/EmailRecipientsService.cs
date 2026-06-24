using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Mathy.ELM.Infrastructure.Services;

/// <summary>
/// Service for resolving email recipients from EmailTemplate.Recipients field
/// Parses comma-delimited recipient identifiers and maps them to CompanyDL distribution lists
/// Uses reflection to dynamically map keys to CompanyDL properties
/// </summary>
public class EmailRecipientsService : IEmailRecipientsService
{
    private readonly MathyELMContext _context;
    private readonly ILogger<EmailRecipientsService> _logger;
    private readonly IEcmLogger _ecmLogger;

    // Cache for property info to avoid repeated reflection
    private static readonly Dictionary<string, PropertyInfo?> PropertyCache = new();

    public EmailRecipientsService(MathyELMContext context, ILogger<EmailRecipientsService> logger, IEcmLogger ecmLogger)
    {
        _context = context;
        _logger = logger;
        _ecmLogger = ecmLogger;
    }

    /// <summary>
    /// Extracts recipients from comma-delimited EmailTemplate.Recipients field and resolves them
    /// Supports CompanyDL fields (ITDL, HRDL, etc.) and special recipients (Manager, HiringManager, Submitter, Employee)
    /// </summary>
    /// <param name="templateName">Name of the email template to find recipients from</param>
    /// <param name="companyCode">Company code to look up in CompanyDL</param>
    /// <param name="deptCode">Department code to look up in CompanyDL (required, filters DL by department)</param>
    /// <param name="managerEmail">Manager email address (for 'Manager' recipient key)</param>
    /// <param name="submitterEmail">Submitter email address (for 'Submitter' recipient key)</param>
    /// <param name="employeeEmail">Employee email address (for 'Employee' recipient key)</param>
    /// <param name="requestType">Request type to filter templates (e.g., 'NEWHIRE', 'PROMOTION')</param>
    /// <returns>List of resolved email addresses</returns>
    public async Task<List<string>> GetRecipientsFromTemplateAsync(
        string templateName,
        int? companyCode,
        int deptCode,
        string? managerEmail = null,
        string? submitterEmail = null,
        string? employeeEmail = null,
        string? requestType = null)
    {
        try
        {
            // Find the email template - filter by both TemplateName and RequestType
            var query = _context.EmailTemplates
                .Where(t => t.TemplateName == templateName && !t.IsDeleted);

            // If requestType is specified, filter by it (values: 'NEWHIRE', 'PROMOTION')
            if (!string.IsNullOrEmpty(requestType))
            {
                query = query.Where(t => t.RequestType == requestType);
            }

            var template = await query.FirstOrDefaultAsync();

            if (template == null)
            {
                _logger.LogWarning("Email template '{TemplateName}' not found", templateName);
                return new List<string>();
            }

            if (string.IsNullOrWhiteSpace(template.Recipients))
            {
                _logger.LogWarning("Email template '{TemplateName}' has no recipients defined", templateName);
                return new List<string>();
            }

            // Fetch CompanyDL if company code is provided
            CompanyDL? companyDL = null;
            if (companyCode.HasValue)
            {
                // First try to find by CompanyCode and DeptCode (exact match)
                _logger.LogInformation("Filtering CompanyDL by CompanyCode {CompanyCode} and DeptCode {DeptCode}", companyCode, deptCode);

                companyDL = await _context.CompanyDLs
                    .Where(c => c.CompanyCode == companyCode.Value && c.DeptCode == deptCode && !c.IsDeleted)
                    .FirstOrDefaultAsync();

                // Fallback: If no exact match found, try to find company-wide DL (DeptCode = 0)
                if (companyDL == null && deptCode != 0)
                {
                    _logger.LogInformation("No exact CompanyDL match found, trying company-wide fallback (DeptCode=0) for CompanyCode {CompanyCode}", companyCode);

                    companyDL = await _context.CompanyDLs
                        .Where(c => c.CompanyCode == companyCode.Value && c.DeptCode == 0 && !c.IsDeleted)
                        .FirstOrDefaultAsync();
                }

                if (companyDL == null)
                {
                    _logger.LogWarning("CompanyDL not found for company code {CompanyCode} (tried DeptCode {DeptCode} and fallback DeptCode 0)", companyCode, deptCode);
                }
                else
                {
                    _logger.LogInformation("Found CompanyDL for CompanyCode {CompanyCode}, DeptCode {DeptCode}", companyDL.CompanyCode, companyDL.DeptCode);
                }
            }

            // Parse and resolve recipients
            var recipients = ResolveRecipients(
                template.Recipients,
                companyDL,
                managerEmail,
                submitterEmail,
                employeeEmail);

            _logger.LogInformation(
                "Resolved {RecipientCount} recipients from template '{TemplateName}' for company {CompanyCode} and department {DeptCode}",
                recipients.Count,
                templateName,
                companyCode ?? 0,
                deptCode
            );

            return recipients;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving recipients from template '{TemplateName}'", templateName);
            return new List<string>();
        }
    }

    /// <summary>
    /// Parses comma-delimited recipient identifiers (e.g., "ITDL, HRDL, Manager") and maps them to their values
    /// Supports CompanyDL fields and special recipient keys (Manager, HiringManager, Submitter, Employee)
    /// </summary>
    /// <param name="recipientKeys">Comma-delimited string of recipient keys (e.g., "ITDL, HRDL, Manager, HiringManager, Submitter")</param>
    /// <param name="companyDL">CompanyDL entity to extract email addresses from</param>
    /// <param name="managerEmail">Manager email address (for 'Manager' recipient key)</param>
    /// <param name="submitterEmail">Submitter email address (for 'Submitter' recipient key)</param>
    /// <param name="employeeEmail">Employee email address (for 'Employee' recipient key)</param>
    /// <returns>List of resolved email addresses (excludes empty/null values)</returns>
    public List<string> ResolveRecipients(
        string recipientKeys,
        CompanyDL? companyDL,
        string? managerEmail = null,
        string? submitterEmail = null,
        string? employeeEmail = null)
    {
        var recipients = new List<string>();

        if (string.IsNullOrWhiteSpace(recipientKeys))
        {
            return recipients;
        }

        // Split by comma and trim whitespace
        var keys = recipientKeys.Split(',')
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToList();

        // Map each key to its corresponding value
        foreach (var key in keys)
        {
            // First check for special recipients (Manager, Submitter, Employee)
            if (IsSpecialRecipientKey(key))
            {
                var specialEmail = MapSpecialRecipient(key, managerEmail, submitterEmail, employeeEmail);
                if (!string.IsNullOrWhiteSpace(specialEmail))
                {
                    recipients.Add(specialEmail);
                }
                // Don't warn if special recipient value is null - it's valid, just not provided
                continue;
            }

            // Then check CompanyDL if available
            if (companyDL != null)
            {
                var email = MapKeyToCompanyDL(key, companyDL);
                if (!string.IsNullOrWhiteSpace(email))
                {
                    // Handle semicolon-delimited emails (some DLs might have multiple addresses)
                    var emailAddresses = email.Split(';', StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrWhiteSpace(e));

                    recipients.AddRange(emailAddresses);
                }
            }
            else if (!IsCompanyDLKey(key))
            {
                // Key is not a CompanyDL key and CompanyDL is null - log warning
                _logger.LogWarning("Cannot resolve recipient key '{Key}' - not a special key and CompanyDL is null", key);
            }
        }

        return recipients.ToList();
    }

    /// <summary>
    /// Maps a recipient key (e.g., "ITDL", "HRDL") to the corresponding CompanyDL property using reflection
    /// Properties are discovered dynamically and cached for performance
    /// </summary>
    /// <param name="key">The recipient key to map (case-insensitive)</param>
    /// <param name="companyDL">The CompanyDL entity instance to get values from</param>
    /// <returns>The email address(es) from the corresponding property, or null if not found</returns>
    private string? MapKeyToCompanyDL(string key, CompanyDL companyDL)
    {
        try
        {
            // Check cache first
            PropertyInfo? property = null;
            if (!PropertyCache.TryGetValue(key, out property))
            {
                // Use reflection to find the property dynamically (case-insensitive)
                property = typeof(CompanyDL).GetProperty(key,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                // Cache the result (even if null)
                PropertyCache[key] = property;
            }

            if (property == null)
            {
                _ecmLogger.LogWarning(LogCategory.EmailNotification,
                    "No property found in CompanyDL for recipient key '{Key}'", key);
                return null;
            }

            // Get the value from the property
            var value = property.GetValue(companyDL) as string;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping recipient key '{Key}' to CompanyDL property", key);
            return null;
        }
    }

    /// <summary>
    /// Checks if a key is a special recipient key (Manager, HiringManager, Submitter, Employee)
    /// </summary>
    /// <param name="key">The recipient key to check</param>
    /// <returns>True if the key is a special recipient key, false otherwise</returns>
    private static bool IsSpecialRecipientKey(string key)
    {
        return key.ToUpperInvariant() switch
        {
            "MANAGER" => true,
            "HIRING-MANAGER" => true,
            "HIRING-MANAGER-EMAIL" => true,
            "SUBMITTER" => true,
            "EMPLOYEE" => true,
            _ => false
        };
    }

    /// <summary>
    /// Maps special recipient keys (Manager, HiringManager, Submitter, Employee) to their actual email addresses
    /// </summary>
    /// <param name="key">The recipient key to check (e.g., "Manager", "HiringManager", "Submitter", "Employee")</param>
    /// <param name="managerEmail">Manager email address (for 'Manager' and 'HiringManager' recipient keys)</param>
    /// <param name="submitterEmail">Submitter email address (for 'Submitter' recipient key)</param>
    /// <param name="employeeEmail">Employee email address (for 'Employee' recipient key)</param>
    /// <returns>The resolved email address, or null if key is not a special recipient</returns>
    private static string? MapSpecialRecipient(string key, string? managerEmail, string? submitterEmail, string? employeeEmail)
    {
        return key.ToUpperInvariant() switch
        {
            "MANAGER" => managerEmail,
            "HIRING-MANAGER" => managerEmail,
            "HIRING-MANAGER-EMAIL" => managerEmail,
            "SUBMITTER" => submitterEmail,
            "EMPLOYEE" => employeeEmail,
            _ => null
        };
    }

    /// <summary>
    /// Checks if a key is a CompanyDL property key using reflection
    /// Dynamically discovers if the property exists in CompanyDL without hardcoding
    /// </summary>
    /// <param name="key">The recipient key to check</param>
    /// <returns>True if the key is a CompanyDL property, false otherwise</returns>
    private static bool IsCompanyDLKey(string key)
    {
        try
        {
            var property = typeof(CompanyDL).GetProperty(key,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            // Check if property exists and is a string type
            return property != null && property.PropertyType == typeof(string);
        }
        catch
        {
            return false;
        }
    }
}
