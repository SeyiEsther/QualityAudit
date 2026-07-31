namespace QualityAudit.Models;

/// <summary>
/// One failed line check for the dashboard failure board. Mirrors a row of dbo.vw_Failures
/// (any result where the main Result or a Plans/NDT/Docs sub-check is NOK).
/// </summary>
public class FailureRecord
{
    public DateOnly AuditDate { get; set; }
    public string Shift { get; set; } = "";
    public string Auditor { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string Location { get; set; } = "";
    public byte Severity { get; set; }
    public bool SpecialMeasures { get; set; }
    public string? PartNo { get; set; }
    public string? Plans { get; set; }
    public string? Ndt { get; set; }
    public string? Docs { get; set; }
    public string? Deviation { get; set; }
    public string? Customer { get; set; }
    public string? ActionTaken { get; set; }
}
