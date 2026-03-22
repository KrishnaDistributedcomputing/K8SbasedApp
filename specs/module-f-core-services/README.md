# Module F – Core Microservices & Domain Model

## Objective

Module F defines the six bounded context microservices that implement the EHR business logic. Each service owns a single domain, exposes a versioned REST API, and communicates with other services through events. This module covers the domain decomposition strategy, aggregate design, service specifications, and the migration path from the current monolith to independent services.

---

## Strategic Domain Decomposition — 6 Bounded Contexts

### 1. Patient Management

**Owner**: Patient Service
**Description**: Core identity, demographics, insurance, clinical indicators
**Aggregate Root**: Patient

| Entity | Description |
|---|---|
| Patient | Core demographics — name, DOB, gender, contact, address |
| Demographics | Extended patient data — ethnicity, language, marital status |
| Insurance | Insurance carrier, policy ID, group, effective dates |
| Allergy | Allergen, type (Drug/Food/Environmental), severity, reaction |
| Vitals | Temperature, HR, BP, RR, SpO2, weight, height, timestamp |
| EmergencyContact | Name, phone, relationship |

**Domain Events Published**: `PatientRegistered`, `PatientUpdated`, `AllergyRegistered`, `CriticalVitalsAlert`
**Current v1 Endpoints**: `/api/v1/patients`, `/api/v1/allergies`, `/api/v1/vitals`
**V2 Endpoints**: `/api/v2/patients` (paginated), `/api/v2/patients/{id}/clinical-summary`

---

### 2. Clinical Encounters

**Owner**: Clinical Service
**Description**: Medical encounters, diagnoses, treatment plans, clinical notes
**Aggregate Root**: Encounter

| Entity | Description |
|---|---|
| Encounter | Visit context — date, type, chief complaint, status |
| MedicalRecord | Clinical documentation tied to an encounter |
| Diagnosis | ICD-10 coded diagnosis with primary/secondary classification |
| TreatmentPlan | Ordered treatments, procedures, follow-up instructions |
| ClinicalNotes | Free-text provider notes, assessments, progress notes |

**Domain Events Published**: `EncounterCompleted`, `DiagnosisRecorded`
**Current v1 Endpoints**: `/api/v1/medicalrecords`
**V2 Endpoints**: `/api/v2/medicalrecords` (paginated with encounterType and diagnosisCode filters)

---

### 3. Scheduling

**Owner**: Scheduling Service
**Description**: Appointment booking, provider availability, waitlists, reminders
**Aggregate Root**: Appointment

| Entity | Description |
|---|---|
| Appointment | Patient-provider scheduling — time, duration, status, type, reason |
| Availability | Provider time slots — day, start/end, recurrence rules |
| Waitlist | Patient waitlist entry — preferred provider, preferred times, priority |
| Reminder | Appointment reminders — SMS/email, scheduled send time |

**Domain Events Published**: `AppointmentScheduled`, `AppointmentCompleted`, `AppointmentCancelled`
**Current v1 Endpoints**: `/api/v1/appointments` (filters: patientId, providerId, status, date), PATCH status
**V2 Endpoints**: `/api/v2/appointments` (paginated), `/api/v2/appointments/today`

---

### 4. Pharmacy & Rx

**Owner**: Pharmacy Service
**Description**: Prescriptions, medications, drug interactions, refills
**Aggregate Root**: Prescription

| Entity | Description |
|---|---|
| Prescription | Medication order — drug, dosage, frequency, route, duration |
| Medication | Drug catalog — name, class, contraindications, formulary status |
| Refill | Refill request — count, pharmacy, status, filled date |
| DrugInteraction | Known interactions between medications — severity, recommendation |

**Domain Events Published**: `PrescriptionOrdered`, `PrescriptionDispensed`, `DrugInteractionDetected`
**Current v1 Endpoints**: `/api/v1/prescriptions` (filters: patientId, status), PATCH status
**V2 Endpoints**: `/api/v2/prescriptions` (paginated with medication name search)
**V3 Endpoints**: `/api/v3/ai/drug-interactions` (AI-powered screening)

---

### 5. Laboratory & Diagnostics

**Owner**: Laboratory Service
**Description**: Lab orders, results, LOINC codes, imaging, specimens
**Aggregate Root**: LabOrder

| Entity | Description |
|---|---|
| LabOrder | Test order — ordering provider, priority, status |
| LabResult | Test result — value, unit, reference range, flag (Normal/High/Low/Critical) |
| ImagingOrder | Radiology/imaging orders — modality, body part, indication |
| Specimen | Specimen tracking — type, collection time, status |

**Domain Events Published**: `LabResultReady`, `CriticalLabResult`, `ImagingCompleted`
**Current v1 Endpoints**: `/api/v1/labresults` (filters: patientId, status), PATCH result update
**V2 Endpoints**: `/api/v2/labresults` (paginated with flag and testName filters)

---

### 6. Provider Management

**Owner**: Provider Service
**Description**: Clinician profiles, credentials, schedules, departments
**Aggregate Root**: Provider

| Entity | Description |
|---|---|
| Provider | Clinician profile — name, specialty, license, department, active status |
| Credential | Professional certifications, board certifications, DEA number |
| Schedule | Provider weekly schedule template — available hours, break times |
| Department | Organizational unit — name, location, head provider |

**Domain Events Published**: `ProviderActivated`, `ProviderDeactivated`, `ScheduleUpdated`
**Current v1 Endpoints**: `/api/v1/providers` (filter: specialty)
**V2 Endpoints**: `/api/v2/providers` (paginated with workload), `/api/v2/providers/{id}/schedule`

---

## Clean Architecture per Service

Each microservice follows a four-layer architecture:

### API Layer
Controllers, Minimal APIs, gRPC endpoints. Request validation, DTO mapping, API versioning.

### Application Layer
CQRS: Commands via MediatR, Queries via Dapper. Application services, validators, mappers.

### Domain Layer
Aggregates, Entities, Value Objects. Domain Events, Domain Services, Repository Ports.

### Infrastructure Layer
EF Core for writes, Dapper for reads. Kafka producer/consumer. External API clients.

```
src/services/{service-name}/
├── src/{ServiceName}.Api/
│   ├── Controllers/V1/
│   ├── Controllers/V2/
│   ├── Controllers/V3/
│   ├── Domain/
│   │   ├── Aggregates/
│   │   ├── ValueObjects/
│   │   └── Events/
│   ├── Application/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── Handlers/
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   └── Messaging/
│   └── Dockerfile
├── tests/
│   ├── UnitTests/
│   └── IntegrationTests/
└── helm/{service-name}/
    ├── Chart.yaml
    ├── values.yaml
    └── templates/
```

---

## Platform Services (Non-Domain)

In addition to the six bounded context services, five platform services handle cross-cutting concerns:

| Service | Purpose | Type |
|---|---|---|
| **API Gateway** | YARP/NGINX reverse proxy, routing, correlation ID injection | Ingress |
| **Streaming Flush Service** | Consumes vitals.stream topic, batches and persists to DB | Worker |
| **Notification Service** | Dispatches email/SMS alerts for clinical events | Worker |
| **Audit Service** | Maintains immutable event trail from all domain events | Worker |
| **Projection Worker** | Builds denormalized read models for reporting/dashboard | Worker |

**Total target services**: 11 (6 domain + 5 platform)

---

## API Version Support Matrix

| Service | v1 (Stable) | v2 (Enhanced) | v3 (Preview) |
|---|---|---|---|
| Patient Service | Full CRUD | Paginated DTOs, Clinical Summary | FHIR R4 Patient |
| Clinical Service | Full CRUD | Paginated with diagnosis filters | — |
| Scheduling Service | Full CRUD + PATCH status | Paginated, today's appointments | — |
| Pharmacy Service | Full CRUD + PATCH status | Paginated, medication search | AI drug interactions |
| Lab Service | Full CRUD + PATCH result | Paginated, flag/test filters | — |
| Provider Service | Full CRUD | Workload metrics, schedule endpoint | — |
| Dashboard | — | Clinical KPI dashboard | Population health |
| AI/ML | — | — | Readmission risk, guidelines |
| FHIR | — | — | Patient resource, bulk export |
| Streaming | — | — | Event subscriptions, event types |

---

## Service Communication Matrix

| From ↓ / To → | Patient | Clinical | Scheduling | Pharmacy | Lab | Provider |
|---|---|---|---|---|---|---|
| **Patient** | — | Event | Event | Event | — | — |
| **Clinical** | API | — | API | Event | Event | API |
| **Scheduling** | API | — | — | — | — | API |
| **Pharmacy** | API | — | — | — | — | API |
| **Lab** | API | Event | — | — | — | API |
| **Provider** | — | — | — | — | — | — |

- **API**: Synchronous REST call (service-to-service within cluster)
- **Event**: Asynchronous via Kafka topic

---

## Current Monolith → Microservice Migration Plan

### Phase 1 (Complete): Monolith with API Versioning
- Single ASP.NET Core 8 application with 8 controllers
- Three API versions (v1/v2/v3) via Asp.Versioning.Mvc
- Single PostgreSQL database with 8 tables

### Phase 2: Extract Patient Service
- First bounded context to extract (highest domain isolation)
- Owns patients, allergies, vitals tables
- Publishes PatientRegistered, CriticalVitalsAlert events
- Monolith continues serving remaining 5 domains

### Phase 3: Extract Clinical + Scheduling
- Clinical Service owns medical_records table
- Scheduling Service owns appointments table
- Both consume patient events for local read models

### Phase 4: Extract Pharmacy + Lab + Provider
- Complete decomposition of remaining domains
- All services own their schemas
- Monolith retired

---

## Dependencies

- **Upstream**: Module B (AKS cluster), Module C (ACR, Key Vault), Module D (PostgreSQL), Module E (Event Hubs)
- **Downstream**: Module G (API Management routes to services), Module H (telemetry from services)
