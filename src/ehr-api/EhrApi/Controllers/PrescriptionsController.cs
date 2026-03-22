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
public class PrescriptionsController : ControllerBase
{
    private readonly EhrDbContext _db;
    public PrescriptionsController(EhrDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Prescription>>> GetAll(
        [FromQuery] int? patientId, [FromQuery] string? status)
    {
        var query = _db.Prescriptions
            .Include(r => r.Patient).Include(r => r.Provider).AsQueryable();
        if (patientId.HasValue) query = query.Where(r => r.PatientId == patientId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Status == status);
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Prescription>> Get(int id)
    {
        var rx = await _db.Prescriptions
            .Include(r => r.Patient).Include(r => r.Provider)
            .FirstOrDefaultAsync(r => r.Id == id);
        return rx is null ? NotFound() : rx;
    }

    [HttpPost]
    public async Task<ActionResult<Prescription>> Create(Prescription rx)
    {
        rx.CreatedAt = DateTime.UtcNow;
        _db.Prescriptions.Add(rx);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = rx.Id }, rx);
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var rx = await _db.Prescriptions.FindAsync(id);
        if (rx is null) return NotFound();
        rx.Status = status;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
