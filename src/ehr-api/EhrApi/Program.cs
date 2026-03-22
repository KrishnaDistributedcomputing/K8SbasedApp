using Microsoft.EntityFrameworkCore;
using EhrApi.Data;
using System.Text.Json.Serialization;

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new() { Title = "EHR API", Version = "v1", Description = "Electronic Health Record System API" });
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
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "EHR API v1"));

app.UseCors();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ehr-api", timestamp = DateTime.UtcNow }));

app.Run();
