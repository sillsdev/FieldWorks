-- update database FROM version 200140 to 200141
BEGIN TRANSACTION  --( will be rolled back if wrong version#

------------------------------------------------------------
-- FWM-118 : Add ReversalIndexEntry.ReversalForm:MultiUnicode
------------------------------------------------------------
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5053006, 16, 5053, null, 'ReversalForm',0,Null, null, null, null)
go

------------------------------------------------------------
-- FWM-113 : Add LexicalDatabase.StylesheetVersion:Guid
------------------------------------------------------------
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5005020, 6, 5005, null, 'StylesheetVersion',0,Null, null, null, null)
go

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200140
begin
	UPDATE Version$ SET DbVer = 200141
	COMMIT TRANSACTION
	print 'database updated to version 200141'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200140 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO