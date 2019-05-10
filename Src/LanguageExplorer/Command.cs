// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// List of all unique commands used on main menus and/or tool bars.
	/// In some cases, an enum represents a menu that contains nested menus with regular commands.
	/// The separators are shared between all menus and tool bars.
	/// Some enum values are shared between menus and tool bar buttons, or in one case, between two menu items.
	/// </summary>
	internal enum Command
	{
		#region Shared separator values.
		Separator1,
		Separator2,
		Separator3,
		Separator4,
		Separator5,
		Separator6,
		Separator7,
		Separator8,
		Separator9,
		Separator10,
		Separator11,
		#endregion Shared separator values.

		#region Menus

		/// <summary>
		/// File menu
		/// </summary>
		CmdNewLangProject,
		CmdChooseLangProject,
		// Separator1 goes here
		ProjectManagementMenu, // Menu that contains sub-menus.
			CmdProjectProperties,
			CmdBackup,
			CmdRestoreFromBackup,
			// Separator2 goes here
			CmdProjectLocation,
			CmdDeleteProject,
			CmdCreateProjectShortcut,
		// Separator3 goes here
		CmdArchiveWithRamp,
		CmdUploadToWebonary,
		// Separator4 goes here
		// Not used in 9 or 10: CmdPageSetup,
		CmdPrint,
		// Separator5 goes here
		ImportMenu, // Menu that contains sub-menus.
			CmdImportSFMLexicon,
			CmdImportLinguaLinksData,
			CmdImportLiftData,
			CmdImportInterlinearSfm,
			CmdImportWordsAndGlossesSfm,
			CmdImportInterlinearData,
			CmdImportSFMNotebook,
			CmdImportTranslatedLists,
		CmdExport,
		// Separator6 goes here
		CmdExportInterlinear,
		CmdExportDiscourseChart,
		CmdClose,

		/// <summary>
		/// Send/Receive menu
		/// </summary>
		CmdFLExBridge,
		CmdViewMessages,
		CmdLiftBridge,
		CmdViewLiftMessages,
		CmdObtainAnyFlexBridgeProject,
		CmdObtainLiftProject,
		CmdObtainFirstFlexBridgeProject,
		CmdObtainFirstLiftProject,
		CmdHelpChorus,
		CmdCheckForFlexBridgeUpdates,
		CmdHelpAboutFLEXBridge,

		/// <summary>
		/// Edit menu
		/// </summary>
		CmdUndo,
		CmdRedo,
		CmdCut,
		CmdCopy,
		CmdPaste,
		CmdPasteHyperlink,
		CmdCopyLocationAsHyperlink,
		CmdGoToEntry,
		CmdGoToRecord,
		CmdFindAndReplaceText,
		CmdReplaceText,
		CmdSelectAll,
		CmdDeleteRecord,
		CmdGoToReversalEntry,
		CmdGoToWfiWordform,
		CmdDeleteCustomList,

		/// <summary>
		/// View menu
		/// </summary>
		CmdRefresh,
		LexicalToolsList,
		WordToolsList,
		GrammarToolsList,
		NotebookToolsList,
		ListsToolsList,
		ShowInvisibleSpaces,
		Show_DictionaryPubPreview,
		ShowHiddenFields,
		FiltersList,
			NoFilter,
		CmdChooseTexts,

		/// <summary>
		/// Data menu
		/// </summary>
		CmdFirstRecord,
		CmdPreviousRecord,
		CmdNextRecord,
		CmdLastRecord,
		CmdApproveAndMoveNext,
		CmdApproveForWholeTextAndMoveNext,
		CmdNextIncompleteBundle,
		CmdApprove,
		ApproveAnalysisMovementMenu,
		CmdApproveAndMoveNextSameLine,
		CmdMoveFocusBoxRight,
		CmdMoveFocusBoxLeft,
		BrowseMovementMenu,
		CmdBrowseMoveNext,
		CmdNextIncompleteBundleNc,
		CmdBrowseMoveNextSameLine,
		CmdMoveFocusBoxRightNc,
		CmdMoveFocusBoxLeftNc,
		CmdMakePhrase,
		CmdBreakPhrase,
		CmdRepeatLastMoveLeft,
		CmdRepeatLastMoveRight,
		CmdApproveAll,

		/// <summary>
		/// Insert menu
		/// </summary>
		CmdInsertLexEntry,
		CmdInsertSense,
		CmdInsertVariant,
		CmdDataTree_Insert_AlternateForm,
		CmdInsertReversalEntry,
		CmdDataTree_Insert_Pronunciation,
		CmdInsertMediaFile,
		CmdDataTree_Insert_Etymology,
		CmdInsertSubsense,
		CmdInsertPicture,
		CmdInsertExtNote,
		CmdInsertText,
		CmdAddNote,
		CmdAddWordGlossesToFreeTrans,
		ClickInvisibleSpace,
		CmdGuessWordBreaks,
		CmdImportWordSet,
		CmdInsertHumanApprovedAnalysis,
		CmdInsertRecord,
		CmdInsertSubrecord,
		CmdInsertSubsubrecord,
		CmdAddToLexicon,
		CmdInsertSemDom,
		CmdDataTree_Insert_SemanticDomain,
		CmdInsertAnnotationDef,
		CmdInsertPossibility,
		CmdInsertCustomItem,
		CmdInsertMorphType,
		CmdInsertLexEntryInflType,
		CmdInsertLexEntryType,
		CmdDataTree_Insert_LexEntryInflType,
		CmdDataTree_Insert_LexEntryType,
		CmdInsertAnthroCategory,
		CmdDataTree_Insert_AnthroCategory,
		CmdInsertPerson,
		CmdInsertLocation,
		CmdDataTree_Insert_Location,
		CmdInsertLexRefType,
		CmdInsertFeatureType,
		CmdDataTree_Insert_Possibility,
		CmdDataTree_Insert_CustomItem,
		CmdAddCustomList,
		CmdInsertPOS,
		CmdDataTree_Insert_POS_SubPossibilities,
		CmdDataTree_Insert_POS_AffixTemplate,
		CmdDataTree_Insert_POS_AffixSlot,
		CmdDataTree_Insert_POS_InflectionClass,
		CmdInsertEndocentricCompound,
		CmdInsertExocentricCompound,
		CmdInsertExceptionFeature,
		CmdInsertPhonologicalClosedFeature,
		CmdInsertClosedFeature,
		CmdInsertComplexFeature,
		CmdDataTree_Insert_ClosedFeature_Value,
		CmdInsertPhoneme,
		CmdDataTree_Insert_Phoneme_Code,
		CmdInsertSegmentNaturalClasses,
		CmdInsertFeatureNaturalClasses,
		CmdInsertPhEnvironment,
		CmdInsertPhRegularRule,
		CmdInsertPhMetathesisRule,
		CmdInsertMorphemeACP,
		CmdInsertAllomorphACP,
		CmdInsertACPGroup,
		CmdShowCharMap,
		CmdInsertLinkToFile,

		/// <summary>
		/// Format menu
		/// </summary>
		CmdFormatStyle,
		CmdFormatApplyStyle,
		WritingSystemList,
		CmdVernacularWritingSystemProperties,
		CmdAnalysisWritingSystemProperties,

		/// <summary>
		/// Tools menu
		/// </summary>
		Configure, // Menu that contains sub-menus.
			CmdConfigureDictionary,
			CmdConfigureInterlinear,
			CmdConfigureXmlDocView,
			CmdConfigureList,
			CmdConfigureColumns,
			CmdConfigHeadwordNumbers,
			CmdRestoreDefaults,
			CmdAddCustomField,
		CmdMergeEntry,
		CmdLexiconLookup,
		ITexts_AddWordsToLexicon,
		Spelling, // Menu that contains sub-menus.
			CmdEditSpellingStatus,
			CmdViewIncorrectWords,
			CmdUseVernSpellingDictionary,
			CmdChangeSpelling,
		CmdProjectUtilities,
		CmdToolsOptions,
		CmdMacroF2,
		CmdMacroF3,
		CmdMacroF4,
		CmdMacroF6,
		CmdMacroF7,
		CmdMacroF8,
		CmdMacroF9,
		CmdMacroF10,
		CmdMacroF11,
		CmdMacroF12,

		/// <summary>
		/// Parser menu
		/// </summary>
		CmdParseAllWords,
		CmdReparseAllWords,
		CmdReInitializeParser,
		CmdStopParser,
		CmdTryAWord,
		CmdParseWordsInCurrentText,
		CmdParseCurrentWord,
		CmdClearSelectedWordParserAnalyses,
		ChooseParserMenu,
		CmdChooseXAmpleParser,
		CmdChooseHCParser,
		CmdEditParserParameters,

		/// <summary>
		/// Window menu
		/// </summary>
		CmdNewWindow,

		/// <summary>
		/// Help menu
		/// </summary>
		CmdHelpLanguageExplorer,
		CmdHelpTraining,
		CmdHelpDemoMovies,
		Resources, // Menu that contains sub-menus.
			CmdHelpLexicographyIntro,
			CmdHelpMorphologyIntro,
			CmdHelpNotesSendReceive,
			CmdHelpNotesSFMDatabaseImport,
			CmdHelpNotesLinguaLinksDatabaseImport,
			CmdHelpNotesInterlinearImport,
			CmdHelpNotesWritingSystems,
			CmdHelpXLingPap,
		CmdHelpReportBug,
		CmdHelpMakeSuggestion,
		CmdHelpAbout,

		#endregion  Menus

		#region Toolbars

		/// <summary>
		/// Format toolbar
		/// </summary>
		// WritingSystemList, Also on Format menu
		CombinedStylesList,

		/// <summary>
		/// Insert toolbar
		/// </summary>
		// CmdInsertLexEntry,
		// CmdGoToEntry,
		// CmdInsertReversalEntry,
		// CmdGoToReversalEntry,
		// CmdInsertText,
		// CmdAddNote,
		// CmdApproveAll,
		// CmdInsertHumanApprovedAnalysis,
		// CmdGoToWfiWordform,
		// CmdFindAndReplaceText,
		// CmdBreakPhrase,
		// CmdInsertRecord,
		// CmdGoToRecord,
		// CmdAddToLexicon,
		// CmdLexiconLookup,
		// CmdInsertSemDom,
		// CmdDataTree_Insert_SemanticDomain,
		// CmdInsertAnnotationDef,
		// CmdInsertPossibility,
		// CmdInsertCustomItem,
		// CmdInsertMorphType,
		// CmdInsertLexEntryInflType,
		// CmdInsertLexEntryType,
		// CmdDataTree_Insert_LexEntryInflType,
		// CmdDataTree_Insert_LexEntryType,
		// CmdInsertAnthroCategory,
		// CmdDataTree_Insert_AnthroCategory,
		// CmdInsertPerson,
		// CmdInsertLocation,
		// CmdDataTree_Insert_Location,
		// CmdInsertLexRefType,
		// CmdInsertFeatureType,
		// CmdDataTree_Insert_Possibility,
		// CmdDataTree_Insert_CustomItem,
		CmdDuplicateSemDom,
		CmdDuplicateAnnotationDef,
		CmdDuplicatePossibility,
		CmdDuplicateCustomItem,
		CmdDuplicateMorphType,
		CmdDuplicateAnthroCategory,
		CmdDuplicatePerson,
		CmdDuplicateLocation,
		CmdDuplicateLexRefType,
		CmdDuplicateFeatureType,
		// CmdInsertPOS,
		// CmdDataTree_Insert_POS_SubPossibilities,
		// CmdInsertEndocentricCompound,
		// CmdInsertExocentricCompound,
		// CmdInsertExceptionFeature,
		// CmdInsertPhonologicalClosedFeature,
		// CmdInsertClosedFeature,
		// CmdInsertComplexFeature,
		// CmdInsertPhoneme,
		// CmdInsertSegmentNaturalClasses,
		// CmdInsertFeatureNaturalClasses,
		// CmdInsertPhEnvironment,
		// CmdInsertPhRegularRule,
		// CmdInsertPhMetathesisRule,
		// CmdInsertMorphemeACP,
		// CmdInsertAllomorphACP,
		// CmdInsertACPGroup,

		/// <summary>
		/// View ToolBar
		/// </summary>
		// CmdChooseTexts,
		CmdChangeFilterClearAll,

		/// <summary>
		/// Standard ToolBar
		/// </summary>
		CmdHistoryBack,
		CmdHistoryForward,
		// CmdDeleteRecord,
		// CmdUndo,
		// CmdRedo,
		// CmdRefresh,
		// CmdFirstRecord,
		// CmdPreviousRecord,
		// CmdNextRecord,
		// CmdLastRecord,
		CmdFLExLiftBridge,

		#endregion Toolbars

		#region context menus

		// <menu id="mnuObjectChoices">
		CmdEntryJumpToDefault,
		CmdWordformJumpToAnalyses,
		Show_Concordance_of, // Menu that contains sub-menus.
			CmdWordformJumpToConcordance,
			CmdAnalysisJumpToConcordance,
			CmdMorphJumpToConcordance,
			CmdEntryJumpToConcordance,
			CmdSenseJumpToConcordance,
			CmdLexGramInfoJumpToConcordance,
			CmdWordGlossJumpToConcordance,
			CmdWordPOSJumpToConcordance,
		CmdPOSJumpToDefault,
		CmdWordPOSJumpToDefault,
		CmdEndoCompoundRuleJumpToDefault,
		CmdExoCompoundRuleJumpToDefault,
		CmdPhonemeJumpToDefault,
		CmdNaturalClassJumpToDefault,
		CmdEnvironmentsJumpToDefault,
		// <item label="-" translate="do not translate" />
		CmdDeleteSelectedObject,

		// <menu id="mnuBrowseView">
		//CmdEntryJumpToDefault,
		//CmdWordformJumpToAnalyses,
		//CmdPOSJumpToDefault,
		//CmdWordformJumpToConcordance,
		//CmdEntryJumpToConcordance,
		//CmdSenseJumpToConcordance,
		CmdPOSJumpToConcordance,
		//CmdWordGlossJumpToConcordance,
		//CmdWordPOSJumpToConcordance,
		//CmdWordPOSJumpToDefault,
		//CmdEndoCompoundRuleJumpToDefault,
		//CmdExoCompoundRuleJumpToDefault,
		//CmdPhonemeJumpToDefault,
		//CmdNaturalClassJumpToDefault,
		//CmdEnvironmentsJumpToDefault,
		// <item label="-" translate="do not translate" />
		//CmdDeleteSelectedObject,
		/*
    <!-- The following commands are involked/displayed on a right click on a slice on at Possibility list item.

			 In the C# code see the following  class ReferenceViewBase and class ReferenceBaseUi
			 where ContextMenuId returns  "mnuReferenceChoices".

			 Search in the xml files for the particular command (for example CmdJumpToAnthroList and CmdJumpToAnthroList2)
			 See how the command has the following parameters
				 className="CmAnthroItem" ownerClass="LangProject" ownerField="AnthroList"
			 These parameters must be used to determine that this command is only shown on slices which contain
			 Anthropology Categories.  The messsage is the command that is executed.-->
		*/
		// <menu id="mnuReferenceChoices">
		//CmdEntryJumpToDefault,
		CmdRecordJumpToDefault,
		//CmdAnalysisJumpToConcordance,
		// <item label="-" translate="do not translate" />
		CmdLexemeFormJumpToConcordance,
		//CmdEntryJumpToConcordance,
		//CmdSenseJumpToConcordance,
		CmdJumpToAcademicDomainList,
		CmdJumpToAnthroList,
		CmdJumpToLexiconEditWithFilter,
		CmdJumpToNotebookEditWithFilter,
		CmdJumpToConfidenceList,
		CmdJumpToDialectLabelsList,
		CmdJumpToDiscChartMarkerList,
		CmdJumpToDiscChartTemplateList,
		CmdJumpToEducationList,
		CmdJumpToRoleList,
		CmdJumpToExtNoteTypeList,
		CmdJumpToComplexEntryTypeList,
		CmdJumpToVariantEntryTypeList,
		CmdJumpToTextMarkupTagsList,
		CmdJumpToLexRefTypeList,
		CmdJumpToLanguagesList,
		CmdJumpToLocationList,
		CmdJumpToPublicationList,
		CmdJumpToMorphTypeList,
		CmdJumpToPeopleList,
		CmdJumpToPositionList,
		CmdJumpToRestrictionsList,
		CmdJumpToSemanticDomainList,
		CmdJumpToGenreList,
		CmdJumpToSenseTypeList,
		CmdJumpToStatusList,
		CmdJumpToTranslationTypeList,
		CmdJumpToUsageTypeList,
		CmdJumpToRecordTypeList,
		CmdJumpToTimeOfDayList,
		// <item label="-" translate="do not translate" />
		CmdShowSubentryUnderComponent,
		CmdVisibleComplexForm,
		CmdMoveTargetToPreviousInSequence,
		CmdMoveTargetToNextInSequence,
#if RANDYTODO
			// TODO:
  <contextMenus>
    <menu id="mnuReorderVector">
      <item command="CmdMoveTargetToPreviousInSequence" />
      <item command="CmdMoveTargetToNextInSequence" />
      <item command="CmdAlphabeticalOrder" />
    </menu>
    <menu id="mnuBrowseHeader">
      <item label="Sorted From End" boolProperty="SortedFromEnd" />
      <item label="Sorted By Length" boolProperty="SortedByLength" />
    </menu>
    <menu id="PaneBar_ReversalIndicesMenu">
      <menu id="ReversalIndexPaneMenu" icon="MenuWidget" alignment="right">
        <item command="CmdShowAllPublications" />
        <menu list="Publications" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="SelectedPublication" />
        <item label="-" translate="do not translate" />
        <item command="CmdPublicationsJumpToDefault" />
      </menu>
      <menu id="ReversalIndexPaneMenu" icon="MenuWidget" alignment="left">
        <menu inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" list="ReversalIndexList" property="ReversalIndexPublicationLayout" />
        <item label="-" translate="do not translate" />
        <item command="CmdConfigureDictionary" />
      </menu>
    </menu>
    <menu id="mnuEnvReferenceChoices">
      <item command="CmdJumpToEnvironmentList" />
      <item command="CmdShowEnvironmentErrorMessage" />
      <item label="-" translate="do not translate" />
      <item command="CmdInsertEnvSlash" />
      <item command="CmdInsertEnvUnderscore" />
      <item command="CmdInsertEnvNaturalClass" />
      <item command="CmdInsertEnvOptionalItem" />
      <item command="CmdInsertEnvHashMark" />
    </menu>
    <menu id="mnuEnvChoices">
      <item command="CmdShowEnvironmentErrorMessage" />
      <item label="-" translate="do not translate" />
      <item command="CmdInsertEnvSlash" />
      <item command="CmdInsertEnvUnderscore" />
      <item command="CmdInsertEnvNaturalClass" />
      <item command="CmdInsertEnvOptionalItem" />
      <item command="CmdInsertEnvHashMark" />
    </menu>
    <menu id="mnuStTextChoices">
      <item command="CmdCut" />
      <item command="CmdCopy" />
      <item command="CmdPaste" />
      <item label="-" translate="do not translate" />
      <item command="CmdLexiconLookup" />
      <item command="CmdAddToLexicon" />
    </menu>
    <menu id="mnuDataTree_Object">
      <menu id="mnuVisibility" label="Field Visibility">
        <item command="CmdAlwaysVisible" />
        <item command="CmdIfData" />
        <item command="CmdNormallyHidden" />
      </menu>
      <item command="CmdDataTree_Help" />
    </menu>
    <menu id="mnuDataTree_MultiStringSlice">
      <item label="-" translate="do not translate" />
      <!--Enhance so we don't have to copy the mnuVisibility <include path="CommonDataTreeInclude.xml" query="root/contextMenus/menu/menu[@id='mnuVisibility']"/> -->
      <menu id="mnuVisibility" label="Field Visibility">
        <item command="CmdAlwaysVisible" />
        <item command="CmdIfData" />
        <item command="CmdNormallyHidden" />
      </menu>
      <menu id="DataTree_WritingSystemsMenu" label="Writing Systems">
        <item command="CmdDataTree_WritingSystemMenu_ShowAllRightNow" />
        <menu list="WritingSystemOptionsForSlice" inline="true" emptyAllowed="true" behavior="singlePropertySequenceValue" property="SelectedWritingSystemHvosForCurrentContextMenu" defaultPropertyValue="" />
        <item command="CmdDataTree_WritingSystemMenu_Configure" />
      </menu>
      <item command="CmdDataTree_Help" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_posEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_posEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_compoundRuleAdvancedEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_compoundRuleAdvancedEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_phonemeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_phonemeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_naturalClassedit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_naturalClassedit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_EnvironmentEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_EnvironmentEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_PhonologicalRuleEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_PhonologicalRuleEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_AdhocCoprohibEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_AdhocCoprohibEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_phonologicalFeaturesAdvancedEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_phonologicalFeaturesAdvancedEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_featuresAdvancedEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_featuresAdvancedEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_ProdRestrictEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_ProdRestrictEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_lexiconProblems" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_lexiconProblems" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_rapidDataEntry" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_rapidDataEntry" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_domainTypeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_domainTypeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_anthroEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_anthroEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_confidenceEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_confidenceEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_chartmarkEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_chartmarkEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_charttempEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_charttempEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_dialectsListEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_dialectsListEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_educationEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_educationEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_extNoteTypeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_extNoteTypeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_roleEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_roleEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_genresEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_genresEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_featureTypesAdvancedEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_featureTypesAdvancedEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_languagesListEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_languagesListEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_lexRefEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_lexRefEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_locationsEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_locationsEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_publicationsEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_publicationsEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_complexEntryTypeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_complexEntryTypeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_variantEntryTypeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_variantEntryTypeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_morphTypeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_morphTypeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_peopleEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_peopleEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_positionsEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_positionsEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_restrictionsEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_restrictionsEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_semanticDomainEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_semanticDomainEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_senseTypeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_senseTypeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_statusEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_statusEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_translationTypeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_translationTypeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_recTypeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_recTypeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_scrNoteTypesEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_scrNoteTypesEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_timeOfDayEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_timeOfDayEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_weatherConditionEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_weatherConditionEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_textMarkupTagsEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_textMarkupTagsEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_usageTypeEdit" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_usageTypeEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowHiddenFields_reversalToolReversalIndexPOS" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_reversalToolReversalIndexPOS" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_WordformDetail" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_WordsEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_LexicalDetail" label="">
      <menu id="LexEntryPaneMenu" icon="MenuWidget">
        <item label="Show Dictionary Preview" boolProperty="Show_DictionaryPubPreview" />
        <item label="-" translate="do not translate" />
        <item label="Insert _Sense" command="CmdInsertSense" />
        <item label="Insert Subsense (in sense)" command="CmdInsertSubsense" />
        <item label="Insert _Variant" command="CmdInsertVariant" />
        <item label="Insert A_llomorph" command="CmdDataTree_Insert_AlternateForm" />
        <item label="Insert _Pronunciation" command="CmdDataTree_Insert_Pronunciation" />
        <item label="Insert Sound or Movie _File" command="CmdInsertMediaFile" />
        <item label="Insert _Etymology" command="CmdDataTree_Insert_Etymology" />
        <item label="-" translate="do not translate" />
        <item command="CmdChangeToComplexForm" />
        <item command="CmdChangeToVariant" />
        <item command="CmdMergeEntry" defaultVisible="false" />
        <item label="-" translate="do not translate" />
        <item command="CmdRootEntryJumpToConcordance" />
      </menu>
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_lexiconEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_Dictionary" label="">
      <!-- *** Put any alignment="right" menus in first. *** -->
      <menu id="LexEntryPaneMenu" icon="MenuWidget" alignment="right">
        <menu list="Configurations" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="DictionaryPublicationLayout" />
        <item label="-" translate="do not translate" />
        <item command="CmdConfigureDictionary" />
      </menu>
      <menu id="LexEntryPaneMenu" icon="MenuWidget" alignment="left">
        <item command="CmdShowAllPublications" />
        <menu list="Publications" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="SelectedPublication" />
        <item label="-" translate="do not translate" />
        <item command="CmdPublicationsJumpToDefault" />
      </menu>
    </menu>
    <menu id="PaneBar_ReversalEntryDetail" label="">
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_reversalToolEditComplete" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="PaneBar_ShowFailingItems_Classified" label="">
      <item label="Show Unused Items" boolProperty="ShowFailingItems_lexiconClassifiedDictionary" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="mnuDataTree_Help">
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_LexemeForm">
      <item command="CmdMorphJumpToConcordance" label="Show Lexeme Form in Concordance" />
      <item command="CmdDataTree_Swap_LexemeForm" />
      <item command="CmdDataTree_Convert_LexemeForm_AffixProcess" />
      <item command="CmdDataTree_Convert_LexemeForm_AffixAllomorph" />
    </menu>
    <menu id="mnuDataTree_LexemeFormContext">
      <item command="CmdEntryJumpToConcordance" />
      <!-- Show Entry in Concordance -->
      <item command="CmdLexemeFormJumpToConcordance" />
      <item command="CmdDataTree_Swap_LexemeForm" />
    </menu>
    <menu id="mnuDataTree_CitationFormContext">
      <item command="CmdEntryJumpToConcordance" />
    </menu>
    <menu id="mnuDataTree_Allomorphs">
      <item command="CmdDataTree_Insert_StemAllomorph" />
      <item command="CmdDataTree_Insert_AffixAllomorph" />
    </menu>
    <menu id="mnuDataTree_AlternateForms">
      <item command="CmdDataTree_Insert_AlternateForm" />
      <item command="CmdDataTree_Insert_AffixProcess" />
    </menu>
    <menu id="mnuDataTree_Allomorphs_Hotlinks">
      <item command="CmdDataTree_Insert_StemAllomorph" />
      <item command="CmdDataTree_Insert_AffixAllomorph" />
    </menu>
    <menu id="mnuDataTree_AlternateForms_Hotlinks">
      <item command="CmdDataTree_Insert_AlternateForm" />
    </menu>
    <menu id="mnuDataTree_VariantForms">
      <item command="CmdDataTree_Insert_VariantForm" />
    </menu>
    <menu id="mnuDataTree_VariantForms_Hotlinks">
      <item command="CmdDataTree_Insert_VariantForm" />
    </menu>
    <menu id="mnuDataTree_VariantForm">
      <item command="CmdEntryJumpToDefault" />
      <item command="CmdEntryJumpToConcordance" />
      <item command="CmdDataTree_Delete_VariantReference" />
      <item command="CmdDataTree_Delete_Variant" />
    </menu>
    <menu id="mnuDataTree_VariantFormContext">
      <item command="CmdEntryJumpToDefault" />
      <item label="-" translate="do not translate" />
      <item command="CmdEntryJumpToConcordance" />
    </menu>
    <menu id="mnuDataTree_Allomorph">
      <item command="CmdDataTree_MoveUp_Allomorph" />
      <item command="CmdDataTree_MoveDown_Allomorph" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Merge_Allomorph" />
      <item command="CmdDataTree_Delete_Allomorph" />
      <item command="CmdDataTree_Swap_Allomorph" />
      <item command="CmdDataTree_Convert_Allomorph_AffixProcess" />
      <item label="-" translate="do not translate" />
      <item command="CmdMorphJumpToConcordance" label="Show Allomorph in Concordance" />
    </menu>
    <menu id="mnuDataTree_AffixProcess">
      <item command="CmdDataTree_MoveUp_Allomorph" />
      <item command="CmdDataTree_MoveDown_Allomorph" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Delete_Allomorph" />
      <item command="CmdDataTree_Swap_Allomorph" />
      <item command="CmdDataTree_Convert_Allomorph_AffixAllomorph" />
      <item label="-" translate="do not translate" />
      <item command="CmdMorphJumpToConcordance" label="Show Allomorph in Concordance" />
    </menu>
    <menu id="mnuDataTree_Picture">
      <item command="CmdDataTree_Properties_Picture" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_MoveUp_Picture" />
      <item command="CmdDataTree_MoveDown_Picture" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Delete_Picture" />
    </menu>
    <menu id="mnuDataTree_AlternateForm">
      <item command="CmdDataTree_MoveUp_AlternateForm" />
      <item command="CmdDataTree_MoveDown_AlternateForm" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Merge_AlternateForm" />
      <item command="CmdDataTree_Delete_AlternateForm" />
    </menu>
    <menu id="mnuDataTree_MSAs">
      <item command="CmdDataTree_Insert_DerivationAffixMsa" />
      <item command="CmdDataTree_Insert_InflectionAffixMsa" />
      <item command="CmdDataTree_Insert_UnclassifiedAffixMsa" />
      <item command="CmdDataTree_Insert_StemMsa" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_MSA">
      <item command="CmdDataTree_Help" />
      <item command="CmdDataTree_Merge_MSA" />
      <item command="CmdDataTree_Delete_MSA" />
    </menu>
    <menu id="mnuDataTree_Variants">
      <item command="CmdDataTree_Insert_Variant" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Variant">
      <item command="CmdDataTree_Delete_Variant" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Senses">
      <item command="CmdDataTree_Insert_SubSense" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Sense">
      <item command="CmdDataTree_Insert_Example" />
      <item command="CmdFindExampleSentence" />
      <item command="CmdDataTree_Insert_ExtNote" />
      <item command="CmdDataTree_Insert_SenseBelow" />
      <item command="CmdDataTree_Insert_SubSense" />
      <item command="CmdInsertPicture" label="Insert _Picture" defaultVisible="false" />
      <item label="-" translate="do not translate" />
      <item command="CmdSenseJumpToConcordance" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_MoveUp_Sense" />
      <item command="CmdDataTree_MoveDown_Sense" />
      <item command="CmdDataTree_MakeSub_Sense" />
      <item command="CmdDataTree_Promote_Sense" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Merge_Sense" />
      <item command="CmdDataTree_Split_Sense" />
      <item command="CmdDataTree_Delete_Sense" />
    </menu>
    <menu id="mnuDataTree_Sense_Hotlinks">
      <item command="CmdDataTree_Insert_Example" />
      <item command="CmdDataTree_Insert_SenseBelow" />
    </menu>
    <menu id="mnuDataTree_Examples">
      <item command="CmdDataTree_Insert_Example" />
      <item command="CmdFindExampleSentence" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Example">
      <item command="CmdDataTree_Insert_Translation" />
      <item command="CmdDataTree_Delete_Example" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_MoveUp_Example" />
      <item command="CmdDataTree_MoveDown_Example" />
      <item label="-" translate="do not translate" />
      <item command="CmdFindExampleSentence" />
    </menu>
    <menu id="mnuDataTree_Example_ForNotes">
      <item command="CmdDataTree_Insert_Example" label="Insert new Example in Note" />
      <item command="CmdDataTree_Insert_Translation" />
      <item command="CmdDataTree_Delete_Example" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_MoveUp_Example" />
      <item command="CmdDataTree_MoveDown_Example" />
      <item label="-" translate="do not translate" />
      <item command="CmdFindExampleSentence" />
    </menu>
    <menu id="mnuDataTree_Translations">
      <item command="CmdDataTree_Insert_Translation" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Translation">
      <item command="CmdDataTree_Delete_Translation" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_ExtendedNotes">
      <item command="CmdDataTree_Insert_ExtNote" />
    </menu>
    <menu id="mnuDataTree_ExtendedNote">
      <item command="CmdDataTree_Delete_ExtNote" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_MoveUp_ExtNote" />
      <item command="CmdDataTree_MoveDown_ExtNote" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Insert_ExampleInNote" />
    </menu>
    <menu id="mnuDataTree_ExtendedNote_Hotlinks">
      <item command="CmdDataTree_Insert_ExtNote" label="Insert Extended Note" />
    </menu>
    <menu id="mnuDataTree_ExtendedNote_Examples">
      <item command="CmdDataTree_Insert_ExampleInNote" />
      <item command="CmdDataTree_Delete_ExampleInNote" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_MoveUp_ExampleInNote" />
      <item command="CmdDataTree_MoveDown_ExampleInNote" />
    </menu>
    <menu id="mnuDataTree_Pronunciation">
      <item command="CmdDataTree_Insert_Pronunciation" label="Insert _Pronunciation" />
      <item command="CmdInsertMediaFile" label="Insert _Sound or Movie" defaultVisible="false" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_MoveUp_Pronunciation" />
      <item command="CmdDataTree_MoveDown_Pronunciation" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Delete_Pronunciation" />
      <item label="-" translate="do not translate" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_DeletePronunciation">
      <item command="CmdDataTree_Delete_Pronunciation" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_SubEntryLink">
      <item command="CmdDataTree_Insert_SubEntryLink" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Subsenses">
      <item command="CmdDataTree_Insert_SubSense" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Etymology">
      <item command="CmdDataTree_Insert_Etymology" label="Insert _Etymology" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_MoveUp_Etymology" />
      <item command="CmdDataTree_MoveDown_Etymology" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Delete_Etymology" />
    </menu>
    <menu id="mnuDataTree_DeleteEtymology">
      <item command="CmdDataTree_Delete_Etymology" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Etymology_Hotlinks">
      <item command="CmdDataTree_Insert_Etymology" label="Insert _Etymology" />
    </menu>
    <menu id="mnuDataTree_DeleteAddLexReference">
      <item command="CmdDataTree_Delete_LexReference" />
      <item command="CmdDataTree_Add_ToLexReference" />
      <item command="CmdDataTree_EditDetails_LexReference" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_DeleteReplaceLexReference">
      <item command="CmdDataTree_Delete_LexReference" />
      <item command="CmdDataTree_Replace_LexReference" />
      <item command="CmdDataTree_EditDetails_LexReference" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_InsertReversalSubentry">
      <item command="CmdDataTree_Insert_ReversalSubentry" />
    </menu>
    <menu id="mnuDataTree_InsertReversalSubentry_Hotlinks">
      <item command="CmdDataTree_Insert_ReversalSubentry" />
    </menu>
    <menu id="mnuDataTree_MoveReversalIndexEntry">
      <item command="CmdDataTree_MoveUp_ReversalSubentry" />
      <item command="CmdDataTree_MoveDown_ReversalSubentry" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Move_MoveReversalIndexEntry" />
      <item command="CmdDataTree_Promote_ProReversalIndexEntry" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Merge_Subentry" />
      <item command="CmdDataTree_Delete_Subentry" />
    </menu>
    <menu id="mnuDataTree_Environments_Insert">
      <item command="CmdDataTree_Insert_Slash" />
      <item command="CmdDataTree_Insert_Underscore" />
      <item command="CmdDataTree_Insert_NaturalClass" />
      <item command="CmdDataTree_Insert_OptionalItem" />
      <item command="CmdDataTree_Insert_HashMark" />
    </menu>
    <menu id="mnuDataTree_CmMedia">
      <item command="CmdDeleteMediaFile" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_VariantSpec">
      <item command="CmdDataTree_MoveUp_VariantSpec" />
      <item command="CmdDataTree_MoveDown_VariantSpec" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Insert_VariantSpec" />
      <item command="CmdDataTree_Delete_VariantSpec" />
    </menu>
    <menu id="mnuDataTree_ComplexFormSpec">
      <item command="CmdDataTree_Delete_ComplexFormSpec" />
    </menu>
    <menu id="PaneBar_ITextContent" label="">
      <item label="Add Words to Lexicon" boolProperty="ITexts_AddWordsToLexicon" defaultVisible="false" settingsGroup="local" />
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_interlinearEdit" defaultVisible="false" settingsGroup="local" />
    </menu>
    <menu id="mnuIText_FreeTrans">
      <item command="CmdDeleteFreeTrans" />
    </menu>
    <menu id="mnuIText_LitTrans">
      <item command="CmdDeleteLitTrans" />
    </menu>
    <menu id="mnuIText_Note">
      <item command="CmdDeleteNote" />
    </menu>
    <menu id="mnuIText_RawText">
      <item command="CmdCut" />
      <item command="CmdCopy" />
      <item command="CmdPaste" />
      <item label="-" translate="do not translate" />
      <item command="CmdLexiconLookup" />
      <item command="CmdWordformJumpToAnalyses" defaultVisible="false" />
      <item command="CmdWordformJumpToConcordance" defaultVisible="false" />
    </menu>
    <menu id="mnuFocusBox">
      <item command="CmdApproveAndMoveNext" />
      <item command="CmdApproveForWholeTextAndMoveNext" />
      <item command="CmdNextIncompleteBundle" />
      <item command="CmdApprove">Approve the suggested analysis and stay on this word</item>
      <menu id="ApproveAnalysisMovementMenu" label="_Approve suggestion and" defaultVisible="false">
        <item command="CmdApproveAndMoveNextSameLine" />
        <item command="CmdMoveFocusBoxRight" />
        <item command="CmdMoveFocusBoxLeft" />
      </menu>
      <menu id="BrowseMovementMenu" label="Leave _suggestion and" defaultVisible="false">
        <item command="CmdBrowseMoveNext" />
        <item command="CmdNextIncompleteBundleNc" />
        <item command="CmdBrowseMoveNextSameLine" />
        <item command="CmdMoveFocusBoxRightNc" />
        <item command="CmdMoveFocusBoxLeftNc" />
      </menu>
      <item command="CmdMakePhrase" defaultVisible="false" />
      <item command="CmdBreakPhrase" defaultVisible="false" />
      <item label="-" translate="do not translate" />
      <item command="CmdRepeatLastMoveLeft" defaultVisible="false" />
      <item command="CmdRepeatLastMoveRight" defaultVisible="false" />
      <item command="CmdApproveAll">Approve all the suggested analyses and stay on this word</item>
    </menu>
    <menu id="mnuDataTree_MainWordform">
      <item command="CmdShowWordformConc" />
      <item command="CmdRespeller" />
      <item command="CmdDataTree_Delete_MainWordform" />
    </menu>
    <menu id="mnuDataTree_WordformSpelling">
      <item command="CmdRespeller" />
    </menu>
    <menu id="mnuDataTree_MainWordform_Hotlinks">
      <item command="CmdShowWordformConc" />
    </menu>
    <menu id="mnuDataTree_HumanApprovedAnalysisSummary">
      <item command="CmdInsertHumanApprovedAnalysis" />
    </menu>
    <menu id="mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks">
      <item command="CmdInsertHumanApprovedAnalysis" />
    </menu>
    <menu id="mnuDataTree_HumanApprovedAnalysis">
      <item command="CmdShowHumanApprovedAnalysisConc" />
      <item command="CmdAnalysisJumpToConcordance" />
      <menu id="mnuDataTree_HumanApprovedStatus" label="User Opinion">
        <item command="CmdAnalysisApprove" />
        <item command="CmdAnalysisUnknown" />
        <item command="CmdAnalysisDisapprove" />
      </menu>
      <item command="CmdDataTree_Insert_WordGloss" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Delete_HumanApprovedAnalysis" />
    </menu>
    <menu id="mnuDataTree_HumanApprovedAnalysis_Hotlinks">
      <item command="CmdShowHumanApprovedAnalysisConc" />
    </menu>
    <menu id="mnuDataTree_ParserProducedAnalysis">
      <menu id="mnuDataTree_ParserProducedStatus" label="User Opinion">
        <item command="CmdAnalysisApprove" />
        <item command="CmdAnalysisUnknown" />
        <item command="CmdAnalysisDisapprove" />
      </menu>
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Delete_ParserProducedAnalysis" />
    </menu>
    <menu id="mnuDataTree_HumanDisapprovedAnalysis">
      <menu id="mnuDataTree_HumanDisapprovedStatus" label="User Opinion">
        <item command="CmdAnalysisApprove" />
        <item command="CmdAnalysisUnknown" />
        <item command="CmdAnalysisDisapprove" />
      </menu>
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Delete_HumanDisapprovedAnalysis" />
    </menu>
    <menu id="mnuDataTree_WordGlossForm">
      <item command="CmdShowWordGlossConc" />
      <item command="CmdWordGlossJumpToConcordance" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Merge_WordGloss" />
      <item command="CmdDataTree_Delete_WordGloss" />
    </menu>
    <menu id="mnuComplexConcordance">
      <item command="CmdPatternNodeOccurOnce" />
      <item command="CmdPatternNodeOccurZeroMore" />
      <item command="CmdPatternNodeOccurOneMore" />
      <item command="CmdPatternNodeSetOccur" />
      <item label="-" translate="do not translate" />
      <item command="CmdPatternNodeSetCriteria" />
      <item command="CmdPatternNodeGroup" />
    </menu>
    <menu id="PaneBar_RecordDetail" label="">
      <menu id="RecordPaneMenu" icon="MenuWidget">
        <item label="Insert _Subrecord" command="CmdDataTree_Insert_Subrecord" />
        <item label="Insert S_ubrecord of Subrecord" command="CmdDataTree_Insert_Subsubrecord" defaultVisible="false" />
        <item command="CmdDemoteRecord" />
      </menu>
      <item label="Show Hidden Fields" boolProperty="ShowHiddenFields_notebookEdit" defaultVisible="true" settingsGroup="local" />
    </menu>
    <menu id="mnuDataTree_Subrecord_Hotlinks">
      <!-- Observation blue links -->
      <item command="CmdDataTree_Insert_Subrecord" />
    </menu>
    <menu id="mnuDataTree_Participants">
      <item command="CmdDataTree_Delete_Participants" />
    </menu>
    <menu id="mnuDataTree_SubRecords">
      <item command="CmdDataTree_Insert_Subrecord" />
    </menu>
    <menu id="mnuDataTree_SubRecords_Hotlinks">
      <!-- Subrecords blue links -->
      <item command="CmdDataTree_Insert_Subrecord" />
    </menu>
    <menu id="mnuDataTree_SubRecordSummary">
      <!-- Observation dropdown context menu -->
      <item command="CmdDataTree_Insert_Subrecord" />
      <item command="CmdDataTree_Insert_Subsubrecord" />
      <item command="CmdMoveRecordUp" />
      <item command="CmdMoveRecordDown" />
      <item command="CmdPromoteSubrecord" />
      <item command="CmdDemoteSubrecord" />
    </menu>
    <menu id="mnuDataTree_InsertQuestion">
      <item command="CmdDataTree_Insert_Question" />
    </menu>
    <menu id="mnuDataTree_DeleteQuestion">
      <item command="CmdDataTree_Delete_Question" />
    </menu>
    <menu id="mnuDataTree_SubPossibilities">
      <item command="CmdDataTree_Insert_Possibility" />
    </menu>
    <menu id="mnuDataTree_SubSemanticDomain">
      <item command="CmdDataTree_Insert_SemanticDomain" />
    </menu>
    <menu id="mnuDataTree_SubCustomItem">
      <item command="CmdDataTree_Insert_CustomItem" />
    </menu>
    <menu id="mnuDataTree_SubAnnotationDefn">
      <item command="CmdDataTree_Insert_AnnotationDefn" />
    </menu>
    <menu id="mnuDataTree_SubMorphType">
      <item command="CmdDataTree_Insert_MorphType" />
    </menu>
    <menu id="mnuDataTree_SubComplexEntryType">
      <item command="CmdDataTree_Insert_LexEntryType" />
    </menu>
    <menu id="mnuDataTree_SubVariantEntryType">
      <item command="CmdDataTree_Insert_LexEntryType" />
    </menu>
    <menu id="mnuDataTree_SubAnthroCategory">
      <item command="CmdDataTree_Insert_AnthroCategory" />
    </menu>
    <menu id="mnuDataTree_DeletePossibility">
      <item command="CmdDataTree_Delete_Possibility" />
    </menu>
    <menu id="mnuDataTree_DeleteCustomItem">
      <item command="CmdDataTree_Delete_CustomItem" />
    </menu>
    <menu id="mnuDataTree_SubLocation">
      <item command="CmdDataTree_Insert_Location" />
    </menu>
    <menu id="mnuDataTree_MoveMainReversalPOS">
      <item command="CmdDataTree_Move_MoveReversalPOS" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Merge_MergeReversalPOS" />
      <item command="CmdDataTree_Delete_ReversalSubPOS" />
    </menu>
    <menu id="mnuDataTree_MoveReversalPOS">
      <item command="CmdDataTree_Move_MoveReversalPOS" />
      <item command="CmdDataTree_Promote_ProReversalSubPOS" />
      <item label="-" translate="do not translate" />
      <item command="CmdDataTree_Merge_MergeReversalPOS" />
      <item command="CmdDataTree_Delete_ReversalSubPOS" />
    </menu>
    <menu id="mnuDataTree_Help">
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Text">
      <item command="CmdJumpToText" />
    </menu>
    <menu id="mnuTextInfo_Notebook">
      <item command="CmdJumpToNotebook" />
    </menu>
    <menu id="mnuDataTree_Adhoc_Group_Members">
      <item command="CmdDataTree_Insert_Adhoc_Group_Members_Morpheme" />
      <item command="CmdDataTree_Insert_Adhoc_Group_Members_Allomorph" />
      <item command="CmdDataTree_Insert_Adhoc_Group_Members_Group" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Delete_Adhoc_Morpheme">
      <item command="CmdDataTree_Delete_Adhoc_Group_Members_Morpheme" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Delete_Adhoc_Allomorph">
      <item command="CmdDataTree_Delete_Adhoc_Group_Members_Allomorph" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Delete_Adhoc_Group">
      <item command="CmdDataTree_Delete_Adhoc_Group_Members_Group" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_CompoundRule_LinkerAffixAllomorph">
      <item command="CmdDataTree_Create_CompoundRule_LinkerAffixAllomorph" />
      <item command="CmdDataTree_Delete_CompoundRule_LinkerAffixAllomorph" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_FeatureStructure_Feature">
      <item command="CmdDataTree_Delete_FeatureStructure_Feature" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_ClosedFeature_Values">
      <item command="CmdDataTree_Insert_ClosedFeature_Value" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_ClosedFeature_Value">
      <item command="CmdDataTree_Delete_ClosedFeature_Value" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_FeatureStructure_Features">
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_POS_AffixSlots">
      <item command="CmdDataTree_Insert_POS_AffixSlot" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_POS_AffixSlot">
      <item command="CmdDataTree_Delete_POS_AffixSlot" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_POS_AffixTemplates">
      <item command="CmdDataTree_Insert_POS_AffixTemplate" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_POS_AffixTemplate">
      <item command="CmdDataTree_Delete_POS_AffixTemplate" />
      <item command="CmdDataTree_Copy_POS_AffixTemplate" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_POS_InflectionClass_Subclasses">
      <item command="CmdDataTree_Insert_POS_InflectionClass_Subclasses" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_POS_InflectionClasses">
      <item command="CmdDataTree_Insert_POS_InflectionClass" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_POS_InflectionClass">
      <item command="CmdDataTree_Delete_POS_InflectionClass" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_POS_StemNames">
      <item command="CmdDataTree_Insert_POS_StemName" />
    </menu>
    <menu id="mnuDataTree_POS_StemName">
      <item command="CmdDataTree_Delete_POS_StemName" />
    </menu>
    <menu id="mnuDataTree_MoStemName_Regions">
      <item command="CmdDataTree_Insert_MoStemName_Region" />
    </menu>
    <menu id="mnuDataTree_MoStemName_Region">
      <item command="CmdDataTree_Delete_MoStemName_Region" />
    </menu>
    <menu id="mnuDataTree_POS_SubPossibilities">
      <item command="CmdDataTree_Insert_POS_SubPossibilities" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Phoneme_Codes">
      <item command="CmdDataTree_Insert_Phoneme_Code" label="Insert Grapheme" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_Phoneme_Code">
      <item command="CmdDataTree_Delete_Phoneme_Code" />
      <!-- <item command="CmdDataTree_Help"/> -->
    </menu>
    <menu id="mnuDataTree_StringRepresentation_Insert">
      <item command="CmdDataTree_Insert_Env_Slash" />
      <item command="CmdDataTree_Insert_Env_Underscore" />
      <item command="CmdDataTree_Insert_Env_NaturalClass" />
      <item command="CmdDataTree_Insert_Env_OptionalItem" />
      <item command="CmdDataTree_Insert_Env_HashMark" />
    </menu>
    <menu id="mnuInflAffixTemplate_Help">
      <item command="CmdInflAffixTemplate_Help" />
    </menu>
    <menu id="mnuInflAffixTemplate_TemplateTable">
      <item command="CmdInflAffixTemplate_Add_InflAffixMsa" />
      <item command="CmdInflAffixTemplate_Insert_Slot_Before" />
      <item command="CmdInflAffixTemplate_Insert_Slot_After" />
      <item command="CmdInflAffixTemplate_Move_Slot_Left" />
      <item command="CmdInflAffixTemplate_Move_Slot_Right" />
      <item command="CmdInflAffixTemplate_Toggle_Slot_Optionality" />
      <item command="CmdInflAffixTemplate_Remove_Slot" />
      <item command="CmdInflAffixTemplate_Remove_InflAffixMsa" />
      <item command="CmdEntryJumpToDefault" />
      <item command="CmdInflAffixTemplate_Help" />
    </menu>
    <menu id="mnuPhRegularRule">
      <item command="CmdCtxtOccurOnce" />
      <item command="CmdCtxtOccurZeroMore" />
      <item command="CmdCtxtOccurOneMore" />
      <item command="CmdCtxtSetOccur" />
      <item label="-" translate="do not translate" />
      <item command="CmdCtxtSetFeatures" />
      <item label="-" translate="do not translate" />
      <item command="CmdCtxtJumpToNC" />
      <item command="CmdCtxtJumpToPhoneme" />
    </menu>
    <menu id="mnuPhMetathesisRule">
      <item command="CmdCtxtSetFeatures" />
      <item label="-" translate="do not translate" />
      <item command="CmdCtxtJumpToNC" />
      <item command="CmdCtxtJumpToPhoneme" />
    </menu>
    <menu id="mnuMoAffixProcess">
      <item command="CmdCtxtSetFeatures" />
      <item command="CmdMappingSetFeatures" />
      <item command="CmdMappingSetNC" />
      <item label="-" translate="do not translate" />
      <item command="CmdCtxtJumpToNC" />
      <item command="CmdCtxtJumpToPhoneme" />
      <item label="-" translate="do not translate" />
      <item command="CmdMappingJumpToNC" />
      <item command="CmdMappingJumpToPhoneme" />
    </menu>
  </contextMenus>
#endif
		#endregion context menus

		#region Others
		/// <summary>
		/// A family of merge commands, such as: CmdDataTree_Merge_Allomorph
		/// </summary>
		DataTreeMerge,
		CmdDataTree_Split_Sense,
		CmdCtxtSetFeatures,
		/// <summary>
		/// A family of sub possibility commands, such as: CmdDataTree_Insert_Possibility
		/// </summary>
		AddNewSubPossibilityListItem,
		SandboxJumpToTool,
		/// <summary>
		/// Used for these original commands:
		///		CmdWordformJumpToConcordance
		///		CmdAnalysisJumpToConcordance
		///		CmdMorphJumpToConcordance
		///		CmdEntryJumpToConcordance
		///		CmdSenseJumpToConcordance
		///		CmdLexGramInfoJumpToConcordance
		///		CmdWordGlossJumpToConcordance
		///		CmdWordPOSJumpToConcordance
		/// </summary>
		JumpToConcordance
		#endregion Others
	}
}