using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using EhrApi.Models;

namespace EhrApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicalRecordsController : ControllerBase
{
    private readonly EhrDbContext _db;
    public MedicalRecordsController(EhrDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MedicalRecord>>> GetAll([FromQuery] int? patientId)
    {
        var query = _db.MedicalRecords
            .Include(m => m.Patient).Include(m => m.Provider).AsQueryable();
        if (patientId.HasValue) query = query.Where(m => m.PatientId == patientId);
        return await query.OrderByDescending(m => m.EncounterDate).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MedicalRecord>> Get(int id)
    {
        var record = await _db.MedicalRecords
            .Include(m => m.Patient).Include(m => m.Provider)
            .FirstOrDefaultAsync(m => m.Id == id);
        return record is null ? NotFound() : record;
    }

    [HttpPost]
    public async Task<ActionResult<MedicalRecord>> Create(MedicalRecord record)
    {
        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        _db.MedicalRecords.Add(record);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = record.Id }, record);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, MedicalRecord record)
    {
        if (id != record.Id) return BadRequest();
        record.UpdatedAt = DateTime.UtcNow;
        _db.Entry(record).State = EntityState.Modified;
        _db.Entry(record).Property(x => x.CreatedAt).IsModified = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
