-- update database FROM version 200112 to 200113
-- fix the deficiencies of the the 200110 to 200111 migration... :-(
-- also fix a few other problems introduced by bad code recently.

BEGIN TRANSACTION  --( will be rolled back if wrong version#

ALTER TABLE LexEntry ADD ImportResidue ntext NULL
ALTER TABLE LexEntry ADD ImportResidue_Fmt image NULL
go
UPDATE LexEntry
SET ImportResidue=ImportResidue2, ImportResidue_Fmt=ImportResidue2_Fmt
go
ALTER TABLE LexEntry DROP COLUMN ImportResidue2
ALTER TABLE LexEntry DROP COLUMN ImportResidue2_Fmt
go
exec UpdateClassView$ 5002, 1

ALTER TABLE LexSense ADD ImportResidue ntext NULL
ALTER TABLE LexSense ADD ImportResidue_Fmt image NULL
go
UPDATE LexSense
SET ImportResidue=ImportResidue2, ImportResidue_Fmt=ImportResidue2_Fmt
go
ALTER TABLE LexSense DROP COLUMN ImportResidue2
ALTER TABLE LexSense DROP COLUMN ImportResidue2_Fmt
go
exec UpdateClassView$ 5016, 1

ALTER TABLE PhEnvironment ADD StringRepresentation nvarchar(4000) NULL
ALTER TABLE PhEnvironment ADD StringRepresentation_Fmt varbinary(8000) NULL
go
UPDATE PhEnvironment
SET StringRepresentation=StringRepresentation2, StringRepresentation_Fmt=StringRepresentation2_Fmt
go
ALTER TABLE PhEnvironment DROP COLUMN StringRepresentation2
ALTER TABLE PhEnvironment DROP COLUMN StringRepresentation2_Fmt
go
exec UpdateClassView$ 5097, 1

-- Remove any writing system with the ICU Locale of 'all analysis', plus any Reversal Index
-- created for that writing system.
DECLARE @wsBad int, @riBad int
DECLARE @nvId nvarchar(20)
SELECT @wsBad=Id FROM LgWritingSystem WHERE ICULocale='all analysis'
IF @wsBad is not null BEGIN
	SELECT @riBad=Id FROM ReversalIndex WHERE WritingSystem=@wsBad
	IF @riBad IS NOT NULL BEGIN
		SET @nvId = CONVERT(nvarchar(20), @riBad)
		EXEC DeleteObjects @nvId
	END
	SET @nvId = CONVERT(nvarchar(20), @wsBad)
	EXEC DeleteObjects @nvId
END
GO
-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200112
begin
	UPDATE Version$ SET DbVer = 200113
	COMMIT TRANSACTION
	print 'database updated to version 200113'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200112 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
