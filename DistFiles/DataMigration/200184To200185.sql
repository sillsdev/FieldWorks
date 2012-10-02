-- Update database from version 200184 to 200185
-- This fixes a botch in migrating from 200072 to 200073 which may affect only a handful of DBs.
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

DECLARE @cflid INT
SELECT @cflid=COUNT(*) FROM Field$ WHERE Id=5035003
IF @cflid = 0
BEGIN
	-- Add MoForm_IsAbstract : boolean
	INSERT INTO [Field$]
		([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
		VALUES(5035003, 1, 5035, NULL, 'IsAbstract', 0, NULL, NULL, NULL, NULL)
END
---------------------------------------------------------------------
DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200184
BEGIN
	UPDATE Version$ SET DbVer = 200185
	COMMIT TRANSACTION
	PRINT 'database updated to version 200185'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200184 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
