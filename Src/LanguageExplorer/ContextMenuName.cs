// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl_2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace LanguageExplorer
{
	/// <summary>
	/// Instances of this class contain one of the existing context menu names. An existing
	/// context menu name is one of the const string's declared in this class.
	/// </summary>
	public class ContextMenuName
	{
		private string strValue;

		#region Constants

		public const string
		nullValue                                                    = "nullValue",
		mnuObjectChoices                                             = "mnuObjectChoices",
		mnuBrowseView												 = "mnuBrowseView",
		mnuReferenceChoices										     = "mnuReferenceChoices",
		mnuReorderVector											 = "mnuReorderVector",
		mnuBrowseHeader											     = "mnuBrowseHeader",
		PaneBar_ReversalIndicesMenu								     = "PaneBar-ReversalIndicesMenu",
		mnuEnvReferenceChoices										 = "mnuEnvReferenceChoices",
		mnuEnvChoices												 = "mnuEnvChoices",
		mnuStTextChoices											 = "mnuStTextChoices",
		mnuDataTree_Object											 = "mnuDataTree-Object",
		mnuDataTree_MultiStringSlice								 = "mnuDataTree-MultiStringSlice",
		PaneBar_ShowHiddenFields_posEdit							 = "PaneBar-ShowHiddenFields-posEdit",
		PaneBar_ShowHiddenFields_compoundRuleAdvancedEdit			 = "PaneBar-ShowHiddenFields-compoundRuleAdvancedEdit",
		PaneBar_ShowHiddenFields_phonemeEdit						 = "PaneBar-ShowHiddenFields-phonemeEdit",
		PaneBar_ShowHiddenFields_naturalClassedit					 = "PaneBar-ShowHiddenFields-naturalClassedit",
		PaneBar_ShowHiddenFields_EnvironmentEdit					 = "PaneBar-ShowHiddenFields-EnvironmentEdit",
		PaneBar_ShowHiddenFields_PhonologicalRuleEdit				 = "PaneBar-ShowHiddenFields-PhonologicalRuleEdit",
		PaneBar_ShowHiddenFields_AdhocCoprohibEdit					 = "PaneBar-ShowHiddenFields-AdhocCoprohibEdit",
		PaneBar_ShowHiddenFields_phonologicalFeaturesAdvancedEdit	 = "PaneBar-ShowHiddenFields-phonologicalFeaturesAdvancedEdit",
		PaneBar_ShowHiddenFields_featuresAdvancedEdit				 = "PaneBar-ShowHiddenFields-featuresAdvancedEdit",
		PaneBar_ShowHiddenFields_ProdRestrictEdit					 = "PaneBar-ShowHiddenFields-ProdRestrictEdit",
		PaneBar_ShowHiddenFields_lexiconProblems					 = "PaneBar-ShowHiddenFields-lexiconProblems",
		PaneBar_ShowHiddenFields_rapidDataEntry					     = "PaneBar-ShowHiddenFields-rapidDataEntry",
		PaneBar_ShowHiddenFields_domainTypeEdit					     = "PaneBar-ShowHiddenFields-domainTypeEdit",
		PaneBar_ShowHiddenFields_anthroEdit						     = "PaneBar-ShowHiddenFields-anthroEdit",
		PaneBar_ShowHiddenFields_confidenceEdit					     = "PaneBar-ShowHiddenFields-confidenceEdit",
		PaneBar_ShowHiddenFields_chartmarkEdit						 = "PaneBar-ShowHiddenFields-chartmarkEdit",
		PaneBar_ShowHiddenFields_charttempEdit						 = "PaneBar-ShowHiddenFields-charttempEdit",
		PaneBar_ShowHiddenFields_dialectsListEdit					 = "PaneBar-ShowHiddenFields-dialectsListEdit",
		PaneBar_ShowHiddenFields_educationEdit						 = "PaneBar-ShowHiddenFields-educationEdit",
		PaneBar_ShowHiddenFields_extNoteTypeEdit					 = "PaneBar-ShowHiddenFields-extNoteTypeEdit",
		PaneBar_ShowHiddenFields_roleEdit							 = "PaneBar-ShowHiddenFields-roleEdit",
		PaneBar_ShowHiddenFields_genresEdit						     = "PaneBar-ShowHiddenFields-genresEdit",
		PaneBar_ShowHiddenFields_featureTypesAdvancedEdit			 = "PaneBar-ShowHiddenFields-featureTypesAdvancedEdit",
		PaneBar_ShowHiddenFields_languagesListEdit					 = "PaneBar-ShowHiddenFields-languagesListEdit",
		PaneBar_ShowHiddenFields_lexRefEdit						     = "PaneBar-ShowHiddenFields-lexRefEdit",
		PaneBar_ShowHiddenFields_locationsEdit						 = "PaneBar-ShowHiddenFields-locationsEdit",
		PaneBar_ShowHiddenFields_publicationsEdit					 = "PaneBar-ShowHiddenFields-publicationsEdit",
		PaneBar_ShowHiddenFields_complexEntryTypeEdit				 = "PaneBar-ShowHiddenFields-complexEntryTypeEdit",
		PaneBar_ShowHiddenFields_variantEntryTypeEdit				 = "PaneBar-ShowHiddenFields-variantEntryTypeEdit",
		PaneBar_ShowHiddenFields_morphTypeEdit						 = "PaneBar-ShowHiddenFields-morphTypeEdit",
		PaneBar_ShowHiddenFields_peopleEdit						     = "PaneBar-ShowHiddenFields-peopleEdit",
		PaneBar_ShowHiddenFields_positionsEdit						 = "PaneBar-ShowHiddenFields-positionsEdit",
		PaneBar_ShowHiddenFields_restrictionsEdit					 = "PaneBar-ShowHiddenFields-restrictionsEdit",
		PaneBar_ShowHiddenFields_semanticDomainEdit				     = "PaneBar-ShowHiddenFields-semanticDomainEdit",
		PaneBar_ShowHiddenFields_senseTypeEdit						 = "PaneBar-ShowHiddenFields-senseTypeEdit",
		PaneBar_ShowHiddenFields_statusEdit						     = "PaneBar-ShowHiddenFields-statusEdit",
		PaneBar_ShowHiddenFields_translationTypeEdit				 = "PaneBar-ShowHiddenFields-translationTypeEdit",
		PaneBar_ShowHiddenFields_recTypeEdit						 = "PaneBar-ShowHiddenFields-recTypeEdit",
		PaneBar_ShowHiddenFields_scrNoteTypesEdit					 = "PaneBar-ShowHiddenFields-scrNoteTypesEdit",
		PaneBar_ShowHiddenFields_timeOfDayEdit						 = "PaneBar-ShowHiddenFields-timeOfDayEdit",
		PaneBar_ShowHiddenFields_weatherConditionEdit				 = "PaneBar-ShowHiddenFields-weatherConditionEdit",
		PaneBar_ShowHiddenFields_textMarkupTagsEdit				     = "PaneBar-ShowHiddenFields-textMarkupTagsEdit",
		PaneBar_ShowHiddenFields_usageTypeEdit						 = "PaneBar-ShowHiddenFields-usageTypeEdit",
		PaneBar_ShowHiddenFields_reversalToolReversalIndexPOS		 = "PaneBar-ShowHiddenFields-reversalToolReversalIndexPOS",
		PaneBar_WordformDetail										 = "PaneBar-WordformDetail",
		PaneBar_Dictionary											 = "PaneBar-Dictionary",
		PaneBar_ReversalEntryDetail								     = "PaneBar-ReversalEntryDetail",
		PaneBar_ShowFailingItems_Classified						     = "PaneBar-ShowFailingItems-Classified",
		mnuDataTree_Help											 = "mnuDataTree-Help",
		mnuDataTree_LexemeForm										 = "mnuDataTree-LexemeForm",
		mnuDataTree_LexemeFormContext								 = "mnuDataTree-LexemeFormContext",
		mnuDataTree_CitationFormContext							     = "mnuDataTree-CitationFormContext",
		mnuDataTree_Allomorphs										 = "mnuDataTree-Allomorphs",
		mnuDataTree_AlternateForms									 = "mnuDataTree-AlternateForms",
		mnuDataTree_Allomorphs_Hotlinks							     = "mnuDataTree-Allomorphs-Hotlinks",
		mnuDataTree_AlternateForms_Hotlinks						     = "mnuDataTree-AlternateForms-Hotlinks",
		mnuDataTree_VariantForms									 = "mnuDataTree-VariantForms",
		mnuDataTree_VariantForms_Hotlinks							 = "mnuDataTree-VariantForms-Hotlinks",
		mnuDataTree_VariantForm									     = "mnuDataTree-VariantForm",
		mnuDataTree_VariantFormContext								 = "mnuDataTree-VariantFormContext",
		mnuDataTree_Allomorph										 = "mnuDataTree-Allomorph",
		mnuDataTree_AffixProcess									 = "mnuDataTree-AffixProcess",
		mnuDataTree_Picture										     = "mnuDataTree-Picture",
		mnuDataTree_AlternateForm									 = "mnuDataTree-AlternateForm",
		mnuDataTree_MSAs											 = "mnuDataTree-MSAs",
		mnuDataTree_MSA											     = "mnuDataTree-MSA",
		mnuDataTree_Variants										 = "mnuDataTree-Variants",
		mnuDataTree_Variant										     = "mnuDataTree-Variant",
		mnuDataTree_Senses											 = "mnuDataTree-Senses",
		mnuDataTree_Sense											 = "mnuDataTree-Sense",
		mnuDataTree_Sense_Hotlinks									 = "mnuDataTree-Sense-Hotlinks",
		mnuDataTree_Examples										 = "mnuDataTree-Examples",
		mnuDataTree_Example										     = "mnuDataTree-Example",
		mnuDataTree_Example_ForNotes								 = "mnuDataTree-Example-ForNotes",
		mnuDataTree_Translations									 = "mnuDataTree-Translations",
		mnuDataTree_Translation									     = "mnuDataTree-Translation",
		mnuDataTree_ExtendedNotes									 = "mnuDataTree-ExtendedNotes",
		mnuDataTree_ExtendedNote									 = "mnuDataTree-ExtendedNote",
		mnuDataTree_ExtendedNote_Hotlinks							 = "mnuDataTree-ExtendedNote-Hotlinks",
		mnuDataTree_ExtendedNote_Examples							 = "mnuDataTree-ExtendedNote-Examples",
		mnuDataTree_Pronunciation									 = "mnuDataTree-Pronunciation",
		mnuDataTree_DeletePronunciation							     = "mnuDataTree-DeletePronunciation",
		mnuDataTree_SubEntryLink									 = "mnuDataTree-SubEntryLink",
		mnuDataTree_Subsenses										 = "mnuDataTree-Subsenses",
		mnuDataTree_Etymology										 = "mnuDataTree-Etymology",
		mnuDataTree_DeleteEtymology								     = "mnuDataTree-DeleteEtymology",
		mnuDataTree_Etymology_Hotlinks								 = "mnuDataTree-Etymology-Hotlinks",
		mnuDataTree_DeleteAddLexReference							 = "mnuDataTree-DeleteAddLexReference",
		mnuDataTree_DeleteReplaceLexReference						 = "mnuDataTree-DeleteReplaceLexReference",
		mnuDataTree_InsertReversalSubentry							 = "mnuDataTree-InsertReversalSubentry",
		mnuDataTree_InsertReversalSubentry_Hotlinks				     = "mnuDataTree-InsertReversalSubentry-Hotlinks",
		mnuDataTree_MoveReversalIndexEntry							 = "mnuDataTree-MoveReversalIndexEntry",
		mnuDataTree_Environments_Insert							     = "mnuDataTree-Environments-Insert",
		mnuDataTree_CmMedia										     = "mnuDataTree-CmMedia",
		mnuDataTree_VariantSpec									     = "mnuDataTree-VariantSpec",
		mnuDataTree_ComplexFormSpec								     = "mnuDataTree-ComplexFormSpec",
		PaneBar_ITextContent										 = "PaneBar-ITextContent",
		mnuIText_FreeTrans											 = "mnuIText-FreeTrans",
		mnuIText_LitTrans											 = "mnuIText-LitTrans",
		mnuIText_Note												 = "mnuIText-Note",
		mnuIText_RawText											 = "mnuIText-RawText",
		mnuFocusBox												     = "mnuFocusBox",
		mnuDataTree_MainWordform									 = "mnuDataTree-MainWordform",
		mnuDataTree_WordformSpelling								 = "mnuDataTree-WordformSpelling",
		mnuDataTree_MainWordform_Hotlinks							 = "mnuDataTree-MainWordform-Hotlinks",
		mnuDataTree_HumanApprovedAnalysisSummary					 = "mnuDataTree-HumanApprovedAnalysisSummary",
		mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks			 = "mnuDataTree-HumanApprovedAnalysisSummary-Hotlinks",
		mnuDataTree_HumanApprovedAnalysis							 = "mnuDataTree-HumanApprovedAnalysis",
		mnuDataTree_HumanApprovedAnalysis_Hotlinks					 = "mnuDataTree-HumanApprovedAnalysis-Hotlinks",
		mnuDataTree_ParserProducedAnalysis							 = "mnuDataTree-ParserProducedAnalysis",
		mnuDataTree_HumanDisapprovedAnalysis						 = "mnuDataTree-HumanDisapprovedAnalysis",
		mnuDataTree_WordGlossForm									 = "mnuDataTree-WordGlossForm",
		mnuComplexConcordance										 = "mnuComplexConcordance",
		PaneBar_RecordDetail										 = "PaneBar-RecordDetail",
		mnuDataTree_Subrecord_Hotlinks								 = "mnuDataTree_Subrecord_Hotlinks",
		mnuDataTree_Participants									 = "mnuDataTree-Participants",
		mnuDataTree_SubRecords										 = "mnuDataTree-SubRecords",
		mnuDataTree_SubRecords_Hotlinks							     = "mnuDataTree-SubRecords-Hotlinks",
		mnuDataTree_SubRecordSummary								 = "mnuDataTree-SubRecordSummary",
		mnuDataTree_InsertQuestion									 = "mnuDataTree-InsertQuestion",
		mnuDataTree_DeleteQuestion									 = "mnuDataTree-DeleteQuestion",
		mnuDataTree_SubPossibilities								 = "mnuDataTree-SubPossibilities",
		mnuDataTree_SubSemanticDomain								 = "mnuDataTree-SubSemanticDomain",
		mnuDataTree_SubCustomItem									 = "mnuDataTree-SubCustomItem",
		mnuDataTree_SubAnnotationDefn								 = "mnuDataTree-SubAnnotationDefn",
		mnuDataTree_SubMorphType									 = "mnuDataTree-SubMorphType",
		mnuDataTree_SubComplexEntryType							     = "mnuDataTree-SubComplexEntryType",
		mnuDataTree_SubVariantEntryType							     = "mnuDataTree-SubVariantEntryType",
		mnuDataTree_SubAnthroCategory								 = "mnuDataTree-SubAnthroCategory",
		mnuDataTree_DeletePossibility								 = "mnuDataTree-DeletePossibility",
		mnuDataTree_DeleteCustomItem								 = "mnuDataTree-DeleteCustomItem",
		mnuDataTree_SubLocation									     = "mnuDataTree-SubLocation",
		mnuDataTree_MoveMainReversalPOS							     = "mnuDataTree-MoveMainReversalPOS",
		mnuDataTree_MoveReversalPOS								     = "mnuDataTree-MoveReversalPOS",
		mnuDataTree_Text											 = "mnuDataTree-Text",
		mnuTextInfo_Notebook										 = "mnuTextInfo-Notebook",
		mnuDataTree_Adhoc_Group_Members							     = "mnuDataTree-Adhoc-Group-Members",
		mnuDataTree_Delete_Adhoc_Morpheme							 = "mnuDataTree-Delete-Adhoc-Morpheme",
		mnuDataTree_Delete_Adhoc_Allomorph							 = "mnuDataTree-Delete-Adhoc-Allomorph",
		mnuDataTree_Delete_Adhoc_Group								 = "mnuDataTree-Delete-Adhoc-Group",
		mnuDataTree_FeatureStructure_Feature						 = "mnuDataTree-FeatureStructure-Feature",
		mnuDataTree_ClosedFeature_Values							 = "mnuDataTree-ClosedFeature-Values",
		mnuDataTree_ClosedFeature_Value							     = "mnuDataTree-ClosedFeature-Value",
		mnuDataTree_FeatureStructure_Features						 = "mnuDataTree-FeatureStructure-Features",
		mnuDataTree_POS_AffixSlots									 = "mnuDataTree-POS-AffixSlots",
		mnuDataTree_POS_AffixSlot									 = "mnuDataTree-POS-AffixSlot",
		mnuDataTree_POS_AffixTemplates								 = "mnuDataTree-POS-AffixTemplates",
		mnuDataTree_POS_AffixTemplate								 = "mnuDataTree-POS-AffixTemplate",
		mnuDataTree_POS_InflectionClass_Subclasses					 = "mnuDataTree-POS-InflectionClass-Subclasses",
		mnuDataTree_POS_InflectionClasses							 = "mnuDataTree-POS-InflectionClasses",
		mnuDataTree_POS_InflectionClass							     = "mnuDataTree-POS-InflectionClass",
		mnuDataTree_POS_StemNames									 = "mnuDataTree-POS-StemNames",
		mnuDataTree_POS_StemName									 = "mnuDataTree-POS-StemName",
		mnuDataTree_MoStemName_Regions								 = "mnuDataTree-MoStemName-Regions",
		mnuDataTree_MoStemName_Region								 = "mnuDataTree-MoStemName-Region",
		mnuDataTree_POS_SubPossibilities							 = "mnuDataTree-POS-SubPossibilities",
		mnuDataTree_Phoneme_Codes									 = "mnuDataTree-Phoneme-Codes",
		mnuDataTree_Phoneme_Code									 = "mnuDataTree-Phoneme-Code",
		mnuDataTree_StringRepresentation_Insert					     = "mnuDataTree-StringRepresentation-Insert",
		mnuInflAffixTemplate_TemplateTable							 = "mnuInflAffixTemplate-TemplateTable",
		mnuPhRegularRule											 = "mnuPhRegularRule",
		mnuPhMetathesisRule										     = "mnuPhMetathesisRule",
		mnuMoAffixProcess											 = "mnuMoAffixProcess";

		#endregion Constants

		/// <summary>
		/// Constructor.
		/// Throws an exception if the string passed in is not one of the const string's
		/// declared in this class.
		/// </summary>
		public ContextMenuName(string str)
		{
			ContextMenuNameValidator.Instance.Validate(str);
			strValue = str;
		}

		/// <summary>
		/// Implicit conversion operator from a ContextMenuName to a string.
		/// </summary>
		public static implicit operator string(ContextMenuName contMnu) => contMnu.strValue;

		/// <summary>
		/// Implicit conversion operator from a string to a ContextMenuName.
		/// Throws an exception if the string passed in is not one of the const string's
		/// declared in this class.
		/// </summary>
		public static implicit operator ContextMenuName(string str)
		{
			return new ContextMenuName(str);
		}
	}

	/// <summary>
	/// Singleton class used to validate that a string is one of the const string's in
	/// ContextMenuName.
	/// </summary>
	public sealed class ContextMenuNameValidator
	{
		private static ContextMenuNameValidator instance = null;
		private readonly List<string> ListOfValidNames;

		private ContextMenuNameValidator()
		{
			// Use reflection to get a list of all the public const string's in ContextMenuName.
			ListOfValidNames =
				typeof(ContextMenuName)
					.GetFields()
					.Select(x => x.GetValue(null).ToString())
					.ToList();
		}

		public static ContextMenuNameValidator Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new ContextMenuNameValidator();
				}
				return instance;
			}
		}

		/// <summary>
		/// Validate that the string passed in is one of the constants in
		/// ContextMenuName.
		/// Throws an exception if it is not.
		/// </summary>
		public void Validate(string str)
		{
			if (!ListOfValidNames.Contains(str))
			{
				string errorMessage = "The string '" + str + "' is not a ContextMenuName";
				throw new ApplicationException(errorMessage);
			}
		}
	}

}