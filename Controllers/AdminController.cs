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

    // ===================== Weekly severity review =====================

    [HttpGet("severities")]
    public async Task<IActionResult> GetSeverities([FromQuery] DateOnly? weekStarting, [FromQuery] int? departmentId)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;

        var week = weekStarting.HasValue ? WeekHelper.WeekStarting(weekStarting.Value) : WeekHelper.NextWeek();
        var prev = week.AddDays(-7);

        var itemsQuery = _db.AuditItems.AsNoTracking().Where(i => i.IsActive);
        if (departmentId is > 0) itemsQuery = itemsQuery.Where(i => i.DepartmentId == departmentId);
        var items = await itemsQuery.OrderBy(i => i.DepartmentId).ThenBy(i => i.SortOrder).ToListAsync();

        var itemIds = items.Select(i => i.Id).ToList();
        var assignments = await _db.SeverityAssignments.AsNoTracking()
            .Where(a => itemIds.Contains(a.AuditItemId)).ToListAsync();

        byte Resolve(int itemId, DateOnly upto, byte fallback) =>
            assignments.Where(a => a.AuditItemId == itemId && a.WeekStarting <= upto)
                       .OrderByDescending(a => a.WeekStarting).FirstOrDefault()?.Severity ?? fallback;

        var rows = items.Select(i =>
        {
            var thisWeek = assignments.FirstOrDefault(a => a.AuditItemId == i.Id && a.WeekStarting == week);
            return new SeverityReviewRow
            {
                AuditItemId = i.Id,
                DepartmentId = i.DepartmentId,
                DisplayName = i.DisplayName,
                Location = i.Location,
                SortOrder = i.SortOrder,
                Severity = thisWeek?.Severity ?? Resolve(i.Id, week, i.DefaultSeverity),
                IsFallback = thisWeek is null,
                PreviousSeverity = Resolve(i.Id, prev, i.DefaultSeverity)
            };
        }).ToList();

        return Ok(new { weekStarting = week, previousWeekStarting = prev, rows });
    }

    [HttpPost("severities")]
    public async Task<IActionResult> SetSeverities([FromBody] SeverityBulkRequest? req)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        if (req is null || req.Assignments.Count == 0) return BadRequest(new { error = "No assignments supplied." });

        var week = WeekHelper.WeekStarting(req.WeekStarting);
        var setBy = string.IsNullOrWhiteSpace(req.SetBy) ? _user.CurrentUser : req.SetBy.Trim();
        var now = DateTime.UtcNow;

        var itemIds = req.Assignments.Select(a => a.AuditItemId).Distinct().ToList();
        var existing = await _db.SeverityAssignments
            .Where(a => a.WeekStarting == week && itemIds.Contains(a.AuditItemId)).ToListAsync();

        foreach (var input in req.Assignments)
        {
            if (input.Severity is < 1 or > 3) continue;
            var row = existing.FirstOrDefault(a => a.AuditItemId == input.AuditItemId);
            if (row is null)
                _db.SeverityAssignments.Add(new SeverityAssignment { AuditItemId = input.AuditItemId, WeekStarting = week, Severity = input.Severity, SetBy = setBy, SetAt = now });
            else { row.Severity = input.Severity; row.SetBy = setBy; row.SetAt = now; }
        }

        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not save severities: " + ex.Message }); }
        return Ok(new { weekStarting = week, saved = req.Assignments.Count });
    }

    // ===================== Machines =====================

    [HttpGet("audit-items")]
    public async Task<IActionResult> GetAuditItems([FromQuery] int? departmentId)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        var q = _db.AuditItems.AsNoTracking().AsQueryable();
        if (departmentId is > 0) q = q.Where(i => i.DepartmentId == departmentId);
        return Ok(await q.OrderBy(i => i.DepartmentId).ThenBy(i => i.SortOrder).ToListAsync());
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
        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not add machine: " + ex.Message }); }
        return Ok(item);
    }

    [HttpPut("audit-items/{id:int}")]
    public async Task<IActionResult> UpdateAuditItem(int id, [FromBody] AuditItemInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        var item = await _db.AuditItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item is null) return NotFound(new { error = $"Machine {id} not found." });

        if (input.DepartmentId > 0) item.DepartmentId = input.DepartmentId;
        if (!string.IsNullOrWhiteSpace(input.DisplayName)) item.DisplayName = input.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(input.Location)) item.Location = input.Location.Trim();
        if (input.DefaultSeverity is >= 1 and <= 3) item.DefaultSeverity = input.DefaultSeverity;
        item.SpecialMeasures = input.SpecialMeasures;
        item.SortOrder = input.SortOrder;
        item.IsActive = input.IsActive;   // false = retire

        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not save machine: " + ex.Message }); }
        return Ok(item);
    }

    // ===================== Users =====================

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        return Ok(await _db.AuditUsers.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync());
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] UserInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        if (string.IsNullOrWhiteSpace(input.DisplayName)) return BadRequest(new { error = "A name is required." });

        var user = new AuditUser
        {
            DisplayName = input.DisplayName.Trim(),
            Email = NullIfBlank(input.Email),
            Username = NullIfBlank(input.Username),
            IsAdmin = input.IsAdmin,
            IsActive = input.IsActive
        };
        _db.AuditUsers.Add(user);
        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not add user: " + ex.Message }); }
        return Ok(user);
    }

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        var user = await _db.AuditUsers.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound(new { error = $"User {id} not found." });

        if (!string.IsNullOrWhiteSpace(input.DisplayName)) user.DisplayName = input.DisplayName.Trim();
        user.Email = NullIfBlank(input.Email);
        user.Username = NullIfBlank(input.Username);
        user.IsAdmin = input.IsAdmin;
        user.IsActive = input.IsActive;

        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not save user: " + ex.Message }); }
        return Ok(user);
    }

    // ===================== Customers =====================

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers()
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        return Ok(await _db.Customers.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync());
    }

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] CustomerInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        if (string.IsNullOrWhiteSpace(input.Name)) return BadRequest(new { error = "A name is required." });

        var customer = new Customer { Name = input.Name.Trim(), SortOrder = input.SortOrder, IsActive = input.IsActive };
        _db.Customers.Add(customer);
        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not add customer: " + ex.Message }); }
        return Ok(customer);
    }

    [HttpPut("customers/{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CustomerInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer is null) return NotFound(new { error = $"Customer {id} not found." });

        if (!string.IsNullOrWhiteSpace(input.Name)) customer.Name = input.Name.Trim();
        customer.SortOrder = input.SortOrder;
        customer.IsActive = input.IsActive;

        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not save customer: " + ex.Message }); }
        return Ok(customer);
    }

    // ===================== Check points =====================

    [HttpGet("check-points")]
    public async Task<IActionResult> GetCheckPoints()
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        return Ok(await _db.CheckPoints.AsNoTracking().OrderBy(c => c.SortOrder).ToListAsync());
    }

    [HttpPost("check-points")]
    public async Task<IActionResult> CreateCheckPoint([FromBody] CheckPointInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.Label))
            return BadRequest(new { error = "Code and label are required." });

        var cp = new CheckPoint
        {
            Code = input.Code.Trim(),
            Label = input.Label.Trim(),
            MinSeverity = input.MinSeverity is >= 1 and <= 3 ? input.MinSeverity : (byte)1,
            Conditional = input.Conditional,
            SortOrder = input.SortOrder,
            IsActive = input.IsActive
        };
        _db.CheckPoints.Add(cp);
        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not add check point: " + ex.Message }); }
        return Ok(cp);
    }

    [HttpPut("check-points/{id:int}")]
    public async Task<IActionResult> UpdateCheckPoint(int id, [FromBody] CheckPointInput input)
    {
        var guard = await GuardAsync(); if (guard != null) return guard;
        var cp = await _db.CheckPoints.FirstOrDefaultAsync(c => c.Id == id);
        if (cp is null) return NotFound(new { error = $"Check point {id} not found." });

        if (!string.IsNullOrWhiteSpace(input.Code)) cp.Code = input.Code.Trim();
        if (!string.IsNullOrWhiteSpace(input.Label)) cp.Label = input.Label.Trim();
        if (input.MinSeverity is >= 1 and <= 3) cp.MinSeverity = input.MinSeverity;
        cp.Conditional = input.Conditional;
        cp.SortOrder = input.SortOrder;
        cp.IsActive = input.IsActive;

        try { await _db.SaveChangesAsync(); }
        catch (Exception ex) { return StatusCode(500, new { error = "Could not save check point: " + ex.Message }); }
        return Ok(cp);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
