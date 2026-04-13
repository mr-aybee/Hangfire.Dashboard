using Hangfire;
using Hangfire.Dashboard.Options;
using Hangfire.Dashboard.Services;
using Hangfire.InMemory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hangfire.Dashboard.Tests;

[TestClass]
public class HangfireJobServiceTests
{
    private JobStorage _storage = null!;
    private HangfireJobService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _storage = new InMemoryStorage();
        GlobalConfiguration.Configuration.UseStorage(_storage);
        var client = new BackgroundJobClient(_storage);
        var opts = Microsoft.Extensions.Options.Options.Create(new HangfireDashboardOptions());
        _sut = new HangfireJobService(_storage, client, opts);
    }

    [TestMethod]
    public void GetStats_ReturnsNonNull()
    {
        var stats = _sut.GetStats();
        Assert.IsNotNull(stats);
        Assert.AreEqual(0, stats.Enqueued);
        Assert.AreEqual(0, stats.Failed);
    }

    [TestMethod]
    public void GetJobs_EmptyStorage_ReturnsEmptyList()
    {
        var request = new Contracts.JobListRequest { Page = 1, PageSize = 10 };
        var result = _sut.GetJobs(request);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0, result.Items.Count);
    }

    [TestMethod]
    public void GetJobDetail_InvalidId_ReturnsNull()
    {
        var detail = _sut.GetJobDetail("nonexistent-999");
        Assert.IsNull(detail);
    }

    [TestMethod]
    public void EnqueueAndGetStats_ReflectsEnqueuedCount()
    {
        var client = new BackgroundJobClient(_storage);
        client.Enqueue(() => Console.WriteLine("test"));

        var stats = _sut.GetStats();
        Assert.IsTrue(stats.Enqueued >= 1);
    }

    [TestMethod]
    public void EnqueueAndGetJobs_ReturnsJob()
    {
        var client = new BackgroundJobClient(_storage);
        var jobId = client.Enqueue(() => Console.WriteLine("test-job"));

        var request = new Contracts.JobListRequest { Page = 1, PageSize = 10, State = "Enqueued" };
        var result = _sut.GetJobs(request);

        Assert.IsTrue(result.TotalCount >= 1);
        Assert.IsTrue(result.Items.Any(j => j.Id == jobId));
    }

    [TestMethod]
    public void GetQueues_ReturnsDefaultQueue()
    {
        var client = new BackgroundJobClient(_storage);
        client.Enqueue(() => Console.WriteLine("queue-test"));

        var queues = _sut.GetQueues();
        Assert.IsTrue(queues.Any(q => q.Name == "default"));
    }

    [TestMethod]
    public void DeleteJob_RemovesJob()
    {
        var client = new BackgroundJobClient(_storage);
        var jobId = client.Enqueue(() => Console.WriteLine("delete-me"));

        var deleted = _sut.DeleteJob(jobId);
        Assert.IsTrue(deleted);
    }

    [TestMethod]
    public void RequeueJob_SucceedsOnEnqueuedJob()
    {
        var client = new BackgroundJobClient(_storage);
        var jobId = client.Enqueue(() => Console.WriteLine("requeue-me"));

        var result = _sut.RequeueJob(jobId);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void BulkDelete_DeletesMultipleJobs()
    {
        var client = new BackgroundJobClient(_storage);
        var id1 = client.Enqueue(() => Console.WriteLine("bulk-1"));
        var id2 = client.Enqueue(() => Console.WriteLine("bulk-2"));

        var result = _sut.BulkDelete(new[] { id1, id2 });
        Assert.AreEqual(2, result.Succeeded);
    }

    [TestMethod]
    public void GetRecurringJobs_ReturnsEmpty()
    {
        var jobs = _sut.GetRecurringJobs();
        Assert.IsNotNull(jobs);
    }

    [TestMethod]
    public void GetServers_ReturnsEmpty()
    {
        var servers = _sut.GetServers();
        Assert.IsNotNull(servers);
        Assert.AreEqual(0, servers.Count);
    }
}
