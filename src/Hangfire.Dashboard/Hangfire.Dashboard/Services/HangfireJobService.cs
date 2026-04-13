using System.Text.Json;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Options;
using Hangfire.Dashboard.Contracts;
using Hangfire.Dashboard.Options;

namespace Hangfire.Dashboard.Services;

/// <summary>
/// Default implementation of <see cref="IHangfireJobService"/>.
/// Wraps Hangfire's <see cref="IMonitoringApi"/> and <see cref="IBackgroundJobClient"/>
/// to provide all querying and management operations used by the dashboard.
/// </summary>
public sealed class HangfireJobService(
    JobStorage jobStorage,
    IBackgroundJobClient backgroundJobClient,
    IOptions<HangfireDashboardOptions> options) : IHangfireJobService
{
    private readonly HangfireDashboardOptions _options = options.Value;

    /// <inheritdoc />
    public HangfireStatsDto GetStats()
    {
        var stats = jobStorage.GetMonitoringApi().GetStatistics();

        return new HangfireStatsDto
        {
            Enqueued = stats.Enqueued,
            Scheduled = stats.Scheduled,
            Processing = stats.Processing,
            Succeeded = stats.Succeeded,
            Failed = stats.Failed,
            Deleted = stats.Deleted,
            Recurring = stats.Recurring,
            Servers = stats.Servers,
        };
    }

    /// <inheritdoc />
    public JobListResponse<JobSummaryDto> GetJobs(JobListRequest request)
    {
        var api = jobStorage.GetMonitoringApi();

        var statesToQuery = string.IsNullOrWhiteSpace(request.State)
            ? new List<string> { "Enqueued", "Processing", "Scheduled", "Succeeded", "Failed", "Deleted" }
            : new List<string> { request.State };

        var allJobs = new List<JobSummaryDto>();
        foreach (var state in statesToQuery)
        {
            allJobs.AddRange(FetchByState(api, state));
        }

        var filtered = ApplyFilters(allJobs, request);

        var asc = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        IEnumerable<JobSummaryDto> ordered = request.SortBy?.ToLowerInvariant() switch
        {
            "id" => asc ? filtered.OrderBy(j => j.Id) : filtered.OrderByDescending(j => j.Id),
            "method" => asc ? filtered.OrderBy(j => j.Method) : filtered.OrderByDescending(j => j.Method),
            "queue" => asc ? filtered.OrderBy(j => j.Queue) : filtered.OrderByDescending(j => j.Queue),
            "state" => asc ? filtered.OrderBy(j => j.State) : filtered.OrderByDescending(j => j.State),
            "startedat" => asc ? filtered.OrderBy(j => j.StartedAt) : filtered.OrderByDescending(j => j.StartedAt),
            "finishedat" => asc ? filtered.OrderBy(j => j.FinishedAt) : filtered.OrderByDescending(j => j.FinishedAt),
            "durationms" => asc ? filtered.OrderBy(j => j.DurationMs) : filtered.OrderByDescending(j => j.DurationMs),
            _ => filtered.OrderByDescending(j => j.EnqueuedAt),
        };

        var orderedList = ordered.ToList();
        var totalCount = orderedList.Count;
        var pageSize = Math.Max(1, request.PageSize);
        var page = Math.Max(1, request.Page);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new JobListResponse<JobSummaryDto>
        {
            Items = orderedList.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
        };
    }

    /// <inheritdoc />
    public JobDetailDto? GetJobDetail(string id)
    {
        var detail = jobStorage.GetMonitoringApi().JobDetails(id);
        if (detail is null)
        {
            return null;
        }

        return MapToJobDetail(id, detail);
    }

    /// <inheritdoc />
    public List<QueueSummaryDto> GetQueues()
    {
        return jobStorage.GetMonitoringApi()
            .Queues()
            .Select(q => new QueueSummaryDto
            {
                Name = q.Name,
                Length = q.Length,
                Fetched = q.Fetched ?? 0,
            })
            .ToList();
    }

    /// <inheritdoc />
    public List<ServerSummaryDto> GetServers()
    {
        return jobStorage.GetMonitoringApi()
            .Servers()
            .Select(s => new ServerSummaryDto
            {
                Name = s.Name,
                Heartbeat = s.Heartbeat,
                Queues = s.Queues?.ToList() ?? [],
                WorkersCount = s.WorkersCount,
                StartedAt = s.StartedAt,
            })
            .ToList();
    }

    /// <inheritdoc />
    public List<Contracts.RecurringJobDto> GetRecurringJobs()
    {
        using var connection = jobStorage.GetConnection();
        return connection.GetRecurringJobs()
            .Select(j => new Contracts.RecurringJobDto
            {
                Id = j.Id,
                Method = j.Job is not null
                    ? $"{j.Job.Type?.Name}.{j.Job.Method?.Name}"
                    : j.Id,
                Cron = j.Cron,
                Queue = j.Queue,
                NextExecution = j.NextExecution,
                LastExecution = j.LastExecution,
                LastJobState = j.LastJobState,
                Error = j.Error,
            })
            .ToList();
    }

    /// <inheritdoc />
    public bool RequeueJob(string id) => backgroundJobClient.Requeue(id);

    /// <inheritdoc />
    public bool DeleteJob(string id) => backgroundJobClient.Delete(id);

    /// <inheritdoc />
    public BulkJobResultDto BulkRequeue(IReadOnlyList<string> ids)
    {
        var result = new BulkJobResultDto();
        foreach (var id in ids)
        {
            if (RequeueJob(id))
            {
                result.Succeeded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public BulkJobResultDto BulkDelete(IReadOnlyList<string> ids)
    {
        var result = new BulkJobResultDto();
        foreach (var id in ids)
        {
            if (DeleteJob(id))
            {
                result.Succeeded++;
            }
            else
            {
                result.Failed++;
            }
        }

        return result;
    }

    // ─── Private: Filtering ─────────────────────────────────────────────────
    private static IEnumerable<JobSummaryDto> ApplyFilters(
        IEnumerable<JobSummaryDto> source,
        JobListRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.JobId))
        {
            source = source.Where(j => j.Id == request.JobId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Queue))
        {
            source = source.Where(j =>
                j.Queue.Equals(request.Queue.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Method))
        {
            source = source.Where(j =>
                j.Method.Contains(request.Method.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (request.DateFrom.HasValue)
        {
            source = source.Where(j => j.EnqueuedAt >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            source = source.Where(j => j.EnqueuedAt <= request.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.PayloadSearch))
        {
            var term = request.PayloadSearch.Trim();
            source = source.Where(j =>
                j.PayloadJson.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return source;
    }

    // ─── Private: State Fetching ─────────────────────────────────────────────
    private IEnumerable<JobSummaryDto> FetchByState(IMonitoringApi api, string state)
    {
        return state switch
        {
            "Enqueued" => FetchEnqueued(api),
            "Processing" => FetchProcessing(api),
            "Scheduled" => FetchScheduled(api),
            "Succeeded" => FetchSucceeded(api),
            "Failed" => FetchFailed(api),
            "Deleted" => FetchDeleted(api),
            _ => [],
        };
    }

    private List<JobSummaryDto> FetchEnqueued(IMonitoringApi api)
    {
        var result = new List<JobSummaryDto>();
        foreach (var queue in api.Queues())
        {
            var jobs = api.EnqueuedJobs(queue.Name, 0, _options.MaxJobsPerStateQuery);
            result.AddRange(jobs
                .Where(j => j.Value is not null)
                .Select(j => new JobSummaryDto
                {
                    Id = j.Key,
                    Method = ResolveMethod(j.Value.Job),
                    Queue = queue.Name,
                    State = "Enqueued",
                    EnqueuedAt = j.Value.EnqueuedAt,
                    PayloadJson = SerializeArgs(j.Value.Job),
                }));
        }

        return result;
    }

    private IEnumerable<JobSummaryDto> FetchProcessing(IMonitoringApi api)
    {
        return api.ProcessingJobs(0, _options.MaxJobsPerStateQuery)
            .Where(j => j.Value is not null)
            .Select(j => new JobSummaryDto
            {
                Id = j.Key,
                Method = ResolveMethod(j.Value.Job),
                Queue = string.Empty,
                State = "Processing",
                EnqueuedAt = j.Value.StartedAt,
                StartedAt = j.Value.StartedAt,
                PayloadJson = SerializeArgs(j.Value.Job),
            });
    }

    private IEnumerable<JobSummaryDto> FetchScheduled(IMonitoringApi api)
    {
        return api.ScheduledJobs(0, _options.MaxJobsPerStateQuery)
            .Where(j => j.Value is not null)
            .Select(j => new JobSummaryDto
            {
                Id = j.Key,
                Method = ResolveMethod(j.Value.Job),
                Queue = string.Empty,
                State = "Scheduled",
                EnqueuedAt = j.Value.ScheduledAt,
                PayloadJson = SerializeArgs(j.Value.Job),
            });
    }

    private IEnumerable<JobSummaryDto> FetchSucceeded(IMonitoringApi api)
    {
        return api.SucceededJobs(0, _options.MaxJobsPerStateQuery)
            .Where(j => j.Value is not null)
            .Select(j => new JobSummaryDto
            {
                Id = j.Key,
                Method = ResolveMethod(j.Value.Job),
                Queue = string.Empty,
                State = "Succeeded",
                EnqueuedAt = j.Value.SucceededAt,
                FinishedAt = j.Value.SucceededAt,
                DurationMs = j.Value.TotalDuration.HasValue
                    ? (long)j.Value.TotalDuration.Value
                    : null,
                PayloadJson = SerializeArgs(j.Value.Job),
            });
    }

    private IEnumerable<JobSummaryDto> FetchFailed(IMonitoringApi api)
    {
        return api.FailedJobs(0, _options.MaxJobsPerStateQuery)
            .Where(j => j.Value is not null)
            .Select(j => new JobSummaryDto
            {
                Id = j.Key,
                Method = ResolveMethod(j.Value.Job),
                Queue = string.Empty,
                State = "Failed",
                EnqueuedAt = j.Value.FailedAt,
                FinishedAt = j.Value.FailedAt,
                HasError = true,
                PayloadJson = SerializeArgs(j.Value.Job),
            });
    }

    private IEnumerable<JobSummaryDto> FetchDeleted(IMonitoringApi api)
    {
        return api.DeletedJobs(0, _options.MaxJobsPerStateQuery)
            .Where(j => j.Value is not null)
            .Select(j => new JobSummaryDto
            {
                Id = j.Key,
                Method = ResolveMethod(j.Value.Job),
                Queue = string.Empty,
                State = "Deleted",
                EnqueuedAt = j.Value.DeletedAt,
                FinishedAt = j.Value.DeletedAt,
                PayloadJson = SerializeArgs(j.Value.Job),
            });
    }

    // ─── Private: Mapping helpers ────────────────────────────────────────────
    private static string ResolveMethod(global::Hangfire.Common.Job? job)
    {
        if (job is null)
        {
            return string.Empty;
        }

        return $"{job.Type?.Name}.{job.Method?.Name}";
    }

    private static string SerializeArgs(global::Hangfire.Common.Job? job)
    {
        if (job?.Args is null || job.Args.Count == 0)
        {
            return string.Empty;
        }

        try
        {
            return JsonSerializer.Serialize(job.Args);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static List<JobParameterDto> BuildParameters(
        System.Reflection.ParameterInfo[]? paramInfos,
        IReadOnlyList<object>? args)
    {
        if (args is null || args.Count == 0)
        {
            return [];
        }

        var result = new List<JobParameterDto>(args.Count);
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];
            var typeName = paramInfos is not null && i < paramInfos.Length
                ? paramInfos[i].ParameterType.Name
                : arg?.GetType().Name ?? "object";

            string? jsonValue = null;
            if (arg is not null)
            {
                try
                {
                    jsonValue = JsonSerializer.Serialize(arg, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    });
                }
                catch
                {
                    jsonValue = arg.ToString();
                }
            }

            result.Add(new JobParameterDto { TypeName = typeName, JsonValue = jsonValue });
        }

        return result;
    }

    private static JobDetailDto MapToJobDetail(string id, JobDetailsDto detail)
    {
        var latestState = detail.History?.FirstOrDefault();
        var currentState = latestState?.StateName ?? "Unknown";

        var methodName = detail.Job is not null ? ResolveMethod(detail.Job) : "Unknown";
        var parameters = BuildParameters(detail.Job?.Method?.GetParameters(), detail.Job?.Args);

        var failedState = detail.History?.FirstOrDefault(h =>
            h.StateName.Equals("Failed", StringComparison.OrdinalIgnoreCase));

        string? exceptionMessage = null;
        string? exceptionStackTrace = null;
        if (failedState?.Data is not null)
        {
            failedState.Data.TryGetValue("ExceptionMessage", out exceptionMessage);
            failedState.Data.TryGetValue("ExceptionDetails", out exceptionStackTrace);
        }

        var enqueuedAt = detail.History?
            .LastOrDefault(h => h.StateName.Equals("Enqueued", StringComparison.OrdinalIgnoreCase))?.CreatedAt;

        var startedAt = detail.History?
            .FirstOrDefault(h => h.StateName.Equals("Processing", StringComparison.OrdinalIgnoreCase))?.CreatedAt;

        DateTime? finishedAt = null;
        if (currentState is "Succeeded" or "Failed" or "Deleted")
        {
            finishedAt = latestState?.CreatedAt;
        }

        long? durationMs = null;
        if (startedAt.HasValue && finishedAt.HasValue)
        {
            durationMs = (long)(finishedAt.Value - startedAt.Value).TotalMilliseconds;
        }

        return new JobDetailDto
        {
            Id = id,
            Method = methodName,
            Queue = string.Empty,
            State = currentState,
            EnqueuedAt = enqueuedAt,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            DurationMs = durationMs,
            HasError = failedState is not null,
            Parameters = parameters,
            StateHistory = detail.History?.Select(h => new JobStateHistoryDto
            {
                State = h.StateName,
                Reason = h.Reason,
                EnteredAt = h.CreatedAt,
            }).ToList() ?? [],
            ExceptionMessage = exceptionMessage,
            ExceptionStackTrace = exceptionStackTrace,
        };
    }
}
