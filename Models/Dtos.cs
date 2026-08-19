namespace QualityAudit.Models;

// ============================================================================
// Request bodies
// ============================================================================

public class SubmissionRequest
{
    public int DepartmentId { get; set; }
    public DateOnly AuditDate { get; set; }
    public string Shift { get; set; } = "";
    public string Auditor { get; set; } = "";
    public string? OtherNotes { get; set; }
    public List<ResultInput> Results { get; set; } = new();
}

public class ResultInput
{
    public int AuditItemId { get; set; }
    public string? Result { get; set; }                 // OK / NOT_OK / NOT_AUDITED
    public int? FailureModeId { get; set; }
    public int? NotAuditedReasonId { get; set; }
    public string? PartNo { get; set; }
    public string? SerialNo { get; set; }
    public string? CritDimsChecked { get; set; }
    public string? QmIpVersionChecked { get; set; }
    public string? Comment { get; set; }
    public string? Customer { get; set; }
    public string? ActionTaken { get; set; }
}

public class SeverityBulkRequest
{
    public DateOnly WeekStarting { get; set; }
    public string? SetBy { get; set; }
    public List<SeverityAssignmentInput> Assignments { get; set; } = new();
}

public class SeverityAssignmentInput
{
    public int AuditItemId { get; set; }
    public byte Severity { get; set; }
}

public class AuditItemInput
{
    public int? Id { get; set; }
    public int DepartmentId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "Ph1";
    public byte DefaultSeverity { get; set; } = 1;
    public bool SpecialMeasures { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class FailureModeInput
{
    public int? Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// ============================================================================
// Response shapes
// ============================================================================

/// <summary>GET /api/form/{departmentId} — everything the entry form is built from.</summary>
public class FormResponse
{
    public Department Department { get; set; } = new();
    public List<MachineDto> Machines { get; set; } = new();
    public List<SeverityLevel> SeverityLevels { get; set; } = new();
    public List<FailureMode> FailureModes { get; set; } = new();
    public List<NotAuditedReason> NotAuditedReasons { get; set; } = new();
}

public class MachineDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public bool SpecialMeasures { get; set; }
    public int SortOrder { get; set; }
    public byte Severity { get; set; }
    public bool IsFallback { get; set; }
}

public class SubmissionListItem
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public DateOnly AuditDate { get; set; }
    public string Shift { get; set; } = "";
    public string Auditor { get; set; } = "";
    public int Checked { get; set; }
    public int FailCount { get; set; }
}

public class SubmissionDetail
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "";
    public DateOnly AuditDate { get; set; }
    public DateOnly WeekStarting { get; set; }
    public string Shift { get; set; } = "";
    public string Auditor { get; set; } = "";
    public string? OtherNotes { get; set; }
    public List<DetailResult> Results { get; set; } = new();
}

public class DetailResult
{
    public int AuditItemId { get; set; }
    public string MachineName { get; set; } = "";
    public string Location { get; set; } = "";
    public byte SeverityAtAudit { get; set; }
    public string Result { get; set; } = "";
    public string? FailureMode { get; set; }
    public string? NotAuditedReason { get; set; }
    public string? PartNo { get; set; }
    public string? SerialNo { get; set; }
    public string? CritDimsChecked { get; set; }
    public string? QmIpVersionChecked { get; set; }
    public string? Comment { get; set; }
    public string? Customer { get; set; }
    public string? ActionTaken { get; set; }
}

public class DashboardSummary
{
    public DateOnly WeekStarting { get; set; }
    public DateOnly PreviousWeekStarting { get; set; }
    public WeekStats Current { get; set; } = new();
    public WeekStats Previous { get; set; } = new();
    public List<ComplianceRow> Compliance { get; set; } = new();
    public List<SeverityRate> PassBySeverity { get; set; } = new();
    public List<LocationRate> PassByLocation { get; set; } = new();
}

public class WeekStats
{
    public DateOnly WeekStarting { get; set; }
    public int ShiftsLogged { get; set; }
    public int TotalChecks { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public int NotAuditedCount { get; set; }
    public decimal PassRate { get; set; }
}

public class ComplianceRow
{
    public int AuditItemId { get; set; }
    public string DisplayName { get; set; } = "";
    public byte Severity { get; set; }
    public int Expected { get; set; }
    public int Actual { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public int NotAudited { get; set; }
    public bool UnderTarget { get; set; }
}

public class SeverityRate
{
    public int Severity { get; set; }
    public int Total { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public decimal Rate { get; set; }
}

public class LocationRate
{
    public string Location { get; set; } = "";
    public int Total { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public decimal Rate { get; set; }
}

public class OverviewMonth
{
    public string Month { get; set; } = "";     // yyyy-MM
    public int Total { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public decimal PassRate { get; set; }
    public decimal FailRate { get; set; }
}

public class SeverityReviewRow
{
    public int AuditItemId { get; set; }
    public int DepartmentId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public byte Severity { get; set; }          // in force for the chosen week
    public bool IsFallback { get; set; }        // no explicit assignment for that week
    public byte? PreviousSeverity { get; set; } // week before, for "what changed"
}
