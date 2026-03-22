using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using EhrApi.Models.V3;
using Asp.Versioning;

namespace EhrApi.Controllers.V3;

/// <summary>
/// AI-powered Clinical Decision Support — Preview.
/// Returns mock data illustrating the future AI/ML pipeline.
/// </summary>
[ApiVersion(3.0)]
[ApiController]
[Route("api/v{version:apiVersion}/ai")]
public class ClinicalAiV3Controller : ControllerBase
{
    private readonly EhrDbContext _db;
    public ClinicalAiV3Controller(EhrDbContext db) => _db = db;

    /// <summary>Drug interaction check (simulated)</summary>
    [HttpPost("drug-interactions")]
    public ActionResult<ClinicalDecisionResponse> CheckDrugInteractions(
        [FromBody] ClinicalDecisionRequest request)
    {
        var interactions = new List<DrugInteractionAlert>();

        // Simulated interaction logic for demo
        if (request.CurrentMedications.Length >= 2)
        {
            interactions.Add(new DrugInteractionAlert(
                "Moderate",
                request.CurrentMedications[0],
                request.CurrentMedications[1],
                "Potential interaction detected — monitor patient closely.",
                "Consider alternative medication or adjust dosing schedule."));
        }

        return Ok(new ClinicalDecisionResponse(
            Guid.NewGuid().ToString("N")[..12],
            DateTime.UtcNow,
            "completed",
            "AI-generated — not a substitute for clinical judgment. Always verify with clinical pharmacist.",
            interactions.ToArray(),
            [new ClinicalGuideline("GL-001", "Medication Safety Monitoring",
                "ISMP 2025", "Perform drug interaction screening for all new prescriptions.",
                "Level A — Strong Evidence")],
            [],
            ["Review patient allergy list", "Confirm renal function before dosing"]));
    }

    /// <summary>Clinical guideline recommendations (simulated)</summary>
    [HttpPost("guidelines")]
    public ActionResult<ClinicalDecisionResponse> GetGuidelines(
        [FromBody] ClinicalDecisionRequest request)
    {
        var guidelines = new List<ClinicalGuideline>();

        foreach (var dx in request.ActiveDiagnoses)
        {
            guidelines.Add(new ClinicalGuideline(
                $"GL-{dx.GetHashCode():X8}"[..10],
                $"Best Practice Guideline for {dx}",
                "AMA Clinical Practice Guidelines 2025",
                $"Follow evidence-based protocol for {dx}. Schedule follow-up within 2 weeks.",
                "Level B — Moderate Evidence"));
        }

        return Ok(new ClinicalDecisionResponse(
            Guid.NewGuid().ToString("N")[..12],
            DateTime.UtcNow,
            "completed",
            "AI-generated — clinical validation required.",
            [], guidelines.ToArray(), [],
            ["Schedule follow-up appointment", "Order baseline labs"]));
    }

    /// <summary>Readmission risk prediction (simulated)</summary>
    [HttpGet("readmission-risk/{patientId:int}")]
    public async Task<ActionResult<ReadmissionRiskResponse>> ReadmissionRisk(int patientId)
    {
        var patient = await _db.Patients
            .Include(p => p.Appointments)
            .Include(p => p.MedicalRecords)
            .Include(p => p.Prescriptions)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient is null) return NotFound();

        // Simulated risk score based on data density
        var dataPoints = patient.Appointments.Count + patient.MedicalRecords.Count + patient.Prescriptions.Count;
        var riskScore = Math.Min(0.95m, 0.05m + (dataPoints * 0.03m));
        var level = riskScore switch
        {
            > 0.7m => "High",
            > 0.4m => "Moderate",
            _ => "Low"
        };

        return Ok(new ReadmissionRiskResponse(
            patientId, Math.Round(riskScore, 3), level,
            ["Prior admission count", "Active prescriptions", "Comorbidity index", "Age factor"],
            "EHR-ReadmitPredict v0.1-preview",
            DateTime.UtcNow,
            "Preview model — not validated for clinical use. For demonstration purposes only."));
    }

    /// <summary>Population health insights (simulated)</summary>
    [HttpGet("population-health")]
    public async Task<ActionResult<IEnumerable<PopulationHealthInsight>>> PopulationHealth()
    {
        var totalPatients = await _db.Patients.CountAsync();
        var activeRx = await _db.Prescriptions.CountAsync(rx => rx.Status == "Active");
        var pendingLabs = await _db.LabResults.CountAsync(l => l.Status == "Pending");
        var completedAppts = await _db.Appointments.CountAsync(a => a.Status == "Completed");

        return Ok(new[]
        {
            new PopulationHealthInsight("Patient Census", "Demographics", totalPatients, "Stable", 2.1m, totalPatients, "Last 30 days"),
            new PopulationHealthInsight("Active Prescriptions", "Pharmacy", activeRx, "Increasing", 5.3m, activeRx, "Last 30 days"),
            new PopulationHealthInsight("Pending Lab Results", "Laboratory", pendingLabs, "Decreasing", -8.2m, pendingLabs, "Last 7 days"),
            new PopulationHealthInsight("Appointment Completion Rate", "Operations", completedAppts > 0 ? Math.Round((decimal)completedAppts / Math.Max(1, totalPatients) * 100, 1) : 0m, "Improving", 3.7m, completedAppts, "Last 30 days")
        });
    }
}
