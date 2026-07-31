namespace QualityAudit.Models;

/// <summary>
/// Everything the dashboard needs for its KPI row, the two breakdown charts, and the
/// recent-shifts list — returned in a single call for a given date range.
/// </summary>
public class DashboardSummary
{
    public int ShiftsLogged { get; set; }
    public int TotalChecks { get; set; }
    public decimal PassRate { get; set; }       // OK / (OK + NOK), one decimal place
    public int FailCount { get; set; }          // count of main-result NOK

    public List<LocationRate> PassByLocation { get; set; } = new();
    public List<SeverityRate> PassBySeverity { get; set; } = new();
    public List<RecentShift> RecentShifts { get; set; } = new();
}

public class LocationRate
{
    public string Location { get; set; } = "";  // Ph1 / Ph3
    public int Total { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public decimal Rate { get; set; }
}

public class SeverityRate
{
    public int Severity { get; set; }           // 1 / 2 / 3
    public int Total { get; set; }
    public int Pass { get; set; }
    public int Fail { get; set; }
    public decimal Rate { get; set; }
}

public class RecentShift
{
    public int Id { get; set; }
    public DateOnly AuditDate { get; set; }
    public string Shift { get; set; } = "";
    public string Auditor { get; set; } = "";
    public int Checked { get; set; }
    public int Fails { get; set; }
}
