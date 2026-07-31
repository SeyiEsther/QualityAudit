namespace QualityAudit.Models;

/// <summary>
/// The auditor's verdict for a single machine within a shift audit. Maps to one
/// dbo.Results row. Only machines that were actually checked (Result is not null)
/// are posted by the frontend.
/// </summary>
public class SubmissionResult
{
    public int AuditItemId { get; set; }
    public string? Result { get; set; }        // OK / NOK / NA
    public string? PartNo { get; set; }         // Drawing / Part No.
    public string? Plans { get; set; }          // sub-check: Plans in place & used
    public string? Ndt { get; set; }            // sub-check: Destructive / NDT in place
    public string? Docs { get; set; }           // sub-check: Area docs up to date
    public string? Deviation { get; set; }
    public string? Customer { get; set; }
    public string? ActionTaken { get; set; }    // Hold / Audit / QA Tracker
}
