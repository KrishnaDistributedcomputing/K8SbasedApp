using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using EhrApi.Models.V2;
using Asp.Versioning;

namespace EhrApi.Controllers.V2;

[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/dashboard")]
public class DashboardV2Controller : ControllerBase
{
    private readonly EhrDbContext _db;
    public DashboardV2Controller(EhrDbContext db) => _db = db;

    /// <summary>Clinical dashboard with real-time KPIs</summary>
    [HttpGet]
    public async Task<ActionResult<ClinicalDashboardDto>> GetDashboard()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var totalPatients = await _db.Patients.CountAsync();
        var totalProviders = await _db.Providers.CountAsync(p => p.IsActive);
        var todayAppts = await _db.Appointments.CountAsync(a => a.ScheduledAt.Date == today);
        var pendingLabs = await _db.LabResults.CountAsync(l => l.Status == "Pending");
        var activeRx = await _db.Prescriptions.CountAsync(rx => rx.Status == "Active");
        var criticalAlerts = await _db.LabResults.CountAsync(l => l.Flag == "Critical" && l.Status != "Reviewed");

        var upcoming = await _db.Appointments
            .Include(a => a.Patient).Include(a => a.Provider)
            .Where(a => a.ScheduledAt >= now && a.Status == "Scheduled")
            .OrderBy(a => a.ScheduledAt).Take(10)
            .Select(a => new AppointmentListDto(
                a.Id, a.PatientId, a.Patient.FirstName + " " + a.Patient.LastName,
                a.ProviderId, a.Provider.FirstName + " " + a.Provider.LastName,
                a.ScheduledAt, a.DurationMinutes, a.Status, a.Type, a.Reason))
            .ToListAsync();

        var criticalLabs = await _db.LabResults
            .Include(l => l.Patient)
            .Where(l => l.Flag == "Critical" && l.Status != "Reviewed")
            .OrderByDescending(l => l.OrderedAt).Take(10)
            .Select(l => new LabResultDto(
                l.Id, l.TestName, l.TestCode, l.Result, l.Unit,
                l.ReferenceRange, l.Status, l.Flag, l.OrderedAt, l.CompletedAt))
            .ToListAsync();

        return Ok(new ClinicalDashboardDto(
            totalPatients, totalProviders, todayAppts, pendingLabs,
            activeRx, criticalAlerts, upcoming, criticalLabs));
    }
}

[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/medicalrecords")]
public class MedicalRecordsV2Controller : ControllerBase
{
    private readonly EhrDbContext _db;
    public MedicalRecordsV2Controller(EhrDbContext db) => _db = db;

    /// <summary>Paginated medical records with filters</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<MedicalRecordListDto>>> GetAll(
        [FromQuery] int? patientId, [FromQuery] int? providerId,
        [FromQuery] string? encounterType, [FromQuery] string? diagnosisCode,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = _db.MedicalRecords
            .Include(m => m.Patient).Include(m => m.Provider).AsQueryable();

        if (patientId.HasValue) query = query.Where(m => m.PatientId == patientId);
        if (providerId.HasValue) query = query.Where(m => m.ProviderId == providerId);
        if (!string.IsNullOrWhiteSpace(encounterType))
            query = query.Where(m => m.EncounterType == encounterType);
        if (!string.IsNullOrWhiteSpace(diagnosisCode))
            query = query.Where(m => m.DiagnosisCode != null && m.DiagnosisCode.Contains(diagnosisCode));

        var totalCount = await query.CountAsync();

        var items = await query.OrderByDescending(m => m.EncounterDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new MedicalRecordListDto(
                m.Id, m.PatientId, m.Patient.FirstName + " " + m.Patient.LastName,
                m.ProviderId, m.Provider.FirstName + " " + m.Provider.LastName,
                m.EncounterDate, m.EncounterType, m.DiagnosisCode, m.ChiefComplaint))
            .ToListAsync();

        return Ok(new PagedResult<MedicalRecordListDto>
        {
            Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
        });
    }
}

[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/prescriptions")]
public class PrescriptionsV2Controller : ControllerBase
{
    private readonly EhrDbContext _db;
    public PrescriptionsV2Controller(EhrDbContext db) => _db = db;

    /// <summary>Paginated prescriptions with filters</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PrescriptionDto>>> GetAll(
        [FromQuery] int? patientId, [FromQuery] string? status,
        [FromQuery] string? medication,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = _db.Prescriptions.AsQueryable();
        if (patientId.HasValue) query = query.Where(rx => rx.PatientId == patientId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(rx => rx.Status == status);
        if (!string.IsNullOrWhiteSpace(medication))
            query = query.Where(rx => rx.MedicationName.Contains(medication));

        var totalCount = await query.CountAsync();

        var items = await query.OrderByDescending(rx => rx.StartDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(rx => new PrescriptionDto(
                rx.Id, rx.MedicationName, rx.Dosage, rx.Frequency,
                rx.Route, rx.StartDate, rx.EndDate, rx.Refills, rx.Status, rx.Pharmacy))
            .ToListAsync();

        return Ok(new PagedResult<PrescriptionDto>
        {
            Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
        });
    }
}

[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/labresults")]
public class LabResultsV2Controller : ControllerBase
{
    private readonly EhrDbContext _db;
    public LabResultsV2Controller(EhrDbContext db) => _db = db;

    /// <summary>Paginated lab results with filters</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<LabResultDto>>> GetAll(
        [FromQuery] int? patientId, [FromQuery] string? status,
        [FromQuery] string? flag, [FromQuery] string? testName,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = _db.LabResults.AsQueryable();
        if (patientId.HasValue) query = query.Where(l => l.PatientId == patientId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(l => l.Status == status);
        if (!string.IsNullOrWhiteSpace(flag)) query = query.Where(l => l.Flag == flag);
        if (!string.IsNullOrWhiteSpace(testName))
            query = query.Where(l => l.TestName.Contains(testName));

        var totalCount = await query.CountAsync();

        var items = await query.OrderByDescending(l => l.OrderedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new LabResultDto(
                l.Id, l.TestName, l.TestCode, l.Result, l.Unit,
                l.ReferenceRange, l.Status, l.Flag, l.OrderedAt, l.CompletedAt))
            .ToListAsync();

        return Ok(new PagedResult<LabResultDto>
        {
            Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
        });
    }
}
