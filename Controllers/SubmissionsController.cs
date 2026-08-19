using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QualityAudit.Data;
using QualityAudit.Models;
using QualityAudit.Services;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/submissions")]
public class SubmissionsController : ControllerBase
{
    private static readonly string[] ValidResults = { "OK", "NOT_OK", "NOT_AUDITED" };

    private readonly QualityAuditContext _db;

    public SubmissionsController(QualityAuditContext db) => _db = db;

    // -----------------------------------------------------------------------
    // POST /api/submissions
    // -----------------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SubmissionRequest? req)
    {
        if (req is null)
            return BadRequest(new { error = "Request body is missing." });
        if (req.DepartmentId <= 0)
            return BadRequest(new { error = "A department is required." });
        if (string.IsNullOrWhiteSpace(req.Shift) || string.IsNullOrWhiteSpace(req.Auditor))
            return BadRequest(new { error = "Shift and Auditor are required." });

        // Only rows the auditor actually addressed (a Result was picked) are saved.
        var picked = req.Results
            .Where(r => !string.IsNullOrWhiteSpace(r.Result))
            .ToList();
        if (picked.Count == 0)
            return BadRequest(new { error = "At least one machine must be checked before saving." });

        // Validate result values and the structured-requirement rules.
        var problems = new List<string>();
        foreach (var r in picked)
        {
            var value = r.Result!.Trim();
            if (!ValidResults.Contains(value))
            {
                problems.Add($"Item {r.AuditItemId}: '{value}' is not a valid result.");
                continue;
            }
            if (value == "NOT_OK" && r.FailureModeId is null or <= 0)
                problems.Add($"Item {r.AuditItemId}: a failure mode is required for a Not OK result.");
            if (value == "NOT_AUDITED" && r.NotAuditedReasonId is null or <= 0)
                problems.Add($"Item {r.AuditItemId}: a reason is required for a Not Audited result.");
        }
        if (problems.Count > 0)
            return BadRequest(new { error = "Some rows are incomplete.", details = problems });

        var weekStarting = WeekHelper.MondayOf(req.AuditDate);

        // Resolve the severity in force for each machine that week: the latest assignment
        // whose WeekStarting is on or before this audit's week, else the machine's default.
        var itemIds = picked.Select(r => r.AuditItemId).Distinct().ToList();

        var defaults = await _db.AuditItems.AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.DefaultSeverity);

        var assignmentRows = await _db.SeverityAssignments.AsNoTracking()
            .Where(a => itemIds.Contains(a.AuditItemId) && a.WeekStarting <= weekStarting)
            .ToListAsync();

        var liveSeverity = assignmentRows
            .GroupBy(a => a.AuditItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.WeekStarting).First().Severity);

        byte ResolveSeverity(int auditItemId) =>
            liveSeverity.TryGetValue(auditItemId, out var sev) ? sev
            : defaults.TryGetValue(auditItemId, out var def) ? def
            : (byte)1;

        var submission = new Submission
        {
            DepartmentId = req.DepartmentId,
            AuditDate = req.AuditDate,
            WeekStarting = weekStarting,           // computed server-side; client value ignored
            Shift = req.Shift.Trim(),
            Auditor = req.Auditor.Trim(),
            OtherNotes = NullIfBlank(req.OtherNotes),
            Results = picked.Select(r => new Result
            {
                AuditItemId = r.AuditItemId,
                SeverityAtAudit = ResolveSeverity(r.AuditItemId),   // snapshot — never joined live later
                Outcome = r.Result!.Trim(),
                FailureModeId = r.Result!.Trim() == "NOT_OK" ? r.FailureModeId : null,
                NotAuditedReasonId = r.Result!.Trim() == "NOT_AUDITED" ? r.NotAuditedReasonId : null,
                PartNo = NullIfBlank(r.PartNo),
                SerialNo = NullIfBlank(r.SerialNo),
                CritDimsChecked = NullIfBlank(r.CritDimsChecked),
                QmIpVersionChecked = NullIfBlank(r.QmIpVersionChecked),
                Comment = NullIfBlank(r.Comment),
                Customer = NullIfBlank(r.Customer),
                ActionTaken = NullIfBlank(r.ActionTaken)
            }).ToList()
        };

        try
        {
            _db.Submissions.Add(submission);
            // One SaveChanges = one transaction: the submission and all its results commit
            // together, or nothing does.
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Could not save the shift audit: " + ex.Message });
        }

        return Ok(new { id = submission.Id });
    }

    // -----------------------------------------------------------------------
    // GET /api/submissions?from=&to=&departmentId=&shift=  — History list.
    // -----------------------------------------------------------------------
    [HttpGet]
    public async Task<IEnumerable<SubmissionListItem>> List(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int? departmentId, [FromQuery] string? shift)
    {
        var (f, t) = RangeHelper.Resolve(from, to);

        var query = _db.Submissions.AsNoTracking()
            .Where(s => s.AuditDate >= f && s.AuditDate <= t);

        if (departmentId is > 0)
            query = query.Where(s => s.DepartmentId == departmentId);
        if (!string.IsNullOrWhiteSpace(shift))
            query = query.Where(s => s.Shift == shift);

        return await query
            .OrderByDescending(s => s.AuditDate).ThenByDescending(s => s.Id)
            .Select(s => new SubmissionListItem
            {
                Id = s.Id,
                DepartmentId = s.DepartmentId,
                AuditDate = s.AuditDate,
                Shift = s.Shift,
                Auditor = s.Auditor,
                // "checked" = genuinely audited (OK or Not OK); Not Audited is excluded.
                Checked = s.Results.Count(r => r.Outcome == "OK" || r.Outcome == "NOT_OK"),
                FailCount = s.Results.Count(r => r.Outcome == "NOT_OK")
            })
            .ToListAsync();
    }

    // -----------------------------------------------------------------------
    // GET /api/submissions/{id}  — full read-only detail for the replay view.
    // -----------------------------------------------------------------------
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var detail = await _db.Submissions.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SubmissionDetail
            {
                Id = s.Id,
                DepartmentId = s.DepartmentId,
                DepartmentName = s.Department!.Name,
                AuditDate = s.AuditDate,
                WeekStarting = s.WeekStarting,
                Shift = s.Shift,
                Auditor = s.Auditor,
                OtherNotes = s.OtherNotes,
                Results = s.Results
                    .OrderBy(r => r.AuditItem!.SortOrder)
                    .Select(r => new DetailResult
                    {
                        AuditItemId = r.AuditItemId,
                        MachineName = r.AuditItem!.DisplayName,
                        Location = r.AuditItem.Location,
                        SeverityAtAudit = r.SeverityAtAudit,
                        Result = r.Outcome,
                        FailureMode = r.FailureMode != null ? r.FailureMode.Label : null,
                        NotAuditedReason = r.NotAuditedReason != null ? r.NotAuditedReason.Label : null,
                        PartNo = r.PartNo,
                        SerialNo = r.SerialNo,
                        CritDimsChecked = r.CritDimsChecked,
                        QmIpVersionChecked = r.QmIpVersionChecked,
                        Comment = r.Comment,
                        Customer = r.Customer,
                        ActionTaken = r.ActionTaken
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (detail is null)
            return NotFound(new { error = $"Submission {id} not found." });

        return Ok(detail);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
