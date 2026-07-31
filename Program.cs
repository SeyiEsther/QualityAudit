using QualityAudit.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC controllers for the /api endpoints. System.Text.Json defaults to camelCase,
// which matches the JSON shapes the frontend expects.
builder.Services.AddControllers();

// Single data-access service (Dapper over Microsoft.Data.SqlClient). Stateless —
// it opens a fresh connection per call — so a singleton is fine.
builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

// Serve wwwroot/index.html as the single-page app, then wire up the API.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
