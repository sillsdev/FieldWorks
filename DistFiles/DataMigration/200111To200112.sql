-- update database FROM version 200111 to 200112

BEGIN TRANSACTION  --( will be rolled back if wrong version#

--( Remove obsolete SP.
if object_id('WasParsingDataModified') is not null begin
	print 'removing proc WasParsingDataModified'
	drop proc WasParsingDataModified
end
go

--( Add first of two new SP, which replace the old one.
if object_id('WasParsingGrammarDataModified') is not null begin
	print 'removing proc WasParsingGrammarDataModified'
	drop proc WasParsingGrammarDataModified
end
print 'creating proc WasParsingGrammarDataModified'
go
/*****************************************************************************
 * WasParsingGrammarDataModified
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
CREATE PROC [WasParsingGrammarDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co (readuncommitted)
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ IN
			(4, -- FsComplexFeature 4
			49, -- FsFeatureSystem 49
			50, -- FsClosedFeature 50
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			65, -- FsSymbolicFeatureValue 65
			5001, -- MoStemMsa 5001
			5026, -- MoAdhocCoProhibition 5026
			5027, -- MoAffixAllomorph 5027 (Actually only want MoAffixForm, but it doesn't work)
			--5028, -- MoAffixForm 5028 (Does not work because is a superclass)
			5030, -- MoCompoundRule 5030
			5031, -- MoDerivationalAffixMsa 5031
			5033, -- MoEndocentricCompound 5033
			5034, -- MoExocentricCompound 5034
			5036, -- MoInflAffixSlot 5036
			5037, -- MoInflAffixTemplate 5037
			5038, -- MoInflectionalAffixMsa 5038
			5039, -- MoInflectionClass 5039
			5040, -- MoMorphologicalData 5040
			5041, -- MoMorphoSyntaxAnalysis 5041
			5042, -- MoMorphType 5042
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

--( Add second of two new SP, which replace the old one.
if object_id('WasParsingLexiconDataModified') is not null begin
	print 'removing proc WasParsingLexiconDataModified'
	drop proc WasParsingLexiconDataModified
end
print 'creating proc WasParsingLexiconDataModified'
go
/*****************************************************************************
 * WasParsingLexiconDataModified
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
CREATE PROC [WasParsingLexiconDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co (readuncommitted)
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ IN
			(
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			5001, -- MoStemMsa 5001
			5002, -- LexEntry 5002
			5005, -- LexicalDatabase 5005
			5027, -- MoAffixAllomorph 5027
			5028, -- MoAffixForm 5028
			5031, -- MoDerivationalAffixMsa 5031
			5035, -- MoForm 5035
			5038, -- MoInflectionalAffixMsa 5038
			5045, -- MoStemAllomorph 5045
			5117 -- MoUnclassifiedAffixMsa 5117
			))
GO

-------------------------------------------------------------------------------
-- Finish or roll back transaction as applicable
-------------------------------------------------------------------------------
declare @dbVersion int
SELECT @dbVersion = DbVer FROM Version$
if @dbVersion = 200111
begin
	UPDATE Version$ SET DbVer = 200112
	COMMIT TRANSACTION
	print 'database updated to version 200112'
end
else
begin
	ROLLBACK TRANSACTION
	print 'Update aborted: this works only if DbVer = 200111 (DbVer = ' +
			convert(varchar, @dbVersion) + ')'
end
GO
