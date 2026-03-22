using System.ComponentModel.DataAnnotations;

namespace EhrApi.Models;

public class LabResult
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int? OrderedByProviderId { get; set; }

    [Required, MaxLength(200)]
    public string TestName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TestCode { get; set; } // LOINC code

    [MaxLength(200)]
    public string? Result { get; set; }

    [MaxLength(50)]
    public string? Unit { get; set; }

    [MaxLength(100)]
    public string? ReferenceRange { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled

    [MaxLength(50)]
    public string? Flag { get; set; } // Normal, High, Low, Critical

    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class Allergy
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Allergen { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string AllergyType { get; set; } = string.Empty; // Drug, Food, Environmental

    [MaxLength(50)]
    public string? Severity { get; set; } // Mild, Moderate, Severe, Life-Threatening

    [MaxLength(500)]
    public string? Reaction { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
}

public class Vitals
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public decimal? Temperature { get; set; } // Fahrenheit
    public int? HeartRate { get; set; } // BPM
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? RespiratoryRate { get; set; }
    public decimal? OxygenSaturation { get; set; } // Percentage
    public decimal? Weight { get; set; } // lbs
    public decimal? Height { get; set; } // inches

    [MaxLength(500)]
    public string? Notes { get; set; }
}
