using Microsoft.AspNetCore.Mvc;
using QualityAudit.Models;
using QualityAudit.Services;

namespace QualityAudit.Controllers;

[ApiController]
[Route("api/submissions")]
public class SubmissionsController : ControllerBase
{
    private readonly DatabaseService _db;

    public SubmissionsController(DatabaseService db) => _db = db;

    /// <summary>Saves a complete shift audit (header + checked results) in one transaction.</summary>
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Submission? submission)
    {
        if (submission is null)
            return BadRequest("Request body is missing.");

        if (string.IsNullOrWhiteSpace(submission.Shift) || string.IsNullOrWhiteSpace(submission.Auditor))
            return BadRequest("Shift and Auditor are required.");

        var checkedResults = submission.Results
            .Where(r => !string.IsNullOrWhiteSpace(r.Result))
            .ToList();

        if (checkedResults.Count == 0)
            return BadRequest("At least one machine must be checked before saving.");

        submission.Results = checkedResults;

        var id = await _db.CreateSubmissionAsync(submission);
        return Ok(new { id });
    }
}
