-- update database FROM version 200142 to 200143
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-------------------------------------------------------------------------------
-- FWM-111: If the migration from 200139 to 200140 was not successful...
-- In Scripture,
-- insert DisplayFootnoteReference
-- rename DefaultFootnoteMarker to FootnoteMarkerSymbol
-- add FootnoteMarkerType
-- add DisplayCrossRefReference
-- add CrossRefMarkerSymbol
-- add CrossRefMarkerType
-------------------------------------------------------------------------------

-- If a field named 'DefaultDisplayFootnoteReference' or 'DisplayFootnoteReference' is not in Scripture,
-- it was deleted by a previous migration script. Insert 'DisplayFootnoteReference'.
IF NOT EXISTS(select * from [Field$] WHERE [Id] = 3001012 AND
   ([NAME] = 'DefaultDisplayFootnoteReference' OR [NAME] = 'DisplayFootnoteReference'))
BEGIN
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001012, 1, 3001, NULL, 'DisplayFootnoteReference', 0, NULL, NULL)
END

-- Confirm that one field in Scripture is named 'DefaultFootnoteMarker'. If so, it needs to be
-- renamed to 'FootnoteMarkerSymbol' and other fields added to Scripture as well.
IF EXISTS(select * from [Field$] WHERE [Id] = 3001010 AND [NAME] = 'DefaultFootnoteMarker')
	CREATE TABLE #SavedScrValues(FootnoteMarkerSymbol nvarchar(4000))
GO

-- NOTE: The data migration from 200139 to 200140 failed because a 'GO' did not follow certain
--       SQL commands. Commands below are grouped under separate conditional statements whenever
--       a 'GO' is required because 'GO' cannot be inside an BEGIN..END block (such as an IF
--       statement) and we only want to update the database under the condition that the other
--       migration script failed (the SavedScrValues table exists).
-- NOTE: (SteveMiller, Dec 7, 2006) Although the IF blocks don't fire in SQL Server Express, I'm
--		still getting an error complaing that Scripture.DefaultFootnoteMaker doesn't exist. Express
--		must be checking for the column when making an execution plan. Since the odds of someone
--		being between versions 200139 and 200143 are slim to none, and the code seems to be fixed
--		for everyone else, I'm remarking the block to pull out the value. The rest of the code
--		shouldn't fire, so I'm leaving that unremarked.

-- save the values that need to be migrated into new fields
--IF EXISTS(select * from [tempdb].[dbo].[sysobjects] where type = 'U' and SUBSTRING(name, 0, 16) = '#SavedScrValues')
--BEGIN
--	INSERT INTO #SavedScrValues
--		SELECT [DefaultFootnoteMarker] FROM [Scripture]
--END
--GO

IF EXISTS(select * from [tempdb].[dbo].[sysobjects] where type = 'U' and SUBSTRING(name, 0, 16)  = '#SavedScrValues')
BEGIN
	DELETE FROM [Field$] WHERE [Id] = 3001010
END
GO

IF EXISTS(select * from [tempdb].[dbo].[sysobjects] where type = 'U' and SUBSTRING(name, 0, 16) = '#SavedScrValues')
BEGIN
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001010, 15, 3001, NULL, 'FootnoteMarkerSymbol', 0, NULL, NULL)

	-- add Scripture.FootnoteMarkerType
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
			VALUES (3001026, 2, 3001, NULL, 'FootnoteMarkerType', 0, NULL, NULL)

	-- add Scripture.DisplayCrossRefReference
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001027, 1, 3001, NULL, 'DisplayCrossRefReference', 0, NULL, NULL)

	-- add Scripture.CrossRefMarkerSymbol
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001028, 15, 3001, NULL, 'CrossRefMarkerSymbol', 0, NULL, NULL)

	-- add Scripture.CrossRefMarkerType
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001029, 2, 3001, NULL, 'CrossRefMarkerType', 0, NULL, NULL)

	----------------------------------------------------------------------------
	-- FWM-114: In Scripture,
	-- add CrossRefsCombinedWithFootnotes
	-------------------------------------------------------------------------------
	-- add Scripture.CrossRefsCombinedWithFootnotes
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001030, 1, 3001, NULL, 'CrossRefsCombinedWithFootnotes', 0, NULL, NULL)
END
GO

--update the Scripture class
IF EXISTS(select * from [tempdb].[dbo].[sysobjects] where type = 'U' and SUBSTRING(name, 0, 16) = '#SavedScrValues')
	EXEC DefineCreateProc$ 3001
GO
IF EXISTS(select * from [tempdb].[dbo].[sysobjects] where type = 'U' and SUBSTRING(name, 0, 16) = '#SavedScrValues')
	EXEC UpdateClassView$ 3001, 1
GO

	-------------------------------------------------------------------------------
	-- FWM-111:
	-- * Set data in FootnoteMarkerType FootnoteMarkerSymbol based on original
	--   value in DefaultFootnoteMarker
	-- * Set data in DisplayFootnoteReference to the original value in
	--   DefaultDisplayFootnoteReference
	-------------------------------------------------------------------------------
IF EXISTS(select * from [tempdb].[dbo].[sysobjects] where type = 'U' and SUBSTRING(name, 0, 16) = '#SavedScrValues')
BEGIN
	EXEC sp_columns @table_name = Scripture, @column_name = 'FootnoteMarkerSymbol'
	IF (@@rowcount = 1)
	BEGIN
		UPDATE [Scripture] SET [DisplayFootnoteReference] = 0,
			[FootnoteMarkerSymbol] = #SavedScrValues.FootnoteMarkerSymbol,
			[FootnoteMarkerType] = 1,
			[CrossRefMarkerSymbol] = '*',
			[DisplayCrossRefReference] = 1,
			[CrossRefMarkerType] = 2
			FROM #SavedScrValues

		UPDATE [Scripture] SET [FootnoteMarkerType] = 0,
			[FootnoteMarkerSymbol] = '*'
			FROM #SavedScrValues
			WHERE #SavedScrValues.FootnoteMarkerSymbol = 'a'

		UPDATE [Scripture] SET [FootnoteMarkerType] = 2,
			[FootnoteMarkerSymbol] = '*'
			FROM #SavedScrValues
			WHERE #SavedScrValues.FootnoteMarkerSymbol IS NULL
	END
END
GO

IF EXISTS(select * from [tempdb].[dbo].[sysobjects] where type = 'U' and SUBSTRING(name, 0, 16) = '#SavedScrValues')
	DROP TABLE #SavedScrValues
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200142
BEGIN
	UPDATE [Version$] SET [DbVer] = 200143
	COMMIT TRANSACTION
	PRINT 'database updated to version 200143'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200142 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
