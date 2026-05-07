# API Gateway Kubernetes Deployment Guide

## Overview

This guide provides comprehensive instructions for deploying the API Gateway/BFF to Kubernetes with production-ready configurations including high availability, auto-scaling, monitoring, and security.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                         Internet                             │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    Ingress Controller                        │
│                  (NGINX + TLS/SSL)                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   LoadBalancer Service                       │
│                  (Port 80 → 8080)                           │
└────────────────────────┬────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
    ┌────────┐      ┌────────┐      ┌────────┐
    │  Pod 1 │      │  Pod 2 │      │  Pod 3 │
    │        │      │        │      │        │
    │ 8080   │      │ 8080   │      │ 8080   │
    │ 8081   │      │ 8081   │      │ 8081   │
    └────────┘      └────────┘      └────────┘
         │               │               │
         └───────────────┼───────────────┘
                         │
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
    ┌─────────┐    ┌─────────┐    ┌─────────┐
    │  Redis  │    │  Auth   │    │Services │
    │  Cache  │    │ Service │    │(User,   │
    │         │    │         │    │Order)   │
    └─────────┘    └─────────┘    └─────────┘
```

## Prerequisites

### Required Tools

- **kubectl** (v1.24+): Kubernetes command-line tool
- **kustomize** (v4.0+): Template-free customization of Kubernetes manifests (optional, kubectl has built-in support)
- **Docker**: For building container images
- **helm** (optional): For installing dependencies like Redis, Prometheus

### Required Infrastructure

- **Kubernetes Cluster**: Version 1.24 or higher
- **Container Registry**: Docker Hub, AWS ECR, GCR, or Azure ACR
- **Redis**: For distributed caching (can be deployed in-cluster or external)
- **Authentication Service**: OAuth2/OIDC provider (e.g., Auth0, Keycloak, Azure AD)

### Optional Components

- **Ingress Controller**: NGINX, Traefik, or cloud provider ingress
- **Cert Manager**: For automatic TLS certificate management
- **Prometheus Operator**: For metrics collection and monitoring
- **Grafana**: For metrics visualization

## Quick Start

### 1. Clone and Navigate

```bash
cd ApiGateway/k8s
```

### 2. Create Secrets

```bash
# Copy template
cp secrets.yaml.template secrets.yaml

# Edit with your values
vim secrets.yaml

# Apply secrets
kubectl apply -f secrets.yaml
```

### 3. Update Configuration

Edit `configmap.yaml` with your environment-specific values:
- Authentication authority URL
- Allowed CORS origins
- Service endpoints

### 4. Build and Push Image

```bash
# Build image
docker build -t your-registry/api-gateway:v1.0.0 -f ../Dockerfile ../..

# Push to registry
docker push your-registry/api-gateway:v1.0.0
```

### 5. Deploy

**Development**:
```bash
./deploy.sh development apply
```

**Production**:
```bash
./deploy.sh production apply
```

## Detailed Configuration

### Resource Requirements

#### Development Environment

| Resource | Request | Limit |
|----------|---------|-------|
| CPU      | 100m    | 500m  |
| Memory   | 256Mi   | 512Mi |
| Replicas | 1       | 1     |

#### Production Environment

| Resource | Request | Limit |
|----------|---------|-------|
| CPU      | 500m    | 2000m |
| Memory   | 1Gi     | 2Gi   |
| Replicas | 5-20    | Auto  |

### Health Checks

#### Liveness Probe
- **Endpoint**: `/health/live`
- **Purpose**: Determines if pod should be restarted
- **Initial Delay**: 30 seconds
- **Period**: 10 seconds
- **Timeout**: 5 seconds
- **Failure Threshold**: 3 attempts

#### Readiness Probe
- **Endpoint**: `/health/ready`
- **Purpose**: Determines if pod should receive traffic
- **Initial Delay**: 10 seconds
- **Period**: 5 seconds
- **Timeout**: 3 seconds
- **Failure Threshold**: 3 attempts

### Auto-Scaling Configuration

#### Development
- **Min Replicas**: 1
- **Max Replicas**: 3
- **CPU Target**: 70%
- **Memory Target**: 80%

#### Production
- **Min Replicas**: 5
- **Max Replicas**: 20
- **CPU Target**: 60%
- **Memory Target**: 70%

**Scale-Up Behavior**:
- Aggressive: 100% increase or 4 pods every 30 seconds
- No stabilization window (immediate response)

**Scale-Down Behavior**:
- Conservative: 50% decrease or 2 pods every 60 seconds
- 5-minute stabilization window (prevents flapping)

### Graceful Shutdown

The deployment implements graceful shutdown to ensure zero-downtime deployments:

1. **preStop Hook**: 15-second sleep allows load balancer to remove pod
2. **Readiness Probe Fails**: Pod stops receiving new traffic
3. **In-Flight Requests**: Complete within termination grace period
4. **Termination Grace Period**: 30 seconds for cleanup
5. **Force Kill**: After grace period if still running

## Security

### Network Policies

The `networkpolicy.yaml` implements defense-in-depth:

**Ingress Rules**:
- Allow traffic from ingress controller on port 8080
- Allow traffic from Prometheus on port 8081 (metrics)

**Egress Rules**:
- Allow DNS resolution (port 53)
- Allow traffic to downstream services (ports 80, 443)
- Allow traffic to Redis (port 6379)
- Allow traffic to authentication service (port 443)

### Pod Security

**Security Context** (to be added):
```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 1000
  capabilities:
    drop:
    - ALL
  readOnlyRootFilesystem: true
```

### Secrets Management

**Current**: Kubernetes Secrets (base64 encoded)

**Recommended for Production**:
- **HashiCorp Vault**: External secrets management
- **AWS Secrets Manager**: For AWS deployments
- **Azure Key Vault**: For Azure deployments
- **Google Secret Manager**: For GCP deployments

Integration example with External Secrets Operator:
```yaml
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: api-gateway-secrets
spec:
  secretStoreRef:
    name: vault-backend
    kind: SecretStore
  target:
    name: api-gateway-secrets
  data:
  - secretKey: redis-connection-string
    remoteRef:
      key: api-gateway/redis
      property: connection-string
```

## Monitoring and Observability

### Metrics

**Prometheus Integration**:
```bash
# Apply ServiceMonitor
kubectl apply -f servicemonitor.yaml

# Verify metrics are being scraped
kubectl get servicemonitor api-gateway
```

**Key Metrics**:
- `gateway_requests_total`: Total request count by route and status
- `gateway_request_duration_seconds`: Request latency histogram
- `gateway_downstream_calls_total`: Downstream service call count
- `gateway_cache_hits_total`: Cache hit count
- `gateway_cache_misses_total`: Cache miss count
- `gateway_circuit_breaker_state`: Circuit breaker state (0=closed, 1=open)
- `gateway_rate_limit_rejections_total`: Rate limit rejection count

### Logging

**Log Aggregation**:
- Logs are written to stdout/stderr
- Kubernetes automatically collects logs
- Use log aggregation tools:
  - **ELK Stack**: Elasticsearch, Logstash, Kibana
  - **Loki**: Grafana Loki with Promtail
  - **Cloud Native**: CloudWatch, Stackdriver, Azure Monitor

**View Logs**:
```bash
# All pods
kubectl logs -l app=api-gateway --tail=100 -f

# Specific pod
kubectl logs api-gateway-<pod-id> -f

# Previous container (after crash)
kubectl logs api-gateway-<pod-id> --previous
```

### Distributed Tracing

**OpenTelemetry Integration**:
- Traces are exported to configured collector
- Configure collector endpoint via environment variable:
  ```yaml
  env:
  - name: OTEL_EXPORTER_OTLP_ENDPOINT
    value: "http://otel-collector:4317"
  ```

**Trace Visualization**:
- **Jaeger**: Open-source tracing platform
- **Zipkin**: Distributed tracing system
- **Cloud Native**: AWS X-Ray, Azure Application Insights, Google Cloud Trace

## Disaster Recovery

### Backup

**ConfigMaps and Secrets**:
```bash
# Backup
kubectl get configmap api-gateway-config -o yaml > backup/configmap.yaml
kubectl get secret api-gateway-secrets -o yaml > backup/secrets.yaml

# Restore
kubectl apply -f backup/configmap.yaml
kubectl apply -f backup/secrets.yaml
```

**Automated Backup**:
- Use Velero for cluster-wide backup and restore
- Schedule regular backups of namespace resources

### High Availability

**Multi-Zone Deployment**:
```yaml
spec:
  template:
    spec:
      affinity:
        podAntiAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
          - weight: 100
            podAffinityTerm:
              labelSelector:
                matchLabels:
                  app: api-gateway
              topologyKey: topology.kubernetes.io/zone
```

**Pod Disruption Budget**:
- Ensures minimum 2 replicas during voluntary disruptions
- Prevents complete service outage during maintenance

### Rollback

**Automatic Rollback**:
```bash
# Check rollout status
kubectl rollout status deployment/api-gateway

# View rollout history
kubectl rollout history deployment/api-gateway

# Rollback to previous version
kubectl rollout undo deployment/api-gateway

# Rollback to specific revision
kubectl rollout undo deployment/api-gateway --to-revision=2
```

## Troubleshooting

### Common Issues

#### Pods Not Starting

**Symptoms**: Pods stuck in `Pending`, `CrashLoopBackOff`, or `ImagePullBackOff`

**Diagnosis**:
```bash
kubectl describe pod api-gateway-<pod-id>
kubectl logs api-gateway-<pod-id>
```

**Common Causes**:
- Insufficient cluster resources
- Image pull errors (wrong registry, missing credentials)
- Configuration errors (missing secrets, invalid ConfigMap)
- Health check failures

#### Service Not Accessible

**Symptoms**: Cannot reach service from outside cluster

**Diagnosis**:
```bash
kubectl get svc api-gateway
kubectl get endpoints api-gateway
kubectl describe svc api-gateway
```

**Common Causes**:
- No healthy pods (check readiness probe)
- LoadBalancer not provisioned (cloud provider issue)
- Network policies blocking traffic
- Ingress misconfiguration

#### High Memory Usage

**Symptoms**: Pods being OOMKilled, high memory utilization

**Diagnosis**:
```bash
kubectl top pods -l app=api-gateway
kubectl describe pod api-gateway-<pod-id>
```

**Solutions**:
- Increase memory limits
- Investigate memory leaks in application
- Optimize cache settings
- Review connection pooling configuration

#### HPA Not Scaling

**Symptoms**: HPA not creating/removing pods despite load

**Diagnosis**:
```bash
kubectl describe hpa api-gateway-hpa
kubectl get --raw /apis/metrics.k8s.io/v1beta1/nodes
```

**Common Causes**:
- Metrics server not installed/running
- Insufficient permissions for HPA
- Resource requests not defined
- Metrics not available yet (wait 1-2 minutes)

### Debug Commands

```bash
# Get all resources
kubectl get all -l app=api-gateway

# Describe deployment
kubectl describe deployment api-gateway

# Get events
kubectl get events --sort-by='.lastTimestamp' | grep api-gateway

# Execute command in pod
kubectl exec -it api-gateway-<pod-id> -- /bin/sh

# Port forward for local testing
kubectl port-forward svc/api-gateway 8080:80

# Check resource usage
kubectl top pods -l app=api-gateway
kubectl top nodes
```

## Performance Tuning

### Connection Pooling

Configure HTTP client connection pooling:
```json
{
  "Gateway": {
    "HttpClient": {
      "MaxConnectionsPerServer": 100,
      "ConnectionLifetimeMinutes": 5,
      "PooledConnectionIdleTimeoutMinutes": 2
    }
  }
}
```

### Cache Optimization

Tune cache settings for your workload:
```json
{
  "CacheSettings": {
    "Memory": {
      "SizeLimit": 104857600,
      "CompactionPercentage": 0.25
    },
    "DefaultTtlSeconds": 300
  }
}
```

### Resource Limits

Adjust based on load testing results:
- Start with recommended values
- Monitor actual usage with `kubectl top`
- Increase limits if pods are throttled
- Decrease requests if over-provisioned

## Compliance and Governance

### Resource Quotas

Limit namespace resource usage:
```yaml
apiVersion: v1
kind: ResourceQuota
metadata:
  name: api-gateway-quota
spec:
  hard:
    requests.cpu: "10"
    requests.memory: 20Gi
    limits.cpu: "20"
    limits.memory: 40Gi
    pods: "50"
```

### Limit Ranges

Set default limits for pods:
```yaml
apiVersion: v1
kind: LimitRange
metadata:
  name: api-gateway-limits
spec:
  limits:
  - max:
      cpu: "2"
      memory: 2Gi
    min:
      cpu: 100m
      memory: 128Mi
    default:
      cpu: 500m
      memory: 512Mi
    defaultRequest:
      cpu: 250m
      memory: 256Mi
    type: Container
```

## Cost Optimization

### Right-Sizing

- Use VPA (Vertical Pod Autoscaler) to recommend resource limits
- Monitor actual usage vs. requested resources
- Adjust HPA thresholds to prevent over-scaling

### Spot/Preemptible Instances

Use node affinity for cost savings:
```yaml
spec:
  template:
    spec:
      affinity:
        nodeAffinity:
          preferredDuringSchedulingIgnoredDuringExecution:
          - weight: 1
            preference:
              matchExpressions:
              - key: node.kubernetes.io/instance-type
                operator: In
                values:
                - spot
                - preemptible
```

### Cluster Autoscaler

Enable cluster autoscaler to scale nodes based on pod requirements.

## Support and Maintenance

### Regular Maintenance Tasks

**Weekly**:
- Review pod restarts and errors
- Check resource utilization trends
- Review security scan results

**Monthly**:
- Update base images for security patches
- Review and optimize resource limits
- Audit access logs and security events

**Quarterly**:
- Kubernetes version upgrades
- Dependency updates
- Disaster recovery testing

### Upgrade Strategy

1. **Test in Development**: Deploy new version to dev environment
2. **Validate**: Run integration tests and smoke tests
3. **Canary Deployment**: Deploy to small percentage of production pods
4. **Monitor**: Watch metrics, logs, and error rates
5. **Full Rollout**: Gradually increase percentage
6. **Rollback Plan**: Keep previous version ready for quick rollback

## Additional Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Kustomize Documentation](https://kustomize.io/)
- [NGINX Ingress Controller](https://kubernetes.github.io/ingress-nginx/)
- [Prometheus Operator](https://prometheus-operator.dev/)
- [Cert Manager](https://cert-manager.io/)
- [Velero Backup](https://velero.io/)

## Requirements Validation

This deployment satisfies the following requirements from the API Gateway/BFF specification:

| Requirement | Description | Implementation |
|-------------|-------------|----------------|
| 26.1 | Graceful shutdown | preStop hook + termination grace period |
| 26.2 | Configuration via ConfigMaps/Secrets | configmap.yaml + secrets.yaml |
| 27.1 | Horizontal pod autoscaling | hpa.yaml with CPU/memory targets |
| 28.1 | Kubernetes ConfigMap support | configmap.yaml |
| 28.2 | Kubernetes Secrets support | secrets.yaml |
| 28.3 | Liveness probe | /health/live endpoint |
| 28.4 | Readiness probe | /health/ready endpoint |
| 28.5 | HPA support | hpa.yaml (3-10 replicas) |
| 28.6 | Rolling updates | Deployment strategy |
| 28.7 | Stdout logging | Serilog console sink |
