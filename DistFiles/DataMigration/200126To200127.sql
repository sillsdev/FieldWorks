-- update database FROM version 200126 to 200127
-- Fix possible botched format values from bug in XML import code.
-- Note that this migration also has an XML file for updating the Semantic Domain List.

BEGIN TRANSACTION  --( will be rolled back if wrong version#

DECLARE @fmtResidue varbinary(20)
DECLARE @wsAnal int
SELECT @wsAnal=Id FROM LgWritingSystem WHERE ICULocale=N'en'
IF @wsAnal is null
	SELECT TOP 1 @wsAnal=Dst FROM LanguageProject_CurrentAnalysisWritingSystems ORDER BY Ord
IF @wsAnal is null
	SELECT TOP 1 @wsAnal=Dst FROM LanguageProject_AnalysisWritingSystems
SET @fmtResidue = dbo.fnGetFormatForWs(@wsAnal)

UPDATE LexSense
SET ImportResidue_Fmt=@fmtResidue
WHERE DATALENGTH(ImportResidue_Fmt)=400 AND
	  SUBSTRING(ImportResidue_Fmt, 0, 8)=0x0000000000000000

UPDATE LexEntry
SET ImportResidue_Fmt=@fmtResidue
WHERE DATALENGTH(ImportResidue_Fmt)=400 AND
	  SUBSTRING(ImportResidue_Fmt, 0, 8)=0x0000000000000000
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200126
begin
	UPDATE Version$ SET DbVer = 200127
	COMMIT TRANSACTION
	print 'database updated to version 200127'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200126 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
