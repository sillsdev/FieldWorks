-- update database FROM version 200109 to 200110

BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Remove unused DeleteObject$
-------------------------------------------------------------------------------

if object_id('DeleteObject$') is not null begin
	drop proc [DeleteObject$]
end
go

-------------------------------------------------------------------------------
-- Class$ wasn't calling DefineCreateProc$ like it should've been. Make sure
-- databases are all OK. The following loop is from LangProjSP.sql. FDB-107.
-------------------------------------------------------------------------------

begin
	print 'creating CreateObject_ procedures...'

	declare @sClassName sysname, @clid int

	declare class_cur cursor local fast_forward for
	select	[Name], [Id]
	from	[Class$]
	where	[Abstract] = 0

	-- loop through each non-abstract class and build an ObjectCreate_ procedure
	open class_cur
	fetch class_cur into @sClassName, @clid
	while @@fetch_status = 0 begin
		exec DefineCreateProc$ @clid
		fetch class_cur into @sClassName, @clid
	end

	close class_cur
	deallocate class_cur
end
go

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200109
begin
	UPDATE Version$ SET DbVer = 200110
	COMMIT TRANSACTION
	print 'database updated to version 200110'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200109 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
