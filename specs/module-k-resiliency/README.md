# Module K – Resiliency & High Availability

## Objective

Module K defines failure modes, mitigation strategies, and recovery procedures. The EHR platform must remain available during node failures, deployment rollouts, and upstream dependency outages. Every failure scenario has a documented detection, response, and recovery path.

---

## Availability Targets

| Metric | POC Target | Production Target |
|---|---|---|
| API Uptime | 99% | 99.9% |
| Data Durability | 99.9% | 99.999% |
| RTO (Recovery Time) | 30 min | 5 min |
| RPO (Recovery Point) | 1 hour | 5 min |
| Max Concurrent Users | 50 | 500 |
| P95 Latency | < 2s | < 500ms |

---

## Pod Disruption Budgets

```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: ehr-api-pdb
  namespace: ehr
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app: ehr-api
---
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: ehr-frontend-pdb
  namespace: ehr
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app: ehr-frontend
```

**Rule**: Every deployment with 2+ replicas must have a PDB. `minAvailable: 1` ensures at least one pod serves traffic during voluntary disruptions (node drains, upgrades).

---

## Horizontal Pod Autoscaler

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: ehr-api-hpa
  namespace: ehr
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: ehr-api
  minReplicas: 2
  maxReplicas: 8
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 80
  behavior:
    scaleUp:
      stabilizationWindowSeconds: 60
      policies:
        - type: Pods
          value: 2
          periodSeconds: 60
    scaleDown:
      stabilizationWindowSeconds: 300
      policies:
        - type: Pods
          value: 1
          periodSeconds: 120
```

---

## Health Probes

| Service | Liveness | Readiness | Startup |
|---|---|---|---|
| ehr-api | GET /health/live (10s interval) | GET /health/ready (5s interval) | GET /health/live (5s, 30 failures) |
| ehr-frontend | GET / (15s interval) | GET / (5s interval) | — |
| PostgreSQL | TCP 5432 | TCP 5432 | — |

### Probe Configuration

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
  failureThreshold: 3
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 3
startupProbe:
  httpGet:
    path: /health/live
    port: 8080
  periodSeconds: 5
  failureThreshold: 30
```

---

## Failure Scenarios & Recovery

### Scenario 1: Single Node Failure

| Aspect | Detail |
|---|---|
| Detection | AKS node health check (kubelet heartbeat) |
| Impact | Pods on failed node rescheduled |
| Mitigation | 3-node cluster, PDB ensures min availability |
| Recovery | Automatic — AKS reschedules pods within 5 min |
| Data Loss | None — stateless API pods, state in PostgreSQL |

### Scenario 2: Database Connection Failure

| Aspect | Detail |
|---|---|
| Detection | Health check /health/ready fails (EF Core ping) |
| Impact | API returns 503, readiness probe removes pod from service |
| Mitigation | Connection resilience with retry (Npgsql EnableRetryOnFailure) |
| Recovery | Automatic when database recovers, pods re-enter service |
| Data Loss | None — uncommitted transactions roll back |

### Scenario 3: Bad Deployment (Application Crash)

| Aspect | Detail |
|---|---|
| Detection | Startup probe fails, CrashLoopBackOff |
| Impact | New pods fail, old pods continue serving (RollingUpdate) |
| Mitigation | `maxUnavailable: 0` ensures old pods stay until new ones ready |
| Recovery | `kubectl rollout undo deployment/ehr-api -n ehr` |
| Data Loss | None |

### Scenario 4: ACR Unavailable

| Aspect | Detail |
|---|---|
| Detection | ImagePullBackOff errors |
| Impact | Cannot scale up or restart pods |
| Mitigation | `imagePullPolicy: IfNotPresent` — cached images work |
| Recovery | Automatic when ACR recovers |
| Data Loss | None |

### Scenario 5: Certificate Expiry

| Aspect | Detail |
|---|---|
| Detection | cert-manager alerts (Module H), browser warnings |
| Impact | HTTPS connections fail for ingress |
| Mitigation | cert-manager auto-renewal 30 days before expiry |
| Recovery | Manual cert rotation if auto-renewal fails |
| Data Loss | None |

### Scenario 6: Full Cluster Failure

| Aspect | Detail |
|---|---|
| Detection | Azure Monitor alerts, all endpoints unreachable |
| Impact | Total service outage |
| Mitigation | Azure Web Apps serve as secondary access path |
| Recovery | Re-create AKS cluster from IaC, redeploy from ACR, reconnect PostgreSQL |
| Data Loss | None — PostgreSQL is external to AKS |

---

## Retry Policies

### EF Core Database Retry

```csharp
options.UseNpgsql(connStr, npgsqlOptions => {
    npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorCodesToAdd: null
    );
});
```

### HTTP Client Retry (Polly)

```csharp
builder.Services.AddHttpClient("downstream")
    .AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt))))
    .AddTransientHttpErrorPolicy(p =>
        p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

| Policy | Retry Count | Backoff | Circuit Break |
|---|---|---|---|
| Database | 5 | Up to 30s (exponential) | — |
| HTTP Downstream | 3 | 2s, 4s, 8s (exponential) | 5 failures → 30s open |
| Redis Cache | 2 | 1s, 2s | — |
| Event Publish | 3 | 1s, 2s, 4s | 10 failures → 60s open |

---

## Deployment Strategy

### Rolling Update (Current)

```yaml
strategy:
  type: RollingUpdate
  rollingUpdate:
    maxUnavailable: 0
    maxSurge: 1
```

- Zero-downtime: old pods serve traffic until new pods pass readiness
- Rollback: `kubectl rollout undo` restores previous ReplicaSet

### Blue-Green (Target State)

- Two full deployments (blue/green) behind ingress
- Switch traffic by updating ingress backend service
- Instant rollback by switching back
- Detailed in Module N (AKS Change Management)

---

## Dependencies

- **Upstream**: Module B (AKS cluster config), Module D (database resilience settings), Module H (monitoring detects failures)
- **Downstream**: Module L (resiliency scenarios are exit criteria), Module N (upgrade resiliency)
