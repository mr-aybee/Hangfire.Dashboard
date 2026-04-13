using Hangfire.Dashboard.Apis;
using Hangfire.Dashboard.Contracts;
using Hangfire.Dashboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Hangfire.Dashboard.Tests;

[TestClass]
public class HangfireControllerTests
{
    private Mock<IHangfireJobService> _serviceMock = null!;
    private HangfireController _controller = null!;

    [TestInitialize]
    public void Setup()
    {
        _serviceMock = new Mock<IHangfireJobService>();
        var logger = new Mock<ILogger<HangfireController>>();
        _controller = new HangfireController(_serviceMock.Object, logger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TestMethod]
    public void GetStats_ReturnsOk()
    {
        _serviceMock.Setup(s => s.GetStats()).Returns(new HangfireStatsDto());
        var result = _controller.GetStats();
        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        Assert.AreEqual(200, ok.StatusCode);
    }

    [TestMethod]
    public void GetJobs_ReturnsOk()
    {
        _serviceMock.Setup(s => s.GetJobs(It.IsAny<JobListRequest>()))
            .Returns(new JobListResponse<JobSummaryDto> { Items = new(), TotalCount = 0 });
        var result = _controller.GetJobs(new JobListRequest { Page = 1, PageSize = 10 });
        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
    }

    [TestMethod]
    public void GetJobDetail_NotFound_Returns404()
    {
        _serviceMock.Setup(s => s.GetJobDetail("x")).Returns((JobDetailDto?)null);
        var result = _controller.GetJobDetail("x");
        var notFound = result as NotFoundObjectResult;
        Assert.IsNotNull(notFound);
    }

    [TestMethod]
    public void GetJobDetail_Found_ReturnsOk()
    {
        _serviceMock.Setup(s => s.GetJobDetail("1"))
            .Returns(new JobDetailDto { Id = "1" });
        var result = _controller.GetJobDetail("1");
        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
    }

    [TestMethod]
    public void DeleteJob_Success_ReturnsOk()
    {
        _serviceMock.Setup(s => s.DeleteJob("1")).Returns(true);
        var result = _controller.DeleteJob("1");
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public void DeleteJob_Failure_ReturnsBadRequest()
    {
        _serviceMock.Setup(s => s.DeleteJob("x")).Returns(false);
        var result = _controller.DeleteJob("x");
        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public void RequeueJob_Success_ReturnsOk()
    {
        _serviceMock.Setup(s => s.RequeueJob("1")).Returns(true);
        var result = _controller.RequeueJob("1");
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public void GetQueues_ReturnsOk()
    {
        _serviceMock.Setup(s => s.GetQueues()).Returns(new List<QueueSummaryDto>());
        var result = _controller.GetQueues();
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public void GetServers_ReturnsOk()
    {
        _serviceMock.Setup(s => s.GetServers()).Returns(new List<ServerSummaryDto>());
        var result = _controller.GetServers();
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }

    [TestMethod]
    public void GetRecurringJobs_ReturnsOk()
    {
        _serviceMock.Setup(s => s.GetRecurringJobs()).Returns(new List<RecurringJobDto>());
        var result = _controller.GetRecurringJobs();
        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
    }
}
