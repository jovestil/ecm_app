using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Mathy.ELM.Core.Authorization;

public class CompanyAuthorizationRequirement : IAuthorizationRequirement
{
    public string CompanyCode { get; }

    public CompanyAuthorizationRequirement(string companyCode)
    {
        CompanyCode = companyCode;
    }
}

public class CompanyAuthorizationHandler : AuthorizationHandler<CompanyAuthorizationRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CompanyAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CompanyAuthorizationRequirement requirement)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        if (user == null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Get user's authorized companies from claims
        var userCompanies = user.FindAll("company")?.Select(c => c.Value).ToList() ?? new List<string>();
        
        // If no specific company requirement, allow access (for system admins)
        if (string.IsNullOrEmpty(requirement.CompanyCode))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Check if user has access to the required company
        if (userCompanies.Contains(requirement.CompanyCode) || 
            user.IsInRole("SystemAdmin") || 
            user.IsInRole("HRAdmin"))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}

// Attribute for easy use on controllers/actions
public class RequireCompanyAccessAttribute : AuthorizeAttribute
{
    public RequireCompanyAccessAttribute(string companyCode = "")
    {
        Policy = $"CompanyAccess:{companyCode}";
    }
}