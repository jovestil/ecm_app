using Microsoft.AspNetCore.Http;

namespace Mathy.ELM.Core.Services;

public interface IRoleFilterService
{
    (int? CompanyCode, int? DeptCode) GetCompanyAndDeptFromSelectedRole();
    bool IsSystemAdminOrEcmAdmin();
}

public class RoleFilterService : IRoleFilterService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserContextService _userContextService;

    public RoleFilterService(
        IHttpContextAccessor httpContextAccessor,
        IUserContextService userContextService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userContextService = userContextService;
    }

    public (int? CompanyCode, int? DeptCode) GetCompanyAndDeptFromSelectedRole()
    {
        try
        {
            // Check if user is system admin or ECM_ADMIN first
            if (IsSystemAdminOrEcmAdmin())
            {
                return (null, null); // No filtering for admins
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return (null, null);

            // Get the X-Selected-Role header (can be comma-separated for multi-role support)
            var selectedRolesHeader = httpContext.Request.Headers["X-Selected-Role"].FirstOrDefault();
            if (string.IsNullOrEmpty(selectedRolesHeader))
                return (null, null);

            // Parse comma-separated roles
            var selectedRoles = selectedRolesHeader.Split(',').Select(r => r.Trim()).ToArray();

            // If any selected role is ECM_ADMIN, no filtering
            if (selectedRoles.Any(r => r.Equals("ECM_ADMIN", StringComparison.OrdinalIgnoreCase)))
                return (null, null);

            // This will be resolved via the DbContext during query execution
            // We return a special marker to indicate role-based filtering should occur
            return (-1, -1); // Special marker values that will be resolved in the query filter
        }
        catch
        {
            return (null, null);
        }
    }

    public bool IsSystemAdminOrEcmAdmin()
    {
        try
        {
            return _userContextService.IsInRole("SystemAdmin") || 
                   _userContextService.IsInRole("HRAdmin") ||
                   _userContextService.IsInRole("ECM_ADMIN");
        }
        catch
        {
            return false;
        }
    }
}