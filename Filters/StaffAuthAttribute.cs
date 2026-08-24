using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MnceShisanyama.Api.Models;
using MnceShisanyama.Api.Services;

namespace MnceShisanyama.Api.Filters;

/// <summary>
/// Guards an endpoint so it can only be called with a valid "X-Staff-Token" header
/// obtained from POST /api/auth/staff-login. Optionally restrict to a single role,
/// e.g. [StaffAuth(StaffRole.Admin)] for menu management / dashboard endpoints that
/// only managers should touch.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class StaffAuthAttribute : Attribute, IAsyncActionFilter
{
    private readonly StaffRole? _requiredRole;

    public StaffAuthAttribute() { }

    public StaffAuthAttribute(StaffRole requiredRole)
    {
        _requiredRole = requiredRole;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var authService = context.HttpContext.RequestServices.GetRequiredService<StaffAuthService>();

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Staff-Token", out var tokenHeader) ||
            string.IsNullOrWhiteSpace(tokenHeader))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Missing X-Staff-Token header." });
            return;
        }

        if (!authService.TryGetSession(tokenHeader.ToString(), out var session) || session is null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Invalid or expired staff session." });
            return;
        }

        if (_requiredRole is not null && session.Role != _requiredRole)
        {
            context.Result = new ObjectResult(new { message = "You do not have permission to do that." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        context.HttpContext.Items["StaffSession"] = session;
        await next();
    }
}
