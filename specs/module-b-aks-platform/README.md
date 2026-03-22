# Module B – AKS Platform & Container Orchestration

## Objective

Module B defines the Azure Kubernetes Service cluster architecture, node pool design, networking configuration, ingress routing, certificate management, and autoscaling policies. The AKS cluster is the execution platform for all containerized EHR microservices and supporting infrastructure components.

---

## Cluster Specification

| Property | Value |
|---|---|
| **Cluster Name** | `aks-store-demo` |
| **Region** | Canada Central |
| **Kubernetes Version** | 1.33 |
| **Network Plugin** | Azure CNI |
| **Network Policy** | Calico (target) |
| **DNS Service IP** | Cluster-assigned |
| **Max Pods per Node** | 250 |
| **RBAC** | Enabled |
| **Managed Identity** | System-assigned |

---

## Node Pool Architecture

### Current State (POC)

A single system node pool handles all workloads — Kubernetes system components, application pods, and ingress infrastructure.

| Pool | VM Size | Count | Min | Max | Mode | Purpose |
|---|---|---|---|---|---|---|
| `nodepool1` | Standard_DS2_v2 | 3 | 3 | 3 | System | All workloads |

**Per-Node Capacity**: 2 vCPU, 7 GB RAM, 250 max pods.
**Total Cluster Capacity**: 6 vCPU, 21 GB RAM, 750 max pods.

### Target State (Production)

Three specialized node pools with independent autoscaling:

| Pool | VM Size | Min | Max | Mode | Taints | Purpose |
|---|---|---|---|---|---|---|
| `system` | Standard_DS2_v2 | 1 | 2 | System | `CriticalAddonsOnly=true:NoSchedule` | K8s system pods, ingress, cert-manager, Istio |
| `app` | Standard_DS3_v2 | 2 | 6 | User | None | 6 API-facing microservices |
| `worker` | Standard_DS2_v2 | 0 | 3 | User | `workload=background:NoSchedule` | Background workers (notification, audit, projection, streaming-flush) |

The worker pool scales to zero when no events are queued, eliminating idle compute costs. Pods targeting the worker pool use matching tolerations.

---

## Namespace Design

| Namespace | Purpose | Pods |
|---|---|---|
| `ehr` | EHR Platform services | ehr-api (2), ehr-frontend (2) → target: 11 services |
| `pets` | AKS Store Demo (reference app) | store-front, store-admin, order-service, product-service, makeline-service, ai-service, mongodb, rabbitmq, virtual-customer |
| `ingress-nginx` | NGINX Ingress Controller | ingress-nginx-controller |
| `cert-manager` | TLS certificate automation | cert-manager, cert-manager-cainjector, cert-manager-webhook |
| `kube-system` | Kubernetes system components | coredns, metrics-server, kube-proxy, etc. |
| `istio-system` | Service mesh (target) | istiod, istio-ingressgateway, istio-egressgateway |
| `monitoring` | Observability stack (target) | prometheus, grafana, loki, tempo |

---

## Ingress Architecture

### NGINX Ingress Controller

| Property | Value |
|---|---|
| **Version** | 1.12.0 |
| **Type** | Kubernetes LoadBalancer |
| **External IP** | 20.175.132.82 |
| **TLS** | cert-manager with self-signed ClusterIssuer |
| **Namespace** | ingress-nginx |

### Ingress Rules (Current)

```yaml
# AKS Store Demo Ingress
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: store-ingress
  namespace: pets
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
spec:
  ingressClassName: nginx
  tls:
    - hosts: ["20.175.132.82"]
      secretName: store-tls
  rules:
    - host: "20.175.132.82"
      http:
        paths:
          - path: /
            pathType: Prefix
            backend:
              service:
                name: store-front
                port: { number: 80 }
          - path: /admin
            pathType: Prefix
            backend:
              service:
                name: store-admin
                port: { number: 80 }
```

### Target Ingress Rules (EHR Services)

```yaml
# EHR Platform Ingress
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: ehr-ingress
  namespace: ehr
  annotations:
    nginx.ingress.kubernetes.io/ssl-redirect: "true"
    nginx.ingress.kubernetes.io/proxy-body-size: "10m"
    nginx.ingress.kubernetes.io/rate-limit-connections: "50"
spec:
  ingressClassName: nginx
  tls:
    - hosts: ["ehr.canadacentral.cloudapp.azure.com"]
      secretName: ehr-tls
  rules:
    - host: "ehr.canadacentral.cloudapp.azure.com"
      http:
        paths:
          - path: /api/v1/patients
            pathType: Prefix
            backend:
              service: { name: patient-service, port: { number: 8080 } }
          - path: /api/v1/appointments
            pathType: Prefix
            backend:
              service: { name: scheduling-service, port: { number: 8080 } }
          - path: /api/v1/prescriptions
            pathType: Prefix
            backend:
              service: { name: pharmacy-service, port: { number: 8080 } }
          - path: /api/v1/labresults
            pathType: Prefix
            backend:
              service: { name: lab-service, port: { number: 8080 } }
```

---

## Certificate Management

cert-manager v1.17.1 automates TLS certificate lifecycle.

### ClusterIssuer (Current — Self-Signed)

```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: selfsigned-issuer
spec:
  selfSigned: {}
```

### Target: Let's Encrypt Production

```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: ehr-platform@example.com
    privateKeySecretRef:
      name: letsencrypt-prod-key
    solvers:
      - http01:
          ingress:
            class: nginx
```

---

## Auto-Scaling Configuration

### Horizontal Pod Autoscaler (per service)

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: patient-service-hpa
  namespace: ehr
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: patient-service
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
```

### Pod Disruption Budget (per critical service)

```yaml
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: patient-service-pdb
  namespace: ehr
spec:
  minAvailable: 1
  selector:
    matchLabels:
      app: patient-service
```

---

## Health Probes

Every EHR service exposes Kubernetes health probes:

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 15
  periodSeconds: 20
  timeoutSeconds: 5
  failureThreshold: 3
readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
  timeoutSeconds: 3
  failureThreshold: 3
```

The `/health` endpoint returns:
```json
{
  "status": "healthy",
  "service": "ehr-api",
  "timestamp": "2026-03-22T14:30:00Z"
}
```

---

## Resource Quotas & Limits

### Per-Pod Resource Configuration

| Service | CPU Request | CPU Limit | Memory Request | Memory Limit |
|---|---|---|---|---|
| patient-service | 100m | 500m | 128Mi | 512Mi |
| clinical-service | 100m | 500m | 128Mi | 512Mi |
| scheduling-service | 100m | 250m | 128Mi | 256Mi |
| pharmacy-service | 100m | 250m | 128Mi | 256Mi |
| lab-service | 100m | 250m | 128Mi | 256Mi |
| provider-service | 50m | 250m | 64Mi | 256Mi |
| notification-worker | 50m | 250m | 64Mi | 128Mi |
| audit-worker | 50m | 250m | 64Mi | 128Mi |

### Namespace Resource Quota

```yaml
apiVersion: v1
kind: ResourceQuota
metadata:
  name: ehr-quota
  namespace: ehr
spec:
  hard:
    requests.cpu: "4"
    requests.memory: 8Gi
    limits.cpu: "8"
    limits.memory: 16Gi
    pods: "50"
```

---

## Upgrade Strategy

Kubernetes version upgrades follow the blue-green node pool strategy documented in Module N. The current version (1.33) will upgrade to 1.34 using:

1. Upgrade control plane only (`az aks upgrade --control-plane-only`)
2. Add green node pool with new K8s version
3. Cordon and drain blue (old) nodes
4. Validate workload health on green pool
5. Delete blue node pool

The interactive Upgrade Schedule Planner at `/upgrade-planner.html` generates complete timelines with CLI commands for each phase.

---

## Dependencies

- **Upstream**: Module A (resource group, VNet, ACR)
- **Downstream**: Module F (services deploy to AKS), Module H (monitoring stack), Module K (resiliency testing)
