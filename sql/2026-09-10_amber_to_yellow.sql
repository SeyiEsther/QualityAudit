-- ============================================================================
-- Change severity 2 ("Amber") colour to a clearly distinct yellow.
--
-- Reason: a colour-blind auditor finds the current amber (#b45309) too close to
-- the red (#b91c1c). Only the colour changes — the label stays "Amber".
--
-- Safe: ColourHex is a display attribute of the severity rule set. Results store
-- SeverityAtAudit as the NUMBER (2), never the colour, so this does NOT alter or
-- invalidate any existing submission. The app reads ColourHex from this table at
-- runtime, so no redeploy is required — a browser refresh picks up the new colour.
--
-- Run in SSMS on CSMSVR02.
-- ============================================================================
USE RittalQualityAudit;
GO

UPDATE dbo.SeverityLevels
SET ColourHex = '#FF8200'          -- bright Fanta-style yellow-orange, clearly distinct from red
WHERE Severity = 2;
GO

SELECT Severity, Name, ColourHex FROM dbo.SeverityLevels ORDER BY Severity;
GO
