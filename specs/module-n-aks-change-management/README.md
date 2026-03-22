# Module N – AKS Change Management

## Objective

Module N defines the Kubernetes upgrade strategy, node pool management procedures, and change control processes. Every cluster change — K8s version upgrade, node image update, configuration change — follows a documented, tested, and reversible procedure.

---

## Kubernetes Version Strategy

### Current State

| Component | Version |
|---|---|
| AKS Cluster | 1.33.x |
| Node Image | Ubuntu 22.04 (AKS-managed) |
| NGINX Ingress | 1.12.0 |
| cert-manager | 1.17.1 |

### Upgrade Cadence

| Activity | Frequency | Window |
|---|---|---|
| K8s minor version upgrade | Every 4 months (aligned with AKS support) | Maintenance window |
| Node image update | Monthly (auto-upgrade channel) | Rolling, automatic |
| Ingress controller upgrade | Quarterly | Maintenance window |
| cert-manager upgrade | As needed (security patches) | Maintenance window |

### AKS Auto-Upgrade Channels

| Channel | Behavior | Recommended For |
|---|---|---|
| none | No automatic upgrades | — |
| patch | Auto-apply patch versions | Production |
| stable | Auto-apply N-1 minor version | Risk-averse environments |
| rapid | Auto-apply latest minor version | Dev/test |
| node-image | Auto-apply node image updates only | All environments |

**POC Configuration**: `node-image` channel (node OS patches auto-applied, K8s version upgrades manual).

```bash
az aks update \
  --resource-group rg-aks-store-demo \
  --name aks-store-demo \
  --auto-upgrade-channel node-image
```

---

## Blue-Green Node Pool Upgrade

### Procedure

The blue-green strategy enables zero-downtime K8s upgrades by creating a new node pool, migrating workloads, then removing the old pool.

**Phase 1: Prepare (Day -7)**

```bash
# Check available versions
az aks get-upgrades \
  --resource-group rg-aks-store-demo \
  --name aks-store-demo \
  -o table

# Review deprecation notices
kubectl get apiversions | grep -i deprecated
```

**Phase 2: Create Green Pool (Day 0)**

```bash
# Add new node pool with target K8s version
az aks nodepool add \
  --resource-group rg-aks-store-demo \
  --cluster-name aks-store-demo \
  --name green \
  --node-count 3 \
  --node-vm-size Standard_DS2_v2 \
  --kubernetes-version 1.34.0 \
  --labels pool=green \
  --no-wait
```

**Phase 3: Cordon Blue Pool**

```bash
# Prevent new pods from scheduling on blue nodes
kubectl cordon -l agentpool=nodepool1

# Verify green nodes are Ready
kubectl get nodes -l agentpool=green
```

**Phase 4: Drain Blue Pool**

```bash
# Gracefully evict pods (respects PDB)
kubectl drain -l agentpool=nodepool1 \
  --ignore-daemonsets \
  --delete-emptydir-data \
  --grace-period=60

# Verify all pods rescheduled to green
kubectl get pods -n ehr -o wide
```

**Phase 5: Validate**

```bash
# Health checks
kubectl get pods -n ehr -o wide  # All on green nodes
curl -k https://20.175.132.82/health  # API responsive
curl -k https://20.175.132.82/api/patients  # Data accessible

# Run acceptance tests
# (Module L scenarios)
```

**Phase 6: Remove Blue Pool**

```bash
# Only after validation passes
az aks nodepool delete \
  --resource-group rg-aks-store-demo \
  --cluster-name aks-store-demo \
  --name nodepool1 \
  --no-wait
```

**Rollback (if validation fails):**

```bash
# Uncordon blue nodes
kubectl uncordon -l agentpool=nodepool1

# Drain green nodes back to blue
kubectl drain -l agentpool=green \
  --ignore-daemonsets \
  --delete-emptydir-data

# Delete green pool
az aks nodepool delete \
  --resource-group rg-aks-store-demo \
  --cluster-name aks-store-demo \
  --name green
```

---

## Upgrade Planner

An interactive upgrade planner is available at:

**URL**: https://ehr-architecture-canadacentral.azurewebsites.net/upgrade-planner.html

Features:
- Visual timeline of K8s version support windows
- Configurable upgrade schedule (conservative vs. aggressive)
- Pre-upgrade checklist generator
- Rollback procedure reference

---

## Change Control Process

### Change Request Template

| Field | Description |
|---|---|
| Change ID | CHG-YYYY-NNN |
| Description | What is changing and why |
| Impact | Services affected, expected downtime |
| Risk Level | Low / Medium / High / Critical |
| Rollback Plan | Step-by-step reversal procedure |
| Validation | How to confirm success |
| Approver | Platform lead sign-off |
| Schedule | Maintenance window date/time |

### Change Categories

| Category | Examples | Approval | Lead Time |
|---|---|---|---|
| Standard | Node image update, HPA tuning | Auto-approved | 1 day |
| Normal | K8s version upgrade, ingress update | Team lead | 3 days |
| Emergency | Security patch, critical fix | Post-approval | Immediate |

---

## Maintenance Windows

| Window | Schedule | Duration | Activities |
|---|---|---|---|
| Weekly | Saturday 02:00-06:00 ET | 4 hours | Node image updates, minor config |
| Monthly | 1st Saturday 00:00-08:00 ET | 8 hours | K8s upgrades, component updates |
| Emergency | Any time | As needed | Critical security patches |

### Maintenance Mode Procedure

```bash
# 1. Scale down non-critical workloads
kubectl scale deployment ehr-frontend -n ehr --replicas=0

# 2. Perform maintenance
# ... (upgrade steps)

# 3. Validate core services
kubectl get pods -n ehr
curl -k https://20.175.132.82/health

# 4. Scale back up
kubectl scale deployment ehr-frontend -n ehr --replicas=2

# 5. Full validation
# Run Module L acceptance tests
```

---

## Addon & Component Upgrade Matrix

| Component | Current | Upgrade Path | Dependency |
|---|---|---|---|
| NGINX Ingress | 1.12.0 | Helm upgrade | Check K8s version compatibility |
| cert-manager | 1.17.1 | Helm upgrade | Check CRD migration notes |
| CSI Secret Store | — (target) | Helm install | AKS addon or Helm |
| Prometheus/Grafana | — (target) | Helm install | monitoring namespace |

### Helm Upgrade Process

```bash
# 1. Check current version
helm list -A

# 2. Update repo
helm repo update

# 3. Dry-run upgrade
helm upgrade ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --dry-run

# 4. Apply upgrade
helm upgrade ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx \
  --wait --timeout 300s

# 5. Verify
kubectl get pods -n ingress-nginx
```

---

## Dependencies

- **Upstream**: Module B (AKS cluster config), Module H (monitoring during upgrades), Module K (PDB/HPA for availability during upgrades)
- **Downstream**: Module L (upgrade success is exit criterion)
