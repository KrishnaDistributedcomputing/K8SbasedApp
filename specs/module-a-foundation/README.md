# Module A – Foundation & Infrastructure-as-Code

## Objective

Module A defines the foundational Azure infrastructure that supports the entire EHR Cloud Platform. Every resource — from the resource group to the virtual network to the Key Vault — is defined declaratively, versioned in source control, and deployable through automated pipelines. This module establishes the base layer upon which all other modules build.

---

## Resource Group & Region Strategy

All EHR Cloud Platform resources reside in a single resource group `rg-aks-store-demo` deployed in **Azure Canada Central**. The choice of Canada Central is deliberate:

- **Data Sovereignty**: All Protected Health Information (PHI) remains within Canadian borders, meeting PIPEDA and provincial health privacy requirements.
- **Service Availability**: Canada Central supports all required services — AKS, PostgreSQL Flexible Server, Container Registry, App Service, Event Hubs, Key Vault, and API Management.
- **Latency**: Sub-10ms latency to major Canadian healthcare institutions.
- **Paired Region**: Canada East serves as the disaster recovery pair for geo-redundant backups.

**Subscription**: `Jan 2026 Subscription` (ID: `e62428e7-08dd-4bc2-82e2-2c51586d9105`)

---

## Azure Resource Inventory

### Compute

| Resource | Type | SKU | Purpose |
|---|---|---|---|
| `aks-store-demo` | Azure Kubernetes Service | Standard, 3× Standard_DS2_v2 | Container orchestration for all microservices |
| `asp-ehr-canadacentral` | App Service Plan | B2 Linux | Hosts 4 container Web Apps for external access |
| `ehr-api-canadacentral` | Web App | Container (B2) | EHR REST API with Swagger UI |
| `ehr-portal-canadacentral` | Web App | Container (B2) | EHR SPA Frontend |
| `ehr-costing-canadacentral` | Web App | Container (B2) | Cost breakdown dashboard |
| `ehr-architecture-canadacentral` | Web App | Container (B2) | Architecture docs, landing portal, upgrade planner |

### Data

| Resource | Type | SKU | Purpose |
|---|---|---|---|
| `ehrdb-canadacentral` | PostgreSQL Flexible Server | Standard_B2s (Burstable), v16, 32GB | Primary database for all EHR data |

### Container Registry

| Resource | Type | SKU | Purpose |
|---|---|---|---|
| `ehracrcanada` | Azure Container Registry | Basic | Docker image registry (5 repositories) |

### Networking

| Resource | Type | Details | Purpose |
|---|---|---|---|
| NGINX Ingress Controller | Kubernetes LoadBalancer | v1.12.0, IP: 20.175.132.82 | External traffic routing into AKS |
| cert-manager | Kubernetes Operator | v1.17.1 | TLS certificate automation |
| Self-signed ClusterIssuer | cert-manager resource | — | TLS for ingress endpoints |

---

## ACR Repository Inventory

The Azure Container Registry `ehracrcanada.azurecr.io` hosts five repositories:

| Repository | Latest Tag | Size | Description |
|---|---|---|---|
| `ehr-api` | v4 | ~85MB | ASP.NET Core 8 EHR API (multi-stage build) |
| `ehr-frontend` | v3-webapp | ~25MB | SPA frontend (nginx:alpine + static assets) |
| `architecture-docs` | v5 | ~15MB | Landing portal + architecture docs + planner |
| `costing-app` | v1 | ~15MB | Azure cost breakdown dashboard |

Admin access is enabled on the registry for Web App container pulls. Target state replaces admin credentials with Managed Identity-based ACR pull.

---

## Infrastructure-as-Code Strategy

### Current State: Azure CLI Automation

The POC was provisioned using Azure CLI commands executed in sequence. Key provisioning commands include:

```bash
# Resource Group
az group create --name rg-aks-store-demo --location canadacentral

# AKS Cluster
az aks create --resource-group rg-aks-store-demo \
  --name aks-store-demo \
  --location canadacentral \
  --node-count 3 \
  --node-vm-size Standard_DS2_v2 \
  --kubernetes-version 1.33 \
  --generate-ssh-keys

# PostgreSQL
az postgres flexible-server create \
  --resource-group rg-aks-store-demo \
  --name ehrdb-canadacentral \
  --location canadacentral \
  --admin-user ehradmin \
  --sku-name Standard_B2s \
  --tier Burstable \
  --storage-size 32 \
  --version 16

# Container Registry
az acr create --resource-group rg-aks-store-demo \
  --name ehracrcanada \
  --sku Basic \
  --admin-enabled true

# App Service Plan
az appservice plan create \
  --resource-group rg-aks-store-demo \
  --name asp-ehr-canadacentral \
  --sku B2 \
  --is-linux
```

### Target State: Bicep Modules

The target infrastructure-as-code approach uses Bicep modules organized by resource type:

```
infrastructure/
├── main.bicep                    # Orchestrator
├── modules/
│   ├── aks.bicep                 # AKS cluster + node pools
│   ├── postgres.bicep            # PostgreSQL Flexible Server
│   ├── acr.bicep                 # Container Registry
│   ├── appservice.bicep          # App Service Plan + Web Apps
│   ├── keyvault.bicep            # Key Vault + secrets
│   ├── vnet.bicep                # Virtual Network + subnets
│   ├── eventhubs.bicep           # Event Hubs namespace + topics
│   └── apim.bicep                # API Management instance
├── parameters/
│   ├── dev.bicepparam            # Development environment
│   ├── staging.bicepparam        # Staging environment
│   └── prod.bicepparam           # Production environment
└── .github/workflows/
    └── infra-deploy.yml          # GitHub Actions for IaC deployment
```

Each Bicep module is parameterized for environment-specific values (SKU, node count, storage size) and uses Azure-managed encryption keys by default.

---

## Environment Lifecycle Management

### Scale-Down Script

Reduces compute costs by ~60% during off-hours:

```bash
# Scale AKS to minimum
az aks nodepool update --resource-group rg-aks-store-demo \
  --cluster-name aks-store-demo --name nodepool1 --min-count 1

# Stop non-essential Web Apps
az webapp stop --resource-group rg-aks-store-demo --name ehr-costing-canadacentral
az webapp stop --resource-group rg-aks-store-demo --name ehr-architecture-canadacentral
```

### Scale-Up Script

Restores full capacity for business hours:

```bash
az aks nodepool update --resource-group rg-aks-store-demo \
  --cluster-name aks-store-demo --name nodepool1 --min-count 3

az webapp start --resource-group rg-aks-store-demo --name ehr-costing-canadacentral
az webapp start --resource-group rg-aks-store-demo --name ehr-architecture-canadacentral
```

### Teardown Script

Destroys all resources in a single command:

```bash
az group delete --name rg-aks-store-demo --yes --no-wait
```

---

## Naming Conventions

| Resource Type | Pattern | Example |
|---|---|---|
| Resource Group | `rg-{project}` | `rg-aks-store-demo` |
| AKS Cluster | `aks-{project}` | `aks-store-demo` |
| PostgreSQL | `{function}db-{region}` | `ehrdb-canadacentral` |
| ACR | `{function}acr{region}` | `ehracrcanada` |
| App Service Plan | `asp-{function}-{region}` | `asp-ehr-canadacentral` |
| Web App | `{function}-{region}` | `ehr-api-canadacentral` |
| Key Vault | `kv-{function}-{region}` | `kv-ehr-canadacentral` |

---

## Tagging Strategy

All resources should carry these tags for cost allocation and lifecycle management:

| Tag | Value | Purpose |
|---|---|---|
| `project` | `ehr-cloud-platform` | Cost allocation |
| `environment` | `poc` / `dev` / `staging` / `prod` | Environment identification |
| `owner` | `ehr-platform-team` | Ownership |
| `module` | `A` through `N` | Module traceability |
| `managed-by` | `bicep` / `manual` | IaC tracking |

---

## Dependencies

Module A has no upstream dependencies — it is the foundational layer. All other modules depend on Module A:

- **Module B** (AKS Platform) requires the resource group, VNet, and ACR
- **Module C** (Shared Services) requires the resource group and Key Vault
- **Module D** (Data Layer) requires the resource group and VNet
- **Module E** (Messaging) requires the resource group
- **Module F** (Core Services) requires AKS, ACR, and PostgreSQL
- **Module I** (Security) requires Key Vault and Entra ID
- **Module J** (CI/CD) requires ACR and AKS credentials
