using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Mathy.ELM.Core.Enums;
using Mathy.ELM.Core.Interfaces;
using Mathy.ELM.Core.Services;
using Mathy.ELM.Infrastructure.Data;

namespace Mathy.ELM.Infrastructure.Services;

/// <summary>
/// Service for accessing the current user's context from the HTTP request.
/// Implementation includes employee lookup for CreatedBy/ModifiedBy tracking.
/// </summary>
public class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MathyELMContext _context;
    private readonly IEcmLogger _ecmLogger;

    // Cache for employee number lookup (per request, since service is scoped)
    private int? _cachedEmployeeNumber;
    private bool _employeeNumberLookupPerformed;

    public UserContextService(IHttpContextAccessor httpContextAccessor, MathyELMContext context, IEcmLogger ecmLogger)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
        _ecmLogger = ecmLogger;
    }

    public string GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               user?.FindFirst("sub")?.Value ??
               user?.FindFirst("oid")?.Value;

        if (userId == null)
        {
            _ecmLogger.LogInfo(LogCategory.Authentication, "User ID not found in token");
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }

    public string GetUserEmail()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        return user?.FindFirst(ClaimTypes.Email)?.Value ??
               user?.FindFirst("email")?.Value ??
               user?.FindFirst("preferred_username")?.Value ??
               user?.FindFirst(ClaimTypes.Upn)?.Value ??
               string.Empty;
    }

    public string GetUserName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.Name)?.Value ??
               user?.FindFirst("name")?.Value ??
               GetUserDisplayName();
    }

    public string GetUserDisplayName()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        // Try to get 'name' claim (full display name)
        var displayName = user?.FindFirst("name")?.Value ??
                         user?.FindFirst(ClaimTypes.Name)?.Value;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        // If no name claim found, try given_name + family_name
        var givenName = user?.FindFirst("given_name")?.Value ?? "";
        var familyName = user?.FindFirst("family_name")?.Value ?? "";
        var combinedName = $"{givenName} {familyName}".Trim();

        if (!string.IsNullOrWhiteSpace(combinedName))
        {
            return combinedName;
        }

        // Last resort: return Unknown User (not email)
        return "Unknown User";
    }

    public List<string> GetUserCompanies()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var companyClaims = user?.FindAll("company")?.Select(c => c.Value).ToList() ?? new List<string>();

        // If no company claims found, try to extract from groups or other claims
        if (!companyClaims.Any())
        {
            var groups = user?.FindAll("groups")?.Select(g => g.Value).ToList() ?? new List<string>();
            // This would need to be mapped to actual company codes based on your AD group structure
            // For now, return empty list if no company claims exist
        }

        return companyClaims;
    }

    public List<string> GetUserRoles()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        // Try different possible role claim types
        var roles = user?.FindAll("roles")?.Select(c => c.Value).ToList();

        if (roles == null || !roles.Any())
        {
            roles = user?.FindAll("role")?.Select(c => c.Value).ToList();
        }

        if (roles == null || !roles.Any())
        {
            roles = user?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToList();
        }

        if (roles == null || !roles.Any())
        {
            roles = user?.FindAll("groups")?.Select(c => c.Value).ToList();
        }

        return roles ?? new List<string>();
    }

    public bool IsInRole(string role)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.IsInRole(role) ?? false;
    }

    /// <summary>
    /// Gets the EmployeeNumber of the logged-in user by looking up their email in the Employees table.
    /// Returns 0 if the user is not found in the Employees table.
    /// Result is cached per request for performance.
    /// </summary>
    public int GetUserEmployeeNumber()
    {
        // Return cached result if already looked up
        if (_employeeNumberLookupPerformed)
        {
            return _cachedEmployeeNumber ?? 0;
        }

        _employeeNumberLookupPerformed = true;

        try
        {
            // Get the user's email from the JWT token
            var userEmail = GetUserEmail();

            if (string.IsNullOrWhiteSpace(userEmail))
            {
                _ecmLogger.LogInfo(LogCategory.Authentication, "User email is empty, cannot lookup employee number");
                _cachedEmployeeNumber = 0;
                return 0;
            }

            // Look up the employee by WorkEmail (case-insensitive)
            var employee = _context.Employees
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.WorkEmail != null && e.WorkEmail.ToLower() == userEmail.ToLower())
                .Select(e => new { e.EmployeeNumber })
                .FirstOrDefault();

            _cachedEmployeeNumber = employee?.EmployeeNumber ?? 0;

            if (_cachedEmployeeNumber == 0)
            {
                _ecmLogger.LogInfo(LogCategory.Authentication, $"Employee not found for email: {userEmail}");
            }
            else
            {
                _ecmLogger.LogInfo(LogCategory.Authentication, $"Resolved employee number {_cachedEmployeeNumber} for user: {userEmail}");
            }

            return _cachedEmployeeNumber.Value;
        }
        catch (Exception ex)
        {
            // If any error occurs (e.g., no HttpContext, database error), return 0
            _ecmLogger.LogError(LogCategory.Authentication, "Error looking up employee number", ex);
            _cachedEmployeeNumber = 0;
            return 0;
        }
    }
}
