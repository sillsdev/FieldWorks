-- update database FROM version 200170 to 200171
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

	----------------------------------------------------------------------------
	-- FWM-115: in ScriptureBook
	-- add CanonicalNum
	-- FWM-117: in ScrScriptureNote
	-- add DateResolved
	-- FWM-125: in Scripture
	-- add DisplaySymbolInFootnote
	-- add DisplaySymbolInCrossRef
	-- TE-5224: in CmTranslations that belong to Scripture
	-- set Type equal to id for the CmPossibility for the back translation type
	-- in LanguageProject.TranslationTypes
	-------------------------------------------------------------------------------
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3002013, 2, 3002, NULL, 'CanonicalNum', 0, NULL, NULL)
GO
	--update the ScriptureBook class
	EXEC UpdateClassView$ 3002, 1
GO

	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3018008, 5, 3018, NULL, 'DateResolved', 0, NULL, NULL)
GO
	--update the ScrScriptureNote class
	EXEC UpdateClassView$ 3018, 1
GO

	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001031, 1, 3001, NULL, 'DisplaySymbolInFootnote', 0, NULL, NULL)
GO
	INSERT INTO [Field$] ([Id], [Type], [Class], [DstCls], [Name], [Custom], [CustomId], [Big])
		VALUES (3001032, 1, 3001, NULL, 'DisplaySymbolInCrossRef', 0, NULL, NULL)
GO
	--update the Scripture class
	EXEC UpdateClassView$ 3001, 1
GO
	--in CmTranslations that belong to Scripture (only Back Translations),
	--set the Type to refer to the back translation CmPossibility in LanguageProject.TranslationTypes
	update CmTranslation
	set [Type] = (select id from Cmpossibility_ where [Guid$] = '80A0DDDB-8B4B-4454-B872-88ADEC6F2ABA')
	from CmTranslation
	inner join CmObject cmto on (cmto.id = CmTranslation.id)
	inner join StTxtPara_ p on (p.id = cmto.Owner$)
	inner join StText_ st on (st.id = p.Owner$)
	where cmto.Ownflid$ = 16008 and
	st.Ownflid$ between 3001000 and 3999999
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
DECLARE @dbVersion int
SELECT @dbVersion = [DbVer] FROM [Version$]
IF @dbVersion = 200170
BEGIN
	UPDATE [Version$] SET [DbVer] = 200171
	COMMIT TRANSACTION
	PRINT 'database updated to version 200171'
END

ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200170 (DbVer = ' +
		convert(varchar, @dbVersion) + ')'
END
GO
