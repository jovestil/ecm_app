using Hangfire.Dashboard;

namespace Mathy.ELM.Api;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var environment = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        
        // In development or local environment, allow access without authentication
        if (environment.IsDevelopment() || environment.EnvironmentName == "Local")
        {
            return true;
        }
        
        // In production, require authentication and specific roles
        return httpContext.User.Identity?.IsAuthenticated == true &&
               (httpContext.User.IsInRole("SystemAdmin") || httpContext.User.IsInRole("HRAdmin"));
    }
}