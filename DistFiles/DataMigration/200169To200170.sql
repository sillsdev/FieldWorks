-- Update database FROM version 200169 to 200170
BEGIN TRANSACTION  --( will be rolled back if wrong version#)
-----------------------------------------------------------------------
-- Add Genre possibility list and new Text properties
-----------------------------------------------------------------------
insert into [Field$]
([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
values(5054009, 26, 5054, 7, 'Genres',0,Null, null, null, null)

insert into [Field$]
([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
values(5054010, 16, 5054, null, 'Abbreviation',0,Null, null, null, null)

insert into [Field$]
([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
values(5054011, 1, 5054, null, 'IsTranslated',0,Null, null, null, null)

insert into [Field$]
([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Min], [Max], [Big])
values(6001052, 23, 6001, 8, 'GenreList',0,Null, null, null, null)

GO

-----------------------------------------------------------------------
-- Create initial Genre list
-----------------------------------------------------------------------

declare @lp int, @list int, @en int, @item1 int, @item2 int, @item3 int, @now datetime, @guid uniqueidentifier, @fmt varbinary(4000)
select @now = getdate()
select top 1 @lp=id from LanguageProject
select @en=id from LgWritingSystem where ICULocale = 'en'
select top 1 @fmt=Fmt from MultiStr$ where Ws=@en order by Fmt

exec CreateObject_CmPossibilityList @en, 'Genres', @now, @now,
	@en, 'This list holds genre by which interlinear texts can be classified.', @fmt,
	127, 0, 1, 0, 0, 0, @en, 'gnrs', NULL, 0, 0, 7, 0, -3, NULL,
	@lp, 6001052, NULL, @list output, @guid output

exec CreateObject_CmPossibility @en, 'Monologue', @en, 'mnlg',
	@en, 'A text with a single participant, typically a narrator addressing an audience', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@list, 8008, NULL, @item1 output, @guid output

exec CreateObject_CmPossibility @en, 'Narrative', @en, 'nar',
	@en, 'An account of events, a text that describes or projects a contingent succession of actions', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'History', @en, 'his',
	@en, 'An account of real events', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Fiction', @en, 'fict',
	@en, 'An account of invented events', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Traditional Narrative', @en, 'tr. nar',
	@en, 'A traditional account of events of unknown historicity', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Prophecy', @en, 'proph',
	@en, 'An account of events that are projected to happen at a later time, typically involving a supernatural agency', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Procedure', @en, 'proc',
	@en, 'A text that describes or explains a process', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Descriptive Procedure', @en, 'desc. proc',
	@en, 'Describes how something was done', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Prescriptive Procedure', @en, 'pres. proc',
	@en, 'Gives instructions about how to do something', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Behavioral Text', @en, 'beh. txt',
	@en, 'A text that motivates or describes behavior without a framework of contingent succession', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Hortatory Text', @en, 'hor. txt',
	@en, 'A text intended to motivate the audience to do something or to act or to believe in a particular way; a.k.a. Motivational Text', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Normative Text', @en, 'norm. txt',
	@en, 'A text intended to express the norms of a society for the purpose of motivating someone to conform to those norms', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Promisory Text', @en, 'pro. txt',
	@en, 'A text in which the originator promises to do something or to act or to believe in a particular way', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Behavioral Description', @en, 'beh. desc',
	@en, 'A text, such as a Eulogy, that describes the accomplishments of a person without a contingent succession of actions', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Exposition', @en, 'expo',
	@en, 'A text that explains something other than a behavior, a process or a succession of events; typically it explains facts or ideas', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Projection', @en, 'proj',
	@en, 'A text dealing with projected facts or planned status (not a proposal for action, which would be Hortatory; not focusing on human or divine agency, which would be Prophecy)', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Descriptive Exposition', @en, 'desc. expo',
	@en, 'Exposition that describes an object, location or situation in static terms', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Didactic Exposition', @en, 'did. expo',
	@en, 'Exposition specifically intended for teaching another person about a body of facts', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item2, 7004, NULL, @item3 output, @guid output

exec CreateObject_CmPossibility @en, 'Dialogue', @en, 'dia',
	@en, 'Literal speech between individuals or quoted text that uses quotation formulas (typically using the verb ''to say'')', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@list, 8008, NULL, @item1 output, @guid output

exec CreateObject_CmPossibility @en, 'Conversation', @en, 'conv',
	@en, 'Dialogue involving mutual cooperation between the speakers', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Verbal Conflict', @en, 'verb. confl',
	@en, 'Dialogue in which the speakers resist mutual resolution of one or more propositions', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Negotiation', @en, 'neg',
	@en, 'Dialogue in which a speaker seeks to obtain a benefit from the other speaker', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Drama', @en, 'dram',
	@en, 'Acted out speech or quoted text that does not use quotation formulas', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Word List', @en, 'wd. lst',
	@en, 'A list of words used for language research or documentation', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@list, 8008, NULL, @item1 output, @guid output

exec CreateObject_CmPossibility @en, 'Paradigm', @en, 'para',
	@en, 'A list of well-formed words, typically arranged according to inflection', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Ill-formed Words', @en, 'ill. wds',
	@en, 'A list of words that are not well-formed in the language, e.g. an anti-paradigm or list of common misspellings', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Metrical Category', @en, 'met. cat',
	@en, 'Genre according to metrical form (versus content)', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@list, 8008, NULL, @item1 output, @guid output

exec CreateObject_CmPossibility @en, 'Prose', @en, 'pros',
	@en, 'Non-metrical arrangement of the text, typically using the grammar of ordinary speech', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

exec CreateObject_CmPossibility @en, 'Poetry', @en, 'poet',
	@en, 'Metrical arrangement of the text, typically employing rules for rhyme and rhythm', @fmt,
	0, @now, @now, NULL, -1073741824, -1073741824, -1073741824, 0, 0, 0,
	@item1, 7004, NULL, @item2 output, @guid output

GO
-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200169
BEGIN
	UPDATE [Version$] SET [DbVer] = 200170
	COMMIT TRANSACTION
	PRINT 'database updated to version 200170'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200169 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
