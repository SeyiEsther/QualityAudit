using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<QualityAuditContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RittalQualityAudit")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserContext>();
builder.Services.AddSingleton<AttachmentStorage>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
