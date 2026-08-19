using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Models;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/form")]
public class FormController : ControllerBase
{
    private readonly QualityAuditContext _db;

    public FormController(QualityAuditContext db) => _db = db;

    /// <summary>
    /// Everything the entry form needs in one call: the department, its machine list with
    /// this week's live severity (from vw_CurrentSeverity), the severity rule set, and the
    /// failure-mode / not-audited-reason dropdowns. The frontend builds the whole form from this.
    /// </summary>
    [HttpGet("{departmentId:int}")]
    public async Task<IActionResult> Get(int departmentId)
    {
        var department = await _db.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == departmentId);
        if (department is null)
            return NotFound(new { error = $"Department {departmentId} not found." });

        var machines = await _db.Set<CurrentSeverity>().AsNoTracking()
            .Where(c => c.DepartmentId == departmentId)
            .OrderBy(c => c.SortOrder)
            .Select(c => new MachineDto
            {
                Id = c.AuditItemId,
                DisplayName = c.DisplayName,
                Location = c.Location,
                SpecialMeasures = c.SpecialMeasures,
                SortOrder = c.SortOrder,
                Severity = c.Severity,
                IsFallback = c.IsFallback != 0
            })
            .ToListAsync();

        var severityLevels = await _db.SeverityLevels.AsNoTracking()
            .OrderBy(s => s.Severity).ToListAsync();

        var failureModes = await _db.FailureModes.AsNoTracking()
            .Where(f => f.IsActive).OrderBy(f => f.SortOrder).ToListAsync();

        var reasons = await _db.NotAuditedReasons.AsNoTracking()
            .Where(r => r.IsActive).OrderBy(r => r.SortOrder).ToListAsync();

        return Ok(new FormResponse
        {
            Department = department,
            Machines = machines,
            SeverityLevels = severityLevels,
            FailureModes = failureModes,
            NotAuditedReasons = reasons
        });
    }
}
