using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Models;
using QualityAudit.Services;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/form")]
public class FormController : ControllerBase
{
    private readonly QualityAuditContext _db;

    public FormController(QualityAuditContext db) => _db = db;

    /// <summary>
    /// Everything the entry form needs in one call. Machine severity is resolved for the
    /// requested date's week directly from SeverityAssignments (NOT vw_CurrentSeverity, which
    /// only knows today's week), falling back to AuditItems.DefaultSeverity.
    /// </summary>
    [HttpGet("{departmentId:int}")]
    public async Task<IActionResult> Get(int departmentId, [FromQuery] DateOnly? date)
    {
        var department = await _db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == departmentId);
        if (department is null)
            return NotFound(new { error = $"Department {departmentId} not found." });

        var auditDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var weekStarting = WeekHelper.WeekStarting(auditDate);

        var items = await _db.AuditItems.AsNoTracking()
            .Where(i => i.DepartmentId == departmentId && i.IsActive)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();

        var itemIds = items.Select(i => i.Id).ToList();
        var assignments = await _db.SeverityAssignments.AsNoTracking()
            .Where(a => itemIds.Contains(a.AuditItemId) && a.WeekStarting <= weekStarting)
            .ToListAsync();

        var live = assignments
            .GroupBy(a => a.AuditItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.WeekStarting).First().Severity);

        var machines = items.Select(i => new MachineDto
        {
            Id = i.Id,
            DisplayName = i.DisplayName,
            Location = i.Location,
            SpecialMeasures = i.SpecialMeasures,
            SortOrder = i.SortOrder,
            Severity = live.TryGetValue(i.Id, out var sev) ? sev : i.DefaultSeverity,
            IsFallback = !live.ContainsKey(i.Id)
        }).ToList();

        var response = new FormResponse
        {
            Department = department,
            WeekStarting = weekStarting,
            Machines = machines,
            SeverityLevels = await _db.SeverityLevels.AsNoTracking().OrderBy(s => s.Severity).ToListAsync(),
            CheckPoints = await _db.CheckPoints.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(),
            Customers = await _db.Customers.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(),
            ActionTypes = await _db.ActionTypes.AsNoTracking().Where(a => a.IsActive).OrderBy(a => a.SortOrder).ToListAsync(),
            NotAuditedReasons = await _db.NotAuditedReasons.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.SortOrder).ToListAsync(),
            AuditUsers = await _db.AuditUsers.AsNoTracking().Where(u => u.IsActive).OrderBy(u => u.DisplayName)
                .Select(u => new UserDto { Id = u.Id, DisplayName = u.DisplayName, Email = u.Email, IsAdmin = u.IsAdmin })
                .ToListAsync()
        };

        return Ok(response);
    }
}
