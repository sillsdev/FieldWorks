-- Update database from version 200245 to 200246
BEGIN TRANSACTION  --( will be rolled back if wrong version #)
-------------------------------------------------------------------------------
-- Added missing command when deleting LexEntry_Condition in 200242To200243 migration.
-- Any time we delete a reference field, we need to call CreateDeleteObj on the referenced class
-- in order to update the TR_xxx_ObjDel_Del trigger.
-- This fixes LT-9325-9330.

exec CreateDeleteObj 7  -- CmPossibility
go

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200245
BEGIN
	UPDATE Version$ SET DbVer = 200246
	COMMIT TRANSACTION
	PRINT 'database updated to version 200246'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200245 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
