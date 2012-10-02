-- update database from version 200039 to 200040
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------

--( FWM-76:
--(	1) Remove ReversalIndexes reference property from LanguageProject
--(	2) Add to LexicalDatabase, ReversalIndexes owning collection of ReversalIndex.

DELETE FROM Field$ WHERE Id = 6001010

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5005017, 25, 5005, 5052, 'ReversalIndexes', 0, NULL, NULL)

GO

--( FWM-77 (for ReversalIndexEntry):
--(	2) Add Form: Unicode
--(	3) Add WritingSystem atomic reference to LgWritingSystem

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5053004, 15, 5053, NULL, 'Form', 0, NULL, NULL)

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5053005, 24, 5053, 24, 'WritingSystem', 0, NULL, NULL)

--( FWM-74 (for MoInflectionalAffixMsa):
--( PartOfSpeech --> PartOfSpeech
--( Slot --> MoInflectionalAffixSlot

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5038005, 24, 5038, 5049, 'PartOfSpeech', 0, NULL, NULL)

INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (5038006, 24, 5038, 5036, 'Slot', 0, NULL, NULL)

-------------------------------------------------------------------------------

declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200039
begin
	update Version$ set DbVer = 200040
	COMMIT TRANSACTION
	print 'database updated to version 200040'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200039 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
