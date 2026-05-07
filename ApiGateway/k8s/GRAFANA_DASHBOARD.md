# API Gateway Grafana Dashboard

## Overview

This document describes the Grafana dashboard requirements for monitoring the API Gateway/BFF. The dashboard provides comprehensive visibility into gateway performance, health, and behavior.

**Validates: Requirements 19.4**

## Dashboard Structure

The dashboard is organized into the following sections:

1. **Overview** - High-level metrics and KPIs
2. **Request Metrics** - Request rate, latency, and throughput
3. **Error Tracking** - Error rates and status code distribution
4. **Cache Performance** - Cache hit rates and efficiency
5. **Downstream Services** - Service health and performance
6. **Resilience** - Circuit breaker states and rate limiting
7. **Resource Usage** - CPU, memory, and pod metrics

## Metrics Source

All metrics are exposed by the API Gateway at the `/metrics` endpoint in Prometheus format. The metrics are collected by Prometheus via the ServiceMonitor configuration.

### Available Metrics

From `ApiGateway/Observability/GatewayMetrics.cs`:

- `gateway_requests_total` - Counter of total requests (labels: route, method, status)
- `gateway_request_duration_seconds` - Histogram of request duration (labels: route, method, status)
- `gateway_downstream_calls_total` - Counter of downstream service calls (labels: service, method, status)
- `gateway_cache_hits_total` - Counter of cache hits (labels: route)
- `gateway_cache_misses_total` - Counter of cache misses (labels: route)
- `gateway_circuit_breaker_state` - Gauge of circuit breaker state (labels: service, values: 0=closed, 1=half-open, 2=open)
- `gateway_rate_limit_rejections_total` - Counter of rate limit rejections (labels: route, partition_by)

## Dashboard Panels

### 1. Overview Section

#### 1.1 Total Request Rate (Stat Panel)

**Description**: Current request rate across all routes

**Query**:
```promql
sum(rate(gateway_requests_total[5m]))
```

**Panel Configuration**:
- Visualization: Stat
- Unit: requests/sec (ops)
- Thresholds: 
  - Green: < 1000 req/s
  - Yellow: 1000-5000 req/s
  - Red: > 5000 req/s
- Decimals: 2

#### 1.2 Average Response Time (Stat Panel)

**Description**: Average response time across all requests

**Query**:
```promql
sum(rate(gateway_request_duration_seconds_sum[5m])) / sum(rate(gateway_request_duration_seconds_count[5m]))
```

**Panel Configuration**:
- Visualization: Stat
- Unit: seconds (s)
- Thresholds:
  - Green: < 0.5s
  - Yellow: 0.5-2s
  - Red: > 2s
- Decimals: 3

#### 1.3 Error Rate (Stat Panel)

**Description**: Percentage of requests returning 5xx errors

**Query**:
```promql
(sum(rate(gateway_requests_total{status=~"5.."}[5m])) / sum(rate(gateway_requests_total[5m]))) * 100
```

**Panel Configuration**:
- Visualization: Stat
- Unit: percent (0-100)
- Thresholds:
  - Green: < 1%
  - Yellow: 1-5%
  - Red: > 5%
- Decimals: 2

#### 1.4 Cache Hit Rate (Stat Panel)

**Description**: Percentage of requests served from cache

**Query**:
```promql
(sum(rate(gateway_cache_hits_total[5m])) / (sum(rate(gateway_cache_hits_total[5m])) + sum(rate(gateway_cache_misses_total[5m])))) * 100
```

**Panel Configuration**:
- Visualization: Stat
- Unit: percent (0-100)
- Thresholds:
  - Red: < 50%
  - Yellow: 50-80%
  - Green: > 80%
- Decimals: 2

### 2. Request Metrics Section

#### 2.1 Request Rate by Route (Time Series)

**Description**: Request rate over time, broken down by route

**Query**:
```promql
sum(rate(gateway_requests_total[5m])) by (route)
```

**Panel Configuration**:
- Visualization: Time series
- Unit: requests/sec (ops)
- Legend: Show as table with values (min, max, current)
- Stack: None
- Fill opacity: 10%

#### 2.2 Request Rate by Status Code (Time Series)

**Description**: Request rate grouped by HTTP status code

**Query**:
```promql
sum(rate(gateway_requests_total[5m])) by (status)
```

**Panel Configuration**:
- Visualization: Time series
- Unit: requests/sec (ops)
- Legend: Show as table
- Stack: Normal
- Color scheme:
  - 2xx: Green
  - 3xx: Blue
  - 4xx: Yellow
  - 5xx: Red

#### 2.3 Request Latency Percentiles (Time Series)

**Description**: Request latency at different percentiles (p50, p95, p99)

**Queries**:
```promql
# P50
histogram_quantile(0.50, sum(rate(gateway_request_duration_seconds_bucket[5m])) by (le))

# P95
histogram_quantile(0.95, sum(rate(gateway_request_duration_seconds_bucket[5m])) by (le))

# P99
histogram_quantile(0.99, sum(rate(gateway_request_duration_seconds_bucket[5m])) by (le))
```

**Panel Configuration**:
- Visualization: Time series
- Unit: seconds (s)
- Legend: Show p50, p95, p99
- Y-axis: Logarithmic scale (optional)

#### 2.4 Request Latency by Route (Time Series)

**Description**: P95 latency broken down by route

**Query**:
```promql
histogram_quantile(0.95, sum(rate(gateway_request_duration_seconds_bucket[5m])) by (le, route))
```

**Panel Configuration**:
- Visualization: Time series
- Unit: seconds (s)
- Legend: Show as table with current values
- Threshold line at 2s (warning level)

#### 2.5 Request Throughput (Time Series)

**Description**: Total number of requests over time

**Query**:
```promql
sum(increase(gateway_requests_total[1m]))
```

**Panel Configuration**:
- Visualization: Time series
- Unit: requests
- Legend: Hide
- Fill opacity: 20%

### 3. Error Tracking Section

#### 3.1 Error Rate Over Time (Time Series)

**Description**: Error rate (5xx responses) over time

**Query**:
```promql
(sum(rate(gateway_requests_total{status=~"5.."}[5m])) / sum(rate(gateway_requests_total[5m]))) * 100
```

**Panel Configuration**:
- Visualization: Time series
- Unit: percent (0-100)
- Thresholds:
  - Line at 5% (critical)
  - Line at 1% (warning)
- Color: Red

#### 3.2 Errors by Route (Bar Gauge)

**Description**: Error count by route in the last hour

**Query**:
```promql
sum(increase(gateway_requests_total{status=~"5.."}[1h])) by (route)
```

**Panel Configuration**:
- Visualization: Bar gauge
- Unit: requests
- Orientation: Horizontal
- Display mode: Gradient
- Color: Red gradient

#### 3.3 Status Code Distribution (Pie Chart)

**Description**: Distribution of HTTP status codes

**Query**:
```promql
sum(increase(gateway_requests_total[1h])) by (status)
```

**Panel Configuration**:
- Visualization: Pie chart
- Unit: requests
- Legend: Show values and percentages
- Color scheme: By status code family

#### 3.4 4xx Errors by Route (Time Series)

**Description**: Client errors (4xx) by route

**Query**:
```promql
sum(rate(gateway_requests_total{status=~"4.."}[5m])) by (route)
```

**Panel Configuration**:
- Visualization: Time series
- Unit: requests/sec (ops)
- Legend: Show as table
- Color: Yellow/Orange

### 4. Cache Performance Section

#### 4.1 Cache Hit Rate Over Time (Time Series)

**Description**: Cache hit rate percentage over time

**Query**:
```promql
(sum(rate(gateway_cache_hits_total[5m])) / (sum(rate(gateway_cache_hits_total[5m])) + sum(rate(gateway_cache_misses_total[5m])))) * 100
```

**Panel Configuration**:
- Visualization: Time series
- Unit: percent (0-100)
- Thresholds:
  - Line at 50% (minimum acceptable)
  - Line at 80% (target)
- Fill opacity: 20%

#### 4.2 Cache Hit Rate by Route (Bar Gauge)

**Description**: Cache hit rate for each route

**Query**:
```promql
(sum(rate(gateway_cache_hits_total[5m])) by (route) / (sum(rate(gateway_cache_hits_total[5m])) by (route) + sum(rate(gateway_cache_misses_total[5m])) by (route))) * 100
```

**Panel Configuration**:
- Visualization: Bar gauge
- Unit: percent (0-100)
- Orientation: Horizontal
- Thresholds:
  - Red: < 50%
  - Yellow: 50-80%
  - Green: > 80%

#### 4.3 Cache Operations (Time Series)

**Description**: Cache hits and misses over time

**Queries**:
```promql
# Cache Hits
sum(rate(gateway_cache_hits_total[5m]))

# Cache Misses
sum(rate(gateway_cache_misses_total[5m]))
```

**Panel Configuration**:
- Visualization: Time series
- Unit: operations/sec (ops)
- Legend: Show hits and misses
- Stack: None
- Colors:
  - Hits: Green
  - Misses: Orange

#### 4.4 Cache Efficiency by Route (Table)

**Description**: Detailed cache statistics per route

**Queries**:
```promql
# Hit Rate
(sum(rate(gateway_cache_hits_total[5m])) by (route) / (sum(rate(gateway_cache_hits_total[5m])) by (route) + sum(rate(gateway_cache_misses_total[5m])) by (route))) * 100

# Total Operations
sum(rate(gateway_cache_hits_total[5m])) by (route) + sum(rate(gateway_cache_misses_total[5m])) by (route)

# Hits
sum(rate(gateway_cache_hits_total[5m])) by (route)

# Misses
sum(rate(gateway_cache_misses_total[5m])) by (route)
```

**Panel Configuration**:
- Visualization: Table
- Columns: Route, Hit Rate (%), Total Ops/s, Hits/s, Misses/s
- Sort by: Hit Rate descending
- Cell display mode: Color background for hit rate

### 5. Downstream Services Section

#### 5.1 Downstream Call Rate (Time Series)

**Description**: Rate of calls to downstream services

**Query**:
```promql
sum(rate(gateway_downstream_calls_total[5m])) by (service)
```

**Panel Configuration**:
- Visualization: Time series
- Unit: requests/sec (ops)
- Legend: Show as table with current values
- Stack: None

#### 5.2 Downstream Call Success Rate (Time Series)

**Description**: Success rate (2xx responses) for downstream calls

**Query**:
```promql
(sum(rate(gateway_downstream_calls_total{status=~"2.."}[5m])) by (service) / sum(rate(gateway_downstream_calls_total[5m])) by (service)) * 100
```

**Panel Configuration**:
- Visualization: Time series
- Unit: percent (0-100)
- Legend: Show as table
- Thresholds:
  - Line at 95% (minimum acceptable)
  - Line at 99% (target)

#### 5.3 Downstream Errors by Service (Bar Gauge)

**Description**: Error count by downstream service

**Query**:
```promql
sum(increase(gateway_downstream_calls_total{status=~"5.."}[1h])) by (service)
```

**Panel Configuration**:
- Visualization: Bar gauge
- Unit: requests
- Orientation: Horizontal
- Color: Red gradient

#### 5.4 Downstream Status Codes (Table)

**Description**: Status code distribution per downstream service

**Queries**:
```promql
# Total Calls
sum(rate(gateway_downstream_calls_total[5m])) by (service)

# 2xx
sum(rate(gateway_downstream_calls_total{status=~"2.."}[5m])) by (service)

# 4xx
sum(rate(gateway_downstream_calls_total{status=~"4.."}[5m])) by (service)

# 5xx
sum(rate(gateway_downstream_calls_total{status=~"5.."}[5m])) by (service)
```

**Panel Configuration**:
- Visualization: Table
- Columns: Service, Total/s, 2xx/s, 4xx/s, 5xx/s
- Cell display mode: Color text (green for 2xx, yellow for 4xx, red for 5xx)

### 6. Resilience Section

#### 6.1 Circuit Breaker States (Stat Panel)

**Description**: Current circuit breaker states for all services

**Query**:
```promql
gateway_circuit_breaker_state
```

**Panel Configuration**:
- Visualization: Stat
- Unit: None
- Value mappings:
  - 0 → "Closed" (Green)
  - 1 → "Half-Open" (Yellow)
  - 2 → "Open" (Red)
- Repeat: By service label

#### 6.2 Circuit Breaker State History (Time Series)

**Description**: Circuit breaker state changes over time

**Query**:
```promql
gateway_circuit_breaker_state
```

**Panel Configuration**:
- Visualization: Time series
- Unit: None
- Legend: Show by service
- Y-axis: 0-2 (Closed, Half-Open, Open)
- Step line: After

#### 6.3 Rate Limit Rejections (Time Series)

**Description**: Rate of requests rejected due to rate limiting

**Query**:
```promql
sum(rate(gateway_rate_limit_rejections_total[5m])) by (route)
```

**Panel Configuration**:
- Visualization: Time series
- Unit: requests/sec (ops)
- Legend: Show as table
- Color: Orange/Red

#### 6.4 Rate Limit Rejection Rate (Time Series)

**Description**: Percentage of requests rejected by rate limiter

**Query**:
```promql
(sum(rate(gateway_rate_limit_rejections_total[5m])) by (route) / sum(rate(gateway_requests_total[5m])) by (route)) * 100
```

**Panel Configuration**:
- Visualization: Time series
- Unit: percent (0-100)
- Legend: Show as table
- Threshold line at 10% (warning)

#### 6.5 Rate Limit Rejections by Partition (Pie Chart)

**Description**: Distribution of rate limit rejections by partition type

**Query**:
```promql
sum(increase(gateway_rate_limit_rejections_total[1h])) by (partition_by)
```

**Panel Configuration**:
- Visualization: Pie chart
- Unit: requests
- Legend: Show values and percentages

### 7. Resource Usage Section

#### 7.1 Pod CPU Usage (Time Series)

**Description**: CPU usage per pod

**Query**:
```promql
rate(container_cpu_usage_seconds_total{pod=~"api-gateway-.*"}[5m])
```

**Panel Configuration**:
- Visualization: Time series
- Unit: cores
- Legend: Show by pod
- Threshold line at CPU limit

#### 7.2 Pod Memory Usage (Time Series)

**Description**: Memory usage per pod

**Query**:
```promql
container_memory_working_set_bytes{pod=~"api-gateway-.*"} / 1024 / 1024
```

**Panel Configuration**:
- Visualization: Time series
- Unit: MiB
- Legend: Show by pod
- Threshold line at memory limit

#### 7.3 Pod Count (Stat Panel)

**Description**: Current number of running pods

**Query**:
```promql
count(kube_pod_status_phase{pod=~"api-gateway-.*", phase="Running"})
```

**Panel Configuration**:
- Visualization: Stat
- Unit: pods
- Thresholds:
  - Red: < 2 (below minimum)
  - Yellow: 2-3
  - Green: >= 3

#### 7.4 HPA Status (Time Series)

**Description**: Current and desired replica count

**Queries**:
```promql
# Current Replicas
kube_horizontalpodautoscaler_status_current_replicas{horizontalpodautoscaler="api-gateway-hpa"}

# Desired Replicas
kube_horizontalpodautoscaler_status_desired_replicas{horizontalpodautoscaler="api-gateway-hpa"}
```

**Panel Configuration**:
- Visualization: Time series
- Unit: replicas
- Legend: Show current and desired
- Step line: After

## Dashboard Variables

Configure the following template variables for dynamic filtering:

### 1. Namespace Variable

**Name**: `namespace`
**Type**: Query
**Query**: `label_values(gateway_requests_total, namespace)`
**Multi-value**: No
**Include All**: No

### 2. Route Variable

**Name**: `route`
**Type**: Query
**Query**: `label_values(gateway_requests_total{namespace="$namespace"}, route)`
**Multi-value**: Yes
**Include All**: Yes

### 3. Service Variable

**Name**: `service`
**Type**: Query
**Query**: `label_values(gateway_downstream_calls_total{namespace="$namespace"}, service)`
**Multi-value**: Yes
**Include All**: Yes

### 4. Time Range Variable

**Name**: `interval`
**Type**: Interval
**Values**: `1m,5m,10m,30m,1h`
**Auto**: Yes

## Dashboard Settings

### General Settings

- **Name**: API Gateway - Performance & Health
- **Tags**: api-gateway, bff, performance, monitoring
- **Timezone**: Browser time
- **Refresh**: 30s
- **Time range**: Last 1 hour (default)

### Row Organization

1. **Overview Row** (collapsed by default: No)
   - Panels: 1.1, 1.2, 1.3, 1.4

2. **Request Metrics Row** (collapsed by default: No)
   - Panels: 2.1, 2.2, 2.3, 2.4, 2.5

3. **Error Tracking Row** (collapsed by default: Yes)
   - Panels: 3.1, 3.2, 3.3, 3.4

4. **Cache Performance Row** (collapsed by default: Yes)
   - Panels: 4.1, 4.2, 4.3, 4.4

5. **Downstream Services Row** (collapsed by default: Yes)
   - Panels: 5.1, 5.2, 5.3, 5.4

6. **Resilience Row** (collapsed by default: Yes)
   - Panels: 6.1, 6.2, 6.3, 6.4, 6.5

7. **Resource Usage Row** (collapsed by default: Yes)
   - Panels: 7.1, 7.2, 7.3, 7.4

## Alert Integration

The dashboard should link to the Prometheus alerts defined in `prometheus-rules.yaml`:

- **ApiGatewayHighErrorRate**: Link from Error Rate panel (1.3)
- **ApiGatewayCircuitBreakerOpen**: Link from Circuit Breaker States panel (6.1)
- **ApiGatewayHighLatency**: Link from Request Latency panel (2.3)
- **ApiGatewayHighRateLimitRejections**: Link from Rate Limit Rejections panel (6.3)
- **ApiGatewayLowCacheHitRate**: Link from Cache Hit Rate panel (1.4)

## Dashboard Export

The dashboard can be exported as JSON and stored in version control for GitOps workflows. Use the following naming convention:

**File**: `api-gateway-dashboard.json`
**Location**: `ApiGateway/k8s/grafana/`

## Usage Guidelines

### For Developers

- Monitor request latency and error rates during deployments
- Investigate cache efficiency for optimization opportunities
- Track downstream service health and performance
- Identify routes with high error rates or latency

### For DevOps/SRE

- Monitor resource usage and HPA behavior
- Track circuit breaker states and rate limiting
- Investigate alerts and anomalies
- Capacity planning based on traffic patterns

### For Product/Business

- Track overall request volume and trends
- Monitor API availability and performance SLAs
- Identify usage patterns by route
- Measure cache effectiveness for cost optimization

## Troubleshooting

### No Data Displayed

1. Verify Prometheus is scraping the `/metrics` endpoint
2. Check ServiceMonitor configuration in `servicemonitor.yaml`
3. Verify pods are running: `kubectl get pods -l app=api-gateway`
4. Check metrics endpoint: `kubectl port-forward svc/api-gateway 8081:8081` then visit `http://localhost:8081/metrics`

### Incorrect Metrics

1. Verify metric names match those in `GatewayMetrics.cs`
2. Check label names and values in queries
3. Verify time range and interval settings
4. Check for metric cardinality issues

### Performance Issues

1. Reduce query interval for high-cardinality metrics
2. Use recording rules for complex queries
3. Limit time range for expensive queries
4. Consider using Grafana query caching

## Related Documentation

- [Prometheus Alerting Rules](./prometheus-rules.yaml)
- [ServiceMonitor Configuration](./servicemonitor.yaml)
- [Deployment Guide](./DEPLOYMENT_GUIDE.md)
- [Gateway Metrics Implementation](../Observability/GatewayMetrics.cs)
- [Requirements Document](../../.kiro/specs/api-gateway-bff/requirements.md) - Requirement 19.4

## Maintenance

### Regular Reviews

- **Weekly**: Review dashboard for missing or redundant panels
- **Monthly**: Optimize slow queries and update thresholds
- **Quarterly**: Align with new metrics and requirements

### Version History

- **v1.0**: Initial dashboard with core metrics (request rate, latency, errors, cache)
- Future versions will add:
  - WebSocket connection metrics
  - BFF-specific panels per client type
  - Cost analysis panels
  - SLA compliance tracking
