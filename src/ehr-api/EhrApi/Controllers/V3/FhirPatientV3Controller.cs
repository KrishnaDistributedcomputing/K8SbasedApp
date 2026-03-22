using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using EhrApi.Models;
using EhrApi.Models.V3;
using Asp.Versioning;

namespace EhrApi.Controllers.V3;

/// <summary>
/// FHIR R4 compliant patient resource endpoints.
/// Preview — returns FHIR-aligned JSON representations of EHR patients.
/// </summary>
[ApiVersion(3.0)]
[ApiController]
[Route("api/v{version:apiVersion}/fhir/Patient")]
public class FhirPatientV3Controller : ControllerBase
{
    private readonly EhrDbContext _db;
    public FhirPatientV3Controller(EhrDbContext db) => _db = db;

    /// <summary>Search patients — FHIR Bundle response</summary>
    [HttpGet]
    public async Task<ActionResult<FhirBundle>> Search(
        [FromQuery] string? family, [FromQuery] string? given,
        [FromQuery] string? gender, [FromQuery] int _count = 20, [FromQuery] int _offset = 0)
    {
        var query = _db.Patients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(family)) query = query.Where(p => p.LastName.Contains(family));
        if (!string.IsNullOrWhiteSpace(given)) query = query.Where(p => p.FirstName.Contains(given));
        if (!string.IsNullOrWhiteSpace(gender)) query = query.Where(p => p.Gender.ToLower() == gender.ToLower());

        var total = await query.CountAsync();
        var patients = await query.OrderBy(p => p.LastName).Skip(_offset).Take(_count).ToListAsync();

        var entries = patients.Select(p => new FhirBundleEntry(
            $"Patient/{p.Id}",
            MapToFhir(p))).ToArray();

        return Ok(new FhirBundle("Bundle", "searchset", total,
            [new FhirBundleLink("self", $"{Request.Scheme}://{Request.Host}{Request.Path}")],
            entries));
    }

    /// <summary>Read patient by ID — FHIR Patient resource</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<FhirPatientResource>> Read(int id)
    {
        var p = await _db.Patients.FindAsync(id);
        if (p is null) return NotFound();
        return Ok(MapToFhir(p));
    }

    private static FhirPatientResource MapToFhir(Patient p) => new(
        "Patient", p.Id.ToString(),
        new FhirMeta("1", p.UpdatedAt.ToString("O"),
            ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]),
        [new FhirHumanName("official", p.LastName, [p.FirstName])],
        p.Gender.ToLower(),
        p.DateOfBirth.ToString("yyyy-MM-dd"),
        p.Phone != null || p.Email != null
            ? [.. (p.Phone != null ? [new FhirContactPoint("phone", p.Phone, "home")] : Array.Empty<FhirContactPoint>()),
               .. (p.Email != null ? [new FhirContactPoint("email", p.Email, "home")] : Array.Empty<FhirContactPoint>())]
            : null,
        p.Address != null ? [new FhirAddress("home", [p.Address], "", "", "")] : null,
        p.InsuranceId != null
            ? [new FhirIdentifier("http://ehr.example.com/insurance", p.InsuranceId)]
            : null);
}
