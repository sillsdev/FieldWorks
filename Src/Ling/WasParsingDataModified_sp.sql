if object_id('WasParsingDataModified') is not null begin
	print 'removing proc WasParsingDataModified'
	drop proc WasParsingDataModified
end
print 'creating proc WasParsingDataModified'
go
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
CREATE PROC [WasParsingDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ BETWEEN 5026 AND 5045
			OR co.Class$ IN (
			kclidFsClosedFeature, -- FsClosedFeature 50
			kclidFsClosedValue, -- FsClosedValue 51
			kclidFsComplexFeature, -- FsComplexFeature 4
			kclidFsComplexValue, -- FsComplexValue 53
			kclidFsFeatStruc, -- FsFeatureStructure 57
			kclidFsFeatStrucType, -- FsFeatureStructureType 59
			kclidFsFeatureSystem, -- FsFeatureSystem 49
			kclidFsSymFeatVal, -- FsSymFeatVal 65
			kclidLexDb, -- LexDb 5005
			kclidLexEntry, -- LexEntry 5002
			kclidMoAdhocProhib, -- MoAdhocProhib 5026
			kclidMoAdhocProhibGr, -- MoAdhocProhibGr 5110
			kclidMoAffixAllomorph, -- MoAffixAllomorph 5027 (Actually only want MoAffixForm, but it doesn't work)
			kclidMoAffixForm, -- MoAffixForm 5028
			kclidMoAffixProcess, -- MoAffixProcess 5029
			kclidMoAlloAdhocProhib, -- MoAlloAdhocProhib 5101
			kclidMoCompoundRule, -- MoCompoundRule 5030
			kclidMoCopyFromInput, -- MoCopyFromInput 5103
			kclidMoDerivAffMsa, -- MoDerivAffMsa 5031
			kclidMoDerivStepMsa, -- MoDerivStepMsa 5031
			kclidMoEndoCompound, -- MoEndoCompound 5033
			kclidMoExoCompound, -- MoExoCompound 5034
			kclidMoForm, -- MoForm 5035
			kclidMoInflAffixSlot, -- MoInflAffixSlot 5036
			kclidMoInflAffixTemplate, -- MoInflAffixTemplate 5037
			kclidMoInflAffMsa, -- MoInflAffMsa 5038
			kclidMoInflClass, -- MoInflClass 5039
			kclidMoInsertNC, -- MoInsertNC 5069
			kclidMoInsertPhones, -- MoInsertPhones 5068
			kclidMoModifyFromInput, -- MoModifyFromInput 5070
			kclidMoMorphAdhocProhib, -- MoMorphAdhocProhib 5102
			kclidMoMorphData, -- MoMorphData 5040
			kclidMoMorphSynAnalysis, -- MoMorphSynAnalysis 5041
			kclidMoMorphType, -- MoMorphType 5042
			kclidMoStemAllomorph, -- MoStemAllomorph 5045
			kclidMoStemMsa, -- MoStemMsa 5001
			kclidMoUnclassifiedAffixMsa, -- MoUnclassifiedAffixMsa 5117
			kclidPartOfSpeech, -- PartOfSpeech 5049
			kclidPhCode, -- PhCode 5098
			kclidPhEnvironment, -- PhEnvironment 5097
			kclidPhFeatureConstraint, -- PhFeatureConstraint 5096
			kclidPhIterationContext, -- PhIterationContext 5082
			kclidPhMetathesisRule, -- PhMetathesisRule 5130
			kclidPhNCFeatures, -- PhNCFeatures 5094
			kclidPhNCSegments, -- PhNCSegments 5095
			kclidPhPhonData, -- PhPhonData 5099
			kclidPhPhoneme, -- PhPhoneme 5092
			kclidPhRegularRule, -- PhRegularRule 5129
			kclidPhSegmentRule, -- PhSegmentRule 5128
			kclidPhSegRuleRHS, -- PhSegRuleRHS
			kclidPhSequenceContext, -- PhSequenceContext 5083
			kclidPhSimpleContextBdry, -- PhSimpleContextBdry 5085
			kclidPhSimpleContextNC, -- PhSimpleContextNC 5086
			kclidPhSimpleContextSeg, -- PhSimpleContextSeg 5087
			kclidPhVariable -- PhVariable 5088
			))
GO
