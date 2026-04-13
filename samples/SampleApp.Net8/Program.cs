using Hangfire;
using Hangfire.Dashboard.Extensions;
using Hangfire.InMemory;
using SampleApp;

var builder = WebApplication.CreateBuilder(args);

// ── Hangfire with InMemory storage ────────────────────────
builder.Services.AddHangfire(cfg => cfg.UseInMemoryStorage());
builder.Services.AddHangfireServer(opts =>
{
    opts.Queues = new[] { "default", "critical", "low" };
});

// ── Hangfire Dashboard RCL (AllowAnonymous for local dev) ─
builder.Services.AddHangfireDashboard(opts =>
{
    opts.KnownQueues = ["default", "critical", "low"];
    opts.Authorization.AllowAnonymous = true;
});

builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();

// ── Seed sample jobs ──────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var client = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

    // Fire-and-forget
    for (int i = 0; i < 5; i++)
        client.Enqueue<SampleJobs>(j => j.QuickJob());

    // Delayed
    client.Schedule<SampleJobs>(j => j.SlowJob(), TimeSpan.FromMinutes(5));

    // Parameterised
    client.Enqueue<SampleJobs>(j => j.ParameterisedJob("test-user", 42));

    // Failing job
    client.Enqueue<SampleJobs>(j => j.FailingJob());

    // Recurring
    RecurringJob.AddOrUpdate<SampleJobs>("cleanup", j => j.RecurringCleanup(), Cron.Minutely);
}

app.Run();
