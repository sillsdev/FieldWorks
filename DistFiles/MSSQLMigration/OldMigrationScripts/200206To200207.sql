-- Update database from version 200206 to 200207
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- 200202To200203.sql incorrectly renamed class 5110 with a final capital R.
-- It should be a lowercase r instead for C# code to work.
-------------------------------------------------------------------------------
-- first go to an intermediate name which won't conflict case insensitively.
EXECUTE RenameClass 5110, 'MoAdhocProhibGRX'
EXECUTE RenameClass 5110, 'MoAdhocProhibGr'

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200206
BEGIN
	UPDATE Version$ SET DbVer = 200207
	COMMIT TRANSACTION
	PRINT 'database updated to version 200207'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200206 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
