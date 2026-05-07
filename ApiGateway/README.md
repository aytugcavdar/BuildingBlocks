# API Gateway/BFF

A production-ready API Gateway and Backend for Frontend (BFF) implementation using .NET 10.0, ASP.NET Core, and YARP (Yet Another Reverse Proxy).

## Overview

The API Gateway provides a unified entry point for client applications to access microservices in the BuildingBlocks infrastructure. It implements both the API Gateway pattern (single entry point for all clients) and the BFF pattern (client-specific backends).

### Key Features

- **Request Routing**: Flexible path-based and method-based routing to downstream services
- **Authentication & Authorization**: JWT token validation and role-based access control
- **Rate Limiting**: Per-route and per-user rate limiting to protect downstream services
- **Caching**: Gateway-level response caching with configurable TTL and vary-by rules
- **Request Aggregation**: Combine multiple downstream service calls into a single response
- **Request/Response Transformation**: Transform headers, JSON fields, and data structures
- **Resilience**: Circuit breakers, retries with exponential backoff, and timeouts
- **Service Discovery**: Kubernetes DNS-based service discovery with static URL fallback
- **Load Balancing**: Round-robin, least-connections, and weighted load balancing
- **Health Checks**: Aggregated health status from downstream services
- **Observability**: OpenTelemetry tracing, Prometheus metrics, and structured logging
- **Security**: Security headers, CORS, request size limits, and sensitive header removal
- **API Versioning**: URL path versioning with default version fallback
- **BFF Support**: Client-specific routes and transformations for web, mobile, and desktop

## Quick Start

### Prerequisites

- .NET 10.0 SDK
- Redis (for distributed caching)
- OpenTelemetry Collector (optional, for tracing)

### Configuration

Configure the gateway in `appsettings.json`:

```json
{
  "Gateway": {
    "Routes": [
      {
        "RouteId": "user-service",
        "UpstreamPathPattern": "/api/users/{**catch-all}",
        "DownstreamServiceUrl": "http://user-service",
        "DownstreamPathTemplate": "/api/users/{**catch-all}",
        "RequireAuthentication": true,
        "RateLimit": {
          "PermitLimit": 100,
          "WindowSeconds": 60
        }
      }
    ],
    "Authentication": {
      "Authority": "https://auth.example.com",
      "Audience": "api-gateway"
    }
  }
}
```

### Running the Gateway

```bash
cd ApiGateway
dotnet run
```

The gateway will start on:
- HTTP: `http://localhost:8080`
- Metrics: `http://localhost:8081/metrics`

### Health Checks

- Liveness: `http://localhost:8080/health/live`
- Readiness: `http://localhost:8080/health/ready`
- Downstream: `http://localhost:8080/health/downstream`

## Documentation

### Core Features

- **[API Versioning Guide](./API_VERSIONING.md)** - Complete guide to API versioning with examples
- **[Configuration Reference](#configuration-reference)** - Detailed configuration options
- **[Middleware Pipeline](#middleware-pipeline)** - Understanding the request processing flow
- **[Observability](#observability)** - Logging, tracing, and metrics

### Configuration Reference

#### Route Configuration

Each route in `Gateway.Routes` supports the following properties:

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `RouteId` | string | Yes | Unique identifier for the route |
| `UpstreamPathPattern` | string | Yes | Path pattern to match incoming requests (e.g., `/api/users/{**catch-all}`) |
| `DownstreamServiceUrl` | string | Yes | Base URL of the downstream service |
| `DownstreamPathTemplate` | string | Yes | Path template for downstream request |
| `HttpMethods` | string[] | No | Allowed HTTP methods (default: all) |
| `Enabled` | bool | No | Whether the route is enabled (default: true) |
| `TimeoutSeconds` | int | No | Request timeout in seconds (default: 30) |
| `RequireAuthentication` | bool | No | Whether authentication is required (default: true) |
| `RequiredRoles` | string[] | No | Required user roles for authorization |
| `RequiredPolicies` | string[] | No | Required authorization policies |
| `ApiVersion` | string | No | API version identifier (e.g., "v1", "v2") |
| `IsDefaultVersion` | bool | No | Whether this is the default version (default: false) |
| `ClientType` | string | No | BFF client type ("web", "mobile", "desktop") |
| `IsAggregation` | bool | No | Whether this is an aggregation endpoint |
| `AggregationTargets` | string[] | No | Downstream services to aggregate |
| `RateLimit` | object | No | Rate limiting configuration |
| `Cache` | object | No | Caching configuration |
| `Transformation` | object | No | Request/response transformation rules |

#### Rate Limiting Configuration

```json
{
  "RateLimit": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "PartitionBy": "user"
  }
}
```

- `PermitLimit`: Maximum number of requests allowed
- `WindowSeconds`: Time window in seconds
- `PartitionBy`: Partition key ("user", "ip", "global")

#### Caching Configuration

```json
{
  "Cache": {
    "Enabled": true,
    "TtlSeconds": 300,
    "VaryByHeaders": ["Accept-Language"],
    "VaryByQueryParams": ["page", "pageSize"]
  }
}
```

- `Enabled`: Whether caching is enabled
- `TtlSeconds`: Time-to-live in seconds
- `VaryByHeaders`: Headers to include in cache key
- `VaryByQueryParams`: Query parameters to include in cache key

#### Transformation Configuration

```json
{
  "Transformation": {
    "RequestHeaderMappings": {
      "X-Old-Header": "X-New-Header"
    },
    "ResponseFieldMappings": {
      "user_id": "userId",
      "user_name": "userName"
    },
    "ResponseFieldsToRemove": ["internal_field"],
    "ResponseFieldsToAdd": {
      "api_version": "v2"
    }
  }
}
```

### Middleware Pipeline

The gateway processes requests through the following middleware pipeline:

1. **Exception Handling** - Global error handling with RFC 7807 Problem Details
2. **CORS** - Cross-Origin Resource Sharing
3. **Security Headers** - Inject security headers (X-Content-Type-Options, X-Frame-Options, etc.)
4. **Correlation ID** - Generate or preserve correlation IDs
5. **Request Size Limit** - Enforce maximum request body size
6. **Authentication** - JWT token validation
7. **Authorization** - Role and policy-based authorization
8. **Rate Limiting** - Per-route and per-user rate limiting
9. **Gateway Cache** - Response caching at gateway level
10. **YARP Proxy** - Reverse proxy to downstream services

### Observability

#### Logging

The gateway uses Serilog for structured logging with the following log levels:

- **Information**: Request routing, cache hits/misses, service discovery
- **Warning**: Rate limit exceeded, circuit breaker state changes
- **Error**: Downstream service failures, transformation errors
- **Debug**: Detailed request/response information (when enabled)

All logs include:
- Correlation ID
- Route ID
- API Version (if configured)
- User ID (if authenticated)

#### Tracing

OpenTelemetry distributed tracing with W3C Trace Context propagation:

- Span created for each incoming request
- Child spans for downstream service calls
- Span attributes include route name, HTTP method, status code, API version
- Trace context propagated to downstream services

#### Metrics

Prometheus metrics exposed at `/metrics`:

| Metric | Type | Description |
|--------|------|-------------|
| `gateway_requests_total` | Counter | Total number of requests by route and status |
| `gateway_request_duration_seconds` | Histogram | Request duration by route |
| `gateway_downstream_calls_total` | Counter | Downstream service calls by service and status |
| `gateway_cache_hits_total` | Counter | Cache hits by route |
| `gateway_cache_misses_total` | Counter | Cache misses by route |
| `gateway_circuit_breaker_state` | Gauge | Circuit breaker state (0=closed, 1=open, 2=half-open) |
| `gateway_rate_limit_rejections_total` | Counter | Rate limit rejections by route |

### API Versioning

The gateway supports URL path versioning with default version fallback. See the [API Versioning Guide](./API_VERSIONING.md) for detailed documentation and examples.

**Quick Example:**

```json
{
  "Routes": [
    {
      "RouteId": "users-v1",
      "UpstreamPathPattern": "/api/v1/users/{**catch-all}",
      "ApiVersion": "v1"
    },
    {
      "RouteId": "users-v2",
      "UpstreamPathPattern": "/api/v2/users/{**catch-all}",
      "ApiVersion": "v2"
    },
    {
      "RouteId": "users-default",
      "UpstreamPathPattern": "/api/users/{**catch-all}",
      "ApiVersion": "v2",
      "IsDefaultVersion": true
    }
  ]
}
```

### Request Aggregation

Combine multiple downstream service calls into a single response:

```json
{
  "RouteId": "user-profile-aggregation",
  "UpstreamPathPattern": "/bff/mobile/user-profile/{userId}",
  "IsAggregation": true,
  "AggregationTargets": ["user-service", "order-service", "preference-service"]
}
```

The gateway will call all target services in parallel and return a combined response:

```json
{
  "responses": {
    "user-service": {
      "success": true,
      "statusCode": 200,
      "data": { "userId": "123", "name": "John Doe" }
    },
    "order-service": {
      "success": true,
      "statusCode": 200,
      "data": { "orders": [...] }
    }
  },
  "totalDuration": "00:00:00.245",
  "hasErrors": false
}
```

### BFF Pattern

Create client-specific routes with optimized responses:

```json
{
  "Routes": [
    {
      "RouteId": "mobile-dashboard",
      "UpstreamPathPattern": "/bff/mobile/dashboard",
      "ClientType": "mobile",
      "IsAggregation": true,
      "AggregationTargets": ["user-service", "notification-service"],
      "Transformation": {
        "ResponseFieldsToRemove": ["internal_metadata"]
      }
    },
    {
      "RouteId": "web-dashboard",
      "UpstreamPathPattern": "/bff/web/dashboard",
      "ClientType": "web",
      "IsAggregation": true,
      "AggregationTargets": ["user-service", "notification-service", "analytics-service"]
    }
  ]
}
```

## Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY bin/Release/net10.0/publish/ .
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
```

### Kubernetes

See the [Kubernetes deployment manifests](../.kiro/specs/api-gateway-bff/design.md#deployment) in the design document.

Key features:
- 3 replicas for high availability
- Liveness and readiness probes
- Horizontal pod autoscaling (3-10 replicas)
- ConfigMap for configuration
- Secrets for sensitive data

### Environment Variables

Override configuration using environment variables:

```bash
Gateway__Authentication__Authority=https://auth.example.com
Gateway__Cache__ConnectionString=redis:6379
Gateway__Observability__EnableTracing=true
```

## Security

### Authentication

JWT token validation using BuildingBlocks.Security:

```json
{
  "Authentication": {
    "Enabled": true,
    "Authority": "https://auth.example.com",
    "Audience": "api-gateway",
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true,
    "ClockSkewSeconds": 300
  }
}
```

### Authorization

Role-based and policy-based authorization:

```json
{
  "RouteId": "admin-api",
  "RequireAuthentication": true,
  "RequiredRoles": ["Admin"],
  "RequiredPolicies": ["AdminPolicy"]
}
```

### Security Headers

Automatically added to all responses:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Strict-Transport-Security` (for HTTPS)

Sensitive headers removed from downstream responses:
- `Server`
- `X-Powered-By`
- `X-AspNet-Version`

### Request Size Limits

```json
{
  "Security": {
    "MaxRequestBodySizeBytes": 10485760
  }
}
```

## Troubleshooting

### Common Issues

**Issue: 401 Unauthorized**
- Check JWT token is valid and not expired
- Verify `Authentication.Authority` matches token issuer
- Ensure `Authentication.Audience` matches token audience

**Issue: 429 Too Many Requests**
- Rate limit exceeded
- Check `Retry-After` header for wait time
- Adjust `RateLimit.PermitLimit` if needed

**Issue: 503 Service Unavailable**
- Circuit breaker is open
- Check downstream service health
- Review circuit breaker configuration

**Issue: Cache not working**
- Ensure Redis is running and accessible
- Check `Cache.Enabled` is true
- Verify route uses GET method (only GET requests are cached)

### Logs

View structured logs:

```bash
# View all logs
dotnet run | jq

# Filter by correlation ID
dotnet run | jq 'select(.CorrelationId == "abc-123")'

# Filter by route
dotnet run | jq 'select(.RouteId == "user-service")'
```

### Metrics

Query Prometheus metrics:

```bash
# Request rate
rate(gateway_requests_total[5m])

# Error rate
rate(gateway_requests_total{status=~"5.."}[5m])

# Cache hit rate
gateway_cache_hits_total / (gateway_cache_hits_total + gateway_cache_misses_total)
```

## Development

### Building

```bash
dotnet build ApiGateway/ApiGateway.csproj
```

### Running Tests

```bash
dotnet test ApiGateway.Tests/ApiGateway.Tests.csproj
```

### Local Development

1. Start Redis:
   ```bash
   docker run -d -p 6379:6379 redis:latest
   ```

2. Configure local settings in `appsettings.Development.json`

3. Run the gateway:
   ```bash
   dotnet run --project ApiGateway/ApiGateway.csproj
   ```

## References

- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [BuildingBlocks Architecture](../../README.md)
- [Requirements Document](../.kiro/specs/api-gateway-bff/requirements.md)
- [Design Document](../.kiro/specs/api-gateway-bff/design.md)
- [API Versioning Guide](./API_VERSIONING.md)

## License

This project is part of the BuildingBlocks infrastructure.
