# API Versioning Guide

## Overview

The API Gateway supports API versioning to enable multiple API versions to coexist during migrations and evolution. This allows you to:

- Maintain backward compatibility while introducing breaking changes
- Gradually migrate clients from old to new API versions
- Route requests to version-specific downstream service endpoints
- Provide a default version for clients that don't specify a version

## Versioning Strategies

### 1. URL Path Versioning (Recommended)

Version is specified in the URL path (e.g., `/api/v1/users`, `/api/v2/users`).

**Advantages:**
- Clear and explicit versioning
- Easy to test and debug
- Works with all HTTP clients
- Cacheable at CDN/proxy level

**Configuration Example:**

```json
{
  "Gateway": {
    "Routes": [
      {
        "RouteId": "user-service-v1",
        "UpstreamPathPattern": "/api/v1/users/{**catch-all}",
        "DownstreamServiceUrl": "http://user-service-v1",
        "DownstreamPathTemplate": "/api/users/{**catch-all}",
        "ApiVersion": "v1",
        "IsDefaultVersion": false,
        "Enabled": true
      },
      {
        "RouteId": "user-service-v2",
        "UpstreamPathPattern": "/api/v2/users/{**catch-all}",
        "DownstreamServiceUrl": "http://user-service-v2",
        "DownstreamPathTemplate": "/api/users/{**catch-all}",
        "ApiVersion": "v2",
        "IsDefaultVersion": true,
        "Enabled": true
      }
    ]
  }
}
```

### 2. Header-Based Versioning

Version is specified in a custom header (e.g., `X-API-Version: 2`).

**Note:** While the gateway supports version metadata, header-based routing requires custom middleware. For most use cases, URL path versioning is recommended.

## Configuration Properties

### RouteConfig Properties

- **`ApiVersion`** (string, optional): The API version identifier (e.g., "v1", "v2", "2.0")
- **`IsDefaultVersion`** (bool, default: false): Whether this route should handle requests without a version specified

## Common Scenarios

### Scenario 1: Simple Version Migration

You have a v1 API and want to introduce v2 with breaking changes:

```json
{
  "Gateway": {
    "Routes": [
      {
        "RouteId": "orders-v1",
        "UpstreamPathPattern": "/api/v1/orders/{**catch-all}",
        "DownstreamServiceUrl": "http://order-service:8080",
        "DownstreamPathTemplate": "/api/v1/orders/{**catch-all}",
        "ApiVersion": "v1",
        "IsDefaultVersion": false,
        "RequireAuthentication": true
      },
      {
        "RouteId": "orders-v2",
        "UpstreamPathPattern": "/api/v2/orders/{**catch-all}",
        "DownstreamServiceUrl": "http://order-service:8080",
        "DownstreamPathTemplate": "/api/v2/orders/{**catch-all}",
        "ApiVersion": "v2",
        "IsDefaultVersion": true,
        "RequireAuthentication": true
      }
    ]
  }
}
```

**Client Usage:**
```bash
# Old clients using v1
curl https://api.example.com/api/v1/orders

# New clients using v2
curl https://api.example.com/api/v2/orders

# Clients without version (routes to v2, the default)
curl https://api.example.com/api/orders
```

### Scenario 2: Separate Downstream Services per Version

Different versions route to completely separate downstream services:

```json
{
  "Gateway": {
    "Routes": [
      {
        "RouteId": "products-v1",
        "UpstreamPathPattern": "/api/v1/products/{**catch-all}",
        "DownstreamServiceUrl": "http://product-service-v1:8080",
        "DownstreamPathTemplate": "/api/products/{**catch-all}",
        "ApiVersion": "v1",
        "IsDefaultVersion": false
      },
      {
        "RouteId": "products-v2",
        "UpstreamPathPattern": "/api/v2/products/{**catch-all}",
        "DownstreamServiceUrl": "http://product-service-v2:8080",
        "DownstreamPathTemplate": "/api/products/{**catch-all}",
        "ApiVersion": "v2",
        "IsDefaultVersion": true
      }
    ]
  }
}
```

### Scenario 3: Default Version Fallback

Provide a default version for clients that don't specify a version:

```json
{
  "Gateway": {
    "Routes": [
      {
        "RouteId": "users-v1",
        "UpstreamPathPattern": "/api/v1/users/{**catch-all}",
        "DownstreamServiceUrl": "http://user-service",
        "DownstreamPathTemplate": "/api/v1/users/{**catch-all}",
        "ApiVersion": "v1",
        "IsDefaultVersion": false
      },
      {
        "RouteId": "users-v2",
        "UpstreamPathPattern": "/api/v2/users/{**catch-all}",
        "DownstreamServiceUrl": "http://user-service",
        "DownstreamPathTemplate": "/api/v2/users/{**catch-all}",
        "ApiVersion": "v2",
        "IsDefaultVersion": false
      },
      {
        "RouteId": "users-default",
        "UpstreamPathPattern": "/api/users/{**catch-all}",
        "DownstreamServiceUrl": "http://user-service",
        "DownstreamPathTemplate": "/api/v2/users/{**catch-all}",
        "ApiVersion": "v2",
        "IsDefaultVersion": true
      }
    ]
  }
}
```

**Behavior:**
- `/api/v1/users` → routes to v1 endpoint
- `/api/v2/users` → routes to v2 endpoint
- `/api/users` → routes to v2 endpoint (default version)

### Scenario 4: Version-Specific Caching and Rate Limiting

Different versions can have different caching and rate limiting policies:

```json
{
  "Gateway": {
    "Routes": [
      {
        "RouteId": "api-v1",
        "UpstreamPathPattern": "/api/v1/data/{**catch-all}",
        "DownstreamServiceUrl": "http://data-service",
        "DownstreamPathTemplate": "/api/v1/data/{**catch-all}",
        "ApiVersion": "v1",
        "IsDefaultVersion": false,
        "Cache": {
          "Enabled": true,
          "TtlSeconds": 600
        },
        "RateLimit": {
          "PermitLimit": 50,
          "WindowSeconds": 60,
          "PartitionBy": "user"
        }
      },
      {
        "RouteId": "api-v2",
        "UpstreamPathPattern": "/api/v2/data/{**catch-all}",
        "DownstreamServiceUrl": "http://data-service",
        "DownstreamPathTemplate": "/api/v2/data/{**catch-all}",
        "ApiVersion": "v2",
        "IsDefaultVersion": true,
        "Cache": {
          "Enabled": true,
          "TtlSeconds": 300
        },
        "RateLimit": {
          "PermitLimit": 100,
          "WindowSeconds": 60,
          "PartitionBy": "user"
        }
      }
    ]
  }
}
```

## Best Practices

### 1. Use Semantic Versioning

Use clear version identifiers:
- ✅ Good: `v1`, `v2`, `v3`
- ✅ Good: `v1.0`, `v2.0`, `v2.1`
- ❌ Avoid: `latest`, `new`, `old`

### 2. Set a Default Version

Always configure one route as the default version (`IsDefaultVersion: true`) to handle requests without explicit version:

```json
{
  "RouteId": "users-default",
  "UpstreamPathPattern": "/api/users/{**catch-all}",
  "ApiVersion": "v2",
  "IsDefaultVersion": true
}
```

### 3. Document Version Deprecation

When deprecating a version:
1. Announce deprecation timeline to API consumers
2. Keep the deprecated version running for a grace period
3. Monitor usage metrics to ensure migration is complete
4. Disable the route by setting `"Enabled": false`

### 4. Version-Specific Transformations

Use transformation rules to adapt between versions:

```json
{
  "RouteId": "users-v1",
  "UpstreamPathPattern": "/api/v1/users/{**catch-all}",
  "DownstreamServiceUrl": "http://user-service",
  "DownstreamPathTemplate": "/api/v2/users/{**catch-all}",
  "ApiVersion": "v1",
  "Transformation": {
    "ResponseFieldMappings": {
      "userId": "user_id",
      "userName": "user_name"
    }
  }
}
```

### 5. Monitor Version Usage

Track version usage through metrics and logs:
- The gateway includes `ApiVersion` in route metadata
- Use observability tools to monitor which versions are being used
- Make data-driven decisions about version deprecation

## Observability

### Logs

The gateway includes API version in log messages:

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "message": "Request routed to downstream service",
  "routeId": "users-v2",
  "apiVersion": "v2",
  "path": "/api/v2/users/123",
  "correlationId": "abc-123"
}
```

### Metrics

Version-specific metrics are available:
- `gateway_requests_total{route="users-v2",version="v2"}`
- `gateway_request_duration_seconds{route="users-v2",version="v2"}`

### Tracing

OpenTelemetry spans include version information:
- Span attribute: `api.version = "v2"`
- Span attribute: `route.id = "users-v2"`

## Migration Strategy

### Phase 1: Introduce New Version

1. Deploy new version alongside existing version
2. Configure both routes in the gateway
3. Set old version as default

```json
{
  "Routes": [
    {
      "RouteId": "api-v1",
      "UpstreamPathPattern": "/api/v1/resource/{**catch-all}",
      "ApiVersion": "v1",
      "IsDefaultVersion": true
    },
    {
      "RouteId": "api-v2",
      "UpstreamPathPattern": "/api/v2/resource/{**catch-all}",
      "ApiVersion": "v2",
      "IsDefaultVersion": false
    }
  ]
}
```

### Phase 2: Migrate Clients

1. Update client applications to use v2
2. Monitor v1 usage metrics
3. Communicate with remaining v1 users

### Phase 3: Switch Default Version

1. Update configuration to make v2 the default
2. Add default route pointing to v2

```json
{
  "Routes": [
    {
      "RouteId": "api-v1",
      "UpstreamPathPattern": "/api/v1/resource/{**catch-all}",
      "ApiVersion": "v1",
      "IsDefaultVersion": false
    },
    {
      "RouteId": "api-v2",
      "UpstreamPathPattern": "/api/v2/resource/{**catch-all}",
      "ApiVersion": "v2",
      "IsDefaultVersion": false
    },
    {
      "RouteId": "api-default",
      "UpstreamPathPattern": "/api/resource/{**catch-all}",
      "ApiVersion": "v2",
      "IsDefaultVersion": true
    }
  ]
}
```

### Phase 4: Deprecate Old Version

1. Announce deprecation date
2. Monitor v1 usage (should be minimal)
3. Disable v1 route: `"Enabled": false`
4. Remove v1 route from configuration

## Troubleshooting

### Issue: Requests not routing to correct version

**Symptoms:** Requests to `/api/v2/users` are routing to v1 service

**Solution:** Check route order and path patterns. YARP matches routes in order, so ensure more specific patterns come first:

```json
{
  "Routes": [
    {
      "RouteId": "users-v2",
      "UpstreamPathPattern": "/api/v2/users/{**catch-all}"
    },
    {
      "RouteId": "users-v1",
      "UpstreamPathPattern": "/api/v1/users/{**catch-all}"
    },
    {
      "RouteId": "users-default",
      "UpstreamPathPattern": "/api/users/{**catch-all}"
    }
  ]
}
```

### Issue: Default version not working

**Symptoms:** Requests to `/api/users` return 404

**Solution:** Ensure you have a route without version in the path pattern and `IsDefaultVersion: true`:

```json
{
  "RouteId": "users-default",
  "UpstreamPathPattern": "/api/users/{**catch-all}",
  "IsDefaultVersion": true
}
```

### Issue: Version metadata not appearing in logs

**Symptoms:** Logs don't show API version

**Solution:** Ensure `ApiVersion` is set in route configuration. The gateway automatically includes it in structured logs.

## Examples

### Complete Configuration Example

```json
{
  "Gateway": {
    "Routes": [
      {
        "RouteId": "users-v1",
        "UpstreamPathPattern": "/api/v1/users/{**catch-all}",
        "DownstreamServiceUrl": "http://user-service",
        "DownstreamPathTemplate": "/api/v1/users/{**catch-all}",
        "HttpMethods": ["GET", "POST", "PUT", "DELETE"],
        "Enabled": true,
        "TimeoutSeconds": 30,
        "RequireAuthentication": true,
        "ApiVersion": "v1",
        "IsDefaultVersion": false,
        "RateLimit": {
          "PermitLimit": 50,
          "WindowSeconds": 60,
          "PartitionBy": "user"
        },
        "Cache": {
          "Enabled": true,
          "TtlSeconds": 600
        }
      },
      {
        "RouteId": "users-v2",
        "UpstreamPathPattern": "/api/v2/users/{**catch-all}",
        "DownstreamServiceUrl": "http://user-service",
        "DownstreamPathTemplate": "/api/v2/users/{**catch-all}",
        "HttpMethods": ["GET", "POST", "PUT", "DELETE"],
        "Enabled": true,
        "TimeoutSeconds": 30,
        "RequireAuthentication": true,
        "ApiVersion": "v2",
        "IsDefaultVersion": false,
        "RateLimit": {
          "PermitLimit": 100,
          "WindowSeconds": 60,
          "PartitionBy": "user"
        },
        "Cache": {
          "Enabled": true,
          "TtlSeconds": 300
        }
      },
      {
        "RouteId": "users-default",
        "UpstreamPathPattern": "/api/users/{**catch-all}",
        "DownstreamServiceUrl": "http://user-service",
        "DownstreamPathTemplate": "/api/v2/users/{**catch-all}",
        "HttpMethods": ["GET", "POST", "PUT", "DELETE"],
        "Enabled": true,
        "TimeoutSeconds": 30,
        "RequireAuthentication": true,
        "ApiVersion": "v2",
        "IsDefaultVersion": true,
        "RateLimit": {
          "PermitLimit": 100,
          "WindowSeconds": 60,
          "PartitionBy": "user"
        },
        "Cache": {
          "Enabled": true,
          "TtlSeconds": 300
        }
      }
    ]
  }
}
```

## References

- [Requirements Document - Requirement 21: API Versioning Support](../.kiro/specs/api-gateway-bff/requirements.md#requirement-21-api-versioning-support)
- [Design Document - Property 22: API version routing](../.kiro/specs/api-gateway-bff/design.md#property-22-api-version-routing)
- [Design Document - Property 23: Default version fallback](../.kiro/specs/api-gateway-bff/design.md#property-23-default-version-fallback)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
