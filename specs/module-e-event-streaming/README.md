# Module E – Event Streaming & Asynchronous Messaging

## Objective

Module E defines the event-driven architecture that enables asynchronous communication between EHR microservices. Azure Event Hubs with Kafka API provides the event backbone. Services publish domain events when significant state changes occur, and downstream consumers process these events independently — enabling loose coupling, eventual consistency, and real-time clinical alerting.

---

## Event Hub Configuration (Target)

| Property | Value |
|---|---|
| **Namespace** | `ehub-ehr-canadacentral` |
| **SKU** | Standard (20 consumer groups, 1 TU) |
| **Protocol** | Kafka API (SASL/SSL on port 9093) |
| **Retention** | 1–7 days depending on topic |
| **Partitions** | 3–12 depending on topic |
| **Region** | Canada Central |

---

## Kafka Topic Design

| Topic | Partitions | Retention | Partition Key | Producers | Consumers |
|---|---|---|---|---|---|
| `ehr.patient.events` | 12 | 7d | patientId | patient-service | clinical, scheduling, pharmacy, audit |
| `ehr.encounter.events` | 12 | 7d | encounterId | clinical-service | pharmacy, lab, notification, audit |
| `ehr.appointment.events` | 6 | 3d | appointmentId | scheduling-service | notification, audit |
| `ehr.prescription.events` | 6 | 7d | prescriptionId | pharmacy-service | notification, audit |
| `ehr.lab.events` | 6 | 7d | labOrderId | lab-service | clinical, notification, audit |
| `ehr.vitals.stream` | 12 | 1d | patientId | patient-service | streaming-flush, clinical |
| `ehr.audit.events` | 6 | 30d | entityId | all services | audit-service |
| `ehr.notifications.commands` | 3 | 1d | recipientId | all services | notification-service |

**Total**: 8 topics, 63 partitions

---

## Domain Event Catalog

### Critical Events (SLA: < 1 second processing)

| Event | Producer | Consumers | Trigger |
|---|---|---|---|
| `CriticalVitalsAlert` | patient-service | notification, clinical, audit | Vitals outside critical thresholds |
| `CriticalLabResult` | lab-service | clinical, notification, audit | Lab result flagged as Critical |

### High-Priority Events (SLA: < 5 seconds processing)

| Event | Producer | Consumers | Trigger |
|---|---|---|---|
| `AllergyRegistered` | patient-service | pharmacy (interaction check), audit | New allergy recorded |
| `EncounterCompleted` | clinical-service | pharmacy, lab, notification | Encounter marked complete |
| `PrescriptionOrdered` | pharmacy-service | notification, audit | New prescription written |
| `LabResultReady` | lab-service | clinical, notification | Lab result completed |

### Normal Events (SLA: < 30 seconds processing)

| Event | Producer | Consumers | Trigger |
|---|---|---|---|
| `PatientRegistered` | patient-service | clinical, scheduling, audit | New patient created |
| `PatientUpdated` | patient-service | clinical, scheduling | Demographics changed |
| `AppointmentScheduled` | scheduling-service | notification, audit | Appointment booked |
| `AppointmentCompleted` | scheduling-service | clinical, audit | Appointment finished |

---

## CloudEvents Schema

All domain events follow the CloudEvents v1.0 specification:

```json
{
  "specversion": "1.0",
  "id": "evt-a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "type": "ehr.patient.PatientRegistered",
  "source": "/ehr/patient-service",
  "time": "2026-03-22T14:30:00Z",
  "datacontenttype": "application/json",
  "subject": "patient/15",
  "correlationid": "corr-x1y2z3",
  "data": {
    "patientId": 15,
    "firstName": "Sarah",
    "lastName": "Johnson",
    "dateOfBirth": "1985-06-15",
    "gender": "Female",
    "insuranceId": "INS-2026-0015"
  }
}
```

### Required CloudEvents Headers

| Field | Type | Description |
|---|---|---|
| `specversion` | string | Always `"1.0"` |
| `id` | string | Unique event ID (UUID) |
| `type` | string | `ehr.{context}.{EventName}` |
| `source` | string | `/ehr/{service-name}` |
| `time` | datetime | ISO 8601 UTC timestamp |
| `subject` | string | `{entity}/{id}` |
| `correlationid` | string | Request correlation ID for end-to-end tracing |

---

## CQRS Pattern

The architecture implements Command Query Responsibility Segregation to optimize read and write paths independently.

### Write Path (Commands)

```
Client → API Gateway → Service Controller → MediatR Command Handler
  → EF Core DbContext → PostgreSQL (write)
  → Event Publisher → Kafka Topic
```

- Commands use MediatR pipeline with FluentValidation
- Write operations use EF Core with explicit transactions
- Events published after successful commit (outbox pattern target)

### Read Path (Queries)

```
Client → API Gateway → Service Controller → Dapper Query Handler
  → PostgreSQL (read replica target) / Redis Cache
```

- Queries bypass EF Core and use Dapper for raw SQL performance
- Results cached in Redis with TTL-based invalidation
- v2 API endpoints use projection queries that return only DTO fields

---

## Streaming Flush Strategies

### Immediate Flush (Critical)

For critical vitals alerts and critical lab results:

```
Event → Consumer → Direct Write to DB → Push Notification → Alert Dashboard
```

Processing target: < 1 second. No batching, no buffering.

### Micro-Batch Flush (Standard)

For routine vitals, appointment events, and prescription events:

```
Events → 100ms window or 50-event batch → Bulk Write to DB → Update Cache
```

### Debounced Flush (Analytics)

For audit events and reporting aggregation:

```
Events → 5-second window → Aggregate → Write Summary to Reporting DB
```

---

## Dead Letter Queue Handling

Messages that fail processing after all retry attempts are moved to the dead-letter queue:

| Subscription | Max Retries | Lock Duration | DLQ TTL | Action |
|---|---|---|---|---|
| notification | 5 | 60s | 7 days | Alert operations team |
| audit | 5 | 60s | 30 days | Manual reprocessing required |
| projection | 3 | 30s | 3 days | Rebuild projection from source |
| streaming-flush | 3 | 30s | 1 day | Data reconciliation check |

### DLQ Monitor (Target)

A lightweight Kubernetes CronJob that checks DLQ depth every 5 minutes and fires alerts to Microsoft Teams or PagerDuty if messages accumulate.

---

## Event-Driven Integration Patterns

### Saga Pattern (Target — Order Fulfillment)

For multi-step clinical workflows that span services:

```
1. Encounter Created (clinical-service)
2. → Lab Order Placed (lab-service subscribes)
3. → Prescription Ordered (pharmacy-service subscribes)
4. → Notification Sent (notification-service subscribes)
5. All completed → Encounter Fulfilled
```

Compensation logic handles partial failures — if the lab order fails, the encounter is flagged for manual review rather than silently lost.

### Event Sourcing (Target — Audit Trail)

The Audit Service consumes all domain events and maintains an append-only event store. Any entity's complete history can be reconstructed by replaying its events.

---

## Dependencies

- **Upstream**: Module A (Event Hubs namespace provisioning)
- **Downstream**: Module F (producers), Module G (workers consume events), Module H (event metrics)
