namespace Hangfire.Dashboard.Options;

/// <summary>
/// Configuration options for the Hangfire Dashboard RCL module.
/// Configure via <see cref="Extensions.HangfireDashboardServiceCollectionExtensions.AddHangfireDashboard"/>.
/// </summary>
public sealed class HangfireDashboardOptions
{
    /// <summary>
    /// Gets or sets the list of known queue names displayed in the filter dropdown.
    /// Default: default, critical, notifications, emails, reports.
    /// </summary>
    public List<string> KnownQueues { get; set; } =
    [
        "default",
        "critical",
        "notifications",
        "emails",
        "reports",
    ];

    /// <summary>
    /// Gets or sets the maximum number of jobs fetched from Hangfire storage per state
    /// when building the unified jobs list. Higher values allow more complete in-memory
    /// filtering but increase memory usage.
    /// Default: 1000.
    /// </summary>
    public int MaxJobsPerStateQuery { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the default page size shown in the jobs table on first load.
    /// Default: 25.
    /// </summary>
    public int DefaultPageSize { get; set; } = 25;

    /// <summary>
    /// Gets or sets an optional Razor layout path to override the built-in layout.
    /// When <see langword="null"/>, the RCL's built-in Bootstrap 5 layout is used.
    /// </summary>
    /// <example>
    /// <code>opts.LayoutPath = "~/Views/Shared/_Layout.cshtml";</code>
    /// </example>
    public string? LayoutPath { get; set; }

    /// <summary>
    /// Gets the authorization options for the dashboard.
    /// </summary>
    public HangfireDashboardAuthOptions Authorization { get; set; } = new();
}
