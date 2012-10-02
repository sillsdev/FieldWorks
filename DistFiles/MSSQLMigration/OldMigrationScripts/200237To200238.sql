-- Update database from version 200237 to 200238
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- LT-7277:  LexEntry - Add EntryRefs: owning sequence of LexEntryRef.
-- Add new LexEntryRef class, subclass of CmObject
-- Add attributes/relationships, VariantEntryTypes: sequence reference to LexEntryType,
--     ComplexEntryTypes sequence reference to LexEntryType, PrimaryLexemes: sequence reference to CmObject,
--     ComponentLexemes: sequence reference to CmObject, HideMinorEntry: Integer, Summary: MultiString.
-- LexDb - Add VariantEntryTypes: atomic owning CmPossibilityList,
--         ComplexEntryTypes: atomic owning CmPossibilityList.
-------------------------------------------------------------------------------

insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5127, 5, 0, 0, 'LexEntryRef')
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002034, 27, 5002,
		5127, 'EntryRefs',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5127001, 28, 5127,
		5118, 'VariantEntryTypes',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5127002, 28, 5127,
		5118, 'ComplexEntryTypes',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5127003, 28, 5127,
		0, 'PrimaryLexemes',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5127004, 28, 5127,
		0, 'ComponentLexemes',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5127005, 2, 5127,
		null, 'HideMinorEntry',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5127006, 14, 5127,
		null, 'Summary',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5005022, 23, 5005,
		8, 'VariantEntryTypes',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5005023, 23, 5005,
		8, 'ComplexEntryTypes',0,Null, null, null, null)
go
---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200237
BEGIN
	UPDATE Version$ SET DbVer = 200238
	COMMIT TRANSACTION
	PRINT 'database updated to version 200238'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200237 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
