namespace SampleApp;

/// <summary>
/// Example background jobs for testing the Hangfire Dashboard.
/// </summary>
public class SampleJobs
{
    private readonly ILogger<SampleJobs> _logger;

    public SampleJobs(ILogger<SampleJobs> logger)
    {
        _logger = logger;
    }

    public void QuickJob()
    {
        _logger.LogInformation("QuickJob executed at {Time}", DateTime.UtcNow);
    }

    public async Task SlowJob()
    {
        _logger.LogInformation("SlowJob started at {Time}", DateTime.UtcNow);
        await Task.Delay(3000);
        _logger.LogInformation("SlowJob completed");
    }

    public void FailingJob()
    {
        throw new InvalidOperationException("This job is designed to fail for testing purposes.");
    }

    public void ParameterisedJob(string name, int count)
    {
        _logger.LogInformation("ParameterisedJob: name={Name}, count={Count}", name, count);
    }

    public void RecurringCleanup()
    {
        _logger.LogInformation("RecurringCleanup executed at {Time}", DateTime.UtcNow);
    }
}
