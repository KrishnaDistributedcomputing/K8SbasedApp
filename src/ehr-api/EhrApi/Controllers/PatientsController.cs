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
public class PatientsController : ControllerBase
{
    private readonly EhrDbContext _db;
    public PatientsController(EhrDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Patient>>> GetAll([FromQuery] string? search)
    {
        var query = _db.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.LastName.Contains(search) || p.FirstName.Contains(search));
        return await query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Patient>> Get(int id)
    {
        var patient = await _db.Patients
            .Include(p => p.Allergies)
            .Include(p => p.Vitals.OrderByDescending(v => v.RecordedAt).Take(5))
            .FirstOrDefaultAsync(p => p.Id == id);
        return patient is null ? NotFound() : patient;
    }

    [HttpPost]
    public async Task<ActionResult<Patient>> Create(Patient patient)
    {
        patient.CreatedAt = DateTime.UtcNow;
        patient.UpdatedAt = DateTime.UtcNow;
        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = patient.Id }, patient);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Patient patient)
    {
        if (id != patient.Id) return BadRequest();
        patient.UpdatedAt = DateTime.UtcNow;
        _db.Entry(patient).State = EntityState.Modified;
        _db.Entry(patient).Property(x => x.CreatedAt).IsModified = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient is null) return NotFound();
        _db.Patients.Remove(patient);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
