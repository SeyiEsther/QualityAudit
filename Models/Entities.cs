using System.Text.Json.Serialization;

namespace QualityAudit.Models;

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? FormRef { get; set; }
    public bool HasNdtCheck { get; set; }
    public decimal TargetPercent { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class SeverityLevel
{
    public byte Severity { get; set; }
    public string Name { get; set; } = "";
    public string AuditName { get; set; } = "";
    public string ColourHex { get; set; } = "";
    public string FrequencyCode { get; set; } = "";
    public string FrequencyLabel { get; set; } = "";
    public int ChecksPerWeek { get; set; }
    public string Instruction { get; set; } = "";
}

public class CheckPoint
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public byte MinSeverity { get; set; }
    public bool Conditional { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class AuditItem
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";
    public byte DefaultSeverity { get; set; }
    public bool SpecialMeasures { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }

    [JsonIgnore] public Department? Department { get; set; }
    [JsonIgnore] public ICollection<Result> Results { get; set; } = new List<Result>();
}

public class SeverityAssignment
{
    public int Id { get; set; }
    public int AuditItemId { get; set; }
    public DateOnly WeekStarting { get; set; }
    public byte Severity { get; set; }
    public string? SetBy { get; set; }
    public DateTime SetAt { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ActionType
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

public class AuditUser
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public string? Username { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
}

public class Submission
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public DateOnly AuditDate { get; set; }
    public DateOnly WeekStarting { get; set; }
    public string Shift { get; set; } = "";
    public string Auditor { get; set; } = "";
    public string? AreaLine { get; set; }
    public string? OtherNotes { get; set; }
    public bool IsComplete { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }

    [JsonIgnore] public Department? Department { get; set; }
    [JsonIgnore] public ICollection<Result> Results { get; set; } = new List<Result>();
}

public class Result
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int AuditItemId { get; set; }
    public byte SeverityAtAudit { get; set; }

    public string Outcome { get; set; } = "";
    public string? PlansResult { get; set; }
    public string? NdtResult { get; set; }
    public string? AreaDocsResult { get; set; }
    public int? NotAuditedReasonId { get; set; }
    public string? PartNo { get; set; }
    public string? Deviation { get; set; }
    public int? CustomerId { get; set; }
    public int? ActionTypeId { get; set; }
    public string? ActionDetail { get; set; }

    [JsonIgnore] public Submission? Submission { get; set; }
    [JsonIgnore] public AuditItem? AuditItem { get; set; }
    [JsonIgnore] public NotAuditedReason? NotAuditedReason { get; set; }
    [JsonIgnore] public Customer? Customer { get; set; }
    [JsonIgnore] public ActionType? ActionType { get; set; }
    [JsonIgnore] public ICollection<ResultCheckPoint> CheckPoints { get; set; } = new List<ResultCheckPoint>();
    [JsonIgnore] public ICollection<ResultAttachment> Attachments { get; set; } = new List<ResultAttachment>();
}

public class ResultCheckPoint
{
    public int Id { get; set; }
    public int ResultId { get; set; }
    public int CheckPointId { get; set; }
    public string Answer { get; set; } = "";

    [JsonIgnore] public Result? Result { get; set; }
    [JsonIgnore] public CheckPoint? CheckPoint { get; set; }
}

public class ResultAttachment
{
    public int Id { get; set; }
    public int ResultId { get; set; }
    public string FileName { get; set; } = "";
    public string StoredPath { get; set; } = "";
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    [JsonIgnore] public Result? Result { get; set; }
}
