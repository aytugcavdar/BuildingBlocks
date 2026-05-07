# API Versioning Implementation Summary

## Overview

This document summarizes the implementation of API versioning support (Task 15.1) for the API Gateway/BFF.

## Implementation Date

January 2025

## Requirements Addressed

- **Requirement 21.1**: URL path versioning support (e.g., `/api/v1/users`, `/api/v2/users`)
- **Requirement 21.4**: Default version fallback when no version is specified

## Changes Made

### 1. RouteConfig Model Enhancement

**File**: `ApiGateway/Configuration/RouteConfig.cs`

Added two new properties to support API versioning:

```csharp
public string? ApiVersion { get; set; } // API version (e.g., "v1", "v2")
public bool IsDefaultVersion { get; set; } = false; // Whether this is the default version
```

**Purpose**:
- `ApiVersion`: Identifies the API version for the route (e.g., "v1", "v2", "2.0")
- `IsDefaultVersion`: Marks a route as the default version to handle requests without explicit version

### 2. YarpRouteBuilder Metadata Enhancement

**File**: `ApiGateway/Configuration/YarpRouteBuilder.cs`

Added version metadata to YARP route configuration:

```csharp
// Add API version metadata
if (!string.IsNullOrEmpty(route.ApiVersion))
{
    metadata["ApiVersion"] = route.ApiVersion;
    metadata["IsDefaultVersion"] = route.IsDefaultVersion.ToString();
}
```

**Purpose**:
- Makes version information available to middleware and logging
- Enables version-specific metrics and tracing
- Supports future version-based routing logic

### 3. Configuration Example

**File**: `ApiGateway/appsettings.json`

Updated with versioned route examples:

```json
{
  "Routes": [
    {
      "RouteId": "user-service-v1",
      "UpstreamPathPattern": "/api/v1/users/{**catch-all}",
      "DownstreamPathTemplate": "/api/v1/users/{**catch-all}",
      "ApiVersion": "v1",
      "IsDefaultVersion": false
    },
    {
      "RouteId": "user-service-v2",
      "UpstreamPathPattern": "/api/v2/users/{**catch-all}",
      "DownstreamPathTemplate": "/api/v2/users/{**catch-all}",
      "ApiVersion": "v2",
      "IsDefaultVersion": false
    },
    {
      "RouteId": "user-service",
      "UpstreamPathPattern": "/api/users/{**catch-all}",
      "DownstreamPathTemplate": "/api/v2/users/{**catch-all}",
      "ApiVersion": "v2",
      "IsDefaultVersion": true
    }
  ]
}
```

**Purpose**:
- Demonstrates versioned routing configuration
- Shows default version fallback pattern
- Provides working example for developers

### 4. Comprehensive Documentation

**File**: `ApiGateway/API_VERSIONING.md`

Created comprehensive guide covering:
- URL path versioning strategy
- Configuration properties and examples
- Common scenarios (version migration, separate services, default fallback)
- Best practices (semantic versioning, deprecation, monitoring)
- Observability (logs, metrics, tracing)
- Migration strategy (4-phase approach)
- Troubleshooting guide
- Complete configuration examples

**Purpose**:
- Educate developers on API versioning patterns
- Provide copy-paste configuration examples
- Document best practices and common pitfalls
- Enable self-service troubleshooting

### 5. Main README Update

**File**: `ApiGateway/README.md`

Created comprehensive README with:
- Overview of all gateway features
- Quick start guide
- Configuration reference
- API versioning section with link to detailed guide
- Observability documentation
- Deployment instructions
- Security guidelines
- Troubleshooting section

**Purpose**:
- Central documentation hub for the API Gateway
- Quick reference for common tasks
- Links to detailed feature documentation

## How It Works

### Version Extraction

Version is extracted from the URL path pattern using YARP's built-in path matching:

1. Request arrives: `GET /api/v2/users/123`
2. YARP matches against route patterns in order
3. Route with pattern `/api/v2/users/{**catch-all}` matches
4. Request is routed to configured downstream service
5. Version metadata is available for logging and metrics

### Default Version Fallback

When no version is specified in the URL:

1. Request arrives: `GET /api/users/123`
2. YARP matches against route patterns
3. Route with pattern `/api/users/{**catch-all}` and `IsDefaultVersion: true` matches
4. Request is routed to the default version's downstream service
5. Downstream path template can map to a specific version (e.g., `/api/v2/users/{**catch-all}`)

### Route Matching Order

YARP matches routes in the order they appear in configuration. For versioning to work correctly:

1. More specific patterns (with version) should come first
2. Default pattern (without version) should come last
3. Example order:
   - `/api/v2/users/{**catch-all}` (most specific)
   - `/api/v1/users/{**catch-all}` (specific)
   - `/api/users/{**catch-all}` (default, least specific)

## Design Decisions

### 1. Leverage YARP's Path Matching

**Decision**: Use YARP's built-in path pattern matching instead of custom middleware.

**Rationale**:
- YARP already provides powerful path matching with parameter extraction
- No need to reinvent the wheel
- Better performance (compiled route matching)
- Consistent with existing routing logic

**Trade-offs**:
- Requires explicit route configuration for each version
- Cannot dynamically route based on version without configuration

### 2. Metadata-Based Approach

**Decision**: Store version information in route metadata rather than custom routing logic.

**Rationale**:
- Keeps version information available for observability
- Enables future enhancements (version-specific middleware)
- Follows YARP's extensibility patterns
- Minimal code changes required

**Trade-offs**:
- Version information is informational, not functional
- Actual routing is still based on path patterns

### 3. Configuration-First Approach

**Decision**: Require explicit configuration for each versioned route.

**Rationale**:
- Clear and explicit versioning strategy
- Easy to understand and debug
- Supports different downstream services per version
- Allows version-specific policies (rate limiting, caching)

**Trade-offs**:
- More verbose configuration
- Manual updates needed for new versions

### 4. URL Path Versioning Only

**Decision**: Implement URL path versioning, not header-based versioning.

**Rationale**:
- URL path versioning is more explicit and discoverable
- Works with all HTTP clients and tools
- Cacheable at CDN/proxy level
- Easier to test and debug
- Industry standard (REST APIs)

**Trade-offs**:
- URLs change between versions
- Cannot version without changing URL

## Testing Strategy

### Manual Testing

Test versioned routing:

```bash
# Test v1 endpoint
curl http://localhost:8080/api/v1/users

# Test v2 endpoint
curl http://localhost:8080/api/v2/users

# Test default version (should route to v2)
curl http://localhost:8080/api/users
```

### Property-Based Testing

Future tasks will implement property-based tests:

- **Property 22**: API version routing correctness
- **Property 23**: Default version fallback behavior

### Integration Testing

Verify version metadata in logs and metrics:

```bash
# Check logs include version
curl http://localhost:8080/api/v2/users
# Verify log entry includes: "ApiVersion": "v2"

# Check metrics include version label
curl http://localhost:8081/metrics | grep gateway_requests_total
# Verify metric includes: version="v2"
```

## Observability

### Logs

Version information is automatically included in structured logs:

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "message": "Request routed to downstream service",
  "routeId": "user-service-v2",
  "apiVersion": "v2",
  "path": "/api/v2/users/123",
  "correlationId": "abc-123"
}
```

### Metrics

Version-specific metrics are available:

```
gateway_requests_total{route="user-service-v2",version="v2",status="200"} 1234
gateway_request_duration_seconds{route="user-service-v2",version="v2"} 0.045
```

### Tracing

OpenTelemetry spans include version attributes:

```
Span: gateway-request
  Attributes:
    - api.version: "v2"
    - route.id: "user-service-v2"
    - http.method: "GET"
    - http.target: "/api/v2/users/123"
```

## Migration Path

For existing deployments without versioning:

### Phase 1: Add Version Metadata

Add `ApiVersion` to existing routes without changing URLs:

```json
{
  "RouteId": "user-service",
  "UpstreamPathPattern": "/api/users/{**catch-all}",
  "ApiVersion": "v1",
  "IsDefaultVersion": true
}
```

### Phase 2: Introduce New Version

Add new versioned routes alongside existing:

```json
{
  "RouteId": "user-service-v2",
  "UpstreamPathPattern": "/api/v2/users/{**catch-all}",
  "ApiVersion": "v2",
  "IsDefaultVersion": false
}
```

### Phase 3: Migrate Default

Update default version to point to new version:

```json
{
  "RouteId": "user-service",
  "UpstreamPathPattern": "/api/users/{**catch-all}",
  "DownstreamPathTemplate": "/api/v2/users/{**catch-all}",
  "ApiVersion": "v2",
  "IsDefaultVersion": true
}
```

### Phase 4: Deprecate Old Version

Disable old version route:

```json
{
  "RouteId": "user-service-v1",
  "Enabled": false
}
```

## Future Enhancements

Potential future improvements:

1. **Header-Based Versioning**: Support `X-API-Version` header
2. **Version Negotiation**: Content negotiation for version selection
3. **Version Deprecation Warnings**: Add `Deprecated` header to old versions
4. **Version Analytics**: Dashboard showing version usage over time
5. **Automatic Version Detection**: Detect version from downstream service responses
6. **Version-Specific Middleware**: Apply different middleware based on version

## References

- [Requirements Document - Requirement 21](../.kiro/specs/api-gateway-bff/requirements.md#requirement-21-api-versioning-support)
- [Design Document - Properties 22 & 23](../.kiro/specs/api-gateway-bff/design.md#property-22-api-version-routing)
- [API Versioning Guide](./API_VERSIONING.md)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)

## Conclusion

The API versioning implementation provides a solid foundation for managing multiple API versions in the gateway. The configuration-first approach using YARP's path matching is simple, performant, and maintainable. The comprehensive documentation ensures developers can easily adopt and use versioning in their projects.
