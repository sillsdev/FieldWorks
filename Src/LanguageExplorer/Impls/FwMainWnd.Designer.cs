// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer.Impls
{
	partial class FwMainWnd
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwMainWnd));
			this._menuStrip = new System.Windows.Forms.MenuStrip();
			this._fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdNewLangProject = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdChooseLangProject = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripFileMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.projectManagementToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdProjectProperties = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdBackup = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdRestoreFromBackup = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripFileMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdProjectLocation = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDeleteProject = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdCreateProjectShortcut = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripFileMenuSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdArchiveWithRamp = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdUploadToWebonary = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripFileMenuSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdPrint = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripFileMenuSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.ImportMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdImportSFMLexicon = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdImportLinguaLinksData = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdImportLiftData = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdImportInterlinearSfm = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdImportWordsAndGlossesSfm = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdImportInterlinearData = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdImportSFMNotebook = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdImportTranslatedLists = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdExport = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdExportInterlinear = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdExportDiscourseChart = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripFileMenuSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdClose = new System.Windows.Forms.ToolStripMenuItem();
			this._sendReceiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdFLExBridge = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdViewMessages = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdLiftBridge = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdViewLiftMessages = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSendReceiveMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdObtainAnyFlexBridgeProject = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdObtainLiftProject = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSendReceiveMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdObtainFirstFlexBridgeProject = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdObtainFirstLiftProject = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSendReceiveMenuSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdHelpChorus = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdCheckForFlexBridgeUpdates = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpAboutFLEXBridge = new System.Windows.Forms.ToolStripMenuItem();
			this._editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripEditMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripEditMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.pasteHyperlinkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyLocationAsHyperlinkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripEditMenuSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdGoToEntry = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdGoToRecord = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdFindAndReplaceText = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdReplaceText = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripEditMenuSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripEditMenuSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdGoToReversalEntry = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdGoToWfiWordform = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDeleteCustomList = new System.Windows.Forms.ToolStripMenuItem();
			this._viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripViewMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.LexicalToolsList = new System.Windows.Forms.ToolStripMenuItem();
			this.WordToolsList = new System.Windows.Forms.ToolStripMenuItem();
			this.GrammarToolsList = new System.Windows.Forms.ToolStripMenuItem();
			this.NotebookToolsList = new System.Windows.Forms.ToolStripMenuItem();
			this.ListsToolsList = new System.Windows.Forms.ToolStripMenuItem();
			this.ShowInvisibleSpaces = new System.Windows.Forms.ToolStripMenuItem();
			this.Show_DictionaryPubPreview = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripViewMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.filtersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.noFilterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdChooseTexts = new System.Windows.Forms.ToolStripMenuItem();
			this._dataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._data_First = new System.Windows.Forms.ToolStripMenuItem();
			this._data_Previous = new System.Windows.Forms.ToolStripMenuItem();
			this._data_Next = new System.Windows.Forms.ToolStripMenuItem();
			this._data_Last = new System.Windows.Forms.ToolStripMenuItem();
			this.dataMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdApproveAndMoveNext = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdApproveForWholeTextAndMoveNext = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdNextIncompleteBundle = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdApprove = new System.Windows.Forms.ToolStripMenuItem();
			this.ApproveAnalysisMovementMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdApproveAndMoveNextSameLine = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMoveFocusBoxRight = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMoveFocusBoxLeft = new System.Windows.Forms.ToolStripMenuItem();
			this.BrowseMovementMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdBrowseMoveNext = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdNextIncompleteBundleNc = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdBrowseMoveNextSameLine = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMoveFocusBoxRightNc = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMoveFocusBoxLeftNc = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMakePhrase = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdBreakPhrase = new System.Windows.Forms.ToolStripMenuItem();
			this.dataMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdRepeatLastMoveLeft = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdRepeatLastMoveRight = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdApproveAll = new System.Windows.Forms.ToolStripMenuItem();
			this._insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertLexEntry = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertSense = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertVariant = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDataTree_Insert_AlternateForm = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertReversalEntry = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDataTree_Insert_Pronunciation = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertMediaFile = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDataTree_Insert_Etymology = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertSubsense = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertPicture = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertExtNote = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertText = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdAddNote = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdAddWordGlossesToFreeTrans = new System.Windows.Forms.ToolStripMenuItem();
			this.ClickInsertsInvisibleSpace = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdGuessWordBreaks = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdImportWordSet = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertHumanApprovedAnalysis = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertRecord = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertSubrecord = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertSubsubrecord = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdAddToLexicon = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertPossibility = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDataTree_Insert_Possibility = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdAddCustomList = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDataTree_Insert_POS_AffixTemplate = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDataTree_Insert_POS_AffixSlot = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDataTree_Insert_POS_InflectionClass = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertEndocentricCompound = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertExocentricCompound = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertExceptionFeature = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertPhonologicalClosedFeature = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertClosedFeature = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertComplexFeature = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDataTree_Insert_ClosedFeature_Value = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertPhoneme = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdDataTree_Insert_Phoneme_Code = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertSegmentNaturalClasses = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertFeatureNaturalClasses = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertPhEnvironment = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertPhRegularRule = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertPhMetathesisRule = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertMorphemeACP = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertAllomorphACP = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertACPGroup = new System.Windows.Forms.ToolStripMenuItem();
			this.insertMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdShowCharMap = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdInsertLinkToFile = new System.Windows.Forms.ToolStripMenuItem();
			this._formatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdFormatStyle = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdFormatApplyStyle = new System.Windows.Forms.ToolStripMenuItem();
			this.WritingSystemMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdVernacularWritingSystemProperties = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdAnalysisWritingSystemProperties = new System.Windows.Forms.ToolStripMenuItem();
			this._toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.configureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdConfigureDictionary = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdConfigureInterlinear = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdConfigureXmlDocView = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdConfigureList = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdConfigureColumns = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdConfigHeadwordNumbers = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdRestoreDefaults = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdAddCustomField = new System.Windows.Forms.ToolStripMenuItem();
			this.toolMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdMergeEntry = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdLexiconLookup = new System.Windows.Forms.ToolStripMenuItem();
			this.ITexts_AddWordsToLexicon = new System.Windows.Forms.ToolStripMenuItem();
			this.toolMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ToolsMenu_SpellingMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdEditSpellingStatus = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdViewIncorrectWords = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdUseVernSpellingDictionary = new System.Windows.Forms.ToolStripMenuItem();
			this.toolMenuSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdChangeSpelling = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdProjectUtilities = new System.Windows.Forms.ToolStripMenuItem();
			this.toolMenuSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdToolsOptions = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF2 = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF3 = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF4 = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF6 = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF7 = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF8 = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF9 = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF10 = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF11 = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdMacroF12 = new System.Windows.Forms.ToolStripMenuItem();
			this._parserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdParseAllWords = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdReparseAllWords = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdReInitializeParser = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdStopParser = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripParserMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdTryAWord = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdParseWordsInCurrentText = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdParseCurrentWord = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdClearSelectedWordParserAnalyses = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripParserMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ChooseParserMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdChooseXAmpleParser = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdChooseHCParser = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdEditParserParameters = new System.Windows.Forms.ToolStripMenuItem();
			this._windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.newWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpLanguageExplorer = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpTraining = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpDemoMovies = new System.Windows.Forms.ToolStripMenuItem();
			this.HelpMenu_ResourcesMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpLexicographyIntro = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpMorphologyIntro = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpNotesSendReceive = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpNotesSFMDatabaseImport = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpNotesLinguaLinksDatabaseImport = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpNotesInterlinearImport = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpNotesWritingSystems = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpXLingPap = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripHelpMenuSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdHelpReportBug = new System.Windows.Forms.ToolStripMenuItem();
			this.CmdHelpMakeSuggestion = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripHelpMenuSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.CmdHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripStandard = new System.Windows.Forms.ToolStrip();
			this.CmdHistoryBack = new System.Windows.Forms.ToolStripButton();
			this.CmdHistoryForward = new System.Windows.Forms.ToolStripButton();
			this.standardToolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.Toolbar_CmdDeleteRecord = new System.Windows.Forms.ToolStripButton();
			this.standardToolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.Toolbar_CmdUndo = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdRedo = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdRefresh = new System.Windows.Forms.ToolStripButton();
			this.standardToolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.Toolbar_CmdFirstRecord = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdPreviousRecord = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdNextRecord = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdLastRecord = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdFLExLiftBridge = new System.Windows.Forms.ToolStripButton();
			this.toolStripContainer = new System.Windows.Forms.ToolStripContainer();
			this._statusbar = new System.Windows.Forms.StatusBar();
			this.statusBarPanelMessage = new System.Windows.Forms.StatusBarPanel();
			this.statusBarPanelProgress = new System.Windows.Forms.StatusBarPanel();
			this.statusBarPanelArea = new System.Windows.Forms.StatusBarPanel();
			this.statusBarPanelRecordNumber = new System.Windows.Forms.StatusBarPanel();
			this.toolStripView = new System.Windows.Forms.ToolStrip();
			this.Toolbar_CmdChooseTexts = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdChangeFilterClearAll = new System.Windows.Forms.ToolStripButton();
			this.toolStripInsert = new System.Windows.Forms.ToolStrip();
			this.Toolbar_CmdInsertLexEntry = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdGoToEntry = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertReversalEntry = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdGoToReversalEntry = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertText = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdAddNote = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdApproveAllButton = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertHumanApprovedAnalysis = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdGoToWfiWordform = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdFindAndReplaceText = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdBreakPhraseButton = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertRecord = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdGoToRecord = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdAddToLexicon = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdLexiconLookup = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertPossibility = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdDataTree_Insert_Possibility = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertEndocentricCompound = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertExocentricCompound = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertExceptionFeature = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertPhonologicalClosedFeature = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertClosedFeature = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertComplexFeature = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertPhoneme = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertSegmentNaturalClasses = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertFeatureNaturalClasses = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertPhEnvironment = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertPhRegularRule = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertPhMetathesisRule = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertMorphemeACP = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertAllomorphACP = new System.Windows.Forms.ToolStripButton();
			this.Toolbar_CmdInsertACPGroup = new System.Windows.Forms.ToolStripButton();
			this.toolStripFormat = new System.Windows.Forms.ToolStrip();
			this.Toolbar_WritingSystemList = new System.Windows.Forms.ToolStripComboBox();
			this.Toolbar_CombinedStylesList = new System.Windows.Forms.ToolStripComboBox();
			this.mainContainer = new LanguageExplorer.Controls.CollapsingSplitContainer();
			this._sidePane = new LanguageExplorer.Controls.SilSidePane.SidePane();
			this._rightPanel = new System.Windows.Forms.Panel();
			this._menuStrip.SuspendLayout();
			this.toolStripStandard.SuspendLayout();
			this.toolStripContainer.ContentPanel.SuspendLayout();
			this.toolStripContainer.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelMessage)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelProgress)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelArea)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelRecordNumber)).BeginInit();
			this.toolStripView.SuspendLayout();
			this.toolStripInsert.SuspendLayout();
			this.toolStripFormat.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.mainContainer)).BeginInit();
			this.mainContainer.Panel1.SuspendLayout();
			this.mainContainer.Panel2.SuspendLayout();
			this.mainContainer.SuspendLayout();
			this.SuspendLayout();
			// 
			// _menuStrip
			// 
			this._menuStrip.Dock = System.Windows.Forms.DockStyle.None;
			this._menuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
			this._menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._fileToolStripMenuItem,
            this._sendReceiveToolStripMenuItem,
            this._editToolStripMenuItem,
            this._viewToolStripMenuItem,
            this._dataToolStripMenuItem,
            this._insertToolStripMenuItem,
            this._formatToolStripMenuItem,
            this._toolsToolStripMenuItem,
            this._parserToolStripMenuItem,
            this._windowToolStripMenuItem,
            this._helpToolStripMenuItem});
			this._menuStrip.Location = new System.Drawing.Point(0, 0);
			this._menuStrip.Name = "_menuStrip";
			this._menuStrip.Size = new System.Drawing.Size(791, 24);
			this._menuStrip.TabIndex = 1;
			this._menuStrip.Text = "menuStrip1";
			// 
			// _fileToolStripMenuItem
			// 
			this._fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdNewLangProject,
            this.CmdChooseLangProject,
            this.toolStripFileMenuSeparator1,
            this.projectManagementToolStripMenuItem,
            this.toolStripFileMenuSeparator3,
            this.CmdArchiveWithRamp,
            this.CmdUploadToWebonary,
            this.toolStripFileMenuSeparator4,
            this.CmdPrint,
            this.toolStripFileMenuSeparator5,
            this.ImportMenu,
            this.CmdExport,
            this.CmdExportInterlinear,
            this.CmdExportDiscourseChart,
            this.toolStripFileMenuSeparator6,
            this.CmdClose});
			this._fileToolStripMenuItem.Name = "_fileToolStripMenuItem";
			this._fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this._fileToolStripMenuItem.Text = "&File";
			// 
			// CmdNewLangProject
			// 
			this.CmdNewLangProject.Image = ((System.Drawing.Image)(resources.GetObject("CmdNewLangProject.Image")));
			this.CmdNewLangProject.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CmdNewLangProject.Name = "CmdNewLangProject";
			this.CmdNewLangProject.Size = new System.Drawing.Size(211, 22);
			this.CmdNewLangProject.Text = "&New FieldWorks Project...";
			this.CmdNewLangProject.ToolTipText = "Create a new FieldWorks project.";
			// 
			// CmdChooseLangProject
			// 
			this.CmdChooseLangProject.Image = ((System.Drawing.Image)(resources.GetObject("CmdChooseLangProject.Image")));
			this.CmdChooseLangProject.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CmdChooseLangProject.Name = "CmdChooseLangProject";
			this.CmdChooseLangProject.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.CmdChooseLangProject.Size = new System.Drawing.Size(211, 22);
			this.CmdChooseLangProject.Text = "&Open...";
			this.CmdChooseLangProject.ToolTipText = "Open an existing FieldWorks project.";
			// 
			// toolStripFileMenuSeparator1
			// 
			this.toolStripFileMenuSeparator1.Name = "toolStripFileMenuSeparator1";
			this.toolStripFileMenuSeparator1.Size = new System.Drawing.Size(208, 6);
			// 
			// projectManagementToolStripMenuItem
			// 
			this.projectManagementToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdProjectProperties,
            this.CmdBackup,
            this.CmdRestoreFromBackup,
            this.toolStripFileMenuSeparator2,
            this.CmdProjectLocation,
            this.CmdDeleteProject,
            this.CmdCreateProjectShortcut});
			this.projectManagementToolStripMenuItem.Name = "projectManagementToolStripMenuItem";
			this.projectManagementToolStripMenuItem.Size = new System.Drawing.Size(211, 22);
			this.projectManagementToolStripMenuItem.Text = "Project &Management";
			// 
			// CmdProjectProperties
			// 
			this.CmdProjectProperties.Name = "CmdProjectProperties";
			this.CmdProjectProperties.Size = new System.Drawing.Size(237, 22);
			this.CmdProjectProperties.Text = "Field&Works Project Properties...";
			this.CmdProjectProperties.ToolTipText = "Edit the special properties of this FieldWorks project (such as name and writing " +
    "systems).";
			// 
			// CmdBackup
			// 
			this.CmdBackup.Name = "CmdBackup";
			this.CmdBackup.Size = new System.Drawing.Size(237, 22);
			this.CmdBackup.Text = "&Back up this Project...";
			this.CmdBackup.ToolTipText = "Back up a FieldWorks project.";
			// 
			// CmdRestoreFromBackup
			// 
			this.CmdRestoreFromBackup.Name = "CmdRestoreFromBackup";
			this.CmdRestoreFromBackup.Size = new System.Drawing.Size(237, 22);
			this.CmdRestoreFromBackup.Text = "&Restore a Project...";
			this.CmdRestoreFromBackup.ToolTipText = "Restore a FieldWorks project.";
			// 
			// toolStripFileMenuSeparator2
			// 
			this.toolStripFileMenuSeparator2.Name = "toolStripFileMenuSeparator2";
			this.toolStripFileMenuSeparator2.Size = new System.Drawing.Size(234, 6);
			// 
			// CmdProjectLocation
			// 
			this.CmdProjectLocation.Name = "CmdProjectLocation";
			this.CmdProjectLocation.Size = new System.Drawing.Size(237, 22);
			this.CmdProjectLocation.Text = "Project Locations...";
			// 
			// CmdDeleteProject
			// 
			this.CmdDeleteProject.Image = ((System.Drawing.Image)(resources.GetObject("CmdDeleteProject.Image")));
			this.CmdDeleteProject.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CmdDeleteProject.Name = "CmdDeleteProject";
			this.CmdDeleteProject.Size = new System.Drawing.Size(237, 22);
			this.CmdDeleteProject.Text = "&Delete Project...";
			this.CmdDeleteProject.ToolTipText = "Delete FieldWorks project.";
			// 
			// CmdCreateProjectShortcut
			// 
			this.CmdCreateProjectShortcut.Name = "CmdCreateProjectShortcut";
			this.CmdCreateProjectShortcut.Size = new System.Drawing.Size(237, 22);
			this.CmdCreateProjectShortcut.Text = "&Create Shortcut on Desktop";
			this.CmdCreateProjectShortcut.ToolTipText = "Create a desktop shortcut to this project.";
			// 
			// toolStripFileMenuSeparator3
			// 
			this.toolStripFileMenuSeparator3.Name = "toolStripFileMenuSeparator3";
			this.toolStripFileMenuSeparator3.Size = new System.Drawing.Size(208, 6);
			// 
			// CmdArchiveWithRamp
			// 
			this.CmdArchiveWithRamp.Image = ((System.Drawing.Image)(resources.GetObject("CmdArchiveWithRamp.Image")));
			this.CmdArchiveWithRamp.Name = "CmdArchiveWithRamp";
			this.CmdArchiveWithRamp.Size = new System.Drawing.Size(211, 22);
			this.CmdArchiveWithRamp.Text = "&Archive with RAMP (SIL)...";
			this.CmdArchiveWithRamp.ToolTipText = "Starts RAMP (if it is installed) and prepares an archive package for uploading.";
			// 
			// CmdUploadToWebonary
			// 
			this.CmdUploadToWebonary.Name = "CmdUploadToWebonary";
			this.CmdUploadToWebonary.Size = new System.Drawing.Size(211, 22);
			this.CmdUploadToWebonary.Text = "Upload to &Webonary...";
			// 
			// toolStripFileMenuSeparator4
			// 
			this.toolStripFileMenuSeparator4.Name = "toolStripFileMenuSeparator4";
			this.toolStripFileMenuSeparator4.Size = new System.Drawing.Size(208, 6);
			// 
			// CmdPrint
			// 
			this.CmdPrint.Enabled = false;
			this.CmdPrint.Name = "CmdPrint";
			this.CmdPrint.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.P)));
			this.CmdPrint.Size = new System.Drawing.Size(211, 22);
			this.CmdPrint.Text = "&Print...";
			this.CmdPrint.ToolTipText = "Print";
			// 
			// toolStripFileMenuSeparator5
			// 
			this.toolStripFileMenuSeparator5.Name = "toolStripFileMenuSeparator5";
			this.toolStripFileMenuSeparator5.Size = new System.Drawing.Size(208, 6);
			// 
			// ImportMenu
			// 
			this.ImportMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdImportSFMLexicon,
            this.CmdImportLinguaLinksData,
            this.CmdImportLiftData,
            this.CmdImportInterlinearSfm,
            this.CmdImportWordsAndGlossesSfm,
            this.CmdImportInterlinearData,
            this.CmdImportSFMNotebook,
            this.CmdImportTranslatedLists});
			this.ImportMenu.Name = "ImportMenu";
			this.ImportMenu.Size = new System.Drawing.Size(211, 22);
			this.ImportMenu.Text = "&Import";
			// 
			// CmdImportSFMLexicon
			// 
			this.CmdImportSFMLexicon.Name = "CmdImportSFMLexicon";
			this.CmdImportSFMLexicon.Size = new System.Drawing.Size(273, 22);
			this.CmdImportSFMLexicon.Text = "Standard Format Lexicon...";
			// 
			// CmdImportLinguaLinksData
			// 
			this.CmdImportLinguaLinksData.Name = "CmdImportLinguaLinksData";
			this.CmdImportLinguaLinksData.Size = new System.Drawing.Size(273, 22);
			this.CmdImportLinguaLinksData.Text = "&LinguaLinks Data...";
			this.CmdImportLinguaLinksData.Visible = false;
			// 
			// CmdImportLiftData
			// 
			this.CmdImportLiftData.Name = "CmdImportLiftData";
			this.CmdImportLiftData.Size = new System.Drawing.Size(273, 22);
			this.CmdImportLiftData.Text = "L&IFT Lexicon...";
			this.CmdImportLiftData.Visible = false;
			// 
			// CmdImportInterlinearSfm
			// 
			this.CmdImportInterlinearSfm.Name = "CmdImportInterlinearSfm";
			this.CmdImportInterlinearSfm.Size = new System.Drawing.Size(273, 22);
			this.CmdImportInterlinearSfm.Text = "Standard Format I&nterlinear...";
			this.CmdImportInterlinearSfm.Visible = false;
			// 
			// CmdImportWordsAndGlossesSfm
			// 
			this.CmdImportWordsAndGlossesSfm.Name = "CmdImportWordsAndGlossesSfm";
			this.CmdImportWordsAndGlossesSfm.Size = new System.Drawing.Size(273, 22);
			this.CmdImportWordsAndGlossesSfm.Text = "Standard Format W&ords and Glosses...";
			this.CmdImportWordsAndGlossesSfm.Visible = false;
			// 
			// CmdImportInterlinearData
			// 
			this.CmdImportInterlinearData.Name = "CmdImportInterlinearData";
			this.CmdImportInterlinearData.Size = new System.Drawing.Size(273, 22);
			this.CmdImportInterlinearData.Text = "FLExText Interl&inear...";
			this.CmdImportInterlinearData.Visible = false;
			// 
			// CmdImportSFMNotebook
			// 
			this.CmdImportSFMNotebook.Name = "CmdImportSFMNotebook";
			this.CmdImportSFMNotebook.Size = new System.Drawing.Size(273, 22);
			this.CmdImportSFMNotebook.Text = "Standard Format &Notebook data...";
			this.CmdImportSFMNotebook.Visible = false;
			// 
			// CmdImportTranslatedLists
			// 
			this.CmdImportTranslatedLists.Name = "CmdImportTranslatedLists";
			this.CmdImportTranslatedLists.Size = new System.Drawing.Size(273, 22);
			this.CmdImportTranslatedLists.Text = "&Translated List Content";
			// 
			// CmdExport
			// 
			this.CmdExport.Enabled = false;
			this.CmdExport.Name = "CmdExport";
			this.CmdExport.Size = new System.Drawing.Size(211, 22);
			this.CmdExport.Text = "&Export...";
			this.CmdExport.ToolTipText = "Export this FieldWorks project to a file.";
			// 
			// CmdExportInterlinear
			// 
			this.CmdExportInterlinear.Name = "CmdExportInterlinear";
			this.CmdExportInterlinear.Size = new System.Drawing.Size(211, 22);
			this.CmdExportInterlinear.Text = "&Export Interlinear...";
			this.CmdExportInterlinear.Visible = false;
			// 
			// CmdExportDiscourseChart
			// 
			this.CmdExportDiscourseChart.Name = "CmdExportDiscourseChart";
			this.CmdExportDiscourseChart.Size = new System.Drawing.Size(211, 22);
			this.CmdExportDiscourseChart.Text = "Export Discourse Chart...";
			this.CmdExportDiscourseChart.Visible = false;
			// 
			// toolStripFileMenuSeparator6
			// 
			this.toolStripFileMenuSeparator6.Name = "toolStripFileMenuSeparator6";
			this.toolStripFileMenuSeparator6.Size = new System.Drawing.Size(208, 6);
			// 
			// CmdClose
			// 
			this.CmdClose.Name = "CmdClose";
			this.CmdClose.Size = new System.Drawing.Size(211, 22);
			this.CmdClose.Text = "&Close";
			this.CmdClose.ToolTipText = "Close this project.";
			// 
			// _sendReceiveToolStripMenuItem
			// 
			this._sendReceiveToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdFLExBridge,
            this.CmdViewMessages,
            this.CmdLiftBridge,
            this.CmdViewLiftMessages,
            this.toolStripSendReceiveMenuSeparator1,
            this.CmdObtainAnyFlexBridgeProject,
            this.CmdObtainLiftProject,
            this.toolStripSendReceiveMenuSeparator2,
            this.CmdObtainFirstFlexBridgeProject,
            this.CmdObtainFirstLiftProject,
            this.toolStripSendReceiveMenuSeparator3,
            this.CmdHelpChorus,
            this.CmdCheckForFlexBridgeUpdates,
            this.CmdHelpAboutFLEXBridge});
			this._sendReceiveToolStripMenuItem.Name = "_sendReceiveToolStripMenuItem";
			this._sendReceiveToolStripMenuItem.Size = new System.Drawing.Size(90, 20);
			this._sendReceiveToolStripMenuItem.Text = "&Send/Receive";
			// 
			// CmdFLExBridge
			// 
			this.CmdFLExBridge.Image = ((System.Drawing.Image)(resources.GetObject("CmdFLExBridge.Image")));
			this.CmdFLExBridge.Name = "CmdFLExBridge";
			this.CmdFLExBridge.Size = new System.Drawing.Size(339, 22);
			this.CmdFLExBridge.Text = "Send/Receive &Project (with other FLEx users)...";
			this.CmdFLExBridge.ToolTipText = "Send/Receive this Project (lexicon and texts)";
			this.CmdFLExBridge.Visible = false;
			// 
			// CmdViewMessages
			// 
			this.CmdViewMessages.Name = "CmdViewMessages";
			this.CmdViewMessages.Size = new System.Drawing.Size(339, 22);
			this.CmdViewMessages.Text = "&View Project Messages...";
			this.CmdViewMessages.ToolTipText = "View Project questions, merge conflicts, and notifications";
			this.CmdViewMessages.Visible = false;
			// 
			// CmdLiftBridge
			// 
			this.CmdLiftBridge.Image = ((System.Drawing.Image)(resources.GetObject("CmdLiftBridge.Image")));
			this.CmdLiftBridge.Name = "CmdLiftBridge";
			this.CmdLiftBridge.Size = new System.Drawing.Size(339, 22);
			this.CmdLiftBridge.Text = "Send/Receive &Lexicon (WeSay)...";
			this.CmdLiftBridge.ToolTipText = "Send/Receive only the Lexicon in this project (with WeSay or other programs that " +
    "use LIFT)";
			this.CmdLiftBridge.Visible = false;
			// 
			// CmdViewLiftMessages
			// 
			this.CmdViewLiftMessages.Name = "CmdViewLiftMessages";
			this.CmdViewLiftMessages.Size = new System.Drawing.Size(339, 22);
			this.CmdViewLiftMessages.Text = "Vie&w Lexicon Messages...";
			this.CmdViewLiftMessages.ToolTipText = "View Lexicon (LIFT) questions, merge conflicts, and notifications";
			this.CmdViewLiftMessages.Visible = false;
			// 
			// toolStripSendReceiveMenuSeparator1
			// 
			this.toolStripSendReceiveMenuSeparator1.Name = "toolStripSendReceiveMenuSeparator1";
			this.toolStripSendReceiveMenuSeparator1.Size = new System.Drawing.Size(336, 6);
			this.toolStripSendReceiveMenuSeparator1.Visible = false;
			// 
			// CmdObtainAnyFlexBridgeProject
			// 
			this.CmdObtainAnyFlexBridgeProject.Image = ((System.Drawing.Image)(resources.GetObject("CmdObtainAnyFlexBridgeProject.Image")));
			this.CmdObtainAnyFlexBridgeProject.Name = "CmdObtainAnyFlexBridgeProject";
			this.CmdObtainAnyFlexBridgeProject.Size = new System.Drawing.Size(339, 22);
			this.CmdObtainAnyFlexBridgeProject.Text = "&Get Project from Colleague...";
			this.CmdObtainAnyFlexBridgeProject.ToolTipText = "Receive a Project or Lexicon from a colleague for the first time (creates a new p" +
    "roject)";
			this.CmdObtainAnyFlexBridgeProject.Visible = false;
			// 
			// CmdObtainLiftProject
			// 
			this.CmdObtainLiftProject.Image = ((System.Drawing.Image)(resources.GetObject("CmdObtainLiftProject.Image")));
			this.CmdObtainLiftProject.Name = "CmdObtainLiftProject";
			this.CmdObtainLiftProject.Size = new System.Drawing.Size(339, 22);
			this.CmdObtainLiftProject.Text = "Get Lexicon (WeSay) and &Merge with this Project...";
			this.CmdObtainLiftProject.ToolTipText = "Receive a Lexicon for the first time (from a WeSay or other LIFT project) and mer" +
    "ge it into the current project";
			this.CmdObtainLiftProject.Visible = false;
			// 
			// toolStripSendReceiveMenuSeparator2
			// 
			this.toolStripSendReceiveMenuSeparator2.Name = "toolStripSendReceiveMenuSeparator2";
			this.toolStripSendReceiveMenuSeparator2.Size = new System.Drawing.Size(336, 6);
			this.toolStripSendReceiveMenuSeparator2.Visible = false;
			// 
			// CmdObtainFirstFlexBridgeProject
			// 
			this.CmdObtainFirstFlexBridgeProject.Image = ((System.Drawing.Image)(resources.GetObject("CmdObtainFirstFlexBridgeProject.Image")));
			this.CmdObtainFirstFlexBridgeProject.Name = "CmdObtainFirstFlexBridgeProject";
			this.CmdObtainFirstFlexBridgeProject.Size = new System.Drawing.Size(339, 22);
			this.CmdObtainFirstFlexBridgeProject.Text = "Send this &Project for the first time...";
			this.CmdObtainFirstFlexBridgeProject.ToolTipText = "Creates a full project repository for this project for the first time (other proj" +
    "ect members will use \"Get Project\")";
			this.CmdObtainFirstFlexBridgeProject.Visible = false;
			// 
			// CmdObtainFirstLiftProject
			// 
			this.CmdObtainFirstLiftProject.Image = ((System.Drawing.Image)(resources.GetObject("CmdObtainFirstLiftProject.Image")));
			this.CmdObtainFirstLiftProject.Name = "CmdObtainFirstLiftProject";
			this.CmdObtainFirstLiftProject.Size = new System.Drawing.Size(339, 22);
			this.CmdObtainFirstLiftProject.Text = "Send this &Lexicon for the first time (to WeSay)...";
			this.CmdObtainFirstLiftProject.ToolTipText = "Creates a LIFT repository for the lexical data in this project for the first time" +
    " (for use with WeSay and other LIFT programs)";
			this.CmdObtainFirstLiftProject.Visible = false;
			// 
			// toolStripSendReceiveMenuSeparator3
			// 
			this.toolStripSendReceiveMenuSeparator3.Name = "toolStripSendReceiveMenuSeparator3";
			this.toolStripSendReceiveMenuSeparator3.Size = new System.Drawing.Size(336, 6);
			this.toolStripSendReceiveMenuSeparator3.Visible = false;
			// 
			// CmdHelpChorus
			// 
			this.CmdHelpChorus.Name = "CmdHelpChorus";
			this.CmdHelpChorus.Size = new System.Drawing.Size(339, 22);
			this.CmdHelpChorus.Text = "&Help...";
			this.CmdHelpChorus.ToolTipText = "Help for using Chorus-enabled features";
			this.CmdHelpChorus.Visible = false;
			// 
			// CmdCheckForFlexBridgeUpdates
			// 
			this.CmdCheckForFlexBridgeUpdates.Name = "CmdCheckForFlexBridgeUpdates";
			this.CmdCheckForFlexBridgeUpdates.Size = new System.Drawing.Size(339, 22);
			this.CmdCheckForFlexBridgeUpdates.Text = "Check for FLEx Bridge &Updates...";
			this.CmdCheckForFlexBridgeUpdates.ToolTipText = "Check for FLEx Bridge updates";
			this.CmdCheckForFlexBridgeUpdates.Visible = false;
			// 
			// CmdHelpAboutFLEXBridge
			// 
			this.CmdHelpAboutFLEXBridge.Name = "CmdHelpAboutFLEXBridge";
			this.CmdHelpAboutFLEXBridge.Size = new System.Drawing.Size(339, 22);
			this.CmdHelpAboutFLEXBridge.Text = "&About FLEx Bridge...";
			this.CmdHelpAboutFLEXBridge.ToolTipText = "Display information about FLEx Bridge";
			this.CmdHelpAboutFLEXBridge.Visible = false;
			// 
			// _editToolStripMenuItem
			// 
			this._editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripEditMenuSeparator1,
            this.cutToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.toolStripEditMenuSeparator2,
            this.pasteHyperlinkToolStripMenuItem,
            this.copyLocationAsHyperlinkToolStripMenuItem,
            this.toolStripEditMenuSeparator3,
            this.CmdGoToEntry,
            this.CmdGoToRecord,
            this.CmdFindAndReplaceText,
            this.CmdReplaceText,
            this.toolStripEditMenuSeparator4,
            this.selectAllToolStripMenuItem,
            this.deleteToolStripMenuItem,
            this.toolStripEditMenuSeparator5,
            this.CmdGoToReversalEntry,
            this.CmdGoToWfiWordform,
            this.CmdDeleteCustomList});
			this._editToolStripMenuItem.Name = "_editToolStripMenuItem";
			this._editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this._editToolStripMenuItem.Text = "&Edit";
			// 
			// undoToolStripMenuItem
			// 
			this.undoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("undoToolStripMenuItem.Image")));
			this.undoToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
			this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
			this.undoToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.undoToolStripMenuItem.Text = "&Undo";
			this.undoToolStripMenuItem.ToolTipText = "Undo previous actions.";
			// 
			// redoToolStripMenuItem
			// 
			this.redoToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("redoToolStripMenuItem.Image")));
			this.redoToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
			this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
			this.redoToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.redoToolStripMenuItem.Text = "&Redo";
			this.redoToolStripMenuItem.ToolTipText = "Redo previous actions.";
			// 
			// toolStripEditMenuSeparator1
			// 
			this.toolStripEditMenuSeparator1.Name = "toolStripEditMenuSeparator1";
			this.toolStripEditMenuSeparator1.Size = new System.Drawing.Size(216, 6);
			// 
			// cutToolStripMenuItem
			// 
			this.cutToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("cutToolStripMenuItem.Image")));
			this.cutToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.cutToolStripMenuItem.Text = "Cu&t";
			this.cutToolStripMenuItem.ToolTipText = "Cut";
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripMenuItem.Image")));
			this.copyToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.copyToolStripMenuItem.Text = "&Copy";
			this.copyToolStripMenuItem.ToolTipText = "Copy";
			// 
			// pasteToolStripMenuItem
			// 
			this.pasteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("pasteToolStripMenuItem.Image")));
			this.pasteToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.pasteToolStripMenuItem.Text = "&Paste";
			this.pasteToolStripMenuItem.ToolTipText = "Paste";
			// 
			// toolStripEditMenuSeparator2
			// 
			this.toolStripEditMenuSeparator2.Name = "toolStripEditMenuSeparator2";
			this.toolStripEditMenuSeparator2.Size = new System.Drawing.Size(216, 6);
			// 
			// pasteHyperlinkToolStripMenuItem
			// 
			this.pasteHyperlinkToolStripMenuItem.Name = "pasteHyperlinkToolStripMenuItem";
			this.pasteHyperlinkToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.pasteHyperlinkToolStripMenuItem.Text = "Paste &Hyperlink";
			this.pasteHyperlinkToolStripMenuItem.ToolTipText = "Paste clipboard content as hyperlink.";
			// 
			// copyLocationAsHyperlinkToolStripMenuItem
			// 
			this.copyLocationAsHyperlinkToolStripMenuItem.Name = "copyLocationAsHyperlinkToolStripMenuItem";
			this.copyLocationAsHyperlinkToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.copyLocationAsHyperlinkToolStripMenuItem.Text = "Copy &Location as Hyperlink";
			this.copyLocationAsHyperlinkToolStripMenuItem.ToolTipText = "Create a hyperlink to this location and copy it to the clipboard.";
			// 
			// toolStripEditMenuSeparator3
			// 
			this.toolStripEditMenuSeparator3.Name = "toolStripEditMenuSeparator3";
			this.toolStripEditMenuSeparator3.Size = new System.Drawing.Size(216, 6);
			// 
			// CmdGoToEntry
			// 
			this.CmdGoToEntry.Image = ((System.Drawing.Image)(resources.GetObject("CmdGoToEntry.Image")));
			this.CmdGoToEntry.Name = "CmdGoToEntry";
			this.CmdGoToEntry.Size = new System.Drawing.Size(219, 22);
			this.CmdGoToEntry.Text = "&Find lexical Entry...";
			this.CmdGoToEntry.Visible = false;
			// 
			// CmdGoToRecord
			// 
			this.CmdGoToRecord.Image = ((System.Drawing.Image)(resources.GetObject("CmdGoToRecord.Image")));
			this.CmdGoToRecord.Name = "CmdGoToRecord";
			this.CmdGoToRecord.Size = new System.Drawing.Size(219, 22);
			this.CmdGoToRecord.Text = "&Find Record...";
			this.CmdGoToRecord.Visible = false;
			// 
			// CmdFindAndReplaceText
			// 
			this.CmdFindAndReplaceText.Image = ((System.Drawing.Image)(resources.GetObject("CmdFindAndReplaceText.Image")));
			this.CmdFindAndReplaceText.Name = "CmdFindAndReplaceText";
			this.CmdFindAndReplaceText.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.F)));
			this.CmdFindAndReplaceText.Size = new System.Drawing.Size(219, 22);
			this.CmdFindAndReplaceText.Text = "Find...";
			this.CmdFindAndReplaceText.ToolTipText = "Find and Replace Text";
			// 
			// CmdReplaceText
			// 
			this.CmdReplaceText.Name = "CmdReplaceText";
			this.CmdReplaceText.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
			this.CmdReplaceText.Size = new System.Drawing.Size(219, 22);
			this.CmdReplaceText.Text = "Replace...";
			// 
			// toolStripEditMenuSeparator4
			// 
			this.toolStripEditMenuSeparator4.Name = "toolStripEditMenuSeparator4";
			this.toolStripEditMenuSeparator4.Size = new System.Drawing.Size(216, 6);
			// 
			// selectAllToolStripMenuItem
			// 
			this.selectAllToolStripMenuItem.Name = "selectAllToolStripMenuItem";
			this.selectAllToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
			this.selectAllToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.selectAllToolStripMenuItem.Text = "Select &All";
			// 
			// deleteToolStripMenuItem
			// 
			this.deleteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("deleteToolStripMenuItem.Image")));
			this.deleteToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Delete)));
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(219, 22);
			this.deleteToolStripMenuItem.Text = "Delete";
			// 
			// toolStripEditMenuSeparator5
			// 
			this.toolStripEditMenuSeparator5.Name = "toolStripEditMenuSeparator5";
			this.toolStripEditMenuSeparator5.Size = new System.Drawing.Size(216, 6);
			this.toolStripEditMenuSeparator5.Visible = false;
			// 
			// CmdGoToReversalEntry
			// 
			this.CmdGoToReversalEntry.Image = ((System.Drawing.Image)(resources.GetObject("CmdGoToReversalEntry.Image")));
			this.CmdGoToReversalEntry.Name = "CmdGoToReversalEntry";
			this.CmdGoToReversalEntry.Size = new System.Drawing.Size(219, 22);
			this.CmdGoToReversalEntry.Text = "&Find reversal entry...";
			this.CmdGoToReversalEntry.Visible = false;
			// 
			// CmdGoToWfiWordform
			// 
			this.CmdGoToWfiWordform.Image = ((System.Drawing.Image)(resources.GetObject("CmdGoToWfiWordform.Image")));
			this.CmdGoToWfiWordform.Name = "CmdGoToWfiWordform";
			this.CmdGoToWfiWordform.Size = new System.Drawing.Size(219, 22);
			this.CmdGoToWfiWordform.Text = "&Find Wordform...";
			this.CmdGoToWfiWordform.Visible = false;
			// 
			// CmdDeleteCustomList
			// 
			this.CmdDeleteCustomList.Name = "CmdDeleteCustomList";
			this.CmdDeleteCustomList.Size = new System.Drawing.Size(219, 22);
			this.CmdDeleteCustomList.Text = "Delete Custom &List";
			this.CmdDeleteCustomList.Visible = false;
			// 
			// _viewToolStripMenuItem
			// 
			this._viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.toolStripViewMenuSeparator1,
            this.LexicalToolsList,
            this.WordToolsList,
            this.GrammarToolsList,
            this.NotebookToolsList,
            this.ListsToolsList,
            this.ShowInvisibleSpaces,
            this.Show_DictionaryPubPreview,
            this.toolStripViewMenuSeparator2,
            this.filtersToolStripMenuItem,
            this.CmdChooseTexts});
			this._viewToolStripMenuItem.Name = "_viewToolStripMenuItem";
			this._viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this._viewToolStripMenuItem.Text = "&View";
			// 
			// refreshToolStripMenuItem
			// 
			this.refreshToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("refreshToolStripMenuItem.Image")));
			this.refreshToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
			this.refreshToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.refreshToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
			this.refreshToolStripMenuItem.Text = "&Refresh";
			this.refreshToolStripMenuItem.ToolTipText = "Refresh the screen.";
			// 
			// toolStripViewMenuSeparator1
			// 
			this.toolStripViewMenuSeparator1.Name = "toolStripViewMenuSeparator1";
			this.toolStripViewMenuSeparator1.Size = new System.Drawing.Size(201, 6);
			// 
			// LexicalToolsList
			// 
			this.LexicalToolsList.Name = "LexicalToolsList";
			this.LexicalToolsList.Size = new System.Drawing.Size(204, 22);
			this.LexicalToolsList.Text = "&Lexicon";
			// 
			// WordToolsList
			// 
			this.WordToolsList.Name = "WordToolsList";
			this.WordToolsList.Size = new System.Drawing.Size(204, 22);
			this.WordToolsList.Text = "&Text && Words";
			// 
			// GrammarToolsList
			// 
			this.GrammarToolsList.Name = "GrammarToolsList";
			this.GrammarToolsList.Size = new System.Drawing.Size(204, 22);
			this.GrammarToolsList.Text = "&Grammar";
			// 
			// NotebookToolsList
			// 
			this.NotebookToolsList.Name = "NotebookToolsList";
			this.NotebookToolsList.Size = new System.Drawing.Size(204, 22);
			this.NotebookToolsList.Text = "&Notebook";
			// 
			// ListsToolsList
			// 
			this.ListsToolsList.Name = "ListsToolsList";
			this.ListsToolsList.Size = new System.Drawing.Size(204, 22);
			this.ListsToolsList.Text = "Li&sts";
			// 
			// ShowInvisibleSpaces
			// 
			this.ShowInvisibleSpaces.CheckOnClick = true;
			this.ShowInvisibleSpaces.Name = "ShowInvisibleSpaces";
			this.ShowInvisibleSpaces.Size = new System.Drawing.Size(204, 22);
			this.ShowInvisibleSpaces.Text = "Invisible Spaces";
			this.ShowInvisibleSpaces.ToolTipText = "View the invisible, zero-width spaces in this text.";
			this.ShowInvisibleSpaces.Visible = false;
			// 
			// Show_DictionaryPubPreview
			// 
			this.Show_DictionaryPubPreview.Name = "Show_DictionaryPubPreview";
			this.Show_DictionaryPubPreview.Size = new System.Drawing.Size(204, 22);
			this.Show_DictionaryPubPreview.Text = "Show &Dictionary Preview";
			this.Show_DictionaryPubPreview.Visible = false;
			// 
			// toolStripViewMenuSeparator2
			// 
			this.toolStripViewMenuSeparator2.Name = "toolStripViewMenuSeparator2";
			this.toolStripViewMenuSeparator2.Size = new System.Drawing.Size(201, 6);
			// 
			// filtersToolStripMenuItem
			// 
			this.filtersToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noFilterToolStripMenuItem});
			this.filtersToolStripMenuItem.Name = "filtersToolStripMenuItem";
			this.filtersToolStripMenuItem.Size = new System.Drawing.Size(204, 22);
			this.filtersToolStripMenuItem.Text = "Filters";
			// 
			// noFilterToolStripMenuItem
			// 
			this.noFilterToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("noFilterToolStripMenuItem.Image")));
			this.noFilterToolStripMenuItem.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.noFilterToolStripMenuItem.Name = "noFilterToolStripMenuItem";
			this.noFilterToolStripMenuItem.Size = new System.Drawing.Size(119, 22);
			this.noFilterToolStripMenuItem.Text = "No Filter";
			// 
			// CmdChooseTexts
			// 
			this.CmdChooseTexts.Image = ((System.Drawing.Image)(resources.GetObject("CmdChooseTexts.Image")));
			this.CmdChooseTexts.Name = "CmdChooseTexts";
			this.CmdChooseTexts.Size = new System.Drawing.Size(204, 22);
			this.CmdChooseTexts.Text = "Choose Texts...";
			this.CmdChooseTexts.Visible = false;
			// 
			// _dataToolStripMenuItem
			// 
			this._dataToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._data_First,
            this._data_Previous,
            this._data_Next,
            this._data_Last,
            this.dataMenuSeparator1,
            this.CmdApproveAndMoveNext,
            this.CmdApproveForWholeTextAndMoveNext,
            this.CmdNextIncompleteBundle,
            this.CmdApprove,
            this.ApproveAnalysisMovementMenu,
            this.BrowseMovementMenu,
            this.CmdMakePhrase,
            this.CmdBreakPhrase,
            this.dataMenuSeparator2,
            this.CmdRepeatLastMoveLeft,
            this.CmdRepeatLastMoveRight,
            this.CmdApproveAll});
			this._dataToolStripMenuItem.Name = "_dataToolStripMenuItem";
			this._dataToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
			this._dataToolStripMenuItem.Text = "&Data";
			// 
			// _data_First
			// 
			this._data_First.Image = global::LanguageExplorer.LanguageExplorerResources.FWFirstArrow;
			this._data_First.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._data_First.Name = "_data_First";
			this._data_First.Size = new System.Drawing.Size(317, 22);
			this._data_First.Text = "&First";
			this._data_First.ToolTipText = "Show the first item.";
			// 
			// _data_Previous
			// 
			this._data_Previous.Image = global::LanguageExplorer.LanguageExplorerResources.FWLeftArrow;
			this._data_Previous.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._data_Previous.Name = "_data_Previous";
			this._data_Previous.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.P)));
			this._data_Previous.Size = new System.Drawing.Size(317, 22);
			this._data_Previous.Text = "&Previous";
			this._data_Previous.ToolTipText = "Show the previous item.";
			// 
			// _data_Next
			// 
			this._data_Next.Image = global::LanguageExplorer.LanguageExplorerResources.FWRightArrow;
			this._data_Next.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._data_Next.Name = "_data_Next";
			this._data_Next.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.N)));
			this._data_Next.Size = new System.Drawing.Size(317, 22);
			this._data_Next.Text = "&Next";
			this._data_Next.ToolTipText = "Show the next item.";
			// 
			// _data_Last
			// 
			this._data_Last.Image = global::LanguageExplorer.LanguageExplorerResources.FWLastArrow;
			this._data_Last.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._data_Last.Name = "_data_Last";
			this._data_Last.Size = new System.Drawing.Size(317, 22);
			this._data_Last.Text = "&Last";
			this._data_Last.ToolTipText = "Show the last item.";
			// 
			// dataMenuSeparator1
			// 
			this.dataMenuSeparator1.Name = "dataMenuSeparator1";
			this.dataMenuSeparator1.Size = new System.Drawing.Size(314, 6);
			this.dataMenuSeparator1.Visible = false;
			// 
			// CmdApproveAndMoveNext
			// 
			this.CmdApproveAndMoveNext.Image = ((System.Drawing.Image)(resources.GetObject("CmdApproveAndMoveNext.Image")));
			this.CmdApproveAndMoveNext.Name = "CmdApproveAndMoveNext";
			this.CmdApproveAndMoveNext.Size = new System.Drawing.Size(317, 22);
			this.CmdApproveAndMoveNext.Text = "&Approve and Move Next";
			this.CmdApproveAndMoveNext.ToolTipText = "Approve the suggested analysis and move to the next word.";
			this.CmdApproveAndMoveNext.Visible = false;
			// 
			// CmdApproveForWholeTextAndMoveNext
			// 
			this.CmdApproveForWholeTextAndMoveNext.Image = ((System.Drawing.Image)(resources.GetObject("CmdApproveForWholeTextAndMoveNext.Image")));
			this.CmdApproveForWholeTextAndMoveNext.Name = "CmdApproveForWholeTextAndMoveNext";
			this.CmdApproveForWholeTextAndMoveNext.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
			this.CmdApproveForWholeTextAndMoveNext.Size = new System.Drawing.Size(317, 22);
			this.CmdApproveForWholeTextAndMoveNext.Text = "Approve &Throughout this Text";
			this.CmdApproveForWholeTextAndMoveNext.ToolTipText = "Approve the suggested analysis throughout this text, and move to the next word.";
			this.CmdApproveForWholeTextAndMoveNext.Visible = false;
			// 
			// CmdNextIncompleteBundle
			// 
			this.CmdNextIncompleteBundle.Name = "CmdNextIncompleteBundle";
			this.CmdNextIncompleteBundle.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.J)));
			this.CmdNextIncompleteBundle.Size = new System.Drawing.Size(317, 22);
			this.CmdNextIncompleteBundle.Text = "Approve and &Jump to Next Incomplete";
			this.CmdNextIncompleteBundle.ToolTipText = "Approve the suggested analysis, and jump to the next word with a suggested or inc" +
    "omplete analysis.";
			this.CmdNextIncompleteBundle.Visible = false;
			// 
			// CmdApprove
			// 
			this.CmdApprove.Name = "CmdApprove";
			this.CmdApprove.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.CmdApprove.Size = new System.Drawing.Size(317, 22);
			this.CmdApprove.Text = "Approve and &Stay";
			this.CmdApprove.ToolTipText = "Approve_and_StayTooltip";
			this.CmdApprove.Visible = false;
			// 
			// ApproveAnalysisMovementMenu
			// 
			this.ApproveAnalysisMovementMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdApproveAndMoveNextSameLine,
            this.CmdMoveFocusBoxRight,
            this.CmdMoveFocusBoxLeft});
			this.ApproveAnalysisMovementMenu.Name = "ApproveAnalysisMovementMenu";
			this.ApproveAnalysisMovementMenu.Size = new System.Drawing.Size(317, 22);
			this.ApproveAnalysisMovementMenu.Text = "&Approve suggestion and";
			this.ApproveAnalysisMovementMenu.Visible = false;
			// 
			// CmdApproveAndMoveNextSameLine
			// 
			this.CmdApproveAndMoveNextSameLine.Name = "CmdApproveAndMoveNextSameLine";
			this.CmdApproveAndMoveNextSameLine.Size = new System.Drawing.Size(197, 22);
			this.CmdApproveAndMoveNextSameLine.Text = "Move Next, &Same Line";
			this.CmdApproveAndMoveNextSameLine.ToolTipText = "Approve the suggested analysis and move to the next word, to the same interlinear" +
    " line.";
			this.CmdApproveAndMoveNextSameLine.Visible = false;
			// 
			// CmdMoveFocusBoxRight
			// 
			this.CmdMoveFocusBoxRight.Name = "CmdMoveFocusBoxRight";
			this.CmdMoveFocusBoxRight.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Right)));
			this.CmdMoveFocusBoxRight.Size = new System.Drawing.Size(197, 22);
			this.CmdMoveFocusBoxRight.Text = "Move &Right";
			this.CmdMoveFocusBoxRight.ToolTipText = "Approve the suggested analysis and move to the word on the right.";
			this.CmdMoveFocusBoxRight.Visible = false;
			// 
			// CmdMoveFocusBoxLeft
			// 
			this.CmdMoveFocusBoxLeft.Name = "CmdMoveFocusBoxLeft";
			this.CmdMoveFocusBoxLeft.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Left)));
			this.CmdMoveFocusBoxLeft.Size = new System.Drawing.Size(197, 22);
			this.CmdMoveFocusBoxLeft.Text = "Move &Left";
			this.CmdMoveFocusBoxLeft.ToolTipText = "Approve the suggested analysis and move to the word on the left.";
			this.CmdMoveFocusBoxLeft.Visible = false;
			// 
			// BrowseMovementMenu
			// 
			this.BrowseMovementMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdBrowseMoveNext,
            this.CmdNextIncompleteBundleNc,
            this.CmdBrowseMoveNextSameLine,
            this.CmdMoveFocusBoxRightNc,
            this.CmdMoveFocusBoxLeftNc});
			this.BrowseMovementMenu.Name = "BrowseMovementMenu";
			this.BrowseMovementMenu.Size = new System.Drawing.Size(317, 22);
			this.BrowseMovementMenu.Text = "Leave &suggestion and";
			this.BrowseMovementMenu.Visible = false;
			// 
			// CmdBrowseMoveNext
			// 
			this.CmdBrowseMoveNext.Name = "CmdBrowseMoveNext";
			this.CmdBrowseMoveNext.Size = new System.Drawing.Size(229, 22);
			this.CmdBrowseMoveNext.Text = "Move &Next";
			this.CmdBrowseMoveNext.ToolTipText = "Leave the suggested analysis as a suggestion, and move to the next word.";
			this.CmdBrowseMoveNext.Visible = false;
			// 
			// CmdNextIncompleteBundleNc
			// 
			this.CmdNextIncompleteBundleNc.Name = "CmdNextIncompleteBundleNc";
			this.CmdNextIncompleteBundleNc.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.J)));
			this.CmdNextIncompleteBundleNc.Size = new System.Drawing.Size(229, 22);
			this.CmdNextIncompleteBundleNc.Text = "&Jump to Next";
			this.CmdNextIncompleteBundleNc.ToolTipText = "Leave the suggested analysis as a suggestion, and jump to the next word with a su" +
    "ggested or incomplete analysis.";
			this.CmdNextIncompleteBundleNc.Visible = false;
			// 
			// CmdBrowseMoveNextSameLine
			// 
			this.CmdBrowseMoveNextSameLine.Name = "CmdBrowseMoveNextSameLine";
			this.CmdBrowseMoveNextSameLine.Size = new System.Drawing.Size(229, 22);
			this.CmdBrowseMoveNextSameLine.Text = "Move Next, &Same Line";
			this.CmdBrowseMoveNextSameLine.ToolTipText = "Approve the suggested analysis and move to the next word, to the same interlinear" +
    " line.";
			this.CmdBrowseMoveNextSameLine.Visible = false;
			// 
			// CmdMoveFocusBoxRightNc
			// 
			this.CmdMoveFocusBoxRightNc.Name = "CmdMoveFocusBoxRightNc";
			this.CmdMoveFocusBoxRightNc.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Right)));
			this.CmdMoveFocusBoxRightNc.Size = new System.Drawing.Size(229, 22);
			this.CmdMoveFocusBoxRightNc.Text = "Move &Right";
			this.CmdMoveFocusBoxRightNc.ToolTipText = "Approve the suggested analysis and move to the word on the right.";
			this.CmdMoveFocusBoxRightNc.Visible = false;
			// 
			// CmdMoveFocusBoxLeftNc
			// 
			this.CmdMoveFocusBoxLeftNc.Name = "CmdMoveFocusBoxLeftNc";
			this.CmdMoveFocusBoxLeftNc.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.Left)));
			this.CmdMoveFocusBoxLeftNc.Size = new System.Drawing.Size(229, 22);
			this.CmdMoveFocusBoxLeftNc.Text = "Move &Left";
			this.CmdMoveFocusBoxLeftNc.ToolTipText = "Approve the suggested analysis and move to the word on the left.";
			this.CmdMoveFocusBoxLeftNc.Visible = false;
			// 
			// CmdMakePhrase
			// 
			this.CmdMakePhrase.Image = ((System.Drawing.Image)(resources.GetObject("CmdMakePhrase.Image")));
			this.CmdMakePhrase.Name = "CmdMakePhrase";
			this.CmdMakePhrase.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M)));
			this.CmdMakePhrase.Size = new System.Drawing.Size(317, 22);
			this.CmdMakePhrase.Text = "&Make phrase with next word";
			this.CmdMakePhrase.Visible = false;
			// 
			// CmdBreakPhrase
			// 
			this.CmdBreakPhrase.Image = ((System.Drawing.Image)(resources.GetObject("CmdBreakPhrase.Image")));
			this.CmdBreakPhrase.Name = "CmdBreakPhrase";
			this.CmdBreakPhrase.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
			this.CmdBreakPhrase.Size = new System.Drawing.Size(317, 22);
			this.CmdBreakPhrase.Text = "&Break phrase into words";
			this.CmdBreakPhrase.ToolTipText = "Break selected phrase into words.";
			this.CmdBreakPhrase.Visible = false;
			// 
			// dataMenuSeparator2
			// 
			this.dataMenuSeparator2.Name = "dataMenuSeparator2";
			this.dataMenuSeparator2.Size = new System.Drawing.Size(314, 6);
			this.dataMenuSeparator2.Visible = false;
			// 
			// CmdRepeatLastMoveLeft
			// 
			this.CmdRepeatLastMoveLeft.Name = "CmdRepeatLastMoveLeft";
			this.CmdRepeatLastMoveLeft.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Left)));
			this.CmdRepeatLastMoveLeft.Size = new System.Drawing.Size(317, 22);
			this.CmdRepeatLastMoveLeft.Text = "Move &Left (last thing moved)";
			this.CmdRepeatLastMoveLeft.Visible = false;
			// 
			// CmdRepeatLastMoveRight
			// 
			this.CmdRepeatLastMoveRight.Name = "CmdRepeatLastMoveRight";
			this.CmdRepeatLastMoveRight.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Right)));
			this.CmdRepeatLastMoveRight.Size = new System.Drawing.Size(317, 22);
			this.CmdRepeatLastMoveRight.Text = "Move &Right (last thing moved)";
			this.CmdRepeatLastMoveRight.Visible = false;
			// 
			// CmdApproveAll
			// 
			this.CmdApproveAll.Name = "CmdApproveAll";
			this.CmdApproveAll.Size = new System.Drawing.Size(317, 22);
			this.CmdApproveAll.Text = "Approve All";
			this.CmdApproveAll.ToolTipText = "Approve all the suggested analyses in this text.";
			this.CmdApproveAll.Visible = false;
			// 
			// _insertToolStripMenuItem
			// 
			this._insertToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdInsertLexEntry,
            this.CmdInsertSense,
            this.CmdInsertVariant,
            this.CmdDataTree_Insert_AlternateForm,
            this.CmdInsertReversalEntry,
            this.CmdDataTree_Insert_Pronunciation,
            this.CmdInsertMediaFile,
            this.CmdDataTree_Insert_Etymology,
            this.CmdInsertSubsense,
            this.CmdInsertPicture,
            this.CmdInsertExtNote,
            this.CmdInsertText,
            this.CmdAddNote,
            this.CmdAddWordGlossesToFreeTrans,
            this.ClickInsertsInvisibleSpace,
            this.CmdGuessWordBreaks,
            this.CmdImportWordSet,
            this.CmdInsertHumanApprovedAnalysis,
            this.CmdInsertRecord,
            this.CmdInsertSubrecord,
            this.CmdInsertSubsubrecord,
            this.CmdAddToLexicon,
            this.CmdInsertPossibility,
            this.CmdDataTree_Insert_Possibility,
            this.CmdAddCustomList,
            this.CmdDataTree_Insert_POS_AffixTemplate,
            this.CmdDataTree_Insert_POS_AffixSlot,
            this.CmdDataTree_Insert_POS_InflectionClass,
            this.CmdInsertEndocentricCompound,
            this.CmdInsertExocentricCompound,
            this.CmdInsertExceptionFeature,
            this.CmdInsertPhonologicalClosedFeature,
            this.CmdInsertClosedFeature,
            this.CmdInsertComplexFeature,
            this.CmdDataTree_Insert_ClosedFeature_Value,
            this.CmdInsertPhoneme,
            this.CmdDataTree_Insert_Phoneme_Code,
            this.CmdInsertSegmentNaturalClasses,
            this.CmdInsertFeatureNaturalClasses,
            this.CmdInsertPhEnvironment,
            this.CmdInsertPhRegularRule,
            this.CmdInsertPhMetathesisRule,
            this.CmdInsertMorphemeACP,
            this.CmdInsertAllomorphACP,
            this.CmdInsertACPGroup,
            this.insertMenuSeparator1,
            this.CmdShowCharMap,
            this.CmdInsertLinkToFile});
			this._insertToolStripMenuItem.Name = "_insertToolStripMenuItem";
			this._insertToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
			this._insertToolStripMenuItem.Text = "&Insert";
			// 
			// CmdInsertLexEntry
			// 
			this.CmdInsertLexEntry.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertLexEntry.Image")));
			this.CmdInsertLexEntry.Name = "CmdInsertLexEntry";
			this.CmdInsertLexEntry.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertLexEntry.Text = "&Entry...";
			this.CmdInsertLexEntry.Visible = false;
			// 
			// CmdInsertSense
			// 
			this.CmdInsertSense.Name = "CmdInsertSense";
			this.CmdInsertSense.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertSense.Text = "&Sense";
			this.CmdInsertSense.Visible = false;
			// 
			// CmdInsertVariant
			// 
			this.CmdInsertVariant.Name = "CmdInsertVariant";
			this.CmdInsertVariant.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertVariant.Text = "&Variant";
			this.CmdInsertVariant.Visible = false;
			// 
			// CmdDataTree_Insert_AlternateForm
			// 
			this.CmdDataTree_Insert_AlternateForm.Name = "CmdDataTree_Insert_AlternateForm";
			this.CmdDataTree_Insert_AlternateForm.Size = new System.Drawing.Size(296, 22);
			this.CmdDataTree_Insert_AlternateForm.Text = "A&llomorph";
			this.CmdDataTree_Insert_AlternateForm.Visible = false;
			// 
			// CmdInsertReversalEntry
			// 
			this.CmdInsertReversalEntry.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertReversalEntry.Image")));
			this.CmdInsertReversalEntry.Name = "CmdInsertReversalEntry";
			this.CmdInsertReversalEntry.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertReversalEntry.Text = "Reversal Entry";
			this.CmdInsertReversalEntry.Visible = false;
			// 
			// CmdDataTree_Insert_Pronunciation
			// 
			this.CmdDataTree_Insert_Pronunciation.Name = "CmdDataTree_Insert_Pronunciation";
			this.CmdDataTree_Insert_Pronunciation.Size = new System.Drawing.Size(296, 22);
			this.CmdDataTree_Insert_Pronunciation.Text = "&Pronunciation";
			this.CmdDataTree_Insert_Pronunciation.Visible = false;
			// 
			// CmdInsertMediaFile
			// 
			this.CmdInsertMediaFile.Name = "CmdInsertMediaFile";
			this.CmdInsertMediaFile.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertMediaFile.Text = "&Sound or Movie";
			this.CmdInsertMediaFile.Visible = false;
			// 
			// CmdDataTree_Insert_Etymology
			// 
			this.CmdDataTree_Insert_Etymology.Name = "CmdDataTree_Insert_Etymology";
			this.CmdDataTree_Insert_Etymology.Size = new System.Drawing.Size(296, 22);
			this.CmdDataTree_Insert_Etymology.Text = "&Etymology";
			this.CmdDataTree_Insert_Etymology.Visible = false;
			// 
			// CmdInsertSubsense
			// 
			this.CmdInsertSubsense.Name = "CmdInsertSubsense";
			this.CmdInsertSubsense.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertSubsense.Text = "Subsense (in sense)";
			this.CmdInsertSubsense.Visible = false;
			// 
			// CmdInsertPicture
			// 
			this.CmdInsertPicture.Name = "CmdInsertPicture";
			this.CmdInsertPicture.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertPicture.Text = "&Picture";
			this.CmdInsertPicture.Visible = false;
			// 
			// CmdInsertExtNote
			// 
			this.CmdInsertExtNote.Name = "CmdInsertExtNote";
			this.CmdInsertExtNote.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertExtNote.Text = "&Extended Note";
			this.CmdInsertExtNote.Visible = false;
			// 
			// CmdInsertText
			// 
			this.CmdInsertText.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertText.Image")));
			this.CmdInsertText.Name = "CmdInsertText";
			this.CmdInsertText.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
			this.CmdInsertText.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertText.Text = "New &Text";
			this.CmdInsertText.Visible = false;
			// 
			// CmdAddNote
			// 
			this.CmdAddNote.Image = ((System.Drawing.Image)(resources.GetObject("CmdAddNote.Image")));
			this.CmdAddNote.Name = "CmdAddNote";
			this.CmdAddNote.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.CmdAddNote.Size = new System.Drawing.Size(296, 22);
			this.CmdAddNote.Text = "&Note";
			this.CmdAddNote.Visible = false;
			// 
			// CmdAddWordGlossesToFreeTrans
			// 
			this.CmdAddWordGlossesToFreeTrans.Name = "CmdAddWordGlossesToFreeTrans";
			this.CmdAddWordGlossesToFreeTrans.Size = new System.Drawing.Size(296, 22);
			this.CmdAddWordGlossesToFreeTrans.Text = "&Word Glosses";
			this.CmdAddWordGlossesToFreeTrans.Visible = false;
			// 
			// ClickInsertsInvisibleSpace
			// 
			this.ClickInsertsInvisibleSpace.CheckOnClick = true;
			this.ClickInsertsInvisibleSpace.Image = ((System.Drawing.Image)(resources.GetObject("ClickInsertsInvisibleSpace.Image")));
			this.ClickInsertsInvisibleSpace.Name = "ClickInsertsInvisibleSpace";
			this.ClickInsertsInvisibleSpace.Size = new System.Drawing.Size(296, 22);
			this.ClickInsertsInvisibleSpace.Text = "Click Inserts Invisible Space";
			this.ClickInsertsInvisibleSpace.ToolTipText = "Turn on mode in which a click inserts an invisible, zero-width space.";
			this.ClickInsertsInvisibleSpace.Visible = false;
			// 
			// CmdGuessWordBreaks
			// 
			this.CmdGuessWordBreaks.Name = "CmdGuessWordBreaks";
			this.CmdGuessWordBreaks.Size = new System.Drawing.Size(296, 22);
			this.CmdGuessWordBreaks.Text = "&Guess Word Breaks";
			this.CmdGuessWordBreaks.ToolTipText = "Have Language Explorer insert invisible, zero-width spaces as word breaks accordi" +
    "ng to the word list and lexical entries.";
			this.CmdGuessWordBreaks.Visible = false;
			// 
			// CmdImportWordSet
			// 
			this.CmdImportWordSet.Name = "CmdImportWordSet";
			this.CmdImportWordSet.Size = new System.Drawing.Size(296, 22);
			this.CmdImportWordSet.Text = "&Import Word Set...";
			this.CmdImportWordSet.Visible = false;
			// 
			// CmdInsertHumanApprovedAnalysis
			// 
			this.CmdInsertHumanApprovedAnalysis.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertHumanApprovedAnalysis.Image")));
			this.CmdInsertHumanApprovedAnalysis.Name = "CmdInsertHumanApprovedAnalysis";
			this.CmdInsertHumanApprovedAnalysis.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertHumanApprovedAnalysis.Text = "Add Approved Analysis...";
			this.CmdInsertHumanApprovedAnalysis.Visible = false;
			// 
			// CmdInsertRecord
			// 
			this.CmdInsertRecord.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertRecord.Image")));
			this.CmdInsertRecord.Name = "CmdInsertRecord";
			this.CmdInsertRecord.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.CmdInsertRecord.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertRecord.Text = "Record";
			this.CmdInsertRecord.Visible = false;
			// 
			// CmdInsertSubrecord
			// 
			this.CmdInsertSubrecord.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertSubrecord.Image")));
			this.CmdInsertSubrecord.Name = "CmdInsertSubrecord";
			this.CmdInsertSubrecord.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertSubrecord.Text = "Subrecord";
			this.CmdInsertSubrecord.Visible = false;
			// 
			// CmdInsertSubsubrecord
			// 
			this.CmdInsertSubsubrecord.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertSubsubrecord.Image")));
			this.CmdInsertSubsubrecord.Name = "CmdInsertSubsubrecord";
			this.CmdInsertSubsubrecord.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertSubsubrecord.Text = "Subrecord of subrecord";
			this.CmdInsertSubsubrecord.Visible = false;
			// 
			// CmdAddToLexicon
			// 
			this.CmdAddToLexicon.Image = ((System.Drawing.Image)(resources.GetObject("CmdAddToLexicon.Image")));
			this.CmdAddToLexicon.Name = "CmdAddToLexicon";
			this.CmdAddToLexicon.Size = new System.Drawing.Size(296, 22);
			this.CmdAddToLexicon.Text = "Entry...";
			this.CmdAddToLexicon.Visible = false;
			// 
			// CmdInsertPossibility
			// 
			this.CmdInsertPossibility.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertPossibility.Image")));
			this.CmdInsertPossibility.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.CmdInsertPossibility.Name = "CmdInsertPossibility";
			this.CmdInsertPossibility.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertPossibility.Text = "&Item";
			this.CmdInsertPossibility.Visible = false;
			// 
			// CmdDataTree_Insert_Possibility
			// 
			this.CmdDataTree_Insert_Possibility.Image = global::LanguageExplorer.LanguageExplorerResources.Insert_Sub_Cat;
			this.CmdDataTree_Insert_Possibility.ImageTransparentColor = System.Drawing.Color.Fuchsia;
			this.CmdDataTree_Insert_Possibility.Name = "CmdDataTree_Insert_Possibility";
			this.CmdDataTree_Insert_Possibility.Size = new System.Drawing.Size(296, 22);
			this.CmdDataTree_Insert_Possibility.Text = "Subitem";
			this.CmdDataTree_Insert_Possibility.Visible = false;
			// 
			// CmdAddCustomList
			// 
			this.CmdAddCustomList.Name = "CmdAddCustomList";
			this.CmdAddCustomList.Size = new System.Drawing.Size(296, 22);
			this.CmdAddCustomList.Text = "Custom &List...";
			this.CmdAddCustomList.Visible = false;
			// 
			// CmdDataTree_Insert_POS_AffixTemplate
			// 
			this.CmdDataTree_Insert_POS_AffixTemplate.Name = "CmdDataTree_Insert_POS_AffixTemplate";
			this.CmdDataTree_Insert_POS_AffixTemplate.Size = new System.Drawing.Size(296, 22);
			this.CmdDataTree_Insert_POS_AffixTemplate.Text = "Insert Affix Template";
			this.CmdDataTree_Insert_POS_AffixTemplate.Visible = false;
			// 
			// CmdDataTree_Insert_POS_AffixSlot
			// 
			this.CmdDataTree_Insert_POS_AffixSlot.Name = "CmdDataTree_Insert_POS_AffixSlot";
			this.CmdDataTree_Insert_POS_AffixSlot.Size = new System.Drawing.Size(296, 22);
			this.CmdDataTree_Insert_POS_AffixSlot.Text = "Insert Affix Slot";
			this.CmdDataTree_Insert_POS_AffixSlot.Visible = false;
			// 
			// CmdDataTree_Insert_POS_InflectionClass
			// 
			this.CmdDataTree_Insert_POS_InflectionClass.Name = "CmdDataTree_Insert_POS_InflectionClass";
			this.CmdDataTree_Insert_POS_InflectionClass.Size = new System.Drawing.Size(296, 22);
			this.CmdDataTree_Insert_POS_InflectionClass.Text = "Insert Inflection Class";
			this.CmdDataTree_Insert_POS_InflectionClass.Visible = false;
			// 
			// CmdInsertEndocentricCompound
			// 
			this.CmdInsertEndocentricCompound.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertEndocentricCompound.Image")));
			this.CmdInsertEndocentricCompound.Name = "CmdInsertEndocentricCompound";
			this.CmdInsertEndocentricCompound.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertEndocentricCompound.Text = "Headed Compound";
			this.CmdInsertEndocentricCompound.Visible = false;
			// 
			// CmdInsertExocentricCompound
			// 
			this.CmdInsertExocentricCompound.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertExocentricCompound.Image")));
			this.CmdInsertExocentricCompound.Name = "CmdInsertExocentricCompound";
			this.CmdInsertExocentricCompound.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertExocentricCompound.Text = "Non-headed Compound";
			this.CmdInsertExocentricCompound.Visible = false;
			// 
			// CmdInsertExceptionFeature
			// 
			this.CmdInsertExceptionFeature.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertExceptionFeature.Image")));
			this.CmdInsertExceptionFeature.Name = "CmdInsertExceptionFeature";
			this.CmdInsertExceptionFeature.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertExceptionFeature.Text = "&Exception Feature...";
			this.CmdInsertExceptionFeature.Visible = false;
			// 
			// CmdInsertPhonologicalClosedFeature
			// 
			this.CmdInsertPhonologicalClosedFeature.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertPhonologicalClosedFeature.Image")));
			this.CmdInsertPhonologicalClosedFeature.Name = "CmdInsertPhonologicalClosedFeature";
			this.CmdInsertPhonologicalClosedFeature.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertPhonologicalClosedFeature.Text = "&Phonological Feature...";
			this.CmdInsertPhonologicalClosedFeature.Visible = false;
			// 
			// CmdInsertClosedFeature
			// 
			this.CmdInsertClosedFeature.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertClosedFeature.Image")));
			this.CmdInsertClosedFeature.Name = "CmdInsertClosedFeature";
			this.CmdInsertClosedFeature.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertClosedFeature.Text = "&Feature...";
			this.CmdInsertClosedFeature.Visible = false;
			// 
			// CmdInsertComplexFeature
			// 
			this.CmdInsertComplexFeature.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertComplexFeature.Image")));
			this.CmdInsertComplexFeature.Name = "CmdInsertComplexFeature";
			this.CmdInsertComplexFeature.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertComplexFeature.Text = "&Complex Feature...";
			this.CmdInsertComplexFeature.Visible = false;
			// 
			// CmdDataTree_Insert_ClosedFeature_Value
			// 
			this.CmdDataTree_Insert_ClosedFeature_Value.Name = "CmdDataTree_Insert_ClosedFeature_Value";
			this.CmdDataTree_Insert_ClosedFeature_Value.Size = new System.Drawing.Size(296, 22);
			this.CmdDataTree_Insert_ClosedFeature_Value.Text = "Insert Feature Value";
			this.CmdDataTree_Insert_ClosedFeature_Value.Visible = false;
			// 
			// CmdInsertPhoneme
			// 
			this.CmdInsertPhoneme.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertPhoneme.Image")));
			this.CmdInsertPhoneme.Name = "CmdInsertPhoneme";
			this.CmdInsertPhoneme.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.CmdInsertPhoneme.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertPhoneme.Text = "Phoneme";
			this.CmdInsertPhoneme.Visible = false;
			// 
			// CmdDataTree_Insert_Phoneme_Code
			// 
			this.CmdDataTree_Insert_Phoneme_Code.Name = "CmdDataTree_Insert_Phoneme_Code";
			this.CmdDataTree_Insert_Phoneme_Code.Size = new System.Drawing.Size(296, 22);
			this.CmdDataTree_Insert_Phoneme_Code.Text = "Grapheme";
			this.CmdDataTree_Insert_Phoneme_Code.Visible = false;
			// 
			// CmdInsertSegmentNaturalClasses
			// 
			this.CmdInsertSegmentNaturalClasses.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertSegmentNaturalClasses.Image")));
			this.CmdInsertSegmentNaturalClasses.Name = "CmdInsertSegmentNaturalClasses";
			this.CmdInsertSegmentNaturalClasses.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.CmdInsertSegmentNaturalClasses.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertSegmentNaturalClasses.Text = "Natural Class (Phonemes)";
			this.CmdInsertSegmentNaturalClasses.Visible = false;
			// 
			// CmdInsertFeatureNaturalClasses
			// 
			this.CmdInsertFeatureNaturalClasses.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertFeatureNaturalClasses.Image")));
			this.CmdInsertFeatureNaturalClasses.Name = "CmdInsertFeatureNaturalClasses";
			this.CmdInsertFeatureNaturalClasses.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertFeatureNaturalClasses.Text = "Natural Class (Features)";
			this.CmdInsertFeatureNaturalClasses.Visible = false;
			// 
			// CmdInsertPhEnvironment
			// 
			this.CmdInsertPhEnvironment.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertPhEnvironment.Image")));
			this.CmdInsertPhEnvironment.Name = "CmdInsertPhEnvironment";
			this.CmdInsertPhEnvironment.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I)));
			this.CmdInsertPhEnvironment.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertPhEnvironment.Text = "Environment";
			this.CmdInsertPhEnvironment.Visible = false;
			// 
			// CmdInsertPhRegularRule
			// 
			this.CmdInsertPhRegularRule.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertPhRegularRule.Image")));
			this.CmdInsertPhRegularRule.Name = "CmdInsertPhRegularRule";
			this.CmdInsertPhRegularRule.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertPhRegularRule.Text = "Phonological Rule";
			this.CmdInsertPhRegularRule.Visible = false;
			// 
			// CmdInsertPhMetathesisRule
			// 
			this.CmdInsertPhMetathesisRule.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertPhMetathesisRule.Image")));
			this.CmdInsertPhMetathesisRule.Name = "CmdInsertPhMetathesisRule";
			this.CmdInsertPhMetathesisRule.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertPhMetathesisRule.Text = "Metathesis Rule";
			this.CmdInsertPhMetathesisRule.Visible = false;
			// 
			// CmdInsertMorphemeACP
			// 
			this.CmdInsertMorphemeACP.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertMorphemeACP.Image")));
			this.CmdInsertMorphemeACP.Name = "CmdInsertMorphemeACP";
			this.CmdInsertMorphemeACP.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertMorphemeACP.Text = "Rule to prevent morpheme co-occurrence";
			this.CmdInsertMorphemeACP.Visible = false;
			// 
			// CmdInsertAllomorphACP
			// 
			this.CmdInsertAllomorphACP.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertAllomorphACP.Image")));
			this.CmdInsertAllomorphACP.Name = "CmdInsertAllomorphACP";
			this.CmdInsertAllomorphACP.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertAllomorphACP.Text = "Rule to prevent allomorph co-occurrence";
			this.CmdInsertAllomorphACP.Visible = false;
			// 
			// CmdInsertACPGroup
			// 
			this.CmdInsertACPGroup.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertACPGroup.Image")));
			this.CmdInsertACPGroup.Name = "CmdInsertACPGroup";
			this.CmdInsertACPGroup.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertACPGroup.Text = "Group of ad hoc rules";
			this.CmdInsertACPGroup.Visible = false;
			// 
			// insertMenuSeparator1
			// 
			this.insertMenuSeparator1.Name = "insertMenuSeparator1";
			this.insertMenuSeparator1.Size = new System.Drawing.Size(293, 6);
			// 
			// CmdShowCharMap
			// 
			this.CmdShowCharMap.Enabled = false;
			this.CmdShowCharMap.Image = ((System.Drawing.Image)(resources.GetObject("CmdShowCharMap.Image")));
			this.CmdShowCharMap.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CmdShowCharMap.Name = "CmdShowCharMap";
			this.CmdShowCharMap.Size = new System.Drawing.Size(296, 22);
			this.CmdShowCharMap.Text = "Special &character...";
			this.CmdShowCharMap.ToolTipText = "Start the Character Map utility.";
			// 
			// CmdInsertLinkToFile
			// 
			this.CmdInsertLinkToFile.Enabled = false;
			this.CmdInsertLinkToFile.Image = ((System.Drawing.Image)(resources.GetObject("CmdInsertLinkToFile.Image")));
			this.CmdInsertLinkToFile.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CmdInsertLinkToFile.Name = "CmdInsertLinkToFile";
			this.CmdInsertLinkToFile.Size = new System.Drawing.Size(296, 22);
			this.CmdInsertLinkToFile.Text = "L&ink to File...";
			this.CmdInsertLinkToFile.ToolTipText = "Insert a link to an external file.";
			// 
			// _formatToolStripMenuItem
			// 
			this._formatToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdFormatStyle,
            this.CmdFormatApplyStyle,
            this.WritingSystemMenu,
            this.CmdVernacularWritingSystemProperties,
            this.CmdAnalysisWritingSystemProperties});
			this._formatToolStripMenuItem.Name = "_formatToolStripMenuItem";
			this._formatToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
			this._formatToolStripMenuItem.Text = "F&ormat";
			// 
			// CmdFormatStyle
			// 
			this.CmdFormatStyle.Name = "CmdFormatStyle";
			this.CmdFormatStyle.Size = new System.Drawing.Size(262, 22);
			this.CmdFormatStyle.Text = "&Styles...";
			this.CmdFormatStyle.ToolTipText = "Add, delete, or change styles.";
			// 
			// CmdFormatApplyStyle
			// 
			this.CmdFormatApplyStyle.Name = "CmdFormatApplyStyle";
			this.CmdFormatApplyStyle.Size = new System.Drawing.Size(262, 22);
			this.CmdFormatApplyStyle.Text = "&Apply Style...";
			// 
			// WritingSystemMenu
			// 
			this.WritingSystemMenu.Name = "WritingSystemMenu";
			this.WritingSystemMenu.Size = new System.Drawing.Size(262, 22);
			this.WritingSystemMenu.Text = "&Writing System";
			// 
			// CmdVernacularWritingSystemProperties
			// 
			this.CmdVernacularWritingSystemProperties.Name = "CmdVernacularWritingSystemProperties";
			this.CmdVernacularWritingSystemProperties.Size = new System.Drawing.Size(262, 22);
			this.CmdVernacularWritingSystemProperties.Text = "Set up &Vernacular Writing Systems...";
			this.CmdVernacularWritingSystemProperties.ToolTipText = "Add, remove, or change the writing systems specified for this project.";
			// 
			// CmdAnalysisWritingSystemProperties
			// 
			this.CmdAnalysisWritingSystemProperties.Name = "CmdAnalysisWritingSystemProperties";
			this.CmdAnalysisWritingSystemProperties.Size = new System.Drawing.Size(262, 22);
			this.CmdAnalysisWritingSystemProperties.Text = "Set up &Analysis Writing Systems...";
			// 
			// _toolsToolStripMenuItem
			// 
			this._toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.configureToolStripMenuItem,
            this.toolMenuSeparator1,
            this.CmdMergeEntry,
            this.CmdLexiconLookup,
            this.ITexts_AddWordsToLexicon,
            this.toolMenuSeparator2,
            this.ToolsMenu_SpellingMenu,
            this.CmdProjectUtilities,
            this.toolMenuSeparator4,
            this.CmdToolsOptions,
            this.CmdMacroF2,
            this.CmdMacroF3,
            this.CmdMacroF4,
            this.CmdMacroF6,
            this.CmdMacroF7,
            this.CmdMacroF8,
            this.CmdMacroF9,
            this.CmdMacroF10,
            this.CmdMacroF11,
            this.CmdMacroF12});
			this._toolsToolStripMenuItem.Name = "_toolsToolStripMenuItem";
			this._toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
			this._toolsToolStripMenuItem.Text = "&Tools";
			// 
			// configureToolStripMenuItem
			// 
			this.configureToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdConfigureDictionary,
            this.CmdConfigureInterlinear,
            this.CmdConfigureXmlDocView,
            this.CmdConfigureList,
            this.CmdConfigureColumns,
            this.CmdConfigHeadwordNumbers,
            this.CmdRestoreDefaults,
            this.CmdAddCustomField});
			this.configureToolStripMenuItem.Name = "configureToolStripMenuItem";
			this.configureToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.configureToolStripMenuItem.Text = "Configure";
			// 
			// CmdConfigureDictionary
			// 
			this.CmdConfigureDictionary.Name = "CmdConfigureDictionary";
			this.CmdConfigureDictionary.Size = new System.Drawing.Size(190, 22);
			this.CmdConfigureDictionary.Text = "{0}";
			this.CmdConfigureDictionary.Visible = false;
			// 
			// CmdConfigureInterlinear
			// 
			this.CmdConfigureInterlinear.Name = "CmdConfigureInterlinear";
			this.CmdConfigureInterlinear.Size = new System.Drawing.Size(190, 22);
			this.CmdConfigureInterlinear.Text = "Interlinear..";
			this.CmdConfigureInterlinear.Visible = false;
			// 
			// CmdConfigureXmlDocView
			// 
			this.CmdConfigureXmlDocView.Name = "CmdConfigureXmlDocView";
			this.CmdConfigureXmlDocView.Size = new System.Drawing.Size(190, 22);
			this.CmdConfigureXmlDocView.Text = "{0}";
			this.CmdConfigureXmlDocView.Visible = false;
			// 
			// CmdConfigureList
			// 
			this.CmdConfigureList.Name = "CmdConfigureList";
			this.CmdConfigureList.Size = new System.Drawing.Size(190, 22);
			this.CmdConfigureList.Text = "List...";
			this.CmdConfigureList.Visible = false;
			// 
			// CmdConfigureColumns
			// 
			this.CmdConfigureColumns.Image = ((System.Drawing.Image)(resources.GetObject("CmdConfigureColumns.Image")));
			this.CmdConfigureColumns.Name = "CmdConfigureColumns";
			this.CmdConfigureColumns.Size = new System.Drawing.Size(190, 22);
			this.CmdConfigureColumns.Text = "&Columns...";
			this.CmdConfigureColumns.ToolTipText = "Configure current browse pane.";
			this.CmdConfigureColumns.Visible = false;
			// 
			// CmdConfigHeadwordNumbers
			// 
			this.CmdConfigHeadwordNumbers.Name = "CmdConfigHeadwordNumbers";
			this.CmdConfigHeadwordNumbers.Size = new System.Drawing.Size(190, 22);
			this.CmdConfigHeadwordNumbers.Text = "Headword Numbers...";
			this.CmdConfigHeadwordNumbers.Visible = false;
			// 
			// CmdRestoreDefaults
			// 
			this.CmdRestoreDefaults.Name = "CmdRestoreDefaults";
			this.CmdRestoreDefaults.Size = new System.Drawing.Size(190, 22);
			this.CmdRestoreDefaults.Text = "Restore Defaults...";
			// 
			// CmdAddCustomField
			// 
			this.CmdAddCustomField.Name = "CmdAddCustomField";
			this.CmdAddCustomField.Size = new System.Drawing.Size(190, 22);
			this.CmdAddCustomField.Text = "Custom &Fields...";
			this.CmdAddCustomField.ToolTipText = "Add or edit custom fields.";
			this.CmdAddCustomField.Visible = false;
			// 
			// toolMenuSeparator1
			// 
			this.toolMenuSeparator1.Name = "toolMenuSeparator1";
			this.toolMenuSeparator1.Size = new System.Drawing.Size(188, 6);
			// 
			// CmdMergeEntry
			// 
			this.CmdMergeEntry.Name = "CmdMergeEntry";
			this.CmdMergeEntry.Size = new System.Drawing.Size(191, 22);
			this.CmdMergeEntry.Text = "&Merge with entry...";
			this.CmdMergeEntry.Visible = false;
			// 
			// CmdLexiconLookup
			// 
			this.CmdLexiconLookup.Image = ((System.Drawing.Image)(resources.GetObject("CmdLexiconLookup.Image")));
			this.CmdLexiconLookup.Name = "CmdLexiconLookup";
			this.CmdLexiconLookup.Size = new System.Drawing.Size(191, 22);
			this.CmdLexiconLookup.Text = "Find in &Dictionary...";
			this.CmdLexiconLookup.Visible = false;
			// 
			// ITexts_AddWordsToLexicon
			// 
			this.ITexts_AddWordsToLexicon.Name = "ITexts_AddWordsToLexicon";
			this.ITexts_AddWordsToLexicon.Size = new System.Drawing.Size(191, 22);
			this.ITexts_AddWordsToLexicon.Text = "Add Words to Lexicon";
			this.ITexts_AddWordsToLexicon.Visible = false;
			// 
			// toolMenuSeparator2
			// 
			this.toolMenuSeparator2.Name = "toolMenuSeparator2";
			this.toolMenuSeparator2.Size = new System.Drawing.Size(188, 6);
			this.toolMenuSeparator2.Visible = false;
			// 
			// ToolsMenu_SpellingMenu
			// 
			this.ToolsMenu_SpellingMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdEditSpellingStatus,
            this.CmdViewIncorrectWords,
            this.CmdUseVernSpellingDictionary,
            this.toolMenuSeparator3,
            this.CmdChangeSpelling});
			this.ToolsMenu_SpellingMenu.Name = "ToolsMenu_SpellingMenu";
			this.ToolsMenu_SpellingMenu.Size = new System.Drawing.Size(191, 22);
			this.ToolsMenu_SpellingMenu.Text = "Spelling";
			// 
			// CmdEditSpellingStatus
			// 
			this.CmdEditSpellingStatus.Name = "CmdEditSpellingStatus";
			this.CmdEditSpellingStatus.Size = new System.Drawing.Size(239, 22);
			this.CmdEditSpellingStatus.Text = "Edit Spelling Status";
			this.CmdEditSpellingStatus.Visible = false;
			// 
			// CmdViewIncorrectWords
			// 
			this.CmdViewIncorrectWords.Name = "CmdViewIncorrectWords";
			this.CmdViewIncorrectWords.Size = new System.Drawing.Size(239, 22);
			this.CmdViewIncorrectWords.Text = "View Incorrect Words in Use";
			this.CmdViewIncorrectWords.Visible = false;
			// 
			// CmdUseVernSpellingDictionary
			// 
			this.CmdUseVernSpellingDictionary.Name = "CmdUseVernSpellingDictionary";
			this.CmdUseVernSpellingDictionary.Size = new System.Drawing.Size(239, 22);
			this.CmdUseVernSpellingDictionary.Text = "Show Vernacular Spelling Errors";
			this.CmdUseVernSpellingDictionary.Visible = false;
			// 
			// toolMenuSeparator3
			// 
			this.toolMenuSeparator3.Name = "toolMenuSeparator3";
			this.toolMenuSeparator3.Size = new System.Drawing.Size(236, 6);
			this.toolMenuSeparator3.Visible = false;
			// 
			// CmdChangeSpelling
			// 
			this.CmdChangeSpelling.Name = "CmdChangeSpelling";
			this.CmdChangeSpelling.Size = new System.Drawing.Size(239, 22);
			this.CmdChangeSpelling.Text = "Change Spelling...";
			this.CmdChangeSpelling.Visible = false;
			// 
			// CmdProjectUtilities
			// 
			this.CmdProjectUtilities.Name = "CmdProjectUtilities";
			this.CmdProjectUtilities.Size = new System.Drawing.Size(191, 22);
			this.CmdProjectUtilities.Text = "&Utilities...";
			this.CmdProjectUtilities.ToolTipText = "Run some special utilities to process your data.";
			// 
			// toolMenuSeparator4
			// 
			this.toolMenuSeparator4.Name = "toolMenuSeparator4";
			this.toolMenuSeparator4.Size = new System.Drawing.Size(188, 6);
			// 
			// CmdToolsOptions
			// 
			this.CmdToolsOptions.Name = "CmdToolsOptions";
			this.CmdToolsOptions.Size = new System.Drawing.Size(191, 22);
			this.CmdToolsOptions.Text = "&Options...";
			// 
			// CmdMacroF2
			// 
			this.CmdMacroF2.Name = "CmdMacroF2";
			this.CmdMacroF2.ShortcutKeys = System.Windows.Forms.Keys.F2;
			this.CmdMacroF2.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF2.Text = "F2";
			this.CmdMacroF2.Visible = false;
			// 
			// CmdMacroF3
			// 
			this.CmdMacroF3.Name = "CmdMacroF3";
			this.CmdMacroF3.ShortcutKeys = System.Windows.Forms.Keys.F3;
			this.CmdMacroF3.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF3.Text = "F3";
			this.CmdMacroF3.Visible = false;
			// 
			// CmdMacroF4
			// 
			this.CmdMacroF4.Name = "CmdMacroF4";
			this.CmdMacroF4.ShortcutKeys = System.Windows.Forms.Keys.F4;
			this.CmdMacroF4.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF4.Text = "F4";
			this.CmdMacroF4.Visible = false;
			// 
			// CmdMacroF6
			// 
			this.CmdMacroF6.Name = "CmdMacroF6";
			this.CmdMacroF6.ShortcutKeys = System.Windows.Forms.Keys.F6;
			this.CmdMacroF6.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF6.Text = "F6";
			this.CmdMacroF6.Visible = false;
			// 
			// CmdMacroF7
			// 
			this.CmdMacroF7.Name = "CmdMacroF7";
			this.CmdMacroF7.ShortcutKeys = System.Windows.Forms.Keys.F7;
			this.CmdMacroF7.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF7.Text = "F7";
			this.CmdMacroF7.Visible = false;
			// 
			// CmdMacroF8
			// 
			this.CmdMacroF8.Name = "CmdMacroF8";
			this.CmdMacroF8.ShortcutKeys = System.Windows.Forms.Keys.F7;
			this.CmdMacroF8.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF8.Text = "F8";
			this.CmdMacroF8.Visible = false;
			// 
			// CmdMacroF9
			// 
			this.CmdMacroF9.Name = "CmdMacroF9";
			this.CmdMacroF9.ShortcutKeys = System.Windows.Forms.Keys.F9;
			this.CmdMacroF9.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF9.Text = "F9";
			this.CmdMacroF9.Visible = false;
			// 
			// CmdMacroF10
			// 
			this.CmdMacroF10.Name = "CmdMacroF10";
			this.CmdMacroF10.ShortcutKeys = System.Windows.Forms.Keys.F10;
			this.CmdMacroF10.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF10.Text = "F10";
			this.CmdMacroF10.Visible = false;
			// 
			// CmdMacroF11
			// 
			this.CmdMacroF11.Name = "CmdMacroF11";
			this.CmdMacroF11.ShortcutKeys = System.Windows.Forms.Keys.F11;
			this.CmdMacroF11.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF11.Text = "F11";
			this.CmdMacroF11.Visible = false;
			// 
			// CmdMacroF12
			// 
			this.CmdMacroF12.Name = "CmdMacroF12";
			this.CmdMacroF12.ShortcutKeys = System.Windows.Forms.Keys.F12;
			this.CmdMacroF12.Size = new System.Drawing.Size(191, 22);
			this.CmdMacroF12.Text = "F12";
			this.CmdMacroF12.Visible = false;
			// 
			// _parserToolStripMenuItem
			// 
			this._parserToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdParseAllWords,
            this.CmdReparseAllWords,
            this.CmdReInitializeParser,
            this.CmdStopParser,
            this.toolStripParserMenuSeparator1,
            this.CmdTryAWord,
            this.CmdParseWordsInCurrentText,
            this.CmdParseCurrentWord,
            this.CmdClearSelectedWordParserAnalyses,
            this.toolStripParserMenuSeparator2,
            this.ChooseParserMenu,
            this.CmdEditParserParameters});
			this._parserToolStripMenuItem.Name = "_parserToolStripMenuItem";
			this._parserToolStripMenuItem.Size = new System.Drawing.Size(51, 20);
			this._parserToolStripMenuItem.Text = "Pa&rser";
			// 
			// CmdParseAllWords
			// 
			this.CmdParseAllWords.Name = "CmdParseAllWords";
			this.CmdParseAllWords.Size = new System.Drawing.Size(228, 22);
			this.CmdParseAllWords.Text = "Pa&rse all words";
			this.CmdParseAllWords.ToolTipText = "Start the Parser running.";
			// 
			// CmdReparseAllWords
			// 
			this.CmdReparseAllWords.Name = "CmdReparseAllWords";
			this.CmdReparseAllWords.Size = new System.Drawing.Size(228, 22);
			this.CmdReparseAllWords.Text = "&Reparse all words";
			// 
			// CmdReInitializeParser
			// 
			this.CmdReInitializeParser.Name = "CmdReInitializeParser";
			this.CmdReInitializeParser.Size = new System.Drawing.Size(228, 22);
			this.CmdReInitializeParser.Text = "Re&load Grammar / Lexicon";
			this.CmdReInitializeParser.ToolTipText = "Reload the Parser information.";
			// 
			// CmdStopParser
			// 
			this.CmdStopParser.Name = "CmdStopParser";
			this.CmdStopParser.Size = new System.Drawing.Size(228, 22);
			this.CmdStopParser.Text = "&Stop Parser";
			this.CmdStopParser.ToolTipText = "Stop the Parser.";
			// 
			// toolStripParserMenuSeparator1
			// 
			this.toolStripParserMenuSeparator1.Name = "toolStripParserMenuSeparator1";
			this.toolStripParserMenuSeparator1.Size = new System.Drawing.Size(225, 6);
			// 
			// CmdTryAWord
			// 
			this.CmdTryAWord.Name = "CmdTryAWord";
			this.CmdTryAWord.Size = new System.Drawing.Size(228, 22);
			this.CmdTryAWord.Text = "&Try a Word...";
			this.CmdTryAWord.ToolTipText = "Have the Parser try a single word.";
			// 
			// CmdParseWordsInCurrentText
			// 
			this.CmdParseWordsInCurrentText.Name = "CmdParseWordsInCurrentText";
			this.CmdParseWordsInCurrentText.Size = new System.Drawing.Size(228, 22);
			this.CmdParseWordsInCurrentText.Text = "Parse Words in Te&xt";
			// 
			// CmdParseCurrentWord
			// 
			this.CmdParseCurrentWord.Name = "CmdParseCurrentWord";
			this.CmdParseCurrentWord.Size = new System.Drawing.Size(228, 22);
			this.CmdParseCurrentWord.Text = "Parse &Current Word";
			this.CmdParseCurrentWord.ToolTipText = "Have the Parser parse just the current word.";
			// 
			// CmdClearSelectedWordParserAnalyses
			// 
			this.CmdClearSelectedWordParserAnalyses.Name = "CmdClearSelectedWordParserAnalyses";
			this.CmdClearSelectedWordParserAnalyses.Size = new System.Drawing.Size(228, 22);
			this.CmdClearSelectedWordParserAnalyses.Text = "Clear Current Parser &Analyses";
			// 
			// toolStripParserMenuSeparator2
			// 
			this.toolStripParserMenuSeparator2.Name = "toolStripParserMenuSeparator2";
			this.toolStripParserMenuSeparator2.Size = new System.Drawing.Size(225, 6);
			// 
			// ChooseParserMenu
			// 
			this.ChooseParserMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdChooseXAmpleParser,
            this.CmdChooseHCParser});
			this.ChooseParserMenu.Name = "ChooseParserMenu";
			this.ChooseParserMenu.Size = new System.Drawing.Size(228, 22);
			this.ChooseParserMenu.Text = "Choose Parser";
			// 
			// CmdChooseXAmpleParser
			// 
			this.CmdChooseXAmpleParser.Name = "CmdChooseXAmpleParser";
			this.CmdChooseXAmpleParser.Size = new System.Drawing.Size(139, 22);
			this.CmdChooseXAmpleParser.Tag = "";
			this.CmdChooseXAmpleParser.Text = "XAmple";
			// 
			// CmdChooseHCParser
			// 
			this.CmdChooseHCParser.Name = "CmdChooseHCParser";
			this.CmdChooseHCParser.Size = new System.Drawing.Size(139, 22);
			this.CmdChooseHCParser.Tag = "";
			this.CmdChooseHCParser.Text = "Hermit Crab";
			// 
			// CmdEditParserParameters
			// 
			this.CmdEditParserParameters.Name = "CmdEditParserParameters";
			this.CmdEditParserParameters.Size = new System.Drawing.Size(228, 22);
			this.CmdEditParserParameters.Text = "&Edit Parser Parameters...";
			this.CmdEditParserParameters.ToolTipText = "Edit the special parameters for the Parser.";
			// 
			// _windowToolStripMenuItem
			// 
			this._windowToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newWindowToolStripMenuItem});
			this._windowToolStripMenuItem.Name = "_windowToolStripMenuItem";
			this._windowToolStripMenuItem.Size = new System.Drawing.Size(63, 20);
			this._windowToolStripMenuItem.Text = "&Window";
			// 
			// newWindowToolStripMenuItem
			// 
			this.newWindowToolStripMenuItem.Name = "newWindowToolStripMenuItem";
			this.newWindowToolStripMenuItem.Size = new System.Drawing.Size(145, 22);
			this.newWindowToolStripMenuItem.Text = "&New Window";
			this.newWindowToolStripMenuItem.ToolTipText = "Launch a new window of this editor.";
			// 
			// _helpToolStripMenuItem
			// 
			this._helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdHelpLanguageExplorer,
            this.CmdHelpTraining,
            this.CmdHelpDemoMovies,
            this.HelpMenu_ResourcesMenu,
            this.toolStripHelpMenuSeparator1,
            this.CmdHelpReportBug,
            this.CmdHelpMakeSuggestion,
            this.toolStripHelpMenuSeparator2,
            this.CmdHelpAbout});
			this._helpToolStripMenuItem.Name = "_helpToolStripMenuItem";
			this._helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this._helpToolStripMenuItem.Text = "&Help";
			// 
			// CmdHelpLanguageExplorer
			// 
			this.CmdHelpLanguageExplorer.Name = "CmdHelpLanguageExplorer";
			this.CmdHelpLanguageExplorer.ShortcutKeys = System.Windows.Forms.Keys.F1;
			this.CmdHelpLanguageExplorer.Size = new System.Drawing.Size(217, 22);
			this.CmdHelpLanguageExplorer.Text = "&Language Explorer...";
			this.CmdHelpLanguageExplorer.ToolTipText = "Help on using Language Explorer (only available in English).";
			// 
			// CmdHelpTraining
			// 
			this.CmdHelpTraining.Name = "CmdHelpTraining";
			this.CmdHelpTraining.Size = new System.Drawing.Size(217, 22);
			this.CmdHelpTraining.Text = "&Training";
			this.CmdHelpTraining.ToolTipText = "Training for using Language Explorer (only available in English).";
			// 
			// CmdHelpDemoMovies
			// 
			this.CmdHelpDemoMovies.Name = "CmdHelpDemoMovies";
			this.CmdHelpDemoMovies.Size = new System.Drawing.Size(217, 22);
			this.CmdHelpDemoMovies.Text = "&Demo Movies...";
			this.CmdHelpDemoMovies.ToolTipText = "Run the Demo Movies.";
			// 
			// HelpMenu_ResourcesMenu
			// 
			this.HelpMenu_ResourcesMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdHelpLexicographyIntro,
            this.CmdHelpMorphologyIntro,
            this.CmdHelpNotesSendReceive,
            this.CmdHelpNotesSFMDatabaseImport,
            this.CmdHelpNotesLinguaLinksDatabaseImport,
            this.CmdHelpNotesInterlinearImport,
            this.CmdHelpNotesWritingSystems,
            this.CmdHelpXLingPap});
			this.HelpMenu_ResourcesMenu.Name = "HelpMenu_ResourcesMenu";
			this.HelpMenu_ResourcesMenu.Size = new System.Drawing.Size(217, 22);
			this.HelpMenu_ResourcesMenu.Text = "&Resources";
			// 
			// CmdHelpLexicographyIntro
			// 
			this.CmdHelpLexicographyIntro.Name = "CmdHelpLexicographyIntro";
			this.CmdHelpLexicographyIntro.Size = new System.Drawing.Size(318, 22);
			this.CmdHelpLexicographyIntro.Text = "Introduction to &Lexicography...";
			// 
			// CmdHelpMorphologyIntro
			// 
			this.CmdHelpMorphologyIntro.Name = "CmdHelpMorphologyIntro";
			this.CmdHelpMorphologyIntro.Size = new System.Drawing.Size(318, 22);
			this.CmdHelpMorphologyIntro.Text = "Introduction to &Parsing...";
			// 
			// CmdHelpNotesSendReceive
			// 
			this.CmdHelpNotesSendReceive.Name = "CmdHelpNotesSendReceive";
			this.CmdHelpNotesSendReceive.Size = new System.Drawing.Size(318, 22);
			this.CmdHelpNotesSendReceive.Text = "Technical Notes on FieldWorks Send-&Receive...";
			this.CmdHelpNotesSendReceive.ToolTipText = "Display technical notes on FieldWorks Send/Receive (only available in English).";
			// 
			// CmdHelpNotesSFMDatabaseImport
			// 
			this.CmdHelpNotesSFMDatabaseImport.Name = "CmdHelpNotesSFMDatabaseImport";
			this.CmdHelpNotesSFMDatabaseImport.Size = new System.Drawing.Size(318, 22);
			this.CmdHelpNotesSFMDatabaseImport.Text = "Technical Notes on &SFM Database Import...";
			// 
			// CmdHelpNotesLinguaLinksDatabaseImport
			// 
			this.CmdHelpNotesLinguaLinksDatabaseImport.Name = "CmdHelpNotesLinguaLinksDatabaseImport";
			this.CmdHelpNotesLinguaLinksDatabaseImport.Size = new System.Drawing.Size(318, 22);
			this.CmdHelpNotesLinguaLinksDatabaseImport.Text = "Technical Notes on Lin&guaLinks Import...";
			// 
			// CmdHelpNotesInterlinearImport
			// 
			this.CmdHelpNotesInterlinearImport.Name = "CmdHelpNotesInterlinearImport";
			this.CmdHelpNotesInterlinearImport.Size = new System.Drawing.Size(318, 22);
			this.CmdHelpNotesInterlinearImport.Text = "Technical Notes on &Interlinear Import...";
			// 
			// CmdHelpNotesWritingSystems
			// 
			this.CmdHelpNotesWritingSystems.Name = "CmdHelpNotesWritingSystems";
			this.CmdHelpNotesWritingSystems.Size = new System.Drawing.Size(318, 22);
			this.CmdHelpNotesWritingSystems.Text = "Technical Notes on &Writing Systems...";
			this.CmdHelpNotesWritingSystems.ToolTipText = "Display technical notes on Writing Systems (only available in English).";
			// 
			// CmdHelpXLingPap
			// 
			this.CmdHelpXLingPap.Name = "CmdHelpXLingPap";
			this.CmdHelpXLingPap.Size = new System.Drawing.Size(318, 22);
			this.CmdHelpXLingPap.Text = "Editing Linguistics Papers Using &XLingPaper...";
			this.CmdHelpXLingPap.ToolTipText = "You can edit your Grammar Sketch in XLingPaper format using an XML editor (only a" +
    "vailable in English).";
			// 
			// toolStripHelpMenuSeparator1
			// 
			this.toolStripHelpMenuSeparator1.Name = "toolStripHelpMenuSeparator1";
			this.toolStripHelpMenuSeparator1.Size = new System.Drawing.Size(214, 6);
			// 
			// CmdHelpReportBug
			// 
			this.CmdHelpReportBug.Name = "CmdHelpReportBug";
			this.CmdHelpReportBug.Size = new System.Drawing.Size(217, 22);
			this.CmdHelpReportBug.Text = "&Report a Problem...";
			// 
			// CmdHelpMakeSuggestion
			// 
			this.CmdHelpMakeSuggestion.Name = "CmdHelpMakeSuggestion";
			this.CmdHelpMakeSuggestion.Size = new System.Drawing.Size(217, 22);
			this.CmdHelpMakeSuggestion.Text = "&Make a Suggestion...";
			// 
			// toolStripHelpMenuSeparator2
			// 
			this.toolStripHelpMenuSeparator2.Name = "toolStripHelpMenuSeparator2";
			this.toolStripHelpMenuSeparator2.Size = new System.Drawing.Size(214, 6);
			// 
			// CmdHelpAbout
			// 
			this.CmdHelpAbout.Name = "CmdHelpAbout";
			this.CmdHelpAbout.Size = new System.Drawing.Size(217, 22);
			this.CmdHelpAbout.Text = "&About Language Explorer...";
			this.CmdHelpAbout.ToolTipText = "Display version information about this application.";
			// 
			// toolStripStandard
			// 
			this.toolStripStandard.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStripStandard.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.toolStripStandard.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.CmdHistoryBack,
            this.CmdHistoryForward,
            this.standardToolStripSeparator1,
            this.Toolbar_CmdDeleteRecord,
            this.standardToolStripSeparator2,
            this.Toolbar_CmdUndo,
            this.Toolbar_CmdRedo,
            this.Toolbar_CmdRefresh,
            this.standardToolStripSeparator3,
            this.Toolbar_CmdFirstRecord,
            this.Toolbar_CmdPreviousRecord,
            this.Toolbar_CmdNextRecord,
            this.Toolbar_CmdLastRecord,
            this.Toolbar_CmdFLExLiftBridge});
			this.toolStripStandard.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.toolStripStandard.Location = new System.Drawing.Point(3, 24);
			this.toolStripStandard.Name = "toolStripStandard";
			this.toolStripStandard.Size = new System.Drawing.Size(270, 27);
			this.toolStripStandard.TabIndex = 2;
			this.toolStripStandard.Text = "Standard";
			// 
			// CmdHistoryBack
			// 
			this.CmdHistoryBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.CmdHistoryBack.Enabled = false;
			this.CmdHistoryBack.Image = ((System.Drawing.Image)(resources.GetObject("CmdHistoryBack.Image")));
			this.CmdHistoryBack.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CmdHistoryBack.Name = "CmdHistoryBack";
			this.CmdHistoryBack.Size = new System.Drawing.Size(24, 24);
			this.CmdHistoryBack.Text = "Go Back";
			// 
			// CmdHistoryForward
			// 
			this.CmdHistoryForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.CmdHistoryForward.Enabled = false;
			this.CmdHistoryForward.Image = ((System.Drawing.Image)(resources.GetObject("CmdHistoryForward.Image")));
			this.CmdHistoryForward.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CmdHistoryForward.Name = "CmdHistoryForward";
			this.CmdHistoryForward.Size = new System.Drawing.Size(24, 24);
			this.CmdHistoryForward.Text = "Go Forward";
			// 
			// standardToolStripSeparator1
			// 
			this.standardToolStripSeparator1.Name = "standardToolStripSeparator1";
			this.standardToolStripSeparator1.Size = new System.Drawing.Size(6, 27);
			// 
			// Toolbar_CmdDeleteRecord
			// 
			this.Toolbar_CmdDeleteRecord.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdDeleteRecord.Enabled = false;
			this.Toolbar_CmdDeleteRecord.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdDeleteRecord.Image")));
			this.Toolbar_CmdDeleteRecord.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdDeleteRecord.Name = "Toolbar_CmdDeleteRecord";
			this.Toolbar_CmdDeleteRecord.Size = new System.Drawing.Size(24, 24);
			// 
			// standardToolStripSeparator2
			// 
			this.standardToolStripSeparator2.Name = "standardToolStripSeparator2";
			this.standardToolStripSeparator2.Size = new System.Drawing.Size(6, 27);
			// 
			// Toolbar_CmdUndo
			// 
			this.Toolbar_CmdUndo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdUndo.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdUndo.Image")));
			this.Toolbar_CmdUndo.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdUndo.Name = "Toolbar_CmdUndo";
			this.Toolbar_CmdUndo.Size = new System.Drawing.Size(24, 24);
			this.Toolbar_CmdUndo.Text = "Undo";
			this.Toolbar_CmdUndo.ToolTipText = "Undo previous actions.";
			// 
			// Toolbar_CmdRedo
			// 
			this.Toolbar_CmdRedo.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdRedo.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdRedo.Image")));
			this.Toolbar_CmdRedo.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdRedo.Name = "Toolbar_CmdRedo";
			this.Toolbar_CmdRedo.Size = new System.Drawing.Size(24, 24);
			this.Toolbar_CmdRedo.Text = "Redo";
			this.Toolbar_CmdRedo.ToolTipText = "Redo previous actions.";
			// 
			// Toolbar_CmdRefresh
			// 
			this.Toolbar_CmdRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdRefresh.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdRefresh.Image")));
			this.Toolbar_CmdRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdRefresh.Name = "Toolbar_CmdRefresh";
			this.Toolbar_CmdRefresh.Size = new System.Drawing.Size(24, 24);
			this.Toolbar_CmdRefresh.Text = "Refresh";
			this.Toolbar_CmdRefresh.ToolTipText = "Refresh the screen.";
			// 
			// standardToolStripSeparator3
			// 
			this.standardToolStripSeparator3.Name = "standardToolStripSeparator3";
			this.standardToolStripSeparator3.Size = new System.Drawing.Size(6, 27);
			// 
			// Toolbar_CmdFirstRecord
			// 
			this.Toolbar_CmdFirstRecord.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdFirstRecord.Image = global::LanguageExplorer.LanguageExplorerResources.FWFirstArrow;
			this.Toolbar_CmdFirstRecord.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdFirstRecord.Name = "Toolbar_CmdFirstRecord";
			this.Toolbar_CmdFirstRecord.Size = new System.Drawing.Size(24, 24);
			this.Toolbar_CmdFirstRecord.ToolTipText = "Show the first item.";
			// 
			// Toolbar_CmdPreviousRecord
			// 
			this.Toolbar_CmdPreviousRecord.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdPreviousRecord.Image = global::LanguageExplorer.LanguageExplorerResources.FWLeftArrow;
			this.Toolbar_CmdPreviousRecord.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdPreviousRecord.Name = "Toolbar_CmdPreviousRecord";
			this.Toolbar_CmdPreviousRecord.Size = new System.Drawing.Size(24, 24);
			this.Toolbar_CmdPreviousRecord.ToolTipText = "Show the previous item.";
			// 
			// Toolbar_CmdNextRecord
			// 
			this.Toolbar_CmdNextRecord.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdNextRecord.Image = global::LanguageExplorer.LanguageExplorerResources.FWRightArrow;
			this.Toolbar_CmdNextRecord.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdNextRecord.Name = "Toolbar_CmdNextRecord";
			this.Toolbar_CmdNextRecord.Size = new System.Drawing.Size(24, 24);
			this.Toolbar_CmdNextRecord.ToolTipText = "Show the next item.";
			// 
			// Toolbar_CmdLastRecord
			// 
			this.Toolbar_CmdLastRecord.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdLastRecord.Image = global::LanguageExplorer.LanguageExplorerResources.FWLastArrow;
			this.Toolbar_CmdLastRecord.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdLastRecord.Name = "Toolbar_CmdLastRecord";
			this.Toolbar_CmdLastRecord.Size = new System.Drawing.Size(24, 24);
			this.Toolbar_CmdLastRecord.ToolTipText = "Show the last item.";
			// 
			// Toolbar_CmdFLExLiftBridge
			// 
			this.Toolbar_CmdFLExLiftBridge.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdFLExLiftBridge.Enabled = false;
			this.Toolbar_CmdFLExLiftBridge.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdFLExLiftBridge.Image")));
			this.Toolbar_CmdFLExLiftBridge.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdFLExLiftBridge.Name = "Toolbar_CmdFLExLiftBridge";
			this.Toolbar_CmdFLExLiftBridge.Size = new System.Drawing.Size(24, 24);
			this.Toolbar_CmdFLExLiftBridge.ToolTipText = "Send/Receive data in this project";
			this.Toolbar_CmdFLExLiftBridge.Visible = false;
			// 
			// toolStripContainer
			// 
			this.toolStripContainer.BottomToolStripPanelVisible = false;
			// 
			// toolStripContainer.ContentPanel
			// 
			this.toolStripContainer.ContentPanel.AutoScroll = true;
			this.toolStripContainer.ContentPanel.Controls.Add(this.mainContainer);
			this.toolStripContainer.ContentPanel.Controls.Add(this._statusbar);
			this.toolStripContainer.ContentPanel.Size = new System.Drawing.Size(791, 399);
			this.toolStripContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStripContainer.LeftToolStripPanelVisible = false;
			this.toolStripContainer.Location = new System.Drawing.Point(0, 0);
			this.toolStripContainer.Name = "toolStripContainer";
			this.toolStripContainer.RightToolStripPanelVisible = false;
			this.toolStripContainer.Size = new System.Drawing.Size(791, 450);
			this.toolStripContainer.TabIndex = 3;
			this.toolStripContainer.Text = "toolStripContainer1";
			// 
			// toolStripContainer.TopToolStripPanel
			// 
			this.toolStripContainer.TopToolStripPanel.Controls.Add(this._menuStrip);
			this.toolStripContainer.TopToolStripPanel.Controls.Add(this.toolStripStandard);
			this.toolStripContainer.TopToolStripPanel.Controls.Add(this.toolStripView);
			this.toolStripContainer.TopToolStripPanel.Controls.Add(this.toolStripInsert);
			this.toolStripContainer.TopToolStripPanel.Controls.Add(this.toolStripFormat);
			// 
			// _statusbar
			// 
			this._statusbar.Location = new System.Drawing.Point(0, 381);
			this._statusbar.Margin = new System.Windows.Forms.Padding(2);
			this._statusbar.Name = "_statusbar";
			this._statusbar.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.statusBarPanelMessage,
            this.statusBarPanelProgress,
            this.statusBarPanelArea,
            this.statusBarPanelRecordNumber});
			this._statusbar.ShowPanels = true;
			this._statusbar.Size = new System.Drawing.Size(791, 18);
			this._statusbar.TabIndex = 4;
			// 
			// statusBarPanelMessage
			// 
			this.statusBarPanelMessage.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.statusBarPanelMessage.MinWidth = 40;
			this.statusBarPanelMessage.Name = "statusBarPanelMessage";
			this.statusBarPanelMessage.Text = "Message";
			this.statusBarPanelMessage.Width = 60;
			// 
			// statusBarPanelProgress
			// 
			this.statusBarPanelProgress.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.statusBarPanelProgress.MinWidth = 40;
			this.statusBarPanelProgress.Name = "statusBarPanelProgress";
			this.statusBarPanelProgress.Text = "Progress";
			this.statusBarPanelProgress.Width = 59;
			// 
			// statusBarPanelArea
			// 
			this.statusBarPanelArea.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
			this.statusBarPanelArea.Name = "statusBarPanelArea";
			this.statusBarPanelArea.Width = 564;
			// 
			// statusBarPanelRecordNumber
			// 
			this.statusBarPanelRecordNumber.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Contents;
			this.statusBarPanelRecordNumber.MinWidth = 40;
			this.statusBarPanelRecordNumber.Name = "statusBarPanelRecordNumber";
			this.statusBarPanelRecordNumber.Text = "RecordNumber";
			this.statusBarPanelRecordNumber.Width = 91;
			// 
			// toolStripView
			// 
			this.toolStripView.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStripView.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Toolbar_CmdChooseTexts,
            this.Toolbar_CmdChangeFilterClearAll});
			this.toolStripView.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.toolStripView.Location = new System.Drawing.Point(273, 24);
			this.toolStripView.Name = "toolStripView";
			this.toolStripView.Size = new System.Drawing.Size(35, 25);
			this.toolStripView.TabIndex = 5;
			// 
			// Toolbar_CmdChooseTexts
			// 
			this.Toolbar_CmdChooseTexts.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdChooseTexts.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdChooseTexts.Image")));
			this.Toolbar_CmdChooseTexts.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdChooseTexts.Name = "Toolbar_CmdChooseTexts";
			this.Toolbar_CmdChooseTexts.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdChooseTexts.ToolTipText = "Choose texts to display and use.";
			this.Toolbar_CmdChooseTexts.Visible = false;
			// 
			// Toolbar_CmdChangeFilterClearAll
			// 
			this.Toolbar_CmdChangeFilterClearAll.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdChangeFilterClearAll.Enabled = false;
			this.Toolbar_CmdChangeFilterClearAll.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdChangeFilterClearAll.Image")));
			this.Toolbar_CmdChangeFilterClearAll.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdChangeFilterClearAll.Name = "Toolbar_CmdChangeFilterClearAll";
			this.Toolbar_CmdChangeFilterClearAll.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdChangeFilterClearAll.Text = "toolStripButtonChangeFilterClearAll";
			this.Toolbar_CmdChangeFilterClearAll.ToolTipText = "Turn off all filters";
			// 
			// toolStripInsert
			// 
			this.toolStripInsert.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStripInsert.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Toolbar_CmdInsertLexEntry,
            this.Toolbar_CmdGoToEntry,
            this.Toolbar_CmdInsertReversalEntry,
            this.Toolbar_CmdGoToReversalEntry,
            this.Toolbar_CmdInsertText,
            this.Toolbar_CmdAddNote,
            this.Toolbar_CmdApproveAllButton,
            this.Toolbar_CmdInsertHumanApprovedAnalysis,
            this.Toolbar_CmdGoToWfiWordform,
            this.Toolbar_CmdFindAndReplaceText,
            this.Toolbar_CmdBreakPhraseButton,
            this.Toolbar_CmdInsertRecord,
            this.Toolbar_CmdGoToRecord,
            this.Toolbar_CmdAddToLexicon,
            this.Toolbar_CmdLexiconLookup,
            this.Toolbar_CmdInsertPossibility,
            this.Toolbar_CmdDataTree_Insert_Possibility,
            this.Toolbar_CmdInsertEndocentricCompound,
            this.Toolbar_CmdInsertExocentricCompound,
            this.Toolbar_CmdInsertExceptionFeature,
            this.Toolbar_CmdInsertPhonologicalClosedFeature,
            this.Toolbar_CmdInsertClosedFeature,
            this.Toolbar_CmdInsertComplexFeature,
            this.Toolbar_CmdInsertPhoneme,
            this.Toolbar_CmdInsertSegmentNaturalClasses,
            this.Toolbar_CmdInsertFeatureNaturalClasses,
            this.Toolbar_CmdInsertPhEnvironment,
            this.Toolbar_CmdInsertPhRegularRule,
            this.Toolbar_CmdInsertPhMetathesisRule,
            this.Toolbar_CmdInsertMorphemeACP,
            this.Toolbar_CmdInsertAllomorphACP,
            this.Toolbar_CmdInsertACPGroup});
			this.toolStripInsert.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.toolStripInsert.Location = new System.Drawing.Point(308, 24);
			this.toolStripInsert.Name = "toolStripInsert";
			this.toolStripInsert.Size = new System.Drawing.Size(111, 25);
			this.toolStripInsert.TabIndex = 3;
			// 
			// Toolbar_CmdInsertLexEntry
			// 
			this.Toolbar_CmdInsertLexEntry.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertLexEntry.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertLexEntry.Image")));
			this.Toolbar_CmdInsertLexEntry.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertLexEntry.Name = "Toolbar_CmdInsertLexEntry";
			this.Toolbar_CmdInsertLexEntry.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertLexEntry.ToolTipText = "Create a new lexical entry.";
			this.Toolbar_CmdInsertLexEntry.Visible = false;
			// 
			// Toolbar_CmdGoToEntry
			// 
			this.Toolbar_CmdGoToEntry.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdGoToEntry.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdGoToEntry.Image")));
			this.Toolbar_CmdGoToEntry.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdGoToEntry.Name = "Toolbar_CmdGoToEntry";
			this.Toolbar_CmdGoToEntry.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdGoToEntry.ToolTipText = "Find a lexical entry.";
			this.Toolbar_CmdGoToEntry.Visible = false;
			// 
			// Toolbar_CmdInsertReversalEntry
			// 
			this.Toolbar_CmdInsertReversalEntry.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertReversalEntry.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertReversalEntry.Image")));
			this.Toolbar_CmdInsertReversalEntry.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertReversalEntry.Name = "Toolbar_CmdInsertReversalEntry";
			this.Toolbar_CmdInsertReversalEntry.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertReversalEntry.ToolTipText = "Create a new reversal entry.";
			this.Toolbar_CmdInsertReversalEntry.Visible = false;
			// 
			// Toolbar_CmdGoToReversalEntry
			// 
			this.Toolbar_CmdGoToReversalEntry.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdGoToReversalEntry.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdGoToReversalEntry.Image")));
			this.Toolbar_CmdGoToReversalEntry.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdGoToReversalEntry.Name = "Toolbar_CmdGoToReversalEntry";
			this.Toolbar_CmdGoToReversalEntry.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdGoToReversalEntry.ToolTipText = "Find a reversal entry.";
			this.Toolbar_CmdGoToReversalEntry.Visible = false;
			// 
			// Toolbar_CmdInsertText
			// 
			this.Toolbar_CmdInsertText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertText.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertText.Image")));
			this.Toolbar_CmdInsertText.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertText.Name = "Toolbar_CmdInsertText";
			this.Toolbar_CmdInsertText.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertText.ToolTipText = "Add a new text to the corpus.";
			this.Toolbar_CmdInsertText.Visible = false;
			// 
			// Toolbar_CmdAddNote
			// 
			this.Toolbar_CmdAddNote.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdAddNote.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdAddNote.Image")));
			this.Toolbar_CmdAddNote.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdAddNote.Name = "Toolbar_CmdAddNote";
			this.Toolbar_CmdAddNote.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdAddNote.ToolTipText = "Insert a note.";
			this.Toolbar_CmdAddNote.Visible = false;
			// 
			// Toolbar_CmdApproveAllButton
			// 
			this.Toolbar_CmdApproveAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdApproveAllButton.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdApproveAllButton.Image")));
			this.Toolbar_CmdApproveAllButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdApproveAllButton.Name = "Toolbar_CmdApproveAllButton";
			this.Toolbar_CmdApproveAllButton.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdApproveAllButton.ToolTipText = "Approve all the suggested analyses in this text.";
			this.Toolbar_CmdApproveAllButton.Visible = false;
			// 
			// Toolbar_CmdInsertHumanApprovedAnalysis
			// 
			this.Toolbar_CmdInsertHumanApprovedAnalysis.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertHumanApprovedAnalysis.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertHumanApprovedAnalysis.Image")));
			this.Toolbar_CmdInsertHumanApprovedAnalysis.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertHumanApprovedAnalysis.Name = "Toolbar_CmdInsertHumanApprovedAnalysis";
			this.Toolbar_CmdInsertHumanApprovedAnalysis.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertHumanApprovedAnalysis.ToolTipText = "Create new approved analysis";
			this.Toolbar_CmdInsertHumanApprovedAnalysis.Visible = false;
			// 
			// Toolbar_CmdGoToWfiWordform
			// 
			this.Toolbar_CmdGoToWfiWordform.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdGoToWfiWordform.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdGoToWfiWordform.Image")));
			this.Toolbar_CmdGoToWfiWordform.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdGoToWfiWordform.Name = "Toolbar_CmdGoToWfiWordform";
			this.Toolbar_CmdGoToWfiWordform.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdGoToWfiWordform.Visible = false;
			// 
			// Toolbar_CmdFindAndReplaceText
			// 
			this.Toolbar_CmdFindAndReplaceText.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdFindAndReplaceText.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdFindAndReplaceText.Image")));
			this.Toolbar_CmdFindAndReplaceText.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdFindAndReplaceText.Name = "Toolbar_CmdFindAndReplaceText";
			this.Toolbar_CmdFindAndReplaceText.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdFindAndReplaceText.ToolTipText = "Find and Replace Text";
			this.Toolbar_CmdFindAndReplaceText.Visible = false;
			// 
			// Toolbar_CmdBreakPhraseButton
			// 
			this.Toolbar_CmdBreakPhraseButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdBreakPhraseButton.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdBreakPhraseButton.Image")));
			this.Toolbar_CmdBreakPhraseButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdBreakPhraseButton.Name = "Toolbar_CmdBreakPhraseButton";
			this.Toolbar_CmdBreakPhraseButton.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdBreakPhraseButton.ToolTipText = "Break selected phrase into words.";
			this.Toolbar_CmdBreakPhraseButton.Visible = false;
			// 
			// Toolbar_CmdInsertRecord
			// 
			this.Toolbar_CmdInsertRecord.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertRecord.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertRecord.Image")));
			this.Toolbar_CmdInsertRecord.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertRecord.Name = "Toolbar_CmdInsertRecord";
			this.Toolbar_CmdInsertRecord.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertRecord.ToolTipText = "Create a new Record in your Notebook.";
			this.Toolbar_CmdInsertRecord.Visible = false;
			// 
			// Toolbar_CmdGoToRecord
			// 
			this.Toolbar_CmdGoToRecord.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdGoToRecord.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdGoToRecord.Image")));
			this.Toolbar_CmdGoToRecord.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdGoToRecord.Name = "Toolbar_CmdGoToRecord";
			this.Toolbar_CmdGoToRecord.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdGoToRecord.ToolTipText = "Find a Record in your Notebook.";
			this.Toolbar_CmdGoToRecord.Visible = false;
			// 
			// Toolbar_CmdAddToLexicon
			// 
			this.Toolbar_CmdAddToLexicon.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdAddToLexicon.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdAddToLexicon.Image")));
			this.Toolbar_CmdAddToLexicon.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdAddToLexicon.Name = "Toolbar_CmdAddToLexicon";
			this.Toolbar_CmdAddToLexicon.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdAddToLexicon.ToolTipText = "Add the current word to the lexicon (if it is a vernacular word).";
			this.Toolbar_CmdAddToLexicon.Visible = false;
			// 
			// Toolbar_CmdLexiconLookup
			// 
			this.Toolbar_CmdLexiconLookup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdLexiconLookup.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdLexiconLookup.Image")));
			this.Toolbar_CmdLexiconLookup.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdLexiconLookup.Name = "Toolbar_CmdLexiconLookup";
			this.Toolbar_CmdLexiconLookup.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdLexiconLookup.ToolTipText = "Show dictionary entry for root/stem of current word, or open a search dialog box." +
    "";
			this.Toolbar_CmdLexiconLookup.Visible = false;
			// 
			// Toolbar_CmdInsertPossibility
			// 
			this.Toolbar_CmdInsertPossibility.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertPossibility.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertPossibility.Image")));
			this.Toolbar_CmdInsertPossibility.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.Toolbar_CmdInsertPossibility.Name = "Toolbar_CmdInsertPossibility";
			this.Toolbar_CmdInsertPossibility.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertPossibility.ToolTipText = "Create a new {0}.";
			this.Toolbar_CmdInsertPossibility.Visible = false;
			// 
			// Toolbar_CmdDataTree_Insert_Possibility
			// 
			this.Toolbar_CmdDataTree_Insert_Possibility.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdDataTree_Insert_Possibility.Image = global::LanguageExplorer.LanguageExplorerResources.Insert_Sub_Cat;
			this.Toolbar_CmdDataTree_Insert_Possibility.ImageTransparentColor = System.Drawing.Color.Transparent;
			this.Toolbar_CmdDataTree_Insert_Possibility.Name = "Toolbar_CmdDataTree_Insert_Possibility";
			this.Toolbar_CmdDataTree_Insert_Possibility.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdDataTree_Insert_Possibility.ToolTipText = "Subitem";
			this.Toolbar_CmdDataTree_Insert_Possibility.Visible = false;
			// 
			// Toolbar_CmdInsertEndocentricCompound
			// 
			this.Toolbar_CmdInsertEndocentricCompound.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertEndocentricCompound.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertEndocentricCompound.Image")));
			this.Toolbar_CmdInsertEndocentricCompound.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertEndocentricCompound.Name = "Toolbar_CmdInsertEndocentricCompound";
			this.Toolbar_CmdInsertEndocentricCompound.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertEndocentricCompound.ToolTipText = "Headed Compound";
			this.Toolbar_CmdInsertEndocentricCompound.Visible = false;
			// 
			// Toolbar_CmdInsertExocentricCompound
			// 
			this.Toolbar_CmdInsertExocentricCompound.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertExocentricCompound.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertExocentricCompound.Image")));
			this.Toolbar_CmdInsertExocentricCompound.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertExocentricCompound.Name = "Toolbar_CmdInsertExocentricCompound";
			this.Toolbar_CmdInsertExocentricCompound.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertExocentricCompound.ToolTipText = "Non-headed Compound";
			this.Toolbar_CmdInsertExocentricCompound.Visible = false;
			// 
			// Toolbar_CmdInsertExceptionFeature
			// 
			this.Toolbar_CmdInsertExceptionFeature.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertExceptionFeature.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertExceptionFeature.Image")));
			this.Toolbar_CmdInsertExceptionFeature.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertExceptionFeature.Name = "Toolbar_CmdInsertExceptionFeature";
			this.Toolbar_CmdInsertExceptionFeature.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertExceptionFeature.ToolTipText = "Create a new {0}.";
			this.Toolbar_CmdInsertExceptionFeature.Visible = false;
			// 
			// Toolbar_CmdInsertPhonologicalClosedFeature
			// 
			this.Toolbar_CmdInsertPhonologicalClosedFeature.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertPhonologicalClosedFeature.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertPhonologicalClosedFeature.Image")));
			this.Toolbar_CmdInsertPhonologicalClosedFeature.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertPhonologicalClosedFeature.Name = "Toolbar_CmdInsertPhonologicalClosedFeature";
			this.Toolbar_CmdInsertPhonologicalClosedFeature.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertPhonologicalClosedFeature.ToolTipText = "Add a phonological feature.";
			this.Toolbar_CmdInsertPhonologicalClosedFeature.Visible = false;
			// 
			// Toolbar_CmdInsertClosedFeature
			// 
			this.Toolbar_CmdInsertClosedFeature.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertClosedFeature.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertClosedFeature.Image")));
			this.Toolbar_CmdInsertClosedFeature.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertClosedFeature.Name = "Toolbar_CmdInsertClosedFeature";
			this.Toolbar_CmdInsertClosedFeature.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertClosedFeature.ToolTipText = "Add a feature.";
			this.Toolbar_CmdInsertClosedFeature.Visible = false;
			// 
			// Toolbar_CmdInsertComplexFeature
			// 
			this.Toolbar_CmdInsertComplexFeature.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertComplexFeature.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertComplexFeature.Image")));
			this.Toolbar_CmdInsertComplexFeature.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertComplexFeature.Name = "Toolbar_CmdInsertComplexFeature";
			this.Toolbar_CmdInsertComplexFeature.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertComplexFeature.ToolTipText = "Add a complex feature.";
			this.Toolbar_CmdInsertComplexFeature.Visible = false;
			// 
			// Toolbar_CmdInsertPhoneme
			// 
			this.Toolbar_CmdInsertPhoneme.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertPhoneme.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertPhoneme.Image")));
			this.Toolbar_CmdInsertPhoneme.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertPhoneme.Name = "Toolbar_CmdInsertPhoneme";
			this.Toolbar_CmdInsertPhoneme.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertPhoneme.ToolTipText = "Create a new phoneme. (CTRL+I)";
			this.Toolbar_CmdInsertPhoneme.Visible = false;
			// 
			// Toolbar_CmdInsertSegmentNaturalClasses
			// 
			this.Toolbar_CmdInsertSegmentNaturalClasses.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertSegmentNaturalClasses.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertSegmentNaturalClasses.Image")));
			this.Toolbar_CmdInsertSegmentNaturalClasses.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertSegmentNaturalClasses.Name = "Toolbar_CmdInsertSegmentNaturalClasses";
			this.Toolbar_CmdInsertSegmentNaturalClasses.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertSegmentNaturalClasses.ToolTipText = "Create a new natural class, defined by listing phonemes.";
			this.Toolbar_CmdInsertSegmentNaturalClasses.Visible = false;
			// 
			// Toolbar_CmdInsertFeatureNaturalClasses
			// 
			this.Toolbar_CmdInsertFeatureNaturalClasses.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertFeatureNaturalClasses.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertFeatureNaturalClasses.Image")));
			this.Toolbar_CmdInsertFeatureNaturalClasses.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertFeatureNaturalClasses.Name = "Toolbar_CmdInsertFeatureNaturalClasses";
			this.Toolbar_CmdInsertFeatureNaturalClasses.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertFeatureNaturalClasses.ToolTipText = "Create a new natural class, defined by features.";
			this.Toolbar_CmdInsertFeatureNaturalClasses.Visible = false;
			// 
			// Toolbar_CmdInsertPhEnvironment
			// 
			this.Toolbar_CmdInsertPhEnvironment.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertPhEnvironment.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertPhEnvironment.Image")));
			this.Toolbar_CmdInsertPhEnvironment.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertPhEnvironment.Name = "Toolbar_CmdInsertPhEnvironment";
			this.Toolbar_CmdInsertPhEnvironment.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertPhEnvironment.ToolTipText = "Create a new environment.";
			this.Toolbar_CmdInsertPhEnvironment.Visible = false;
			// 
			// Toolbar_CmdInsertPhRegularRule
			// 
			this.Toolbar_CmdInsertPhRegularRule.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertPhRegularRule.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertPhRegularRule.Image")));
			this.Toolbar_CmdInsertPhRegularRule.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertPhRegularRule.Name = "Toolbar_CmdInsertPhRegularRule";
			this.Toolbar_CmdInsertPhRegularRule.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertPhRegularRule.ToolTipText = "Create a new phonological rule.";
			this.Toolbar_CmdInsertPhRegularRule.Visible = false;
			// 
			// Toolbar_CmdInsertPhMetathesisRule
			// 
			this.Toolbar_CmdInsertPhMetathesisRule.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertPhMetathesisRule.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertPhMetathesisRule.Image")));
			this.Toolbar_CmdInsertPhMetathesisRule.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertPhMetathesisRule.Name = "Toolbar_CmdInsertPhMetathesisRule";
			this.Toolbar_CmdInsertPhMetathesisRule.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertPhMetathesisRule.ToolTipText = "Create a new metathesis rule.";
			this.Toolbar_CmdInsertPhMetathesisRule.Visible = false;
			// 
			// Toolbar_CmdInsertMorphemeACP
			// 
			this.Toolbar_CmdInsertMorphemeACP.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertMorphemeACP.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertMorphemeACP.Image")));
			this.Toolbar_CmdInsertMorphemeACP.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertMorphemeACP.Name = "Toolbar_CmdInsertMorphemeACP";
			this.Toolbar_CmdInsertMorphemeACP.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertMorphemeACP.ToolTipText = "Create a rule to prevent morphemes from co-occurring.";
			this.Toolbar_CmdInsertMorphemeACP.Visible = false;
			// 
			// Toolbar_CmdInsertAllomorphACP
			// 
			this.Toolbar_CmdInsertAllomorphACP.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertAllomorphACP.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertAllomorphACP.Image")));
			this.Toolbar_CmdInsertAllomorphACP.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertAllomorphACP.Name = "Toolbar_CmdInsertAllomorphACP";
			this.Toolbar_CmdInsertAllomorphACP.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertAllomorphACP.ToolTipText = "Create a rule to prevent allomorphs from co-occurring.";
			this.Toolbar_CmdInsertAllomorphACP.Visible = false;
			// 
			// Toolbar_CmdInsertACPGroup
			// 
			this.Toolbar_CmdInsertACPGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.Toolbar_CmdInsertACPGroup.Image = ((System.Drawing.Image)(resources.GetObject("Toolbar_CmdInsertACPGroup.Image")));
			this.Toolbar_CmdInsertACPGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.Toolbar_CmdInsertACPGroup.Name = "Toolbar_CmdInsertACPGroup";
			this.Toolbar_CmdInsertACPGroup.Size = new System.Drawing.Size(23, 22);
			this.Toolbar_CmdInsertACPGroup.ToolTipText = "Create a group of ad hoc rules.";
			this.Toolbar_CmdInsertACPGroup.Visible = false;
			// 
			// toolStripFormat
			// 
			this.toolStripFormat.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStripFormat.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Toolbar_WritingSystemList,
            this.Toolbar_CombinedStylesList});
			this.toolStripFormat.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.toolStripFormat.Location = new System.Drawing.Point(421, 24);
			this.toolStripFormat.Name = "toolStripFormat";
			this.toolStripFormat.Size = new System.Drawing.Size(289, 25);
			this.toolStripFormat.TabIndex = 4;
			// 
			// Toolbar_WritingSystemList
			// 
			this.Toolbar_WritingSystemList.DropDownWidth = 150;
			this.Toolbar_WritingSystemList.Enabled = false;
			this.Toolbar_WritingSystemList.Name = "Toolbar_WritingSystemList";
			this.Toolbar_WritingSystemList.Size = new System.Drawing.Size(121, 25);
			this.Toolbar_WritingSystemList.ToolTipText = "Writing System";
			// 
			// Toolbar_CombinedStylesList
			// 
			this.Toolbar_CombinedStylesList.DropDownWidth = 250;
			this.Toolbar_CombinedStylesList.Enabled = false;
			this.Toolbar_CombinedStylesList.Name = "Toolbar_CombinedStylesList";
			this.Toolbar_CombinedStylesList.Size = new System.Drawing.Size(121, 25);
			this.Toolbar_CombinedStylesList.ToolTipText = "Styles";
			// 
			// mainContainer
			// 
			this.mainContainer.AccessibleName = "CollapsingSplitContainer";
			this.mainContainer.BackColor = System.Drawing.SystemColors.Control;
			this.mainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainContainer.FirstControl = this._sidePane;
			this.mainContainer.FirstLabel = "Sidebar";
			this.mainContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.mainContainer.IsInitializing = false;
			this.mainContainer.Location = new System.Drawing.Point(0, 0);
			this.mainContainer.Name = "mainContainer";
			// 
			// mainContainer.Panel1
			// 
			this.mainContainer.Panel1.Controls.Add(this._sidePane);
			this.mainContainer.Panel1MinSize = 16;
			// 
			// mainContainer.Panel2
			// 
			this.mainContainer.Panel2.Controls.Add(this._rightPanel);
			this.mainContainer.Panel2MinSize = 16;
			this.mainContainer.SecondControl = this._rightPanel;
			this.mainContainer.SecondLabel = "All Content";
			this.mainContainer.Size = new System.Drawing.Size(791, 381);
			this.mainContainer.SplitterDistance = 140;
			this.mainContainer.TabIndex = 0;
			this.mainContainer.TabStop = false;
			// 
			// _sidePane
			// 
			this._sidePane.Dock = System.Windows.Forms.DockStyle.Fill;
			this._sidePane.Location = new System.Drawing.Point(0, 0);
			this._sidePane.Name = "_sidePane";
			this._sidePane.Size = new System.Drawing.Size(140, 381);
			this._sidePane.TabIndex = 1;
			// 
			// _rightPanel
			// 
			this._rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this._rightPanel.Location = new System.Drawing.Point(0, 0);
			this._rightPanel.Name = "_rightPanel";
			this._rightPanel.Size = new System.Drawing.Size(647, 381);
			this._rightPanel.TabIndex = 1;
			// 
			// FwMainWnd
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(791, 450);
			this.Controls.Add(this.toolStripContainer);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this._menuStrip;
			this.Name = "FwMainWnd";
			this.Text = "FieldWorks Language Explorer";
			this._menuStrip.ResumeLayout(false);
			this._menuStrip.PerformLayout();
			this.toolStripStandard.ResumeLayout(false);
			this.toolStripStandard.PerformLayout();
			this.toolStripContainer.ContentPanel.ResumeLayout(false);
			this.toolStripContainer.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer.TopToolStripPanel.PerformLayout();
			this.toolStripContainer.ResumeLayout(false);
			this.toolStripContainer.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelMessage)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelProgress)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelArea)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanelRecordNumber)).EndInit();
			this.toolStripView.ResumeLayout(false);
			this.toolStripView.PerformLayout();
			this.toolStripInsert.ResumeLayout(false);
			this.toolStripInsert.PerformLayout();
			this.toolStripFormat.ResumeLayout(false);
			this.toolStripFormat.PerformLayout();
			this.mainContainer.Panel1.ResumeLayout(false);
			this.mainContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.mainContainer)).EndInit();
			this.mainContainer.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.MenuStrip _menuStrip;
		private System.Windows.Forms.ToolStripMenuItem _fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _sendReceiveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _dataToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _insertToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _formatToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _toolsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _windowToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripFileMenuSeparator1;
		private System.Windows.Forms.ToolStripMenuItem CmdClose;
		private System.Windows.Forms.ToolStripMenuItem CmdHelpLanguageExplorer;
		private System.Windows.Forms.ToolStripMenuItem CmdHelpTraining;
		private System.Windows.Forms.ToolStripMenuItem CmdHelpDemoMovies;
		private System.Windows.Forms.ToolStripMenuItem HelpMenu_ResourcesMenu;
		private System.Windows.Forms.ToolStripMenuItem CmdHelpNotesSendReceive;
		private System.Windows.Forms.ToolStripSeparator toolStripHelpMenuSeparator1;
		private System.Windows.Forms.ToolStripMenuItem CmdHelpReportBug;
		private System.Windows.Forms.ToolStripMenuItem CmdHelpMakeSuggestion;
		private System.Windows.Forms.ToolStripSeparator toolStripHelpMenuSeparator2;
		private System.Windows.Forms.ToolStripMenuItem CmdHelpAbout;
		private System.Windows.Forms.ToolStripMenuItem CmdNewLangProject;
		private System.Windows.Forms.ToolStripMenuItem CmdChooseLangProject;
		private System.Windows.Forms.ToolStripMenuItem projectManagementToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripFileMenuSeparator6;
		private System.Windows.Forms.ToolStripMenuItem CmdProjectProperties;
		private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripViewMenuSeparator1;
		private System.Windows.Forms.ToolStrip toolStripStandard;
		private System.Windows.Forms.ToolStripButton Toolbar_CmdRefresh;
		private System.Windows.Forms.ToolStripSeparator standardToolStripSeparator3;
		private System.Windows.Forms.ToolStripContainer toolStripContainer;
		private System.Windows.Forms.ToolStripMenuItem configureToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CmdVernacularWritingSystemProperties;
		private System.Windows.Forms.ToolStripMenuItem CmdBackup;
		private System.Windows.Forms.ToolStripMenuItem CmdRestoreFromBackup;
		private System.Windows.Forms.ToolStripSeparator toolStripFileMenuSeparator2;
		private System.Windows.Forms.ToolStripMenuItem CmdProjectLocation;
		private System.Windows.Forms.ToolStripMenuItem CmdDeleteProject;
		private System.Windows.Forms.ToolStripMenuItem CmdCreateProjectShortcut;
		private System.Windows.Forms.ToolStripSeparator toolStripFileMenuSeparator3;
		private System.Windows.Forms.ToolStripMenuItem CmdArchiveWithRamp;
		private System.Windows.Forms.ToolStripSeparator toolStripFileMenuSeparator4;
		private System.Windows.Forms.ToolStripMenuItem CmdPrint;
		private System.Windows.Forms.ToolStripSeparator toolStripFileMenuSeparator5;
		private System.Windows.Forms.ToolStripMenuItem ImportMenu;
		private System.Windows.Forms.ToolStripMenuItem CmdExport;
		private System.Windows.Forms.ToolStripMenuItem CmdImportTranslatedLists;
		private System.Windows.Forms.ToolStripMenuItem newWindowToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem CmdHelpNotesWritingSystems;
		private System.Windows.Forms.ToolStripMenuItem CmdHelpXLingPap;
		private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripEditMenuSeparator1;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripEditMenuSeparator2;
		private System.Windows.Forms.ToolStripButton Toolbar_CmdUndo;
		private System.Windows.Forms.ToolStripButton Toolbar_CmdRedo;
		private System.Windows.Forms.ToolStripMenuItem pasteHyperlinkToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyLocationAsHyperlinkToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripEditMenuSeparator3;
		private System.Windows.Forms.StatusBar _statusbar;
		private StatusBarPanel statusBarPanelMessage;
		private StatusBarPanel statusBarPanelProgress;
		private StatusBarPanel statusBarPanelArea;
		private StatusBarPanel statusBarPanelRecordNumber;
		private ToolStripSeparator toolStripEditMenuSeparator4;
		private ToolStripMenuItem selectAllToolStripMenuItem;
		private ToolStripSeparator standardToolStripSeparator2;
		private LanguageExplorer.Controls.CollapsingSplitContainer mainContainer;
		private Panel _rightPanel;
		private Controls.SilSidePane.SidePane _sidePane;
		private ToolStripSeparator toolMenuSeparator1;
		private ToolStripMenuItem CmdProjectUtilities;
		private ToolStripMenuItem _parserToolStripMenuItem;
		private ToolStripMenuItem CmdParseAllWords;
		private ToolStripMenuItem CmdReparseAllWords;
		private ToolStripMenuItem CmdReInitializeParser;
		private ToolStripMenuItem CmdStopParser;
		private ToolStripSeparator toolStripParserMenuSeparator1;
		private ToolStripMenuItem CmdTryAWord;
		private ToolStripMenuItem CmdParseWordsInCurrentText;
		private ToolStripMenuItem CmdParseCurrentWord;
		private ToolStripMenuItem CmdClearSelectedWordParserAnalyses;
		private ToolStripSeparator toolStripParserMenuSeparator2;
		private ToolStripMenuItem ChooseParserMenu;
		private ToolStripMenuItem CmdChooseXAmpleParser;
		private ToolStripMenuItem CmdChooseHCParser;
		private ToolStripMenuItem CmdEditParserParameters;
		private ToolStripMenuItem _data_First;
		private ToolStripMenuItem _data_Previous;
		private ToolStripMenuItem _data_Next;
		private ToolStripMenuItem _data_Last;
		private ToolStripButton Toolbar_CmdFirstRecord;
		private ToolStripButton Toolbar_CmdPreviousRecord;
		private ToolStripButton Toolbar_CmdNextRecord;
		private ToolStripButton Toolbar_CmdLastRecord;
		private ToolStripMenuItem CmdShowCharMap;
		private ToolStripMenuItem CmdInsertLinkToFile;
		private ToolStrip toolStripFormat;
		private ToolStrip toolStripInsert;
		private ToolStrip toolStripView;
		private ToolStripButton Toolbar_CmdChangeFilterClearAll;
		private ToolStripButton Toolbar_CmdFLExLiftBridge;
		private ToolStripMenuItem deleteToolStripMenuItem;
		private ToolStripButton Toolbar_CmdDeleteRecord;
		private ToolStripButton CmdHistoryBack;
		private ToolStripButton CmdHistoryForward;
		private ToolStripSeparator standardToolStripSeparator1;
		private ToolStripComboBox Toolbar_WritingSystemList;
		private ToolStripComboBox Toolbar_CombinedStylesList;
		private ToolStripMenuItem CmdImportSFMLexicon;
		private ToolStripMenuItem CmdUploadToWebonary;
		private ToolStripSeparator toolMenuSeparator4;
		private ToolStripMenuItem CmdToolsOptions;
		private ToolStripMenuItem CmdFormatStyle;
		private ToolStripMenuItem CmdFormatApplyStyle;
		private ToolStripMenuItem WritingSystemMenu;
		private ToolStripMenuItem CmdRestoreDefaults;
		private ToolStripMenuItem CmdFindAndReplaceText;
		private ToolStripButton Toolbar_CmdFindAndReplaceText;
		private ToolStripMenuItem CmdReplaceText;
		private ToolStripMenuItem filtersToolStripMenuItem;
		private ToolStripMenuItem noFilterToolStripMenuItem;
		private ToolStripSeparator toolStripViewMenuSeparator2;
		private ToolStripMenuItem ToolsMenu_SpellingMenu;
		private ToolStripMenuItem CmdUseVernSpellingDictionary;
		private ToolStripMenuItem CmdHelpLexicographyIntro;
		private ToolStripMenuItem CmdHelpMorphologyIntro;
		private ToolStripMenuItem CmdHelpNotesSFMDatabaseImport;
		private ToolStripMenuItem CmdHelpNotesLinguaLinksDatabaseImport;
		private ToolStripMenuItem CmdHelpNotesInterlinearImport;
		private ToolStripSeparator dataMenuSeparator1;
		private ToolStripMenuItem CmdApproveAndMoveNext;
		private ToolStripMenuItem CmdApproveForWholeTextAndMoveNext;
		private ToolStripMenuItem CmdNextIncompleteBundle;
		private ToolStripMenuItem CmdApprove;
		private ToolStripMenuItem ApproveAnalysisMovementMenu;
		private ToolStripMenuItem CmdApproveAndMoveNextSameLine;
		private ToolStripMenuItem CmdMoveFocusBoxRight;
		private ToolStripMenuItem CmdMoveFocusBoxLeft;
		private ToolStripMenuItem BrowseMovementMenu;
		private ToolStripMenuItem CmdBrowseMoveNext;
		private ToolStripMenuItem CmdNextIncompleteBundleNc;
		private ToolStripMenuItem CmdBrowseMoveNextSameLine;
		private ToolStripMenuItem CmdMoveFocusBoxRightNc;
		private ToolStripMenuItem CmdMoveFocusBoxLeftNc;
		private ToolStripMenuItem CmdMakePhrase;
		private ToolStripMenuItem CmdBreakPhrase;
		private ToolStripSeparator dataMenuSeparator2;
		private ToolStripMenuItem CmdRepeatLastMoveLeft;
		private ToolStripMenuItem CmdRepeatLastMoveRight;
		private ToolStripMenuItem CmdApproveAll;
		private ToolStripButton Toolbar_CmdInsertLexEntry;
		private ToolStripButton Toolbar_CmdGoToEntry;
		private ToolStripButton Toolbar_CmdInsertReversalEntry;
		private ToolStripButton Toolbar_CmdGoToReversalEntry;
		private ToolStripButton Toolbar_CmdInsertText;
		private ToolStripButton Toolbar_CmdAddNote;
		private ToolStripButton Toolbar_CmdInsertHumanApprovedAnalysis;
		private ToolStripButton Toolbar_CmdApproveAllButton;
		private ToolStripButton Toolbar_CmdGoToWfiWordform;
		private ToolStripButton Toolbar_CmdBreakPhraseButton;
		private ToolStripMenuItem CmdImportLinguaLinksData;
		private ToolStripMenuItem CmdImportLiftData;
		private ToolStripMenuItem CmdImportInterlinearSfm;
		private ToolStripMenuItem CmdImportWordsAndGlossesSfm;
		private ToolStripMenuItem CmdImportInterlinearData;
		private ToolStripMenuItem CmdImportSFMNotebook;
		private ToolStripMenuItem CmdExportInterlinear;
		private ToolStripMenuItem CmdExportDiscourseChart;
		private ToolStripMenuItem CmdFLExBridge;
		private ToolStripMenuItem CmdViewMessages;
		private ToolStripMenuItem CmdLiftBridge;
		private ToolStripMenuItem CmdViewLiftMessages;
		private ToolStripSeparator toolStripSendReceiveMenuSeparator1;
		private ToolStripMenuItem CmdObtainAnyFlexBridgeProject;
		private ToolStripMenuItem CmdObtainLiftProject;
		private ToolStripSeparator toolStripSendReceiveMenuSeparator2;
		private ToolStripMenuItem CmdObtainFirstFlexBridgeProject;
		private ToolStripMenuItem CmdObtainFirstLiftProject;
		private ToolStripSeparator toolStripSendReceiveMenuSeparator3;
		private ToolStripMenuItem CmdHelpChorus;
		private ToolStripMenuItem CmdCheckForFlexBridgeUpdates;
		private ToolStripMenuItem CmdHelpAboutFLEXBridge;
		private ToolStripSeparator toolStripEditMenuSeparator5;
		private ToolStripMenuItem CmdGoToEntry;
		private ToolStripMenuItem CmdGoToRecord;
		private ToolStripMenuItem CmdGoToReversalEntry;
		private ToolStripMenuItem CmdGoToWfiWordform;
		private ToolStripMenuItem CmdDeleteCustomList;
		private ToolStripMenuItem CmdChooseTexts;
		private ToolStripMenuItem LexicalToolsList;
		private ToolStripMenuItem WordToolsList;
		private ToolStripMenuItem GrammarToolsList;
		private ToolStripMenuItem NotebookToolsList;
		private ToolStripMenuItem ListsToolsList;
		private ToolStripMenuItem ShowInvisibleSpaces;
		private ToolStripMenuItem Show_DictionaryPubPreview;
		private ToolStripMenuItem CmdInsertLexEntry;
		private ToolStripMenuItem CmdInsertSense;
		private ToolStripMenuItem CmdInsertVariant;
		private ToolStripMenuItem CmdDataTree_Insert_AlternateForm;
		private ToolStripMenuItem CmdInsertReversalEntry;
		private ToolStripMenuItem CmdDataTree_Insert_Pronunciation;
		private ToolStripMenuItem CmdInsertMediaFile;
		private ToolStripMenuItem CmdDataTree_Insert_Etymology;
		private ToolStripMenuItem CmdInsertSubsense;
		private ToolStripMenuItem CmdInsertPicture;
		private ToolStripMenuItem CmdInsertExtNote;
		private ToolStripMenuItem CmdInsertText;
		private ToolStripMenuItem CmdAddNote;
		private ToolStripMenuItem CmdAddWordGlossesToFreeTrans;
		private ToolStripMenuItem CmdGuessWordBreaks;
		private ToolStripMenuItem ClickInsertsInvisibleSpace;
		private ToolStripMenuItem CmdImportWordSet;
		private ToolStripMenuItem CmdInsertRecord;
		private ToolStripMenuItem CmdInsertSubrecord;
		private ToolStripMenuItem CmdInsertSubsubrecord;
		private ToolStripMenuItem CmdAddToLexicon;
		private ToolStripMenuItem CmdInsertHumanApprovedAnalysis;
		private ToolStripMenuItem CmdInsertPossibility;
		private ToolStripMenuItem CmdDataTree_Insert_Possibility;
		private ToolStripMenuItem CmdAddCustomList;
		private ToolStripMenuItem CmdDataTree_Insert_POS_AffixTemplate;
		private ToolStripMenuItem CmdDataTree_Insert_POS_AffixSlot;
		private ToolStripMenuItem CmdDataTree_Insert_POS_InflectionClass;
		private ToolStripMenuItem CmdInsertEndocentricCompound;
		private ToolStripMenuItem CmdInsertExocentricCompound;
		private ToolStripMenuItem CmdInsertExceptionFeature;
		private ToolStripMenuItem CmdInsertPhonologicalClosedFeature;
		private ToolStripMenuItem CmdInsertClosedFeature;
		private ToolStripMenuItem CmdInsertComplexFeature;
		private ToolStripMenuItem CmdDataTree_Insert_ClosedFeature_Value;
		private ToolStripMenuItem CmdInsertPhoneme;
		private ToolStripMenuItem CmdDataTree_Insert_Phoneme_Code;
		private ToolStripMenuItem CmdInsertSegmentNaturalClasses;
		private ToolStripMenuItem CmdInsertFeatureNaturalClasses;
		private ToolStripMenuItem CmdInsertPhEnvironment;
		private ToolStripMenuItem CmdInsertPhRegularRule;
		private ToolStripMenuItem CmdInsertPhMetathesisRule;
		private ToolStripSeparator insertMenuSeparator1;
		private ToolStripMenuItem CmdInsertMorphemeACP;
		private ToolStripMenuItem CmdInsertAllomorphACP;
		private ToolStripMenuItem CmdInsertACPGroup;
		private ToolStripMenuItem CmdAnalysisWritingSystemProperties;
		private ToolStripMenuItem CmdConfigureDictionary;
		private ToolStripMenuItem CmdConfigureInterlinear;
		private ToolStripMenuItem CmdConfigureList;
		private ToolStripMenuItem CmdConfigureXmlDocView;
		private ToolStripMenuItem CmdConfigureColumns;
		private ToolStripMenuItem CmdConfigHeadwordNumbers;
		private ToolStripMenuItem CmdAddCustomField;
		private ToolStripMenuItem CmdMergeEntry;
		private ToolStripMenuItem CmdLexiconLookup;
		private ToolStripMenuItem ITexts_AddWordsToLexicon;
		private ToolStripSeparator toolMenuSeparator2;
		private ToolStripMenuItem CmdEditSpellingStatus;
		private ToolStripMenuItem CmdViewIncorrectWords;
		private ToolStripSeparator toolMenuSeparator3;
		private ToolStripMenuItem CmdChangeSpelling;
		private ToolStripMenuItem CmdMacroF2;
		private ToolStripMenuItem CmdMacroF3;
		private ToolStripMenuItem CmdMacroF4;
		private ToolStripMenuItem CmdMacroF6;
		private ToolStripMenuItem CmdMacroF7;
		private ToolStripMenuItem CmdMacroF8;
		private ToolStripMenuItem CmdMacroF9;
		private ToolStripMenuItem CmdMacroF10;
		private ToolStripMenuItem CmdMacroF11;
		private ToolStripMenuItem CmdMacroF12;
		private ToolStripButton Toolbar_CmdInsertRecord;
		private ToolStripButton Toolbar_CmdGoToRecord;
		private ToolStripButton Toolbar_CmdAddToLexicon;
		private ToolStripButton Toolbar_CmdLexiconLookup;
		private ToolStripButton Toolbar_CmdInsertPossibility;
		private ToolStripButton Toolbar_CmdDataTree_Insert_Possibility;
		private ToolStripButton Toolbar_CmdInsertEndocentricCompound;
		private ToolStripButton Toolbar_CmdInsertExocentricCompound;
		private ToolStripButton Toolbar_CmdInsertExceptionFeature;
		private ToolStripButton Toolbar_CmdInsertPhonologicalClosedFeature;
		private ToolStripButton Toolbar_CmdInsertClosedFeature;
		private ToolStripButton Toolbar_CmdInsertComplexFeature;
		private ToolStripButton Toolbar_CmdInsertPhoneme;
		private ToolStripButton Toolbar_CmdInsertSegmentNaturalClasses;
		private ToolStripButton Toolbar_CmdInsertFeatureNaturalClasses;
		private ToolStripButton Toolbar_CmdInsertPhEnvironment;
		private ToolStripButton Toolbar_CmdInsertPhRegularRule;
		private ToolStripButton Toolbar_CmdInsertPhMetathesisRule;
		private ToolStripButton Toolbar_CmdInsertMorphemeACP;
		private ToolStripButton Toolbar_CmdInsertAllomorphACP;
		private ToolStripButton Toolbar_CmdInsertACPGroup;
		private ToolStripButton Toolbar_CmdChooseTexts;
	}
}