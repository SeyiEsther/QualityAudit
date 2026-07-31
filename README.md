# RittalQualityAudit — Sheet Metal Shift Audit (QA 343-31)

Digital replacement for the paper QA 343-31 sheet. ASP.NET Core 8 Web API with a
single-page HTML/JS frontend. No React, no npm build step, no Entity Framework —
Dapper over `Microsoft.Data.SqlClient`, consistent with the TL portal.

## Project layout

```
Program.cs                     minimal host: controllers + static files
appsettings.json               connection string
Controllers/
  AuditItemsController.cs       GET  /api/audit-items
  SubmissionsController.cs      POST /api/submissions
  DashboardController.cs        GET  /api/dashboard/summary, /api/dashboard/failures
Models/                         request/response shapes
Services/DatabaseService.cs     all Dapper queries
wwwroot/index.html              the whole frontend (entry form + dashboard)
```

## Database

The `RittalQualityAudit` database on **CSMSVR02** already exists (tables
`AuditItems`, `Submissions`, `Results` plus the `vw_*` views). This app only
connects and queries — it never creates or migrates schema.

Connection string lives in `appsettings.json`:

```
Server=CSMSVR02;Database=RittalQualityAudit;Trusted_Connection=True;TrustServerCertificate=True;
```

## Run locally

```
dotnet restore
dotnet run
```

Then open the URL shown (e.g. http://localhost:5000). The machine list is loaded
from the database at page load — nothing is hardcoded in the HTML, so retiring or
adding a machine is a data change in `dbo.AuditItems` (set `IsActive = 0` to retire).

## Publish to IIS (csm-srv-16)

```
dotnet publish -c Release -o publish
```

Copy the `publish/` folder to the site on csm-srv-16 and point the IIS app pool
at it (No Managed Code, same as the TL portal). The app pool identity needs
`Trusted_Connection` access to `RittalQualityAudit` on CSMSVR02.

## API summary

| Method | Route | Purpose |
| ------ | ----- | ------- |
| GET  | `/api/audit-items` | Active audit items, ordered by `SortOrder`. |
| POST | `/api/submissions` | Save one shift audit (header + checked results) in a transaction. |
| GET  | `/api/dashboard/summary?from=&to=` | KPIs, pass-rate breakdowns, recent shifts. |
| GET  | `/api/dashboard/failures?from=&to=` | Failure board from `vw_Failures`, severity 3 first. |

`from`/`to` default to the last 30 days when omitted.

> Note: the `vw_PassRateBySeverity` / `vw_PassRateByLocation` views aggregate across
> all history with no date column, so the date-ranged `summary` endpoint computes
> the same OK/NOK breakdown directly from the base tables. `vw_Failures` exposes
> `AuditDate`, so the failure board is served straight from that view.
