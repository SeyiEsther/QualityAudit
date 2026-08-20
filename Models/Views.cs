namespace QualityAudit.Models;

// ============================================================================
// Keyless entities mapped to the pre-built SQL views. Property names match the
// view column names. See QualityAuditContext for the ToView() mappings.
// ============================================================================

// dbo.vw_CurrentSeverity — resolves severity for TODAY'S week only (see FormController note).
public class CurrentSeverity
{
    public int AuditItemId { get; set; }
    public int DepartmentId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public bool SpecialMeasures { get; set; }
    public int SortOrder { get; set; }
    public byte Severity { get; set; }
    public int IsFallback { get; set; }
}

// dbo.vw_WeeklyCompliance
public class WeeklyCompliance
{
    public DateOnly WeekStarting { get; set; }
    public int DepartmentId { get; set; }
    public int AuditItemId { get; set; }
    public string DisplayName { get; set; } = "";
    public int SortOrder { get; set; }
    public byte Severity { get; set; }
    public int ExpectedChecks { get; set; }
    public int ActualChecks { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public int NotAuditedCount { get; set; }
}

// dbo.vw_Failures
public class VwFailure
{
    public int SubmissionId { get; set; }
    public DateOnly AuditDate { get; set; }
    public DateOnly WeekStarting { get; set; }
    public string Shift { get; set; } = "";
    public string Auditor { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public string MachineName { get; set; } = "";
    public string Location { get; set; } = "";
    public byte Severity { get; set; }
    public string Result { get; set; } = "";
    public string? PlansResult { get; set; }
    public string? NdtResult { get; set; }
    public string? AreaDocsResult { get; set; }
    public string? PartNo { get; set; }
    public string? Deviation { get; set; }
    public string? Customer { get; set; }
    public string? ActionTaken { get; set; }
    public string? ActionDetail { get; set; }
    public int ResultId { get; set; }
    public int AttachmentCount { get; set; }
}

// dbo.vw_FailuresByCustomer
public class FailuresByCustomer
{
    public DateOnly WeekStarting { get; set; }
    public int DepartmentId { get; set; }
    public string Customer { get; set; } = "";
    public int TotalChecks { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal? FailRatePct { get; set; }
}

// dbo.vw_CheckPointFailures
public class CheckPointFailure
{
    public DateOnly WeekStarting { get; set; }
    public int DepartmentId { get; set; }
    public string CheckPointCode { get; set; } = "";
    public string CheckPointLabel { get; set; } = "";
    public int FailCount { get; set; }
}

// dbo.vw_WeeklySummary
public class WeeklySummary
{
    public DateOnly WeekStarting { get; set; }
    public int DepartmentId { get; set; }
    public int ShiftsLogged { get; set; }
    public int TotalChecks { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public int NotAuditedCount { get; set; }
    public decimal? PassRatePct { get; set; }
}
