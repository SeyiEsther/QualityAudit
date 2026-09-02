using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Models;
using QualityAudit.Services;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/coverage")]
public class CoverageController : ControllerBase
{
    private readonly QualityAuditContext _db;

    public CoverageController(QualityAuditContext db) => _db = db;

    /// <summary>
    /// Per audit item for the chosen week: severity, expected vs actual checks, the shortfall,
    /// a per-day tick count across the Tuesday..Monday audit week, and who/which shifts covered it.
    /// Under-target items are returned first.
    /// </summary>
    [HttpGet]
    public async Task<CoverageResponse> Get([FromQuery] int departmentId, [FromQuery] DateOnly? weekStarting)
    {
        var week = weekStarting.HasValue ? WeekHelper.WeekStarting(weekStarting.Value) : WeekHelper.ThisWeek();
        var days = Enumerable.Range(0, 7).Select(i => week.AddDays(i)).ToList();

        var items = await _db.AuditItems.AsNoTracking()
            .Where(i => i.DepartmentId == departmentId && i.IsActive)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();
        var itemIds = items.Select(i => i.Id).ToList();

        var defaults = items.ToDictionary(i => i.Id, i => i.DefaultSeverity);
        var assignments = await _db.SeverityAssignments.AsNoTracking()
            .Where(a => itemIds.Contains(a.AuditItemId) && a.WeekStarting <= week).ToListAsync();
        var byItem = SeverityMath.GroupByItem(assignments);
        var checksPerWeek = await _db.SeverityLevels.AsNoTracking().ToDictionaryAsync(s => (int)s.Severity, s => s.ChecksPerWeek);

        var rows = await _db.Results.AsNoTracking()
            .Where(r => r.Submission!.IsComplete && r.Submission.DepartmentId == departmentId && r.Submission.WeekStarting == week)
            .Select(r => new { r.AuditItemId, r.Submission!.AuditDate, r.Submission.Shift, r.Submission.Auditor, r.Outcome })
            .ToListAsync();
        var byAuditItem = rows.GroupBy(r => r.AuditItemId).ToDictionary(g => g.Key, g => g.ToList());

        var response = new CoverageResponse
        {
            DepartmentId = departmentId,
            WeekStarting = week,
            Days = days.Select(d => d.ToString("yyyy-MM-dd")).ToList(),
            ItemCount = items.Count
        };

        foreach (var item in items)
        {
            var sev = SeverityMath.Resolve(byItem, defaults, item.Id, week);
            var expected = checksPerWeek.TryGetValue(sev, out var cpw) ? cpw : 0;
            byAuditItem.TryGetValue(item.Id, out var itemRows);
            itemRows ??= new();

            var audited = itemRows.Where(r => r.Outcome == "OK" || r.Outcome == "NOT_OK").ToList();
            var dayCounts = days.Select(d => audited.Count(r => r.AuditDate == d)).ToList();

            response.Items.Add(new CoverageItem
            {
                AuditItemId = item.Id,
                DisplayName = item.DisplayName,
                Location = item.Location,
                SortOrder = item.SortOrder,
                Severity = sev,
                Expected = expected,
                Actual = audited.Count,
                Shortfall = Math.Max(0, expected - audited.Count),
                DayCounts = dayCounts,
                Shifts = itemRows.Select(r => r.Shift).Distinct().ToList(),
                Auditors = itemRows.Select(r => r.Auditor).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().ToList()
            });
        }

        response.ItemsAtTarget = response.Items.Count(i => i.Actual >= i.Expected);
        // Under-target first, then by position.
        response.Items = response.Items
            .OrderByDescending(i => i.Shortfall > 0)
            .ThenByDescending(i => i.Shortfall)
            .ThenBy(i => i.SortOrder)
            .ToList();

        return response;
    }
}
