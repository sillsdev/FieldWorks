-- update database FROM version 200093 to 200094
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Correct migration from 200092 to 200093, the type of the field for check
-- lists was not changed to make it a collection.
-------------------------------------------------------------------------------

ALTER TABLE Field$ DISABLE TRIGGER TR_Field$_No_Upd
update Field$ set Type = 25 where id = 6001050
ALTER TABLE Field$ ENABLE TRIGGER TR_Field$_No_Upd
GO

-------------------------------------------------------------------------------
-- Correct migration from 200092 to 200093, the original code to update the
-- Guid and OwnOrd was failing.
-------------------------------------------------------------------------------

ALTER TABLE CmObject DISABLE TRIGGER TR_CmObject_ValidateOwner
UPDATE [CmObject]
SET	Guid$ = '76FB50CA-F858-469c-B1DE-A73A863E9B10',
	OwnOrd$ = 1
WHERE [OwnFlid$] = 6001050
ALTER TABLE CmObject ENABLE TRIGGER TR_CmObject_ValidateOwner
GO


-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200093
begin
	UPDATE Version$ SET DbVer = 200094
	COMMIT TRANSACTION
	print 'database updated to version 200094'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200093 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
