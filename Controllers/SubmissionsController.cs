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
    private readonly AttachmentStorage _storage;
    private readonly UserContext _user;

    public SubmissionsController(QualityAuditContext db, AttachmentStorage storage, UserContext user)
    {
        _db = db;
        _storage = storage;
        _user = user;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SubmissionRequest? req)
    {
        if (req is null) return BadRequest(new { error = "Request body is missing." });
        var headerError = ValidateHeader(req);
        if (headerError != null) return BadRequest(new { error = headerError });

        var picked = req.Results.Where(r => !string.IsNullOrWhiteSpace(r.Result)).ToList();
        if (picked.Count == 0) return BadRequest(new { error = "At least one machine must be checked before saving." });

        var problems = ValidateRows(picked);
        if (problems.Count > 0) return BadRequest(new { error = "Some rows are incomplete.", details = problems });

        var weekStarting = WeekHelper.WeekStarting(req.AuditDate);
        var severity = await ResolveSeveritiesAsync(picked.Select(r => r.AuditItemId), weekStarting);

        var submission = new Submission
        {
            DepartmentId = req.DepartmentId,
            AuditDate = req.AuditDate,
            WeekStarting = weekStarting,
            Shift = req.Shift.Trim(),
            Auditor = req.Auditor.Trim(),
            AreaLine = NullIfBlank(req.AreaLine),
            OtherNotes = NullIfBlank(req.OtherNotes),
            IsComplete = req.IsComplete,
            LastEditedBy = _user.CurrentUser,
            LastEditedAt = DateTime.UtcNow,
            Results = picked.Select(r => BuildResult(r, severity)).ToList()
        };

        try
        {
            _db.Submissions.Add(submission);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Could not save the shift audit: " + ex.Message });
        }

        return Ok(BuildSaveResult(submission));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Put(int id, [FromBody] SubmissionRequest? req)
    {
        if (req is null) return BadRequest(new { error = "Request body is missing." });
        var headerError = ValidateHeader(req);
        if (headerError != null) return BadRequest(new { error = headerError });

        var picked = req.Results.Where(r => !string.IsNullOrWhiteSpace(r.Result)).ToList();
        if (picked.Count == 0) return BadRequest(new { error = "At least one machine must be checked before saving." });

        var problems = ValidateRows(picked);
        if (problems.Count > 0) return BadRequest(new { error = "Some rows are incomplete.", details = problems });

        var submission = await _db.Submissions
            .Include(s => s.Results).ThenInclude(r => r.CheckPoints)
            .Include(s => s.Results).ThenInclude(r => r.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (submission is null) return NotFound(new { error = $"Submission {id} not found." });

        var weekStarting = WeekHelper.WeekStarting(req.AuditDate);
        var severity = await ResolveSeveritiesAsync(picked.Select(r => r.AuditItemId), weekStarting);

        submission.DepartmentId = req.DepartmentId;
        submission.AuditDate = req.AuditDate;
        submission.WeekStarting = weekStarting;
        submission.Shift = req.Shift.Trim();
        submission.Auditor = req.Auditor.Trim();
        submission.AreaLine = NullIfBlank(req.AreaLine);
        submission.OtherNotes = NullIfBlank(req.OtherNotes);
        submission.IsComplete = req.IsComplete;
        submission.LastEditedBy = _user.CurrentUser;
        submission.LastEditedAt = DateTime.UtcNow;

        var incomingIds = picked.Select(r => r.AuditItemId).ToHashSet();

        foreach (var gone in submission.Results.Where(r => !incomingIds.Contains(r.AuditItemId)).ToList())
        {
            foreach (var att in gone.Attachments)
                TryDeleteFile(att.StoredPath);
            _db.ResultAttachments.RemoveRange(gone.Attachments);
            _db.ResultCheckPoints.RemoveRange(gone.CheckPoints);
            _db.Results.Remove(gone);
        }

        foreach (var input in picked)
        {
            var existing = submission.Results.FirstOrDefault(r => r.AuditItemId == input.AuditItemId);
            if (existing is null)
            {
                submission.Results.Add(BuildResult(input, severity));
            }
            else
            {
                ApplyResult(existing, input, severity);
                _db.ResultCheckPoints.RemoveRange(existing.CheckPoints);
                existing.CheckPoints.Clear();
                foreach (var cp in BuildCheckPoints(input))
                    existing.CheckPoints.Add(cp);
            }
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Could not update the shift audit: " + ex.Message });
        }

        return Ok(BuildSaveResult(submission));
    }

    [HttpGet("draft")]
    public async Task<IActionResult> Draft([FromQuery] int departmentId, [FromQuery] DateOnly date, [FromQuery] string shift)
    {
        var draft = await _db.Submissions.AsNoTracking()
            .Where(s => !s.IsComplete && s.DepartmentId == departmentId && s.AuditDate == date && s.Shift == shift)
            .OrderByDescending(s => s.Id)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (draft == 0) return NoContent();
        return await Detail(draft);
    }

    [HttpGet]
    public async Task<IEnumerable<SubmissionListItem>> List(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int? departmentId, [FromQuery] string? shift, [FromQuery] string? search)
    {
        var (f, t) = RangeHelper.Resolve(from, to);

        var query = _db.Submissions.AsNoTracking().Where(s => s.AuditDate >= f && s.AuditDate <= t);
        if (departmentId is > 0) query = query.Where(s => s.DepartmentId == departmentId);
        if (!string.IsNullOrWhiteSpace(shift)) query = query.Where(s => s.Shift == shift);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Results.Any(r =>
                (r.Deviation != null && r.Deviation.Contains(term)) ||
                (r.ActionDetail != null && r.ActionDetail.Contains(term))));
        }

        return await query
            .OrderByDescending(s => s.AuditDate).ThenByDescending(s => s.Id)
            .Select(s => new SubmissionListItem
            {
                Id = s.Id,
                DepartmentId = s.DepartmentId,
                AuditDate = s.AuditDate,
                Shift = s.Shift,
                Auditor = s.Auditor,
                Checked = s.Results.Count(r => r.Outcome == "OK" || r.Outcome == "NOT_OK"),
                FailCount = s.Results.Count(r => r.Outcome == "NOT_OK"),
                IsComplete = s.IsComplete
            })
            .ToListAsync();
    }

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
                HasNdtCheck = s.Department.HasNdtCheck,
                AuditDate = s.AuditDate,
                WeekStarting = s.WeekStarting,
                Shift = s.Shift,
                Auditor = s.Auditor,
                AreaLine = s.AreaLine,
                OtherNotes = s.OtherNotes,
                IsComplete = s.IsComplete,
                Results = s.Results.OrderBy(r => r.AuditItem!.SortOrder).Select(r => new DetailResult
                {
                    AuditItemId = r.AuditItemId,
                    ResultId = r.Id,
                    MachineName = r.AuditItem!.DisplayName,
                    Location = r.AuditItem.Location,
                    SeverityAtAudit = r.SeverityAtAudit,
                    Result = r.Outcome,
                    PlansResult = r.PlansResult,
                    NdtResult = r.NdtResult,
                    AreaDocsResult = r.AreaDocsResult,
                    NotAuditedReason = r.NotAuditedReason != null ? r.NotAuditedReason.Label : null,
                    NotAuditedReasonId = r.NotAuditedReasonId,
                    PartNo = r.PartNo,
                    Deviation = r.Deviation,
                    Customer = r.Customer != null ? r.Customer.Name : null,
                    CustomerId = r.CustomerId,
                    ActionTaken = r.ActionType != null ? r.ActionType.Label : null,
                    ActionTypeId = r.ActionTypeId,
                    ActionDetail = r.ActionDetail,
                    CheckPoints = r.CheckPoints.Select(cp => new DetailCheckPoint
                    {
                        CheckPointId = cp.CheckPointId,
                        Code = cp.CheckPoint!.Code,
                        Label = cp.CheckPoint.Label,
                        Answer = cp.Answer
                    }).ToList(),
                    Attachments = r.Attachments.Select(a => new AttachmentDto { Id = a.Id, FileName = a.FileName }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (detail is null) return NotFound(new { error = $"Submission {id} not found." });
        return Ok(detail);
    }

    private static string? ValidateHeader(SubmissionRequest req)
    {
        if (req.DepartmentId <= 0) return "A department is required.";
        if (string.IsNullOrWhiteSpace(req.Shift) || string.IsNullOrWhiteSpace(req.Auditor))
            return "Shift and Auditor are required.";
        return null;
    }

    private static List<string> ValidateRows(List<ResultInput> rows)
    {
        var problems = new List<string>();
        foreach (var r in rows)
        {
            var value = r.Result!.Trim();
            if (!ValidResults.Contains(value)) { problems.Add($"Item {r.AuditItemId}: '{value}' is not a valid result."); continue; }

            if (value == "NOT_AUDITED")
            {
                if (r.NotAuditedReasonId is null or <= 0)
                    problems.Add($"Item {r.AuditItemId}: a reason is required for a Not Audited result.");
            }
            else
            {
                var anyNok = value == "NOT_OK"
                    || r.PlansResult == "NOT_OK" || r.NdtResult == "NOT_OK" || r.AreaDocsResult == "NOT_OK";
                if (anyNok && string.IsNullOrWhiteSpace(r.Deviation))
                    problems.Add($"Item {r.AuditItemId}: a deviation is required to evidence a Not OK.");
            }
        }
        return problems;
    }

    private async Task<Func<int, byte>> ResolveSeveritiesAsync(IEnumerable<int> auditItemIds, DateOnly weekStarting)
    {
        var ids = auditItemIds.Distinct().ToList();
        var defaults = await _db.AuditItems.AsNoTracking()
            .Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.DefaultSeverity);
        var assignments = await _db.SeverityAssignments.AsNoTracking()
            .Where(a => ids.Contains(a.AuditItemId) && a.WeekStarting <= weekStarting).ToListAsync();
        var live = assignments.GroupBy(a => a.AuditItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.WeekStarting).First().Severity);

        return itemId => live.TryGetValue(itemId, out var sev) ? sev
                       : defaults.TryGetValue(itemId, out var def) ? def
                       : (byte)1;
    }

    private static Result BuildResult(ResultInput r, Func<int, byte> severity)
    {
        var result = new Result { AuditItemId = r.AuditItemId, SeverityAtAudit = severity(r.AuditItemId) };
        ApplyResult(result, r, severity);
        result.CheckPoints = BuildCheckPoints(r);
        return result;
    }

    private static void ApplyResult(Result target, ResultInput r, Func<int, byte> severity)
    {
        var value = r.Result!.Trim();
        var notAudited = value == "NOT_AUDITED";
        target.SeverityAtAudit = severity(r.AuditItemId);
        target.Outcome = value;
        target.PlansResult = notAudited ? null : NullIfBlank(r.PlansResult);
        target.NdtResult = notAudited ? null : NullIfBlank(r.NdtResult);
        target.AreaDocsResult = notAudited ? null : NullIfBlank(r.AreaDocsResult);
        target.NotAuditedReasonId = notAudited ? r.NotAuditedReasonId : null;
        target.PartNo = notAudited ? null : NullIfBlank(r.PartNo);
        target.Deviation = notAudited ? null : NullIfBlank(r.Deviation);
        target.CustomerId = notAudited ? null : (r.CustomerId is > 0 ? r.CustomerId : null);
        target.ActionTypeId = notAudited ? null : (r.ActionTypeId is > 0 ? r.ActionTypeId : null);
        target.ActionDetail = notAudited ? null : NullIfBlank(r.ActionDetail);
    }

    private static List<ResultCheckPoint> BuildCheckPoints(ResultInput r) =>
        r.Result!.Trim() == "NOT_AUDITED"
            ? new List<ResultCheckPoint>()
            : r.CheckPoints
                .Where(cp => !string.IsNullOrWhiteSpace(cp.Answer) && ValidResults.Contains(cp.Answer!.Trim()))
                .Select(cp => new ResultCheckPoint { CheckPointId = cp.CheckPointId, Answer = cp.Answer!.Trim() })
                .ToList();

    private static SaveResult BuildSaveResult(Submission s) => new()
    {
        Id = s.Id,
        IsComplete = s.IsComplete,
        Results = s.Results.Select(r => new ResultIdMap { AuditItemId = r.AuditItemId, ResultId = r.Id }).ToList()
    };

    private void TryDeleteFile(string storedName)
    {
        try { if (_storage.Exists(storedName)) System.IO.File.Delete(_storage.FullPath(storedName)); }
        catch {  }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
