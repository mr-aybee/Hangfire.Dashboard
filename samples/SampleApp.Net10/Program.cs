using Hangfire;
using Hangfire.Dashboard.Extensions;
using Hangfire.InMemory;
using SampleApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(cfg => cfg.UseInMemoryStorage());
builder.Services.AddHangfireServer(opts =>
{
    opts.Queues = new[] { "default", "critical", "low" };
});

// ── Role-based authorization example ──────────────────────
builder.Services.AddHangfireDashboard(opts =>
{
    opts.KnownQueues = ["default", "critical", "low"];
    opts.Authorization.Roles = new[] { "Admin", "System" };
    // For local dev, override with AllowAnonymous:
    opts.Authorization.AllowAnonymous = true;
});

builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var client = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
    for (int i = 0; i < 5; i++)
        client.Enqueue<SampleJobs>(j => j.QuickJob());
    client.Schedule<SampleJobs>(j => j.SlowJob(), TimeSpan.FromMinutes(5));
    client.Enqueue<SampleJobs>(j => j.FailingJob());
    client.Enqueue<SampleJobs>(j => j.ParameterisedJob("admin", 99));
    RecurringJob.AddOrUpdate<SampleJobs>("cleanup", j => j.RecurringCleanup(), Cron.Minutely);
}

app.Run();
