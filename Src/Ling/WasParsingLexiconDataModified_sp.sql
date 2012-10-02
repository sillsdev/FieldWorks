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
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ IN (
			kclidFsClosedValue, -- FsClosedValue 51
			kclidFsComplexValue, -- FsComplexValue 53
			kclidFsFeatStruc, -- FsFeatureStructure 57
			kclidFsFeatStrucType, -- FsFeatureStructureType 59
			kclidLexDb, -- LexDb 5005
			kclidLexEntry, -- LexEntry 5002
			kclidMoAffixAllomorph, -- MoAffixAllomorph 5027
			kclidMoAffixForm, -- MoAffixForm 5028
			kclidMoAffixProcess, -- MoAffixProcess 5029
			kclidMoCopyFromInput, -- MoCopyFromInput 5103
			kclidMoDerivAffMsa, -- MoDerivAffMsa 5031
			kclidMoForm, -- MoForm 5035
			kclidMoInflAffMsa, -- MoInflAffMsa 5038
			kclidMoInsertNC, -- MoInsertNC 5069
			kclidMoInsertPhones, -- MoInsertPhones 5068
			kclidMoModifyFromInput, -- MoModifyFromInput 5070
			kclidMoStemAllomorph, -- MoStemAllomorph 5045
			kclidMoStemMsa, -- MoStemMsa 5001
			kclidMoUnclassifiedAffixMsa, -- MoUnclassifiedAffixMsa 5117
			kclidPhFeatureConstraint, -- PhFeatureConstraint 5096
			kclidPhNCFeatures, -- PhNCFeatures 5094
			kclidPhNCSegments, -- PhNCSegments 5095
			kclidPhPhonData, -- PhPhonData 5099
			kclidPhPhoneme, -- PhPhoneme 5092
			kclidPhSequenceContext, -- PhSequenceContext 5083
			kclidPhSimpleContextBdry, -- PhSimpleContextBdry 5085
			kclidPhSimpleContextNC, -- PhSimpleContextNC 5086
			kclidPhSimpleContextSeg, -- PhSimpleContextSeg 5087
			kclidPhVariable -- PhVariable 5088
			))
GO
