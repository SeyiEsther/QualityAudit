-- Add three auditors (active, non-admin). Additive and idempotent, matched by email.
-- The IsAuditor update runs only if that column exists. Run in SSMS on CSMSVR02.
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

IF COL_LENGTH('dbo.AuditUsers', 'IsAuditor') IS NOT NULL
    EXEC(N'UPDATE dbo.AuditUsers SET IsAuditor = 1
           WHERE Email IN (N''shosking@rittal-csm.co.uk'',
                           N''zbobrowska@rittal-csm.co.uk'',
                           N''asmith@rittal-csm.co.uk'');');
GO

SELECT Id, DisplayName, Email, IsAdmin, IsActive FROM dbo.AuditUsers ORDER BY Id;
GO
