# RittalQualityAudit — Quality Audit (QA 343-31) · v2

Digital replacement for the paper QA 343-31 sheet. ASP.NET Core 8 Web API with a
single-page HTML/JS frontend. **EF Core** over the existing `RittalQualityAudit`
database (no Dapper, no Entity Framework migrations) — consistent with the TL Portal.

## What v2 changed

- **Severity is weekly, not fixed.** The QEs reset each machine's RAG level every Monday
  (`SeverityAssignments`). Live severity comes from that; `AuditItems.DefaultSeverity` is a
  fallback only. When an audit is saved, each result stores `SeverityAtAudit` — a **snapshot**
  of that week's severity — so changing this week's RAG never rewrites last week's numbers.
- **Departments** (Sheet Metal, Assembly, …) — the machine list is department-scoped.
- **Structured failures.** `NOT_OK` requires a `FailureMode` from a dropdown; `NOT_AUDITED`
  requires a reason. Enforced client-side (blocks save, highlights the row) and server-side (400).
- **Severity drives frequency and depth.** `SeverityLevels` carries `ChecksPerWeek`
  (compliance target), `RequiresCritDims`, `RequiresQmIpVersion`, and the `Instruction` — all data.
- **WeekStarting** is always the Monday of the audit date, computed server-side, never trusted from the client.

## Project layout

```
Program.cs                     host: EF Core + controllers + static files
appsettings.json               connection string (key: RittalQualityAudit)
Data/QualityAuditContext.cs     maps entities + 6 views onto the existing schema
Models/Entities.cs              Department, AuditItem, SeverityLevel, SeverityAssignment,
                                FailureMode, NotAuditedReason, Submission, Result, AuditUser
Models/Views.cs                 keyless entities for the vw_* views
Models/Dtos.cs                  request + response shapes
Services/WeekHelper.cs          Monday-of maths
Services/RangeHelper.cs         date-range defaulting (last 30 days)
Services/UserContext.cs         pluggable identity + IsAdmin check (no auth yet)
Controllers/                    Departments, Form, Submissions, Dashboard, Admin
wwwroot/index.html              the whole frontend (4 tabs)
```

## API

| Method | Route | Purpose |
| ------ | ----- | ------- |
| GET  | `/api/departments` | Active departments. |
| GET  | `/api/form/{departmentId}` | Everything the entry form is built from: machines (live severity), severity rules, failure modes, reasons. |
| POST | `/api/submissions` | Save one shift audit. Computes WeekStarting, snapshots SeverityAtAudit, validates NOT_OK/NOT_AUDITED, one transaction. |
| GET  | `/api/submissions?from=&to=&departmentId=&shift=` | History list. |
| GET  | `/api/submissions/{id}` | Full read-only detail. |
| GET  | `/api/dashboard/summary?departmentId=&weekStarting=` | KPIs, compliance-vs-target, this week vs last. |
| GET  | `/api/dashboard/failures?departmentId=&weekStarting=` | Failure board (severity 3 first). |
| GET  | `/api/dashboard/by-customer?departmentId=&weekStarting=` | Fail rate by customer. |
| GET  | `/api/dashboard/failure-modes?departmentId=&weekStarting=` | Failure-mode breakdown (pie). |
| GET  | `/api/dashboard/overview?departmentId=&months=12` | Rolling monthly pass/fail. |
| GET/POST | `/api/admin/severities?weekStarting=&departmentId=` | Read / bulk-upsert the week's RAG. |
| GET/POST/PUT | `/api/admin/audit-items` | Add / edit / retire machines (soft delete). |
| GET/POST/PUT | `/api/admin/failure-modes` | Add / edit / retire failure modes. |

## Admin access (pluggable, no auth yet)

`Services/UserContext.cs` resolves the current user from an `X-Username` header today and
checks `AuditUsers.IsAdmin`. With **no** username supplied it allows access (there is no auth
yet, same as the TL Portal at this stage) so the QE team isn't locked out. When Windows Auth
lands, only `UserContext` changes. The Admin tab sends the username you type as `X-Username`.

## Run locally / publish

```
dotnet restore && dotnet run          # http://localhost:5000
dotnet publish -c Release -o publish   # copy to IIS on csm-srv-16 (No Managed Code app pool)
```

## Known placeholders (per the schema notes)

- `FailureModes` holds placeholder rows — swap them in the Admin tab, no code change.
- `SeverityLevels.ChecksPerWeek` assumes 3 shifts × 5 days — adjust when confirmed.
- `Results.SerialNo` is provisional — built and optional.
- No authentication yet — see the pluggable seam above.

## Dashboard notes

- Compliance-vs-target, week-over-week, failures, by-customer, and failure-mode breakdown come
  from the pre-built views. Pass-rate-by-severity is aggregated from `vw_WeeklyCompliance`;
  pass-rate-by-location is computed from base rows for the week (no view provides it), with
  `Ph 1 & 3` machines counting toward both Ph1 and Ph3.
- Pie / customer charts are click-to-filter the failure board. "Wall display" enlarges KPIs
  and charts and hides navigation for a QA-office TV.
