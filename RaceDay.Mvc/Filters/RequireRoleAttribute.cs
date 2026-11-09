using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _role;

    public RequireRoleAttribute(string role)
    {
        _role = role;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var currentRole = session.GetString("Role");

        if (string.IsNullOrWhiteSpace(currentRole))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }
        else if (!string.Equals(currentRole, _role, StringComparison.Ordinal))
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }

        return Task.CompletedTask;
    }
}
