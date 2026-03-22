using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.OpenApi.Models;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL
var connStr = builder.Configuration.GetConnectionString("EhrDb")
    ?? Environment.GetEnvironmentVariable("EHR_DB_CONNECTION")
    ?? "Host=localhost;Database=ehrdb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<EhrDbContext>(options =>
    options.UseNpgsql(connStr));

builder.Services.AddControllers()
    .AddJsonOptions(o => {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// API Versioning
builder.Services.AddApiVersioning(options => {
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version"));
}).AddApiExplorer(options => {
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title = "EHR API", Version = "v1",
        Description = "Electronic Health Record System API — Stable release with full CRUD operations for patients, providers, appointments, medical records, prescriptions, lab results, allergies, and vitals.",
        Contact = new OpenApiContact { Name = "EHR Platform Team", Email = "ehr-platform@example.com" }
    });
    c.SwaggerDoc("v2", new OpenApiInfo {
        Title = "EHR API", Version = "v2",
        Description = "Enhanced API with pagination, filtering, patient summaries, clinical dashboard, and HL7 FHIR-aligned DTOs. Adds bulk operations and advanced search.",
        Contact = new OpenApiContact { Name = "EHR Platform Team", Email = "ehr-platform@example.com" }
    });
    c.SwaggerDoc("v3", new OpenApiInfo {
        Title = "EHR API", Version = "v3",
        Description = "Future iteration — AI-powered clinical decision support, predictive risk scoring, FHIR R4 interoperability, real-time streaming endpoints, and GraphQL gateway. Preview only.",
        Contact = new OpenApiContact { Name = "EHR Platform Team", Email = "ehr-platform@example.com" }
    });
});

builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Auto-migrate database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EhrDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c => {
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EHR API v1 — Stable");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "EHR API v2 — Enhanced");
    c.SwaggerEndpoint("/swagger/v3/swagger.json", "EHR API v3 — Future Preview");
});

app.UseCors();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ehr-api", timestamp = DateTime.UtcNow }));

app.Run();
