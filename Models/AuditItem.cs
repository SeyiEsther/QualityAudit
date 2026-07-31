namespace QualityAudit.Models;

/// <summary>
/// One reference row from dbo.AuditItems — a machine / line check on the paper sheet.
/// The frontend builds the entry form dynamically from a list of these, so adding or
/// retiring a machine is a data change in the database, not a code change.
/// </summary>
public class AuditItem
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string Location { get; set; } = "";     // 'Ph1', 'Ph3', 'Ph 1 & 3'
    public byte Severity { get; set; }             // 1 = green, 2 = amber, 3 = red
    public bool SpecialMeasures { get; set; }       // the XX flag
    public int SortOrder { get; set; }
}
