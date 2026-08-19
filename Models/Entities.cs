using System.Text.Json.Serialization;

namespace QualityAudit.Models;

// ============================================================================
// Entities mapped onto the existing RittalQualityAudit v2 schema.
// The app never creates or migrates schema — see QualityAuditContext.
// ============================================================================

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class AuditItem
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public byte DefaultSeverity { get; set; }   // fallback only; live severity is weekly
    public bool SpecialMeasures { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    [JsonIgnore] public Department? Department { get; set; }
    [JsonIgnore] public ICollection<Result> Results { get; set; } = new List<Result>();
}

/// <summary>The rule set for each RAG level — frequency AND check depth live here as data.</summary>
public class SeverityLevel
{
    public byte Severity { get; set; }          // 1 / 2 / 3 (PK)
    public string Name { get; set; } = "";
    public string ColourHex { get; set; } = "";
    public string FrequencyCode { get; set; } = "";
    public string FrequencyLabel { get; set; } = "";
    public int ChecksPerWeek { get; set; }
    public bool RequiresCritDims { get; set; }
    public bool RequiresQmIpVersion { get; set; }
    public string Instruction { get; set; } = "";
}

/// <summary>The weekly RAG review — this is the live severity, one row per machine per week.</summary>
public class SeverityAssignment
{
    public int Id { get; set; }
    public int AuditItemId { get; set; }
    public DateOnly WeekStarting { get; set; }
    public byte Severity { get; set; }
    public string? SetBy { get; set; }
    public DateTime SetAt { get; set; }
}

public class FailureMode
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class NotAuditedReason
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class Submission
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public DateOnly AuditDate { get; set; }
    public DateOnly WeekStarting { get; set; }   // Monday of AuditDate; computed on save
    public string Shift { get; set; } = "";
    public string Auditor { get; set; } = "";
    public string? OtherNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }

    [JsonIgnore] public Department? Department { get; set; }
    [JsonIgnore] public ICollection<Result> Results { get; set; } = new List<Result>();
}

/// <summary>
/// One machine's verdict within a shift audit. <see cref="Outcome"/> maps to the SQL
/// column 'Result' (a property can't share the enclosing type's name).
/// </summary>
public class Result
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int AuditItemId { get; set; }
    public byte SeverityAtAudit { get; set; }   // snapshot of that week's severity

    public string Outcome { get; set; } = "";   // -> column 'Result': OK / NOT_OK / NOT_AUDITED
    public int? FailureModeId { get; set; }
    public int? NotAuditedReasonId { get; set; }
    public string? PartNo { get; set; }
    public string? SerialNo { get; set; }
    public string? CritDimsChecked { get; set; }
    public string? QmIpVersionChecked { get; set; }
    public string? Comment { get; set; }
    public string? Customer { get; set; }
    public string? ActionTaken { get; set; }

    [JsonIgnore] public Submission? Submission { get; set; }
    [JsonIgnore] public AuditItem? AuditItem { get; set; }
    [JsonIgnore] public FailureMode? FailureMode { get; set; }
    [JsonIgnore] public NotAuditedReason? NotAuditedReason { get; set; }
}

public class AuditUser
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Username { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
}
