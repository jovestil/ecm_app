namespace Mathy.ELM.Core.Services;

/// <summary>
/// Service for accessing the current user's context from the HTTP request.
/// </summary>
public interface IUserContextService
{
    string GetUserId();
    string GetUserEmail();
    string GetUserName();
    /// <summary>
    /// Gets the user's display name (full name) from the 'name' claim
    /// Returns the actual user name without fallback to email
    /// </summary>
    string GetUserDisplayName();
    List<string> GetUserCompanies();
    List<string> GetUserRoles();
    bool IsInRole(string role);
    /// <summary>
    /// Gets the EmployeeNumber of the logged-in user by looking up their email in the Employees table.
    /// Returns 0 if the user is not found in the Employees table.
    /// Result is cached per request for performance.
    /// </summary>
    int GetUserEmployeeNumber();
}