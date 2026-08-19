namespace QualityAudit.Services;

/// <summary>Week maths. WeekStarting is always the Monday of the audit date, computed server-side.</summary>
public static class WeekHelper
{
    public static DateOnly MondayOf(DateOnly date)
    {
        // DayOfWeek: Sunday = 0 ... Saturday = 6. Shift so Monday = 0.
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    public static DateOnly ThisMonday() => MondayOf(DateOnly.FromDateTime(DateTime.Today));
}
