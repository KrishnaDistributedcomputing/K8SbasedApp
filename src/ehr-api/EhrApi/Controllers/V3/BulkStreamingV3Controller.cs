using Microsoft.AspNetCore.Mvc;
using EhrApi.Models.V3;
using Asp.Versioning;

namespace EhrApi.Controllers.V3;

/// <summary>
/// Bulk FHIR export and real-time event streaming — Preview stubs.
/// </summary>
[ApiVersion(3.0)]
[ApiController]
[Route("api/v{version:apiVersion}/bulk")]
public class BulkOperationsV3Controller : ControllerBase
{
    /// <summary>Initiate a bulk FHIR export job (stub)</summary>
    [HttpPost("export")]
    public ActionResult<BulkExportStatus> StartExport([FromBody] BulkExportRequest request)
    {
        var jobId = Guid.NewGuid().ToString("N")[..12];

        return Accepted(new BulkExportStatus(
            jobId, "Queued", 0,
            DateTime.UtcNow, null, null));
    }

    /// <summary>Check bulk export job status (stub)</summary>
    [HttpGet("export/{jobId}")]
    public ActionResult<BulkExportStatus> GetExportStatus(string jobId)
    {
        // Simulated completed job
        return Ok(new BulkExportStatus(
            jobId, "Completed", 100,
            DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow,
            [
                new BulkExportFile("Patient", $"/api/v3/bulk/download/{jobId}/Patient.ndjson", 15),
                new BulkExportFile("Observation", $"/api/v3/bulk/download/{jobId}/Observation.ndjson", 42),
                new BulkExportFile("MedicationRequest", $"/api/v3/bulk/download/{jobId}/MedicationRequest.ndjson", 18)
            ]));
    }
}

/// <summary>
/// Real-time event streaming subscriptions — Preview stubs.
/// Supports webhook-based notifications for clinical events.
/// </summary>
[ApiVersion(3.0)]
[ApiController]
[Route("api/v{version:apiVersion}/streaming")]
public class StreamingV3Controller : ControllerBase
{
    /// <summary>Create a streaming subscription (stub)</summary>
    [HttpPost("subscriptions")]
    public ActionResult<StreamSubscription> CreateSubscription(
        [FromBody] StreamSubscription request)
    {
        var sub = new StreamSubscription(
            Guid.NewGuid().ToString("N")[..12],
            request.EventTypes,
            request.CallbackUrl,
            "Active",
            DateTime.UtcNow);

        return Created($"/api/v3/streaming/subscriptions/{sub.SubscriptionId}", sub);
    }

    /// <summary>List active subscriptions (stub)</summary>
    [HttpGet("subscriptions")]
    public ActionResult<IEnumerable<StreamSubscription>> ListSubscriptions()
    {
        return Ok(new[]
        {
            new StreamSubscription("sub-demo-001",
                ["patient.created", "patient.updated"],
                "https://partner-system.example.com/webhooks/ehr",
                "Active", DateTime.UtcNow.AddDays(-7)),
            new StreamSubscription("sub-demo-002",
                ["lab.critical", "vitals.alert"],
                "https://alerting.example.com/webhooks/clinical",
                "Active", DateTime.UtcNow.AddDays(-3))
        });
    }

    /// <summary>List available event types (stub)</summary>
    [HttpGet("event-types")]
    public ActionResult<object> ListEventTypes()
    {
        return Ok(new
        {
            eventTypes = new[]
            {
                new { name = "patient.created", description = "New patient registered", category = "Patient" },
                new { name = "patient.updated", description = "Patient demographics updated", category = "Patient" },
                new { name = "appointment.scheduled", description = "New appointment booked", category = "Scheduling" },
                new { name = "appointment.completed", description = "Appointment marked completed", category = "Scheduling" },
                new { name = "lab.ordered", description = "Lab test ordered", category = "Laboratory" },
                new { name = "lab.completed", description = "Lab result available", category = "Laboratory" },
                new { name = "lab.critical", description = "Critical lab value detected", category = "Laboratory" },
                new { name = "prescription.created", description = "New prescription written", category = "Pharmacy" },
                new { name = "prescription.dispensed", description = "Prescription dispensed", category = "Pharmacy" },
                new { name = "vitals.recorded", description = "Vitals captured", category = "Clinical" },
                new { name = "vitals.alert", description = "Abnormal vitals detected", category = "Clinical" },
                new { name = "record.created", description = "New clinical encounter", category = "Medical Records" }
            }
        });
    }
}
