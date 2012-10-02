-- Update database from version 200064 to 200065
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Remove obsolete UserViewFields
DECLARE @Id int
DECLARE uvfCursor CURSOR local static forward_only read_only FOR
	select uvf.Id from UserViewField uvf
	left outer join field$ f on f.id = uvf.flid
	where uvf.flid > 999 and f.id is null
OPEN uvfCursor
FETCH NEXT FROM uvfCursor INTO @Id
WHILE @@FETCH_STATUS = 0
BEGIN
	EXEC DeleteObj$ @Id
	FETCH NEXT FROM uvfCursor INTO @Id
END
CLOSE uvfCursor
DEALLOCATE uvfCursor

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200064
begin
	update Version$ set DbVer = 200065
	COMMIT TRANSACTION
	print 'database updated to version 200065'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200064 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
