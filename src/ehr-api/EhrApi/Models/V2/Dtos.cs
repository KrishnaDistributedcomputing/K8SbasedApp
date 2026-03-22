using System.ComponentModel.DataAnnotations;

namespace EhrApi.Models.V2;

// ── Patient DTOs ──
public record PatientListDto(
    int Id, string FirstName, string LastName, DateTime DateOfBirth,
    string Gender, string? Phone, string? Email, string? InsuranceId,
    int AllergyCount, int ActivePrescriptions, DateTime CreatedAt);

public record PatientDetailDto(
    int Id, string FirstName, string LastName, DateTime DateOfBirth,
    string Gender, string? Phone, string? Email, string? Address,
    string? InsuranceId, string? BloodType,
    string? EmergencyContactName, string? EmergencyContactPhone,
    DateTime CreatedAt, DateTime UpdatedAt,
    IEnumerable<AllergyDto> Allergies,
    IEnumerable<VitalsDto> RecentVitals,
    PatientStatsDto Stats);

public record PatientStatsDto(
    int TotalAppointments, int TotalRecords, int ActivePrescriptions,
    int PendingLabResults, DateTime? LastVisit);

public record CreatePatientDto(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required] DateTime DateOfBirth,
    [Required, MaxLength(10)] string Gender,
    [MaxLength(20)] string? Phone, [MaxLength(200)] string? Email,
    [MaxLength(500)] string? Address, [MaxLength(50)] string? InsuranceId,
    [MaxLength(20)] string? BloodType,
    [MaxLength(100)] string? EmergencyContactName,
    [MaxLength(20)] string? EmergencyContactPhone);

// ── Provider DTOs ──
public record ProviderListDto(
    int Id, string FirstName, string LastName, string Specialty,
    string? Department, bool IsActive, int UpcomingAppointments);

public record ProviderDetailDto(
    int Id, string FirstName, string LastName, string Specialty,
    string? LicenseNumber, string? Phone, string? Email,
    string? Department, bool IsActive, DateTime CreatedAt,
    ProviderWorkloadDto Workload);

public record ProviderWorkloadDto(
    int TodayAppointments, int WeekAppointments,
    int ActivePatients, int PendingPrescriptions);

// ── Appointment DTOs ──
public record AppointmentListDto(
    int Id, int PatientId, string PatientName,
    int ProviderId, string ProviderName,
    DateTime ScheduledAt, int DurationMinutes,
    string Status, string? Type, string? Reason);

public record AppointmentDetailDto(
    int Id, int PatientId, string PatientName,
    int ProviderId, string ProviderName,
    DateTime ScheduledAt, int DurationMinutes,
    string Status, string? Type, string? Reason, string? Notes,
    DateTime CreatedAt, DateTime UpdatedAt);

// ── Medical Record DTOs ──
public record MedicalRecordListDto(
    int Id, int PatientId, string PatientName,
    int ProviderId, string ProviderName,
    DateTime EncounterDate, string EncounterType,
    string? DiagnosisCode, string? ChiefComplaint);

// ── Clinical Summary ──
public record PatientClinicalSummary(
    PatientDetailDto Patient,
    IEnumerable<AppointmentListDto> RecentAppointments,
    IEnumerable<MedicalRecordListDto> RecentRecords,
    IEnumerable<PrescriptionDto> ActivePrescriptions,
    IEnumerable<LabResultDto> PendingLabs);

public record PrescriptionDto(
    int Id, string MedicationName, string Dosage, string Frequency,
    string? Route, DateTime StartDate, DateTime? EndDate,
    int? Refills, string Status, string? Pharmacy);

public record LabResultDto(
    int Id, string TestName, string? TestCode, string? Result,
    string? Unit, string? ReferenceRange, string Status,
    string? Flag, DateTime OrderedAt, DateTime? CompletedAt);

public record AllergyDto(int Id, string Allergen, string AllergyType,
    string? Severity, string? Reaction, bool IsActive);

public record VitalsDto(int Id, DateTime RecordedAt,
    decimal? Temperature, int? HeartRate,
    int? SystolicBp, int? DiastolicBp,
    int? RespiratoryRate, decimal? OxygenSaturation,
    decimal? Weight, decimal? Height);

// ── Dashboard ──
public record ClinicalDashboardDto(
    int TotalPatients, int TotalProviders,
    int TodayAppointments, int PendingLabResults,
    int ActivePrescriptions, int CriticalAlerts,
    IEnumerable<AppointmentListDto> UpcomingAppointments,
    IEnumerable<LabResultDto> CriticalLabs);
