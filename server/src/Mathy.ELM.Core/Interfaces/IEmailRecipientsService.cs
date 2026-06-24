namespace Mathy.ELM.Core.Interfaces;

/// <summary>
/// Service for resolving email recipients based on EmailTemplate.Recipients field
/// Supports both CompanyDL distribution lists (ITDL, HRDL, etc.) and special recipients (Manager, Submitter, Employee)
/// </summary>
public interface IEmailRecipientsService
{
    /// <summary>
    /// Extracts recipients from comma-delimited EmailTemplate.Recipients field and resolves them
    /// Supports CompanyDL fields (ITDL, HRDL, etc.) and special recipients (Manager, Submitter, Employee)
    /// </summary>
    /// <param name="templateName">Name of the email template to find recipients from</param>
    /// <param name="companyCode">Company code to look up in CompanyDL</param>
    /// <param name="deptCode">Department code to look up in CompanyDL (required, filters DL by department)</param>
    /// <param name="managerEmail">Manager email address (for 'Manager' recipient key)</param>
    /// <param name="submitterEmail">Submitter email address (for 'Submitter' recipient key)</param>
    /// <param name="employeeEmail">Employee email address (for 'Employee' recipient key)</param>
    /// <param name="requestType">Request type to filter templates (e.g., 'NEWHIRE', 'PROMOTION')</param>
    /// <returns>List of resolved email addresses</returns>
    Task<List<string>> GetRecipientsFromTemplateAsync(
        string templateName,
        int? companyCode,
        int deptCode,
        string? managerEmail = null,
        string? submitterEmail = null,
        string? employeeEmail = null,
        string? requestType = null);

    /// <summary>
    /// Parses comma-delimited recipient identifiers (e.g., "ITDL, HRDL, Manager") and maps them to their values
    /// Supports CompanyDL fields and special recipient keys
    /// </summary>
    /// <param name="recipientKeys">Comma-delimited string of recipient keys (e.g., "ITDL, HRDL, Manager, Submitter")</param>
    /// <param name="companyDL">CompanyDL entity to extract email addresses from</param>
    /// <param name="managerEmail">Manager email address (for 'Manager' recipient key)</param>
    /// <param name="submitterEmail">Submitter email address (for 'Submitter' recipient key)</param>
    /// <param name="employeeEmail">Employee email address (for 'Employee' recipient key)</param>
    /// <returns>List of resolved email addresses</returns>
    List<string> ResolveRecipients(
        string recipientKeys,
        Entities.CompanyDL? companyDL,
        string? managerEmail = null,
        string? submitterEmail = null,
        string? employeeEmail = null);
}
