# Module 0 – Core Business Drivers & Outcome Dimensions

## Objective

The EHR Cloud Platform POC exists to validate that a modern, cloud-native microservices architecture on Azure Kubernetes Service can deliver an enterprise-grade Electronic Health Records system while meeting stringent healthcare compliance requirements. Every technical decision made in Modules A through N traces back to one or more of these business drivers. This document defines the rationale, expected outcomes, and success criteria that justify the investment.

The platform replaces the traditional monolithic EHR approach — where patient management, clinical encounters, scheduling, pharmacy, laboratory, and provider management are tightly coupled in a single deployable unit — with a decomposed, independently scalable microservices architecture running on AKS in Azure Canada Central.

---

## Standard Outcome Dimensions

### 1. Modernization

The primary modernization driver is the transition away from monolithic EHR application design toward a decomposed, independently deployable microservices architecture aligned with healthcare domain boundaries. Legacy EHR systems are typically characterized by tightly coupled modules where patient registration, clinical documentation, scheduling, pharmacy, and laboratory functions share a single database, a single deployment pipeline, and a single failure domain. A bug in the lab results module can take down the appointment scheduler. A database migration for pharmacy changes requires full system downtime.

The EHR Cloud Platform addresses this by decomposing the healthcare business domain into six bounded contexts — Patient Management, Clinical Encounters, Scheduling, Pharmacy & Rx, Laboratory & Diagnostics, and Provider Management — each implemented as independent C# microservices built on ASP.NET Core 8. Each service is containerized with its own Docker image, has its own deployment lifecycle in Kubernetes, and communicates with other services through versioned REST APIs (v1, v2, v3) or asynchronous events via Azure Event Hubs with Kafka API.

The current monolithic EHR API (ehr-api:v4) serves as the transitional architecture — a single ASP.NET Core 8 application with 8 Entity Framework Core DbSets covering all domain entities. API versioning infrastructure (Asp.Versioning.Mvc 8.1.0) is already in place with three simultaneous API versions:

- **v1 (Stable)**: Full CRUD across 8 entities with direct EF Core entity responses. Backward-compatible at both `/api/v1/` and legacy `/api/` paths. 8 controllers, 30+ endpoints.
- **v2 (Enhanced)**: DTO-based responses with server-side pagination (PagedResult), clinical dashboard, patient clinical summary, provider workload metrics, and advanced filtering/sorting. 7 controllers, 20+ endpoints.
- **v3 (Future Preview)**: FHIR R4 Patient resource compliance, AI-powered clinical decision support (drug interactions, readmission risk), population health analytics, bulk FHIR export, and real-time event streaming subscriptions. 4 controllers, 12+ endpoints.

Infrastructure provisioning has moved from manual Azure Portal operations to a combination of Azure CLI automation and declarative Kubernetes manifests stored in source control. The target state involves full Bicep/Terraform IaC templates for every Azure resource — from the virtual network to Key Vault to PostgreSQL firewall rules.

The net effect is a system where individual clinical capabilities can evolve, scale, and deploy independently. The pharmacy module can adopt a new drug interaction database without redeploying the appointment scheduler. The lab service can scale up during morning result processing without affecting the patient registration workflow.

---

### 2. Cloud-Native Scalability

Legacy EHR systems typically run on fixed-capacity infrastructure — often physical servers in hospital data centers — where the only scaling option is to purchase and provision additional hardware. This approach requires months of lead time, significant capital expenditure, and cannot respond to the daily demand patterns inherent in healthcare operations. Morning clinics generate 10x the appointment traffic of evening hours. Lab result processing spikes at 6 AM when overnight batch analyzers complete. Emergency department surges are by definition unpredictable.

The EHR Cloud Platform replaces this with elastic, container-based orchestration on Azure Kubernetes Service running Kubernetes v1.33 in Canada Central.

**Cluster Architecture**: The AKS cluster (`aks-store-demo`) runs on three Standard_DS2_v2 nodes (2 vCPU, 7 GB RAM each) with Azure CNI networking and a maximum of 250 pods per node. The current architecture supports 13 running pods across three namespaces — the `ehr` namespace hosting the EHR API (2 replicas) and EHR Frontend (2 replicas), the `pets` namespace hosting the AKS Store Demo (9 pods), and system namespaces for NGINX Ingress Controller v1.12.0 and cert-manager v1.17.1.

**Target Node Pool Design**: As the platform matures beyond POC, the cluster will be organized into three node pools with independent autoscaling policies:

- **System Pool**: Maintains Kubernetes control plane components, ingress controllers, cert-manager, and Istio service mesh components. Fixed 1-2 nodes.
- **Application Pool**: Hosts the six API-facing microservices. Autoscales between 2 and 6 nodes based on CPU utilization. Critical for handling clinical workload peaks.
- **Worker Pool**: Hosts background processors — streaming flush service, notification worker, audit worker, projection worker. Can scale down to zero nodes during off-hours when no events are queued, eliminating idle compute costs.

**Pod-Level Scaling**: Horizontal Pod Autoscalers monitor CPU and memory utilization for each service and adjust replica counts. Critical services (Patient Service, Clinical Service) maintain a minimum of 2 replicas at all times for availability. The current EHR API deployment already runs 2 replicas with health check endpoints at `/health` for Kubernetes liveness and readiness probes.

**Database Scaling**: Azure PostgreSQL Flexible Server (`ehrdb-canadacentral`) runs on a Burstable Standard_B2s SKU (2 vCPU, 4 GB RAM, 32 GB storage) with PostgreSQL 16. The Burstable tier provides baseline performance with the ability to burst above it when needed — ideal for the POC workload pattern where heavy activity occurs during demonstrations and testing, with idle periods in between.

**Caching Layer** (target): Redis will sit in front of the database for frequently accessed resources — patient demographic lookups, provider directory queries, and appointment availability checks. This reduces database connection pressure during traffic spikes and improves P95 response times for read-heavy clinical workflows.

---

### 3. Delivery Acceleration

Legacy EHR systems often have release cycles measured in months or quarters, with manual build processes, informal testing, and deployment procedures documented in runbooks that are executed by hand. Changes are risky, deployments are stressful, and the feedback loop from code change to production verification is measured in weeks.

The EHR Cloud Platform implements an automated build and deployment pipeline that targets a commit-to-deploy time of less than ten minutes.

**Current CI/CD Pipeline**: The project uses Azure Container Registry (`ehracrcanada` — Basic tier with 5 repositories) as the artifact repository. Container images are built using `az acr build` which performs server-side Docker builds and pushes directly to ACR. Image tags use semantic versions (ehr-api:v3, ehr-frontend:v3-webapp, architecture-docs:v5, costing-app:v1) for traceability.

Deployments to AKS use `kubectl apply` for Kubernetes manifests and `az webapp config container set` for the four Azure Web Apps (ehr-api, ehr-portal, ehr-costing, ehr-architecture). Web App deployments specify the full ACR image path with explicit registry credentials.

**Target CI/CD Pipeline** (GitHub Actions):

1. **Contract Validation Stage**: Validate OpenAPI 3.0 specifications for all three API versions (v1, v2, v3). Validate AsyncAPI specifications for event-driven messaging contracts. Pipeline stops if any contract fails linting.

2. **Build & Test Stage**: Restore NuGet packages, compile all .NET 8 projects, run unit tests and integration tests. A build failure stops the pipeline. Code coverage threshold enforcement.

3. **Container Build Stage**: Each service is built as a Docker image using multi-stage builds (SDK for compile → ASP.NET runtime for production). Images are tagged with the Git commit SHA and semantic version. All images pushed to ACR.

4. **Deploy Stage**: Authenticate to AKS using kubelogin with Entra ID. Apply Kubernetes manifests including deployments, services, ingress rules, and HPA configurations. Rolling update strategy ensures zero-downtime deployments.

5. **Smoke Test Stage**: Hit health check endpoints on each deployed service. Verify Swagger UI is accessible. Run a minimal set of API calls to confirm database connectivity and basic CRUD operations.

**Infrastructure Pipeline**: Bicep/Terraform templates deployed through a separate workflow using federated identity credentials (no secrets in repository). Targets the subscription directly for resource group, AKS cluster, PostgreSQL, ACR, and App Service Plan provisioning.

The GitHub repository (`github.com/KrishnaDistributedcomputing/K8SbasedApp`) on the master branch contains all source code, Kubernetes manifests, Dockerfiles, and architecture documentation with full commit history.

---

### 4. User Experience Transformation

Legacy EHR systems frequently depend on thick-client desktop applications installed on hospital workstations — often running Windows Forms or Java Swing interfaces that require specific JRE versions, VPN connections, and IT-managed desktop configurations. Clinicians are tied to specific physical locations. Mobile access is limited or nonexistent. Off-site or telehealth scenarios require workarounds.

The EHR Cloud Platform takes an API-first approach that decouples the user experience from the backend services entirely.

**Current Architecture**: The EHR Portal (`ehr-portal-canadacentral.azurewebsites.net`) is a Single Page Application served through nginx:alpine with a reverse proxy configuration routing API calls to the backend. The EHR API exposes all functionality through RESTful endpoints documented with Swagger/OpenAPI 3.0 at `/swagger`. This means any frontend — web, mobile, or third-party — can consume the APIs.

**Multi-Version API Strategy**: The three API versions serve different consumer needs:

- **v1** serves the existing SPA frontend and simple integration clients that need straightforward CRUD operations with minimal transformation.
- **v2** serves the enhanced portal with paginated data grids, clinical dashboards showing real-time KPIs (total patients, active providers, today's appointments, pending labs, critical alerts), and patient clinical summaries that aggregate data from multiple entities in a single call.
- **v3** serves future FHIR-compliant integrations, AI decision support interfaces, and real-time streaming dashboards that display population health trends.

**API Gateway** (target): NGINX Ingress Controller v1.12.0 currently provides external access at IP 20.175.132.82 with path-based routing. The target architecture adds Azure API Management for rate limiting, OAuth 2.0 token validation, API analytics, and subscription-based access tiers (Free/Standard/Premium).

**Interactive Documentation**: The platform ships with five interactive web applications — the Landing Portal, Architecture Design Document (12 navigable tabs with 9 Mermaid diagrams), Costing Dashboard, K8s Upgrade Planner, and API Version Catalog — all containerized and deployed alongside the core EHR services. These are not external documentation — they are living, deployed artifacts that reflect the current system state.

**Accessibility**: All services are exposed via public HTTPS endpoints through Azure Web Apps (B2 Linux plan), making them accessible from any device with a browser. No VPN, no thick client, no specific OS requirement. The CORS configuration allows any origin, enabling development of mobile and third-party applications.

---

### 5. Cost Optimization

Infrastructure cost is a critical concern for healthcare organizations evaluating cloud migration. Legacy on-premises EHR systems hide their true cost in capital expenditure budgets, data center leases, and IT staff time. Cloud deployments make costs explicit, measurable, and optimizable.

The EHR Cloud Platform implements multiple cost optimization levers documented in the Costing Dashboard (`ehr-costing-canadacentral.azurewebsites.net`).

**Current Monthly Cost Breakdown** (~$448/month):

| Resource | SKU | Monthly Cost | % of Total |
|---|---|---|---|
| AKS Cluster (3× Standard_DS2_v2) | Standard | ~$310 | 69% |
| PostgreSQL Flexible Server | Standard_B2s | ~$50 | 11% |
| App Service Plan (4 Web Apps) | B2 Linux | ~$55 | 12% |
| Azure Container Registry | Basic | ~$5 | 1% |
| NGINX Ingress (Public IP + Egress) | Standard | ~$10 | 2% |
| cert-manager, DNS, Storage | Various | ~$18 | 4% |

**Elastic Scaling**: The AKS node autoscaler adjusts VM count based on actual workload. During active development and demonstration periods, three nodes handle all workloads. During idle periods, the system can scale to minimum node counts. The worker pool (target) will scale to zero when no background events are queued.

**Right-Sizing**: Every resource uses the smallest SKU that meets POC requirements:

- PostgreSQL: Burstable B2s (not General Purpose or Memory Optimized)
- ACR: Basic (not Standard or Premium)
- App Service: B2 (not S1, P1v2, or P2v2)
- AKS Nodes: Standard_DS2_v2 (not DS3, DS4, or D-series v5)

Each has a clear upgrade path when the project graduates to production.

**Environment Lifecycle**: Operational scripts provide scale-down (reduce to minimum) and teardown (delete resource group) capabilities. The teardown command (`az group delete --name rg-aks-store-demo --yes --no-wait`) ensures no orphaned resources accumulate charges.

**Managed Services**: Azure manages patching, backups (7-day retention for PostgreSQL), high availability, and security updates. The team focuses on EHR application development rather than infrastructure maintenance — a significant hidden cost savings compared to self-managed PostgreSQL clusters or Kubernetes installations.

---

### 6. Architectural Standardization

Moving from a monolithic EHR to a standardized microservices design requires deliberate decomposition across four architectural layers, with healthcare-specific patterns applied at each level.

**Frontend Layer**: The NGINX Ingress Controller serves as the boundary between external consumers and internal services. No external client communicates directly with a backend pod. Path-based routing directs traffic: `/` serves the landing portal, `/architecture.html` serves the architecture docs, `/api/v{N}/*` routes to the EHR API. The target architecture adds Azure API Management as an additional layer for authentication, rate limiting, and API analytics.

**Backend Services Layer**: The current monolith implements all eight domain entities (Patient, Provider, Appointment, MedicalRecord, Prescription, LabResult, Allergy, Vitals) in a single ASP.NET Core 8 application. The target architecture decomposes this into six bounded context services:

1. **Patient Service** — Patient demographics, insurance, emergency contacts, allergies, vitals. Aggregate root: Patient.
2. **Clinical Service** — Medical records, encounters, diagnoses (ICD-10), treatment plans, clinical notes. Aggregate root: Encounter.
3. **Scheduling Service** — Appointments, provider availability, waitlists, reminders. Aggregate root: Appointment.
4. **Pharmacy Service** — Prescriptions, medications, drug interactions, refills. Aggregate root: Prescription.
5. **Laboratory Service** — Lab orders, results, LOINC codes, specimens, imaging orders. Aggregate root: LabOrder.
6. **Provider Service** — Clinician profiles, credentials, schedules, departments. Aggregate root: Provider.

Each service owns exactly one bounded context and exposes a focused API surface. Cross-service data access is through published APIs or consumed events — never through shared database access.

**Data Layer**: The current architecture uses a single PostgreSQL 16 database (`ehrdb`) with 8 tables and 133 seed records. Schema-per-service isolation is the target — each service will own its schema within the shared PostgreSQL instance, establishing clear data boundaries. The database migration strategy (documented in Module D) uses the Strangler Fig pattern: new services read from their own schema while a synchronization layer keeps schemas consistent during the transition period.

**Integration Layer**: Azure Event Hubs with Kafka API provides the event backbone. Eight Kafka topics (ehr.patient.events, ehr.encounter.events, ehr.appointment.events, ehr.prescription.events, ehr.lab.events, ehr.vitals.stream, ehr.audit.events, ehr.notifications.commands) carry domain events between services. Producers and consumers are completely decoupled — adding a new analytics consumer requires only a new consumer group, not changes to any publishing service.

**Five Standardization Principles**:

1. **Single Responsibility**: Each service owns one bounded context.
2. **Data Ownership**: Schema-per-service with no cross-schema writes.
3. **Contract-First Design**: OpenAPI 3.0 for synchronous APIs (v1/v2/v3), AsyncAPI for event contracts.
4. **Infrastructure-as-Code**: All resources defined in Bicep/Terraform modules.
5. **Observable-by-Default**: Health checks, structured logging, and Application Insights telemetry on every service.

---

### 7. Integration Modernization

Healthcare integration is among the most complex in any industry. Legacy EHR systems integrate through HL7 v2 messages over MLLP connections, proprietary file formats, batch FTP transfers, custom database replication, and vendor-specific APIs that change without notice. These patterns are fragile, difficult to monitor, and nearly impossible to debug when failures occur.

The EHR Cloud Platform replaces these patterns with modern, resilient, event-driven integration aligned with healthcare interoperability standards.

**API-First Integration**: Every service contract is defined in OpenAPI 3.0 with three versioned API surfaces. External systems integrate by consuming standardized REST APIs with Swagger documentation rather than parsing HL7 v2 pipe-delimited messages or proprietary XML formats. The v3 API surfaces FHIR R4-compliant Patient resources, enabling interoperability with any FHIR-capable system.

**FHIR R4 Interoperability** (v3): The API v3 preview implements HL7 FHIR R4 Patient resource endpoints that map internal EHR patients to the `us-core-patient` StructureDefinition profile. FHIR Bundle search responses enable standards-based patient lookup. The roadmap includes full FHIR resource coverage (Encounter, Observation, MedicationRequest, AllergyIntolerance, Condition) and SMART on FHIR authorization.

**Event-Driven Integration**: Domain events replace batch processing with real-time event flow:

- When a patient is registered, the Patient Service publishes `PatientRegistered` to `ehr.patient.events`. Clinical, scheduling, and audit services consume this event.
- When a critical lab result is detected, the Lab Service publishes `CriticalLabResult` to `ehr.lab.events`. The notification service dispatches an immediate alert to the ordering clinician.
- When a prescription is ordered, the Pharmacy Service publishes `PrescriptionOrdered`. The notification and audit services consume it.
- When critical vitals are recorded, `CriticalVitalsAlert` triggers immediate notification to the clinical team.

Events are categorized by criticality — Critical (CriticalVitalsAlert, CriticalLabResult), High (AllergyRegistered, EncounterCompleted, PrescriptionOrdered, LabResultReady), and Normal (PatientRegistered, AppointmentScheduled). Processing SLAs vary by criticality.

**Pub-Sub Decoupling**: Publishers do not know which consumers exist. The streaming flush service, notification service, audit service, and projection worker each subscribe independently with their own consumer groups, retry policies, and dead-letter queues. Adding a new analytics pipeline or regulatory reporting consumer requires zero changes to any publishing service.

**AI-Powered Clinical Decision Support** (v3 preview): The API surfaces drug interaction checking, clinical guideline recommendations, readmission risk prediction, and population health analytics. While currently returning simulated responses, the architecture is designed to integrate with Azure ML endpoints for real-time inference, with the API layer handling request routing, response caching, and safety disclaimers.

**Bulk Data Exchange**: The v3 API includes FHIR Bulk Data export endpoints for large-scale data extraction — Patient, Observation, and MedicationRequest resources as NDJSON files. This replaces legacy batch database dumps with a standards-compliant, asynchronous export mechanism.

**Correlation Tracking**: Every HTTP request entering through the ingress controller receives a correlation ID that propagates through all synchronous and asynchronous processing steps, enabling end-to-end tracing of a clinical transaction from the initial API call through every service and event handler.

---

### 8. Healthcare Compliance & Data Protection

Unlike most enterprise applications, healthcare systems operate under strict regulatory requirements that impose specific technical controls on data handling, access, audit, and privacy.

**HIPAA Compliance**: The Health Insurance Portability and Accountability Act requires administrative, physical, and technical safeguards for Protected Health Information (PHI). The EHR Cloud Platform implements:

- **Encryption at Rest**: PostgreSQL Flexible Server encrypts all data at rest using Azure-managed keys. All Azure storage services use AES-256 encryption by default.
- **Encryption in Transit**: All connections require TLS 1.2+. PostgreSQL requires SSL for all client connections. HTTPS is enforced on all external endpoints. Internal service-to-service communication within the AKS cluster targets mutual TLS via Istio service mesh.
- **Access Controls**: Azure Entra ID integration with OAuth 2.0 provides role-based access control. Five application roles (EHR.Admin, EHR.Clinician, EHR.Nurse, EHR.LabTech, EHR.Emergency) map to specific API scopes (patient.read/write, encounter.read, prescription.write, lab.read/write, admin.full).
- **Audit Trail**: Every data access and modification generates audit events consumed by the Audit Service. Audit records are immutable and retained according to organizational policy.
- **Minimum Necessary Access**: Each API role grants access only to the endpoints and data required for that role. A LabTech cannot access prescription data. A Nurse cannot access admin endpoints.

**Azure Canada Central**: The deployment region (Canada Central) ensures that all PHI remains within Canadian sovereignty boundaries, meeting Canadian PIPEDA requirements and provincial healthcare privacy legislation.

**Network Isolation**: Azure PostgreSQL is configured with firewall rules allowing only Azure service access (AllowAllAzure rule). The target architecture adds Private Endpoints and Virtual Network integration to eliminate public internet exposure for the database entirely.

---

## Traceability: Business Outcomes to Technical Modules

Each business outcome dimension maps directly to one or more technical modules in the EHR Cloud Platform architecture. This traceability ensures that every infrastructure component and service exists to serve a defined business purpose.

| Outcome Dimension | Primary Modules | Key Components |
|---|---|---|
| **Modernization** | Module F (Core Services), Module A (Foundation) | 6 bounded context microservices, Bicep IaC templates, API versioning (v1/v2/v3) |
| **Cloud-Native Scalability** | Module B (AKS Platform), Module C (Shared Services) | 3-pool AKS cluster, HPA, node autoscaling, Redis cache |
| **Delivery Acceleration** | Module J (CI/CD), Module C (ACR) | GitHub Actions workflows, ACR with 5 repositories, multi-stage Docker builds |
| **User Experience** | Module G (API Management), Module F (Services) | NGINX Ingress, APIM gateway, OpenAPI 3.0, 3 API versions, interactive docs |
| **Cost Optimization** | Modules A-E (Infrastructure), Module M (Cost Mgmt) | Burstable SKUs, autoscaling, lifecycle scripts, Costing Dashboard |
| **Architectural Standardization** | Module F (Services), Module D (Data), Module E (Messaging) | Schema-per-service, event backbone, contract-first design, 6 bounded contexts |
| **Integration Modernization** | Module E (Messaging), Module G (API Mgmt) | Kafka topics, FHIR R4, pub-sub, AI decision support, bulk export |
| **Healthcare Compliance** | Module I (Security), Module D (Data) | HIPAA controls, Entra ID RBAC, encryption, audit trail, Canada Central |

---

## Success Metrics

These metrics are tied to the Module L exit criteria and define when the POC is considered successful.

| Dimension | Metric | Target | Measurement |
|---|---|---|---|
| **Modernization** | Services deployed as independent containers | 11 services across 6 bounded contexts | `kubectl get deployments -n ehr` |
| **Scalability** | Pod autoscaler response time | New pods ready within 60 seconds | HPA event logs during load test |
| **Delivery** | Commit-to-deploy pipeline time | < 10 minutes | GitHub Actions workflow duration |
| **User Experience** | API version coverage | 3 simultaneous versions (v1/v2/v3) | Swagger UI version selector |
| **Cost** | Monthly POC run rate | < $500/month | Azure Cost Management |
| **Standardization** | Contract validation pass rate | 100% OpenAPI + AsyncAPI linting | CI/CD pipeline contract stage |
| **Integration** | Event end-to-end latency | < 5 seconds under normal load | Application Insights traces |
| **Compliance** | HIPAA control coverage | All technical safeguards implemented | Compliance checklist audit |
| **Resilience** | Recovery time for pod/node failures | < 60 seconds automated recovery | Module K failure scenario testing |
| **Data** | Seed data coverage | 133+ records across 8 entities | EF Core migration verification |

---

## Current State vs. Target State

| Aspect | Current State (POC) | Target State (Production) |
|---|---|---|
| Architecture | Monolith with API versioning | 11 independently deployed microservices |
| Database | Single PostgreSQL with 8 tables | 6 schema-isolated databases |
| Messaging | None (direct API calls) | 8 Kafka topics via Azure Event Hubs |
| Service Mesh | None | Istio with mutual TLS |
| Authentication | Open (no auth) | Azure Entra ID + OAuth 2.0 + 5 roles |
| API Gateway | NGINX Ingress | Azure API Management + NGINX |
| Observability | Health check endpoints | OpenTelemetry + Prometheus + Grafana |
| CI/CD | Manual ACR build + deploy | GitHub Actions with 5-stage pipeline |
| IaC | Azure CLI commands | Bicep/Terraform modules |
| Cost Visibility | Static costing dashboard | Real-time Azure Retail Prices API |
| FHIR | v3 preview (Patient only) | Full FHIR R4 resource coverage |
| AI/ML | v3 stubs (simulated) | Azure ML integration for real inference |

---

## Document Revision History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | 2026-03-22 | EHR Platform Team | Initial specification aligned with POC deployment |
