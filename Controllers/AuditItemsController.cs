using Microsoft.AspNetCore.Mvc;
using QualityAudit.Models;
using QualityAudit.Services;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/audit-items")]
public class AuditItemsController : ControllerBase
{
    private readonly DatabaseService _db;

    public AuditItemsController(DatabaseService db) => _db = db;

    /// <summary>All active audit items, ordered by SortOrder. The form is built from this.</summary>
    [HttpGet]
    public async Task<IEnumerable<AuditItem>> Get() => await _db.GetAuditItemsAsync();
}
