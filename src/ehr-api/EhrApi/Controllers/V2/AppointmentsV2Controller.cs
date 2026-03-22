using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using EhrApi.Models.V2;
using Asp.Versioning;

namespace EhrApi.Controllers.V2;

[ApiVersion(2.0)]
[ApiController]
[Route("api/v{version:apiVersion}/appointments")]
public class AppointmentsV2Controller : ControllerBase
{
    private readonly EhrDbContext _db;
    public AppointmentsV2Controller(EhrDbContext db) => _db = db;

    /// <summary>Paginated appointments with filters and DTO responses</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<AppointmentListDto>>> GetAll(
        [FromQuery] int? patientId, [FromQuery] int? providerId,
        [FromQuery] string? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] string? type,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var query = _db.Appointments
            .Include(a => a.Patient).Include(a => a.Provider).AsQueryable();

        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId);
        if (providerId.HasValue) query = query.Where(a => a.ProviderId == providerId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(a => a.Type == type);
        if (from.HasValue) query = query.Where(a => a.ScheduledAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.ScheduledAt <= to.Value);

        var totalCount = await query.CountAsync();

        var items = await query.OrderBy(a => a.ScheduledAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AppointmentListDto(
                a.Id, a.PatientId, a.Patient.FirstName + " " + a.Patient.LastName,
                a.ProviderId, a.Provider.FirstName + " " + a.Provider.LastName,
                a.ScheduledAt, a.DurationMinutes, a.Status, a.Type, a.Reason))
            .ToListAsync();

        return Ok(new PagedResult<AppointmentListDto>
        {
            Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
        });
    }

    /// <summary>Appointment detail with full context</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppointmentDetailDto>> Get(int id)
    {
        var a = await _db.Appointments
            .Include(a => a.Patient).Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (a is null) return NotFound();

        return Ok(new AppointmentDetailDto(
            a.Id, a.PatientId, $"{a.Patient.FirstName} {a.Patient.LastName}",
            a.ProviderId, $"{a.Provider.FirstName} {a.Provider.LastName}",
            a.ScheduledAt, a.DurationMinutes, a.Status, a.Type, a.Reason, a.Notes,
            a.CreatedAt, a.UpdatedAt));
    }

    /// <summary>Today's appointments summary</summary>
    [HttpGet("today")]
    public async Task<ActionResult<IEnumerable<AppointmentListDto>>> Today()
    {
        var today = DateTime.UtcNow.Date;

        var items = await _db.Appointments
            .Include(a => a.Patient).Include(a => a.Provider)
            .Where(a => a.ScheduledAt.Date == today)
            .OrderBy(a => a.ScheduledAt)
            .Select(a => new AppointmentListDto(
                a.Id, a.PatientId, a.Patient.FirstName + " " + a.Patient.LastName,
                a.ProviderId, a.Provider.FirstName + " " + a.Provider.LastName,
                a.ScheduledAt, a.DurationMinutes, a.Status, a.Type, a.Reason))
            .ToListAsync();

        return Ok(items);
    }
}
