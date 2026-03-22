# Module C – Shared Services & Container Registry

## Objective

Module C defines the shared infrastructure services consumed by all microservices — Azure Container Registry for image management, Redis Cache for performance optimization, and Azure Key Vault for secret management. These services are shared across bounded contexts but maintain clear access boundaries.

---

## Azure Container Registry

### Configuration

| Property | Value |
|---|---|
| **Name** | `ehracrcanada` |
| **SKU** | Basic |
| **Login Server** | `ehracrcanada.azurecr.io` |
| **Admin Enabled** | Yes (POC) → Managed Identity (target) |
| **Region** | Canada Central |

### Repository Inventory

| Repository | Tags | Description |
|---|---|---|
| `ehr-api` | v1, v2, v3, v4 | ASP.NET Core 8 EHR REST API |
| `ehr-frontend` | v1, v2-webapp, v3-webapp | SPA frontend with nginx reverse proxy |
| `architecture-docs` | v1, v2, v3, v4, v5 | Architecture docs, landing portal, planner |
| `costing-app` | v1 | Azure cost breakdown dashboard |

### Image Build Strategy

All images use server-side ACR builds for consistent, reproducible builds:

```bash
az acr build \
  --registry ehracrcanada \
  --image ehr-api:v4 \
  --file src/ehr-api/EhrApi/Dockerfile \
  src/ehr-api/EhrApi/
```

### Multi-Stage Dockerfile Pattern

```dockerfile
# Build stage — .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

# Runtime stage — ASP.NET 8 (~85MB)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "EhrApi.dll"]
```

### Image Tagging Strategy

| Tag Format | Example | Purpose |
|---|---|---|
| Semantic version | `ehr-api:v4` | Release tracking |
| Git SHA | `ehr-api:a1b2c3d` | Commit traceability (target) |
| `latest` | `ehr-api:latest` | Development only (never production) |

### Target: Managed Identity ACR Pull

Replace admin credentials with AKS Managed Identity:

```bash
az aks update --resource-group rg-aks-store-demo \
  --name aks-store-demo \
  --attach-acr ehracrcanada
```

---

## Redis Cache (Target)

### Purpose

Redis provides a high-performance caching layer between the API services and PostgreSQL, absorbing read-heavy workloads and protecting the database during traffic spikes.

### Configuration (Target)

| Property | Value |
|---|---|
| **Name** | `redis-ehr-canadacentral` |
| **SKU** | Basic C1 (1 GB, no replication) |
| **Version** | 6.x |
| **TLS** | Required (port 6380) |
| **Eviction Policy** | allkeys-lru |

### Caching Strategy

| Cache Key Pattern | TTL | Source Service | Purpose |
|---|---|---|---|
| `patient:{id}` | 5 min | Patient Service | Frequently accessed demographics |
| `provider:{id}` | 15 min | Provider Service | Provider directory lookups |
| `provider:list:active` | 10 min | Provider Service | Active provider dropdown |
| `dashboard:kpis` | 1 min | Dashboard endpoint | Clinical dashboard KPI counts |
| `schedule:{providerId}:{date}` | 5 min | Scheduling Service | Provider daily schedule |

### Cache-Aside Pattern

```csharp
public async Task<PatientDetailDto?> GetPatientAsync(int id)
{
    var cacheKey = $"patient:{id}";
    var cached = await _redis.GetStringAsync(cacheKey);
    if (cached is not null)
        return JsonSerializer.Deserialize<PatientDetailDto>(cached);

    var patient = await _db.Patients.FindAsync(id);
    if (patient is null) return null;

    var dto = MapToDetailDto(patient);
    await _redis.SetStringAsync(cacheKey,
        JsonSerializer.Serialize(dto),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
    return dto;
}
```

### Cache Invalidation

Cache entries are invalidated when the owning service processes a write operation:

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> Update(int id, UpdatePatientDto dto)
{
    // ... update database ...
    await _redis.RemoveAsync($"patient:{id}");
    return NoContent();
}
```

---

## Azure Key Vault (Target)

### Purpose

Centralizes secret management for all services. No secrets stored in environment variables, appsettings.json, or Kubernetes ConfigMaps.

### Configuration

| Property | Value |
|---|---|
| **Name** | `kv-ehr-canadacentral` |
| **SKU** | Standard |
| **Soft Delete** | Enabled (90-day retention) |
| **Purge Protection** | Enabled |
| **Access Model** | Azure RBAC |

### Secret Inventory

| Secret Name | Consumer | Rotation Frequency |
|---|---|---|
| `postgresql-connection-string` | All data services | 90 days |
| `redis-connection-string` | All API services | 90 days |
| `eventhubs-connection-string` | Producers + consumers | 90 days |
| `acr-password` | CI/CD pipeline | On demand |
| `apim-subscription-key` | External consumers | 30 days |

### AKS Integration: CSI Secret Store Driver

```yaml
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: ehr-secrets
  namespace: ehr
spec:
  provider: azure
  parameters:
    keyvaultName: kv-ehr-canadacentral
    tenantId: "<tenant-id>"
    objects: |
      array:
        - |
          objectName: postgresql-connection-string
          objectType: secret
        - |
          objectName: redis-connection-string
          objectType: secret
```

---

## Dependencies

- **Upstream**: Module A (resource group, VNet)
- **Downstream**: Module F (services pull images from ACR, read secrets from Key Vault, use Redis cache)
