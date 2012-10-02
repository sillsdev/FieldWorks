-- Update database from version 200255 to 200256
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- CLE-91: Scripture Notes Category needs to support heirarchy.
-------------------------------------------------------------------------------

DECLARE @Id INT
SELECT @Id = Dst FROM Scripture_NoteCategories
UPDATE CmPossibilityList SET Depth = 127 WHERE Id = @Id
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200255
BEGIN
	UPDATE Version$ SET DbVer = 200256
	COMMIT TRANSACTION
	PRINT 'database updated to version 20025'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200255 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
