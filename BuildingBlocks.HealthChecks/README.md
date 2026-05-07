# BuildingBlocks.HealthChecks

Comprehensive health monitoring infrastructure for microservices with support for database, cache, message broker, HTTP endpoint, and system resource health checks.

## Features

- **Multiple Health Check Types**
  - Database: PostgreSQL, SQL Server, Entity Framework DbContext
  - Cache: Redis
  - Message Broker: RabbitMQ
  - HTTP Endpoints
  - System Resources: Memory, Disk Space

- **Kubernetes Integration**
  - Liveness probes (`/health/live`)
  - Readiness probes (`/health/ready`)
  - Startup probes (`/health/startup`)

- **Observability**
  - Serilog logging with state transition tracking
  - OpenTelemetry metrics (execution count, duration, status)
  - Health check UI dashboard

- **Advanced Features**
  - Result caching with configurable intervals
  - Timeout configuration per check
  - Graceful degradation (critical vs non-critical checks)
  - Custom health checks support
  - Publisher pattern for notifications

## Installation

Add the package reference to your project:

```xml
<ItemGroup>
  <ProjectReference Include="..\BuildingBlocks.HealthChecks\BuildingBlocks.HealthChecks.csproj" />
</ItemGroup>
```

## Quick Start

### 1. Configure in appsettings.json

```json
{
  "HealthChecks": {
    "LivenessEndpoint": "/health/live",
    "ReadinessEndpoint": "/health/ready",
    "StartupEndpoint": "/health/startup",
    "UIEndpoint": "/health/ui",
    "DefaultTimeoutSeconds": 5,
    "DefaultCacheIntervalSeconds": 30,
    "EnableUI": true,
    "EnableCaching": true,
    "EnablePublishers": true,
    "Memory": {
      "DegradedThresholdBytes": 1073741824,
      "UnhealthyThresholdBytes": 536870912
    },
    "Disk": {
      "DegradedThresholdBytes": 10737418240,
      "UnhealthyThresholdBytes": 5368709120,
      "MonitoredPath": "/"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=mydb;Username=user;Password=pass"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "RabbitMq": {
    "Host": "localhost",
    "VirtualHost": "/",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

### 2. Register in Program.cs

```csharp
using BuildingBlocks.HealthChecks.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add health checks infrastructure
builder.Services.AddBuildingBlocksHealthChecks(builder.Configuration)
    // Database checks
    .AddPostgreSqlHealthCheck(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql")
    .AddDbContextHealthCheck<ApplicationDbContext>()
    
    // Cache checks
    .AddRedisHealthCheck(
        builder.Configuration["Redis:ConnectionString"]!,
        name: "redis")
    
    // Message broker checks
    .AddRabbitMqHealthCheck(
        builder.Configuration,
        name: "rabbitmq")
    
    // HTTP endpoint checks
    .AddHttpEndpointHealthCheck(
        "https://api.example.com/health",
        name: "external-api")
    
    // System resource checks
    .AddMemoryHealthCheck()
    .AddDiskSpaceHealthCheck();

var app = builder.Build();

// Configure health check endpoints
app.UseBuildingBlocksHealthChecks();

app.Run();
```

## Health Check Types

### Database Health Checks

#### PostgreSQL
```csharp
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddPostgreSqlHealthCheck(
        connectionString: "Host=localhost;Database=mydb;Username=user;Password=pass",
        name: "postgresql",
        timeout: TimeSpan.FromSeconds(5),
        tags: new[] { "database", "critical" });
```

#### SQL Server
```csharp
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddSqlServerHealthCheck(
        connectionString: "Server=localhost;Database=mydb;User Id=sa;Password=pass",
        name: "sqlserver",
        timeout: TimeSpan.FromSeconds(5));
```

#### Entity Framework DbContext
```csharp
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddDbContextHealthCheck<ApplicationDbContext>(
        name: "application-db",
        timeout: TimeSpan.FromSeconds(5));
```

### Cache Health Checks

#### Redis
```csharp
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddRedisHealthCheck(
        connectionString: "localhost:6379",
        name: "redis",
        timeout: TimeSpan.FromSeconds(3));
```

### Message Broker Health Checks

#### RabbitMQ
```csharp
// Using configuration
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddRabbitMqHealthCheck(
        configuration,
        name: "rabbitmq");

// Using connection string
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddRabbitMqHealthCheck(
        connectionString: "amqp://guest:guest@localhost/",
        name: "rabbitmq");
```

### HTTP Endpoint Health Checks

```csharp
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddHttpEndpointHealthCheck(
        uri: new Uri("https://api.example.com/health"),
        name: "external-api",
        httpMethod: HttpMethod.Get,
        timeout: TimeSpan.FromSeconds(10));

// Using string URI
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddHttpEndpointHealthCheck(
        uriString: "https://api.example.com/health",
        name: "external-api");
```

### System Resource Health Checks

#### Memory
```csharp
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddMemoryHealthCheck(
        name: "memory",
        timeout: TimeSpan.FromSeconds(2));
```

#### Disk Space
```csharp
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddDiskSpaceHealthCheck(
        name: "disk",
        timeout: TimeSpan.FromSeconds(2));
```

## Custom Health Checks

Implement `IHealthCheck` interface:

```csharp
public class CustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Your health check logic
        var isHealthy = CheckSomething();
        
        if (isHealthy)
        {
            return Task.FromResult(
                HealthCheckResult.Healthy("Everything is fine"));
        }
        
        return Task.FromResult(
            HealthCheckResult.Unhealthy("Something is wrong"));
    }
}

// Register
builder.Services.AddBuildingBlocksHealthChecks(configuration)
    .AddCheck<CustomHealthCheck>(
        name: "custom-check",
        tags: new[] { "custom", "readiness" });
```

## Kubernetes Configuration

### Deployment YAML

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-service
spec:
  template:
    spec:
      containers:
      - name: my-service
        image: my-service:latest
        ports:
        - containerPort: 8080
        livenessProbe:
          httpGet:
            path: /health/live
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /health/startup
            port: 8080
          initialDelaySeconds: 0
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 30
```

## Health Check Tags

Health checks are automatically tagged for Kubernetes probes:

- **Liveness**: Checks if the application is alive (should be restarted if failing)
- **Readiness**: Checks if the application can receive traffic
- **Startup**: Checks if the application has completed startup
- **Critical**: Unhealthy status results in overall Unhealthy
- **NonCritical**: Unhealthy status results in overall Degraded

## Response Format

Health check endpoints return JSON:

```json
{
  "status": "Healthy",
  "timestamp": "2026-05-06T10:30:00Z",
  "totalDuration": 125.5,
  "entries": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": "Database connection successful",
      "duration": 45.2,
      "exception": null,
      "data": {
        "server": "localhost",
        "database": "mydb"
      },
      "tags": ["database", "readiness", "critical"]
    },
    {
      "name": "redis",
      "status": "Healthy",
      "description": "Redis PING successful",
      "duration": 12.3,
      "exception": null,
      "data": {},
      "tags": ["cache", "readiness", "noncritical"]
    }
  ]
}
```

## Health Check UI

Access the health check dashboard at `/health/ui` (configurable).

The UI provides:
- Real-time health status visualization
- Historical health data
- Detailed check information
- Auto-refresh every 10 seconds

## Observability

### Serilog Logging

Health state transitions are automatically logged:

```
[Error] Health check 'postgresql' transitioned from Healthy to Unhealthy. Duration: 5234ms. Description: Connection timeout. Exception: Npgsql.NpgsqlException: Connection timeout
[Warning] Health check 'redis' transitioned from Healthy to Degraded. Duration: 3456ms. Description: Slow response
[Information] Health check 'postgresql' recovered from Unhealthy to Healthy. Duration: 123ms. Description: Connection successful
```

### OpenTelemetry Metrics

Metrics are automatically exported:

- `healthcheck.executions`: Counter of health check executions (tagged by check name and status)
- `healthcheck.duration`: Histogram of execution durations (tagged by check name and status)
- `healthcheck.status`: Gauge of current status (0=Unhealthy, 1=Degraded, 2=Healthy)

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `LivenessEndpoint` | `/health/live` | Liveness probe endpoint path |
| `ReadinessEndpoint` | `/health/ready` | Readiness probe endpoint path |
| `StartupEndpoint` | `/health/startup` | Startup probe endpoint path |
| `UIEndpoint` | `/health/ui` | Health check UI endpoint path |
| `DefaultTimeoutSeconds` | `5` | Default timeout for health checks (1-60) |
| `DefaultCacheIntervalSeconds` | `30` | Cache interval for results (1-300) |
| `EnableUI` | `true` | Enable health check UI |
| `EnableCaching` | `true` | Enable result caching |
| `EnablePublishers` | `true` | Enable health check publishers |
| `Memory.DegradedThresholdBytes` | `1073741824` (1 GB) | Memory degraded threshold |
| `Memory.UnhealthyThresholdBytes` | `536870912` (512 MB) | Memory unhealthy threshold |
| `Disk.DegradedThresholdBytes` | `10737418240` (10 GB) | Disk degraded threshold |
| `Disk.UnhealthyThresholdBytes` | `5368709120` (5 GB) | Disk unhealthy threshold |
| `Disk.MonitoredPath` | `/` | Path to monitor for disk space |

## License

This project is part of the BuildingBlocks microservice infrastructure.
