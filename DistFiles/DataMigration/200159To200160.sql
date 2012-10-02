-- update database FROM version 200159 to 200160
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

	----------------------------------------------------------------------------
	-- FWM-122: In LgWritingSystem,
	-- add DefaultBodyFont
	-- add BodyFontFeatures
	-------------------------------------------------------------------------------
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (24026, 15, 24, NULL, 'DefaultBodyFont', 0, NULL, NULL)
GO
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (24027, 15, 24, NULL, 'BodyFontFeatures', 0, NULL, NULL)
GO

--update the LgWritingSystem class
EXEC UpdateClassView$ 24, 1
GO

-- Populate the new fields based on the existing settings for DefaultSerif
	UPDATE LgWritingSystem
	SET DefaultBodyFont = DefaultSerif,
		BodyFontFeatures = FontVariation
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200159
BEGIN
	UPDATE [Version$] SET [DbVer] = 200160
	COMMIT TRANSACTION
	PRINT 'database updated to version 200160'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200159 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
