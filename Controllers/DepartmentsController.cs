using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Models;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly QualityAuditContext _db;

    public DepartmentsController(QualityAuditContext db) => _db = db;

    /// <summary>Active departments (Sheet Metal, Assembly, ...) ordered for display.</summary>
    [HttpGet]
    public async Task<IEnumerable<Department>> Get() =>
        await _db.Departments.AsNoTracking()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name)
            .ToListAsync();
}
