// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer
{
	/// <summary>
	/// Warehouse for LanguageExplorer constants.
	/// </summary>
	internal static class LanguageExplorerConstants
	{
		#region Statusbar

		/// <summary>Reset on area/tool change. string.Empty</summary>
		/// <remarks>
		/// Only used by RecordList's ResetStatusBarMessageForCurrentObject method
		/// </remarks>
		internal const string StatusBarPanelMessage = "statusBarPanelMessage";
		/// <summary />
		/// <remarks>
		/// Used only by ParserMenuManager
		/// </remarks>
		internal const string StatusBarPanelProgress = "statusBarPanelProgress";
		/// <summary />
		internal const string StatusBarPanelProgressBar = "statusBarPanelProgressBar";
		/// <summary>Reset on area/tool change. string.Empty</summary>
		/// <remarks>
		/// Only used by RecordList's UpdateSortStatusBarPanel method
		/// </remarks>
		internal const string StatusBarPanelSort = "statusBarPanelSort";
		/// <summary>Reset on area/tool change. string.Empty</summary>
		/// <remarks>
		/// Only used by RecordList's UpdateFilterStatusBarPanel method
		/// </remarks>
		internal const string StatusBarPanelFilter = "statusBarPanelFilter";
		/// <summary>Reset on area/tool change. StringTable.Table.GetString("No Records", "Misc");</summary>
		/// <remarks>
		/// Only used by RecordList's UpdateStatusBarRecordNumber method
		/// </remarks>
		internal const string StatusBarPanelRecordNumber = "statusBarPanelRecordNumber";

		#endregion Statusbar

		#region General area/tool
		internal const string AreaChoice = "areaChoice";
		internal const string ToolChoice = "toolChoice";
		internal const string ToolForAreaNamed_ = "ToolForAreaNamed_";
		internal const string InitialArea = "InitialArea";
		internal const string InitialAreaMachineName = LanguageExplorerConstants.LexiconAreaMachineName;
		#endregion General area/tool


		#region Lexicon area
		internal const string LexiconAreaMachineName = "lexicon";
		internal const string LexiconAreaUiName = "Lexical Tools";
		internal const string LexiconAreaDefaultToolMachineName = LexiconEditMachineName;
		internal const string LexiconEditMachineName = "lexiconEdit";
		internal const string LexiconEditUiName = "Lexicon Edit";
		internal const string LexiconBrowseMachineName = "lexiconBrowse";
		internal const string LexiconBrowseUiName = "Browse";
		internal const string LexiconDictionaryMachineName = "lexiconDictionary";
		internal const string LexiconDictionaryUiName = "Dictionary";
		internal const string RapidDataEntryMachineName = "rapidDataEntry";
		internal const string RapidDataEntryUiName = "Collect Words";
		internal const string LexiconClassifiedDictionaryMachineName = "lexiconClassifiedDictionary";
		internal const string LexiconClassifiedDictionaryUiName = "Classified Dictionary";
		internal const string BulkEditEntriesOrSensesMachineName = "bulkEditEntriesOrSenses";
		internal const string BulkEditEntriesOrSensesUiName = "CRASHES (5002 vs 5035): Bulk Edit Entries";
		internal const string ReversalEditCompleteMachineName = "reversalEditComplete";
		internal const string ReversalEditCompleteUiName = "CRASHES (XhtmlDocView): Reversal Indexes";
		internal const string ReversalBulkEditReversalEntriesMachineName = "reversalBulkEditReversalEntries";
		internal const string ReversalBulkEditReversalEntriesUiName = "Bulk Edit Reversal Entries";
		#endregion Lexicon area

		#region Text and Words area
		internal const string TextAndWordsAreaMachineName = "textsWords";
		internal const string TextAndWordsAreaUiName = "Texts & Words";
		internal const string TextAndWordsAreaDefaultToolMachineName = InterlinearEditMachineName;
		internal const string InterlinearEditMachineName = "interlinearEdit";
		internal const string InterlinearEditUiName = "Interlinear Texts";
		internal const string ConcordanceMachineName = "concordance";
		internal const string ConcordanceUiName = "Concordance";
		internal const string ComplexConcordanceMachineName = "complexConcordance";
		internal const string ComplexConcordanceUiName = "Complex Concordance";
		internal const string WordListConcordanceMachineName = "wordListConcordance";
		internal const string WordListConcordanceUiName = "Word List Concordance";
		internal const string AnalysesMachineName = "Analyses";
		internal const string AnalysesUiName = "Word Analyses";
		internal const string BulkEditWordformsMachineName = "bulkEditWordforms";
		internal const string BulkEditWordformsUiName = "Bulk Edit Wordforms";
		internal const string CorpusStatisticsMachineName = "corpusStatistics";
		internal const string CorpusStatisticsUiName = "Statistics";

		internal const string ITexts_AddWordsToLexicon = "ITexts_AddWordsToLexicon";
		internal const string TextSelectedWord = "TextSelectedWord";
		internal const string InterlinearTexts = "interlinearTexts";
		internal const string ShowHiddenFields_interlinearEdit = "ShowHiddenFields_interlinearEdit";
		internal const string InterlinearTab = "InterlinearTab";
		internal const string InterlinearTextsRecordList = "InterlinearTextsRecordList";
		internal const string ConcordanceWords = "concordanceWords";
		internal const string OccurrencesOfSelectedUnit = "OccurrencesOfSelectedUnit";
		internal const string ComplexConcOccurrencesOfSelectedUnit = "complexConcOccurrencesOfSelectedUnit";
		#endregion Text and Words area

		#region Grammar area
		internal const string GrammarAreaMachineName = "grammar";
		internal const string GrammarAreaUiName = "Grammar";
		internal const string GrammarAreaDefaultToolMachineName = PosEditMachineName;
		internal const string PosEditMachineName = "posEdit";
		internal const string PosEditUiName = "Category Edit";
		internal const string CategoryBrowseMachineName = "categoryBrowse";
		internal const string CategoryBrowseUiName = "Categories Browse";
		internal const string CompoundRuleAdvancedEditMachineName = "compoundRuleAdvancedEdit";
		internal const string CompoundRuleAdvancedEditUiName = "Compound Rules";
		internal const string PhonemeEditMachineName = "phonemeEdit";
		internal const string PhonemeEditUiName = "Phonemes";
		internal const string PhonologicalFeaturesAdvancedEditMachineName = "phonologicalFeaturesAdvancedEdit";
		internal const string PhonologicalFeaturesAdvancedEditUiName = "Phonological Features";
		internal const string BulkEditPhonemesMachineName = "bulkEditPhonemes";
		internal const string BulkEditPhonemesUiName = "Bulk Edit Phoneme Features";
		internal const string NaturalClassEditMachineName = "naturalClassEdit";
		internal const string NaturalClassEditUiName = "Natural Classes";
		internal const string EnvironmentEditMachineName = "EnvironmentEdit";
		internal const string EnvironmentEditUiName = "Environments";
		internal const string PhonologicalRuleEditMachineName = "PhonologicalRuleEdit";
		internal const string PhonologicalRuleEditUiName = "Phonological Rules";
		internal const string AdhocCoprohibitionRuleEditMachineName = "AdhocCoprohibitionRuleEdit";
		internal const string AdhocCoprohibitionRuleEditUiName = "Ad hoc Rules";
		internal const string FeaturesAdvancedEditMachineName = "featuresAdvancedEdit";
		internal const string FeaturesAdvancedEditUiName = "Inflection Features";
		internal const string ProdRestrictEditMachineName = "ProdRestrictEdit";
		internal const string ProdRestrictEditUiName = "Exception \"Features\"";
		internal const string GrammarSketchMachineName = "grammarSketch";
		internal const string GrammarSketchUiName = "Grammar Sketch";
		internal const string LexiconProblemsMachineName = "lexiconProblems";
		internal const string LexiconProblemsUiName = "Problems";
		#endregion Grammar area

		#region Notebook area
		internal const string NotebookAreaMachineName = "notebook";
		internal const string NotebookAreaUiName = "Notebook";
		internal const string NotebookAreaDefaultToolMachineName = NotebookEditToolMachineName;
		internal const string NotebookEditToolMachineName = "notebookEdit";
		internal const string NotebookEditToolUiName = "Record Edit";
		internal const string NotebookBrowseToolMachineName = "notebookBrowse";
		internal const string NotebookBrowseToolUiName = "Browse";
		internal const string NotebookDocumentToolMachineName = "notebookDocument";
		internal const string NotebookDocumentToolUiName = "Document";
		#endregion Notebook area

		#region Lists area
		internal const string ListsAreaMachineName = "lists";
		internal const string ListsAreaUiName = "Lists";
		internal const string ListsAreaDefaultToolMachineName = DomainTypeEditMachineName;
		internal const string DomainTypeEditMachineName = "domainTypeEdit";
		internal const string DomainTypeEditUiName = "Academic Domains";
		internal const string AnthroEditMachineName = "anthroEdit";
		internal const string AnthroEditUiName = "Anthropology Categories";
		internal const string ComplexEntryTypeEditMachineName = "complexEntryTypeEdit";
		internal const string ComplexEntryTypeEditUiName = "Complex Form Types";
		internal const string ConfidenceEditMachineName = "confidenceEdit";
		internal const string ConfidenceEditUiName = "Confidence Levels";
		internal const string DialectsListEditMachineName = "dialectsListEdit";
		internal const string DialectsListEditUiName = "Dialect Labels";
		internal const string ChartmarkEditMachineName = "chartmarkEdit";
		internal const string ChartmarkEditUiName = "Text Chart Markers";
		internal const string CharttempEditMachineName = "charttempEdit";
		internal const string CharttempEditUiName = "Text Constituent Chart Templates";
		internal const string EducationEditMachineName = "educationEdit";
		internal const string EducationEditUiName = "Education Levels";
		internal const string RoleEditMachineName = "roleEdit";
		internal const string RoleEditUiName = "Roles";
		internal const string ExtNoteTypeEditMachineName = "extNoteTypeEdit";
		internal const string ExtNoteTypeEditUiName = "Extended Note Types";
		internal const string FeatureTypesAdvancedEditMachineName = "featureTypesAdvancedEdit";
		internal const string FeatureTypesAdvancedEditUiName = "Feature Types";
		internal const string GenresEditMachineName = "genresEdit";
		internal const string GenresEditUiName = "Genres";
		internal const string LanguagesListEditMachineName = "languagesListEdit";
		internal const string LanguagesListEditUiName = "Languages";
		internal const string LexRefEditMachineName = "lexRefEdit";
		internal const string LexRefEditUiName = "Lexical Relations";
		internal const string LocationsEditMachineName = "locationsEdit";
		internal const string LocationsEditUiName = "Locations";
		internal const string PublicationsEditMachineName = "publicationsEdit";
		internal const string PublicationsEditUiName = "Publications";
		internal const string MorphTypeEditMachineName = "morphTypeEdit";
		internal const string MorphTypeEditUiName = "Morpheme Types";
		internal const string PeopleEditMachineName = "peopleEdit";
		internal const string PeopleEditUiName = "People";
		internal const string PositionsEditMachineName = "positionsEdit";
		internal const string PositionsEditUiName = "Positions";
		internal const string RestrictionsEditMachineName = "restrictionsEdit";
		internal const string RestrictionsEditUiName = "Restrictions";
		internal const string SemanticDomainEditMachineName = "semanticDomainEdit";
		internal const string SemanticDomainEditUiName = "Semantic Domains";
		internal const string SenseTypeEditMachineName = "senseTypeEdit";
		internal const string SenseTypeEditUiName = "Sense Types";
		internal const string StatusEditMachineName = "statusEdit";
		internal const string StatusEditUiName = "Status";
		internal const string TextMarkupTagsEditMachineName = "textMarkupTagsEdit";
		internal const string TextMarkupTagsEditUiName = "Text Markup Tags";
		internal const string TranslationTypeEditMachineName = "translationTypeEdit";
		internal const string TranslationTypeEditUiName = "Translation Types";
		internal const string UsageTypeEditMachineName = "usageTypeEdit";
		internal const string UsageTypeEditUiName = "Usages";
		internal const string VariantEntryTypeEditMachineName = "variantEntryTypeEdit";
		internal const string VariantEntryTypeEditUiName = "Variant Types";
		internal const string RecTypeEditMachineName = "recTypeEdit";
		internal const string RecTypeEditUiName = "Notebook Record Types";
		internal const string TimeOfDayEditMachineName = "timeOfDayEdit";
		internal const string TimeOfDayEditUiName = "Time Of Day";
		internal const string ReversalToolReversalIndexPOSMachineName = "reversalToolReversalIndexPOS";
		internal const string ReversalToolReversalIndexPOSUiName = "Reversal Index Categories";
		#endregion Lists area


		#region Misc
		internal const string PartOfSpeechGramInfo = "PartOfSpeechGramInfo";
		internal const string WordPartOfSpeech = "WordPartOfSpeech";
		internal const string RecordListOwningObjChanged = "RecordListOwningObjChanged";
		internal const string InterestingTexts = "InterestingTexts";

		/// <summary>
		/// File extension for dictionary configuration files.
		/// </summary>
		internal const string DictionaryConfigurationFileExtension = ".fwdictconfig";
		/// <summary />
		internal const string AllReversalIndexes = "All Reversal Indexes";
		/// <summary>
		/// Filename (without extension) of the reversal index configuration file
		/// for "all reversal indexes".
		/// </summary>
		internal const string AllReversalIndexesFilenameBase = "AllReversalIndexes";
		internal const string RevIndexDir = "ReversalIndex";
		internal const string App = "App";
		internal const string HomographConfiguration = "HomographConfiguration";
		internal const string HelpTopicProvider = "HelpTopicProvider";
		internal const string PersistAsXmlFactory = "PersistAsXmlFactory";
		internal const string CmObjectUiFactory = "CmObjectUiFactory";
		internal const string LinkHandler = "LinkHandler";
		internal const string MajorFlexComponentParameters = "MajorFlexComponentParameters";
		internal const string RecordListRepository = "RecordListRepository";
		internal const string windowSize = "windowSize";
		internal const string windowLocation = "windowLocation";
		internal const string windowState = "windowState";
		internal const string UseVernSpellingDictionary = "UseVernSpellingDictionary";
		internal const string PropertyTableVersion = "PropertyTableVersion";
		internal const string RecordListWidthGlobal = "RecordListWidthGlobal";
		internal const string SuspendLoadingRecordUntilOnJumpToRecord = "SuspendLoadingRecordUntilOnJumpToRecord";
		internal const string SliceSplitterBaseDistance = "SliceSplitterBaseDistance";
		internal const string SelectedPublication = "SelectedPublication";
		internal const string ShowHiddenFields = "ShowHiddenFields";
		internal const string ShowFailingItems = "ShowFailingItems";
		internal const string Entries = "entries";
		internal const string AllReversalEntries = "AllReversalEntries";
		internal const string SelectedListBarNode = "SelectedListBarNode";
		internal const string SelectedTreeBarNode = "SelectedTreeBarNode";
		internal const string SetToolFromName = "SetToolFromName";
		internal const string JumpToRecord = "JumpToRecord";
		internal const string LinkFollowed = "LinkFollowed";
		internal const string ReversalIndexGuid = "ReversalIndexGuid";
		internal const string SuspendLoadListUntilOnChangeFilter = "SuspendLoadListUntilOnChangeFilter";
		internal const string DataTree = "DataTree";
		internal const string ActiveListSelectedObject = "ActiveListSelectedObject";
		internal const string StopParser = "StopParser";
		internal const string ConsideringClosing = "ConsideringClosing";
		internal const string DelayedRefreshList = "DelayedRefreshList";
		internal const string ItemDataModified = "ItemDataModified";
		internal const string DictionaryType = "Dictionary";
		internal const string ReversalType = "Reversal Index";
		internal const string ReloadAreaTools = "ReloadAreaTools";
		internal const string ReversalEntriesPOS = "ReversalEntriesPOS";
		internal const string MasterRefresh = "MasterRefresh";
		internal const string kChorusNotesExtension = ".ChorusNotes";
		internal const string FLExBridge = "FLExBridge";
		internal const string LiftBridge = "LiftBridge";
		internal const string LastBridgeUsed = "LastBridgeUsed";
		internal static string SendReceiveUser => Environment.UserName;
		public const string FakeLexiconFileName = "Lexicon.fwstub";
		public const string FlexLexiconNotesFileName = FakeLexiconFileName + kChorusNotesExtension;

		#endregion Misc
	}
}