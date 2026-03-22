# Module G – API Management & Versioning

## Objective

Module G defines the API management strategy — versioning, routing, rate limiting, analytics, and subscription tiers. The EHR API supports three simultaneous versions (v1, v2, v3) through URL-segment, header, and query-string versioning. Azure API Management provides the external gateway layer for authentication, throttling, and developer portal access.

---

## API Versioning Configuration

### Asp.Versioning.Mvc Setup

```csharp
builder.Services.AddApiVersioning(options => {
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version"));
}).AddApiExplorer(options => {
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### Version Reader Priority

| Method | Format | Example | Priority |
|---|---|---|---|
| URL Segment | `/api/v{N}/resource` | `/api/v2/patients` | Primary |
| Header | `X-Api-Version: N` | `X-Api-Version: 2` | Secondary |
| Query String | `?api-version=N` | `?api-version=2` | Tertiary |
| Default (v1) | `/api/resource` | `/api/patients` | Backward compat |

### Swagger Multi-Version Configuration

Three Swagger documents are generated and available in the Swagger UI dropdown:

```csharp
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title = "EHR API", Version = "v1",
        Description = "Stable release with full CRUD operations"
    });
    c.SwaggerDoc("v2", new OpenApiInfo {
        Title = "EHR API", Version = "v2",
        Description = "Enhanced with pagination, DTOs, clinical dashboard"
    });
    c.SwaggerDoc("v3", new OpenApiInfo {
        Title = "EHR API", Version = "v3",
        Description = "Future — AI decision support, FHIR R4, streaming"
    });
});
```

---

## API Version Feature Matrix

| Capability | v1 Stable | v2 Enhanced | v3 Preview |
|---|---|---|---|
| CRUD Operations | Full (8 controllers, 30+ endpoints) | Full (7 controllers, 20+ endpoints) | Partial (4 controllers, 12+ endpoints) |
| Response Format | EF Core entities | Purpose-built DTOs | FHIR Resources + DTOs |
| Pagination | None | PagedResult with totalCount, hasNext | FHIR Bundle with _count/_offset |
| Sorting | None | Multi-column (sortBy/sortDir) | N/A |
| Dashboard | None | Clinical KPI dashboard | Population health trends |
| Patient Summary | Include allergies + vitals | Full clinical summary endpoint | FHIR Patient resource |
| Provider Schedule | None | Date-range schedule endpoint | N/A |
| FHIR R4 | None | None | Patient resource, Bundle search |
| AI/ML | None | None | Drug interactions, readmission risk |
| Bulk Export | None | None | FHIR Bulk Data (NDJSON) |
| Event Streaming | None | None | Webhook subscriptions |

---

## Azure API Management (Target)

### APIM Configuration

| Property | Value |
|---|---|
| **Name** | `apim-ehr-canadacentral` |
| **SKU** | Developer (POC) → Standard (production) |
| **Region** | Canada Central |
| **Gateway URL** | `https://apim-ehr-canadacentral.azure-api.net` |

### API Products

| Product | APIs Included | Rate Limit | Subscription |
|---|---|---|---|
| **EHR Free** | v1 read-only endpoints | 100 calls/minute | Self-service |
| **EHR Standard** | v1 + v2 full access | 1000 calls/minute | Approval required |
| **EHR Premium** | v1 + v2 + v3 preview | 5000 calls/minute | Enterprise contract |
| **FHIR Interop** | v3 FHIR endpoints only | 500 calls/minute | Partner agreement |

### APIM Policies

**Inbound Policy** (all products):
```xml
<inbound>
    <base />
    <set-header name="X-Correlation-ID" exists-action="skip">
        <value>@(context.RequestId.ToString())</value>
    </set-header>
    <rate-limit-by-key
        calls="@(context.Product.Name == "EHR Premium" ? 5000 : 1000)"
        renewal-period="60"
        counter-key="@(context.Subscription.Key)" />
    <validate-jwt header-name="Authorization" require-scheme="Bearer">
        <openid-config url="https://login.microsoftonline.com/{tenant}/.well-known/openid-configuration" />
        <required-claims>
            <claim name="aud" match="all">
                <value>api://ehr-api</value>
            </claim>
        </required-claims>
    </validate-jwt>
</inbound>
```

---

## Endpoint Routing — Current vs. Target

### Current: NGINX Ingress + Web Apps

```
External → NGINX Ingress (20.175.132.82) → AKS Services
External → Azure Web Apps (*.azurewebsites.net) → Container Images
```

### Target: APIM + NGINX Ingress

```
External → APIM Gateway → NGINX Ingress → AKS Services
                ↓
            Analytics, Rate Limiting, OAuth Validation
```

---

## Interactive Documentation Pages

| Page | URL | Content |
|---|---|---|
| **API Version Catalog** | `/api-versions.html` | V1/v2/v3 endpoint explorer, feature matrix, roadmap |
| **Swagger UI** | `/swagger` | Live API testing with version selector dropdown |
| **Landing Portal** | `/` | Platform overview with all app links |
| **Architecture Docs** | `/architecture.html` | Versioning strategy section with Mermaid diagrams |

---

## Dependencies

- **Upstream**: Module B (NGINX Ingress), Module F (services to route to)
- **Downstream**: Module I (OAuth token validation), Module H (API analytics)
