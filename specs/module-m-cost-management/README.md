# Module M – Cost Management

## Objective

Module M defines the cost tracking, optimization, and governance strategy. Every Azure resource has a known monthly cost, an assigned cost center, and a defined scaling boundary. The costing dashboard provides real-time visibility into spend vs. budget.

---

## Current Monthly Cost Estimate

### Compute

| Resource | SKU | Qty | Unit Cost | Monthly |
|---|---|---|---|---|
| AKS Node Pool | Standard_DS2_v2 (2 vCPU, 7 GB) | 3 | ~$104 | $312.00 |
| App Service Plan | B2 Linux (2 vCPU, 3.5 GB) | 1 | ~$54 | $54.00 |
| Web Apps (on plan) | — | 4 | $0 (included) | $0.00 |

### Database

| Resource | SKU | Storage | Monthly |
|---|---|---|---|
| PostgreSQL Flexible Server | Standard_B2s (2 vCPU, 4 GB) | 32 GB | ~$52.00 |

### Container Registry

| Resource | Tier | Storage Used | Monthly |
|---|---|---|---|
| ACR (ehracrcanada) | Basic | ~2 GB | ~$5.00 |

### Networking

| Resource | Type | Monthly |
|---|---|---|
| Load Balancer (AKS) | Standard | ~$18.00 |
| Public IP (Ingress) | Static | ~$3.60 |
| Egress (estimated) | 50 GB | ~$4.35 |

### Summary

| Category | Monthly Cost |
|---|---|
| Compute (AKS + App Service) | $366.00 |
| Database | $52.00 |
| Container Registry | $5.00 |
| Networking | $25.95 |
| **Total Estimated** | **$448.95** |

---

## Costing Dashboard

### Implementation

The costing dashboard is a standalone web application deployed to Azure Web App (`ehr-costing-canadacentral`). It:

1. Queries Azure Retail Prices API for current SKU pricing
2. Displays per-resource cost breakdowns
3. Shows category totals and overall monthly estimate
4. Provides optimization recommendations

### Live URL

https://ehr-costing-canadacentral.azurewebsites.net

---

## Cost Optimization Opportunities

### Immediate Savings (POC Phase)

| Optimization | Current | Proposed | Monthly Savings |
|---|---|---|---|
| AKS node auto-shutdown (nights/weekends) | Always on | 10h/day weekdays | ~$200 |
| Spot nodes for non-production | On-demand | Spot instances | ~$180 |
| Reserved Instances (1-year) | Pay-as-you-go | RI for DB + nodes | ~$90 |

### Production Optimizations

| Optimization | Description | Estimated Savings |
|---|---|---|
| Right-size node pools | Separate system/user pools with appropriate SKUs | 15-25% |
| Cluster autoscaler | Scale nodes 1-5 based on demand | 20-40% |
| PostgreSQL auto-pause | Burstable tier pauses when idle | Up to 60% on DB |
| ACR geo-replication removal | Single region for POC | $0 (already Basic) |
| Spot node pools | Tolerant workloads on Spot VMs | 60-80% on those nodes |

---

## Resource Tagging Strategy

All Azure resources are tagged for cost allocation:

| Tag | Value | Purpose |
|---|---|---|
| `project` | ehr-cloud-platform | Cost filtering |
| `environment` | poc | Environment identification |
| `owner` | platform-team | Accountability |
| `cost-center` | CC-EHR-POC | Finance allocation |
| `created-by` | cli | Provisioning method |

### Tag Enforcement

```bash
# Apply tags to resource group (inherited by resources)
az group update -n rg-aks-store-demo --tags \
  project=ehr-cloud-platform \
  environment=poc \
  owner=platform-team \
  cost-center=CC-EHR-POC
```

---

## Budget Alerts

| Alert | Threshold | Action |
|---|---|---|
| Budget Warning | 80% of $500/month | Email notification |
| Budget Critical | 100% of $500/month | Email + Teams notification |
| Anomaly Detection | 25% spike in daily spend | Email notification |
| Resource Creation | Any new resource outside RG | Azure Policy deny |

### Azure Budget Configuration

```bash
az consumption budget create \
  --resource-group rg-aks-store-demo \
  --budget-name ehr-poc-budget \
  --amount 500 \
  --time-grain Monthly \
  --start-date 2026-03-01 \
  --end-date 2026-12-31 \
  --category Cost
```

---

## Cost Scaling Model

| Users | AKS Nodes | DB SKU | App Plan | Est. Monthly |
|---|---|---|---|---|
| 1-50 (POC) | 3x DS2_v2 | B2s | B2 | ~$450 |
| 50-200 | 3x DS2_v2 | B4s | S1 | ~$650 |
| 200-500 | 5x DS3_v2 | D4s | S2 | ~$1,400 |
| 500-1000 | 7x DS3_v2 + spot | D8s | P1v3 | ~$3,200 |
| 1000+ | Auto-scale (3-15) | D16s + read replicas | P2v3 | ~$6,500+ |

---

## Dependencies

- **Upstream**: Module A (resource inventory), Module B (AKS node config)
- **Downstream**: Module 0 (cost optimization business outcome), Module L (cost within budget is exit criterion)
