using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Hangfire.Dashboard.Contracts;
using Hangfire.Dashboard.Filters;
using Hangfire.Dashboard.Services;

namespace Hangfire.Dashboard.Apis;

/// <summary>
/// HTTP adapter for the Hangfire jobs dashboard.
/// Delegates all business logic to <see cref="IHangfireJobService"/>;
/// this class is solely responsible for HTTP concerns (routing, status codes, response wrapping).
/// </summary>
[ApiController]
[Route("api/v1/hangfire")]
[ServiceFilter(typeof(HangfireDashboardAuthorizationFilter))]
public class HangfireController(
    IHangfireJobService jobService,
    ILogger<HangfireController> logger) : ControllerBase
{
    /// <summary>
    /// Returns overall Hangfire storage statistics (counts per state, servers, recurring jobs).
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        try
        {
            return Ok(ApiResponse.Ok(jobService.GetStats()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Hangfire statistics");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve statistics"));
        }
    }

    /// <summary>
    /// Returns a paginated, filterable list of jobs across all states (or a specific one).
    /// </summary>
    /// <param name="request">Filter and pagination parameters.</param>
    [HttpGet("jobs")]
    public IActionResult GetJobs([FromQuery] JobListRequest request)
    {
        try
        {
            return Ok(ApiResponse.Ok(jobService.GetJobs(request)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Hangfire jobs");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve jobs"));
        }
    }

    /// <summary>
    /// Returns full detail for a single job including parameters, state history, and any exception.
    /// </summary>
    /// <param name="id">The Hangfire job ID.</param>
    [HttpGet("jobs/{id}")]
    public IActionResult GetJobDetail(string id)
    {
        try
        {
            var detail = jobService.GetJobDetail(id);
            if (detail is null)
            {
                return NotFound(ApiResponse.Fail($"Job '{id}' not found"));
            }

            return Ok(ApiResponse.Ok(detail));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Hangfire job detail for {JobId}", id);
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve job detail"));
        }
    }

    /// <summary>
    /// Requeues a failed or deleted job for immediate re-execution.
    /// </summary>
    /// <param name="id">The Hangfire job ID to requeue.</param>
    [HttpPost("jobs/{id}/requeue")]
    public IActionResult RequeueJob(string id)
    {
        try
        {
            if (!jobService.RequeueJob(id))
            {
                return BadRequest(ApiResponse.Fail($"Could not requeue job '{id}'. It may not be in a requeue-able state."));
            }

            logger.LogInformation("Hangfire job {JobId} requeued by {User}", id, User.Identity?.Name);
            return Ok(ApiResponse.Ok());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error requeueing Hangfire job {JobId}", id);
            return StatusCode(500, ApiResponse.Fail("Failed to requeue job"));
        }
    }

    /// <summary>
    /// Deletes a Hangfire job.
    /// </summary>
    /// <param name="id">The Hangfire job ID to delete.</param>
    [HttpDelete("jobs/{id}")]
    public IActionResult DeleteJob(string id)
    {
        try
        {
            if (!jobService.DeleteJob(id))
            {
                return BadRequest(ApiResponse.Fail($"Could not delete job '{id}'. It may not exist or be in a deletable state."));
            }

            logger.LogInformation("Hangfire job {JobId} deleted by {User}", id, User.Identity?.Name);
            return Ok(ApiResponse.Ok());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting Hangfire job {JobId}", id);
            return StatusCode(500, ApiResponse.Fail("Failed to delete job"));
        }
    }

    /// <summary>
    /// Returns a summary of all Hangfire queues with their current job counts.
    /// </summary>
    [HttpGet("queues")]
    public IActionResult GetQueues()
    {
        try
        {
            return Ok(ApiResponse.Ok(jobService.GetQueues()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Hangfire queues");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve queues"));
        }
    }

    /// <summary>
    /// Returns all active Hangfire server instances.
    /// </summary>
    [HttpGet("servers")]
    public IActionResult GetServers()
    {
        try
        {
            return Ok(ApiResponse.Ok(jobService.GetServers()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Hangfire servers");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve servers"));
        }
    }

    /// <summary>
    /// Returns all recurring job definitions registered in Hangfire.
    /// </summary>
    [HttpGet("recurring")]
    public IActionResult GetRecurringJobs()
    {
        try
        {
            return Ok(ApiResponse.Ok(jobService.GetRecurringJobs()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching Hangfire recurring jobs");
            return StatusCode(500, ApiResponse.Fail("Failed to retrieve recurring jobs"));
        }
    }

    /// <summary>
    /// Requeues a batch of jobs in a single request.
    /// </summary>
    /// <param name="request">The list of job IDs to requeue.</param>
    [HttpPost("jobs/bulk-requeue")]
    public IActionResult BulkRequeueJobs([FromBody] BulkJobRequest request)
    {
        try
        {
            var result = jobService.BulkRequeue(request.Ids);
            logger.LogInformation(
                "Hangfire bulk requeue by {User}: {Succeeded} succeeded, {Failed} failed",
                User.Identity?.Name,
                result.Succeeded,
                result.Failed);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk-requeueing Hangfire jobs");
            return StatusCode(500, ApiResponse.Fail("Failed to requeue jobs"));
        }
    }

    /// <summary>
    /// Deletes a batch of jobs in a single request.
    /// </summary>
    /// <param name="request">The list of job IDs to delete.</param>
    [HttpPost("jobs/bulk-delete")]
    public IActionResult BulkDeleteJobs([FromBody] BulkJobRequest request)
    {
        try
        {
            var result = jobService.BulkDelete(request.Ids);
            logger.LogInformation(
                "Hangfire bulk delete by {User}: {Succeeded} succeeded, {Failed} failed",
                User.Identity?.Name,
                result.Succeeded,
                result.Failed);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error bulk-deleting Hangfire jobs");
            return StatusCode(500, ApiResponse.Fail("Failed to delete jobs"));
        }
    }
}
