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

#if RANDYTODO
		// TODO: Do I really want to go down this path and create them all at once, and just set handlers, visibility and enabled state, as is done for main menus and tool bar buttons?
#endif
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

		// menu id="mnuReorderVector">
		//CmdMoveTargetToPreviousInSequence,
		//CmdMoveTargetToNextInSequence,
		CmdAlphabeticalOrder,

		// <menu id="mnuBrowseHeader">
		SortedFromEnd, // bool property
		SortedByLength, // bool property

		// <menu id="PaneBar_ReversalIndicesMenu">
		ReversalIndexPaneMenu, // menu container
			CmdShowAllPublications,
			SelectedPublication, // Publications list
			// <item label="-" translate="do not translate" />
			CmdPublicationsJumpToDefault,
		//ReversalIndexPaneMenu, // menu container
			ReversalIndexPublicationLayout, // menu container: ReversalIndexList list
			// <item label="-" translate="do not translate" />
			//CmdConfigureDictionary,

		// <menu id="mnuEnvReferenceChoices">
		CmdJumpToEnvironmentList,
		CmdShowEnvironmentErrorMessage,
		// <item label="-" translate="do not translate" />
		CmdInsertEnvSlash,
		CmdInsertEnvUnderscore,
		CmdInsertEnvNaturalClass,
		CmdInsertEnvOptionalItem,
		CmdInsertEnvHashMark,

		// <menu id="mnuEnvChoices">
		//CmdShowEnvironmentErrorMessage,
		// <item label="-" translate="do not translate" />
		//CmdInsertEnvSlash,
		//CmdInsertEnvUnderscore,
		//CmdInsertEnvNaturalClass,
		//CmdInsertEnvOptionalItem,
		//CmdInsertEnvHashMark,

		// <menu id="mnuStTextChoices">
		//CmdCut,
		//CmdCopy,
		//CmdPaste,
		// <item label="-" translate="do not translate" />
		//CmdLexiconLookup,
		//CmdAddToLexicon,

		// <menu id="mnuDataTree_Object">
		mnuVisibility, // sub-menu
			CmdAlwaysVisible,
			CmdIfData,
			CmdNormallyHidden,
		CmdDataTree_Help,

		// <menu id="mnuDataTree_MultiStringSlice">
		// <item label="-" translate="do not translate" />
		//mnuVisibility, // sub-menu
			//CmdAlwaysVisible,
			//CmdIfData,
			//CmdNormallyHidden,
		DataTree_WritingSystemsMenu, // sub-menu
			CmdDataTree_WritingSystemMenu_ShowAllRightNow,
			SelectedWritingSystemHvosForCurrentContextMenu, // (property). list: WritingSystemOptionsForSlice
		//CmdDataTree_WritingSystemMenu_Configure,
		//CmdDataTree_Help,

		//<menu id="PaneBar_ShowHiddenFields_posEdit" label="">
		ShowHiddenFields_posEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_compoundRuleAdvancedEdit" label="">
		ShowHiddenFields_compoundRuleAdvancedEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_phonemeEdit" label="">
		ShowHiddenFields_phonemeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_naturalClassedit" label="">
		ShowHiddenFields_naturalClassedit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_EnvironmentEdit" label="">
		ShowHiddenFields_EnvironmentEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_PhonologicalRuleEdit" label="">
		ShowHiddenFields_PhonologicalRuleEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_AdhocCoprohibEdit" label="">
		ShowHiddenFields_AdhocCoprohibEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_phonologicalFeaturesAdvancedEdit" label="">
		ShowHiddenFields_phonologicalFeaturesAdvancedEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_featuresAdvancedEdit" label="">
		ShowHiddenFields_featuresAdvancedEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_ProdRestrictEdit" label="">
		ShowHiddenFields_ProdRestrictEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_lexiconProblems" label="">
		ShowHiddenFields_lexiconProblems, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_rapidDataEntry" label="">
		ShowHiddenFields_rapidDataEntry, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_domainTypeEdit" label="">
		ShowHiddenFields_domainTypeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_anthroEdit" label="">
		ShowHiddenFields_anthroEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_confidenceEdit" label="">
		ShowHiddenFields_confidenceEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_chartmarkEdit" label="">
		ShowHiddenFields_chartmarkEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_charttempEdit" label="">
		ShowHiddenFields_charttempEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_dialectsListEdit" label="">
		ShowHiddenFields_dialectsListEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_educationEdit" label="">
		ShowHiddenFields_educationEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_extNoteTypeEdit" label="">
		ShowHiddenFields_extNoteTypeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_roleEdit" label="">
		ShowHiddenFields_roleEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_genresEdit" label="">
		ShowHiddenFields_genresEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_featureTypesAdvancedEdit" label="">
		ShowHiddenFields_featureTypesAdvancedEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_languagesListEdit" label="">
		ShowHiddenFields_languagesListEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_lexRefEdit" label="">
		ShowHiddenFields_lexRefEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_locationsEdit" label="">
		ShowHiddenFields_locationsEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_publicationsEdit" label="">
		ShowHiddenFields_publicationsEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_complexEntryTypeEdit" label="">
		ShowHiddenFields_complexEntryTypeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_variantEntryTypeEdit" label="">
		ShowHiddenFields_variantEntryTypeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_morphTypeEdit" label="">
		ShowHiddenFields_morphTypeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_peopleEdit" label="">
		ShowHiddenFields_peopleEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_positionsEdit" label="">
		ShowHiddenFields_positionsEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_restrictionsEdit" label="">
		ShowHiddenFields_restrictionsEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_semanticDomainEdit" label="">
		ShowHiddenFields_semanticDomainEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_senseTypeEdit" label="">
		ShowHiddenFields_senseTypeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_statusEdit" label="">
		ShowHiddenFields_statusEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_translationTypeEdit" label="">
		ShowHiddenFields_translationTypeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_recTypeEdit" label="">
		ShowHiddenFields_recTypeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_scrNoteTypesEdit" label="">
		ShowHiddenFields_scrNoteTypesEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_timeOfDayEdit" label="">
		ShowHiddenFields_timeOfDayEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_weatherConditionEdit" label="">
		ShowHiddenFields_weatherConditionEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_textMarkupTagsEdit" label="">
		ShowHiddenFields_textMarkupTagsEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_usageTypeEdit" label="">
		ShowHiddenFields_usageTypeEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowHiddenFields_reversalToolReversalIndexPOS" label="">
		ShowHiddenFields_reversalToolReversalIndexPOS, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_WordformDetail" label="">
		ShowHiddenFields_WordsEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_LexicalDetail" label="">
		LexEntryPaneMenu, // sub-menu
			//Show_DictionaryPubPreview, // bool property: "Show Dictionary Preview"
			//<item label="-" translate="do not translate" />
			//CmdInsertSense,
			//CmdInsertSubsense,
			//CmdInsertVariant,
			//CmdDataTree_Insert_AlternateForm,
			//CmdDataTree_Insert_Pronunciation,
			//CmdInsertMediaFile,
			//CmdDataTree_Insert_Etymology,
			//<item label="-" translate="do not translate" />
			CmdChangeToComplexForm,
			CmdChangeToVariant,
			//CmdMergeEntry,
			//<item label="-" translate="do not translate" />
			CmdRootEntryJumpToConcordance,
		ShowHiddenFields_lexiconEdit, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_Dictionary" label="">
		//<!-- *** Put any alignment="right" menus in first. *** -->
		//LexEntryPaneMenu, // right menu
			DictionaryPublicationLayout, //<menu list="Configurations" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="DictionaryPublicationLayout" />
			//<item label="-" translate="do not translate" />
			//CmdConfigureDictionary,
		//LexEntryPaneMenu, // left
			//CmdShowAllPublications,
			//SelectedPublication, //<menu list="Publications" inline="true" emptyAllowed="true" behavior="singlePropertyAtomicValue" property="SelectedPublication" />
			//<item label="-" translate="do not translate" />
			//CmdPublicationsJumpToDefault,

		//<menu id="PaneBar_ReversalEntryDetail" label="">
		ShowHiddenFields_reversalToolEditComplete, // bool property: "Show Hidden Fields"

		//<menu id="PaneBar_ShowFailingItems_Classified" label="">
		ShowFailingItems_lexiconClassifiedDictionary, // bool property: "Show Unused Items"

		// <menu id="mnuDataTree_Help">
		// <!-- <item command="CmdDataTree_Help"/> -->

		// <menu id="mnuDataTree_LexemeForm">
		//CmdMorphJumpToConcordance,
		CmdDataTree_Swap_LexemeForm,
		CmdDataTree_Convert_LexemeForm_AffixProcess,
		CmdDataTree_Convert_LexemeForm_AffixAllomorph,

		// <menu id="mnuDataTree_LexemeFormContext">
		//CmdEntryJumpToConcordance,
		//CmdLexemeFormJumpToConcordance,
		//CmdDataTree_Swap_LexemeForm,

		// <menu id="mnuDataTree_CitationFormContext">
		//CmdEntryJumpToConcordance,

		// <menu id="mnuDataTree_Allomorphs">
		CmdDataTree_Insert_StemAllomorph,
		CmdDataTree_Insert_AffixAllomorph,

		// <menu id="mnuDataTree_AlternateForms">
		//CmdDataTree_Insert_AlternateForm,
		CmdDataTree_Insert_AffixProcess,

		// <menu id="mnuDataTree_Allomorphs_Hotlinks">
		//CmdDataTree_Insert_StemAllomorph,
		//CmdDataTree_Insert_AffixAllomorph,

		// <menu id="mnuDataTree_AlternateForms_Hotlinks">
		//CmdDataTree_Insert_AlternateForm,

		// <menu id="mnuDataTree_VariantForms">
		CmdDataTree_Insert_VariantForm,

		// <menu id="mnuDataTree_VariantForms_Hotlinks">
		//CmdDataTree_Insert_VariantForm,

		// <menu id="mnuDataTree_VariantForm">
		//CmdEntryJumpToDefault,
		//CmdEntryJumpToConcordance,
		CmdDataTree_Delete_VariantReference,
		CmdDataTree_Delete_Variant,

		// <menu id="mnuDataTree_VariantFormContext">
		//CmdEntryJumpToDefault,
		// <item label="-" translate="do not translate" />
		//CmdEntryJumpToConcordance,

		// <menu id="mnuDataTree_Allomorph">
		CmdDataTree_MoveUp_Allomorph,
		CmdDataTree_MoveDown_Allomorph,
		// <item label="-" translate="do not translate" />
		CmdDataTree_Merge_Allomorph,
		CmdDataTree_Delete_Allomorph,
		CmdDataTree_Swap_Allomorph,
		CmdDataTree_Convert_Allomorph_AffixProcess,
		// <item label="-" translate="do not translate" />
		//CmdMorphJumpToConcordance,

		// <menu id="mnuDataTree_AffixProcess">
		//CmdDataTree_MoveUp_Allomorph,
		//CmdDataTree_MoveDown_Allomorph,
		// <item label="-" translate="do not translate" />
		//CmdDataTree_Delete_Allomorph,
		//CmdDataTree_Swap_Allomorph,
		CmdDataTree_Convert_Allomorph_AffixAllomorph,
		// <item label="-" translate="do not translate" />
		//CmdMorphJumpToConcordance,

		// <menu id="mnuDataTree_Picture">
		CmdDataTree_Properties_Picture,
		// <item label="-" translate="do not translate" />
		CmdDataTree_MoveUp_Picture,
		CmdDataTree_MoveDown_Picture,
		// <item label="-" translate="do not translate" />
		CmdDataTree_Delete_Picture,

		// <menu id="mnuDataTree_AlternateForm">
		CmdDataTree_MoveUp_AlternateForm,
		CmdDataTree_MoveDown_AlternateForm,
		// <item label="-" translate="do not translate" />
		CmdDataTree_Merge_AlternateForm,
		CmdDataTree_Delete_AlternateForm,

		// <menu id="mnuDataTree_MSAs">
		CmdDataTree_Insert_DerivationAffixMsa,
		CmdDataTree_Insert_InflectionAffixMsa,
		CmdDataTree_Insert_UnclassifiedAffixMsa,
		CmdDataTree_Insert_StemMsa,
		// <!-- <item command="CmdDataTree_Help"/> -->

		// <menu id="mnuDataTree_MSA">
		//CmdDataTree_Help,
		CmdDataTree_Merge_MSA,
		CmdDataTree_Delete_MSA,

		// <menu id="mnuDataTree_Variants">
		CmdDataTree_Insert_Variant,
		// <!-- <item command="CmdDataTree_Help"/> -->

		// <menu id="mnuDataTree_Senses">
		CmdDataTree_Insert_SubSense,
		// <!-- <item command="CmdDataTree_Help"/> -->

		// <menu id="mnuDataTree_Sense">
		CmdDataTree_Insert_Example,
		CmdFindExampleSentence,
		CmdDataTree_Insert_ExtNote,
		CmdDataTree_Insert_SenseBelow,
		//CmdDataTree_Insert_SubSense,
		//CmdInsertPicture,
		// <item label="-" translate="do not translate" />
		//CmdSenseJumpToConcordance,
		// <item label="-" translate="do not translate" />
		CmdDataTree_MoveUp_Sense,
		CmdDataTree_MoveDown_Sense,
		CmdDataTree_MakeSub_Sense,
		CmdDataTree_Promote_Sense,
		// <item label="-" translate="do not translate" />
		CmdDataTree_Merge_Sense,
		//CmdDataTree_Split_Sense,
		CmdDataTree_Delete_Sense,

		// <menu id="mnuDataTree_Sense_Hotlinks">
		//CmdDataTree_Insert_Example,
		//CmdDataTree_Insert_SenseBelow,

		// <menu id="mnuDataTree_Examples">
		//CmdDataTree_Insert_Example,
		//CmdFindExampleSentence,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Example">
		CmdDataTree_Insert_Translation,
		CmdDataTree_Delete_Example,
		//<item label="-" translate="do not translate" />
		CmdDataTree_MoveUp_Example,
		CmdDataTree_MoveDown_Example,
		//<item label="-" translate="do not translate" />
		//CmdFindExampleSentence,

		//<menu id="mnuDataTree_Example_ForNotes">
		//CmdDataTree_Insert_Example,
		//CmdDataTree_Insert_Translation,
		//CmdDataTree_Delete_Example,
		//<item label="-" translate="do not translate" />
		//CmdDataTree_MoveUp_Example,
		//CmdDataTree_MoveDown_Example,
		//<item label="-" translate="do not translate" />
		//CmdFindExampleSentence,

		//<menu id="mnuDataTree_Translations">
		//CmdDataTree_Insert_Translation,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Translation">
		CmdDataTree_Delete_Translation,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_ExtendedNotes">
		//CmdDataTree_Insert_ExtNote,

		//<menu id="mnuDataTree_ExtendedNote">
		CmdDataTree_Delete_ExtNote,
		//<item label="-" translate="do not translate" />
		CmdDataTree_MoveUp_ExtNote,
		CmdDataTree_MoveDown_ExtNote,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Insert_ExampleInNote,

		//<menu id="mnuDataTree_ExtendedNote_Hotlinks">
		//CmdDataTree_Insert_ExtNote,

		//<menu id="mnuDataTree_ExtendedNote_Examples">
		//CmdDataTree_Insert_ExampleInNote,
		CmdDataTree_Delete_ExampleInNote,
		//<item label="-" translate="do not translate" />
		CmdDataTree_MoveUp_ExampleInNote,
		CmdDataTree_MoveDown_ExampleInNote,

		//<menu id="mnuDataTree_Pronunciation">
		//CmdDataTree_Insert_Pronunciation,
		//CmdInsertMediaFile,
		//<item label="-" translate="do not translate" />
		CmdDataTree_MoveUp_Pronunciation,
		CmdDataTree_MoveDown_Pronunciation,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Delete_Pronunciation,
		//<item label="-" translate="do not translate" />
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_DeletePronunciation">
		//CmdDataTree_Delete_Pronunciation,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_SubEntryLink">
		CmdDataTree_Insert_SubEntryLink,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Subsenses">
		//CmdDataTree_Insert_SubSense,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Etymology">
		//CmdDataTree_Insert_Etymology,
		//<item label="-" translate="do not translate" />
		CmdDataTree_MoveUp_Etymology,
		CmdDataTree_MoveDown_Etymology,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Delete_Etymology,

		//<menu id="mnuDataTree_DeleteEtymology">
		//CmdDataTree_Delete_Etymology,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Etymology_Hotlinks">
		//CmdDataTree_Insert_Etymology,

		//<menu id="mnuDataTree_DeleteAddLexReference">
		CmdDataTree_Delete_LexReference,
		CmdDataTree_Add_ToLexReference,
		CmdDataTree_EditDetails_LexReference,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_DeleteReplaceLexReference">
		//CmdDataTree_Delete_LexReference,
		CmdDataTree_Replace_LexReference,
		//CmdDataTree_EditDetails_LexReference,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_InsertReversalSubentry">
		CmdDataTree_Insert_ReversalSubentry,

		//<menu id="mnuDataTree_InsertReversalSubentry_Hotlinks">
		//CmdDataTree_Insert_ReversalSubentry,

		//<menu id="mnuDataTree_MoveReversalIndexEntry">
		CmdDataTree_MoveUp_ReversalSubentry,
		CmdDataTree_MoveDown_ReversalSubentry,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Move_MoveReversalIndexEntry,
		CmdDataTree_Promote_ProReversalIndexEntry,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Merge_Subentry,
		CmdDataTree_Delete_Subentry,

		//<menu id="mnuDataTree_Environments_Insert">
		CmdDataTree_Insert_Slash,
		CmdDataTree_Insert_Underscore,
		CmdDataTree_Insert_NaturalClass,
		CmdDataTree_Insert_OptionalItem,
		CmdDataTree_Insert_HashMark,

		//<menu id="mnuDataTree_CmMedia">
		CmdDeleteMediaFile,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_VariantSpec">
		CmdDataTree_MoveUp_VariantSpec,
		CmdDataTree_MoveDown_VariantSpec,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Insert_VariantSpec,
		CmdDataTree_Delete_VariantSpec,

		//<menu id="mnuDataTree_ComplexFormSpec">
		CmdDataTree_Delete_ComplexFormSpec,


		//<menu id="PaneBar_ITextContent" label="">
		//ITexts_AddWordsToLexicon, // bool property
		ShowHiddenFields_interlinearEdit, // bool property: "Show Hidden Fields"

		//<menu id="mnuIText_FreeTrans">
		CmdDeleteFreeTrans,

		//<menu id="mnuIText_LitTrans">
		CmdDeleteLitTrans,

		//<menu id="mnuIText_Note">
		CmdDeleteNote,

		//<menu id="mnuIText_RawText">
		//CmdCut,
		//CmdCopy,
		//CmdPaste,
		//<item label="-" translate="do not translate" />
		//CmdLexiconLookup,
		//CmdWordformJumpToAnalyses,
		//CmdWordformJumpToConcordance,

		//<menu id="mnuFocusBox">
		//CmdApproveAndMoveNext,
		//CmdApproveForWholeTextAndMoveNext,
		//CmdNextIncompleteBundle,
		//CmdApprove,
		//ApproveAnalysisMovementMenu,
			//CmdApproveAndMoveNextSameLine,
			//CmdMoveFocusBoxRight,
			//CmdMoveFocusBoxLeft,
		//BrowseMovementMenu,
			//CmdBrowseMoveNext,
			//CmdNextIncompleteBundleNc,
			//CmdBrowseMoveNextSameLine,
			//CmdMoveFocusBoxRightNc,
			//CmdMoveFocusBoxLeftNc,
		//CmdMakePhrase,
		//CmdBreakPhrase,
		//<item label="-" translate="do not translate" />
		//CmdRepeatLastMoveLeft,
		//CmdRepeatLastMoveRight,
		//CmdApproveAll,

		//<menu id="mnuDataTree_MainWordform">
		CmdShowWordformConc,
		CmdRespeller,
		CmdDataTree_Delete_MainWordform,

		//<menu id="mnuDataTree_WordformSpelling">
		//CmdRespeller,

		//<menu id="mnuDataTree_MainWordform_Hotlinks">
		//CmdShowWordformConc,

		//<menu id="mnuDataTree_HumanApprovedAnalysisSummary">
		//CmdInsertHumanApprovedAnalysis,

		//<menu id="mnuDataTree_HumanApprovedAnalysisSummary_Hotlinks">
		//CmdInsertHumanApprovedAnalysis,

		//<menu id="mnuDataTree_HumanApprovedAnalysis">
		CmdShowHumanApprovedAnalysisConc,
		//CmdAnalysisJumpToConcordance,
		mnuDataTree_HumanApprovedStatus, // sub-menu
			CmdAnalysisApprove,
			CmdAnalysisUnknown,
			CmdAnalysisDisapprove,
		CmdDataTree_Insert_WordGloss,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Delete_HumanApprovedAnalysis,

		//<menu id="mnuDataTree_HumanApprovedAnalysis_Hotlinks">
		//CmdShowHumanApprovedAnalysisConc,

		//<menu id="mnuDataTree_ParserProducedAnalysis">
		mnuDataTree_ParserProducedStatus, // sub-menu
			//CmdAnalysisApprove,
			//CmdAnalysisUnknown,
			//CmdAnalysisDisapprove,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Delete_ParserProducedAnalysis,

		//<menu id="mnuDataTree_HumanDisapprovedAnalysis">
		mnuDataTree_HumanDisapprovedStatus, // sub-menu
			//CmdAnalysisApprove,
			//CmdAnalysisUnknown,
			//CmdAnalysisDisapprove,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Delete_HumanDisapprovedAnalysis,

		//<menu id="mnuDataTree_WordGlossForm">
		CmdShowWordGlossConc,
		//CmdWordGlossJumpToConcordance,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Merge_WordGloss,
		CmdDataTree_Delete_WordGloss,

		//<menu id="mnuComplexConcordance">
		CmdPatternNodeOccurOnce,
		CmdPatternNodeOccurZeroMore,
		CmdPatternNodeOccurOneMore,
		CmdPatternNodeSetOccur,
		//<item label="-" translate="do not translate" />
		CmdPatternNodeSetCriteria,
		CmdPatternNodeGroup,

		//<menu id="PaneBar_RecordDetail" label="">
		RecordPaneMenu, // sub-menu
			CmdDataTree_Insert_Subrecord,
			CmdDataTree_Insert_Subsubrecord,
			CmdDemoteRecord,
		ShowHiddenFields_notebookEdit, // bool property: label="Show Hidden Fields"

		//<menu id="mnuDataTree_Subrecord_Hotlinks">
		//<!-- Observation blue links -->
		//CmdDataTree_Insert_Subrecord,

		//<menu id="mnuDataTree_Participants">
		CmdDataTree_Delete_Participants,

		//<menu id="mnuDataTree_SubRecords">
		//CmdDataTree_Insert_Subrecord,

		//<menu id="mnuDataTree_SubRecords_Hotlinks">
		//<!-- Subrecords blue links -->
		//CmdDataTree_Insert_Subrecord,

		//<menu id="mnuDataTree_SubRecordSummary">
		//<!-- Observation dropdown context menu -->
		//CmdDataTree_Insert_Subrecord,
		//CmdDataTree_Insert_Subsubrecord,
		CmdMoveRecordUp,
		CmdMoveRecordDown,
		CmdPromoteSubrecord,
		CmdDemoteSubrecord,

		//<menu id="mnuDataTree_InsertQuestion">
		CmdDataTree_Insert_Question,

		//<menu id="mnuDataTree_DeleteQuestion">
		CmdDataTree_Delete_Question,

		//<menu id="mnuDataTree_SubPossibilities">
		//CmdDataTree_Insert_Possibility,

		//<menu id="mnuDataTree_SubSemanticDomain">
		//CmdDataTree_Insert_SemanticDomain,

		//<menu id="mnuDataTree_SubCustomItem">
		//CmdDataTree_Insert_CustomItem,

		//<menu id="mnuDataTree_SubAnnotationDefn">
		CmdDataTree_Insert_AnnotationDefn,

		//<menu id="mnuDataTree_SubMorphType">
		CmdDataTree_Insert_MorphType,

		//<menu id="mnuDataTree_SubComplexEntryType">
		//CmdDataTree_Insert_LexEntryType,

		//<menu id="mnuDataTree_SubVariantEntryType">
		//CmdDataTree_Insert_LexEntryType,

		//<menu id="mnuDataTree_SubAnthroCategory">
		//CmdDataTree_Insert_AnthroCategory,

		//<menu id="mnuDataTree_DeletePossibility">
		CmdDataTree_Delete_Possibility,

		//<menu id="mnuDataTree_DeleteCustomItem">
		CmdDataTree_Delete_CustomItem,

		//<menu id="mnuDataTree_SubLocation">
		//CmdDataTree_Insert_Location,

		//<menu id="mnuDataTree_MoveMainReversalPOS">
		CmdDataTree_Move_MoveReversalPOS,
		//<item label="-" translate="do not translate" />
		CmdDataTree_Merge_MergeReversalPOS,
		CmdDataTree_Delete_ReversalSubPOS,

		//<menu id="mnuDataTree_MoveReversalPOS">
		//CmdDataTree_Move_MoveReversalPOS,
		CmdDataTree_Promote_ProReversalSubPOS,
		//<item label="-" translate="do not translate" />
		//CmdDataTree_Merge_MergeReversalPOS,
		//CmdDataTree_Delete_ReversalSubPOS,

		//<menu id="mnuDataTree_Help">
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Text">
		CmdJumpToText,

		//<menu id="mnuTextInfo_Notebook">
		CmdJumpToNotebook,

		//<menu id="mnuDataTree_Adhoc_Group_Members">
		CmdDataTree_Insert_Adhoc_Group_Members_Morpheme,
		CmdDataTree_Insert_Adhoc_Group_Members_Allomorph,
		CmdDataTree_Insert_Adhoc_Group_Members_Group,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Delete_Adhoc_Morpheme">
		CmdDataTree_Delete_Adhoc_Group_Members_Morpheme,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Delete_Adhoc_Allomorph">
		CmdDataTree_Delete_Adhoc_Group_Members_Allomorph,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Delete_Adhoc_Group">
		CmdDataTree_Delete_Adhoc_Group_Members_Group,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_CompoundRule_LinkerAffixAllomorph">
		CmdDataTree_Create_CompoundRule_LinkerAffixAllomorph,
		CmdDataTree_Delete_CompoundRule_LinkerAffixAllomorph,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_FeatureStructure_Feature">
		CmdDataTree_Delete_FeatureStructure_Feature,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_ClosedFeature_Values">
		//CmdDataTree_Insert_ClosedFeature_Value,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_ClosedFeature_Value">
		CmdDataTree_Delete_ClosedFeature_Value,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_FeatureStructure_Features">
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_POS_AffixSlots">
		//CmdDataTree_Insert_POS_AffixSlot,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_POS_AffixSlot">
		CmdDataTree_Delete_POS_AffixSlot,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_POS_AffixTemplates">
		//CmdDataTree_Insert_POS_AffixTemplate,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_POS_AffixTemplate">
		CmdDataTree_Delete_POS_AffixTemplate,
		CmdDataTree_Copy_POS_AffixTemplate,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_POS_InflectionClass_Subclasses">
		CmdDataTree_Insert_POS_InflectionClass_Subclasses,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_POS_InflectionClasses">
		//CmdDataTree_Insert_POS_InflectionClass,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_POS_InflectionClass">
		CmdDataTree_Delete_POS_InflectionClass,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_POS_StemNames">
		CmdDataTree_Insert_POS_StemName,

		//<menu id="mnuDataTree_POS_StemName">
		CmdDataTree_Delete_POS_StemName,

		//<menu id="mnuDataTree_MoStemName_Regions">
		CmdDataTree_Insert_MoStemName_Region,

		//<menu id="mnuDataTree_MoStemName_Region">
		CmdDataTree_Delete_MoStemName_Region,

		//<menu id="mnuDataTree_POS_SubPossibilities">
		//CmdDataTree_Insert_POS_SubPossibilities,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Phoneme_Codes">
		//CmdDataTree_Insert_Phoneme_Code,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_Phoneme_Code">
		CmdDataTree_Delete_Phoneme_Code,
		//<!-- <item command="CmdDataTree_Help"/> -->

		//<menu id="mnuDataTree_StringRepresentation_Insert">
		CmdDataTree_Insert_Env_Slash,
		CmdDataTree_Insert_Env_Underscore,
		CmdDataTree_Insert_Env_NaturalClass,
		CmdDataTree_Insert_Env_OptionalItem,
		CmdDataTree_Insert_Env_HashMark,

		//<menu id="mnuInflAffixTemplate_Help">
		CmdInflAffixTemplate_Help,

		//<menu id="mnuInflAffixTemplate_TemplateTable">
		CmdInflAffixTemplate_Add_InflAffixMsa,
		CmdInflAffixTemplate_Insert_Slot_Before,
		CmdInflAffixTemplate_Insert_Slot_After,
		CmdInflAffixTemplate_Move_Slot_Left,
		CmdInflAffixTemplate_Move_Slot_Right,
		CmdInflAffixTemplate_Toggle_Slot_Optionality,
		CmdInflAffixTemplate_Remove_Slot,
		CmdInflAffixTemplate_Remove_InflAffixMsa,
		//CmdEntryJumpToDefault,
		//CmdInflAffixTemplate_Help,

		//<menu id="mnuPhRegularRule">
		CmdCtxtOccurOnce,
		CmdCtxtOccurZeroMore,
		CmdCtxtOccurOneMore,
		CmdCtxtSetOccur,
		//<item label="-" translate="do not translate" />
		//CmdCtxtSetFeatures,
		//<item label="-" translate="do not translate" />
		CmdCtxtJumpToNC,
		CmdCtxtJumpToPhoneme,

		// <menu id="mnuPhMetathesisRule">
		//CmdCtxtSetFeatures,
		//<item label="-" translate="do not translate" />
		//CmdCtxtJumpToNC,
		//CmdCtxtJumpToPhoneme,

		//<menu id="mnuMoAffixProcess">
		//CmdCtxtSetFeatures,
		CmdMappingSetFeatures,
		CmdMappingSetNC,
		//<item label="-" translate="do not translate" />
		//CmdCtxtJumpToNC,
		//CmdCtxtJumpToPhoneme,
		//<item label="-" translate="do not translate" />
		CmdMappingJumpToNC,
		CmdMappingJumpToPhoneme,

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