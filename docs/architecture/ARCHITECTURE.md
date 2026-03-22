# EHR System — Microservice Architecture Design

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Current State Analysis](#2-current-state-analysis)
3. [Target Architecture — Domain-Driven Microservices](#3-target-architecture--domain-driven-microservices)
4. [Bounded Contexts & Domain Model](#4-bounded-contexts--domain-model)
5. [Microservice Decomposition](#5-microservice-decomposition)
6. [Data Streaming & Event-Driven Flush Strategy](#6-data-streaming--event-driven-flush-strategy)
7. [API Management — Ingress & Egress](#7-api-management--ingress--egress)
8. [API Versioning Strategy](#8-api-versioning-strategy)
9. [Infrastructure & Deployment Architecture](#9-infrastructure--deployment-architecture)
10. [Security Architecture](#10-security-architecture)
11. [Observability & Monitoring](#11-observability--monitoring)
12. [Migration Roadmap](#12-migration-roadmap)

---

## 1. Executive Summary

This document defines the **target-state microservice architecture** for the Electronic Health Records (EHR) system, evolving from the current monolithic ASP.NET Core 8 API into a domain-driven, event-sourced, API-managed platform running on Azure Kubernetes Service (AKS).

### Key Architectural Principles

| Principle | Implementation |
|-----------|---------------|
| **Domain-Driven Design** | Bounded Contexts map to independent microservices |
| **Event Streaming** | Apache Kafka / Azure Event Hubs for real-time data flush and inter-service communication |
| **API Gateway** | Azure API Management (APIM) for ingress/egress traffic control, rate limiting, and versioning |
| **Immutable Infrastructure** | Container images versioned via SemVer, deployed to AKS via Helm charts |
| **Zero-Trust Security** | mTLS service mesh, OAuth 2.0 + FHIR-compliant authorization |

---

## 2. Current State Analysis

### 2.1 Current Monolith Architecture

```
┌─────────────────────────────────────────────────────┐
│                  NGINX Ingress Controller            │
│              (20.175.132.82:80/443)                  │
└──────────┬──────────────────────┬────────────────────┘
           │                      │
    ┌──────▼──────┐       ┌───────▼──────┐
    │ ehr-frontend│       │   ehr-api    │
    │  (nginx +   │       │ (ASP.NET 8)  │
    │  SPA HTML)  │       │  Monolith    │
    │  2 replicas │       │  2 replicas  │
    └─────────────┘       └──────┬───────┘
                                 │
                          ┌──────▼───────┐
                          │  PostgreSQL  │
                          │ (Standard_B2s│
                          │  Single DB)  │
                          └──────────────┘
```

### 2.2 Current Domain Entities (Single Bounded Context)

| Entity | Responsibility |
|--------|---------------|
| Patient | Demographics, insurance, emergency contacts |
| Provider | Clinician profiles, specialties, licensing |
| Appointment | Scheduling, status tracking |
| MedicalRecord | Encounter notes, diagnosis (ICD-10), treatment plans |
| Prescription | Medications, dosage, pharmacy routing |
| LabResult | Lab orders, results, LOINC codes, flags |
| Allergy | Allergen registry, severity tracking |
| Vitals | Blood pressure, heart rate, temperature, SpO2 |

### 2.3 Current Limitations

- **Monolithic coupling**: All 8 entities share one DbContext, one database, one deployment unit
- **No event streaming**: All data writes are synchronous, blocking, single-transaction
- **No API versioning**: Single `/api/v1` endpoint, no backward compatibility guarantees
- **No API gateway**: NGINX Ingress handles basic routing but lacks rate limiting, throttling, analytics
- **No domain isolation**: A change to Prescription schema requires redeployment of the entire API
- **Single database**: No read/write separation, no polyglot persistence

---

## 3. Target Architecture — Domain-Driven Microservices

### 3.1 High-Level Target Architecture

```
                        ┌──────────────────────────────────────────┐
                        │            External Consumers            │
                        │   (Portal, Mobile App, Partner APIs)     │
                        └────────────────┬─────────────────────────┘
                                         │ HTTPS
                        ┌────────────────▼─────────────────────────┐
                        │     Azure API Management (APIM)          │
                        │  ┌─────────────────────────────────────┐ │
                        │  │ • Rate Limiting  • OAuth Validation │ │
                        │  │ • API Versioning • Request/Response │ │
                        │  │ • Analytics      • Transformation   │ │
                        │  └─────────────────────────────────────┘ │
                        └──────────┬───────────────┬───────────────┘
                                   │ Ingress       │ Egress
                        ┌──────────▼───────────────▼───────────────┐
                        │         AKS Cluster (Istio Mesh)         │
                        │                                          │
                        │  ┌──────────┐ ┌──────────┐ ┌──────────┐ │
                        │  │ Patient  │ │ Clinical │ │Scheduling│ │
                        │  │ Service  │ │ Service  │ │ Service  │ │
                        │  │  v2.1.0  │ │  v1.3.0  │ │  v1.0.0  │ │
                        │  └────┬─────┘ └────┬─────┘ └────┬─────┘ │
                        │       │            │            │        │
                        │  ┌──────────┐ ┌──────────┐ ┌──────────┐ │
                        │  │Pharmacy  │ │   Lab    │ │ Provider │ │
                        │  │ Service  │ │ Service  │ │ Service  │ │
                        │  │  v1.2.0  │ │  v1.1.0  │ │  v1.0.0  │ │
                        │  └────┬─────┘ └────┬─────┘ └────┬─────┘ │
                        │       │            │            │        │
                        │  ┌────▼────────────▼────────────▼─────┐  │
                        │  │    Event Bus (Azure Event Hubs)    │  │
                        │  │     / Apache Kafka on AKS          │  │
                        │  └────┬────────────┬────────────┬─────┘  │
                        │       │            │            │        │
                        │  ┌────▼─────┐ ┌────▼─────┐ ┌───▼──────┐ │
                        │  │Streaming │ │Notificat-│ │  Audit   │ │
                        │  │ Flush    │ │   ion    │ │  Trail   │ │
                        │  │ Service  │ │ Service  │ │ Service  │ │
                        │  └──────────┘ └──────────┘ └──────────┘ │
                        └──────────────────────────────────────────┘
                                         │
                ┌────────────────────────┬┴──────────────────────────┐
                │                       │                           │
        ┌───────▼───────┐    ┌──────────▼──────────┐    ┌──────────▼──────┐
        │  PostgreSQL   │    │  Azure Cosmos DB    │    │  Azure Blob     │
        │  (Patients,   │    │  (Event Store,      │    │  Storage        │
        │   Providers)  │    │   Audit Logs)       │    │  (Documents,    │
        │               │    │                     │    │   Lab Reports)  │
        └───────────────┘    └─────────────────────┘    └─────────────────┘
```

---

## 4. Bounded Contexts & Domain Model

### 4.1 Strategic Domain Decomposition

Using DDD strategic patterns, the EHR system decomposes into **6 bounded contexts**:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        EHR Domain Space                            │
│                                                                     │
│  ┌─────────────────┐    ┌──────────────────┐   ┌────────────────┐  │
│  │   PATIENT        │    │   CLINICAL        │   │  SCHEDULING    │  │
│  │   MANAGEMENT     │◄──►│   ENCOUNTERS      │◄─►│                │  │
│  │                  │    │                   │   │                │  │
│  │ • Patient        │    │ • MedicalRecord   │   │ • Appointment  │  │
│  │ • Demographics   │    │ • Diagnosis       │   │ • Availability │  │
│  │ • Insurance      │    │ • TreatmentPlan   │   │ • Waitlist     │  │
│  │ • Allergy        │    │ • ClinicalNotes   │   │ • Reminder     │  │
│  │ • Vitals         │    │ • Vitals          │   │                │  │
│  └─────────────────┘    └──────────────────┘   └────────────────┘  │
│           │                      │                      │           │
│           │         Published Events (Kafka)            │           │
│           ▼                      ▼                      ▼           │
│  ┌─────────────────┐    ┌──────────────────┐   ┌────────────────┐  │
│  │   PHARMACY       │    │   LABORATORY      │   │   PROVIDER     │  │
│  │   & Rx           │    │   & DIAGNOSTICS   │   │   MANAGEMENT   │  │
│  │                  │    │                   │   │                │  │
│  │ • Prescription   │    │ • LabOrder        │   │ • Provider     │  │
│  │ • Medication     │    │ • LabResult       │   │ • Credential   │  │
│  │ • Refill         │    │ • ImagingOrder    │   │ • Schedule     │  │
│  │ • Interaction    │    │ • Specimen        │   │ • Department   │  │
│  └─────────────────┘    └──────────────────┘   └────────────────┘  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.2 Context Map — Relationships

| Upstream Context | Downstream Context | Relationship | Pattern |
|--|--|--|--|
| Patient Management | Clinical Encounters | Conformist | Patient aggregate shared via Anti-Corruption Layer |
| Patient Management | Scheduling | Customer-Supplier | Scheduling consumes patient events |
| Clinical Encounters | Pharmacy & Rx | Partnership | Prescriptions generated from encounters |
| Clinical Encounters | Laboratory | Customer-Supplier | Lab orders originate from encounters |
| Provider Management | Scheduling | Conformist | Provider availability feeds scheduling |
| Provider Management | Clinical Encounters | Shared Kernel | Provider identity shared |

### 4.3 Domain Aggregates (Tactical DDD)

#### Patient Management Aggregate

```csharp
// Aggregate Root
public class PatientAggregate
{
    public PatientId Id { get; private set; }            // Value Object
    public PersonName Name { get; private set; }          // Value Object
    public DateOfBirth Dob { get; private set; }          // Value Object
    public Gender Gender { get; private set; }            // Value Object
    public ContactInfo Contact { get; private set; }      // Value Object
    public InsuranceInfo Insurance { get; private set; }   // Value Object
    public EmergencyContact Emergency { get; private set; } // Value Object
    public BloodType BloodType { get; private set; }      // Value Object
    
    private readonly List<Allergy> _allergies = new();
    public IReadOnlyCollection<Allergy> Allergies => _allergies.AsReadOnly();
    
    private readonly List<VitalsReading> _vitals = new();
    public IReadOnlyCollection<VitalsReading> Vitals => _vitals.AsReadOnly();
    
    // Domain Events
    private readonly List<IDomainEvent> _events = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();
    
    public void RegisterAllergy(Allergen allergen, Severity severity, string reaction)
    {
        var allergy = Allergy.Create(Id, allergen, severity, reaction);
        _allergies.Add(allergy);
        _events.Add(new AllergyRegisteredEvent(Id, allergen, severity));
    }
    
    public void RecordVitals(VitalsReading reading)
    {
        reading.Validate();  // Throws if out of physiological range
        _vitals.Add(reading);
        _events.Add(new VitalsRecordedEvent(Id, reading));
        
        if (reading.IsCritical())
            _events.Add(new CriticalVitalsAlertEvent(Id, reading));
    }
}
```

#### Clinical Encounter Aggregate

```csharp
public class EncounterAggregate
{
    public EncounterId Id { get; private set; }
    public PatientId PatientId { get; private set; }
    public ProviderId ProviderId { get; private set; }
    public EncounterType Type { get; private set; }
    public EncounterStatus Status { get; private set; }
    
    private readonly List<Diagnosis> _diagnoses = new();
    private readonly List<ClinicalNote> _notes = new();
    private readonly List<PrescriptionOrder> _prescriptions = new();
    private readonly List<LabOrder> _labOrders = new();
    
    public void AddDiagnosis(IcdCode code, string description)
    {
        _diagnoses.Add(Diagnosis.Create(code, description));
        _events.Add(new DiagnosisAddedEvent(Id, PatientId, code));
    }
    
    public PrescriptionOrder PrescribeMedication(Medication med, Dosage dosage, Duration duration)
    {
        // Domain rule: check drug-allergy interactions
        var order = PrescriptionOrder.Create(Id, PatientId, ProviderId, med, dosage, duration);
        _prescriptions.Add(order);
        _events.Add(new PrescriptionOrderedEvent(Id, PatientId, order));
        return order;
    }
    
    public LabOrder OrderLab(LabTest test, Priority priority)
    {
        var order = LabOrder.Create(Id, PatientId, ProviderId, test, priority);
        _labOrders.Add(order);
        _events.Add(new LabOrderedEvent(Id, PatientId, order));
        return order;
    }
    
    public void Complete(string followUpInstructions)
    {
        Status = EncounterStatus.Completed;
        _events.Add(new EncounterCompletedEvent(Id, PatientId, _diagnoses, _prescriptions, _labOrders));
    }
}
```

---

## 5. Microservice Decomposition

### 5.1 Service Catalog

| Service | Bounded Context | Tech Stack | Database | Port |
|---------|----------------|------------|----------|------|
| `patient-service` | Patient Management | .NET 8 Web API | PostgreSQL (dedicated) | 8081 |
| `clinical-service` | Clinical Encounters | .NET 8 Web API | PostgreSQL (dedicated) | 8082 |
| `scheduling-service` | Scheduling | .NET 8 Web API | PostgreSQL (dedicated) | 8083 |
| `pharmacy-service` | Pharmacy & Rx | .NET 8 Web API | PostgreSQL (dedicated) | 8084 |
| `lab-service` | Laboratory | .NET 8 Web API | PostgreSQL (dedicated) | 8085 |
| `provider-service` | Provider Management | .NET 8 Web API | PostgreSQL (dedicated) | 8086 |
| `streaming-service` | Cross-cutting | .NET 8 Worker | Kafka / Event Hubs | — |
| `notification-service` | Cross-cutting | .NET 8 Worker | Redis + SendGrid | — |
| `audit-service` | Cross-cutting | .NET 8 Worker | Cosmos DB | — |
| `ehr-portal` | Frontend | nginx + SPA | — | 8080 |
| `ehr-bff` | Backend-for-Frontend | .NET 8 Web API | Redis (cache) | 8090 |

### 5.2 Target Repository Structure

```
K8SbasedApp/
├── docs/
│   └── architecture/
│       └── ARCHITECTURE.md                 ← This document
├── src/
│   ├── services/
│   │   ├── patient-service/
│   │   │   ├── src/
│   │   │   │   ├── PatientService.Api/     (.NET 8 Web API)
│   │   │   │   │   ├── Controllers/
│   │   │   │   │   ├── Domain/
│   │   │   │   │   │   ├── Aggregates/
│   │   │   │   │   │   ├── ValueObjects/
│   │   │   │   │   │   ├── Events/
│   │   │   │   │   │   └── Repositories/
│   │   │   │   │   ├── Application/
│   │   │   │   │   │   ├── Commands/
│   │   │   │   │   │   ├── Queries/
│   │   │   │   │   │   └── Handlers/
│   │   │   │   │   ├── Infrastructure/
│   │   │   │   │   │   ├── Persistence/
│   │   │   │   │   │   ├── Messaging/
│   │   │   │   │   │   └── ExternalApis/
│   │   │   │   │   └── Dockerfile
│   │   │   │   └── PatientService.Tests/
│   │   │   └── helm/
│   │   │       └── patient-service/
│   │   ├── clinical-service/               (same structure)
│   │   ├── scheduling-service/
│   │   ├── pharmacy-service/
│   │   ├── lab-service/
│   │   ├── provider-service/
│   │   ├── streaming-service/
│   │   ├── notification-service/
│   │   └── audit-service/
│   ├── frontends/
│   │   ├── ehr-portal/
│   │   └── ehr-bff/
│   ├── shared/
│   │   ├── EHR.SharedKernel/              (Domain events, base classes)
│   │   ├── EHR.Contracts/                 (API contracts, DTOs)
│   │   └── EHR.Messaging/                 (Kafka producers/consumers)
│   └── infrastructure/
│       ├── helm/
│       │   ├── ehr-platform/              (Umbrella chart)
│       │   └── values-{env}.yaml
│       ├── terraform/
│       │   ├── modules/
│       │   │   ├── aks/
│       │   │   ├── postgres/
│       │   │   ├── eventhubs/
│       │   │   ├── apim/
│       │   │   └── acr/
│       │   ├── environments/
│       │   │   ├── dev/
│       │   │   ├── staging/
│       │   │   └── production/
│       │   └── main.tf
│       └── k8s/
│           ├── namespaces.yaml
│           ├── network-policies.yaml
│           └── istio/
├── .github/
│   └── workflows/
│       ├── ci-patient-service.yaml
│       ├── ci-clinical-service.yaml
│       └── deploy-platform.yaml
└── README.md
```

### 5.3 Internal Service Architecture (Clean Architecture per Service)

```
┌─────────────────────────────────────────────────────┐
│                   API Layer                          │
│   Controllers / Minimal APIs / gRPC Endpoints       │
│   Request validation, DTO mapping, versioning       │
├─────────────────────────────────────────────────────┤
│                Application Layer                     │
│   CQRS: Commands (MediatR) + Queries (Dapper)       │
│   Application services, validators, mappers         │
├─────────────────────────────────────────────────────┤
│                 Domain Layer                         │
│   Aggregates, Entities, Value Objects               │
│   Domain Events, Domain Services                    │
│   Repository interfaces (ports)                      │
├─────────────────────────────────────────────────────┤
│              Infrastructure Layer                    │
│   EF Core (write), Dapper (read)                    │
│   Kafka Producer/Consumer                            │
│   External API clients                               │
│   Repository implementations (adapters)              │
└─────────────────────────────────────────────────────┘
```

---

## 6. Data Streaming & Event-Driven Flush Strategy

### 6.1 Event Streaming Architecture

The system uses **Apache Kafka on AKS** (via Strimzi operator) or **Azure Event Hubs** (Kafka-compatible API) for inter-service communication and data flush.

```
                    Producers                          Consumers
              ┌──────────────────┐              ┌──────────────────┐
              │  patient-service │──┐       ┌──►│ streaming-flush  │
              └──────────────────┘  │       │   └──────────────────┘
              ┌──────────────────┐  │       │   ┌──────────────────┐
              │ clinical-service │──┤       ├──►│ notification-svc │
              └──────────────────┘  │       │   └──────────────────┘
              ┌──────────────────┐  │       │   ┌──────────────────┐
              │scheduling-service│──┼──────►├──►│  audit-service   │
              └──────────────────┘  │ Kafka │   └──────────────────┘
              ┌──────────────────┐  │Topics │   ┌──────────────────┐
              │ pharmacy-service │──┤       ├──►│   lab-service    │
              └──────────────────┘  │       │   └──────────────────┘
              ┌──────────────────┐  │       │   ┌──────────────────┐
              │   lab-service    │──┘       └──►│ clinical-service │
              └──────────────────┘              └──────────────────┘
```

### 6.2 Kafka Topic Design

| Topic | Partitions | Retention | Key | Producers | Consumers |
|-------|-----------|-----------|-----|-----------|-----------|
| `ehr.patient.events` | 12 | 7d | `patientId` | patient-service | clinical, scheduling, pharmacy, audit |
| `ehr.encounter.events` | 12 | 7d | `encounterId` | clinical-service | pharmacy, lab, notification, audit |
| `ehr.appointment.events` | 6 | 3d | `appointmentId` | scheduling-service | notification, audit |
| `ehr.prescription.events` | 6 | 7d | `prescriptionId` | pharmacy-service | notification, audit |
| `ehr.lab.events` | 6 | 7d | `labOrderId` | lab-service | clinical, notification, audit |
| `ehr.vitals.stream` | 12 | 1d | `patientId` | patient-service | streaming-flush, clinical |
| `ehr.audit.events` | 6 | 30d | `entityId` | all services | audit-service |
| `ehr.notifications.commands` | 3 | 1d | `recipientId` | all services | notification-service |

### 6.3 Streaming Flush Pattern

The **Streaming Flush Service** implements real-time data materialization using Kafka Streams:

```
┌─────────────────────────────────────────────────────────────────┐
│                     Streaming Flush Service                      │
│                                                                  │
│  ┌──────────────┐    ┌─────────────────┐    ┌────────────────┐  │
│  │   Kafka       │    │  Stream          │    │  Materialized  │  │
│  │   Consumer    │───►│  Processor       │───►│  View Writer   │  │
│  │   (Source)    │    │  (Transform +    │    │  (Sink)        │  │
│  │              │    │   Aggregate)     │    │               │  │
│  └──────────────┘    └─────────────────┘    └───────┬────────┘  │
│                                                      │          │
│                              ┌────────────────────────┘          │
│                              ▼                                   │
│                    ┌──────────────────┐                          │
│                    │  Read-Optimized  │                          │
│                    │  Stores          │                          │
│                    │  • Redis Cache   │                          │
│                    │  • Cosmos DB     │                          │
│                    │  • Search Index  │                          │
│                    └──────────────────┘                          │
└─────────────────────────────────────────────────────────────────┘
```

#### 6.3.1 Flush Strategies

| Strategy | Use Case | Implementation |
|----------|----------|----------------|
| **Immediate Flush** | Critical vitals alerts, lab critical results | Event → Direct write → Notification push |
| **Micro-Batch Flush** | Vitals stream (non-critical) | 5-second tumbling window → Batch insert to read store |
| **Windowed Aggregation** | Dashboard statistics, patient summaries | 1-minute sliding window → Materialize aggregate counts |
| **Change Data Capture** | Cross-service data sync | Debezium CDC → Kafka → Target service DB |

#### 6.3.2 Streaming Flush Implementation

```csharp
// Streaming Flush Worker — processes vitals in micro-batches
public class VitalsFlushProcessor : BackgroundService
{
    private readonly IConsumer<string, VitalsRecordedEvent> _consumer;
    private readonly IRedisCache _cache;
    private readonly ICosmosRepository _cosmos;
    
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _consumer.Subscribe("ehr.vitals.stream");
        var buffer = new List<VitalsRecordedEvent>();
        var flushInterval = TimeSpan.FromSeconds(5);
        var lastFlush = DateTime.UtcNow;

        while (!ct.IsCancellationRequested)
        {
            var result = _consumer.Consume(TimeSpan.FromMilliseconds(100));
            
            if (result != null)
            {
                var evt = result.Message.Value;
                buffer.Add(evt);
                
                // Immediate flush for critical vitals
                if (evt.IsCritical)
                {
                    await FlushCriticalAsync(evt);
                    continue;
                }
            }
            
            // Micro-batch flush every 5 seconds
            if (DateTime.UtcNow - lastFlush >= flushInterval && buffer.Any())
            {
                await FlushBatchAsync(buffer);
                buffer.Clear();
                lastFlush = DateTime.UtcNow;
                _consumer.Commit();
            }
        }
    }
    
    private async Task FlushBatchAsync(List<VitalsRecordedEvent> batch)
    {
        // 1. Write to time-series read store (Cosmos DB)
        await _cosmos.BulkInsertVitalsAsync(batch);
        
        // 2. Update real-time dashboard cache (Redis)
        var grouped = batch.GroupBy(v => v.PatientId);
        foreach (var group in grouped)
        {
            var latest = group.OrderByDescending(v => v.RecordedAt).First();
            await _cache.SetAsync($"vitals:latest:{group.Key}", latest, TimeSpan.FromHours(1));
        }
        
        // 3. Update aggregate statistics
        await _cache.IncrementAsync("stats:vitals:total", batch.Count);
    }
    
    private async Task FlushCriticalAsync(VitalsRecordedEvent evt)
    {
        // Immediate write + alert
        await _cosmos.InsertVitalsAsync(evt);
        await _cache.SetAsync($"vitals:latest:{evt.PatientId}", evt);
        await _cache.PublishAsync("alerts:critical-vitals", evt);
    }
}
```

### 6.4 Event Schema (CloudEvents Envelope)

```json
{
    "specversion": "1.0",
    "id": "evt-20260322-a1b2c3d4",
    "source": "/ehr/patient-service",
    "type": "ehr.patient.vitals.recorded",
    "datacontenttype": "application/json",
    "time": "2026-03-22T18:30:00Z",
    "subject": "patient/PAT-001",
    "data": {
        "patientId": "PAT-001",
        "vitals": {
            "temperature": 101.2,
            "heartRate": 110,
            "systolicBp": 90,
            "diastolicBp": 60,
            "spO2": 92,
            "respiratoryRate": 22
        },
        "isCritical": true,
        "recordedBy": "PRV-003",
        "recordedAt": "2026-03-22T18:30:00Z"
    }
}
```

### 6.5 CQRS Read/Write Separation

```
         Commands (Write)                    Queries (Read)
              │                                    │
              ▼                                    ▼
    ┌──────────────────┐              ┌──────────────────┐
    │   Command API    │              │   Query API      │
    │   (POST/PUT/DEL) │              │   (GET)          │
    └────────┬─────────┘              └────────┬─────────┘
             │                                 │
             ▼                                 ▼
    ┌──────────────────┐              ┌──────────────────┐
    │  Command Handler │              │  Query Handler   │
    │  (MediatR)       │              │  (Dapper)        │
    └────────┬─────────┘              └────────┬─────────┘
             │                                 │
             ▼                                 ▼
    ┌──────────────────┐              ┌──────────────────┐
    │  Write DB        │───Kafka───►  │  Read Store      │
    │  (PostgreSQL)    │   CDC        │  (Redis/Cosmos)  │
    │  Normalized      │              │  Denormalized    │
    └──────────────────┘              └──────────────────┘
```

---

## 7. API Management — Ingress & Egress

### 7.1 Azure API Management (APIM) Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Azure API Management                             │
│                                                                     │
│  INGRESS POLICIES          │          EGRESS POLICIES               │
│  ─────────────────         │          ──────────────────            │
│  • JWT Validation          │          • Response Caching            │
│  • Rate Limiting           │          • Response Transformation     │
│  • IP Filtering            │          • CORS Headers                │
│  • Request Validation      │          • Content Negotiation         │
│  • Quota Enforcement       │          • Error Masking               │
│  • API Key Validation      │          • FHIR Compliance Headers     │
│  • OAuth 2.0 Scopes        │          • Compression (gzip)          │
│  • Throttling              │          • Pagination Headers          │
│  • Correlation ID Inject   │          • ETag Generation             │
│                            │                                        │
└────────────┬───────────────┴─────────────────┬──────────────────────┘
      Inbound│                                 │Outbound
             ▼                                 ▲
┌────────────────────────────────────────────────────────────────────┐
│                    AKS Internal Gateway                            │
│                  (Istio Ingress Gateway)                           │
│                                                                    │
│   /api/v1/patients/*  ──► patient-service:8081                    │
│   /api/v1/encounters/* ──► clinical-service:8082                  │
│   /api/v1/appointments/* ──► scheduling-service:8083              │
│   /api/v1/prescriptions/* ──► pharmacy-service:8084               │
│   /api/v1/labs/*        ──► lab-service:8085                      │
│   /api/v1/providers/*   ──► provider-service:8086                 │
│   /api/v2/patients/*    ──► patient-service-v2:8081               │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

### 7.2 Ingress Policies (Inbound Processing)

```xml
<!-- APIM Inbound Policy -->
<inbound>
    <!-- 1. Correlation ID for distributed tracing -->
    <set-header name="X-Correlation-Id" exists-action="skip">
        <value>@(context.RequestId.ToString())</value>
    </set-header>
    
    <!-- 2. JWT Token Validation (Azure AD / Entra ID) -->
    <validate-jwt header-name="Authorization" require-scheme="Bearer">
        <openid-config url="https://login.microsoftonline.com/{tenant}/.well-known/openid-configuration" />
        <required-claims>
            <claim name="aud" match="any">
                <value>api://ehr-platform</value>
            </claim>
            <claim name="roles" match="any">
                <value>EHR.Read</value>
                <value>EHR.Write</value>
                <value>EHR.Admin</value>
            </claim>
        </required-claims>
    </validate-jwt>
    
    <!-- 3. Rate Limiting: 1000 req/min per subscription -->
    <rate-limit-by-key 
        calls="1000" renewal-period="60" counter-key="@(context.Subscription.Id)" 
        increment-condition="@(context.Response.StatusCode >= 200)" />
    
    <!-- 4. Quota: 50,000 calls/day for Standard tier -->
    <quota-by-key calls="50000" renewal-period="86400" counter-key="@(context.Subscription.Id)" />
    
    <!-- 5. Request size limit (HIPAA: prevent data exfiltration) -->
    <set-header name="X-Request-Timestamp" exists-action="override">
        <value>@(DateTime.UtcNow.ToString("o"))</value>
    </set-header>
</inbound>
```

### 7.3 Egress Policies (Outbound Processing)

```xml
<!-- APIM Outbound Policy -->
<outbound>
    <!-- 1. Remove internal headers -->
    <set-header name="X-Powered-By" exists-action="delete" />
    <set-header name="Server" exists-action="delete" />
    
    <!-- 2. Add security headers -->
    <set-header name="X-Content-Type-Options" exists-action="override">
        <value>nosniff</value>
    </set-header>
    <set-header name="Strict-Transport-Security" exists-action="override">
        <value>max-age=31536000; includeSubDomains</value>
    </set-header>
    
    <!-- 3. FHIR compliance headers -->
    <set-header name="X-FHIR-Version" exists-action="override">
        <value>R4</value>
    </set-header>
    
    <!-- 4. Response caching for GET requests -->
    <cache-store duration="300" />
    
    <!-- 5. Mask sensitive fields in error responses -->
    <choose>
        <when condition="@(context.Response.StatusCode >= 500)">
            <set-body>@{
                return new JObject(
                    new JProperty("error", "Internal Server Error"),
                    new JProperty("correlationId", context.Variables["correlationId"]),
                    new JProperty("timestamp", DateTime.UtcNow)
                ).ToString();
            }</set-body>
        </when>
    </choose>
</outbound>
```

### 7.4 API Products & Subscription Tiers

| Product | Rate Limit | Quota/Day | Audience | Access |
|---------|-----------|-----------|----------|--------|
| **EHR Internal** | 5,000/min | Unlimited | Internal portal, mobile app | OAuth 2.0 (Entra ID) |
| **EHR Partner** | 1,000/min | 100,000 | Partner hospital systems | API Key + OAuth |
| **EHR Public** | 100/min | 10,000 | Public health registries | API Key |
| **EHR Emergency** | Unlimited | Unlimited | Emergency room systems | Mutual TLS |

---

## 8. API Versioning Strategy

### 8.1 Versioning Approach — URL Path + Header Hybrid

```
Primary:   /api/v{major}/resource       (URL path versioning)
Secondary: Accept: application/vnd.ehr.v2+json  (Media type versioning)
Fallback:  X-API-Version: 2.1.0         (Custom header)
```

### 8.2 Version Lifecycle

```
     ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
     │  Preview │───►│  Current │───►│Deprecated│───►│  Sunset  │
     │  (Beta)  │    │   (GA)   │    │ (6 months│    │ (Removed)│
     │          │    │          │    │  notice) │    │          │
     └──────────┘    └──────────┘    └──────────┘    └──────────┘
      v3-preview       v2 (GA)         v1 (Dep)       v0 (Gone)
```

| Phase | Duration | Support Level |
|-------|----------|---------------|
| **Preview** | Variable | No SLA, breaking changes allowed |
| **Current (GA)** | Until next GA | Full SLA, no breaking changes |
| **Deprecated** | 6 months minimum | Security patches only, sunset header added |
| **Sunset** | Post-deprecation | 410 Gone, redirect documentation |

### 8.3 Version Routing in APIM

```xml
<!-- APIM routing for versioned APIs -->
<inbound>
    <set-variable name="apiVersion" value="@{
        // Priority 1: URL path version
        var path = context.Request.Url.Path;
        var match = System.Text.RegularExpressions.Regex.Match(path, @"/api/v(\d+)/");
        if (match.Success) return match.Groups[1].Value;
        
        // Priority 2: Accept header
        var accept = context.Request.Headers.GetValueOrDefault("Accept", "");
        var mediaMatch = System.Text.RegularExpressions.Regex.Match(accept, @"vnd\.ehr\.v(\d+)");
        if (mediaMatch.Success) return mediaMatch.Groups[1].Value;
        
        // Priority 3: X-API-Version header
        return context.Request.Headers.GetValueOrDefault("X-API-Version", "2");
    }" />
    
    <!-- Route to correct backend based on version -->
    <choose>
        <when condition="@(context.Variables.GetValueOrDefault<string>("apiVersion") == "1")">
            <set-backend-service base-url="http://patient-service-v1.ehr.svc.cluster.local" />
            <set-header name="Sunset" exists-action="override">
                <value>Sat, 01 Sep 2026 00:00:00 GMT</value>
            </set-header>
            <set-header name="Deprecation" exists-action="override">
                <value>true</value>
            </set-header>
        </when>
        <when condition="@(context.Variables.GetValueOrDefault<string>("apiVersion") == "2")">
            <set-backend-service base-url="http://patient-service-v2.ehr.svc.cluster.local" />
        </when>
    </choose>
</inbound>
```

### 8.4 Semantic Versioning Convention

```
{service}-v{MAJOR}.{MINOR}.{PATCH}

Examples:
  patient-service-v2.1.0    # Minor feature addition
  patient-service-v2.1.1    # Patch/bugfix
  patient-service-v3.0.0    # Breaking change → new APIM route
```

| Change Type | Version Bump | APIM Impact |
|-------------|-------------|-------------|
| New optional field in response | PATCH | None |
| New endpoint added | MINOR | New operation registered |
| Field renamed/removed | **MAJOR** | New version route created |
| Request schema changed | **MAJOR** | New version route created |
| New query parameter | MINOR | None |

### 8.5 Container Image Tagging Strategy

```
ehracrcanada.azurecr.io/{service}:{semver}
ehracrcanada.azurecr.io/{service}:{semver}-{git-sha}
ehracrcanada.azurecr.io/{service}:latest         # dev only, never production

Examples:
  ehracrcanada.azurecr.io/patient-service:2.1.0
  ehracrcanada.azurecr.io/patient-service:2.1.0-a3f4b5c
  ehracrcanada.azurecr.io/patient-service:2.1.1-rc.1     # Release candidate
```

---

## 9. Infrastructure & Deployment Architecture

### 9.1 Azure Resource Topology

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Azure Subscription                               │
│                  (Jan 2026 Subscription)                                │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │               Resource Group: rg-ehr-platform                   │    │
│  │                                                                 │    │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │    │
│  │  │    AKS       │  │    APIM     │  │     Azure Event Hubs   │ │    │
│  │  │  Cluster     │  │  Gateway    │  │  (Kafka-compat.)       │ │    │
│  │  │  3-5 nodes   │  │  Standard  │  │  Standard tier         │ │    │
│  │  │  D4s_v5      │  │  tier      │  │  12 partitions/topic   │ │    │
│  │  └──────┬───────┘  └─────┬──────┘  └──────────┬──────────────┘ │    │
│  │         │                │                     │                │    │
│  │  ┌──────┴────────────────┴─────────────────────┴──────────────┐ │    │
│  │  │                    Virtual Network                         │ │    │
│  │  │                  (10.0.0.0/16)                             │ │    │
│  │  │                                                            │ │    │
│  │  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐     │ │    │
│  │  │  │ AKS Pods │ │ Postgres │ │  Redis   │ │ Cosmos   │     │ │    │
│  │  │  │ Subnet   │ │ Subnet   │ │ Subnet   │ │ Endpoint │     │ │    │
│  │  │  │10.0.1/24 │ │10.0.2/24 │ │10.0.3/24 │ │10.0.4/24 │     │ │    │
│  │  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘     │ │    │
│  │  └────────────────────────────────────────────────────────────┘ │    │
│  │                                                                 │    │
│  │  ┌─────────────────────────┐  ┌────────────────────────────┐   │    │
│  │  │  PostgreSQL Flexible    │  │  Azure Container Registry  │   │    │
│  │  │  Servers (per service)  │  │  (ehracrcanada)            │   │    │
│  │  │  • patient-db (B2s)     │  │  Premium tier              │   │    │
│  │  │  • clinical-db (B2s)    │  │  Geo-replication           │   │    │
│  │  │  • scheduling-db (B1ms) │  │                            │   │    │
│  │  │  • pharmacy-db (B1ms)   │  │                            │   │    │
│  │  │  • lab-db (B2s)         │  │                            │   │    │
│  │  │  • provider-db (B1ms)   │  │                            │   │    │
│  │  └─────────────────────────┘  └────────────────────────────┘   │    │
│  └─────────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

### 9.2 AKS Namespace Strategy

| Namespace | Services | Purpose |
|-----------|----------|---------|
| `ehr-core` | patient, provider | Core identity services |
| `ehr-clinical` | clinical, pharmacy, lab | Clinical workflow services |
| `ehr-scheduling` | scheduling, notification | Appointment & notification |
| `ehr-platform` | streaming, audit, bff | Platform/cross-cutting |
| `ehr-frontend` | portal | Frontend SPA |
| `ehr-infra` | kafka, redis | Infrastructure components |
| `istio-system` | istio control plane | Service mesh |
| `cert-manager` | cert-manager | TLS certificate management |

### 9.3 Helm Chart Values (Production)

```yaml
# values-production.yaml
global:
  image:
    registry: ehracrcanada.azurecr.io
    pullPolicy: IfNotPresent
  environment: production
  kafka:
    bootstrapServers: "ehr-eventhubs.servicebus.windows.net:9093"
    securityProtocol: SASL_SSL
  
patientService:
  replicaCount: 3
  image:
    repository: patient-service
    tag: "2.1.0"
  resources:
    requests:
      cpu: 250m
      memory: 256Mi
    limits:
      cpu: 1000m
      memory: 1Gi
  autoscaling:
    enabled: true
    minReplicas: 3
    maxReplicas: 10
    targetCPUUtilization: 70
  database:
    host: patient-db.postgres.database.azure.com
    name: patientdb
    sslMode: Require

clinicalService:
  replicaCount: 3
  image:
    repository: clinical-service
    tag: "1.3.0"
  resources:
    requests:
      cpu: 500m
      memory: 512Mi
    limits:
      cpu: 2000m
      memory: 2Gi
  autoscaling:
    enabled: true
    minReplicas: 3
    maxReplicas: 15
    targetCPUUtilization: 65

streamingService:
  replicaCount: 3
  image:
    repository: streaming-service
    tag: "1.0.0"
  kafka:
    consumerGroup: "streaming-flush"
    topics:
      - "ehr.vitals.stream"
      - "ehr.encounter.events"
      - "ehr.lab.events"
```

---

## 10. Security Architecture

### 10.1 Zero-Trust Network Architecture

```
                     Internet
                        │
                  ┌─────▼──────┐
                  │ Azure WAF  │  (DDoS protection, OWASP rules)
                  └─────┬──────┘
                        │
                  ┌─────▼──────┐
                  │   APIM     │  (JWT validation, rate limiting)
                  └─────┬──────┘
                        │ Private Link
                  ┌─────▼──────┐
                  │ Istio      │  (mTLS between all pods)
                  │ Gateway    │
                  └─────┬──────┘
                        │ mTLS
              ┌─────────┼─────────┐
              ▼         ▼         ▼
         ┌────────┐ ┌────────┐ ┌────────┐
         │Service │ │Service │ │Service │  (AuthZ via OPA policies)
         │   A    │ │   B    │ │   C    │
         └───┬────┘ └───┬────┘ └───┬────┘
             │          │          │ Private Endpoints
         ┌───▼──────────▼──────────▼───┐
         │  Azure Private DNS Zone     │
         │  (*.postgres.database.azure │
         │   .com, *.redis.cache...    │
         └─────────────────────────────┘
```

### 10.2 HIPAA Compliance Controls

| Control | Implementation |
|---------|---------------|
| **Encryption at rest** | Azure Storage/DB TDE, AES-256 |
| **Encryption in transit** | TLS 1.3 (APIM → AKS), mTLS (pod-to-pod via Istio) |
| **Access control** | RBAC (Azure AD), ABAC (OPA/Gatekeeper) |
| **Audit logging** | All API calls logged to audit-service → immutable Cosmos DB |
| **PHI data masking** | APIM outbound policies mask SSN, DOB in non-clinical responses |
| **Key management** | Azure Key Vault, Managed Identities (no passwords in env vars) |
| **Network isolation** | NSG + Kubernetes Network Policies + Istio Authorization Policies |

---

## 11. Observability & Monitoring

### 11.1 Observability Stack

```
┌──────────────────────────────────────────────────────────────────┐
│                    Observability Platform                         │
│                                                                  │
│  ┌────────────────┐  ┌─────────────────┐  ┌──────────────────┐  │
│  │   Metrics       │  │    Traces        │  │     Logs         │  │
│  │                │  │                 │  │                  │  │
│  │  Prometheus +  │  │  OpenTelemetry  │  │  Azure Monitor   │  │
│  │  Azure Monitor │  │  → Jaeger /     │  │  + Fluent Bit    │  │
│  │  Metrics       │  │  App Insights   │  │  → Log Analytics │  │
│  └───────┬────────┘  └───────┬─────────┘  └───────┬──────────┘  │
│          │                   │                     │             │
│          └───────────────────┼─────────────────────┘             │
│                              │                                   │
│                    ┌─────────▼──────────┐                        │
│                    │   Grafana          │                        │
│                    │   Dashboards       │                        │
│                    │                    │                        │
│                    │  • Service Health  │                        │
│                    │  • API Latency     │                        │
│                    │  • Kafka Lag       │                        │
│                    │  • DB Connections  │                        │
│                    │  • Error Rates     │                        │
│                    └────────────────────┘                        │
└──────────────────────────────────────────────────────────────────┘
```

### 11.2 Key Metrics per Service

| Metric | Target | Alert Threshold |
|--------|--------|----------------|
| P99 Latency | < 200ms | > 500ms for 5 min |
| Error Rate (5xx) | < 0.1% | > 1% for 2 min |
| Kafka Consumer Lag | < 1000 | > 10,000 for 5 min |
| Pod CPU | < 70% | > 85% for 10 min |
| DB Connection Pool | < 80% | > 90% for 5 min |
| API Request Rate | Baseline ±30% | Anomaly detection |

---

## 12. Migration Roadmap

### 12.1 Strangler Fig Pattern — Incremental Migration

```
Phase 1 (Current → v1.x)        Phase 2 (v2.0)              Phase 3 (v3.0)
┌─────────────────┐          ┌─────────────────┐         ┌─────────────────┐
│   Monolith      │          │  Monolith       │         │                 │
│   (all 8        │          │  (remaining     │         │    Fully        │
│    entities)    │          │   3 entities)   │         │    Decomposed   │
│                 │          │                 │         │    Microservices│
│   ┌───────────┐ │          │  ┌───────────┐  │         │                 │
│   │ Patient   │ │──extract─►  │ Extracted │  │         │  6 independent  │
│   │ Provider  │ │          │  │ Patient + │  │         │  services +     │
│   │ extracted │ │          │  │ Clinical +│  │         │  event bus +    │
│   └───────────┘ │          │  │ Scheduling│  │         │  APIM           │
└─────────────────┘          └─────────────────┘         └─────────────────┘
     + Kafka                      + APIM                      + Full CQRS
     + Event Hubs                 + gRPC internal              + Streaming
       bootstrapped                 communication                 Flush
```

### 12.2 Phase Timeline

| Phase | Milestone | Key Deliverables | Duration |
|-------|-----------|-----------------|----------|
| **1.0** | Foundation | Kafka/Event Hubs setup, shared libraries, APIM deployment, CI/CD pipelines | 4 weeks |
| **1.1** | Patient Extraction | `patient-service` + `provider-service` extracted, API v1 preserved | 3 weeks |
| **1.2** | Event Wiring | Domain events published from extracted services, audit-service consuming | 2 weeks |
| **2.0** | Clinical Extraction | `clinical-service` + `pharmacy-service` + `lab-service` extracted | 4 weeks |
| **2.1** | Scheduling | `scheduling-service` extracted, notification-service added | 3 weeks |
| **2.2** | Streaming | `streaming-flush-service` operational, CQRS read stores populated | 3 weeks |
| **3.0** | Full Platform | Monolith decommissioned, full APIM routing, Istio mesh, Grafana dashboards | 3 weeks |

### 12.3 Database Migration Strategy

```
Current:  1 PostgreSQL (ehrdb) ─── All 8 tables
                │
Phase 1:  ├── patient-db ─── patients, allergies, vitals
          └── ehrdb (remaining) ─── appointments, records, prescriptions, labs, providers
                │
Phase 2:  ├── patient-db
          ├── clinical-db ─── medical_records
          ├── pharmacy-db ─── prescriptions
          ├── lab-db ─── lab_results
          └── ehrdb (shrinking) ─── appointments, providers
                │
Phase 3:  ├── patient-db
          ├── clinical-db
          ├── scheduling-db ─── appointments
          ├── pharmacy-db
          ├── lab-db
          └── provider-db ─── providers
```

Each phase uses **dual-write + CDC** to maintain consistency during migration.

---

## Appendix A: Technology Stack Summary

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **API Gateway** | Azure API Management (Standard v2) | Ingress/egress, versioning, rate limiting |
| **Service Mesh** | Istio 1.22+ | mTLS, traffic management, observability |
| **Container Orchestration** | AKS (Kubernetes 1.33) | Workload scheduling, auto-scaling |
| **Application Runtime** | .NET 8 (ASP.NET Core) | Microservice framework |
| **Event Streaming** | Azure Event Hubs (Kafka API) | Async messaging, event sourcing |
| **Primary Database** | Azure PostgreSQL Flexible Server | Per-service relational storage |
| **Cache** | Azure Cache for Redis | Session, query cache, real-time dashboards |
| **Document Store** | Azure Cosmos DB | Audit logs, event store, materialized views |
| **Object Storage** | Azure Blob Storage | Lab reports, medical documents, images |
| **Container Registry** | Azure Container Registry (Premium) | Image storage, vulnerability scanning |
| **CI/CD** | GitHub Actions | Build, test, deploy pipelines |
| **IaC** | Terraform | Infrastructure provisioning |
| **Package Management** | Helm 3 | Kubernetes deployment charts |
| **Observability** | OpenTelemetry + Grafana + Azure Monitor | Metrics, traces, logs |
| **Secret Management** | Azure Key Vault + Managed Identity | Secrets, certificates, keys |

## Appendix B: Domain Event Catalog

| Event | Source | Consumers | Criticality |
|-------|--------|-----------|-------------|
| `PatientRegistered` | patient-service | clinical, scheduling, audit | Normal |
| `PatientUpdated` | patient-service | clinical, pharmacy, audit | Normal |
| `AllergyRegistered` | patient-service | pharmacy (drug interaction check), audit | High |
| `CriticalVitalsAlert` | patient-service | notification, clinical, audit | **Critical** |
| `VitalsRecorded` | patient-service | streaming-flush, audit | Normal |
| `EncounterStarted` | clinical-service | scheduling (status update), audit | Normal |
| `EncounterCompleted` | clinical-service | pharmacy, lab, notification, audit | High |
| `DiagnosisAdded` | clinical-service | audit | Normal |
| `PrescriptionOrdered` | pharmacy-service | notification, audit | High |
| `PrescriptionDispensed` | pharmacy-service | notification, audit | Normal |
| `LabOrdered` | lab-service | notification, audit | Normal |
| `LabResultReady` | lab-service | clinical, notification, audit | High |
| `CriticalLabResult` | lab-service | clinical, notification, audit | **Critical** |
| `AppointmentScheduled` | scheduling-service | notification, audit | Normal |
| `AppointmentCancelled` | scheduling-service | notification, provider, audit | Normal |
| `AppointmentNoShow` | scheduling-service | notification, audit | Normal |
