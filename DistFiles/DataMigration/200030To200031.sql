-- update database from version 200030 to 200031
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Fix views from previous migration
-------------------------------------------------------------------------------

exec UpdateClassView$ 3001, 1
exec UpdateClassView$ 34, 1
exec UpdateClassView$ 3010, 1
GO

-------------------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200030
begin
	update Version$ set DbVer = 200031
	COMMIT TRANSACTION
	print 'database updated to version 200031'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200030 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
