namespace EhrApi.Models.V3;

// ── FHIR R4 Aligned DTOs ──
public record FhirPatientResource(
    string ResourceType, string Id,
    FhirMeta Meta, FhirHumanName[] Name,
    string Gender, string BirthDate,
    FhirContactPoint[]? Telecom,
    FhirAddress[]? Address,
    FhirIdentifier[]? Identifier);

public record FhirMeta(string VersionId, string LastUpdated, string[] Profile);
public record FhirHumanName(string Use, string Family, string[] Given);
public record FhirContactPoint(string System, string Value, string Use);
public record FhirAddress(string Use, string[] Line, string City, string State, string PostalCode);
public record FhirIdentifier(string System, string Value);

public record FhirBundle(
    string ResourceType, string Type, int Total,
    FhirBundleLink[] Link, FhirBundleEntry[] Entry);

public record FhirBundleLink(string Relation, string Url);
public record FhirBundleEntry(string FullUrl, object Resource);

// ── AI Clinical Decision Support ──
public record ClinicalDecisionRequest(
    int PatientId, string ClinicalContext,
    string[] ActiveDiagnoses, string[] CurrentMedications);

public record ClinicalDecisionResponse(
    string RequestId, DateTime Timestamp,
    string Status, string Disclaimer,
    DrugInteractionAlert[] DrugInteractions,
    ClinicalGuideline[] Guidelines,
    RiskAssessment[] RiskScores,
    string[] SuggestedActions);

public record DrugInteractionAlert(
    string Severity, string Drug1, string Drug2,
    string Description, string Recommendation);

public record ClinicalGuideline(
    string GuidelineId, string Title, string Source,
    string Recommendation, string EvidenceLevel);

public record RiskAssessment(
    string RiskType, decimal Score, string Level,
    string[] ContributingFactors, string Methodology);

// ── Predictive Analytics ──
public record ReadmissionRiskResponse(
    int PatientId, decimal RiskScore, string RiskLevel,
    string[] TopFactors, string ModelVersion,
    DateTime PredictionDate, string Disclaimer);

public record PopulationHealthInsight(
    string Metric, string Category, decimal Value,
    string Trend, decimal ChangePercent,
    int SampleSize, string TimePeriod);

// ── Real-time Streaming ──
public record StreamSubscription(
    string SubscriptionId, string[] EventTypes,
    string CallbackUrl, string Status, DateTime CreatedAt);

// ── Bulk Operations ──
public record BulkExportRequest(string[] ResourceTypes, DateTime? Since, string OutputFormat);
public record BulkExportStatus(
    string JobId, string Status, int PercentComplete,
    DateTime StartedAt, DateTime? CompletedAt,
    BulkExportFile[]? OutputFiles);
public record BulkExportFile(string ResourceType, string Url, int Count);
