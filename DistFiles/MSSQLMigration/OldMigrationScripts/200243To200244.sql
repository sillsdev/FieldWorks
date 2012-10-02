-- Update database from version 200243 to 200244
BEGIN TRANSACTION  --( will be rolled back if wrong version #)
-------------------------------------------------------------------------------
-- Added missed data migration after adding LexEntryRef_ RefType

update LexEntryRef set RefType = 1
where id in (select distinct id from LexEntryRef er
left outer join LexEntryRef_ComplexEntryTypes et on et.src = er.id
left outer join LexEntryRef_PrimaryLexemes pl on pl.src = er.id
where et.dst is not null or pl.dst is not null)
go

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200243
BEGIN
	UPDATE Version$ SET DbVer = 200244
	COMMIT TRANSACTION
	PRINT 'database updated to version 200244'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200243 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
