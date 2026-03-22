using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using EhrApi.Models;
using EhrApi.Models.V2;
using Asp.Versioning;

namespace EhrApi.Controllers.V2;

[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/patients")]
public class PatientsV2Controller : ControllerBase
{
    private readonly EhrDbContext _db;
    public PatientsV2Controller(EhrDbContext db) => _db = db;

    /// <summary>Paginated patient list with search, sort, and summary counts</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientListDto>>> GetAll(
        [FromQuery] string? search, [FromQuery] string? gender,
        [FromQuery] string? sortBy = "lastName", [FromQuery] string? sortDir = "asc",
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = _db.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.LastName.Contains(search)
                || p.FirstName.Contains(search)
                || (p.Email != null && p.Email.Contains(search))
                || (p.InsuranceId != null && p.InsuranceId.Contains(search)));

        if (!string.IsNullOrWhiteSpace(gender))
            query = query.Where(p => p.Gender == gender);

        var totalCount = await query.CountAsync();

        query = sortBy?.ToLower() switch
        {
            "firstname" => sortDir == "desc" ? query.OrderByDescending(p => p.FirstName) : query.OrderBy(p => p.FirstName),
            "dob" => sortDir == "desc" ? query.OrderByDescending(p => p.DateOfBirth) : query.OrderBy(p => p.DateOfBirth),
            "createdat" => sortDir == "desc" ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => sortDir == "desc" ? query.OrderByDescending(p => p.LastName) : query.OrderBy(p => p.LastName)
        };

        var patients = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PatientListDto(
                p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.Gender,
                p.Phone, p.Email, p.InsuranceId,
                p.Allergies.Count,
                p.Prescriptions.Count(rx => rx.Status == "Active"),
                p.CreatedAt))
            .ToListAsync();

        return Ok(new PagedResult<PatientListDto>
        {
            Items = patients, TotalCount = totalCount, Page = page, PageSize = pageSize
        });
    }

    /// <summary>Detailed patient with allergies, recent vitals, and stats</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PatientDetailDto>> Get(int id)
    {
        var p = await _db.Patients
            .Include(p => p.Allergies)
            .Include(p => p.Vitals.OrderByDescending(v => v.RecordedAt).Take(5))
            .Include(p => p.Appointments)
            .Include(p => p.Prescriptions)
            .Include(p => p.LabResults)
            .Include(p => p.MedicalRecords)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p is null) return NotFound();

        var dto = new PatientDetailDto(
            p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.Gender,
            p.Phone, p.Email, p.Address, p.InsuranceId, p.BloodType,
            p.EmergencyContactName, p.EmergencyContactPhone,
            p.CreatedAt, p.UpdatedAt,
            p.Allergies.Select(a => new AllergyDto(a.Id, a.Allergen, a.AllergyType, a.Severity, a.Reaction, a.IsActive)),
            p.Vitals.Select(v => new VitalsDto(v.Id, v.RecordedAt, v.Temperature, v.HeartRate, v.SystolicBp, v.DiastolicBp, v.RespiratoryRate, v.OxygenSaturation, v.Weight, v.Height)),
            new PatientStatsDto(
                p.Appointments.Count,
                p.MedicalRecords.Count,
                p.Prescriptions.Count(rx => rx.Status == "Active"),
                p.LabResults.Count(l => l.Status == "Pending"),
                p.Appointments.Where(a => a.Status == "Completed")
                    .OrderByDescending(a => a.ScheduledAt).FirstOrDefault()?.ScheduledAt));

        return Ok(dto);
    }

    /// <summary>Create a new patient from validated DTO</summary>
    [HttpPost]
    public async Task<ActionResult<PatientDetailDto>> Create(CreatePatientDto dto)
    {
        var patient = new Patient
        {
            FirstName = dto.FirstName, LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth, Gender = dto.Gender,
            Phone = dto.Phone, Email = dto.Email, Address = dto.Address,
            InsuranceId = dto.InsuranceId, BloodType = dto.BloodType,
            EmergencyContactName = dto.EmergencyContactName,
            EmergencyContactPhone = dto.EmergencyContactPhone,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = patient.Id, version = "2" }, null);
    }

    /// <summary>Full clinical summary for a patient</summary>
    [HttpGet("{id:int}/clinical-summary")]
    public async Task<ActionResult<PatientClinicalSummary>> ClinicalSummary(int id)
    {
        var p = await _db.Patients
            .Include(p => p.Allergies)
            .Include(p => p.Vitals.OrderByDescending(v => v.RecordedAt).Take(5))
            .Include(p => p.Appointments.OrderByDescending(a => a.ScheduledAt).Take(10))
                .ThenInclude(a => a.Provider)
            .Include(p => p.MedicalRecords.OrderByDescending(m => m.EncounterDate).Take(10))
                .ThenInclude(m => m.Provider)
            .Include(p => p.Prescriptions.Where(rx => rx.Status == "Active"))
                .ThenInclude(rx => rx.Provider)
            .Include(p => p.LabResults.Where(l => l.Status == "Pending"))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p is null) return NotFound();

        return Ok(new PatientClinicalSummary(
            new PatientDetailDto(p.Id, p.FirstName, p.LastName, p.DateOfBirth, p.Gender,
                p.Phone, p.Email, p.Address, p.InsuranceId, p.BloodType,
                p.EmergencyContactName, p.EmergencyContactPhone,
                p.CreatedAt, p.UpdatedAt,
                p.Allergies.Select(a => new AllergyDto(a.Id, a.Allergen, a.AllergyType, a.Severity, a.Reaction, a.IsActive)),
                p.Vitals.Select(v => new VitalsDto(v.Id, v.RecordedAt, v.Temperature, v.HeartRate, v.SystolicBp, v.DiastolicBp, v.RespiratoryRate, v.OxygenSaturation, v.Weight, v.Height)),
                new PatientStatsDto(p.Appointments.Count, p.MedicalRecords.Count,
                    p.Prescriptions.Count(rx => rx.Status == "Active"),
                    p.LabResults.Count(l => l.Status == "Pending"),
                    p.Appointments.Where(a => a.Status == "Completed").OrderByDescending(a => a.ScheduledAt).FirstOrDefault()?.ScheduledAt)),
            p.Appointments.Select(a => new AppointmentListDto(a.Id, a.PatientId, $"{p.FirstName} {p.LastName}",
                a.ProviderId, $"{a.Provider.FirstName} {a.Provider.LastName}",
                a.ScheduledAt, a.DurationMinutes, a.Status, a.Type, a.Reason)),
            p.MedicalRecords.Select(m => new MedicalRecordListDto(m.Id, m.PatientId, $"{p.FirstName} {p.LastName}",
                m.ProviderId, $"{m.Provider.FirstName} {m.Provider.LastName}",
                m.EncounterDate, m.EncounterType, m.DiagnosisCode, m.ChiefComplaint)),
            p.Prescriptions.Select(rx => new PrescriptionDto(rx.Id, rx.MedicationName, rx.Dosage, rx.Frequency,
                rx.Route, rx.StartDate, rx.EndDate, rx.Refills, rx.Status, rx.Pharmacy)),
            p.LabResults.Select(l => new LabResultDto(l.Id, l.TestName, l.TestCode, l.Result,
                l.Unit, l.ReferenceRange, l.Status, l.Flag, l.OrderedAt, l.CompletedAt))));
    }
}
