using Microsoft.AspNetCore.Http;

namespace Hangfire.Dashboard.Options;

/// <summary>
/// Authentication and authorization options for the Hangfire Dashboard.
/// Supports three modes: role-based, policy-based, and custom delegate filtering.
/// When multiple modes are configured, precedence is: Filter → Policy → Roles.
/// </summary>
public sealed class HangfireDashboardAuthOptions
{
    /// <summary>
    /// Gets or sets the allowed roles. If set, a user must belong to at least one
    /// of these roles to access the dashboard.
    /// </summary>
    /// <example>
    /// <code>opts.Authorization.Roles = ["Admin", "HangfireManager"];</code>
    /// </example>
    public string[]? Roles { get; set; }

    /// <summary>
    /// Gets or sets a named ASP.NET Core authorization policy.
    /// When set, the policy is evaluated via <c>IAuthorizationService</c>.
    /// </summary>
    /// <example>
    /// <code>opts.Authorization.Policy = "HangfireAccess";</code>
    /// </example>
    public string? Policy { get; set; }

    /// <summary>
    /// Gets or sets a custom authorization delegate. Receives the current <see cref="HttpContext"/>
    /// and returns <see langword="true"/> to allow access or <see langword="false"/> to deny.
    /// Has the highest precedence when set.
    /// </summary>
    /// <example>
    /// <code>
    /// opts.Authorization.Filter = ctx =>
    ///     ctx.User.Identity?.IsAuthenticated == true
    ///     &amp;&amp; ctx.User.HasClaim("department", "IT");
    /// </code>
    /// </example>
    public Func<HttpContext, bool>? Filter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip all authorization checks.
    /// <b>WARNING:</b> Only use in development environments. Exposes the dashboard to anonymous users.
    /// Default: <see langword="false"/>.
    /// </summary>
    public bool AllowAnonymous { get; set; }
}
