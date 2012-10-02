-- update database FROM version 200098 to 200099
BEGIN TRANSACTION  --( will be rolled back if wrong version#


-------------------------------------------------------------------------------
-- Correct TR_Class$_InsLast trigger - it was incorrectly calling DefineCreateProc
-- when the class was an abstract class and this caused the insert of an abstract
-- class to fail.
-------------------------------------------------------------------------------

IF OBJECT_ID('TR_Class$_InsLast') IS NOT NULL BEGIN
	PRINT 'removing trigger TR_Class$_InsLast'
	DROP TRIGGER TR_Class$_InsLast
END
GO
PRINT 'creating trigger TR_Class$_InsLast'
GO

CREATE TRIGGER TR_Class$_InsLast ON Class$ FOR INSERT
AS
	DECLARE
		@nErr INT,
		@nClassid INT,
		@nAbstract BIT

	SELECT @nClassId = Id, @nAbstract = Abstract FROM inserted

	IF @nAbstract = 0 BEGIN
		EXEC @nErr = DefineCreateProc$ @nClassId
		IF @nErr <> 0 GOTO LFail
	END

	RETURN

LFail:
	ROLLBACK TRANSACTION
	RETURN
GO

EXEC sp_settriggerorder 'TR_Class$_InsLast', 'last', 'INSERT'
GO

-------------------------------------------------------------------------------
-- Update ImportSettings of Scripture to be an owned collection.
-------------------------------------------------------------------------------

ALTER TABLE Field$ DISABLE TRIGGER TR_Field$_No_Upd
update Field$ set Type = 25 where id = 3001007
ALTER TABLE Field$ ENABLE TRIGGER TR_Field$_No_Upd
GO

-------------------------------------------------------------------------------
-- Add new classes created by model change
-------------------------------------------------------------------------------

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(3016, 3, 0, 0, 'ScrMarkerMapping')
go

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(3013, 3, 0, 1, 'ScrImportSource')
go

exec UpdateClassView$ 3013, 1
GO

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(3014, 3, 3013, 0, 'ScrImportP6Project')
go

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(3015, 3, 3013, 0, 'ScrImportSFFiles')
go

-------------------------------------------------------------------------------
-- Add new fields and relations to ScrImportSettings
-------------------------------------------------------------------------------

insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3008005, 16, 3008,
		null, 'Name',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3008006, 16, 3008,
		null, 'Description',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3008007, 25, 3008,
		3013, 'ScriptureSources',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3008008, 25, 3008,
		3013, 'BackTransSources',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3008009, 25, 3008,
		3013, 'NoteSources',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3008010, 25, 3008,
		3016, 'ScriptureMappings',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3008011, 25, 3008,
		3016, 'NoteMappings',0,Null, null, null, null)
go

exec UpdateClassView$ 3008, 1
GO

-------------------------------------------------------------------------------
-- Add fields and relations to new ScrMarkerMapping class
-------------------------------------------------------------------------------
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016001, 13, 3016,
		null, 'BeginMarker',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016002, 13, 3016,
		null, 'EndMarker',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016003, 1, 3016,
		null, 'Excluded',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016004, 2, 3016,
		null, 'Target',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016005, 2, 3016,
		null, 'Domain',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016006, 13, 3016,
		null, 'ICULocale',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016007, 15, 3016,
		null, 'FootnoteMarker',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016008, 1, 3016,
		null, 'DisplayFootnoteMarker',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016009, 1, 3016,
		null, 'DisplayFootnoteReference',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016010, 24, 3016,
		17, 'Style',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3016011, 24, 3016,
		35, 'NoteType',0,Null, null, null, null)
go

exec UpdateClassView$ 3016, 1
GO

-------------------------------------------------------------------------------
-- Add fields and relations to new ScrImportP6Project class
-------------------------------------------------------------------------------
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3014001, 13, 3014,
		null, 'ParatextID',0,Null, null, null, null)
go

exec UpdateClassView$ 3014, 1
GO

-------------------------------------------------------------------------------
-- Add fields and relations to new ScrImportSFFiles class
-------------------------------------------------------------------------------
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3015001, 2, 3015,
		null, 'FileFormat',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3015002, 25, 3015,
		47, 'Files',0,Null, null, null, null)
go

exec UpdateClassView$ 3015, 1
GO

-------------------------------------------------------------------------------
-- Add fields and relations to new ScrImportSource class
-------------------------------------------------------------------------------
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3013001, 13, 3013,
		null, 'ICULocale',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(3013002, 24, 3013,
		35, 'NoteType',0,Null, null, null, null)
go

exec UpdateClassView$ 3013, 1
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200098
begin
	UPDATE Version$ SET DbVer = 200099
	COMMIT TRANSACTION
	print 'database updated to version 200099'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200098 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
