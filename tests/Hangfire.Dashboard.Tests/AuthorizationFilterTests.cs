using Hangfire.Dashboard.Filters;
using Hangfire.Dashboard.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Claims;

namespace Hangfire.Dashboard.Tests;

[TestClass]
public class AuthorizationFilterTests
{
    private static AuthorizationFilterContext CreateContext(ClaimsPrincipal? user = null)
    {
        var httpContext = new DefaultHttpContext();
        if (user != null) httpContext.User = user;
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static ClaimsPrincipal AuthenticatedUser(params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "testuser") };
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private static HangfireDashboardAuthorizationFilter CreateFilter(Action<HangfireDashboardAuthOptions>? configure = null)
    {
        var dashOpts = new HangfireDashboardOptions();
        configure?.Invoke(dashOpts.Authorization);
        var opts = Microsoft.Extensions.Options.Options.Create(dashOpts);
        return new HangfireDashboardAuthorizationFilter(opts);
    }

    [TestMethod]
    public async Task AllowAnonymous_AlwaysAllows()
    {
        var filter = CreateFilter(a => a.AllowAnonymous = true);
        var ctx = CreateContext();
        await filter.OnAuthorizationAsync(ctx);
        Assert.IsNull(ctx.Result);
    }

    [TestMethod]
    public async Task UnauthenticatedUser_GetsForbidden()
    {
        var filter = CreateFilter();
        var ctx = CreateContext();
        await filter.OnAuthorizationAsync(ctx);
        Assert.IsNotNull(ctx.Result);
        Assert.IsInstanceOfType(ctx.Result, typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task RoleAuthorization_MatchingRole_Allows()
    {
        var filter = CreateFilter(a => a.Roles = new[] { "Admin" });
        var ctx = CreateContext(AuthenticatedUser("Admin"));
        await filter.OnAuthorizationAsync(ctx);
        Assert.IsNull(ctx.Result);
    }

    [TestMethod]
    public async Task RoleAuthorization_NoMatchingRole_Forbids()
    {
        var filter = CreateFilter(a => a.Roles = new[] { "Admin" });
        var ctx = CreateContext(AuthenticatedUser("User"));
        await filter.OnAuthorizationAsync(ctx);
        Assert.IsNotNull(ctx.Result);
        Assert.IsInstanceOfType(ctx.Result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task FilterDelegate_ReturnsTrue_Allows()
    {
        var filter = CreateFilter(a => a.Filter = _ => true);
        var ctx = CreateContext(AuthenticatedUser());
        await filter.OnAuthorizationAsync(ctx);
        Assert.IsNull(ctx.Result);
    }

    [TestMethod]
    public async Task FilterDelegate_ReturnsFalse_Forbids()
    {
        var filter = CreateFilter(a => a.Filter = _ => false);
        var ctx = CreateContext(AuthenticatedUser());
        await filter.OnAuthorizationAsync(ctx);
        Assert.IsNotNull(ctx.Result);
        Assert.IsInstanceOfType(ctx.Result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task AuthenticatedUser_NoRolesOrPolicy_Allows()
    {
        var filter = CreateFilter();
        var ctx = CreateContext(AuthenticatedUser());
        await filter.OnAuthorizationAsync(ctx);
        Assert.IsNull(ctx.Result);
    }
}
