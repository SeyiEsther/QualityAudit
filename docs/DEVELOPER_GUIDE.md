# Quality Audit – Developer Guide

Technical notes for maintaining the Quality Audit app (digital QA 343-34). Read this
alongside the code; it explains the parts that are easy to get wrong.

## Stack

- ASP.NET Core 8 Web API + EF Core 8, SQL Server (`RittalQualityAudit` on CSMSVR02).
- Single-page frontend: one static file, `wwwroot/index.html`. Vanilla HTML/CSS/JS,
  Chart.js from CDN. **No npm, no build step, no framework.**
- Deployed to IIS on csm-srv-16 (same box/pattern as the TL Portal).

## Solution layout

```
Program.cs                 host: DbContext, services, controllers, static files
appsettings.json           connection string + Storage:AttachmentRoot
Data/QualityAuditContext.cs EF model mapped onto the existing schema
Models/
  Entities.cs              tables
  Views.cs                 keyless entities for the vw_* views
  Dtos.cs                  request + response shapes
Services/
  WeekHelper.cs            Tuesday-week maths (mirror of dbo.fn_WeekStarting)
  SeverityMath.cs          resolve a machine's severity for a week
  RangeHelper.cs           default date ranges
  UserContext.cs           current user + admin check (pluggable)
  AttachmentStorage.cs     photos on disk
Controllers/
  DepartmentsController    GET departments
  FormController           GET the whole entry form for a department + date
  SubmissionsController    POST/PUT/GET audits (+ resumable draft)
  AttachmentsController    upload/list/stream photos
  DashboardController      summary, attainment, failures, by-customer, check-points, overview
  CoverageController       per-item coverage grid for a week
  AdminController          severities + CRUD (machines, users, customers, check points,
                           action types, severity levels, department targets)
wwwroot/index.html         the entire frontend
sql/                       one-off DB scripts to run in SSMS
docs/                      this guide + the user guide
```

## Run locally

```
dotnet run
```

Serves on `http://localhost:5000` (see `Properties/launchSettings.json`). The frontend
is static, so editing `wwwroot/index.html` only needs a browser refresh; C# changes
need a rebuild.

Config keys in `appsettings.json`:

- `ConnectionStrings:RittalQualityAudit` – SQL Server connection.
- `Storage:AttachmentRoot` – folder for uploaded photos (a file share in production).

## Build and deploy (IIS)

1. Publish with the folder profile (`Properties/PublishProfiles/FolderProfile.pubxml`)
   or `dotnet publish -c Release`.
2. Copy the output to the site on csm-srv-16; app pool = **No Managed Code**.
3. The app pool identity needs: read/write to `Storage:AttachmentRoot`, and access to
   `RittalQualityAudit` on CSMSVR02.

`index.html` is static, so a frontend-only change can be deployed by copying that one
file. Browsers cache it, so hard-refresh (Ctrl+F5) after deploying.

## Database and EF mapping

**The schema is hand-managed. Do not use EF migrations and do not create/alter tables
from code.** Entities map onto existing objects; a schema change is a `.sql` file in
`sql/` for someone to run in SSMS, plus a matching entity/DTO edit.

Conventions to know:

- Tables map by name. `Result.Outcome` maps to the SQL column **`Result`** (a property
  can't share its class name) via `HasColumnName("Result")`.
- The six `vw_*` views are **keyless entities** (`HasNoKey().ToView(...)`), read-only.
- `DateOnly` maps to SQL `date`; `Departments.TargetPercent` is `decimal` (DECIMAL(5,2)).
- `CreatedAt` / `SetAt` / `UploadedAt` use DB defaults (`ValueGeneratedOnAdd`), so don't
  set them in code.

Tables: Departments, SeverityLevels, CheckPoints, AuditItems, SeverityAssignments,
Customers, ActionTypes, NotAuditedReasons, AuditUsers, Submissions, Results,
ResultCheckPoints, ResultAttachments.

Views: vw_CurrentSeverity, vw_WeeklyCompliance, vw_Failures, vw_FailuresByCustomer,
vw_CheckPointFailures, vw_WeeklySummary. All dashboard views filter `IsComplete = 1`.

## Domain rules that must not break

These are the load-bearing rules. Changing them wrongly corrupts reported figures.

1. **The audit week starts on Tuesday.** `WeekHelper.WeekStarting` mirrors
   `dbo.fn_WeekStarting` (anchored on 1900-01-02, a Tuesday). `Submissions.WeekStarting`
   is computed server-side on save; never trust a client value.
2. **Severity is snapshotted.** On save, each result stores `SeverityAtAudit`, resolved
   for that week from `SeverityAssignments` (latest on/before the week) falling back to
   `AuditItems.DefaultSeverity`. See `SeverityMath.Resolve`. Never join live severity onto
   historical results, or last week's numbers will move when this week's RAG changes.
3. **Machine identity is `AuditItems.Id`.** `DisplayName` is not unique (Assembly repeats
   names). Order by `SortOrder`; show the row number in the UI. Never key on name.
4. **NDT is per department.** The Destructive/NDT sub-check renders only when
   `Departments.HasNdtCheck` is true. For Assembly it's null and stays null.
5. **Result values are exactly `OK`, `NOT_OK`, `NOT_AUDITED`.** The UI shows four buttons:
   OK, Not OK, No Time, Not Running. No Time / Not Running both store `NOT_AUDITED` with
   the `NotAuditedReasons` id whose Code is `NO_TIME` / `NOT_RUNNING` (looked up at
   runtime, not hardcoded).
6. **Mandatory fields** (enforced client-side, server-side, and by DB CHECK constraints):
   any `NOT_OK` (row or sub-check) requires a non-empty `Deviation`; `NOT_AUDITED` requires
   a reason. A `NOT_AUDITED` row stores only its reason — all other fields are nulled on
   save so it can't trip the deviation constraint.
7. **Attainment** (`GET /api/dashboard/attainment`): `expected` = sum over each week in the
   period of each active item's `SeverityLevels.ChecksPerWeek` for that week's severity;
   `actual` = count of results with Result in (OK, NOT_OK) on complete submissions;
   `target` = `Departments.TargetPercent`. Nothing is hardcoded (no 1/3/5/15/98).
8. **`IsComplete`** separates a draft (0, resumable) from a submission (1). Only
   `IsComplete = 1` feeds the dashboard views.

## API surface

All JSON is camelCase; error responses are `{ "error": "..." }`.

| Method | Route | Notes |
| ------ | ----- | ----- |
| GET | `/api/departments` | active departments |
| GET | `/api/form/{departmentId}?date=` | everything the entry form needs; severity resolved for that date's week |
| POST | `/api/submissions` | create draft or submit; returns id + auditItemId→resultId map |
| PUT | `/api/submissions/{id}` | update/resume (upsert by AuditItemId so photos survive) |
| GET | `/api/submissions?from=&to=&departmentId=&shift=&search=` | history list |
| GET | `/api/submissions/{id}` | full detail |
| GET | `/api/submissions/draft?departmentId=&date=&shift=` | resumable draft, if any |
| POST | `/api/results/{resultId}/attachments` | multipart image upload (≤10 MB) |
| GET | `/api/results/{resultId}/attachments`, `/api/attachments/{id}` | list / stream |
| GET | `/api/dashboard/attainment?departmentId=&from=&to=` | headline gauge |
| GET | `/api/dashboard/summary?departmentId=&weekStarting=` | this vs last week + compliance |
| GET | `/api/dashboard/failures` `/by-customer` `/check-points` `/overview` | dashboard feeds |
| GET | `/api/coverage?departmentId=&weekStarting=` | per-item day grid |
| GET/POST | `/api/admin/severities` | weekly RAG review + bulk upsert |
| GET/POST/PUT | `/api/admin/audit-items` `/users` `/customers` `/check-points` `/action-types` | CRUD (retire = soft delete) |
| GET/PUT | `/api/admin/severity-levels` `/departments` | edit frequency/instruction, target |

## Frontend

One file, `wwwroot/index.html`: a `<style>` block, the markup for the tab panes, and one
`<script>`. No modules. Key ideas:

- **Nothing is hardcoded.** `loadForm()` calls `/api/form/{dept}` and builds the machine
  list, RAG legend, dropdowns and check points from the response.
- State: `rows` is an object keyed by `auditItemId`. `machineRowHtml` / `rowDetailHtml`
  render a row; `resultBankHtml` renders the four result buttons.
- Result handlers: `setResult` (OK/Not OK; OK auto-fills sub-checks + check points),
  `setNaResult` (No Time/Not Running → NOT_AUDITED + reason).
- Validation `validateRows` returns `{devBad, reasonBad}`; save is blocked and rows are
  highlighted until fixed.
- Draft safety net: `localStorage` under `qa-draft-v3`, debounced ~500 ms, cleared only
  after a confirmed 200. Server-side drafts (`IsComplete = 0`) are offered via the resume
  banner.
- Severity colours come from `SeverityLevels.ColourHex`; `textOn(hex)` picks readable
  text. Charts use Chart.js from the CDN.

## Auth / admin seam

There is no authentication yet. `Services/UserContext.cs` reads an `X-Username` header
and checks it against `AuditUsers` (Username/Email/DisplayName) for `IsAdmin`. With no
header it allows access (dev). When Windows Auth is added, only `UserContext` changes.

## Common changes

- **Add/retire a machine, user, customer, check point, action type** – use the Admin
  tab, or the `/api/admin/*` endpoints. Retire (soft delete) rather than delete so history
  survives.
- **Change a colour, frequency, target, or instruction** – Admin tab (Severity levels /
  Department targets), or a `sql/` script. These are data; no redeploy needed.
- **Add an API endpoint** – add a method to the relevant controller following the existing
  pattern (inject `QualityAuditContext`, `AsNoTracking()` for reads, project to a DTO in
  `Models/Dtos.cs`). Admin endpoints start with `var guard = await GuardAsync(); if (guard != null) return guard;`.
- **Schema change** – write a `.sql` in `sql/`, run it in SSMS, then add/adjust the entity
  in `Models/Entities.cs` (or a keyless view in `Models/Views.cs`) and the context mapping.
  No migration.

## Gotchas

- The aggregate views have no date column, so date-ranged maths is computed from the base
  tables (see `SeverityMath` + `WeekHelper.WeeksBetween`), not from the views.
- After a frontend change, a stale `index.html` on the server or in the browser cache is
  the usual "it didn't update" cause — redeploy the file and Ctrl+F5.
- Photos need a persisted result first; the frontend saves a draft automatically before
  uploading so a `resultId` exists.
