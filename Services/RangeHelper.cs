namespace QualityAudit.Services;

public static class RangeHelper
{
    public static (DateOnly From, DateOnly To) Resolve(DateOnly? from, DateOnly? to)
    {
        var toDate = to ?? DateOnly.FromDateTime(DateTime.Today);
        var fromDate = from ?? toDate.AddDays(-30);
        return (fromDate, toDate);
    }
}
