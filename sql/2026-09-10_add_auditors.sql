-- ============================================================================
-- Add three auditors to dbo.AuditUsers: Suzanne, Suzu, Adam.
-- Active, non-admin. Surnames taken from their email addresses.
--
-- Safe: additive only. Submissions.Auditor is free text, so no existing
-- submission is affected. New active users simply appear in the auditor picker.
-- Idempotent: re-running skips anyone already present (matched by email).
--
-- NOTE: the known AuditUsers schema is (Id, DisplayName, Email, Username,
-- IsAdmin, IsActive) — there is no IsAuditor column, so these are inserted as
-- IsAdmin = 0, IsActive = 1 (every active user is an auditor). If an IsAuditor
-- column HAS since been added, the guarded block at the end sets it to 1 too.
--
-- Run in SSMS on CSMSVR02.
-- ============================================================================
USE RittalQualityAudit;
GO

INSERT INTO dbo.AuditUsers (DisplayName, Email, IsAdmin, IsActive)
SELECT v.DisplayName, v.Email, 0, 1
FROM (VALUES
    (N'Suzanne Hosking', N'shosking@rittal-csm.co.uk'),
    (N'Suzu Bobrowska',  N'zbobrowska@rittal-csm.co.uk'),
    (N'Adam Smith',      N'asmith@rittal-csm.co.uk')
) AS v (DisplayName, Email)
WHERE NOT EXISTS (SELECT 1 FROM dbo.AuditUsers u WHERE u.Email = v.Email);
GO

-- Only if an IsAuditor column exists (schema addition) — otherwise skipped cleanly.
IF COL_LENGTH('dbo.AuditUsers', 'IsAuditor') IS NOT NULL
    EXEC(N'UPDATE dbo.AuditUsers SET IsAuditor = 1
           WHERE Email IN (N''shosking@rittal-csm.co.uk'',
                           N''zbobrowska@rittal-csm.co.uk'',
                           N''asmith@rittal-csm.co.uk'');');
GO

SELECT Id, DisplayName, Email, IsAdmin, IsActive FROM dbo.AuditUsers ORDER BY Id;
GO
