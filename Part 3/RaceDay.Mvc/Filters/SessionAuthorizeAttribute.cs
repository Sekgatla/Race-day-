using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute(string? requiredRole = null) : Attribute, IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        if (string.IsNullOrWhiteSpace(session.GetString(SessionKeys.ApiToken)))
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
        }
        else if (!string.IsNullOrWhiteSpace(requiredRole) &&
                 !string.Equals(session.GetString(SessionKeys.Role), requiredRole, StringComparison.Ordinal))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
        return Task.CompletedTask;
    }
}
