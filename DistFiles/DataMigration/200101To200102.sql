-- update database FROM version 200101 to 200102
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Add new classes created by model change
-------------------------------------------------------------------------------

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(68, 0, 14, 0, 'StJournalText')
go

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(3017, 3, 0, 0, 'ScrBookAnnotations')
go

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(3018, 3, 37, 0, 'ScrSriptureNote')
go

-------------------------------------------------------------------------------
-- Add new fields and relations to StJournalText
-------------------------------------------------------------------------------

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(68001, 5, 68,
		null, 'DateCreated',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(68002, 5, 68,
		null, 'DateModified',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(68003, 24, 68,
		13, 'CreatedBy',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(68004, 24, 68,
		13, 'ModifiedBy',0,Null, null, null, null)

exec UpdateClassView$ 68, 1
GO

-------------------------------------------------------------------------------
-- Add new fields and relations to ScrScriptureNote
-------------------------------------------------------------------------------

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3018001, 2, 3018,
		null, 'ResolutionStatus',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3018002, 28, 3018,
		7, 'Categories',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3018003, 23, 3018,
		68, 'Quote',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3018004, 23, 3018,
		68, 'Discussion',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3018005, 23, 3018,
		68, 'Recommendation',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3018006, 23, 3018,
		68, 'Resolution',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3018007, 27, 3018,
		68, 'Responses',0,Null, null, null, null)

exec UpdateClassView$ 3018, 1
GO


-------------------------------------------------------------------------------
-- Add new fields and relations to ScrBookAnnotations
-------------------------------------------------------------------------------

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3017001, 24, 3017,
		3004, 'BookID',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3017002, 27, 3017,
		3018, 'Notes',0,Null, null, null, null)

exec UpdateClassView$ 3017, 1
GO

-------------------------------------------------------------------------------
-- Add new fields and relations to Scripture
-------------------------------------------------------------------------------

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3001024, 27, 3001,
		3017, 'BookAnnotations',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3001025, 23, 3001,
		8, 'ScriptureNoteCategories',0,Null, null, null, null)

exec UpdateClassView$ 3001, 1
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200101
begin
	UPDATE Version$ SET DbVer = 200102
	COMMIT TRANSACTION
	print 'database updated to version 200102'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200101 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
