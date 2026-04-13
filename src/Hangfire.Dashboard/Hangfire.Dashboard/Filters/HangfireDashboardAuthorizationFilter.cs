using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Hangfire.Dashboard.Options;

namespace Hangfire.Dashboard.Filters;

/// <summary>
/// Authorization filter for the Hangfire Dashboard.
/// Evaluates <see cref="HangfireDashboardAuthOptions"/> in precedence order:
/// <c>Filter</c> → <c>Policy</c> → <c>Roles</c> → require-authenticated → <c>AllowAnonymous</c>.
/// Register as a scoped service and apply via <c>[ServiceFilter(typeof(HangfireDashboardAuthorizationFilter))]</c>.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter(
    IOptions<HangfireDashboardOptions> options) : IAsyncAuthorizationFilter
{
    private readonly HangfireDashboardAuthOptions _auth = options.Value.Authorization;

    /// <inheritdoc />
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // 1. AllowAnonymous — skip all checks
        if (_auth.AllowAnonymous)
        {
            return;
        }

        var user = context.HttpContext.User;
        var isAuthenticated = user.Identity?.IsAuthenticated == true;

        // 2. Custom delegate filter (highest precedence)
        if (_auth.Filter is not null)
        {
            if (_auth.Filter(context.HttpContext))
            {
                return;
            }

            context.Result = isAuthenticated
                ? new ForbidResult()
                : new UnauthorizedResult();
            return;
        }

        // 3. Policy-based authorization
        if (!string.IsNullOrWhiteSpace(_auth.Policy))
        {
            if (!isAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
            var policyResult = await authService.AuthorizeAsync(user, _auth.Policy);

            if (!policyResult.Succeeded)
            {
                context.Result = new ForbidResult();
            }

            return;
        }

        // 4. Role-based authorization
        if (_auth.Roles is { Length: > 0 })
        {
            if (!isAuthenticated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!_auth.Roles.Any(role => user.IsInRole(role)))
            {
                context.Result = new ForbidResult();
            }

            return;
        }

        // 5. Default — require authentication
        if (!isAuthenticated)
        {
            context.Result = new UnauthorizedResult();
        }
    }
}
