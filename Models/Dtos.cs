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
    public string? AreaLine { get; set; }
    public string? OtherNotes { get; set; }
    public bool IsComplete { get; set; }          // false = save draft, true = submit
    public List<ResultInput> Results { get; set; } = new();
}

public class ResultInput
{
    public int AuditItemId { get; set; }
    public string? Result { get; set; }           // OK / NOT_OK / NOT_AUDITED
    public string? PlansResult { get; set; }
    public string? NdtResult { get; set; }
    public string? AreaDocsResult { get; set; }
    public int? NotAuditedReasonId { get; set; }
    public string? PartNo { get; set; }
    public string? Deviation { get; set; }
    public int? CustomerId { get; set; }
    public int? ActionTypeId { get; set; }
    public string? ActionDetail { get; set; }
    public List<CheckPointAnswerInput> CheckPoints { get; set; } = new();
}

public class CheckPointAnswerInput
{
    public int CheckPointId { get; set; }
    public string? Answer { get; set; }
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

public class UserInput
{
    public int? Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public string? Username { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CustomerInput
{
    public int? Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CheckPointInput
{
    public int? Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public byte MinSeverity { get; set; } = 1;
    public bool Conditional { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// ============================================================================
// Response shapes
// ============================================================================

public class FormResponse
{
    public Department Department { get; set; } = new();
    public DateOnly WeekStarting { get; set; }
    public List<MachineDto> Machines { get; set; } = new();
    public List<SeverityLevel> SeverityLevels { get; set; } = new();
    public List<CheckPoint> CheckPoints { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
    public List<ActionType> ActionTypes { get; set; } = new();
    public List<NotAuditedReason> NotAuditedReasons { get; set; } = new();
    public List<UserDto> AuditUsers { get; set; } = new();
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

public class UserDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
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
    public bool IsComplete { get; set; }
}

public class SubmissionDetail
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = "";
    public bool HasNdtCheck { get; set; }
    public DateOnly AuditDate { get; set; }
    public DateOnly WeekStarting { get; set; }
    public string Shift { get; set; } = "";
    public string Auditor { get; set; } = "";
    public string? AreaLine { get; set; }
    public string? OtherNotes { get; set; }
    public bool IsComplete { get; set; }
    public List<DetailResult> Results { get; set; } = new();
}

public class DetailResult
{
    public int AuditItemId { get; set; }
    public int ResultId { get; set; }
    public string MachineName { get; set; } = "";
    public string Location { get; set; } = "";
    public byte SeverityAtAudit { get; set; }
    public string Result { get; set; } = "";
    public string? PlansResult { get; set; }
    public string? NdtResult { get; set; }
    public string? AreaDocsResult { get; set; }
    public string? NotAuditedReason { get; set; }
    public int? NotAuditedReasonId { get; set; }
    public string? PartNo { get; set; }
    public string? Deviation { get; set; }
    public string? Customer { get; set; }
    public int? CustomerId { get; set; }
    public string? ActionTaken { get; set; }
    public int? ActionTypeId { get; set; }
    public string? ActionDetail { get; set; }
    public List<DetailCheckPoint> CheckPoints { get; set; } = new();
    public List<AttachmentDto> Attachments { get; set; } = new();
}

public class DetailCheckPoint
{
    public int CheckPointId { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public string Answer { get; set; } = "";
}

public class AttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = "";
}

public class SaveResult
{
    public int Id { get; set; }
    public bool IsComplete { get; set; }
    public List<ResultIdMap> Results { get; set; } = new();
}

public class ResultIdMap
{
    public int AuditItemId { get; set; }
    public int ResultId { get; set; }
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
    public int SortOrder { get; set; }
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
    public string Month { get; set; } = "";
    public int Total { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public int Completed { get; set; }   // OK + NOT_OK
    public int Expected { get; set; }    // target checks for the month
    public decimal PassRate { get; set; }
    public decimal FailRate { get; set; }
}

public class AttainmentResult
{
    public int DepartmentId { get; set; }
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int ExpectedChecks { get; set; }
    public int ActualChecks { get; set; }
    public decimal AttainmentPct { get; set; }
    public decimal TargetPct { get; set; }
}

public class CoverageResponse
{
    public int DepartmentId { get; set; }
    public DateOnly WeekStarting { get; set; }
    public List<string> Days { get; set; } = new();   // 7 ISO dates, Tuesday..Monday
    public int ItemsAtTarget { get; set; }
    public int ItemCount { get; set; }
    public List<CoverageItem> Items { get; set; } = new();
}

public class CoverageItem
{
    public int AuditItemId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public int SortOrder { get; set; }
    public byte Severity { get; set; }
    public int Expected { get; set; }
    public int Actual { get; set; }
    public int Shortfall { get; set; }
    public List<int> DayCounts { get; set; } = new();   // aligned to Days
    public List<string> Shifts { get; set; } = new();
    public List<string> Auditors { get; set; } = new();
}

public class DepartmentInput
{
    public decimal TargetPercent { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SeverityLevelInput
{
    public int ChecksPerWeek { get; set; }
    public string Instruction { get; set; } = "";
}

public class ActionTypeInput
{
    public int? Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SeverityReviewRow
{
    public int AuditItemId { get; set; }
    public int DepartmentId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public int SortOrder { get; set; }
    public byte Severity { get; set; }
    public bool IsFallback { get; set; }
    public byte? PreviousSeverity { get; set; }
}
