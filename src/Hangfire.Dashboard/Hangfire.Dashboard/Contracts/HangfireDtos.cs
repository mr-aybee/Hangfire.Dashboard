namespace Hangfire.Dashboard.Contracts;

/// <summary>
/// Request parameters for querying the jobs list.
/// </summary>
public class JobListRequest
{
    /// <summary>Gets or sets the job state filter (e.g. Succeeded, Failed, Processing, Enqueued, Scheduled, Deleted).</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the queue name filter.</summary>
    public string? Queue { get; set; }

    /// <summary>Gets or sets a substring to match against the job method name.</summary>
    public string? Method { get; set; }

    /// <summary>Gets or sets the inclusive start of the enqueued-at date range (UTC).</summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>Gets or sets the inclusive end of the enqueued-at date range (UTC).</summary>
    public DateTime? DateTo { get; set; }

    /// <summary>Gets or sets an exact job ID to search for.</summary>
    public string? JobId { get; set; }

    /// <summary>Gets or sets the 1-based page number.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the number of items per page.</summary>
    public int PageSize { get; set; } = 25;

    /// <summary>Gets or sets the property name to sort by (Id, Method, Queue, State, EnqueuedAt, StartedAt, FinishedAt, DurationMs).</summary>
    public string? SortBy { get; set; }

    /// <summary>Gets or sets the sort direction ("asc" or "desc"). Defaults to "desc".</summary>
    public string? SortDirection { get; set; }

    /// <summary>Gets or sets a substring to match against the serialised JSON payload of the job arguments (case-insensitive contains).</summary>
    public string? PayloadSearch { get; set; }
}

/// <summary>
/// Request body for bulk job operations (delete / requeue).
/// </summary>
public class BulkJobRequest
{
    /// <summary>Gets or sets the list of Hangfire job IDs to operate on.</summary>
    public List<string> Ids { get; set; } = [];
}

/// <summary>
/// Result of a bulk job operation, reporting how many succeeded and how many could not be processed.
/// </summary>
public class BulkJobResultDto
{
    /// <summary>Gets or sets the number of jobs that were successfully processed.</summary>
    public int Succeeded { get; set; }

    /// <summary>Gets or sets the number of jobs that could not be processed (wrong state, not found, etc.).</summary>
    public int Failed { get; set; }
}

/// <summary>
/// Summary row for a single Hangfire job — shown in the jobs table.
/// </summary>
public class JobSummaryDto
{
    /// <summary>Gets or sets the Hangfire job ID.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the fully-qualified method name (e.g. AlarmImportService.ProcessAlarmsAsync).</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the queue the job was placed in.</summary>
    public string Queue { get; set; } = string.Empty;

    /// <summary>Gets or sets the current job state.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets when the job was created/enqueued (UTC).</summary>
    public DateTime? EnqueuedAt { get; set; }

    /// <summary>Gets or sets when the job started processing (UTC).</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Gets or sets when the job finished (UTC).</summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>Gets or sets the duration in milliseconds (null if not yet finished).</summary>
    public long? DurationMs { get; set; }

    /// <summary>Gets or sets a value indicating whether the job failed with an exception.</summary>
    public bool HasError { get; set; }

    /// <summary>Gets or sets the JSON-serialised arguments used only for server-side payload search filtering; not rendered in the UI.</summary>
    public string PayloadJson { get; set; } = string.Empty;
}

/// <summary>
/// A single parameter value for a Hangfire job.
/// </summary>
public class JobParameterDto
{
    /// <summary>Gets or sets the CLR type name of the parameter.</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON-serialised value of the parameter.</summary>
    public string? JsonValue { get; set; }
}

/// <summary>
/// Full detail for a single Hangfire job — shown in the detail modal.
/// </summary>
public class JobDetailDto : JobSummaryDto
{
    /// <summary>Gets or sets the serialised job parameters.</summary>
    public List<JobParameterDto> Parameters { get; set; } = [];

    /// <summary>Gets or sets the state history entries, newest first.</summary>
    public List<JobStateHistoryDto> StateHistory { get; set; } = [];

    /// <summary>Gets or sets the exception message if the job failed.</summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>Gets or sets the full exception stack trace if the job failed.</summary>
    public string? ExceptionStackTrace { get; set; }
}

/// <summary>
/// A single entry in a job's state history.
/// </summary>
public class JobStateHistoryDto
{
    /// <summary>Gets or sets the state name.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets the reason/message associated with this state transition.</summary>
    public string? Reason { get; set; }

    /// <summary>Gets or sets when this state was entered (UTC).</summary>
    public DateTime? EnteredAt { get; set; }
}

/// <summary>
/// Overall Hangfire storage statistics.
/// </summary>
public class HangfireStatsDto
{
    /// <summary>Gets or sets the number of enqueued jobs.</summary>
    public long Enqueued { get; set; }

    /// <summary>Gets or sets the number of scheduled jobs.</summary>
    public long Scheduled { get; set; }

    /// <summary>Gets or sets the number of jobs currently processing.</summary>
    public long Processing { get; set; }

    /// <summary>Gets or sets the number of succeeded jobs.</summary>
    public long Succeeded { get; set; }

    /// <summary>Gets or sets the number of failed jobs.</summary>
    public long Failed { get; set; }

    /// <summary>Gets or sets the number of deleted jobs.</summary>
    public long Deleted { get; set; }

    /// <summary>Gets or sets the number of recurring job definitions.</summary>
    public long Recurring { get; set; }

    /// <summary>Gets or sets the number of active Hangfire servers.</summary>
    public long Servers { get; set; }
}

/// <summary>
/// Summary of a Hangfire queue.
/// </summary>
public class QueueSummaryDto
{
    /// <summary>Gets or sets the queue name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of enqueued jobs waiting.</summary>
    public long Length { get; set; }

    /// <summary>Gets or sets the number of jobs currently fetched by workers.</summary>
    public long Fetched { get; set; }
}

/// <summary>
/// Summary of an active Hangfire server.
/// </summary>
public class ServerSummaryDto
{
    /// <summary>Gets or sets the server name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets when the server last sent a heartbeat (UTC).</summary>
    public DateTime? Heartbeat { get; set; }

    /// <summary>Gets or sets the list of queues this server listens to.</summary>
    public List<string> Queues { get; set; } = [];

    /// <summary>Gets or sets the number of worker threads.</summary>
    public int WorkersCount { get; set; }

    /// <summary>Gets or sets when this server started (UTC).</summary>
    public DateTime? StartedAt { get; set; }
}

/// <summary>
/// A recurring job definition.
/// </summary>
public class RecurringJobDto
{
    /// <summary>Gets or sets the recurring job ID.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the method name.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the cron expression.</summary>
    public string Cron { get; set; } = string.Empty;

    /// <summary>Gets or sets the queue this recurring job is placed in.</summary>
    public string Queue { get; set; } = string.Empty;

    /// <summary>Gets or sets the next scheduled execution (UTC).</summary>
    public DateTime? NextExecution { get; set; }

    /// <summary>Gets or sets the last execution time (UTC).</summary>
    public DateTime? LastExecution { get; set; }

    /// <summary>Gets or sets the state of the most recent job triggered by this recurring job.</summary>
    public string? LastJobState { get; set; }

    /// <summary>Gets or sets the error message if the last job failed.</summary>
    public string? Error { get; set; }
}

/// <summary>
/// Paginated list response wrapper for job queries.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public class JobListResponse<T>
{
    /// <summary>Gets or sets the items on the current page.</summary>
    public List<T> Items { get; set; } = [];

    /// <summary>Gets or sets the total number of matching items across all pages.</summary>
    public long TotalCount { get; set; }

    /// <summary>Gets or sets the current 1-based page number.</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets the page size used for this query.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets or sets the total number of pages.</summary>
    public int TotalPages { get; set; }
}
