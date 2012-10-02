-- Update database from version 200053 to 200054
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Fix problem with failed data migration
exec UpdateClassView$ 5007, 1
go

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200053
begin
	update Version$ set DbVer = 200054
	COMMIT TRANSACTION
	print 'database updated to version 200054'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200053 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
