using Hangfire.Dashboard;

namespace MyDesk.Web.Scheduling;

/// <summary>
/// Restricts the /hangfire dashboard to authenticated users with Admin/Director/Administrator role.
/// In Development we also allow any authenticated user so devs can poke around.
/// </summary>
public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var user = http.User;
        if (user?.Identity?.IsAuthenticated != true) return false;

        if (http.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true)
            return true;

        return user.IsInRole("Admin") || user.IsInRole("Administrator") || user.IsInRole("Director");
    }
}
