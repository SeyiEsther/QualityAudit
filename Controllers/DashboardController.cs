using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Models;
using QualityAudit.Services;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly QualityAuditContext _db;

    public DashboardController(QualityAuditContext db) => _db = db;

    // GET /api/dashboard/summary?departmentId=&weekStarting=
    [HttpGet("summary")]
    public async Task<DashboardSummary> Summary([FromQuery] int departmentId, [FromQuery] DateOnly? weekStarting)
    {
        var week = weekStarting.HasValue ? WeekHelper.WeekStarting(weekStarting.Value) : WeekHelper.ThisWeek();
        var prev = week.AddDays(-7);

        var summaries = await _db.Set<WeeklySummary>().AsNoTracking()
            .Where(v => v.DepartmentId == departmentId && (v.WeekStarting == week || v.WeekStarting == prev))
            .ToListAsync();

        var compliance = await _db.Set<WeeklyCompliance>().AsNoTracking()
            .Where(v => v.DepartmentId == departmentId && v.WeekStarting == week)
            .OrderBy(v => v.SortOrder)
            .ToListAsync();

        var result = new DashboardSummary
        {
            WeekStarting = week,
            PreviousWeekStarting = prev,
            Current = ToWeekStats(week, summaries.FirstOrDefault(s => s.WeekStarting == week)),
            Previous = ToWeekStats(prev, summaries.FirstOrDefault(s => s.WeekStarting == prev)),
            Compliance = compliance.Select(c => new ComplianceRow
            {
                AuditItemId = c.AuditItemId,
                DisplayName = c.DisplayName,
                SortOrder = c.SortOrder,
                Severity = c.Severity,
                Expected = c.ExpectedChecks,
                Actual = c.ActualChecks,
                Pass = c.PassCount,
                Fail = c.FailCount,
                NotAudited = c.NotAuditedCount,
                UnderTarget = c.ActualChecks < c.ExpectedChecks
            }).ToList()
        };

        foreach (var sev in new[] { 3, 2, 1 })
        {
            var rows = compliance.Where(c => c.Severity == sev).ToList();
            var pass = rows.Sum(c => c.PassCount);
            var fail = rows.Sum(c => c.FailCount);
            result.PassBySeverity.Add(new SeverityRate
            {
                Severity = sev, Total = pass + fail, Pass = pass, Fail = fail, Rate = Rate(pass, fail)
            });
        }

        // Pass rate by location — no view provides it; compute from base rows for the week,
        // grouped by the machine's Location (department-agnostic: handles 'Assembly' too).
        var pairs = await _db.Results.AsNoTracking()
            .Where(r => (r.Outcome == "OK" || r.Outcome == "NOT_OK")
                        && r.Submission!.IsComplete
                        && r.Submission.DepartmentId == departmentId
                        && r.Submission.WeekStarting == week)
            .Select(r => new { r.AuditItem!.Location, r.Outcome })
            .ToListAsync();

        result.PassByLocation = pairs
            .GroupBy(x => x.Location)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var pass = g.Count(x => x.Outcome == "OK");
                var fail = g.Count(x => x.Outcome == "NOT_OK");
                return new LocationRate { Location = g.Key, Total = pass + fail, Pass = pass, Fail = fail, Rate = Rate(pass, fail) };
            })
            .ToList();

        return result;
    }

    // GET /api/dashboard/attainment?departmentId=&from=&to=
    // Headline figure: actual audited checks (OK/NOT_OK) against expected checks, where
    // expected sums each week's per-item ChecksPerWeek for the severity in force that week.
    [HttpGet("attainment")]
    public async Task<AttainmentResult> Attainment([FromQuery] int departmentId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var f = from ?? new DateOnly(today.Year, today.Month, 1);
        var t = to ?? today;
        var weeks = WeekHelper.WeeksBetween(f, t).ToList();

        var items = await _db.AuditItems.AsNoTracking()
            .Where(i => i.DepartmentId == departmentId && i.IsActive).ToListAsync();
        var itemIds = items.Select(i => i.Id).ToList();
        var defaults = items.ToDictionary(i => i.Id, i => i.DefaultSeverity);
        var maxWeek = weeks.Count > 0 ? weeks[^1] : WeekHelper.WeekStarting(t);
        var assignments = await _db.SeverityAssignments.AsNoTracking()
            .Where(a => itemIds.Contains(a.AuditItemId) && a.WeekStarting <= maxWeek).ToListAsync();
        var byItem = SeverityMath.GroupByItem(assignments);
        var checksPerWeek = await _db.SeverityLevels.AsNoTracking().ToDictionaryAsync(s => (int)s.Severity, s => s.ChecksPerWeek);

        var expected = 0;
        foreach (var w in weeks)
            foreach (var item in items)
            {
                var sev = SeverityMath.Resolve(byItem, defaults, item.Id, w);
                expected += checksPerWeek.TryGetValue(sev, out var c) ? c : 0;
            }

        var actual = await _db.Results.AsNoTracking().CountAsync(r =>
            (r.Outcome == "OK" || r.Outcome == "NOT_OK")
            && r.Submission!.IsComplete && r.Submission.DepartmentId == departmentId
            && weeks.Contains(r.Submission.WeekStarting));

        var target = await _db.Departments.Where(d => d.Id == departmentId).Select(d => d.TargetPercent).FirstOrDefaultAsync();

        return new AttainmentResult
        {
            DepartmentId = departmentId, From = f, To = t,
            ExpectedChecks = expected, ActualChecks = actual,
            AttainmentPct = expected == 0 ? 0m : Math.Round(100m * actual / expected, 1),
            TargetPct = target
        };
    }

    // GET /api/dashboard/failures?departmentId=&weekStarting=
    [HttpGet("failures")]
    public async Task<IEnumerable<VwFailure>> Failures([FromQuery] int? departmentId, [FromQuery] DateOnly? weekStarting)
    {
        var query = _db.Set<VwFailure>().AsNoTracking();

        if (weekStarting.HasValue)
        {
            var week = WeekHelper.WeekStarting(weekStarting.Value);
            query = query.Where(v => v.WeekStarting == week);
        }
        if (departmentId is > 0)
        {
            var name = await _db.Departments.Where(d => d.Id == departmentId).Select(d => d.Name).FirstOrDefaultAsync();
            query = query.Where(v => v.DepartmentName == name);
        }

        return await query.OrderByDescending(v => v.Severity).ThenByDescending(v => v.AuditDate).ToListAsync();
    }

    // GET /api/dashboard/by-customer?departmentId=&weekStarting=
    [HttpGet("by-customer")]
    public async Task<IEnumerable<FailuresByCustomer>> ByCustomer([FromQuery] int departmentId, [FromQuery] DateOnly? weekStarting)
    {
        var week = weekStarting.HasValue ? WeekHelper.WeekStarting(weekStarting.Value) : WeekHelper.ThisWeek();
        return await _db.Set<FailuresByCustomer>().AsNoTracking()
            .Where(v => v.DepartmentId == departmentId && v.WeekStarting == week)
            .OrderByDescending(v => v.FailCount)
            .ToListAsync();
    }

    // GET /api/dashboard/check-points?departmentId=&weekStarting=
    [HttpGet("check-points")]
    public async Task<IEnumerable<CheckPointFailure>> CheckPoints([FromQuery] int departmentId, [FromQuery] DateOnly? weekStarting)
    {
        var week = weekStarting.HasValue ? WeekHelper.WeekStarting(weekStarting.Value) : WeekHelper.ThisWeek();
        return await _db.Set<CheckPointFailure>().AsNoTracking()
            .Where(v => v.DepartmentId == departmentId && v.WeekStarting == week)
            .OrderByDescending(v => v.FailCount)
            .ToListAsync();
    }

    // GET /api/dashboard/overview?departmentId=&months=12
    [HttpGet("overview")]
    public async Task<IEnumerable<OverviewMonth>> Overview([FromQuery] int departmentId, [FromQuery] int months = 12)
    {
        if (months < 1) months = 12;
        var firstOfThisMonth = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var from = firstOfThisMonth.AddMonths(-(months - 1));

        var raw = await _db.Results.AsNoTracking()
            .Where(r => (r.Outcome == "OK" || r.Outcome == "NOT_OK")
                        && r.Submission!.IsComplete
                        && r.Submission.DepartmentId == departmentId
                        && r.Submission.AuditDate >= from)
            .Select(r => new { r.Submission!.AuditDate, r.Outcome })
            .ToListAsync();

        var byKey = raw
            .GroupBy(x => (x.AuditDate.Year, x.AuditDate.Month))
            .ToDictionary(g => g.Key, g => new
            {
                Pass = g.Count(x => x.Outcome == "OK"),
                Fail = g.Count(x => x.Outcome == "NOT_OK")
            });

        // Expected checks per month = sum of each week's per-item ChecksPerWeek, the week
        // assigned to the month of its Tuesday.
        var items = await _db.AuditItems.AsNoTracking()
            .Where(i => i.DepartmentId == departmentId && i.IsActive).ToListAsync();
        var itemIds = items.Select(i => i.Id).ToList();
        var defaults = items.ToDictionary(i => i.Id, i => i.DefaultSeverity);
        var assignments = await _db.SeverityAssignments.AsNoTracking()
            .Where(a => itemIds.Contains(a.AuditItemId) && a.WeekStarting <= today).ToListAsync();
        var byItem = SeverityMath.GroupByItem(assignments);
        var checksPerWeek = await _db.SeverityLevels.AsNoTracking().ToDictionaryAsync(s => (int)s.Severity, s => s.ChecksPerWeek);

        var expectedByMonth = new Dictionary<(int, int), int>();
        foreach (var w in WeekHelper.WeeksBetween(from, today))
        {
            var e = 0;
            foreach (var item in items)
            {
                var sev = SeverityMath.Resolve(byItem, defaults, item.Id, w);
                e += checksPerWeek.TryGetValue(sev, out var c) ? c : 0;
            }
            var key = (w.Year, w.Month);
            expectedByMonth[key] = (expectedByMonth.TryGetValue(key, out var v) ? v : 0) + e;
        }

        var list = new List<OverviewMonth>();
        for (var m = from; m <= firstOfThisMonth; m = m.AddMonths(1))
        {
            byKey.TryGetValue((m.Year, m.Month), out var g);
            var pass = g?.Pass ?? 0;
            var fail = g?.Fail ?? 0;
            var total = pass + fail;
            expectedByMonth.TryGetValue((m.Year, m.Month), out var expected);
            list.Add(new OverviewMonth
            {
                Month = $"{m.Year:0000}-{m.Month:00}",
                Total = total, Pass = pass, Fail = fail,
                Completed = total, Expected = expected,
                PassRate = Rate(pass, fail),
                FailRate = total == 0 ? 0m : Math.Round(100m * fail / total, 1)
            });
        }
        return list;
    }

    private static WeekStats ToWeekStats(DateOnly week, WeeklySummary? v) => new()
    {
        WeekStarting = week,
        ShiftsLogged = v?.ShiftsLogged ?? 0,
        TotalChecks = v?.TotalChecks ?? 0,
        PassCount = v?.PassCount ?? 0,
        FailCount = v?.FailCount ?? 0,
        NotAuditedCount = v?.NotAuditedCount ?? 0,
        PassRate = v?.PassRatePct ?? 0m
    };

    private static decimal Rate(int pass, int fail)
    {
        var total = pass + fail;
        return total == 0 ? 0m : Math.Round(100m * pass / total, 1);
    }
}
