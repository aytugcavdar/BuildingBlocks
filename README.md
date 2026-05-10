# BuildingBlocks

Reusable .NET building blocks for microservice projects.

This repository is being shaped as a generic infrastructure toolkit that can be reused whenever a new microservice is needed. The goal is to keep domain code out of the shared packages and provide clean, composable defaults for logging, security, caching, messaging, health checks, resilience, and persistence support.

## Current Status

The project is in an active hardening phase.

- Builds cleanly with `0` warnings and `0` errors.
- Test suite currently passes with `91` tests.
- Known vulnerable NuGet package scan is clean.
- Domain-specific messaging artifacts from the old basket/order/payment project have been removed from `BuildingBlocks.Messaging`.
- A composition package now exists for services that want to opt into the full stack with one setup call.

## Repository Layout

```text
BuildingBlocks.Core             Core abstractions, domain primitives, paging, repository contracts
BuildingBlocks.CrossCutting     Caching, validation, exception handling, authentication helpers, locking, rate limiting
BuildingBlocks.Infrastructure   EF Core repository, unit of work, audit, outbox/inbox infrastructure
BuildingBlocks.Security         JWT helpers, password hashing, claims, email auth support
BuildingBlocks.Logging          Serilog and OpenTelemetry setup helpers
BuildingBlocks.Messaging        MassTransit/RabbitMQ setup, integration event contracts, email/SMS/template services
BuildingBlocks.HealthChecks     Health check registration, publishers, telemetry, endpoint helpers
BuildingBlocks.Composition      One-stop registration layer for services that want the full default stack

ApiGateway                      Optional API Gateway/BFF application
ApiGateway.Tests                Current test project
```

## Design Direction

The packages are intended to stay modular.

If a service only needs logging, it should not be forced to pull in RabbitMQ, Redis, or EF Core. If a service wants the full default stack, it can use `BuildingBlocks.Composition`.

```text
Use one package when the service needs one concern.
Use BuildingBlocks.Composition when the service wants the standard platform setup.
Keep domain events and business models inside the owning microservice.
```

## Quick Start

For a typical ASP.NET Core microservice that wants the default stack:

```csharp
using BuildingBlocks.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.AddBuildingBlocksDefaults(options =>
{
    options.ApplicationName = "Catalog.Api";
    options.ConsumersAssembly = typeof(Program).Assembly;

    options.UseJwtAuthentication = true;
    options.UseRateLimiting = true;
});

var app = builder.Build();

app.UseBuildingBlocksDefaults(options =>
{
    options.UseJwtAuthentication = true;
    options.UseRateLimiting = true;
});

app.MapControllers();
app.Run();
```

For partial adoption:

```csharp
builder.AddBuildingBlocksLogging("Catalog.Api");

builder.Services.AddBuildingBlocksCaching(builder.Configuration);
builder.Services.AddBuildingBlocksMessaging(builder.Configuration, typeof(Program).Assembly);
builder.Services.AddBuildingBlocksHealthChecks(builder.Configuration);
builder.Services.AddBuildingBlocksSecurity();
```

## Configuration

The preferred configuration namespace is `BuildingBlocks`.

```json
{
  "BuildingBlocks": {
    "Caching": {
      "EnableL1Cache": true,
      "EnableL2Cache": true,
      "RedisConnectionString": "localhost:6379",
      "DefaultTtlSeconds": 300,
      "EnableCompression": true,
      "CompressionThresholdBytes": 1024
    },
    "Messaging": {
      "RabbitMQ": {
        "Host": "localhost",
        "VirtualHost": "/",
        "UserName": "guest",
        "Password": "guest"
      },
      "Email": {
        "SmtpServer": "",
        "SmtpPort": 587,
        "SenderEmail": "",
        "SenderName": "",
        "Username": "",
        "Password": "",
        "EnableSsl": true
      }
    },
    "Security": {
      "TokenOptions": {
        "Audience": "my-service",
        "Issuer": "my-issuer",
        "SecurityKey": "replace-with-at-least-32-characters",
        "AccessTokenExpiration": 60
      }
    },
    "HealthChecks": {
      "LivenessEndpoint": "/health/live",
      "ReadinessEndpoint": "/health/ready",
      "StartupEndpoint": "/health/startup"
    }
  }
}
```

Legacy section names are still supported as fallback for now:

```text
CacheSettings
RabbitMQ
EmailOptions
TokenOptions
HealthChecks
```

## Public Registration API

Current preferred entry points:

```csharp
builder.AddBuildingBlocksLogging("ServiceName");

services.AddBuildingBlocksSecurity();
services.AddBuildingBlocksJwtAuthentication(configuration);
services.AddBuildingBlocksCaching(configuration);
services.AddBuildingBlocksMessaging(configuration, consumersAssembly);
services.AddBuildingBlocksDistributedLocking(configuration);
services.AddBuildingBlocksRateLimiting();
services.AddBuildingBlocksExceptionHandling();
services.AddBuildingBlocksHealthChecks(configuration);
```

Composition entry points:

```csharp
builder.AddBuildingBlocksDefaults();
app.UseBuildingBlocksDefaults();
```

## Microservice Template

Install the local template:

```powershell
dotnet new install .\templates\microservice --force
```

Create a new service:

```powershell
dotnet new bb-microservice -n Catalog.Api -o .\samples\Catalog.Api
```

Before the packages are published to an internal feed, restore generated services with the local package output:

```powershell
dotnet restore .\samples\Catalog.Api\Catalog.Api.csproj "--source=.\artifacts\packages" "--source=https://api.nuget.org/v3/index.json"
```

The generated service starts with memory caching and health checks enabled. Messaging, JWT, distributed locking, SMS, and rate limiting are disabled by default so the app can run without Redis or RabbitMQ.

## Messaging Scope

`BuildingBlocks.Messaging` is intentionally domain-neutral.

It contains:

- `IIntegrationEvent`
- `IntegrationEvent`
- MassTransit/RabbitMQ registration helpers
- Email, SMS, and template service abstractions/implementations

It does not contain:

- Basket events
- Order events
- Payment events
- Stock events
- User domain events

Each microservice should define and own its own integration events.

## Verification

Use these commands from the repository root:

```powershell
dotnet build BuildingBlocks.sln -v minimal
dotnet test BuildingBlocks.sln -v minimal
Get-ChildItem -Path . -Directory -Filter 'BuildingBlocks.*' |
  Where-Object { $_.Name -notlike '*.Tests' } |
  ForEach-Object { dotnet pack (Join-Path $_.FullName ($_.Name + '.csproj')) --configuration Release --no-build -v minimal }
dotnet list BuildingBlocks.sln package --vulnerable --include-transitive
```

Expected current result:

```text
Build: 0 warnings, 0 errors
Tests: 91 passed
Packages: generated under artifacts/packages
Vulnerable packages: none reported
```

## Upgrade Roadmap

### 1. HealthChecks Dependency Cleanup

Status: completed.

Goal: make `BuildingBlocks.HealthChecks` independent from `BuildingBlocks.Messaging` unless a service explicitly opts into RabbitMQ health checks.

Expected outcome:

- Health checks can be used without pulling messaging/email dependencies.
- RabbitMQ health checks remain available through configuration or optional registration.

### 2. Messaging Publish Abstraction

Status: completed.

Goal: add a thin publish abstraction without hiding MassTransit too much.

Candidate API:

```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent;
}
```

Expected outcome:

- Application code can publish integration events without depending directly on `IPublishEndpoint`.
- Consumers can still use MassTransit directly where it makes sense.

### 3. BuildingBlocks Test Projects

Status: in progress.

Goal: move coverage from mostly gateway-focused tests to package-level tests.

Planned projects:

```text
BuildingBlocks.CrossCutting.Tests   created
BuildingBlocks.Messaging.Tests      created
BuildingBlocks.Security.Tests       created
BuildingBlocks.HealthChecks.Tests   created
BuildingBlocks.Infrastructure.Tests created
```

First focus:

- Cache compression and L1/L2 behavior
- Messaging registration and publish abstraction
- JWT helper behavior
- Outbox/inbox processing

### 4. Central Package Management

Status: completed.

Goal: introduce `Directory.Packages.props`.

Expected outcome:

- Package versions are managed in one place.
- Version drift between packages is easier to detect.
- Security updates become less repetitive.

### 5. Analyzer and Formatting Baseline

Status: completed.

Goal: add `.editorconfig`, analyzer rules, and optional warnings-as-errors for package code.

Expected outcome:

- Consistent style across packages.
- Nullable and API quality issues are caught earlier.
- CI can enforce the baseline.

### 6. CI Quality Gate

Status: completed.

Goal: add GitHub Actions for build, test, and package vulnerability scan.

Expected outcome:

- Every pull request runs:

```text
dotnet restore
dotnet build
dotnet test
dotnet pack
dotnet list package --vulnerable --include-transitive
```

### 7. NuGet Packaging Strategy

Status: completed.

Goal: prepare packages for internal or public NuGet distribution.

Expected outcome:

- Package metadata is consistent.
- Semantic versioning is clear.
- Consumers can reference only the packages they need.

Current package baseline:

```text
VersionPrefix: 0.1.0
Output: artifacts/packages
Packages: BuildingBlocks.Core, CrossCutting, HealthChecks, Infrastructure, Logging, Messaging, Security, Composition
Symbols: snupkg
```

### 8. Microservice Template

Status: completed.

Goal: add a service starter template after the packages stabilize.

Expected outcome:

- New services can start with a consistent API/Application/Domain/Infrastructure/Test layout.
- Shared behavior still comes from packages, not copied template code.

## Current Priority

The immediate next engineering priority is:

```text
Package hardening backlog
```

The reusable package baseline is now in place. The next phase is hardening: API docs, stricter analyzer rules, package dependency trimming, and real sample services.
