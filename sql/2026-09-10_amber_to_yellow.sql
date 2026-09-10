-- Severity 2 colour -> Fanta-style yellow-orange, clearly distinct from red.
-- Display only; existing submissions are unaffected. Run in SSMS on CSMSVR02.
USE RittalQualityAudit;
GO

UPDATE dbo.SeverityLevels SET ColourHex = '#FF8200' WHERE Severity = 2;
GO

SELECT Severity, Name, ColourHex FROM dbo.SeverityLevels ORDER BY Severity;
GO
