#include "Cellar.sqi"

#define CMCG_SQL_DEFNS
#include "Cellar.sqh"
#include "FeatSys.sqh"
#include "Ling.sqh"

#include "Ling.sqo"

// include what used to be in LingSP.sql
#include "RemoveParserApprovedAnalyses$_sp.sql"
#include "WasParsingDataModified_sp.sql"
#include "WasParsingGrammarDataModified_sp.sql"
#include "WasParsingLexiconDataModified_sp.sql"
//Stored procedures related to querying sense information
#include "GetSensesForSense_sp.sql"
#include "fnGetSensesInEntry$_fn.sql"
#include "GetEntriesAndSenses$_sp.sql"
#include "GetEntryForSense_sp.sql"
#include "fnGetEntryForSense.sql"
#include "fnGetAllComplexFormEntryBackRefs.sql"
//Stored procedures related to CmAgent
#include "FindOrCreateCmAgent_sp.sql"
//Stored procedures related to Wordform Analyses
#include "CreateParserProblemAnnotation_sp.sql"
#include "SetAgentEval_sp.sql"
#include "UpdWfiAnalysisAndEval$_sp.sql"
#include "RemoveUnusedAnalyses$_sp.sql"
#include "SetParseFailureEvals_sp.sql"
#include "fnGetParseCountRange_fn.sql"
#include "IsAgentAgreement$_sp.sql"
#include "fnGetDefaultAnalysisGloss_fn.sql"
#include "fnGetDefaultAnalysesGlosses_fn.sql"
// PATRString_sp.sql includes three interrelated stored procedures. See note in file.
#include "PATRString_sp.sql"
#include "DisplayName_PhPhonContextID_sp.sql"
#include "DisplayName_PhEnvironment_sp.sql"
#include "DisplayName_PhPhonContext_sp.sql"
#include "DisplayName_MoForm_sp.sql"
#include "DisplayName_LexEntry_sp.sql"
#include "DisplayName_Msa_sp.sql"
#include "fnGetEntryAltForms.sql"
#include "fnGetEntryGlosses.sql"
#include "fnMatchEntries.sql"
#include "DisplayName_PhTerminalUnit_sp.sql"
#include "MakeMissingAnalysesFromLexicion_sp.sql"
#include "CountUpToDateParas_sp.sql"
#include "GetSegmentIndex_sp.sql"
#include "GetHeadwordsForEntriesOrSenses_fn.sql"
#include "CreateCmBaseAnnotation_sp.sql"
#include "CreateWfiWordform_sp.sql"
#include "FinalLing.sql"

// Concordance
#include "fnConcordForAnalysis.sql"
#include "fnConcordForLexEntry.sql"
#include "fnConcordForLexEntryHvo.sql"
#include "fnConcordForLexGloss.sql"
#include "fnConcordForLexSense.sql"
#include "fnConcordForMoForm.sql"
#include "fnConcordForMorphemes.sql"
#include "fnConcordForPartOfSpeech.sql"
#include "fnConcordForWfiGloss.sql"

// Annotations
#include "fnGetTextAnnotations.sql"
