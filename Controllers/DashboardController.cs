using Microsoft.AspNetCore.Mvc;
using QualityAudit.Models;
using QualityAudit.Services;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DatabaseService _db;

    public DashboardController(DatabaseService db) => _db = db;

    /// <summary>KPIs, breakdown charts, and recent shifts for the given date range.</summary>
    [HttpGet("summary")]
    public async Task<DashboardSummary> Summary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (f, t) = ResolveRange(from, to);
        return await _db.GetSummaryAsync(f, t);
    }

    /// <summary>The failure board — all NOK line checks for the range, severity 3 first.</summary>
    [HttpGet("failures")]
    public async Task<IEnumerable<FailureRecord>> Failures([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var (f, t) = ResolveRange(from, to);
        return await _db.GetFailuresAsync(f, t);
    }

    // Defaults to the last 30 days when either bound is omitted.
    private static (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);
        var fromDate = from ?? toDate.AddDays(-30);
        return (fromDate, toDate);
    }
}
