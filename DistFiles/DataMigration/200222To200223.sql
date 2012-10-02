-- Update database from version 200222 to 200223
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWM-145 and FWM-149: Add LgWritingSystem.SpellCheckDictionary, ScrDraft.Type
-------------------------------------------------------------------------------

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(24029, 15, 24,
		null, 'SpellCheckDictionary',0,Null, null, null, null)
go

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3010004, 2, 3010,
		null, 'Type',0,Null, null, null, null)
go

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200222
BEGIN
	UPDATE Version$ SET DbVer = 200223
	COMMIT TRANSACTION
	PRINT 'database updated to version 200223'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200222 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
