using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Models;
using QualityAudit.Services;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly QualityAuditContext _db;
    private readonly UserContext _user;

    public AdminController(QualityAuditContext db, UserContext user)
    {
        _db = db;
        _user = user;
    }

    private async Task<IActionResult?> GuardAsync() =>
        await _user.IsAdminAsync() ? null : StatusCode(403, new { error = "Admin access required." });

    // =======================================================================
    // Weekly severity review
    // =======================================================================

    /// <summary>
    /// Every active machine with the severity in force for the chosen week and the previous
    /// week (so the reviewer can see what changed). Defaults to the coming Monday.
    /// </summary>
    [HttpGet("severities")]
    public async Task<IActionResult> GetSeverities([FromQuery] DateOnly? weekStarting, [FromQuery] int? departmentId)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;

        var week = weekStarting.HasValue ? WeekHelper.MondayOf(weekStarting.Value) : WeekHelper.ThisMonday();
        var prev = week.AddDays(-7);

        var itemsQuery = _db.AuditItems.AsNoTracking().Where(i => i.IsActive);
        if (departmentId is > 0)
            itemsQuery = itemsQuery.Where(i => i.DepartmentId == departmentId);
        var items = await itemsQuery.OrderBy(i => i.DepartmentId).ThenBy(i => i.SortOrder).ToListAsync();

        var itemIds = items.Select(i => i.Id).ToList();
        var assignments = await _db.SeverityAssignments.AsNoTracking()
            .Where(a => itemIds.Contains(a.AuditItemId))
            .ToListAsync();

        byte Resolve(int itemId, DateOnly upto, byte fallback)
        {
            var latest = assignments
                .Where(a => a.AuditItemId == itemId && a.WeekStarting <= upto)
                .OrderByDescending(a => a.WeekStarting)
                .FirstOrDefault();
            return latest?.Severity ?? fallback;
        }

        var rows = items.Select(i =>
        {
            var explicitThisWeek = assignments.FirstOrDefault(a => a.AuditItemId == i.Id && a.WeekStarting == week);
            return new SeverityReviewRow
            {
                AuditItemId = i.Id,
                DepartmentId = i.DepartmentId,
                DisplayName = i.DisplayName,
                Location = i.Location,
                Severity = explicitThisWeek?.Severity ?? Resolve(i.Id, week, i.DefaultSeverity),
                IsFallback = explicitThisWeek is null,
                PreviousSeverity = Resolve(i.Id, prev, i.DefaultSeverity)
            };
        }).ToList();

        return Ok(new { weekStarting = week, previousWeekStarting = prev, rows });
    }

    /// <summary>Bulk upsert the week's RAG assignments, keyed on (AuditItemId, WeekStarting).</summary>
    [HttpPost("severities")]
    public async Task<IActionResult> SetSeverities([FromBody] SeverityBulkRequest? req)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        if (req is null || req.Assignments.Count == 0)
            return BadRequest(new { error = "No assignments supplied." });

        var week = WeekHelper.MondayOf(req.WeekStarting);
        var setBy = string.IsNullOrWhiteSpace(req.SetBy) ? _user.CurrentUsername : req.SetBy.Trim();
        var now = DateTime.UtcNow;

        var itemIds = req.Assignments.Select(a => a.AuditItemId).Distinct().ToList();
        var existing = await _db.SeverityAssignments
            .Where(a => a.WeekStarting == week && itemIds.Contains(a.AuditItemId))
            .ToListAsync();

        foreach (var input in req.Assignments)
        {
            if (input.Severity is < 1 or > 3) continue;
            var row = existing.FirstOrDefault(a => a.AuditItemId == input.AuditItemId);
            if (row is null)
            {
                _db.SeverityAssignments.Add(new SeverityAssignment
                {
                    AuditItemId = input.AuditItemId,
                    WeekStarting = week,
                    Severity = input.Severity,
                    SetBy = setBy,
                    SetAt = now
                });
            }
            else
            {
                row.Severity = input.Severity;
                row.SetBy = setBy;
                row.SetAt = now;
            }
        }

        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not save severities: " + ex.Message }); }

        return Ok(new { weekStarting = week, saved = req.Assignments.Count });
    }

    // =======================================================================
    // Machines (add / edit / retire — soft delete only)
    // =======================================================================

    [HttpGet("audit-items")]
    public async Task<IActionResult> GetAuditItems([FromQuery] int? departmentId)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;

        var query = _db.AuditItems.AsNoTracking().AsQueryable();
        if (departmentId is > 0)
            query = query.Where(i => i.DepartmentId == departmentId);

        return Ok(await query.OrderBy(i => i.DepartmentId).ThenBy(i => i.SortOrder).ToListAsync());
    }

    [HttpPost("audit-items")]
    public async Task<IActionResult> CreateAuditItem([FromBody] AuditItemInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        if (string.IsNullOrWhiteSpace(input.DisplayName) || input.DepartmentId <= 0)
            return BadRequest(new { error = "Department and name are required." });

        var item = new AuditItem
        {
            DepartmentId = input.DepartmentId,
            DisplayName = input.DisplayName.Trim(),
            Location = string.IsNullOrWhiteSpace(input.Location) ? "Ph1" : input.Location.Trim(),
            DefaultSeverity = input.DefaultSeverity is >= 1 and <= 3 ? input.DefaultSeverity : (byte)1,
            SpecialMeasures = input.SpecialMeasures,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        };
        _db.AuditItems.Add(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("audit-items/{id:int}")]
    public async Task<IActionResult> UpdateAuditItem(int id, [FromBody] AuditItemInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;

        var item = await _db.AuditItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound(new { error = $"Machine {id} not found." });

        item.DepartmentId = input.DepartmentId > 0 ? input.DepartmentId : item.DepartmentId;
        if (!string.IsNullOrWhiteSpace(input.DisplayName)) item.DisplayName = input.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(input.Location)) item.Location = input.Location.Trim();
        if (input.DefaultSeverity is >= 1 and <= 3) item.DefaultSeverity = input.DefaultSeverity;
        item.SpecialMeasures = input.SpecialMeasures;
        item.SortOrder = input.SortOrder;
        item.IsActive = input.IsActive;   // set false to retire (soft delete)

        await _db.SaveChangesAsync();
        return Ok(item);
    }

    // =======================================================================
    // Failure modes (add / edit / retire — soft delete only)
    // =======================================================================

    [HttpGet("failure-modes")]
    public async Task<IActionResult> GetFailureModes()
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        return Ok(await _db.FailureModes.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync());
    }

    [HttpPost("failure-modes")]
    public async Task<IActionResult> CreateFailureMode([FromBody] FailureModeInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Label))
            return BadRequest(new { error = "Code and label are required." });

        var mode = new FailureMode
        {
            Code = input.Code.Trim(),
            Label = input.Label.Trim(),
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        };
        _db.FailureModes.Add(mode);
        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not add failure mode: " + ex.Message }); }
        return Ok(mode);
    }

    [HttpPut("failure-modes/{id:int}")]
    public async Task<IActionResult> UpdateFailureMode(int id, [FromBody] FailureModeInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;

        var mode = await _db.FailureModes.FirstOrDefaultAsync(f => f.Id == id);
        if (mode is null) return NotFound(new { error = $"Failure mode {id} not found." });

        if (!string.IsNullOrWhiteSpace(input.Code)) mode.Code = input.Code.Trim();
        if (!string.IsNullOrWhiteSpace(input.Label)) mode.Label = input.Label.Trim();
        mode.SortOrder = input.SortOrder;
        mode.IsActive = input.IsActive;   // set false to retire

        await _db.SaveChangesAsync();
        return Ok(mode);
    }
}
