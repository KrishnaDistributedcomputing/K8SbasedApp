using System.ComponentModel.DataAnnotations;

namespace EhrApi.Models;

public class MedicalRecord
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required]
    public int ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    [Required]
    public DateTime EncounterDate { get; set; }

    [Required, MaxLength(100)]
    public string EncounterType { get; set; } = string.Empty; // Office Visit, ER, Hospitalization, Telehealth

    [MaxLength(500)]
    public string? ChiefComplaint { get; set; }

    [MaxLength(2000)]
    public string? Diagnosis { get; set; }

    [MaxLength(500)]
    public string? DiagnosisCode { get; set; } // ICD-10

    [MaxLength(2000)]
    public string? TreatmentPlan { get; set; }

    [MaxLength(2000)]
    public string? ClinicalNotes { get; set; }

    [MaxLength(1000)]
    public string? FollowUpInstructions { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
