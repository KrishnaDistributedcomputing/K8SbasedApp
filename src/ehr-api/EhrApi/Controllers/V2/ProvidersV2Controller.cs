using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using EhrApi.Models.V2;
using Asp.Versioning;

namespace EhrApi.Controllers.V2;

[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/providers")]
public class ProvidersV2Controller : ControllerBase
{
    private readonly EhrDbContext _db;
    public ProvidersV2Controller(EhrDbContext db) => _db = db;

    /// <summary>Paginated provider list with workload summary</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProviderListDto>>> GetAll(
        [FromQuery] string? specialty, [FromQuery] bool? isActive,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = _db.Providers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(specialty))
            query = query.Where(p => p.Specialty.Contains(specialty));
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();
        var now = DateTime.UtcNow;

        var providers = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProviderListDto(
                p.Id, p.FirstName, p.LastName, p.Specialty,
                p.Department, p.IsActive,
                p.Appointments.Count(a => a.ScheduledAt > now && a.Status == "Scheduled")))
            .ToListAsync();

        return Ok(new PagedResult<ProviderListDto>
        {
            Items = providers, TotalCount = totalCount, Page = page, PageSize = pageSize
        });
    }

    /// <summary>Provider detail with workload metrics</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProviderDetailDto>> Get(int id)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekEnd = today.AddDays(7);

        var p = await _db.Providers
            .Include(p => p.Appointments)
            .Include(p => p.Prescriptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p is null) return NotFound();

        var uniquePatients = p.Appointments.Select(a => a.PatientId).Distinct().Count();
        var workload = new ProviderWorkloadDto(
            p.Appointments.Count(a => a.ScheduledAt.Date == today && a.Status == "Scheduled"),
            p.Appointments.Count(a => a.ScheduledAt >= today && a.ScheduledAt < weekEnd),
            uniquePatients,
            p.Prescriptions.Count(rx => rx.Status == "Active"));

        return Ok(new ProviderDetailDto(
            p.Id, p.FirstName, p.LastName, p.Specialty,
            p.LicenseNumber, p.Phone, p.Email, p.Department,
            p.IsActive, p.CreatedAt, workload));
    }

    /// <summary>Provider schedule for a given date range</summary>
    [HttpGet("{id:int}/schedule")]
    public async Task<ActionResult<IEnumerable<AppointmentListDto>>> GetSchedule(
        int id, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var start = from ?? DateTime.UtcNow.Date;
        var end = to ?? start.AddDays(7);

        var appointments = await _db.Appointments
            .Include(a => a.Patient)
            .Where(a => a.ProviderId == id && a.ScheduledAt >= start && a.ScheduledAt <= end)
            .OrderBy(a => a.ScheduledAt)
            .Select(a => new AppointmentListDto(
                a.Id, a.PatientId, a.Patient.FirstName + " " + a.Patient.LastName,
                a.ProviderId, "", a.ScheduledAt, a.DurationMinutes,
                a.Status, a.Type, a.Reason))
            .ToListAsync();

        return Ok(appointments);
    }
}
