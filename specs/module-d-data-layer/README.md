# Module D – Data Layer & Database Strategy

## Objective

Module D defines the data persistence strategy for the EHR Cloud Platform — the PostgreSQL Flexible Server configuration, schema-per-service isolation plan, Entity Framework Core data model, migration strategy, seed data design, and the Strangler Fig pattern for decomposing the monolithic database into per-service schemas.

---

## PostgreSQL Flexible Server

### Current Configuration

| Property | Value |
|---|---|
| **Server Name** | `ehrdb-canadacentral` |
| **FQDN** | `ehrdb-canadacentral.postgres.database.azure.com` |
| **Version** | PostgreSQL 16 |
| **SKU** | Standard_B2s (Burstable) |
| **vCPU / RAM** | 2 vCPU / 4 GB |
| **Storage** | 32 GB |
| **Backup Retention** | 7 days |
| **SSL Mode** | Required |
| **Admin User** | `ehradmin` |
| **Database** | `ehrdb` |
| **Firewall** | AllowAllAzure (0.0.0.0) |

### Connection String Pattern

```
Host=ehrdb-canadacentral.postgres.database.azure.com;
Database=ehrdb;
Username=ehradmin;
Password=<from-keyvault>;
SSL Mode=Require;
Trust Server Certificate=true;
```

---

## Entity Framework Core Data Model

### DbContext Configuration

The `EhrDbContext` defines 8 DbSets covering the complete EHR domain:

```csharp
public class EhrDbContext : DbContext
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<Vitals> Vitals => Set<Vitals>();
}
```

### Entity Relationship Model

```
Patient (1) ──── (N) Appointment ──── (1) Provider
Patient (1) ──── (N) MedicalRecord ── (1) Provider
Patient (1) ──── (N) Prescription ─── (1) Provider
Patient (1) ──── (N) LabResult
Patient (1) ──── (N) Allergy
Patient (1) ──── (N) Vitals
Provider (1) ──── (N) Appointment
Provider (1) ──── (N) MedicalRecord
Provider (1) ──── (N) Prescription
```

### Indexes

| Entity | Index Columns | Type | Purpose |
|---|---|---|---|
| Patient | LastName, FirstName | Composite | Name search queries |
| Patient | InsuranceId | Single | Insurance lookup |
| Appointment | ScheduledAt | Single | Date-range queries |

### Entity Specifications

| Entity | Key Fields | Relationships | Records |
|---|---|---|---|
| **Patient** | FirstName, LastName, DOB, Gender, Phone, Email, Address, InsuranceId, BloodType, EmergencyContact | → Appointments, MedicalRecords, Prescriptions, LabResults, Allergies, Vitals | 15 |
| **Provider** | FirstName, LastName, Specialty, LicenseNumber, Department, IsActive | → Appointments, MedicalRecords, Prescriptions | 8 |
| **Appointment** | PatientId, ProviderId, ScheduledAt, Duration, Status, Type, Reason, Notes | ← Patient, Provider | 20 |
| **MedicalRecord** | PatientId, ProviderId, EncounterDate, EncounterType, Diagnosis, DiagnosisCode (ICD-10), TreatmentPlan, ClinicalNotes | ← Patient, Provider | 15 |
| **Prescription** | PatientId, ProviderId, MedicationName, Dosage, Frequency, Route, StartDate, EndDate, Refills, Status, Pharmacy | ← Patient, Provider | 18 |
| **LabResult** | PatientId, OrderedByProviderId, TestName, TestCode (LOINC), Result, Unit, ReferenceRange, Status, Flag | ← Patient | 20 |
| **Allergy** | PatientId, Allergen, AllergyType, Severity, Reaction, IsActive | ← Patient | 12 |
| **Vitals** | PatientId, RecordedAt, Temperature, HeartRate, SystolicBp, DiastolicBp, RespiratoryRate, OxygenSaturation, Weight, Height | ← Patient | 25 |

**Total seed records**: 133

---

## Schema-per-Service Decomposition Plan

### Target Schema Layout

The single `ehrdb` database will be decomposed into 6 schemas aligned with bounded contexts:

| Schema | Owner Service | Tables | Source Tables |
|---|---|---|---|
| `patient` | Patient Service | patients, allergies, vitals, emergency_contacts | Patients, Allergies, Vitals |
| `clinical` | Clinical Service | encounters, medical_records, diagnoses, treatment_plans | MedicalRecords |
| `scheduling` | Scheduling Service | appointments, availability, waitlist, reminders | Appointments |
| `pharmacy` | Pharmacy Service | prescriptions, medications, drug_interactions, refills | Prescriptions |
| `laboratory` | Laboratory Service | lab_orders, lab_results, specimens, imaging_orders | LabResults |
| `provider` | Provider Service | providers, credentials, schedules, departments | Providers |

### Decomposition Strategy: Strangler Fig Pattern

The migration from monolith to per-service schemas follows four phases:

**Phase 1 — Schema Creation** (current → target):
- Create 6 PostgreSQL schemas within the existing `ehrdb` database
- Copy table structures and data from public schema to domain schemas
- Maintain the original public schema tables as read-only copies

**Phase 2 — Dual-Write**:
- Modify the monolithic API to write to both old and new schemas
- New microservices read from their own schema
- Consistency verified through reconciliation queries

**Phase 3 — Cutover**:
- New microservices become the primary write path
- Monolithic API becomes read-only for migrated entities
- Cross-service data access redirected to API calls

**Phase 4 — Cleanup**:
- Remove monolithic API endpoints for migrated entities
- Drop public schema copies
- Optional: Extract schemas to independent PostgreSQL instances

### Data Ownership Rules

1. **Each service writes only to its own schema** — no cross-schema INSERT/UPDATE/DELETE.
2. **Cross-service reads go through APIs** — never through direct SQL joins across schemas.
3. **Denormalized read models** are acceptable — services can maintain their own copies of data received through events.
4. **The Reporting Service** is the exception — it maintains a read-optimized denormalized view aggregated from events published by all services.

---

## Migration Strategy

### EF Core Code-First Migrations

Migrations are generated and applied automatically on startup:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();
    db.Database.Migrate();
}
```

### Current Migration History

| Migration | Date | Changes |
|---|---|---|
| `InitialCreate` | 2026-03-22 | Initial 8 tables with relationships |
| `ExpandedSeedData` | 2026-03-22 | 133 seed records across all entities |

### Target: Per-Service Migration

Each microservice will own its own EF Core DbContext and migration history:

```
src/services/
├── patient-service/
│   └── Infrastructure/Persistence/
│       ├── PatientDbContext.cs
│       └── Migrations/
├── clinical-service/
│   └── Infrastructure/Persistence/
│       ├── ClinicalDbContext.cs
│       └── Migrations/
└── ...
```

---

## Backup & Recovery

| Policy | Value |
|---|---|
| **Automatic Backup** | Enabled (Azure-managed) |
| **Retention** | 7 days (POC) → 35 days (production) |
| **Geo-Redundant Backup** | Disabled (POC) → Enabled (production) |
| **Point-in-Time Restore** | Available to any point within retention window |
| **RPO** | Last backup (up to 24h for POC) |
| **RTO** | < 1 hour via point-in-time restore |

---

## Performance Optimization

### Connection Pooling

The ASP.NET Core application uses Npgsql's built-in connection pooling:

```json
{
  "ConnectionStrings": {
    "EhrDb": "Host=ehrdb-canadacentral.postgres.database.azure.com;Database=ehrdb;Username=ehradmin;Password=<pwd>;SSL Mode=Require;Pooling=true;Minimum Pool Size=2;Maximum Pool Size=20"
  }
}
```

### Query Optimization (Target)

- **v1 API**: Direct EF Core queries with `.Include()` for navigation properties
- **v2 API**: Projection queries using `.Select()` to return only DTO fields — reduces data transfer and memory allocation
- **Read-heavy endpoints**: Target Dapper for raw SQL performance on reporting and dashboard queries

---

## Dependencies

- **Upstream**: Module A (PostgreSQL resource provisioning)
- **Downstream**: Module F (all services depend on data layer), Module E (events carry entity change data)
