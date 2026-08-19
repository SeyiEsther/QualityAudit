namespace QualityAudit.Models;

// ============================================================================
// Keyless entities mapped to the pre-built SQL views. Property names match the
// view column names. See QualityAuditContext for the ToView() mappings.
// ============================================================================

// dbo.vw_CurrentSeverity — live severity this week (falls back to DefaultSeverity).
public class CurrentSeverity
{
    public int AuditItemId { get; set; }
    public int DepartmentId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public bool SpecialMeasures { get; set; }
    public int SortOrder { get; set; }
    public byte Severity { get; set; }
    public int IsFallback { get; set; }   // CASE expression -> int (0/1), not a bit
}

// dbo.vw_WeeklyCompliance — expected vs actual checks per machine per week.
public class WeeklyCompliance
{
    public DateOnly WeekStarting { get; set; }
    public int DepartmentId { get; set; }
    public int AuditItemId { get; set; }
    public string DisplayName { get; set; } = "";
    public byte Severity { get; set; }
    public int ExpectedChecks { get; set; }
    public int ActualChecks { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public int NotAuditedCount { get; set; }
}

// dbo.vw_Failures — every NOT_OK line check with its structured failure mode.
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
    public string? FailureMode { get; set; }
    public string? FailureModeCode { get; set; }
    public string? PartNo { get; set; }
    public string? SerialNo { get; set; }
    public string? Comment { get; set; }
    public string? Customer { get; set; }
    public string? ActionTaken { get; set; }
    public int ResultId { get; set; }
}

// dbo.vw_FailuresByCustomer — pass/fail counts and fail rate per customer per week.
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

// dbo.vw_FailureModeBreakdown — fail counts per failure mode per week.
public class FailureModeBreakdown
{
    public DateOnly WeekStarting { get; set; }
    public int DepartmentId { get; set; }
    public string FailureModeCode { get; set; } = "";
    public string FailureMode { get; set; } = "";
    public int FailCount { get; set; }
}

// dbo.vw_WeeklySummary — one row per department per week.
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
