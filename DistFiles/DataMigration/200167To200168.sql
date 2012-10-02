-- Update database FROM version 200167 to 200168

BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-----------------------------------------------------------------------
-- GetScrBookTitle and GetScrBookInfo no longer needed
-----------------------------------------------------------------------

IF OBJECT_ID('GetScrBookTitle') IS NOT NULL BEGIN
	PRINT 'removing function GetScrBookTitle'
	DROP PROCEDURE GetScrBookTitle
END
GO

IF OBJECT_ID('GetScrBookInfo') IS NOT NULL BEGIN
	PRINT 'removing function GetScrBookInfo'
	DROP PROCEDURE GetScrBookInfo
END
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200167
BEGIN
	UPDATE [Version$] SET [DbVer] = 200168
	COMMIT TRANSACTION
	PRINT 'database updated to version 200168'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200167 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
