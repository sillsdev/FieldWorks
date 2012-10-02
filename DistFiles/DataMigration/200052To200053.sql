-- Update database from version 200052 to 200053
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Define the new fields for LexEntry.
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002025, 14, 5002,
		null, 'Comment2',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002026, 1, 5002,
		null, 'DoNotUseForParsing',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002027, 1, 5002,
		null, 'ExcludeAsHeadword',0,Null, null, null, null)
go

-- Change LexEntry from Abstract to concrete
UPDATE Class$ SET Abstract=0 WHERE [Name]='LexEntry' AND [Abstract]=1

-- Create CreateObject_LexEntry stored procedure.
declare @clid int
SELECT @clid=id FROM Class$ WHERE [Name]='LexEntry'
EXEC DefineCreateProc$ @clid

-- This updates the views for LexEntry and its subclasses.
EXEC UpdateClassView$ 5002

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200052
begin
	update Version$ set DbVer = 200053
	COMMIT TRANSACTION
	print 'database updated to version 200053'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200052 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
