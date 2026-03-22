# Module I – Security & HIPAA Compliance

## Objective

Module I defines the security architecture for an EHR platform handling Protected Health Information (PHI). Every layer — network, identity, data, application, container — is secured following Zero Trust principles and HIPAA Technical Safeguards (45 CFR 164.312).

---

## HIPAA Technical Safeguards Mapping

| Safeguard | Requirement | Implementation |
|---|---|---|
| Access Control (164.312(a)) | Unique user identification | Entra ID + OAuth 2.0 JWT tokens |
| Access Control (164.312(a)) | Emergency access procedure | Break-glass admin account in Key Vault |
| Access Control (164.312(a)) | Automatic logoff | Token expiry: 60 min access, 8 hr refresh |
| Access Control (164.312(a)) | Encryption and decryption | AES-256 at rest, TLS 1.3 in transit |
| Audit Controls (164.312(b)) | Record and examine activity | Structured audit logs, immutable storage |
| Integrity (164.312(c)) | PHI integrity mechanism | Database checksums, event signing |
| Person Authentication (164.312(d)) | Verify identity | MFA via Entra ID Conditional Access |
| Transmission Security (164.312(e)) | Encryption in transit | TLS 1.3 enforced, no plaintext endpoints |

---

## Identity & Access Management

### Microsoft Entra ID Configuration

| Setting | Value |
|---|---|
| Tenant | Single-tenant (organization only) |
| App Registration | ehr-cloud-platform |
| Redirect URIs | https://ehr-portal-canadacentral.azurewebsites.net/callback |
| Token Version | v2.0 |
| ID Token | Enabled (implicit flow for SPA) |
| Access Token | Enabled |

### Application Roles (appRoles)

| Role | Value | Description | Allowed Members |
|---|---|---|---|
| System Administrator | Admin | Full system access, user management | IT Staff |
| Physician | Physician | Full clinical read/write, prescribe | Licensed MDs |
| Nurse | Nurse | Clinical read, vitals write, triage | RNs, LPNs |
| Lab Technician | LabTech | Lab results read/write only | Lab staff |
| Front Desk | FrontDesk | Patient demographics, scheduling only | Administrative |
| Auditor | Auditor | Read-only access to all records + audit logs | Compliance |
| Patient | Patient | Own records read-only (patient portal) | Patients |

### JWT Token Claims

```json
{
  "iss": "https://login.microsoftonline.com/{tenant}/v2.0",
  "sub": "user-object-id",
  "aud": "api://ehr-cloud-platform",
  "roles": ["Physician"],
  "name": "Dr. Sarah Chen",
  "preferred_username": "schen@hospital.ca",
  "oid": "guid",
  "tid": "tenant-guid",
  "iat": 1711100000,
  "exp": 1711103600,
  "nonce": "random"
}
```

### Role-Based Access Matrix

| Resource | Admin | Physician | Nurse | LabTech | FrontDesk | Auditor | Patient |
|---|---|---|---|---|---|---|---|
| Patient Demographics | CRUD | Read | Read | — | CRUD | Read | Own |
| Medical Records | CRUD | CRUD | Read | — | — | Read | Own |
| Prescriptions | CRUD | CRUD | Read | — | — | Read | Own |
| Lab Results | CRUD | Read | Read | CRUD | — | Read | Own |
| Allergies | CRUD | CRUD | CRUD | — | — | Read | Own |
| Vitals | CRUD | CRUD | CRUD | — | — | Read | Own |
| Appointments | CRUD | Read | Read | — | CRUD | Read | Own |
| Audit Logs | Read | — | — | — | — | Read | — |
| User Management | CRUD | — | — | — | — | Read | — |

---

## Network Security

### AKS Network Policies

```yaml
# Deny all ingress by default
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: default-deny-ingress
  namespace: ehr
spec:
  podSelector: {}
  policyTypes:
    - Ingress

---
# Allow ingress only from NGINX controller
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: allow-ingress-from-nginx
  namespace: ehr
spec:
  podSelector:
    matchLabels:
      app: ehr-api
  ingress:
    - from:
        - namespaceSelector:
            matchLabels:
              kubernetes.io/metadata.name: ingress-nginx
      ports:
        - protocol: TCP
          port: 8080
```

### TLS Configuration

| Endpoint | TLS Version | Certificate Source |
|---|---|---|
| Web Apps (*.azurewebsites.net) | TLS 1.2+ | Azure-managed |
| AKS Ingress (20.175.132.82) | TLS 1.2+ | cert-manager (self-signed → Let's Encrypt) |
| PostgreSQL | TLS 1.2+ | Azure-managed, SSL enforced |
| ACR | TLS 1.2+ | Azure-managed |

---

## Data Protection

### Encryption at Rest

| Data Store | Encryption | Key Management |
|---|---|---|
| PostgreSQL | AES-256 (TDE) | Azure-managed keys (service-managed) |
| Azure Blob (backups) | AES-256 | Azure-managed keys |
| Key Vault secrets | AES-256 | HSM-backed (Azure-managed) |
| AKS etcd | AES-256 | Azure-managed keys |

### Encryption in Transit

All service-to-service communication uses TLS. Database connection strings require `Ssl Mode=Require`.

### Sensitive Data Handling

| Data Element | Classification | Storage | Logging | Display |
|---|---|---|---|---|
| Patient Name | PHI | Encrypted at rest | Never logged | Full to authorized roles |
| Date of Birth | PHI | Encrypted at rest | Never logged | Full to authorized roles |
| SSN/Health ID | PHI + PII | Not stored (if possible) | Never logged | Masked (***-**-1234) |
| Diagnosis Codes | PHI | Encrypted at rest | Code only (no description) | Full to clinical roles |
| Connection Strings | Secret | Key Vault only | Never logged | Never displayed |
| JWT Tokens | Credential | Memory only | Never logged | Never displayed |

---

## Key Vault Integration

### Current Secrets

| Secret Name | Purpose | Rotation |
|---|---|---|
| ehrdb-connection-string | PostgreSQL connection | 90 days |
| acr-admin-password | ACR authentication | 90 days |
| jwt-signing-key | Token validation | 180 days |

### CSI Secret Store Driver

```yaml
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: ehr-secrets
  namespace: ehr
spec:
  provider: azure
  parameters:
    usePodIdentity: "false"
    useVMManagedIdentity: "true"
    userAssignedIdentityID: "<managed-identity-client-id>"
    keyvaultName: "kv-ehr-canadacentral"
    objects: |
      array:
        - |
          objectName: ehrdb-connection-string
          objectType: secret
    tenantId: "<tenant-id>"
```

---

## Container Security

| Control | Implementation |
|---|---|
| Base Images | Official .NET 8 aspnet:8.0-alpine, nginx:alpine |
| Non-root User | `USER app` in Dockerfile (default in .NET 8 images) |
| Read-only Filesystem | `readOnlyRootFilesystem: true` in securityContext |
| No Privilege Escalation | `allowPrivilegeEscalation: false` |
| Drop All Capabilities | `drop: ["ALL"]` |
| Image Scanning | ACR vulnerability scanning (Defender for Containers) |
| Signed Images | Content trust via Notary v2 (target state) |

### Pod Security Context

```yaml
securityContext:
  runAsNonRoot: true
  runAsUser: 1654
  fsGroup: 1654
  seccompProfile:
    type: RuntimeDefault
containers:
  - name: ehr-api
    securityContext:
      allowPrivilegeEscalation: false
      readOnlyRootFilesystem: true
      capabilities:
        drop: ["ALL"]
```

---

## Audit Logging

### Audit Events Captured

| Event | Data Captured | Retention |
|---|---|---|
| User Login | UserId, IP, Timestamp, Success/Fail | 1 year |
| Patient Record Access | UserId, PatientId, Action, Timestamp | 6 years (HIPAA) |
| Record Modification | UserId, PatientId, Field, OldValue hash, NewValue hash | 6 years |
| Prescription Created | UserId, PatientId, DrugId, Timestamp | 6 years |
| Admin Action | UserId, Action, Target, Timestamp | 1 year |
| Failed Access Attempt | UserId, Resource, Reason, IP | 1 year |

### Audit Log Immutability

- Audit logs are written to a separate database table with no UPDATE/DELETE permissions
- Append-only storage with Azure Blob immutability policies (target state)
- Log Analytics Workspace for centralized query with KQL

---

## Dependencies

- **Upstream**: Module A (Key Vault provisioned), Module B (AKS network policies), Module D (database encryption)
- **Downstream**: Module J (CI/CD integrates security scanning), Module L (security acceptance criteria)
