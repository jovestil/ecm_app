using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mathy.ELM.Core.Entities;
using Mathy.ELM.Core.Services;

namespace Mathy.ELM.Infrastructure.Extensions;

public static class EmployeeQueryExtensions
{
    public static IQueryable<Employee> ApplyRoleBasedFilter(
        this IQueryable<Employee> query,
        IRoleFilterService roleFilterService,
        IHttpContextAccessor httpContextAccessor,
        IUserContextService userContextService,
        DbSet<PayrollDepartmentShortName> payrollDepartmentShortNames,
        ILogger? logger = null)
    {
        try
        {
            var isAdmin = roleFilterService.IsSystemAdminOrEcmAdmin();

            // Get selected roles from header (can be comma-separated for multi-role support)
            var selectedRolesHeader = httpContextAccessor?.HttpContext?.Request.Headers["X-Selected-Role"].FirstOrDefault();
            var hasSelectedRole = !string.IsNullOrEmpty(selectedRolesHeader);

            logger?.LogDebug("=== EMPLOYEE FILTER DEBUG ===");
            logger?.LogDebug("Is Admin: {IsAdmin}", isAdmin);
            logger?.LogDebug("Selected Role Header: {SelectedRole}", selectedRolesHeader ?? "None");
            logger?.LogDebug("Has Selected Role: {HasSelectedRole}", hasSelectedRole);

            // Determine which roles to use for filtering
            string[] rolesToFilter;

            if (hasSelectedRole)
            {
                // Parse comma-separated roles from header
                var selectedRoles = selectedRolesHeader!.Split(',').Select(r => r.Trim()).Where(r => !string.IsNullOrEmpty(r)).ToArray();

                // Check if ECM_ADMIN is in the selected roles - no filtering needed
                if (selectedRoles.Any(r => r.Equals("ECM_ADMIN", StringComparison.OrdinalIgnoreCase)))
                {
                    logger?.LogDebug("ECM_ADMIN role in selected roles - no filtering applied");
                    return query;
                }

                rolesToFilter = selectedRoles;
                logger?.LogDebug("Using selected roles for filtering: {Roles}", string.Join(", ", rolesToFilter));
            }
            else if (isAdmin)
            {
                // Admin without selected role - no filtering needed
                logger?.LogDebug("Admin user with no selected role - no filtering applied");
                return query;
            }
            else
            {
                // Non-admin without selected role - fallback to user's roles from token
                var userRoles = userContextService.GetUserRoles();

                if (userRoles == null || !userRoles.Any())
                {
                    logger?.LogDebug("User has no roles, returning empty result");
                    return query.Where(e => false);
                }

                rolesToFilter = userRoles.ToArray();
                logger?.LogDebug("Non-admin user - using userRoles for filtering: {Roles}", string.Join(", ", rolesToFilter));
            }

            // Apply role-based filtering
            if (rolesToFilter.Length == 0)
            {
                logger?.LogDebug("No roles to filter by, returning empty result");
                return query.Where(e => false);
            }

            logger?.LogDebug("Applying role-based filtering for roles: {Roles}", string.Join(", ", rolesToFilter));

            // Apply role-based filtering for multiple roles using OR logic
            return query.Where(e =>
                e.PayrollDeptCode != null &&
                payrollDepartmentShortNames
                    .Where(pd => !pd.IsDeleted && rolesToFilter.Contains(pd.DeptShortName))
                    .Any(pd => pd.CompanyCode == e.CompanyCode && pd.DeptCode == e.PayrollDeptCode));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error applying role-based filter, returning empty result for safety");
            // On error, return empty result (safe fallback for non-admin)
            return query.Where(e => false);
        }
    }
}