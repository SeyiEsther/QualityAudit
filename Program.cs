using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core over the existing RittalQualityAudit v2 database. We never migrate — the
// context maps onto tables and views that already exist (see QualityAuditContext).
builder.Services.AddDbContext<QualityAuditContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RittalQualityAudit")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserContext>();

// MVC controllers for the /api endpoints. System.Text.Json defaults to camelCase,
// which matches the JSON shapes the frontend expects.
builder.Services.AddControllers();

var app = builder.Build();

// Serve wwwroot/index.html as the single-page app, then wire up the API.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
