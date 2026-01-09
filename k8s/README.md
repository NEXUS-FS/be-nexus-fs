# NexusFS Kubernetes Deployment

This directory contains Kubernetes manifests for deploying NexusFS to a Kubernetes cluster.

## Prerequisites

- Kubernetes cluster (1.25+)
- kubectl configured
- Docker image built and pushed to registry
- Ingress controller (nginx recommended)
- Cert-manager (for TLS certificates)

## Deployment Steps

### 1. Create Namespace

```bash
kubectl apply -f namespace.yaml
```

### 2. Configure Secrets

**Important:** Edit `secret.yaml` and replace placeholder values with actual credentials:

- `DATABASE_URL`: PostgreSQL connection string
- `JWT_SECRET_KEY`: Strong secret key (minimum 32 characters)
- `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY`: If using S3
- `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`: If using Google Drive

```bash
kubectl apply -f secret.yaml
```

### 3. Apply ConfigMap

```bash
kubectl apply -f configmap.yaml
```

### 4. Deploy PostgreSQL

```bash
kubectl apply -f postgres-deployment.yaml
```

Wait for PostgreSQL to be ready:

```bash
kubectl wait --for=condition=ready pod -l app=postgres -n nexusfs --timeout=300s
```

### 5. Deploy Redis

```bash
kubectl apply -f redis-deployment.yaml
```

### 6. Deploy NexusFS API

```bash
kubectl apply -f nexusfs-deployment.yaml
```

### 7. Configure Ingress (Optional)

Edit `ingress.yaml` and replace `api.nexusfs.example.com` with your actual domain:

```bash
kubectl apply -f ingress.yaml
```

### 8. Enable Horizontal Pod Autoscaling (Optional)

```bash
kubectl apply -f hpa.yaml
```

## Verification

Check deployment status:

```bash
kubectl get all -n nexusfs
```

Check pod logs:

```bash
kubectl logs -f deployment/nexusfs-api -n nexusfs
```

Test health endpoints:

```bash
kubectl port-forward -n nexusfs service/nexusfs-api-service 8080:80
curl http://localhost:8080/health
```

## Monitoring

Access Prometheus metrics:

```bash
kubectl port-forward -n nexusfs service/nexusfs-api-service 8080:80
curl http://localhost:8080/metrics
```

## Scaling

Manual scaling:

```bash
kubectl scale deployment nexusfs-api --replicas=5 -n nexusfs
```

With HPA enabled, scaling happens automatically based on CPU/memory usage.

## Updating

Update the deployment with a new image:

```bash
kubectl set image deployment/nexusfs-api nexusfs-api=nexusfs/api:v1.1.0 -n nexusfs
```

## Cleanup

Remove all resources:

```bash
kubectl delete namespace nexusfs
```

## Production Considerations

1. **Secrets Management**: Use external secrets management (e.g., HashiCorp Vault, AWS Secrets Manager)
2. **Database**: Consider managed PostgreSQL (RDS, Cloud SQL, Azure Database)
3. **Redis**: Consider managed Redis (ElastiCache, Cloud Memorystore)
4. **Monitoring**: Set up Prometheus + Grafana for metrics
5. **Logging**: Configure centralized logging (ELK, Loki, CloudWatch)
6. **Backup**: Implement database backup strategy
7. **Resource Limits**: Adjust resource requests/limits based on actual usage
8. **Network Policies**: Implement network policies for security
9. **Pod Security**: Use Pod Security Standards/Policies
10. **TLS**: Ensure all traffic is encrypted (use cert-manager)

