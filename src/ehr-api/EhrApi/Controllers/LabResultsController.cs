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
public class LabResultsController : ControllerBase
{
    private readonly EhrDbContext _db;
    public LabResultsController(EhrDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LabResult>>> GetAll(
        [FromQuery] int? patientId, [FromQuery] string? status)
    {
        var query = _db.LabResults.AsQueryable();
        if (patientId.HasValue) query = query.Where(l => l.PatientId == patientId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(l => l.Status == status);
        return await query.OrderByDescending(l => l.OrderedAt).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<LabResult>> Create(LabResult lab)
    {
        lab.OrderedAt = DateTime.UtcNow;
        _db.LabResults.Add(lab);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = lab.Id }, lab);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LabResult>> GetById(int id)
    {
        var lab = await _db.LabResults.FindAsync(id);
        return lab is null ? NotFound() : lab;
    }

    [HttpPatch("{id}/result")]
    public async Task<IActionResult> UpdateResult(int id, [FromBody] LabResultUpdate update)
    {
        var lab = await _db.LabResults.FindAsync(id);
        if (lab is null) return NotFound();
        lab.Result = update.Result;
        lab.Unit = update.Unit;
        lab.Flag = update.Flag;
        lab.Status = "Completed";
        lab.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record LabResultUpdate(string Result, string? Unit, string? Flag);
