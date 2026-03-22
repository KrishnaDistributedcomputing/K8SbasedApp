using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using EhrApi.Models;
using Asp.Versioning;

namespace EhrApi.Controllers;

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly EhrDbContext _db;
    public AppointmentsController(EhrDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAll(
        [FromQuery] int? patientId, [FromQuery] int? providerId,
        [FromQuery] string? status, [FromQuery] DateTime? date)
    {
        var query = _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Provider)
            .AsQueryable();

        if (patientId.HasValue) query = query.Where(a => a.PatientId == patientId);
        if (providerId.HasValue) query = query.Where(a => a.ProviderId == providerId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(a => a.Status == status);
        if (date.HasValue) query = query.Where(a => a.ScheduledAt.Date == date.Value.Date);

        return await query.OrderBy(a => a.ScheduledAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Appointment>> Get(int id)
    {
        var appt = await _db.Appointments
            .Include(a => a.Patient).Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == id);
        return appt is null ? NotFound() : appt;
    }

    [HttpPost]
    public async Task<ActionResult<Appointment>> Create(Appointment appointment)
    {
        appointment.CreatedAt = DateTime.UtcNow;
        appointment.UpdatedAt = DateTime.UtcNow;
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = appointment.Id }, appointment);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Appointment appointment)
    {
        if (id != appointment.Id) return BadRequest();
        appointment.UpdatedAt = DateTime.UtcNow;
        _db.Entry(appointment).State = EntityState.Modified;
        _db.Entry(appointment).Property(x => x.CreatedAt).IsModified = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var appt = await _db.Appointments.FindAsync(id);
        if (appt is null) return NotFound();
        appt.Status = status;
        appt.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
