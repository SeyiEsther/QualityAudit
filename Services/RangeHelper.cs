namespace QualityAudit.Services;

/// <summary>Shared date-range defaulting: last 30 days when either bound is omitted.</summary>
public static class RangeHelper
{
    public static (DateOnly From, DateOnly To) Resolve(DateOnly? from, DateOnly? to)
    {
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);
        var fromDate = from ?? toDate.AddDays(-30);
        return (fromDate, toDate);
    }
}
