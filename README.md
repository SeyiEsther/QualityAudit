# RittalQualityAudit — Quality Audit (QA 343-34) · v3

Digital replacement for the paper QA 343-34 sheet. ASP.NET Core 8 Web API with a
single-page HTML/JS frontend. **EF Core** over the existing `RittalQualityAudit` v3
database (no Dapper, no EF migrations) — consistent with the TL Portal.

## The four things v3 gets right

1. **The audit week starts on TUESDAY.** `Services/WeekHelper.cs` mirrors `dbo.fn_WeekStarting`
   exactly (anchor off 1900-01-02, a Tuesday). `WeekStarting` is computed server-side from
   `AuditDate` on every save and never trusted from the client.
2. **Severity is snapshotted, never joined live.** Each result stamps `SeverityAtAudit`, resolved
   from `SeverityAssignments` for that week (falling back to `AuditItems.DefaultSeverity`).
   Changing this week's RAG never alters last week's numbers.
3. **Machine names are not unique.** Identity is `AuditItems.Id`; ordering is `SortOrder`; the UI
   shows the row number so repeated names (e.g. "OEM product check EOL: Meta" ×8) stay distinct.
4. **NDT is per-department.** The Destructive/NDT sub-check renders only when
   `Departments.HasNdtCheck` is true (Sheet Metal), driven by the flag.

## Layout

```
Program.cs                      host: EF Core + controllers + static files + attachment storage
appsettings.json                connection string + Storage:AttachmentRoot
Data/QualityAuditContext.cs     maps 13 tables + 6 views onto the existing v3 schema
Models/Entities.cs / Views.cs / Dtos.cs
Services/WeekHelper.cs          Tuesday-week rule (mirror of fn_WeekStarting)
Services/AttachmentStorage.cs   photos on disk under Storage:AttachmentRoot
Services/UserContext.cs         pluggable identity + IsAdmin check (no auth yet)
Controllers/                    Departments, Form, Submissions, Attachments, Dashboard, Admin
wwwroot/index.html              the whole frontend (4 tabs)
```

## API

| Method | Route | Purpose |
| ------ | ----- | ------- |
| GET  | `/api/departments` | Active departments (incl. `HasNdtCheck`, `FormRef`). |
| GET  | `/api/form/{departmentId}?date=` | Everything the form is built from; machine severity resolved for that date's week. |
| POST | `/api/submissions` | Create a draft (`isComplete:false`) or submit (`true`). Snapshots severity, validates mandatory fields, one transaction. Returns id + auditItemId→resultId map. |
| PUT  | `/api/submissions/{id}` | Resume/update; upserts results by AuditItemId so photos survive. |
| GET  | `/api/submissions/draft?departmentId=&date=&shift=` | The resumable incomplete submission, if any. |
| GET  | `/api/submissions?from=&to=&departmentId=&shift=` | History list. |
| GET  | `/api/submissions/{id}` | Full read-only detail. |
| POST | `/api/results/{resultId}/attachments` | Multipart image upload (≤10 MB), GUID filename on disk. |
| GET  | `/api/attachments/{id}` | Stream a photo back. |
| GET  | `/api/dashboard/summary?departmentId=&weekStarting=` | This week vs last, compliance-vs-target. |
| GET  | `/api/dashboard/failures` · `/by-customer` · `/check-points` · `/overview` | The dashboard feeds. |
| GET/POST | `/api/admin/severities` | Weekly RAG review + bulk upsert (defaults to next Tuesday). |
| GET/POST/PUT | `/api/admin/audit-items` · `/users` · `/customers` · `/check-points` | Self-service admin CRUD (soft delete). |

## Result values

Exactly three, spelled out everywhere (no abbreviations) with hover tooltips:

- **OK** — Acceptable. Product/process meets and conforms to standard. No non-conformity found.
- **Not OK** — Not acceptable. Product/process has deviations from standard.
- **Not Audited** — Not audited. A reason is required.

Mandatory rules (enforced client-side, server-side, and by DB CHECK constraints): any `NOT_OK`
(row or sub-check) requires a **deviation**; `NOT_AUDITED` requires a **reason**; `NOT_OK` prompts
for an **action taken** (recommended, not blocking).

## Resumability & drafts

`Submissions.IsComplete` distinguishes a draft (0) from a submission (1). Only `IsComplete = 1`
feeds the dashboard views. On the New Audit tab, if an incomplete submission exists for the same
department + date + shift, the app offers to resume it (server-side draft). There is also a
localStorage safety net that offers to restore in-progress work and is cleared only after a
confirmed 200.

## Admin access (pluggable, no auth yet)

`UserContext` reads an `X-Username` header and matches it against `AuditUsers` (Username, Email,
or DisplayName), checking `IsAdmin`. With no username supplied it allows access (no auth yet). The
Admin tab has a "Signed in as" picker that sends the chosen identity. `IsAdmin` is editable in the
Users section (currently set on Mark Tapp, Nicky Gleeson, Steven White as a best guess).

## Run / publish

```
dotnet restore && dotnet run          # http://localhost:5000
dotnet publish -c Release -o publish   # copy to IIS on csm-srv-16 (No Managed Code app pool)
```

Set `Storage:AttachmentRoot` in `appsettings.json` to a writable file share the app pool can reach.

## Known open points (build around them, editable — no deploy needed)

- `FailureModes`/`CheckPoints` placeholder content and `SeverityLevels.ChecksPerWeek` (assumes
  5 days × 3 shifts) are editable in Admin.
- `AuditUsers.IsAdmin` is a best guess — editable in Admin.
- Serial-number linkage into HCL Notes is out of scope and not built.
- No authentication yet — see the pluggable seam above.
