-- Update database from version 200051 to 200052
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Define the new LexEntryType class.
insert into Class$ ([Id], [Mod], [Base], [Abstract], [Name])
	values(5118, 5, 7, 0, 'LexEntryType')
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5118001, 16, 5118, null, 'ReverseAbbr',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5118002, 2, 5118, null, 'Type',0,Null, 0, 127, null)

-- Define the new fields for LexEntry.
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002017, 14, 5002,
		null, 'SummaryDefinition2',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002018, 14, 5002,
		null, 'LiteralMeaning2',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002019, 28, 5002,
		0, 'MainEntriesOrSenses2',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002023, 24, 5002,
		7, 'Condition2',0,Null, null, null, null)
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5002024, 24, 5002,
		5118, 'EntryType',0,Null, null, null, null)

-- Define the new field for LexicalDatabase.
insert into [Field$]
	([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
	values(5005018, 23, 5005, 8, 'EntryTypes',0,Null, null, null, null)
go

-- Create the new list and its default set of items.
DECLARE @hvoLexDb int, @wsEn int, @hvoEntryTypeList int, @hvoNewItem int
SELECT @hvoLexDb=Dst from LanguageProject_LexicalDatabase
SELECT @wsEn=id from LgWritingSystem WHERE ICULocale=N'en'

EXEC CreateObject_CmPossibilityList @wsEn, N'Entry Types', null, null,	null, null, null, 1, 0,	0, 0, 1, 0, @wsEn, N'EntTyp',
	null, 0, 0, 5118, 0, -3, @hvoLexDb, 5005018, null, @hvoEntryTypeList output, null, 0, null
UPDATE CmObject SET Guid$='1EE09905-63DD-4C7A-A9BD-1D496743CCD6' WHERE id=@hvoEntryTypeList

EXEC CreateObject_LexEntryType	@wsEn, N'Compound', @wsEn, N'comp. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'comp.', 2, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='1F6AE209-141A-40DB-983C-BEE93AF0CA3C' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Derivation', @wsEn, N'der. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'der.', 2, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='98C273C4-F723-4FB0-80DF-EEDE2204DFCA' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Dialectal Variant', @wsEn, N'dial. var. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'dial. var.', 1, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='024B62C9-93B3-41A0-AB19-587A0030219A' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Free Variant', @wsEn, N'fr. var. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'fr. var.', 1, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='4343B1EF-B54F-4fA4-9998-271319A6D74C' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Idiom', @wsEn, N'id. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'id.', 2, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='B2276DEC-B1A6-4D82-B121-FD114C009C59' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Inflectional Variant', @wsEn, N'inf. var. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'inf. var.', 1, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='FCC61889-00E6-467B-9CF0-8C4F48B9A486' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Keyterm Phrase', @wsEn, N'kt. phr. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'kt. phr.', 2, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='CCE519D8-A9C5-4F28-9C7D-5370788BFBD5' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Main Entry', @wsEn, N'main', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'main', 0, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='5541D063-2D43-4E49-AAAD-BBA4AE5ECCD1' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Phrasal Verb', @wsEn, N'ph. v. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'ph. v.', 2, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='35CEE792-74C8-444E-A9B7-ED0461D4D3B7' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Saying', @wsEn, N'say. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'say.', 2, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='9466D126-246E-400B-8BBA-0703E09BC567' WHERE id=@hvoNewItem

EXEC CreateObject_LexEntryType	@wsEn, N'Spelling Variant', @wsEn, N'sp. var. of', null, null, null, 0, null, null, null, 0, 0, 0, 0, 0, 1,
	@wsEn, N'sp. var.', 1, @hvoEntryTypeList, 8008, null, @hvoNewItem output, null, 0, null
UPDATE CmObject SET Guid$='0C4663B3-4D9A-47af-B9A1-C8565D8112ED' WHERE id=@hvoNewItem
GO

---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200051
begin
	update Version$ set DbVer = 200052
	COMMIT TRANSACTION
	print 'database updated to version 200052'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200051 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
