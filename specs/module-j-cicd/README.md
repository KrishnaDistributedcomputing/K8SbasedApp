# Module J – CI/CD Pipeline

## Objective

Module J defines the continuous integration and continuous deployment pipeline. Every code change flows through a deterministic, automated pipeline: build, test, scan, package, deploy. No manual steps between commit and production except an approval gate.

---

## Pipeline Architecture

### GitHub Actions Workflow

```
  Push/PR to master
        │
        ▼
  ┌─────────────┐
  │  1. Build    │  dotnet restore, dotnet build
  └──────┬──────┘
         │
         ▼
  ┌─────────────┐
  │  2. Test     │  dotnet test, code coverage
  └──────┬──────┘
         │
         ▼
  ┌─────────────┐
  │  3. Scan     │  Security scan, dependency audit
  └──────┬──────┘
         │
         ▼
  ┌─────────────┐
  │  4. Package  │  Docker build, push to ACR
  └──────┬──────┘
         │
         ▼
  ┌─────────────┐
  │  5. Deploy   │  AKS rolling update / Web App swap
  └─────────────┘
```

### Stage Details

| Stage | Tool | Trigger | Duration Target |
|---|---|---|---|
| Build | dotnet 8.0 SDK | Every push/PR | < 2 min |
| Test | dotnet test + xUnit | Every push/PR | < 3 min |
| Scan | dotnet list package --vulnerable, Trivy | Every push/PR | < 2 min |
| Package | docker build, az acr build | Merge to master | < 5 min |
| Deploy | kubectl set image / az webapp config | Merge to master | < 3 min |

---

## Workflow Definition

### CI Pipeline (Pull Requests)

```yaml
name: CI
on:
  pull_request:
    branches: [master]
    paths:
      - 'src/ehr-api/**'
      - 'src/architecture-docs/**'

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore src/ehr-api/EhrApi/EhrApi.csproj

      - name: Build
        run: dotnet build src/ehr-api/EhrApi/EhrApi.csproj --no-restore -c Release

      - name: Test
        run: dotnet test src/ehr-api/EhrApi.Tests/EhrApi.Tests.csproj --no-build -c Release --collect:"XPlat Code Coverage"

      - name: Vulnerability Scan
        run: dotnet list src/ehr-api/EhrApi/EhrApi.csproj package --vulnerable --include-transitive 2>&1 | tee vuln-report.txt

      - name: Upload Coverage
        uses: actions/upload-artifact@v4
        with:
          name: coverage
          path: '**/coverage.cobertura.xml'
```

### CD Pipeline (Merge to Master)

```yaml
name: CD
on:
  push:
    branches: [master]
    paths:
      - 'src/ehr-api/**'

env:
  ACR_NAME: ehracrcanada
  RESOURCE_GROUP: rg-aks-store-demo
  AKS_CLUSTER: aks-store-demo

jobs:
  deploy-api:
    runs-on: ubuntu-latest
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Azure Login
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Build and Push to ACR
        run: |
          az acr build \
            --registry $ACR_NAME \
            --image ehr-api:${{ github.sha }} \
            --image ehr-api:latest \
            --file src/ehr-api/EhrApi/Dockerfile \
            src/ehr-api/EhrApi/

      - name: Set AKS Context
        uses: azure/aks-set-context@v4
        with:
          resource-group: ${{ env.RESOURCE_GROUP }}
          cluster-name: ${{ env.AKS_CLUSTER }}

      - name: Deploy to AKS
        run: |
          kubectl set image deployment/ehr-api \
            ehr-api=$ACR_NAME.azurecr.io/ehr-api:${{ github.sha }} \
            -n ehr

      - name: Update Web App
        run: |
          az webapp config container set \
            --resource-group $RESOURCE_GROUP \
            --name ehr-api-canadacentral \
            --container-image-name $ACR_NAME.azurecr.io/ehr-api:${{ github.sha }} \
            --container-registry-url https://$ACR_NAME.azurecr.io \
            --container-registry-user $ACR_NAME \
            --container-registry-password $(az acr credential show -n $ACR_NAME --query "passwords[0].value" -o tsv)

      - name: Verify Deployment
        run: |
          kubectl rollout status deployment/ehr-api -n ehr --timeout=120s
```

---

## Image Tagging Strategy

| Tag | Purpose | Example |
|---|---|---|
| `:<git-sha>` | Immutable, traceable to exact commit | `ehr-api:a1b2c3d` |
| `:v<N>` | Human-readable release version | `ehr-api:v4` |
| `:latest` | Convenience for dev/testing only | `ehr-api:latest` |

**Rule**: Production deployments always use SHA or version tags. Never deploy `:latest` to production.

---

## Environment Strategy

| Environment | Trigger | AKS Namespace | Approval |
|---|---|---|---|
| Development | Push to feature branch | ehr-dev | None |
| Staging | PR to master | ehr-staging | None |
| Production | Merge to master | ehr | Manual gate |

### Current State (POC)

Single environment (`ehr` namespace) with direct deploy on push. Acceptable for POC phase; multi-environment promotion is a target-state enhancement.

---

## Secret Management in CI/CD

| Secret | Storage | Injection |
|---|---|---|
| AZURE_CREDENTIALS | GitHub Secrets | Service principal JSON |
| ACR_PASSWORD | GitHub Secrets | ACR admin password |
| DB_CONNECTION_STRING | Azure Key Vault | CSI driver at pod startup |
| JWT_SIGNING_KEY | Azure Key Vault | CSI driver at pod startup |

**Rule**: No secrets in code, Dockerfiles, or YAML manifests. All secrets are injected at runtime.

---

## Quality Gates

| Gate | Threshold | Enforcement |
|---|---|---|
| Build Success | Must pass | Block merge |
| Unit Tests | 100% pass | Block merge |
| Code Coverage | > 60% (target: 80%) | Warning (target: block) |
| Vulnerability Scan | No critical/high CVEs | Block merge |
| Container Scan | No critical CVEs in base image | Warning |
| API Contract | OpenAPI spec valid | Block merge |

---

## Dependencies

- **Upstream**: Module A (ACR for image storage), Module B (AKS for deployment target), Module I (security scanning)
- **Downstream**: Module L (exit criteria validated by pipeline)
