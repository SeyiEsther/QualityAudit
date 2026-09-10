namespace QualityAudit.Services;

public static class WeekHelper
{
    private static readonly DateOnly Anchor = new(1900, 1, 2);

    public static DateOnly WeekStarting(DateOnly date)
    {
        var days = date.DayNumber - Anchor.DayNumber;
        var weeks = days / 7;
        return Anchor.AddDays(weeks * 7);
    }

    public static DateOnly ThisWeek() => WeekStarting(DateOnly.FromDateTime(DateTime.Today));

    public static DateOnly NextWeek() => ThisWeek().AddDays(7);

    public static IEnumerable<DateOnly> WeeksBetween(DateOnly from, DateOnly to)
    {
        var w = WeekStarting(from);
        var end = WeekStarting(to);
        while (w <= end) { yield return w; w = w.AddDays(7); }
    }
}
