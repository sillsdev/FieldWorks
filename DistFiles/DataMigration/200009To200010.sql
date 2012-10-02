-- update database from version 200009 to 200010

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200009
begin

	delete from field$ where id = 17010

	update Version$ set DbVer = 200010
	print 'database updated to version 200010'
end
else
begin
	print 'Update aborted: this works only if DbVer = 200009 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
