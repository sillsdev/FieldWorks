-- update database FROM version 200113 to 200114

-- add phrase and discontiguous phrase to morph types.

BEGIN TRANSACTION  --( will be rolled back if wrong version#

declare @list int, @en int, @type int, @now datetime, @guid uniqueidentifier, @fmt varbinary(4000)
select @now = getdate()
select @list=id from CmObject where guid$ = 'd7f713d8-e8cf-11d3-9764-00c04f186933'
select @en=id from LgWritingSystem where ICULocale = 'en'
select top 1 @fmt=Fmt from MultiStr$ where Ws=@en order by Fmt
exec CreateObject_MoMorphType @en, 'phrase', @en, 'phr',
	@en, 'A phrase is a syntactic structure that consists of more than one word but lacks the subject-predicate organization of a clause.',
	@fmt, 0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 1, NULL, NULL, 0,
	@list, 8008, NULL, @type output, @guid output
update CmObject set guid$ = 'A23B6FAA-1052-4F4D-984B-4B338BDAF95F' where id = @type
exec CreateObject_MoMorphType @en, 'discontiguous phrase', @en, 'dis phr',
	@en, 'A discontiguous phrase has discontiguous constituents which (a) are separated from each other by one or more intervening constituents, and (b) are considered either (i) syntactically contiguous and unitary, or (ii) realizing the same, single meaning. An example is French ne...pas.',
	@fmt, 0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 1, NULL, NULL, 0,
	@list, 8008, NULL, @type output, @guid output
update CmObject set guid$ = '0CC8C35A-CEE9-434D-BE58-5D29130FBA5B' where id = @type
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200113
begin
	UPDATE Version$ SET DbVer = 200114
	COMMIT TRANSACTION
	print 'database updated to version 200114'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200113 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
