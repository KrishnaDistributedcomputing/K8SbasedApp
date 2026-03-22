using System.ComponentModel.DataAnnotations;

namespace EhrApi.Models;

public class Prescription
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required]
    public int ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    [Required, MaxLength(200)]
    public string MedicationName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Dosage { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Frequency { get; set; } = string.Empty; // e.g. "Twice daily", "Every 8 hours"

    [MaxLength(100)]
    public string? Route { get; set; } // Oral, IV, Topical, etc.

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int? Refills { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Completed, Cancelled, OnHold

    [MaxLength(500)]
    public string? Instructions { get; set; }

    [MaxLength(500)]
    public string? Pharmacy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
