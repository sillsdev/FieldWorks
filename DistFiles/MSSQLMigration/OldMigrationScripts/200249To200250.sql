-- Update database from version 200249 to 200250
BEGIN TRANSACTION  --( will be rolled back if wrong version #)
-------------------------------------------------------------------------------
-- Update stored procedures used by parser to support Hermit Crab
-- Add CmAgent for Hermit Crab parser

if object_id('WasParsingDataModified') is not null begin
	drop proc WasParsingDataModified
end
go

CREATE PROC [WasParsingDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ BETWEEN 5026 AND 5045
			OR co.Class$ IN (
			50, -- FsClosedFeature 50
			51, -- FsClosedValue 51
			4, -- FsComplexFeature 4
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			49, -- FsFeatureSystem 49
			65, -- FsSymFeatVal 65
			5005, -- LexDb 5005
			5002, -- LexEntry 5002
			5026, -- MoAdhocProhib 5026
			5110, -- MoAdhocProhibGr 5110
			5027, -- MoAffixAllomorph 5027 (Actually only want MoAffixForm, but it doesn't work)
			5028, -- MoAffixForm 5028
			5029, -- MoAffixProcess 5029
			5101, -- MoAlloAdhocProhib 5101
			5030, -- MoCompoundRule 5030
			5103, -- MoCopyFromInput 5103
			5031, -- MoDerivAffMsa 5031
			5032, -- MoDerivStepMsa 5031
			5033, -- MoEndoCompound 5033
			5034, -- MoExoCompound 5034
			5035, -- MoForm 5035
			5036, -- MoInflAffixSlot 5036
			5037, -- MoInflAffixTemplate 5037
			5038, -- MoInflAffMsa 5038
			5039, -- MoInflClass 5039
			5069, -- MoInsertNC 5069
			5068, -- MoInsertPhones 5068
			5070, -- MoModifyFromInput 5070
			5102, -- MoMorphAdhocProhib 5102
			5040, -- MoMorphData 5040
			5041, -- MoMorphSynAnalysis 5041
			5042, -- MoMorphType 5042
			5045, -- MoStemAllomorph 5045
			5001, -- MoStemMsa 5001
			5117, -- MoUnclassifiedAffixMsa 5117
			5049, -- PartOfSpeech 5049
			5098, -- PhCode 5098
			5097, -- PhEnvironment 5097
			5096, -- PhFeatureConstraint 5096
			5082, -- PhIterationContext 5082
			5130, -- PhMetathesisRule 5130
			5094, -- PhNCFeatures 5094
			5095, -- PhNCSegments 5095
			5099, -- PhPhonData 5099
			5092, -- PhPhoneme 5092
			5129, -- PhRegularRule 5129
			5128, -- PhSegmentRule 5128
			5131, -- PhSegRuleRHS
			5083, -- PhSequenceContext 5083
			5085, -- PhSimpleContextBdry 5085
			5086, -- PhSimpleContextNC 5086
			5087, -- PhSimpleContextSeg 5087
			5088 -- PhVariable 5088
			))
GO

if object_id('WasParsingGrammarDataModified') is not null begin
	drop proc WasParsingGrammarDataModified
end
go

CREATE PROC [WasParsingGrammarDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ IN (
			50, -- FsClosedFeature 50
			51, -- FsClosedValue 51
			4, -- FsComplexFeature 4
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			49, -- FsFeatureSystem 49
			65, -- FsSymFeatVal 65
			5026, -- MoAdhocProhib 5026
			5110, -- MoAdhocProhibGr 5110
			5027, -- MoAffixAllomorph 5027 (Actually only want MoAffixForm, but it doesn't work)
			5029, -- MoAffixProcess 5029
			5101, -- MoAlloAdhocProhib 5101
			5030, -- MoCompoundRule 5030
			5103, -- MoCopyFromInput 5103
			5032, -- MoDerivStepMsa 5031
			5033, -- MoEndoCompound 5033
			5034, -- MoExoCompound 5034
			5036, -- MoInflAffixSlot 5036
			5037, -- MoInflAffixTemplate 5037
			5038, -- MoInflAffMsa 5038
			5039, -- MoInflClass 5039
			5069, -- MoInsertNC 5069
			5068, -- MoInsertPhones 5068
			5070, -- MoModifyFromInput 5070
			5102, -- MoMorphAdhocProhib 5102
			5040, -- MoMorphData 5040
			5041, -- MoMorphSynAnalysis 5041
			5042, -- MoMorphType 5042
			5001, -- MoStemMsa 5001
			5117, -- MoUnclassifiedAffixMsa 5117
			5049, -- PartOfSpeech 5049
			5098, -- PhCode 5098
			5097, -- PhEnvironment 5097
			5096, -- PhFeatureConstraint 5096
			5082, -- PhIterationContext 5082
			5130, -- PhMetathesisRule 5130
			5094, -- PhNCFeatures 5094
			5095, -- PhNCSegments 5095
			5099, -- PhPhonData 5099
			5092, -- PhPhoneme 5092
			5129, -- PhRegularRule 5129
			5128, -- PhSegmentRule 5128
			5131, -- PhSegRuleRHS
			5083, -- PhSequenceContext 5083
			5085, -- PhSimpleContextBdry 5085
			5086, -- PhSimpleContextNC 5086
			5087, -- PhSimpleContextSeg 5087
			5088 -- PhVariable 5088
			))
GO

if object_id('WasParsingLexiconDataModified') is not null begin
	drop proc WasParsingLexiconDataModified
end
go

CREATE PROC [WasParsingLexiconDataModified]
			@stampCompare TIMESTAMP
AS
	SELECT TOP 1 Id
	FROM CmObject co
	WHERE co.UpdStmp > @stampCompare
		AND (co.Class$ IN (
			51, -- FsClosedValue 51
			53, -- FsComplexValue 53
			57, -- FsFeatureStructure 57
			59, -- FsFeatureStructureType 59
			5005, -- LexDb 5005
			5002, -- LexEntry 5002
			5027, -- MoAffixAllomorph 5027
			5028, -- MoAffixForm 5028
			5029, -- MoAffixProcess 5029
			5103, -- MoCopyFromInput 5103
			5031, -- MoDerivAffMsa 5031
			5035, -- MoForm 5035
			5038, -- MoInflAffMsa 5038
			5069, -- MoInsertNC 5069
			5068, -- MoInsertPhones 5068
			5070, -- MoModifyFromInput 5070
			5045, -- MoStemAllomorph 5045
			5001, -- MoStemMsa 5001
			5117, -- MoUnclassifiedAffixMsa 5117
			5096, -- PhFeatureConstraint 5096
			5094, -- PhNCFeatures 5094
			5095, -- PhNCSegments 5095
			5099, -- PhPhonData 5099
			5092, -- PhPhoneme 5092
			5083, -- PhSequenceContext 5083
			5085, -- PhSimpleContextBdry 5085
			5086, -- PhSimpleContextNC 5086
			5087, -- PhSimpleContextSeg 5087
			5088 -- PhVariable 5088
			))
GO

-- Create new CmAgent for HC parser
declare @lp int, @agent int, @en int, @guid uniqueidentifier, @fmt varbinary(8000),
	@st int, @para int
select @en = id from LgWritingSystem where IcuLocale = 'en'
select top 1 @lp = id from LangProject
select @agent = id from CmObject where guid$ = '5093D7D7-4F18-4aad-8C86-88389476DF15'
if @agent is null begin
	exec MakeObj_CmAgent @en, 'HCParser', null, 0, 'Normal', @lp, 6001038, null, @agent output, @guid output
	update CmObject set guid$ = '5093D7D7-4F18-4aad-8C86-88389476DF15' where id = @agent
	exec MakeObj_StText 0, @agent, 23004, null, @st output, @guid output
	exec MakeObj_StTxtPara null, null, null, null, null, null, @st, 14001, null, @para output, @guid output
end
go

-----------------------------------------------------------------------------------
------ Finish or roll back transaction as applicable
-----------------------------------------------------------------------------------

DECLARE @dbVersion INT
SELECT @dbVersion = DbVer FROM Version$
IF @dbVersion = 200249
BEGIN
	UPDATE Version$ SET DbVer = 200250
	COMMIT TRANSACTION
	PRINT 'database updated to version 200250'
END
ELSE
BEGIN
	ROLLBACK TRANSACTION
	PRINT 'Update aborted: this works only if DbVer = 200249 (DbVer = ' +
			CONVERT(VARCHAR, @dbVersion) + ')'
END
GO
