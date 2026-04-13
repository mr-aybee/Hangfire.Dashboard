using Microsoft.Extensions.DependencyInjection;
using Hangfire.Dashboard.Filters;
using Hangfire.Dashboard.Options;
using Hangfire.Dashboard.Services;

namespace Hangfire.Dashboard.Extensions;

/// <summary>
/// Service registration extension for the Hangfire Dashboard RCL module.
/// </summary>
public static class HangfireDashboardServiceCollectionExtensions
{
    /// <summary>
    /// Registers Hangfire Dashboard services and configures module options.
    /// Call this from the host's DI registration pipeline, optionally providing
    /// a <paramref name="configure"/> delegate to override default settings.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddHangfireDashboard(opts =>
    /// {
    ///     opts.KnownQueues = ["default", "critical", "bulk"];
    ///     opts.MaxJobsPerStateQuery = 500;
    ///     opts.DefaultPageSize = 50;
    ///     opts.Authorization.Roles = ["Admin"];
    /// });
    /// </code>
    /// </example>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Optional delegate to customise <see cref="HangfireDashboardOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddHangfireDashboard(
        this IServiceCollection services,
        Action<HangfireDashboardOptions>? configure = null)
    {
        // Bind options — apply caller overrides if provided
        services.Configure<HangfireDashboardOptions>(opts => configure?.Invoke(opts));

        // Register authorization filter (scoped for ServiceFilter usage)
        services.AddScoped<HangfireDashboardAuthorizationFilter>();

        // Register service layer (scoped: one per request)
        services.AddScoped<IHangfireJobService, HangfireJobService>();

        return services;
    }
}
