-- Update database from version 200076 to 200077
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Fixed a data migration problem crashing InterlinearText from 200006To200007

update Class$ set Base = 5 where id = 5054
go

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200076
begin
	update Version$ set DbVer = 200077
	COMMIT TRANSACTION
	print 'database updated to version 200076'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200076 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
