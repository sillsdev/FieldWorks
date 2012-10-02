-- update database FROM version 200164 to 200165

BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Add MoStemMsa.FromPartsOfSpeech property
-------------------------------------------------------------------------------
insert into [Field$]
([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
values(5001007, 26, 5001,
5049, 'FromPartsOfSpeech',0,Null, null, null, null)
GO

-------------------------------------------------------------------------------
-- Add clitic morph type
-------------------------------------------------------------------------------
declare @list int, @en int, @es int, @fr int, @type int, @now datetime, @guid uniqueidentifier
declare @fmt varbinary(8000)
declare @oldDesc nvarchar(2000), @newDesc nvarchar(2000)
select @now = getdate()
select @list=id from CmObject where guid$ = 'd7f713d8-e8cf-11d3-9764-00c04f186933'
select @en=id from LgWritingSystem where ICULocale = 'en'
select @es=id from LgWritingSystem where ICULocale = 'es'
select @fr=id from LgWritingSystem where ICULocale = 'fr'
select top 1 @fmt=Fmt from MultiStr$ where Ws=@en order by Fmt
exec CreateObject_MoMorphType @en, 'clitic', @en, 'clit', @en, 'A clitic is a morpheme that has syntactic characteristics of a word, but shows evidence of being phonologically bound to another word. Orthographically, it stands alone.', @fmt, 0,
	@now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 1, null, null, 0,
	@list, 8008, NULL, @type output, @guid output
insert CmPossibility_Name (obj, ws, txt) values (@type, @fr, N'clitique')
insert CmPossibility_Name (obj, ws, txt) values (@type, @es, N'cl' + nchar(237) + 'tico')
insert CmPossibility_Abbreviation (obj, ws, txt) values (@type, @fr, 'clit')
insert CmPossibility_Abbreviation (obj, ws, txt) values (@type, @es, 'clit')
update CmObject set guid$ = 'C2D140E5-7CA9-41f4-A69A-22FC7049DD2C' where id = @type

-------------------------------------------------------------------------------
-- Update descriptions for enclitic and proclitic
-------------------------------------------------------------------------------
set @newDesc = 'An enclitic is a clitic that is phonologically joined at the end of a preceding word to form a single unit. Orthographically, it may attach to the preceding word.'
select @type=id from CmObject where guid$ = 'D7F713E1-E8CF-11D3-9764-00C04F186933'
select @oldDesc = txt from CmPossibility_Description where obj = @type and ws = @en
if @oldDesc is null begin
   insert CmPossibility_Description (obj, ws, txt, fmt) values (@type, @en, @newDesc, @fmt)
end else begin
   update CmPossibility_Description set txt = @newDesc where obj = @type and ws = @en
end

set @newDesc = 'A proclitic is a clitic that precedes the word to which it is phonologically joined. Orthographically, it may attach to the following word.'
select @type=id from CmObject where guid$ = 'D7F713E2-E8CF-11D3-9764-00C04F186933'
select @oldDesc = txt from CmPossibility_Description where obj = @type and ws = @en
if @oldDesc is null begin
   insert CmPossibility_Description (obj, ws, txt, fmt) values (@type, @en, @newDesc, @fmt)
end else begin
   update CmPossibility_Description set txt = @newDesc where obj = @type and ws = @en
end
-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200164
begin
	UPDATE Version$ SET DbVer = 200165
	COMMIT TRANSACTION
	print 'database updated to version 200165'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200164 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
