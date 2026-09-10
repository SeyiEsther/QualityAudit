# RittalQualityAudit

Digital replacement for the paper QA 343-34 shift audit form, covering Sheet Metal
and Assembly. ASP.NET Core 8 with EF Core, a single-page frontend in
`wwwroot/index.html`, backed by SQL Server (`RittalQualityAudit` on CSMSVR02).

## Run

```
dotnet run
```

Open http://localhost:5000. The connection string key is `RittalQualityAudit`
(`appsettings.json`); photo attachments are written under `Storage:AttachmentRoot`.

## Tabs

New Audit, Dashboard, Coverage, History, Admin.

## Notes

- The database schema is hand-managed. The app maps onto the existing tables and
  views and never runs migrations. One-off changes live in `sql/` to run in SSMS.
- The audit week starts on Tuesday. Severity is set per machine each week and
  stored on each result at save time, so historical figures don't change when the
  current week's severity is edited.
- No authentication yet; admin access is checked against `AuditUsers`.
