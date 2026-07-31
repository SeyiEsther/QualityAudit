namespace QualityAudit.Models;

/// <summary>
/// A complete shift audit as posted by the frontend: the header fields plus every
/// machine the auditor actually checked. Maps to one dbo.Submissions row and many
/// dbo.Results rows, written together in a single transaction.
/// </summary>
public class Submission
{
    public string Area { get; set; } = "Sheet Metal";
    public DateOnly AuditDate { get; set; }
    public string Shift { get; set; } = "";        // Early / Late / Nights
    public string Auditor { get; set; } = "";
    public string? OtherNotes { get; set; }
    public List<SubmissionResult> Results { get; set; } = new();
}
