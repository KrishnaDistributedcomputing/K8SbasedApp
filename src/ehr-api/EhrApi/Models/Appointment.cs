using System.ComponentModel.DataAnnotations;

namespace EhrApi.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required]
    public int ProviderId { get; set; }
    public Provider Provider { get; set; } = null!;

    [Required]
    public DateTime ScheduledAt { get; set; }

    public int DurationMinutes { get; set; } = 30;

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Scheduled"; // Scheduled, CheckedIn, InProgress, Completed, Cancelled, NoShow

    [MaxLength(100)]
    public string? Type { get; set; } // General, FollowUp, Emergency, Specialist, Lab, Imaging

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
