using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hangfire.Dashboard.Filters;
using Hangfire.Dashboard.Options;
using Microsoft.AspNetCore.Mvc;

namespace Hangfire.Dashboard.Areas.HangfireDashboard.Pages;

/// <summary>
/// Page model for the Hangfire jobs dashboard.
/// Exposes module configuration (queue list, default page size) to the view;
/// all job data is loaded client-side via the HangfireController REST API.
/// </summary>
[ServiceFilter(typeof(HangfireDashboardAuthorizationFilter))]
public class JobsModel(
    IOptions<HangfireDashboardOptions> options,
    ILogger<JobsModel> logger)
    : PageModel
{
    private readonly HangfireDashboardOptions _options = options.Value;

    /// <summary>
    /// Gets the page title.
    /// </summary>
    public string Title => "Jobs Dashboard";

    /// <summary>
    /// Gets the list of known queue names for the filter dropdown.
    /// </summary>
    public IReadOnlyList<string> KnownQueues => _options.KnownQueues;

    /// <summary>
    /// Gets the default page size for the jobs table.
    /// </summary>
    public int DefaultPageSize => _options.DefaultPageSize;

    /// <summary>
    /// Gets the optional custom layout path, or null to use the built-in layout.
    /// </summary>
    public string? LayoutPath => _options.LayoutPath;

    /// <summary>
    /// Handles the GET request. All job data is loaded client-side via AJAX.
    /// </summary>
    public void OnGet()
    {
        logger.LogDebug("Hangfire Dashboard page loaded");
    }
}
