-- update database FROM version 200139 to 200140
BEGIN TRANSACTION  --( will be rolled back if wrong version#

-------------------------------------------------------------------------------
-- FWM-110:
-- remove footnote properties in ScrMarkerMapping,
-- remove FootnoteSettings in ScrImportSettings,
-- remove ScrImportFootnoteSettings
-------------------------------------------------------------------------------

-- remove FootnoteMarker, DisplayFootnoteMarker, DisplayFootnoteReference
-- from ScrMarkerMapping
DELETE FROM [Field$] WHERE [Id] in (3016007, 3016008, 3016009)
GO
exec UpdateClassView$ 3016
GO

-- remove FootnoteSettings in ScrImportSettings,
DELETE FROM [Field$] WHERE [Id] in (3008004)
GO
exec UpdateClassView$ 3008
GO

-- remove ScrImportFootnoteSettings
DELETE FROM [Field$] WHERE [Class] = 3012
DELETE FROM [ClassPar$] WHERE [Src] = 3012
DELETE FROM [Class$] WHERE [Id] = 3012

DROP VIEW ScrImportFootnoteSettings_
DROP TABLE ScrImportFootnoteSettings
GO

-------------------------------------------------------------------------------
-- FWM-111: In Scripture,
-- remove DefaultDisplayFootnoteMarker
-- rename DefaultDisplayFootnoteReference to DisplayFootnoteReference
-- rename DefaultFootnoteMarker to FootnoteMarkerSymbol
-- add FootnoteMarkerType
-- add DisplayCrossRefReference
-- add CrossRefMarkerSymbol
-- add CrossRefMarkerType
-------------------------------------------------------------------------------

-- remove DefaultDisplayFootnoteMarker
DELETE FROM [Field$] WHERE [Id] in (3001011)
GO

-- save the values that need to be migrated into new fields
CREATE TABLE #SavedScrValues(DisplayFootnoteReference bit, FootnoteMarkerSymbol nvarchar(4000))
GO
INSERT INTO #SavedScrValues
	SELECT [DefaultDisplayFootnoteReference], [DefaultFootnoteMarker]
	FROM [Scripture]
GO

-- rename DefaultDisplayFootnoteReference to DisplayFootnoteReference
-- rename DefaultFootnoteMarker to FootnoteMarkerSymbol
DELETE FROM [Field$] WHERE [Id] = 3001012
GO
INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
VALUES (3001012, 1, 3001, NULL, 'DisplayFootnoteReference', 0, NULL, NULL)
GO
DELETE FROM [Field$] WHERE [Id] = 3001010
GO
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

-------------------------------------------------------------------------------
-- FWM-114: In Scripture,
-- add CrossRefsCombinedWithFootnotes
-------------------------------------------------------------------------------

-- add Scripture.CrossRefsCombinedWithFootnotes
INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
VALUES (3001030, 1, 3001, NULL, 'CrossRefsCombinedWithFootnotes', 0, NULL, NULL)
GO

--update the Scripture class
exec DefineCreateProc$ 3001
GO
exec UpdateClassView$ 3001, 1
GO

-------------------------------------------------------------------------------
-- FWM-111:
-- * Set data in FootnoteMarkerType FootnoteMarkerSymbol based on original
--   value in DefaultFootnoteMarker
-- * Set data in DisplayFootnoteReference to the original value in
--   DefaultDisplayFootnoteReference
-------------------------------------------------------------------------------
EXEC sp_columns @table_name = Scripture, @column_name = 'FootnoteMarkerSymbol'
IF (@@rowcount = 1)
BEGIN
	UPDATE [Scripture] SET [DisplayFootnoteReference] = #SavedScrValues.DisplayFootnoteReference,
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
GO

DROP TABLE #SavedScrValues
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
if @dbVersion = 200139
begin
	UPDATE [Version$] SET [DbVer] = 200140
	COMMIT TRANSACTION
	print 'database updated to version 200140'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200139 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO