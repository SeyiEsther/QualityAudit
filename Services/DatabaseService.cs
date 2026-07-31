using Dapper;
using Microsoft.Data.SqlClient;
using QualityAudit.Models;

namespace QualityAudit.Services;

/// <summary>
/// All database access for the app, using Dapper over Microsoft.Data.SqlClient.
/// No Entity Framework — kept consistent with the TL portal so both systems read
/// the same way. Every method opens and disposes its own connection.
/// </summary>
public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("RittalQualityAudit")
            ?? throw new InvalidOperationException(
                "Connection string 'RittalQualityAudit' is not configured in appsettings.json.");
    }

    private SqlConnection Open()
    {
        var conn = new SqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    // Empty string -> NULL, so blank optional fields store as NULL rather than ''.
    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // -----------------------------------------------------------------------
    // GET /api/audit-items
    // -----------------------------------------------------------------------
    public async Task<IEnumerable<AuditItem>> GetAuditItemsAsync()
    {
        const string sql = @"
            SELECT Id, DisplayName, Location, Severity, SpecialMeasures, SortOrder
            FROM dbo.AuditItems
            WHERE IsActive = 1
            ORDER BY SortOrder;";

        using var conn = Open();
        return await conn.QueryAsync<AuditItem>(sql);
    }

    // -----------------------------------------------------------------------
    // POST /api/submissions  — header + results in a single transaction
    // -----------------------------------------------------------------------
    public async Task<int> CreateSubmissionAsync(Submission submission)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        try
        {
            const string insertSubmission = @"
                INSERT INTO dbo.Submissions (Area, AuditDate, Shift, Auditor, OtherNotes)
                OUTPUT INSERTED.Id
                VALUES (@Area, @AuditDate, @Shift, @Auditor, @OtherNotes);";

            var newId = await conn.ExecuteScalarAsync<int>(insertSubmission, new
            {
                Area = NullIfBlank(submission.Area) ?? "Sheet Metal",
                submission.AuditDate,
                submission.Shift,
                submission.Auditor,
                OtherNotes = NullIfBlank(submission.OtherNotes)
            }, tx);

            // Only persist machines that were actually checked.
            var rows = submission.Results
                .Where(r => !string.IsNullOrWhiteSpace(r.Result))
                .Select(r => new
                {
                    SubmissionId = newId,
                    r.AuditItemId,
                    Result = NullIfBlank(r.Result),
                    PartNo = NullIfBlank(r.PartNo),
                    Plans = NullIfBlank(r.Plans),
                    Ndt = NullIfBlank(r.Ndt),
                    Docs = NullIfBlank(r.Docs),
                    Deviation = NullIfBlank(r.Deviation),
                    Customer = NullIfBlank(r.Customer),
                    ActionTaken = NullIfBlank(r.ActionTaken)
                })
                .ToList();

            if (rows.Count > 0)
            {
                const string insertResult = @"
                    INSERT INTO dbo.Results
                        (SubmissionId, AuditItemId, Result, PartNo, Plans, Ndt, Docs, Deviation, Customer, ActionTaken)
                    VALUES
                        (@SubmissionId, @AuditItemId, @Result, @PartNo, @Plans, @Ndt, @Docs, @Deviation, @Customer, @ActionTaken);";

                // Dapper runs the insert once per element — if any row fails the
                // whole batch throws and we roll back below.
                await conn.ExecuteAsync(insertResult, rows, tx);
            }

            tx.Commit();
            return newId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // GET /api/dashboard/summary
    //
    // The aggregate views (vw_PassRateBySeverity / vw_PassRateByLocation) have no
    // date column, so they can't honour the from/to range. We compute the summary
    // from the base tables using the same OK/NOK semantics those views use. Items
    // located in 'Ph 1 & 3' count toward both Ph1 and Ph3, matching the paper form.
    // -----------------------------------------------------------------------
    public async Task<DashboardSummary> GetSummaryAsync(DateOnly from, DateOnly to)
    {
        const string sql = @"
            -- headline totals
            SELECT
                (SELECT COUNT(*) FROM dbo.Submissions s
                 WHERE s.AuditDate BETWEEN @from AND @to)                          AS ShiftsLogged,
                ISNULL(SUM(CASE WHEN r.Result IS NOT NULL THEN 1 ELSE 0 END), 0)   AS TotalChecks,
                ISNULL(SUM(CASE WHEN r.Result = 'OK'  THEN 1 ELSE 0 END), 0)       AS PassCount,
                ISNULL(SUM(CASE WHEN r.Result = 'NOK' THEN 1 ELSE 0 END), 0)       AS FailCount
            FROM dbo.Results r
            JOIN dbo.Submissions s ON s.Id = r.SubmissionId
            WHERE s.AuditDate BETWEEN @from AND @to;

            -- pass/fail by location (Ph 1 & 3 counts to both)
            SELECT
                ISNULL(SUM(CASE WHEN i.Location LIKE '%1%' AND r.Result = 'OK'  THEN 1 ELSE 0 END), 0) AS Ph1Pass,
                ISNULL(SUM(CASE WHEN i.Location LIKE '%1%' AND r.Result = 'NOK' THEN 1 ELSE 0 END), 0) AS Ph1Fail,
                ISNULL(SUM(CASE WHEN i.Location LIKE '%3%' AND r.Result = 'OK'  THEN 1 ELSE 0 END), 0) AS Ph3Pass,
                ISNULL(SUM(CASE WHEN i.Location LIKE '%3%' AND r.Result = 'NOK' THEN 1 ELSE 0 END), 0) AS Ph3Fail
            FROM dbo.Results r
            JOIN dbo.Submissions s ON s.Id = r.SubmissionId
            JOIN dbo.AuditItems  i ON i.Id = r.AuditItemId
            WHERE s.AuditDate BETWEEN @from AND @to AND r.Result IN ('OK', 'NOK');

            -- pass/fail by severity
            SELECT
                i.Severity                                                    AS Severity,
                ISNULL(SUM(CASE WHEN r.Result = 'OK'  THEN 1 ELSE 0 END), 0)  AS Pass,
                ISNULL(SUM(CASE WHEN r.Result = 'NOK' THEN 1 ELSE 0 END), 0)  AS Fail
            FROM dbo.Results r
            JOIN dbo.Submissions s ON s.Id = r.SubmissionId
            JOIN dbo.AuditItems  i ON i.Id = r.AuditItemId
            WHERE s.AuditDate BETWEEN @from AND @to AND r.Result IN ('OK', 'NOK')
            GROUP BY i.Severity;

            -- recent shifts
            SELECT TOP (10)
                s.Id                                                              AS Id,
                s.AuditDate                                                       AS AuditDate,
                s.Shift                                                           AS Shift,
                s.Auditor                                                         AS Auditor,
                ISNULL(SUM(CASE WHEN r.Result IS NOT NULL THEN 1 ELSE 0 END), 0)  AS Checked,
                ISNULL(SUM(CASE WHEN r.Result = 'NOK'     THEN 1 ELSE 0 END), 0)  AS Fails
            FROM dbo.Submissions s
            LEFT JOIN dbo.Results r ON r.SubmissionId = s.Id
            WHERE s.AuditDate BETWEEN @from AND @to
            GROUP BY s.Id, s.AuditDate, s.Shift, s.Auditor
            ORDER BY s.AuditDate DESC, s.Id DESC;";

        using var conn = Open();
        using var grid = await conn.QueryMultipleAsync(sql, new { from, to });

        var head = await grid.ReadFirstAsync<HeadRow>();
        var loc = await grid.ReadFirstAsync<LocRow>();
        var sevRows = (await grid.ReadAsync<SevRow>()).ToList();
        var recent = (await grid.ReadAsync<RecentShift>()).ToList();

        var summary = new DashboardSummary
        {
            ShiftsLogged = head.ShiftsLogged,
            TotalChecks = head.TotalChecks,
            FailCount = head.FailCount,
            PassRate = Rate(head.PassCount, head.FailCount),
            PassByLocation = new List<LocationRate>
            {
                MakeLocation("Ph1", loc.Ph1Pass, loc.Ph1Fail),
                MakeLocation("Ph3", loc.Ph3Pass, loc.Ph3Fail)
            },
            RecentShifts = recent
        };

        // Always emit all three severities (3, 2, 1) in that order, even at zero.
        var sevLookup = sevRows.ToDictionary(x => (int)x.Severity);
        foreach (var sev in new[] { 3, 2, 1 })
        {
            var pass = sevLookup.TryGetValue(sev, out var row) ? row.Pass : 0;
            var fail = sevLookup.TryGetValue(sev, out row) ? row.Fail : 0;
            summary.PassBySeverity.Add(new SeverityRate
            {
                Severity = sev,
                Total = pass + fail,
                Pass = pass,
                Fail = fail,
                Rate = Rate(pass, fail)
            });
        }

        return summary;
    }

    // -----------------------------------------------------------------------
    // GET /api/dashboard/failures  — straight from the pre-built view
    // -----------------------------------------------------------------------
    public async Task<IEnumerable<FailureRecord>> GetFailuresAsync(DateOnly from, DateOnly to)
    {
        const string sql = @"
            SELECT AuditDate, Shift, Auditor, MachineName, Location, Severity, SpecialMeasures,
                   PartNo, Plans, Ndt, Docs, Deviation, Customer, ActionTaken
            FROM dbo.vw_Failures
            WHERE AuditDate BETWEEN @from AND @to
            ORDER BY Severity DESC, AuditDate DESC;";

        using var conn = Open();
        return await conn.QueryAsync<FailureRecord>(sql, new { from, to });
    }

    private static decimal Rate(int pass, int fail)
    {
        var total = pass + fail;
        return total == 0 ? 0m : Math.Round(100m * pass / total, 1);
    }

    private static LocationRate MakeLocation(string name, int pass, int fail) => new()
    {
        Location = name,
        Total = pass + fail,
        Pass = pass,
        Fail = fail,
        Rate = Rate(pass, fail)
    };

    // Private row shapes used only for reading the QueryMultiple grid.
    private sealed class HeadRow
    {
        public int ShiftsLogged { get; set; }
        public int TotalChecks { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
    }

    private sealed class LocRow
    {
        public int Ph1Pass { get; set; }
        public int Ph1Fail { get; set; }
        public int Ph3Pass { get; set; }
        public int Ph3Fail { get; set; }
    }

    private sealed class SevRow
    {
        public byte Severity { get; set; }
        public int Pass { get; set; }
        public int Fail { get; set; }
    }
}
