-- Update database from version 200058 to 200059
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Remove the Old LexicalRelationGroup objects
DECLARE @hvoGroup int, @nRowCnt int
SELECT TOP 1 @hvoGroup=id FROM LexicalRelationGroup
SET @nRowCnt = @@ROWCOUNT
WHILE @nRowCnt > 0
BEGIN
	EXEC DeleteObj$ @hvoGroup
	SELECT TOP 1 @hvoGroup=id FROM LexicalRelationGroup
	SET @nRowCnt = @@ROWCOUNT
END
-- Remove Old LexicalRelations classes

-- LexSetItem
EXEC DeleteModelClass 5018

-- LexPair
EXEC DeleteModelClass 5010

-- LexPairRelation
EXEC DeleteModelClass 5011

-- LexScale
EXEC DeleteModelClass 5015

-- LexSimpleSet
EXEC DeleteModelClass 5019

-- LexTreeItem
EXEC DeleteModelClass 5023

-- LexTreeRelation
EXEC DeleteModelClass 5024

-- LexSet
EXEC DeleteModelClass 5017

-- LexicalDatabase_LexicalRelations
DELETE FROM Field$ WHERE Id=5005003

-- Drop _CK_LexicalRelationGroup_SetType
ALTER TABLE [dbo].[LexicalRelationGroup] DROP CONSTRAINT [_CK_LexicalRelationGroup_SetType]

-- LexicalRelationGroup
EXEC DeleteModelClass 5006

-- If it doesn't already exist in NewLangProj, Create References List in LexicalDatabase (LT-1872)
DECLARE @hvoLexDb int, @wsEn int, @hvoLexRefList int
SELECT @hvoLexDb=Dst from LanguageProject_LexicalDatabase
SELECT @wsEn=id from LgWritingSystem WHERE ICULocale=N'en'
SELECT @hvoLexRefList=Dst FROM LexicalDatabase_References
IF @hvoLexRefList is null OR @hvoLexRefList=0
BEGIN
	EXEC CreateObject_CmPossibilityList @wsEn, N'Lexical Reference Types',
		null, null,	null, null, null, 1, 0,	0, 0, 1, 0, @wsEn, N'RefTyp',
		null, 0, 0, 5119, 0, -3, @hvoLexDb, 5005019, null, @hvoLexRefList output, null, 0, null
END

-- make sure Sorted param is set.
UPDATE CmPossibilityList SET IsSorted=1 WHERE id=@hvoLexRefList

-- Change LexEntry from Abstract to concrete, in case transition to 200053 didn't work. (LT-1783)
UPDATE Class$ SET Abstract=0 WHERE [Name]='LexEntry' AND [Abstract]=1

-- Obtain the best possible default magic binary format for any description strings.
DECLARE @rgbFmt varbinary(8000)
SELECT top 1 @rgbFmt=Fmt FROM MultiStr$ WHERE Ws=@wsEn ORDER BY Fmt

-- Sense Tree (Part/Whole)
EXEC CreateObject_LexReferenceType
	@CmPossibility_Name_ws = @wsEn, @CmPossibility_Name_txt = N'Part',
	@CmPossibility_Abbreviation_ws = @wsEn, @CmPossibility_Abbreviation_txt = N'pt',
	@CmPossibility_Description_ws = @wsEn, @CmPossibility_Description_txt =
		N'Use this type for part/whole relationships (e.g., room: walls, ceiling, floor).',
	@CmPossibility_Description_fmt = @rgbFmt,
	@CmPossibility_SortSpec = 0, @CmPossibility_DateCreated = null, @CmPossibility_DateModified = null,
	@CmPossibility_HelpId = null, @CmPossibility_ForeColor = 0,	@CmPossibility_BackColor = 0,
	@CmPossibility_UnderColor = 0, @CmPossibility_UnderStyle = 0, @CmPossibility_Hidden = 0, @CmPossibility_IsProtected = 0,
	@LexReferenceType_ReverseAbbreviation_ws = @wsEn,
	@LexReferenceType_ReverseAbbreviation_txt = N'wh',
	@LexReferenceType_MappingType = 2,
	@LexReferenceType_ReverseName_ws = @wsEn,
	@LexReferenceType_ReverseName_txt = N'Whole',
	@Owner = @hvoLexRefList, @OwnFlid = 8008,
	@StartObj = null, @NewObjId = null, @NewObjGuid = null, @fReturnTimestamp = 0, @NewObjTimestamp = null

-- Sense Tree (Generic/Specific)
EXEC CreateObject_LexReferenceType
	@CmPossibility_Name_ws = @wsEn, @CmPossibility_Name_txt = N'Specific',
	@CmPossibility_Abbreviation_ws = @wsEn, @CmPossibility_Abbreviation_txt = N'spec',
	@CmPossibility_Description_ws = @wsEn, @CmPossibility_Description_txt =
		N'Use this type for generic/specific relationships (e.g., animal: pig, cow, dog).',
	@CmPossibility_Description_fmt = @rgbFmt,
	@CmPossibility_SortSpec = 0, @CmPossibility_DateCreated = null, @CmPossibility_DateModified = null,
	@CmPossibility_HelpId = null, @CmPossibility_ForeColor = 0,	@CmPossibility_BackColor = 0,
	@CmPossibility_UnderColor = 0, @CmPossibility_UnderStyle = 0, @CmPossibility_Hidden = 0, @CmPossibility_IsProtected = 0,
	@LexReferenceType_ReverseAbbreviation_ws = @wsEn,
	@LexReferenceType_ReverseAbbreviation_txt = N'gen',
	@LexReferenceType_MappingType = 2,
	@LexReferenceType_ReverseName_ws = @wsEn,
	@LexReferenceType_ReverseName_txt = N'Generic',
	@Owner = @hvoLexRefList, @OwnFlid = 8008,
	@StartObj = null, @NewObjId = null, @NewObjGuid = null, @fReturnTimestamp = 0, @NewObjTimestamp = null

-- Sense Collection (Synonym)
EXEC CreateObject_LexReferenceType
	@CmPossibility_Name_ws = @wsEn, @CmPossibility_Name_txt = N'Synonym',
	@CmPossibility_Abbreviation_ws = @wsEn, @CmPossibility_Abbreviation_txt = N'syn',
	@CmPossibility_Description_ws = @wsEn, @CmPossibility_Description_txt =
		N'Use this type for synonym relationships (e.g., fast, fleet, hasty).',
	@CmPossibility_Description_fmt = @rgbFmt,
	@CmPossibility_SortSpec = 0, @CmPossibility_DateCreated = null, @CmPossibility_DateModified = null,
	@CmPossibility_HelpId = null, @CmPossibility_ForeColor = 0,	@CmPossibility_BackColor = 0,
	@CmPossibility_UnderColor = 0, @CmPossibility_UnderStyle = 0, @CmPossibility_Hidden = 0, @CmPossibility_IsProtected = 0,
	@LexReferenceType_ReverseAbbreviation_ws = null,
	@LexReferenceType_ReverseAbbreviation_txt = null,
	@LexReferenceType_MappingType = 0,
	@LexReferenceType_ReverseName_ws = null,
	@LexReferenceType_ReverseName_txt = null,
	@Owner = @hvoLexRefList, @OwnFlid = 8008,
	@StartObj = null, @NewObjId = null, @NewObjGuid = null, @fReturnTimestamp = 0, @NewObjTimestamp = null

-- Sense Pair (Antonym)
EXEC CreateObject_LexReferenceType
	@CmPossibility_Name_ws = @wsEn, @CmPossibility_Name_txt = N'Antonym',
	@CmPossibility_Abbreviation_ws = @wsEn, @CmPossibility_Abbreviation_txt = N'ant',
	@CmPossibility_Description_ws = @wsEn, @CmPossibility_Description_txt =
		N'Use this type for antonym relationships (e.g., fast, slow).',
	@CmPossibility_Description_fmt = @rgbFmt,
	@CmPossibility_SortSpec = 0, @CmPossibility_DateCreated = null, @CmPossibility_DateModified = null,
	@CmPossibility_HelpId = null, @CmPossibility_ForeColor = 0,	@CmPossibility_BackColor = 0,
	@CmPossibility_UnderColor = 0, @CmPossibility_UnderStyle = 0, @CmPossibility_Hidden = 0, @CmPossibility_IsProtected = 0,
	@LexReferenceType_ReverseAbbreviation_ws = null,
	@LexReferenceType_ReverseAbbreviation_txt = null,
	@LexReferenceType_MappingType = 1,
	@LexReferenceType_ReverseName_ws = null,
	@LexReferenceType_ReverseName_txt = null,
	@Owner = @hvoLexRefList, @OwnFlid = 8008,
	@StartObj = null, @NewObjId = null, @NewObjGuid = null, @fReturnTimestamp = 0, @NewObjTimestamp = null

-- Sense Sequence (Calendar)
EXEC CreateObject_LexReferenceType
	@CmPossibility_Name_ws = @wsEn, @CmPossibility_Name_txt = N'Calendar',
	@CmPossibility_Abbreviation_ws = @wsEn, @CmPossibility_Abbreviation_txt = N'cal',
	@CmPossibility_Description_ws = @wsEn, @CmPossibility_Description_txt =
		N'Use this type for calendar relationships (e.g., days of week, months in year).',
	@CmPossibility_Description_fmt = @rgbFmt,
	@CmPossibility_SortSpec = 0, @CmPossibility_DateCreated = null, @CmPossibility_DateModified = null,
	@CmPossibility_HelpId = null, @CmPossibility_ForeColor = 0,	@CmPossibility_BackColor = 0,
	@CmPossibility_UnderColor = 0, @CmPossibility_UnderStyle = 0, @CmPossibility_Hidden = 0, @CmPossibility_IsProtected = 0,
	@LexReferenceType_ReverseAbbreviation_ws = null,
	@LexReferenceType_ReverseAbbreviation_txt = null,
	@LexReferenceType_MappingType = 3,
	@LexReferenceType_ReverseName_ws = null,
	@LexReferenceType_ReverseName_txt = null,
	@Owner = @hvoLexRefList, @OwnFlid = 8008,
	@StartObj = null, @NewObjId = null, @NewObjGuid = null, @fReturnTimestamp = 0, @NewObjTimestamp = null

-- Entry Collection (See)
EXEC CreateObject_LexReferenceType
	@CmPossibility_Name_ws = @wsEn, @CmPossibility_Name_txt = N'See',
	@CmPossibility_Abbreviation_ws = @wsEn, @CmPossibility_Abbreviation_txt = N'see',
	@CmPossibility_Description_ws = @wsEn, @CmPossibility_Description_txt =
		N'Use this type for general references to other entries.',
	@CmPossibility_Description_fmt = @rgbFmt,
	@CmPossibility_SortSpec = 0, @CmPossibility_DateCreated = null, @CmPossibility_DateModified = null,
	@CmPossibility_HelpId = null, @CmPossibility_ForeColor = 0,	@CmPossibility_BackColor = 0,
	@CmPossibility_UnderColor = 0, @CmPossibility_UnderStyle = 0, @CmPossibility_Hidden = 0, @CmPossibility_IsProtected = 0,
	@LexReferenceType_ReverseAbbreviation_ws = null,
	@LexReferenceType_ReverseAbbreviation_txt = null,
	@LexReferenceType_MappingType = 4,
	@LexReferenceType_ReverseName_ws = null,
	@LexReferenceType_ReverseName_txt = null,
	@Owner = @hvoLexRefList, @OwnFlid = 8008,
	@StartObj = null, @NewObjId = null, @NewObjGuid = null, @fReturnTimestamp = 0, @NewObjTimestamp = null
-- DEBUG
/*
SELECT lrt.*, n.Txt 'Name', rn.Txt 'Reverse', a.Txt 'Abbr', ra.Txt 'RevAbbr', d.Txt 'Desc', d.Fmt
	FROM LexReferenceType lrt
	JOIN CmPossibility_Name n on n.obj=lrt.id
	JOIN CmPossibility_Abbreviation a on a.obj=lrt.id
	JOIN CmPossibility_Description d on d.obj=lrt.id
	LEFT OUTER JOIN LexReferenceType_ReverseAbbreviation ra on ra.obj=lrt.id
	LEFT OUTER JOIN LexReferenceType_ReverseName rn on rn.obj=lrt.id

ROLLBACK TRANSACTION
BEGIN TRANSACTION
*/
---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200058
begin
	update Version$ set DbVer = 200059
	COMMIT TRANSACTION
	print 'database updated to version 200059'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200058 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
