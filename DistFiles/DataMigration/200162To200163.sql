-- update database FROM version 200162 to 200163. This will redo the
-- changes from 200092 -> 200093 because that sometimes failed.
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Add ListVersion (guid) attribute to CmPossibilityList (FWM-83)
-------------------------------------------------------------------------------
if (NOT EXISTS(Select * from [Field$] WHERE [Id] = 8021))
BEGIN
	INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
	VALUES (8021, 6, 8, NULL, 'ListVersion', 0, NULL, NULL)
END
GO

-------------------------------------------------------------------------------
-- Change LanguageProject.CheckLists from atomic to collection (FWM-72)
-- Current contents moved to Key Terms checklist
-------------------------------------------------------------------------------
ALTER TABLE CmObject DISABLE TRIGGER TR_CmObject_ValidateOwner
if (EXISTS(Select * from [CmObject] WHERE [OwnFlid$] = 6001050))
BEGIN
	UPDATE [CmObject]
	SET	Guid$ = '76FB50CA-F858-469c-B1DE-A73A863E9B10',
		OwnOrd$ = 1
	WHERE [OwnFlid$] = 6001050
END
ALTER TABLE CmObject ENABLE TRIGGER TR_CmObject_ValidateOwner
GO

-------------------------------------------------------------------------------
-- Add VerseRefMin (integer) and VerseRefMax (integer) to ScrSection (FWM-92)
-- populate with data from VerseRefStart and verseRefEnd as approximation
-------------------------------------------------------------------------------
-- add ScrSection.VerseRefMin
if (NOT EXISTS(Select * from [Field$] WHERE [Id] = 3005005))
BEGIN
	INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
	VALUES (3005005, 2, 3005, NULL, 'VerseRefMin', 0, NULL, NULL)
END
GO

-- add ScrSection.VerseRefMax
if (NOT EXISTS(Select * from [Field$] WHERE [Id] = 3005006))
BEGIN
	INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
	VALUES (3005006, 2, 3005, NULL, 'VerseRefMax', 0, NULL, NULL)
END
GO

-- copy values from ScrSection.VerseRefStart to ScrSection.VerseRefMin
--              and ScrSection.VerseRefEnd   to ScrSection.VerseRefMax
UPDATE ScrSection SET VerseRefMin=VerseRefStart, VerseRefMax=VerseRefEnd
	WHERE VerseRefMin is NULL OR VerseRefMax is NULL
GO


-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200162
begin
	UPDATE Version$ SET DbVer = 200163
	COMMIT TRANSACTION
	print 'database updated to version 200163'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200162 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
