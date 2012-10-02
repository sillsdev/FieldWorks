-- update database FROM version 200092 to 200093
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- Add ListVersion (guid) attribute to CmPossibilityList (FWM-83)
-------------------------------------------------------------------------------
INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (8021, 6, 8, NULL, 'ListVersion', 0, NULL, NULL)
GO

-------------------------------------------------------------------------------
-- Change LanguageProject.CheckLists from atomic to collection (FWM-72)
-- Current contents moved to Key Terms checklist
-------------------------------------------------------------------------------
-- (SteveMiller) This is fixed in the next data migration, but I fixed it
-- here since I was here anyway.
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
INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (3005005, 2, 3005, NULL, 'VerseRefMin', 0, NULL, NULL)
GO

-- add ScrSection.VerseRefMax
INSERT INTO Field$ (Id, Type, Class, DstCls, Name, Custom, CustomId, Big)
VALUES (3005006, 2, 3005, NULL, 'VerseRefMax', 0, NULL, NULL)
GO

-- copy values from ScrSection.VerseRefStart to ScrSection.VerseRefMin
--              and ScrSection.VerseRefEnd   to ScrSection.VerseRefMax
UPDATE ScrSection SET VerseRefMin=VerseRefStart, VerseRefMax=VerseRefEnd
GO


-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200092
begin
	UPDATE Version$ SET DbVer = 200093
	COMMIT TRANSACTION
	print 'database updated to version 200093'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200092 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
