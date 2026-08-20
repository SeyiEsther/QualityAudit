namespace QualityAudit.Services;

/// <summary>
/// The audit week starts on TUESDAY (Monday is the QE review; changes take effect the next day).
/// This mirrors dbo.fn_WeekStarting exactly: anchor off 1900-01-02 (a Tuesday) and floor to the
/// nearest 7-day boundary. For every real (post-1900) date the day count is non-negative, so C#
/// integer division matches SQL's truncating division.
/// </summary>
public static class WeekHelper
{
    private static readonly DateOnly Anchor = new(1900, 1, 2); // a Tuesday

    public static DateOnly WeekStarting(DateOnly date)
    {
        var days = date.DayNumber - Anchor.DayNumber;
        var weeks = days / 7;
        return Anchor.AddDays(weeks * 7);
    }

    public static DateOnly ThisWeek() => WeekStarting(DateOnly.FromDateTime(DateTime.Today));

    /// <summary>Next Tuesday's week — the default for the Admin weekly severity review.</summary>
    public static DateOnly NextWeek() => ThisWeek().AddDays(7);
}
