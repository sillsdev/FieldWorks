-- update database FROM version 200107 to 200108

BEGIN TRANSACTION  --( will be rolled back if wrong version#

BEGIN
	DECLARE @id int, @peopleList int, @english int, @hvoStTxtPara int
-------------------------------------------------------------------------------
-- Create a "Default User" CmPerson with a fixed GUID
-------------------------------------------------------------------------------
	SELECT	@english = [id]
	FROM	LgWritingSystem
	WHERE	[ICULocale] = 'en'

	SELECT @peopleList = [Dst] FROM LanguageProject_People

	EXEC CreateOwnedObject$
		@clid = 13,
		@id = @id output,
		@guid = '5D543E4F-50D7-41fe-93A7-CF851C1D229E',
		@owner = @peopleList,
		@ownFlid = 8008,
		@type = 27

	UPDATE	CmPossibility
	SET	IsProtected = 1
	WHERE	[id] = @id

	INSERT CmPossibility_Name([Obj], [Ws], [Txt])
	VALUES	(@id, @english, 'Default User')

-------------------------------------------------------------------------------
-- Hook up any StJournalTexts that don't have CreatedBy and ModifiedBy set to
-- refer to the "Default User"
-------------------------------------------------------------------------------
	UPDATE	[StJournalText]
	SET	[CreatedBy]=@id,
		[ModifiedBy]=@id
	WHERE	[CreatedBy] IS NULL

-------------------------------------------------------------------------------
-- Set the style rules for the Remark para style to any paragraphs in
-- an StJournalText that doesn't have a paragraph style.
-------------------------------------------------------------------------------
	UPDATE stpara_
	SET StyleRules = 0x0001850206520065006D00610072006B00
	WHERE owner$ in (select [id] from StJournalText)
	AND StyleRules is NULL

-------------------------------------------------------------------------------
-- Add an empty paragraph with Remark para style to any empty StJournalTexts
-------------------------------------------------------------------------------
	DECLARE EmptyJournalTextsCur CURSOR FOR
		SELECT sjt.[id]
		FROM StJournalText sjt
		WHERE NOT EXISTS (SELECT * from StTxtPara_ WHERE Owner$ = sjt.[id])

	OPEN EmptyJournalTextsCur

	FETCH NEXT FROM EmptyJournalTextsCur
	INTO @id

	WHILE @@FETCH_STATUS = 0
	BEGIN
		INSERT INTO CmObject ([Class$], [Owner$], [OwnFlid$], [OwnOrd$])
		VALUES(16, @id, 14001, 1)
		SET @hvoStTxtPara = @@identity

		INSERT INTO StPara ([id], [StyleRules])
		VALUES(@hvoStTxtPara, 0x0001850206520065006D00610072006B00)

		INSERT INTO StTxtPara ([id])
		VALUES(@hvoStTxtPara)

		FETCH NEXT FROM EmptyJournalTextsCur
			INTO @id
	END

	CLOSE EmptyJournalTextsCur
	DEALLOCATE EmptyJournalTextsCur

END

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------

declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200107
begin
	UPDATE Version$ SET DbVer = 200108
	COMMIT TRANSACTION
	print 'database updated to version 200108'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200107 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
