# API Gateway Kubernetes Deployment

This directory contains Kubernetes manifests for deploying the API Gateway/BFF to a Kubernetes cluster.

## Files

### Base Manifests

- **deployment.yaml**: Main deployment configuration with 3 replicas, health probes, resource limits, and graceful shutdown
- **service.yaml**: LoadBalancer service exposing HTTP (port 80) and metrics (port 9090)
- **configmap.yaml**: Configuration data including auth settings and appsettings.Production.json
- **hpa.yaml**: HorizontalPodAutoscaler for auto-scaling between 3-10 replicas based on CPU/memory
- **secrets.yaml.template**: Template for creating secrets (copy to secrets.yaml and update values)
- **pdb.yaml**: PodDisruptionBudget ensuring minimum 2 replicas during disruptions
- **ingress.yaml**: Ingress resource for external access with TLS
- **servicemonitor.yaml**: ServiceMonitor for Prometheus metrics scraping
- **networkpolicy.yaml**: NetworkPolicy for pod-to-pod communication security
- **kustomization.yaml**: Kustomize configuration for managing manifests

### Overlays

- **overlays/development/**: Development environment configuration (1 replica, reduced resources)
- **overlays/production/**: Production environment configuration (5 replicas, increased resources)

### Scripts

- **deploy.sh**: Automated deployment script for different environments

## Prerequisites

- Kubernetes cluster (1.24+)
- kubectl configured to access your cluster
- Container registry with the api-gateway image
- Redis instance for distributed caching

## Deployment Steps

### Quick Start with Deployment Script

The easiest way to deploy is using the provided script:

```bash
# Deploy to development
./deploy.sh development apply

# Deploy to production
./deploy.sh production apply

# Show diff before applying
./deploy.sh production diff

# Dry run
./deploy.sh production dry-run

# Delete resources
./deploy.sh production delete
```

### Manual Deployment Steps

### 1. Create Secrets

Copy the secrets template and update with your actual values:

```bash
cp secrets.yaml.template secrets.yaml
# Edit secrets.yaml with your actual secret values
kubectl apply -f secrets.yaml
```

**Important**: Never commit `secrets.yaml` to version control. It's included in `.gitignore`.

### 2. Update ConfigMap

Edit `configmap.yaml` to update the following values:
- `auth-authority`: Your authentication authority URL
- `auth-audience`: Your API audience identifier
- `allowed-origin-1`, `allowed-origin-2`: CORS allowed origins

### 3. Build and Push Container Image

```bash
# Build the Docker image
docker build -t your-registry/api-gateway:latest -f ApiGateway/Dockerfile .

# Push to your container registry
docker push your-registry/api-gateway:latest
```

### 4. Update Deployment Image

Edit `deployment.yaml` and update the image reference:

```yaml
image: your-registry/api-gateway:latest
```

### 5. Deploy to Kubernetes

**Using Kustomize (Recommended)**:

```bash
# Deploy to development
kubectl apply -k overlays/development

# Deploy to production
kubectl apply -k overlays/production
```

**Using kubectl directly**:

```bash
kubectl apply -f configmap.yaml
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f hpa.yaml
kubectl apply -f pdb.yaml
```

**Optional resources**:

```bash
# Ingress for external access
kubectl apply -f ingress.yaml

# ServiceMonitor for Prometheus
kubectl apply -f servicemonitor.yaml

# NetworkPolicy for security
kubectl apply -f networkpolicy.yaml
```

### 6. Verify Deployment

Check pod status:

```bash
kubectl get pods -l app=api-gateway
```

Check service status:

```bash
kubectl get svc api-gateway
```

Check HPA status:

```bash
kubectl get hpa api-gateway-hpa
```

View logs:

```bash
kubectl logs -l app=api-gateway --tail=100 -f
```

## Configuration

### Environment Variables

The deployment uses environment variables from ConfigMap and Secrets:

| Variable | Source | Description |
|----------|--------|-------------|
| `AUTH_AUTHORITY` | ConfigMap | Authentication authority URL |
| `AUTH_AUDIENCE` | ConfigMap | API audience identifier |
| `REDIS_CONNECTION_STRING` | Secret | Redis connection string |
| `ALLOWED_ORIGIN_1` | ConfigMap | First CORS allowed origin |
| `ALLOWED_ORIGIN_2` | ConfigMap | Second CORS allowed origin |

### Resource Limits

**Requests** (guaranteed resources):
- CPU: 250m (0.25 cores)
- Memory: 512Mi

**Limits** (maximum resources):
- CPU: 1000m (1 core)
- Memory: 1Gi

Adjust these values based on your workload requirements.

### Health Checks

**Liveness Probe** (`/health/live`):
- Initial delay: 30 seconds
- Period: 10 seconds
- Timeout: 5 seconds
- Failure threshold: 3

**Readiness Probe** (`/health/ready`):
- Initial delay: 10 seconds
- Period: 5 seconds
- Timeout: 3 seconds
- Failure threshold: 3

### Auto-Scaling

The HorizontalPodAutoscaler (HPA) automatically scales the deployment:

**Scaling Triggers**:
- CPU utilization > 70%
- Memory utilization > 80%

**Replica Range**:
- Minimum: 3 replicas (high availability)
- Maximum: 10 replicas

**Scale-Up Behavior**:
- Fast scale-up: 100% increase or 4 pods every 30 seconds
- No stabilization window (immediate response to load)

**Scale-Down Behavior**:
- Conservative scale-down: 50% decrease or 2 pods every 60 seconds
- 5-minute stabilization window to prevent flapping

### Graceful Shutdown

The deployment is configured for graceful shutdown:

1. **preStop Hook**: 15-second sleep to allow load balancer to remove pod from rotation
2. **Termination Grace Period**: 30 seconds for in-flight requests to complete
3. **Health Check Update**: Readiness probe fails immediately, removing pod from service

This ensures zero-downtime deployments and rolling updates.

## Monitoring

### Metrics Endpoint

Prometheus metrics are exposed on port 9090 (mapped from container port 8081):

```bash
# Port-forward to access metrics locally
kubectl port-forward svc/api-gateway 9090:9090

# Access metrics
curl http://localhost:9090/metrics
```

### Health Endpoints

Health check endpoints are available:

```bash
# Liveness probe
curl http://<service-ip>/health/live

# Readiness probe
curl http://<service-ip>/health/ready

# Downstream services health
curl http://<service-ip>/health/downstream
```

## Troubleshooting

### Pods Not Starting

Check pod events:

```bash
kubectl describe pod -l app=api-gateway
```

Check logs:

```bash
kubectl logs -l app=api-gateway --tail=100
```

### Configuration Issues

Verify ConfigMap:

```bash
kubectl get configmap api-gateway-config -o yaml
```

Verify Secrets:

```bash
kubectl get secret api-gateway-secrets -o yaml
```

### Service Not Accessible

Check service endpoints:

```bash
kubectl get endpoints api-gateway
```

Check service details:

```bash
kubectl describe svc api-gateway
```

### HPA Not Scaling

Check HPA status:

```bash
kubectl describe hpa api-gateway-hpa
```

Verify metrics server is running:

```bash
kubectl get deployment metrics-server -n kube-system
```

## Rolling Updates

To update the deployment:

```bash
# Update the image
kubectl set image deployment/api-gateway api-gateway=your-registry/api-gateway:v2

# Or apply updated deployment.yaml
kubectl apply -f deployment.yaml

# Watch rollout status
kubectl rollout status deployment/api-gateway

# Rollback if needed
kubectl rollout undo deployment/api-gateway
```

## Cleanup

To remove all resources:

```bash
kubectl delete -f hpa.yaml
kubectl delete -f service.yaml
kubectl delete -f deployment.yaml
kubectl delete -f configmap.yaml
kubectl delete -f secrets.yaml
```

## Production Considerations

### Ingress and TLS

The `ingress.yaml` file configures external access with TLS:

1. **Update the host**: Change `api.example.com` to your domain
2. **Configure TLS certificate**: 
   - Using cert-manager: The annotation `cert-manager.io/cluster-issuer` will automatically provision certificates
   - Manual: Create a TLS secret with your certificate:
     ```bash
     kubectl create secret tls api-gateway-tls \
       --cert=path/to/cert.crt \
       --key=path/to/cert.key
     ```

### Network Security

The `networkpolicy.yaml` restricts network traffic:

- **Ingress**: Only allows traffic from ingress controller and Prometheus
- **Egress**: Only allows traffic to DNS, downstream services, Redis, and external HTTPS

Update the policy to match your cluster's namespace labels and service requirements.

### Pod Disruption Budget

The `pdb.yaml` ensures high availability during:
- Node maintenance
- Cluster upgrades
- Voluntary disruptions

Minimum 2 replicas will always be available, preventing complete service outage.

### Monitoring with Prometheus

The `servicemonitor.yaml` enables automatic metrics scraping:

1. Requires Prometheus Operator installed in your cluster
2. Metrics are scraped from `/metrics` endpoint every 30 seconds
3. Update `namespaceSelector` to match your namespace

### Environment-Specific Overlays

Use Kustomize overlays for different environments:

**Development** (`overlays/development`):
- 1 replica
- Reduced resources (100m CPU, 256Mi memory)
- Development auth authority
- Local CORS origins

**Production** (`overlays/production`):
- 5 replicas (can scale to 20)
- Increased resources (500m CPU, 1Gi memory)
- Production auth authority
- Production CORS origins
- Includes PodDisruptionBudget

Create additional overlays for staging or other environments as needed.

### Additional Production Considerations

1. **Image Registry**: Use a private container registry with image scanning
2. **Secrets Management**: Consider using external secrets management (e.g., HashiCorp Vault, AWS Secrets Manager)
3. **Network Policies**: Implement network policies to restrict pod-to-pod communication
4. **Resource Quotas**: Set namespace resource quotas to prevent resource exhaustion
5. **Pod Disruption Budgets**: Create PDB to ensure minimum availability during maintenance
6. **Monitoring**: Integrate with Prometheus and Grafana for comprehensive monitoring
7. **Logging**: Configure log aggregation (e.g., ELK stack, Loki)
8. **TLS/SSL**: Configure ingress with TLS certificates for HTTPS
9. **Service Mesh**: Consider using a service mesh (e.g., Istio, Linkerd) for advanced traffic management
10. **Backup**: Regularly backup ConfigMaps and Secrets

## Requirements Validation

This Kubernetes deployment satisfies the following requirements:

- **Requirement 26.1**: Graceful shutdown with preStop hook and termination grace period
- **Requirement 26.2**: Configuration via Kubernetes ConfigMaps and Secrets
- **Requirement 27.1**: Horizontal pod autoscaling based on CPU and memory metrics
- **Requirement 28.1**: Configuration via Kubernetes ConfigMaps
- **Requirement 28.2**: Secrets via Kubernetes Secrets
- **Requirement 28.3**: Liveness probe at /health/live
- **Requirement 28.4**: Readiness probe at /health/ready
- **Requirement 28.5**: Horizontal pod autoscaling support
- **Requirement 28.6**: Rolling updates without downtime
- **Requirement 28.7**: Logs to stdout for Kubernetes log aggregation
