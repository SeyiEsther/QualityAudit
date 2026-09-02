using QualityAudit.Models;

namespace QualityAudit.Services;

/// <summary>
/// Resolves the severity in force for an audit item in a given week: the latest
/// SeverityAssignment on or before that week, falling back to DefaultSeverity.
/// Mirrors the rule used at save time so expected-check maths and the live form agree.
/// </summary>
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
