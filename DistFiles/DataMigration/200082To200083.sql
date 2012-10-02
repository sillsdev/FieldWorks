-- Update database from version 200082 to 200083
BEGIN TRANSACTION  --( will be rolled back if wrong version#)

-- Updates classes to check for parser being outdated.

/*****************************************************************************
 * WasParsingDataModified
 *
 * Description:
 *	Returns a table with zero or one row of any object
 *	of certain classes that have a newer timestamp than
 *	that given in the input parameter.
 * Parameters:
 *	@stampCompare=the timestamp to compare.
 * Returns:
 *	0
 *****************************************************************************/
if object_id('WasParsingDataModified') is not null begin
	print 'removing proc WasParsingDataModified'
	drop proc [WasParsingDataModified]
end
go
print 'creating proc WasParsingDataModified'
go
create proc [WasParsingDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co (readuncommitted)
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ BETWEEN 5026 AND 5045
			OR co.Class$ IN
			(4, -- FsComplexFeature 4
			49, -- FsFeatureSystem 49
			50, -- FsClosedFeature 50
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			65, -- FsSymbolicFeatureValue 65
			5001, -- MoStemMsa 5001
			5002, -- LexEntry 5002
			5005, -- LexicalDatabase 5005
			5049, -- PartOfSpeech 5049
			5092, -- PhPhoneme 5092
			5095, -- PhNCSegments 5095
			5097, -- PhEnvironment 5097
			5098, -- PhCode 5098
			5099, -- PhPhonologicalData 5099
			5101, -- MoAllomorphAdhocCoProhibition 5101
			5102, -- MoMorphemeAdhocCoProhibition 5102
			5110, -- MoAdhocCoProhibitionGroup 5110
			5117 -- MoUnclassifiedAffixMsa 5117
			))

GO
---------------------------------------------------------------------
declare @dbVersion int
select @dbVersion = DbVer from Version$
if @dbVersion = 200082
begin
	update Version$ set DbVer = 200083
	COMMIT TRANSACTION
	print 'database updated to version 200083'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200082 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO