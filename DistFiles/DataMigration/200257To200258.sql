-- Update database from version 200257 to 200258
BEGIN TRANSACTION  --( will be rolled back if wrong version#)
BEGIN
	DECLARE	@idFilter int,
		@idRow int,
		@guid uniqueidentifier

	----------------------------------------------------
	-- Create Advanced Filter
	----------------------------------------------------
	SET @idFilter = null
	SET @guid = null
	EXEC	MakeObj_CmFilter
		@CmFilter_Name = 'kstidNoteMultiFilter',
		@CmFilter_ClassId = 3018,
		@CmFilter_App = 'A7D421E1-1DD3-11D5-B720-0010A4B54856',
		@CmFilter_Type = 0,
		@CmFilter_ColumnInfo = 'replace me',
		@CmFilter_ShowPrompt = 1,
		@CmFilter_PromptText = 'Setup a filter that handles multiple criteria:',
		@Owner = 1,
		@OwnFlid = 6001024,
		@NewObjId = @idFilter OUTPUT,
		@NewObjGuid = @guid OUTPUT

	SET @idRow = null
	SET @guid = null
	EXEC	MakeObj_CmRow
		@Owner = @idFilter,
		@OwnFlid = 9007,
		@NewObjId = @idRow OUTPUT,
		@NewObjGuid = @guid OUTPUT

	-- No need for a cell for this filter. The cells will be generated at runtime

END
GO

BEGIN
	----------------------------------------------------
	-- Localize the previously defined filters
	----------------------------------------------------

	UPDATE CmFilter SET [Name] = 'kstidConsultantNoteFilter'
	WHERE [Name] = 'Consultant' AND [App] = 'A7D421E1-1DD3-11D5-B720-0010A4B54856'

	UPDATE CmFilter SET [Name] = 'kstidTranslatorNoteFilter'
	WHERE [Name] = 'Translator' AND [App] = 'A7D421E1-1DD3-11D5-B720-0010A4B54856'

	UPDATE CmFilter SET [Name] = 'kstidOpenNoteFilter'
	WHERE [Name] = 'Open' AND [App] = 'A7D421E1-1DD3-11D5-B720-0010A4B54856'

	UPDATE CmFilter SET [Name] = 'kstidCategoryNoteFilter'
	WHERE [Name] = 'Category' AND [App] = 'A7D421E1-1DD3-11D5-B720-0010A4B54856'
END
GO

---------------------------------------------------------------------------------
---- Finish or roll back transaction as applicable
---------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200257
BEGIN
	UPDATE Version$ SET DbVer = 200258
	COMMIT TRANSACTION
	PRINT 'database updated to version 200258'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200257 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
