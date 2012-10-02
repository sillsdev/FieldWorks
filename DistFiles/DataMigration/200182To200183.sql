-- update database FROM version 200182 to 200183
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FDB-176: Migration for removing fixed path from file names
-------------------------------------------------------------------------------

--( We have to find directory names with \Pictures\ or \Media\ starting from
--( the *end* of the file. CHARINDEX doesn't work from the end of the string
--( so we have to kludge it by using REVERSE. Still works fast enough.

UPDATE CmFile
SET InternalPath = SUBSTRING(InternalPath,
	LEN(InternalPath) - (CHARINDEX('\serutciP\', REVERSE(InternalPath)) + 7),
	CHARINDEX('\serutciP\', REVERSE(InternalPath)) + 8)
WHERE InternalPath LIKE '%\Pictures\%'

UPDATE CmFile
SET InternalPath = SUBSTRING(InternalPath,
	LEN(InternalPath) - (CHARINDEX('\aideM\', REVERSE(InternalPath)) + 4),
	CHARINDEX('\aideM\', REVERSE(InternalPath)) + 5)
WHERE InternalPath LIKE '%\Media\%'

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200182
BEGIN
	UPDATE [Version$] SET [DbVer] = 200183
	COMMIT TRANSACTION
	PRINT 'database updated to version 200183'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200182 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
