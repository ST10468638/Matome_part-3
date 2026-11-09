using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay_.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string? _requiredRole;

    public SessionAuthorizeAttribute()
    {
    }

    public SessionAuthorizeAttribute(string requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");

        if (userId is null)
        {
            context.Result = new ObjectResult(new
            {
                message = "Authentication is required to access this endpoint."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };

            return Task.CompletedTask;
        }

        if (_requiredRole is not null)
        {
            var currentRole = session.GetString("Role");

            if (!string.Equals(
                    currentRole,
                    _requiredRole,
                    StringComparison.Ordinal))
            {
                context.Result = new ObjectResult(new
                {
                    message = "Your account does not have permission to access this endpoint."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }

        return Task.CompletedTask;
    }
}