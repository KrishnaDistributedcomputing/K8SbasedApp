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
public class AllergiesController : ControllerBase
{
    private readonly EhrDbContext _db;
    public AllergiesController(EhrDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Allergy>>> GetByPatient([FromQuery] int patientId)
    {
        return await _db.Allergies.Where(a => a.PatientId == patientId).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Allergy>> Create(Allergy allergy)
    {
        _db.Allergies.Add(allergy);
        await _db.SaveChangesAsync();
        return Created($"/api/allergies/{allergy.Id}", allergy);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var allergy = await _db.Allergies.FindAsync(id);
        if (allergy is null) return NotFound();
        _db.Allergies.Remove(allergy);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiVersion(1.0)]
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class VitalsController : ControllerBase
{
    private readonly EhrDbContext _db;
    public VitalsController(EhrDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vitals>>> GetByPatient(
        [FromQuery] int patientId, [FromQuery] int count = 10)
    {
        return await _db.Vitals
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.RecordedAt)
            .Take(count)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Vitals>> Create(Vitals vitals)
    {
        vitals.RecordedAt = DateTime.UtcNow;
        _db.Vitals.Add(vitals);
        await _db.SaveChangesAsync();
        return Created($"/api/vitals/{vitals.Id}", vitals);
    }
}
