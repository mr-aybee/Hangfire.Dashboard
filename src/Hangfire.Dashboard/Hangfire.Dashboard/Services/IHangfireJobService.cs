using Hangfire.Dashboard.Contracts;

namespace Hangfire.Dashboard.Services;

/// <summary>
/// Abstraction over Hangfire's monitoring and management APIs.
/// Encapsulates all storage interactions, mapping, filtering, and pagination logic
/// so that the HTTP layer remains a thin adapter.
/// </summary>
public interface IHangfireJobService
{
    /// <summary>
    /// Returns aggregate statistics across all job states and servers.
    /// </summary>
    HangfireStatsDto GetStats();

    /// <summary>
    /// Returns a paginated, filtered list of jobs across all (or a specific) state.
    /// </summary>
    /// <param name="request">Filter and pagination parameters.</param>
    JobListResponse<JobSummaryDto> GetJobs(JobListRequest request);

    /// <summary>
    /// Returns full detail for a single job including parameters, state history, and any exception.
    /// Returns <see langword="null"/> when the job does not exist in storage.
    /// </summary>
    /// <param name="id">The Hangfire job ID.</param>
    JobDetailDto? GetJobDetail(string id);

    /// <summary>
    /// Returns a summary of all queues currently tracked by the storage provider.
    /// </summary>
    List<QueueSummaryDto> GetQueues();

    /// <summary>
    /// Returns all active Hangfire server instances.
    /// </summary>
    List<ServerSummaryDto> GetServers();

    /// <summary>
    /// Returns all recurring job definitions registered in the storage.
    /// </summary>
    List<RecurringJobDto> GetRecurringJobs();

    /// <summary>
    /// Requeues a job so it will be picked up again by a worker.
    /// </summary>
    /// <param name="id">The Hangfire job ID.</param>
    /// <returns><see langword="true"/> if the job was successfully requeued; <see langword="false"/> if it was not in a requeue-able state.</returns>
    bool RequeueJob(string id);

    /// <summary>
    /// Soft-deletes a job by transitioning it to the Deleted state.
    /// </summary>
    /// <param name="id">The Hangfire job ID.</param>
    /// <returns><see langword="true"/> if the job was successfully deleted; <see langword="false"/> otherwise.</returns>
    bool DeleteJob(string id);

    /// <summary>
    /// Requeues a batch of jobs, returning a count of successes and failures.
    /// </summary>
    /// <param name="ids">The Hangfire job IDs to requeue.</param>
    BulkJobResultDto BulkRequeue(IReadOnlyList<string> ids);

    /// <summary>
    /// Deletes a batch of jobs, returning a count of successes and failures.
    /// </summary>
    /// <param name="ids">The Hangfire job IDs to delete.</param>
    BulkJobResultDto BulkDelete(IReadOnlyList<string> ids);
}
