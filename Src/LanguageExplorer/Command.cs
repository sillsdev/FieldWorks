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
		// File menu
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

		// SendReceive menu
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

		// Edit menu
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

		// View menu
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

		// Data menu
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

		// Insert menu
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

		// Format menu
		CmdFormatStyle,
		CmdFormatApplyStyle,
		WritingSystemList,
		CmdVernacularWritingSystemProperties, // (Also on Tools menu)
		CmdAnalysisWritingSystemProperties, // (Also on Tools menu)

		// Tools menu
		Configure, // Menu that contains sub-menus.
			CmdConfigureDictionary,
			CmdConfigureInterlinear,
			CmdConfigureXmlDocView,
			CmdConfigureList,
			CmdConfigureColumns,
			CmdConfigHeadwordNumbers,
			CmdRestoreDefaults,
			// CmdVernacularWritingSystemProperties (Also on Format menu)
			// CmdAnalysisWritingSystemProperties (Also on Format menu)
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

		// Parser menu
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

		// Window menu
		CmdNewWindow,

		// Help menu
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
		// Format toolbar
		// WritingSystemList, Also on Format menu
		CombinedStylesList,
		// Insert toolbar
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

		// View ToolBar
		// CmdChooseTexts,
		CmdChangeFilterClearAll,

		// Standard ToolBar
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

		#region Others
		/// <summary>
		/// A family of merge commands, such as: CmdDataTree-Merge-Allomorph
		/// </summary>
		DataTreeMerge,
		CmdDataTree_Split_Sense,
		CmdCtxtSetFeatures,
		/// <summary>
		/// A family of sub possibility commands, such as: CmdDataTree-Insert-Possibility
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