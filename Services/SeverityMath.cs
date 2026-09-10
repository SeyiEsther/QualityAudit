using QualityAudit.Models;

namespace QualityAudit.Services;

public static class SeverityMath
{
    public static byte Resolve(
        IReadOnlyDictionary<int, List<SeverityAssignment>> byItem,
        IReadOnlyDictionary<int, byte> defaults,
        int auditItemId,
        DateOnly week)
    {
        if (byItem.TryGetValue(auditItemId, out var list))
        {
            SeverityAssignment? best = null;
            foreach (var a in list)
                if (a.WeekStarting <= week && (best is null || a.WeekStarting > best.WeekStarting))
                    best = a;
            if (best is not null) return best.Severity;
        }
        return defaults.TryGetValue(auditItemId, out var def) ? def : (byte)1;
    }

    public static Dictionary<int, List<SeverityAssignment>> GroupByItem(IEnumerable<SeverityAssignment> assignments)
    {
        var map = new Dictionary<int, List<SeverityAssignment>>();
        foreach (var a in assignments)
        {
            if (!map.TryGetValue(a.AuditItemId, out var list)) map[a.AuditItemId] = list = new();
            list.Add(a);
        }
        return map;
    }
}
