-- update database FROM version 200172 to 200173
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

	----------------------------------------------------------------------------
	-- This is a remedial data migration to fix a mistake in the migration from
	-- 200170 to 200171
	-- FWM-117: in ScrScriptureNote
	-- add DateResolved
	-- FWM-125: in Scripture
	-- add DisplaySymbolInFootnote
	-- add DisplaySymbolInCrossRef
	-- TE-5224: in CmTranslations that belong to Scripture
	-- set Type equal to id for the CmPossibility for the back translation type
	-- in LanguageProject.TranslationTypes
	-------------------------------------------------------------------------------
	IF NOT EXISTS(SELECT * FROM [Field$] WHERE [Id] = 3018008)
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3018008, 5, 3018, NULL, 'DateResolved', 0, NULL, NULL)
GO
	--update the ScrScriptureNote class
	EXEC UpdateClassView$ 3018, 1
GO

	IF NOT EXISTS(SELECT * FROM [Field$] WHERE [Id] = 3001031)
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001031, 1, 3001, NULL, 'DisplaySymbolInFootnote', 0, NULL, NULL)
GO
	IF NOT EXISTS(SELECT * FROM [Field$] WHERE [Id] = 3001032)
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001032, 1, 3001, NULL, 'DisplaySymbolInCrossRef', 0, NULL, NULL)
GO
	--update the Scripture class
	EXEC UpdateClassView$ 3001, 1
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200172
BEGIN
	UPDATE [Version$] SET [DbVer] = 200173
	COMMIT TRANSACTION
	PRINT 'database updated to version 200173'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200172 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
