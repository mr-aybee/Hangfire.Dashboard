namespace SampleApp;

public class SampleJobs
{
    private readonly ILogger<SampleJobs> _logger;

    public SampleJobs(ILogger<SampleJobs> logger) => _logger = logger;

    public void QuickJob() => _logger.LogInformation("QuickJob executed at {Time}", DateTime.UtcNow);

    public async Task SlowJob()
    {
        _logger.LogInformation("SlowJob started");
        await Task.Delay(3000);
        _logger.LogInformation("SlowJob completed");
    }

    public void FailingJob() => throw new InvalidOperationException("Designed to fail.");

    public void ParameterisedJob(string name, int count) =>
        _logger.LogInformation("ParameterisedJob: {Name}, {Count}", name, count);

    public void RecurringCleanup() => _logger.LogInformation("RecurringCleanup at {Time}", DateTime.UtcNow);
}
