using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using EhrApi.Models;

namespace EhrApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidersController : ControllerBase
{
    private readonly EhrDbContext _db;
    public ProvidersController(EhrDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Provider>>> GetAll([FromQuery] string? specialty)
    {
        var query = _db.Providers.AsQueryable();
        if (!string.IsNullOrWhiteSpace(specialty))
            query = query.Where(p => p.Specialty.Contains(specialty));
        return await query.OrderBy(p => p.LastName).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Provider>> Get(int id)
    {
        var provider = await _db.Providers.FindAsync(id);
        return provider is null ? NotFound() : provider;
    }

    [HttpPost]
    public async Task<ActionResult<Provider>> Create(Provider provider)
    {
        _db.Providers.Add(provider);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = provider.Id }, provider);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Provider provider)
    {
        if (id != provider.Id) return BadRequest();
        _db.Entry(provider).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
