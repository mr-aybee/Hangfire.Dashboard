# Hangfire.Dashboard

[![NuGet](https://img.shields.io/nuget/v/Hangfire.Dashboard.svg)](https://www.nuget.org/packages/Hangfire.Dashboard)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A standalone **Razor Class Library (RCL)** that provides a modern, responsive dashboard UI for [Hangfire](https://www.hangfire.io/) background job processing. Ships as a NuGet package with built-in Bootstrap 5 layout, Chart.js graphs, and configurable authentication/authorisation.

**Developed by [abmalik.com](https://abmalik.com)**

---

## Features

- **Full dashboard UI** — stats cards, jobs table with filtering/sorting/pagination, queues, servers, recurring jobs, and graphs
- **Built-in standalone layout** — Bootstrap 5.3 + Font Awesome 6 (CDN), no external layout dependency required
- **Customisable layout** — optionally point to your own `_Layout.cshtml`
- **3-mode authorisation** — Role-based, Policy-based, or custom delegate filter
- **REST API** — 10 endpoints at `/api/v1/hangfire/*` for programmatic access
- **Dark mode** — automatic and toggle-based with `localStorage` persistence
- **Multi-target** — `net8.0`, `net9.0`, `net10.0`
- **NuGet-ready** — `dotnet pack` produces a release-ready `.nupkg`

## Installation

```shell
dotnet add package Hangfire.Dashboard
```

Or via the Package Manager Console:

```powershell
Install-Package Hangfire.Dashboard
```

## Quick Start

```csharp
using Hangfire;
using Hangfire.Dashboard.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Hangfire with your preferred storage
builder.Services.AddHangfire(cfg => cfg.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

// Add the dashboard (AllowAnonymous for local dev)
builder.Services.AddHangfireDashboard(opts =>
{
    opts.KnownQueues = new[] { "default", "critical" };
    opts.Authorization.AllowAnonymous = true;
});

builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();

app.Run();
```

Then navigate to `/Jobs` (Razor Page) or use the API at `/api/v1/hangfire/stats`.

## Authorisation

The library supports three authorisation modes configured via `HangfireDashboardAuthOptions`:

### 1. Allow Anonymous (development)

```csharp
builder.Services.AddHangfireDashboard(opts =>
{
    opts.Authorization.AllowAnonymous = true;
});
```

### 2. Role-based

```csharp
builder.Services.AddHangfireDashboard(opts =>
{
    opts.Authorization.Roles = new[] { "Admin", "System" };
});
```

### 3. Policy-based

```csharp
builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("HangfireAccess", p => p.RequireAuthenticatedUser());
});

builder.Services.AddHangfireDashboard(opts =>
{
    opts.Authorization.Policy = "HangfireAccess";
});
```

### 4. Custom Delegate

```csharp
builder.Services.AddHangfireDashboard(opts =>
{
    opts.Authorization.Filter = httpContext =>
        httpContext.User.HasClaim("department", "engineering");
});
```

> **Precedence:** `AllowAnonymous` → `Filter` → `Policy` → `Roles` → require-authenticated

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `KnownQueues` | `string[]` | `["default"]` | Queue names shown in filters |
| `MaxJobsPerStateQuery` | `int` | `1000` | Max jobs fetched per state from Hangfire |
| `DefaultPageSize` | `int` | `25` | Default page size in the jobs table |
| `LayoutPath` | `string?` | `null` | Custom Razor layout path (null = built-in layout) |
| `Authorization` | `HangfireDashboardAuthOptions` | — | See authorisation section above |

## REST API Endpoints

All endpoints are under `/api/v1/hangfire` and protected by the same authorisation filter.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/stats` | Dashboard statistics |
| GET | `/jobs` | Paginated, filtered job list |
| GET | `/jobs/{id}` | Job detail with parameters & state history |
| POST | `/jobs/{id}/requeue` | Requeue a single job |
| DELETE | `/jobs/{id}` | Delete a single job |
| POST | `/jobs/bulk-requeue` | Bulk requeue jobs |
| POST | `/jobs/bulk-delete` | Bulk delete jobs |
| GET | `/queues` | Queue summaries |
| GET | `/servers` | Server summaries |
| GET | `/recurring-jobs` | Recurring job list |

## Custom Layout

To use your own layout instead of the built-in one:

```csharp
builder.Services.AddHangfireDashboard(opts =>
{
    opts.LayoutPath = "~/Views/Shared/_Layout.cshtml";
});
```

The dashboard page will render inside your layout. Ensure your layout includes jQuery 3.7+, Bootstrap 5.3+, Chart.js 4+, and Font Awesome 6+.

## Project Structure

```
Hangfire.Dashboard/
├── Readme.md
├── Directory.Build.props
├── src/
│   └── Hangfire.Dashboard/
│       ├── Hangfire.Dashboard.slnx
│       └── Hangfire.Dashboard/
│           ├── Hangfire.Dashboard.csproj
│           ├── Apis/                      # REST API controller
│           ├── Contracts/                 # DTOs and ApiResponse
│           ├── Extensions/                # DI service registration
│           ├── Filters/                   # Authorization filter
│           ├── Options/                   # Configuration options
│           ├── Services/                  # Hangfire job service
│           ├── Areas/HangfireDashboard/   # Razor Pages & layout
│           └── wwwroot/                   # CSS & JS static assets
├── tests/
│   └── Hangfire.Dashboard.Tests/          # MSTest unit tests
└── samples/
    ├── SampleApp.Net8/                    # AllowAnonymous example
    ├── SampleApp.Net9/                    # Policy-based auth example
    └── SampleApp.Net10/                   # Role-based auth example
```

## Building

```shell
cd src/Hangfire.Dashboard
dotnet build Hangfire.Dashboard.slnx
dotnet pack Hangfire.Dashboard/Hangfire.Dashboard.csproj -c Release
```

## Running Tests

```shell
dotnet test ../../tests/Hangfire.Dashboard.Tests
```

## Running a Sample App

```shell
cd samples/SampleApp.Net8
dotnet run
```

Then open `https://localhost:5001/Jobs` in your browser.

## License

MIT — see [LICENSE](LICENSE) for details.

---

*Developed by [abmalik.com](https://abmalik.com)*
