# Module L – Exit Criteria & Acceptance

## Objective

Module L defines the success criteria that must be met before the POC is considered complete. Each criterion is measurable, testable, and mapped to a business outcome from Module 0. Nothing ships without passing these gates.

---

## POC Exit Criteria

### Infrastructure (Module A, B, C)

| # | Criterion | Validation Method | Status |
|---|---|---|---|
| L-01 | AKS cluster running with 3 nodes in Canada Central | `kubectl get nodes` returns 3 Ready nodes | PASS |
| L-02 | All pods in ehr namespace healthy | `kubectl get pods -n ehr` shows Running, 0 restarts | PASS |
| L-03 | NGINX Ingress Controller serving traffic | External IP reachable, HTTP 200 on / | PASS |
| L-04 | ACR contains all application images | `az acr repository list` shows 5 repos | PASS |
| L-05 | PostgreSQL accessible from AKS pods | EF Core migration runs, API returns data | PASS |
| L-06 | cert-manager issuing certificates | `kubectl get certificates` shows Ready=True | PASS |

### Application (Module F, G)

| # | Criterion | Validation Method | Status |
|---|---|---|---|
| L-07 | EHR API responds on /swagger | HTTP 200, Swagger UI loads | PASS |
| L-08 | All 8 entity CRUD endpoints functional | POST/GET/PUT/DELETE return correct status codes | PASS |
| L-09 | Seed data loaded (133+ records) | GET /api/patients returns seeded patients | PASS |
| L-10 | API versioning v1/v2/v3 operational | /api/v1/, /api/v2/, /api/v3/ all return data | PENDING |
| L-11 | v2 pagination works correctly | /api/v2/patients?page=1&pageSize=5 returns paged results | PENDING |
| L-12 | v3 FHIR endpoints return valid structure | /api/v3/fhir/patients returns FHIR Bundle | PENDING |
| L-13 | Frontend portal loads and displays data | HTTP 200, patient list renders | PASS |

### External Access (Module B, G)

| # | Criterion | Validation Method | Status |
|---|---|---|---|
| L-14 | AKS Ingress routes to API | https://20.175.132.82/api/patients returns JSON | PASS |
| L-15 | Azure Web App - API accessible | https://ehr-api-canadacentral.azurewebsites.net/swagger | PASS |
| L-16 | Azure Web App - Portal accessible | https://ehr-portal-canadacentral.azurewebsites.net | PASS |
| L-17 | Azure Web App - Costing accessible | https://ehr-costing-canadacentral.azurewebsites.net | PASS |
| L-18 | Azure Web App - Architecture docs accessible | https://ehr-architecture-canadacentral.azurewebsites.net | PASS |

### Documentation (All Modules)

| # | Criterion | Validation Method | Status |
|---|---|---|---|
| L-19 | Architecture design document complete | 12+ tabs in interactive webpage | PASS |
| L-20 | K8s upgrade strategy documented | Upgrade planner with interactive schedule | PASS |
| L-21 | API version catalog page live | Version comparison, endpoint explorer | PASS |
| L-22 | Spec documents for all modules (0-N) | 15 README.md files in specs/ directory | IN PROGRESS |
| L-23 | All code in GitHub repository | https://github.com/KrishnaDistributedcomputing/K8SbasedApp | PASS |

### Observability (Module H)

| # | Criterion | Validation Method | Status |
|---|---|---|---|
| L-24 | Health endpoints respond | /health returns 200 with status JSON | PASS |
| L-25 | Structured logging active | Application logs in JSON format | PASS |
| L-26 | Web App diagnostic logs enabled | `az webapp log download` returns log files | PASS |

### Security (Module I)

| # | Criterion | Validation Method | Status |
|---|---|---|---|
| L-27 | TLS enforced on all public endpoints | No plaintext HTTP connections accepted | PASS |
| L-28 | Database requires SSL | `Ssl Mode=Require` in connection string | PASS |
| L-29 | No secrets in source code | grep for passwords/keys in repo returns 0 | PASS |
| L-30 | Container runs as non-root | Dockerfile USER directive or .NET default | PASS |

---

## Acceptance Test Scenarios

### Scenario 1: Patient Lifecycle

```
1. POST /api/v1/patients → 201 Created (new patient)
2. GET /api/v1/patients/{id} → 200 OK (patient details)
3. PUT /api/v1/patients/{id} → 200 OK (update demographics)
4. GET /api/v2/patients?search=John → 200 OK (paginated results)
5. DELETE /api/v1/patients/{id} → 204 No Content
```

### Scenario 2: Clinical Workflow

```
1. POST /api/v1/appointments → 201 (schedule appointment)
2. POST /api/v1/vitals → 201 (record vitals at visit)
3. POST /api/v1/medicalrecords → 201 (document encounter)
4. POST /api/v1/prescriptions → 201 (prescribe medication)
5. POST /api/v1/labresults → 201 (order lab)
6. GET /api/v2/dashboard → 200 (clinical dashboard KPIs)
```

### Scenario 3: Resilience

```
1. Scale API to 1 replica
2. kubectl delete pod ehr-api-xxx → pod restarts
3. During restart, API returns 503 briefly
4. New pod passes readiness, traffic resumes
5. No data loss observed
```

### Scenario 4: Deployment Rollback

```
1. Deploy broken image (bad tag)
2. New pods fail startup probe
3. Old pods continue serving (maxUnavailable: 0)
4. kubectl rollout undo → previous version restored
5. Zero downtime confirmed
```

---

## Sign-Off Matrix

| Stakeholder | Criteria Owned | Sign-Off |
|---|---|---|
| Platform Engineer | L-01 through L-06 | Infrastructure works |
| Backend Developer | L-07 through L-12 | API functional |
| Frontend Developer | L-13, L-16 | Portal functional |
| DevOps Engineer | L-14, L-15, L-17, L-18 | External access works |
| Technical Writer | L-19 through L-23 | Documentation complete |
| Security Engineer | L-27 through L-30 | Security baseline met |

---

## Dependencies

- **Upstream**: All modules (L validates outputs of every module)
- **Downstream**: Module 0 (business outcomes achieved)
