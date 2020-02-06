// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using IWshRuntimeLibrary;
using LanguageExplorer.Archiving;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.SilSidePane;
using LanguageExplorer.Controls.Styles;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorer.SendReceive;
using LanguageExplorer.UtilityTools;
using SIL.Collections;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Resources;
using SIL.IO;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;
using SIL.Reporting;
using SIL.Utils;
using File = System.IO.File;
using FileUtils = SIL.LCModel.Utils.FileUtils;
using Win32 = SIL.FieldWorks.Common.FwUtils.Win32;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Main window class for FW/FLEx.
	/// </summary>
	/// <remarks>
	/// The main control for this window is an instance of CollapsingSplitContainer (mainContainer data member).
	/// "mainContainer" holds the SidePane (_sidePane) in its Panel1/FirstControl (left side).
	/// It holds a tool-specific control in its right side (Panel2/SecondControl).
	/// </remarks>
	[Export(typeof(IFwMainWnd))]
	internal sealed partial class FwMainWnd : Form, IFwMainWnd
	{
		[Import]
		private IAreaRepository _areaRepository;
		[Import]
		private IPropertyTable _propertyTable;
		[Import]
		private IPublisher _publisher;
		[Import]
		private ISubscriber _subscriber;
		[Import]
		private IFlexApp _flexApp;
		[Import]
		private MacroMenuHandler _macroMenuHandler;
		[Import]
		private IdleQueue _idleQueue;
		static bool _inUndoRedo; // true while executing an Undo/Redo command.
		private bool _windowIsCopy;
		private FwLinkArgs _startupLink;
		/// <summary>
		///  Web browser to use in Linux
		/// </summary>
		private string _webBrowserProgramLinux = "firefox";
		private const string SidebarWidthGlobal = "SidebarWidthGlobal";
		private const string AllowInsertLinkToFile = "AllowInsertLinkToFile";
		private IRecordListRepositoryForTools _recordListRepositoryForTools;
		private ActiveViewHelper _viewHelper;
		private IArea _currentArea;
		private ITool _currentTool;
		private LcmStyleSheet _stylesheet;
		private DateTime _lastToolChange = DateTime.MinValue;
		private readonly HashSet<string> _toolsReportedToday = new HashSet<string>();
		private bool _persistWindowSize;
		private ParserMenuManager _parserMenuManager;
		private DataNavigationManager _dataNavigationManager;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private SendReceiveMenuManager _sendReceiveMenuManager;
		private WritingSystemListHandler _writingSystemListHandler;
		private CombinedStylesListHandler _combinedStylesListHandler;
		private LinkHandler _linkHandler;
		private readonly Stack<List<IdleProcessingHelper>> _idleProcessingHelpers = new Stack<List<IdleProcessingHelper>>();
		private ISharedEventHandlers _sharedEventHandlers = new SharedEventHandlers();
		private HashSet<string> _temporaryPropertyNames = new HashSet<string>();
		private static uint s_wm_kmselectlang = Win32.RegisterWindowMessage("WM_KMSELECTLANG");

		/// <summary />
		public FwMainWnd()
		{
			InitializeComponent();
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == s_wm_kmselectlang)
			{
				var focusWnd = Win32.GetFocus();
				if (focusWnd != IntPtr.Zero)
				{
					Win32.SendMessage(focusWnd, m.Msg, m.WParam, m.LParam);
					focusWnd = IntPtr.Zero;
					return; // No need to pass it on to the superclass, since we dealt with it.
				}
			}
			base.WndProc(ref m);
			// In Mono, closing a dialog invokes WM_ACTIVATE on the active form, which then selects
			// its active control.  This swallows keyboard input.  To prevent this, we select the
			// desired control if one has been established so that keyboard input can still be seen
			// by that control.  (See FWNX-785.)
			if (MiscUtils.IsMono && m.Msg == (int)Win32.WinMsgs.WM_ACTIVATE && m.HWnd == Handle &&
				DesiredControl != null && DesiredControl.Visible && DesiredControl.Enabled)
			{
				DesiredControl.Select();
			}
		}

		private static bool SetupCrashDetectorFile()
		{
			if (File.Exists(CrashOnStartupDetectorPathName))
			{
				return true;
			}
			// Create the crash detector file for next time.
			// Make sure the folder exists first.
			Directory.CreateDirectory(CrashOnStartupDetectorPathName.Substring(0, CrashOnStartupDetectorPathName.LastIndexOf(Path.DirectorySeparatorChar)));
			using (var writer = File.CreateText(CrashOnStartupDetectorPathName))
			{
				writer.Close();
			}
			return false;
		}

		private void SetupWindowSizeIfNeeded(Size restoreSize)
		{
			if (restoreSize == Size)
			{
				return;
			}
			// It will be the same as what is now in the file and the prop table,
			// so skip updating the table.
			_persistWindowSize = false;
			Size = restoreSize;
			_persistWindowSize = true;
		}

		private void SetupStylesheet()
		{
			_stylesheet = new LcmStyleSheet();
			_stylesheet.Init(Cache, Cache.LanguageProject.Hvo, LangProjectTags.kflidStyles);
		}

		#region Cache main menu items

		private Dictionary<MainMenu, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>> CacheMenuItems()
		{
			return new Dictionary<MainMenu, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>>
			{
				{ MainMenu.File,  CacheFileMenuItems()},
				{ MainMenu.SendReceive,  CacheSendReceiveMenuItems()},
				{ MainMenu.Edit,  CacheEditMenuItems()},
				{ MainMenu.View,  CacheViewMenuItems()},
				{ MainMenu.Data,  CacheDataMenuItems()},
				{ MainMenu.Insert,  CacheInsertMenuItems()},
				{ MainMenu.Format,  CacheFormatMenuItems()},
				{ MainMenu.Tools,  CacheToolsMenuItems()},
				{ MainMenu.Parser,  CacheParserMenuItems()},
				{ MainMenu.Window,  CacheWindowMenuItems()},
				{ MainMenu.Help,  CacheHelpMenuItems()}
			};
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheFileMenuItems()
		{
			var separatorCache1 = new StandAloneSeparatorMenuBundle(toolStripFileMenuSeparator1, new List<ToolStripMenuItem> { CmdNewLangProject, CmdChooseLangProject }, new List<ToolStripMenuItem> { projectManagementToolStripMenuItem });
			var separatorCache3 = new SeparatorMenuBundle(toolStripFileMenuSeparator3, separatorCache1, new List<ToolStripMenuItem> { CmdArchiveWithRamp, CmdUploadToWebonary });
			var separatorCache4 = new SeparatorMenuBundle(toolStripFileMenuSeparator4, separatorCache3, new List<ToolStripMenuItem> { CmdPrint });
			var separatorCache5 = new SeparatorMenuBundle(toolStripFileMenuSeparator5, separatorCache4, new List<ToolStripMenuItem> { ImportMenu, CmdExport, CmdExportInterlinear, CmdExportDiscourseChart });
			var separatorCache6 = new SeparatorMenuBundle(toolStripFileMenuSeparator6, separatorCache5, new List<ToolStripMenuItem> { CmdClose });
			var separatorCaches = new List<ISeparatorMenuBundle>
			{
				separatorCache1,
				// Sub-menus
				new StandAloneSeparatorMenuBundle(toolStripFileMenuSeparator2, new List<ToolStripMenuItem> { CmdProjectProperties, CmdBackup, CmdRestoreFromBackup }, new List<ToolStripMenuItem> { CmdProjectLocation, CmdDeleteProject, CmdCreateProjectShortcut }),
				separatorCache3,
				separatorCache4,
				separatorCache5,
				separatorCache6
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{ Command.CmdNewLangProject, CmdNewLangProject },
				{ Command.CmdChooseLangProject, CmdChooseLangProject },
				{ Command.Separator1, toolStripFileMenuSeparator1 },
				{ Command.ProjectManagementMenu, projectManagementToolStripMenuItem },
					{ Command.CmdProjectProperties, CmdProjectProperties },
					{ Command.CmdBackup, CmdBackup },
					{ Command.CmdRestoreFromBackup, CmdRestoreFromBackup },
					{ Command.Separator2, toolStripFileMenuSeparator2 },
					{ Command.CmdProjectLocation, CmdProjectLocation },
					{ Command.CmdDeleteProject, CmdDeleteProject },
					{ Command.CmdCreateProjectShortcut, CmdCreateProjectShortcut },
				{ Command.Separator3, toolStripFileMenuSeparator3 },
				{ Command.CmdArchiveWithRamp, CmdArchiveWithRamp },
				{ Command.CmdUploadToWebonary, CmdUploadToWebonary },
				{ Command.Separator4, toolStripFileMenuSeparator4 },
				{ Command.CmdPrint, CmdPrint },
				{ Command.Separator5, toolStripFileMenuSeparator5 },
				{ Command.ImportMenu, ImportMenu },
					{ Command.CmdImportSFMLexicon, CmdImportSFMLexicon },
					{ Command.CmdImportLinguaLinksData, CmdImportLinguaLinksData },
					{ Command.CmdImportLiftData, CmdImportLiftData },
					{ Command.CmdImportInterlinearSfm, CmdImportInterlinearSfm },
					{ Command.CmdImportWordsAndGlossesSfm, CmdImportWordsAndGlossesSfm },
					{ Command.CmdImportInterlinearData, CmdImportInterlinearData },
					{ Command.CmdImportSFMNotebook, CmdImportSFMNotebook },
					{ Command.CmdImportTranslatedLists, CmdImportTranslatedLists },
				{ Command.CmdExport, CmdExport },
				{ Command.CmdExportInterlinear, CmdExportInterlinear },
				{ Command.CmdExportDiscourseChart, CmdExportDiscourseChart },
				{ Command.Separator6, toolStripFileMenuSeparator6 },
				{ Command.CmdClose, CmdClose }
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, separatorCaches);
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheSendReceiveMenuItems()
		{
			var separatorCache1 = new StandAloneSeparatorMenuBundle(toolStripSendReceiveMenuSeparator1,
				new List<ToolStripMenuItem> { CmdFLExBridge, CmdViewMessages, CmdLiftBridge, CmdViewLiftMessages },
				new List<ToolStripMenuItem> { CmdObtainAnyFlexBridgeProject, CmdObtainLiftProject });
			var separatorCache2 = new SeparatorMenuBundle(toolStripSendReceiveMenuSeparator2,
				separatorCache1,
				new List<ToolStripMenuItem> { CmdObtainFirstFlexBridgeProject, CmdObtainFirstLiftProject });
			var separatorCache3 = new SeparatorMenuBundle(toolStripSendReceiveMenuSeparator3,
				separatorCache2,
				new List<ToolStripMenuItem> { CmdHelpChorus, CmdCheckForFlexBridgeUpdates, CmdHelpAboutFLEXBridge });
			var separatorCaches = new List<ISeparatorMenuBundle>
			{
				separatorCache1,
				separatorCache2,
				separatorCache3
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{Command.CmdFLExBridge, CmdFLExBridge},
				{Command.CmdViewMessages, CmdViewMessages},
				{Command.CmdLiftBridge, CmdLiftBridge},
				{Command.CmdViewLiftMessages, CmdViewLiftMessages},
				{Command.Separator1, toolStripSendReceiveMenuSeparator1},
				{Command.CmdObtainAnyFlexBridgeProject, CmdObtainAnyFlexBridgeProject},
				{Command.CmdObtainLiftProject, CmdObtainLiftProject},
				{Command.Separator2, toolStripSendReceiveMenuSeparator2},
				{Command.CmdObtainFirstFlexBridgeProject, CmdObtainFirstFlexBridgeProject},
				{Command.CmdObtainFirstLiftProject, CmdObtainFirstLiftProject},
				{Command.Separator3, toolStripSendReceiveMenuSeparator3},
				{Command.CmdHelpChorus, CmdHelpChorus},
				{Command.CmdCheckForFlexBridgeUpdates, CmdCheckForFlexBridgeUpdates},
				{Command.CmdHelpAboutFLEXBridge, CmdHelpAboutFLEXBridge}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, separatorCaches);
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheEditMenuItems()
		{
			var separatorCache1 = new StandAloneSeparatorMenuBundle(toolStripEditMenuSeparator1,
				new List<ToolStripMenuItem> { undoToolStripMenuItem, redoToolStripMenuItem },
				new List<ToolStripMenuItem> { cutToolStripMenuItem, copyToolStripMenuItem, pasteToolStripMenuItem });
			var separatorCache2 = new SeparatorMenuBundle(toolStripEditMenuSeparator2,
				separatorCache1,
				new List<ToolStripMenuItem> { pasteHyperlinkToolStripMenuItem, copyLocationAsHyperlinkToolStripMenuItem });
			var separatorCache3 = new SeparatorMenuBundle(toolStripEditMenuSeparator3,
				separatorCache2,
				new List<ToolStripMenuItem> { CmdGoToEntry, CmdGoToRecord, CmdFindAndReplaceText, CmdReplaceText });
			var separatorCache4 = new SeparatorMenuBundle(toolStripEditMenuSeparator4,
				separatorCache3,
				new List<ToolStripMenuItem> { selectAllToolStripMenuItem, deleteToolStripMenuItem });
			var separatorCache5 = new SeparatorMenuBundle(toolStripEditMenuSeparator5,
				separatorCache4,
				new List<ToolStripMenuItem> { CmdGoToReversalEntry, CmdGoToWfiWordform, CmdDeleteCustomList });
			var separatorCaches = new List<ISeparatorMenuBundle>
			{
				separatorCache1,
				separatorCache2,
				separatorCache3,
				separatorCache4,
				separatorCache5
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{ Command.CmdUndo, undoToolStripMenuItem},
				{ Command.CmdRedo, redoToolStripMenuItem},
				{ Command.Separator1, toolStripEditMenuSeparator1},
				{ Command.CmdCut, cutToolStripMenuItem},
				{ Command.CmdCopy, copyToolStripMenuItem},
				{ Command.CmdPaste, pasteToolStripMenuItem},
				{ Command.Separator2, toolStripEditMenuSeparator2},
				{ Command.CmdPasteHyperlink, pasteHyperlinkToolStripMenuItem},
				{ Command.CmdCopyLocationAsHyperlink, copyLocationAsHyperlinkToolStripMenuItem},
				{ Command.Separator3, toolStripEditMenuSeparator3},
				{ Command.CmdGoToEntry, CmdGoToEntry},
				{ Command.CmdGoToRecord, CmdGoToRecord},
				{ Command.CmdFindAndReplaceText, CmdFindAndReplaceText},
				{ Command.CmdReplaceText, CmdReplaceText},
				{ Command.Separator4, toolStripEditMenuSeparator4},
				{ Command.CmdSelectAll, selectAllToolStripMenuItem},
				{ Command.CmdDeleteRecord, deleteToolStripMenuItem},
				{ Command.Separator5, toolStripEditMenuSeparator5},
				{ Command.CmdGoToReversalEntry, CmdGoToReversalEntry},
				{ Command.CmdGoToWfiWordform, CmdGoToWfiWordform},
				{ Command.CmdDeleteCustomList, CmdDeleteCustomList}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, separatorCaches);
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheViewMenuItems()
		{
			var separatorCache1 = new StandAloneSeparatorMenuBundle(toolStripViewMenuSeparator1,
				new List<ToolStripMenuItem> { refreshToolStripMenuItem },
				new List<ToolStripMenuItem> { LexicalToolsList, WordToolsList, GrammarToolsList, NotebookToolsList, ListsToolsList, ShowInvisibleSpaces, Show_DictionaryPubPreview });
			var separatorCache2 = new SeparatorMenuBundle(toolStripViewMenuSeparator2,
				separatorCache1,
				new List<ToolStripMenuItem> { filtersToolStripMenuItem, CmdChooseTexts });
			var separatorCaches = new List<ISeparatorMenuBundle>
			{
				separatorCache1,
				separatorCache2
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{Command.CmdRefresh, refreshToolStripMenuItem},
				{Command.Separator1, toolStripViewMenuSeparator1},
				{Command.LexicalToolsList, LexicalToolsList},
				{Command.WordToolsList, WordToolsList},
				{Command.GrammarToolsList, GrammarToolsList},
				{Command.NotebookToolsList, NotebookToolsList},
				{Command.ListsToolsList, ListsToolsList},
				{Command.ShowInvisibleSpaces, ShowInvisibleSpaces},
				{Command.Show_DictionaryPubPreview, Show_DictionaryPubPreview},
				{Command.Separator2, toolStripViewMenuSeparator2},
				{Command.FiltersList, filtersToolStripMenuItem},
					{Command.NoFilter, noFilterToolStripMenuItem},
				{Command.CmdChooseTexts, CmdChooseTexts}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, separatorCaches);
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheDataMenuItems()
		{
			var separatorCache1 = new StandAloneSeparatorMenuBundle(dataMenuSeparator1,
				new List<ToolStripMenuItem> { _data_First, _data_Previous, _data_Next, _data_Last },
				new List<ToolStripMenuItem> { CmdApproveAndMoveNext, CmdApproveForWholeTextAndMoveNext, CmdNextIncompleteBundle, CmdApprove, ApproveAnalysisMovementMenu,
					BrowseMovementMenu, CmdMakePhrase, CmdBreakPhrase });
			var separatorCache2 = new SeparatorMenuBundle(dataMenuSeparator2,
				separatorCache1,
				new List<ToolStripMenuItem> { CmdRepeatLastMoveLeft, CmdRepeatLastMoveRight, CmdApproveAll });
			var separatorCaches = new List<ISeparatorMenuBundle>
			{
				separatorCache1,
				separatorCache2
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{Command.CmdFirstRecord, _data_First},
				{Command.CmdPreviousRecord, _data_Previous},
				{Command.CmdNextRecord, _data_Next},
				{Command.CmdLastRecord, _data_Last},
				{Command.Separator1, dataMenuSeparator1},
				{Command.CmdApproveAndMoveNext, CmdApproveAndMoveNext},
				{Command.CmdApproveForWholeTextAndMoveNext, CmdApproveForWholeTextAndMoveNext},
				{Command.CmdNextIncompleteBundle, CmdNextIncompleteBundle},
				{Command.CmdApprove, CmdApprove},
				{Command.ApproveAnalysisMovementMenu, ApproveAnalysisMovementMenu},
					{Command.CmdApproveAndMoveNextSameLine, CmdApproveAndMoveNextSameLine},
					{Command.CmdMoveFocusBoxRight, CmdMoveFocusBoxRight},
					{Command.CmdMoveFocusBoxLeft, CmdMoveFocusBoxLeft},
				{Command.BrowseMovementMenu, BrowseMovementMenu},
					{Command.CmdBrowseMoveNext, CmdBrowseMoveNext},
					{Command.CmdNextIncompleteBundleNc, CmdNextIncompleteBundleNc},
					{Command.CmdBrowseMoveNextSameLine, CmdBrowseMoveNextSameLine},
					{Command.CmdMoveFocusBoxRightNc, CmdMoveFocusBoxRightNc},
					{Command.CmdMoveFocusBoxLeftNc, CmdMoveFocusBoxLeftNc},
				{Command.CmdMakePhrase, CmdMakePhrase},
				{Command.CmdBreakPhrase, CmdBreakPhrase},
				{Command.Separator2, dataMenuSeparator2},
				{Command.CmdRepeatLastMoveLeft, CmdRepeatLastMoveLeft},
				{Command.CmdRepeatLastMoveRight, CmdRepeatLastMoveRight},
				{Command.CmdApproveAll, CmdApproveAll}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, separatorCaches);
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheInsertMenuItems()
		{
			var separatorCache = new StandAloneSeparatorMenuBundle(insertMenuSeparator1,
				new List<ToolStripMenuItem> { CmdInsertLexEntry, CmdInsertSense, CmdInsertVariant, CmdDataTree_Insert_AlternateForm, CmdInsertReversalEntry, CmdDataTree_Insert_Pronunciation,
					CmdInsertMediaFile, CmdDataTree_Insert_Etymology, CmdInsertSubsense, CmdInsertPicture, CmdInsertExtNote, CmdInsertText, CmdAddNote, CmdAddWordGlossesToFreeTrans,
					ClickInsertsInvisibleSpace, CmdGuessWordBreaks, CmdImportWordSet, CmdInsertHumanApprovedAnalysis, CmdInsertRecord, CmdInsertSubrecord, CmdInsertSubsubrecord, CmdAddToLexicon,
					CmdInsertPossibility, CmdDataTree_Insert_Possibility, CmdAddCustomList, CmdDataTree_Insert_POS_AffixTemplate, CmdDataTree_Insert_POS_AffixSlot,
					CmdDataTree_Insert_POS_InflectionClass, CmdInsertEndocentricCompound, CmdInsertExocentricCompound, CmdInsertExceptionFeature, CmdInsertPhonologicalClosedFeature,
					CmdInsertClosedFeature, CmdInsertComplexFeature, CmdDataTree_Insert_ClosedFeature_Value, CmdInsertPhoneme, CmdDataTree_Insert_Phoneme_Code, CmdInsertSegmentNaturalClasses,
					CmdInsertFeatureNaturalClasses, CmdInsertPhEnvironment, CmdInsertPhRegularRule, CmdInsertPhMetathesisRule,
					CmdInsertMorphemeACP, CmdInsertAllomorphACP, CmdInsertACPGroup
				},
				new List<ToolStripMenuItem> { CmdShowCharMap, CmdInsertLinkToFile });
			var separatorCaches = new List<ISeparatorMenuBundle>
			{
				separatorCache
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{Command.CmdInsertLexEntry, CmdInsertLexEntry},
				{Command.CmdInsertSense, CmdInsertSense},
				{Command.CmdInsertVariant, CmdInsertVariant},
				{Command.CmdDataTree_Insert_AlternateForm, CmdDataTree_Insert_AlternateForm},
				{Command.CmdInsertReversalEntry, CmdInsertReversalEntry},
				{Command.CmdDataTree_Insert_Pronunciation, CmdDataTree_Insert_Pronunciation},
				{Command.CmdInsertMediaFile, CmdInsertMediaFile},
				{Command.CmdDataTree_Insert_Etymology, CmdDataTree_Insert_Etymology},
				{Command.CmdInsertSubsense, CmdInsertSubsense},
				{Command.CmdInsertPicture, CmdInsertPicture},
				{Command.CmdInsertExtNote, CmdInsertExtNote},
				{Command.CmdInsertText, CmdInsertText},
				{Command.CmdAddNote, CmdAddNote},
				{Command.CmdAddWordGlossesToFreeTrans, CmdAddWordGlossesToFreeTrans},
				{Command.ClickInvisibleSpace, ClickInsertsInvisibleSpace},
				{Command.CmdGuessWordBreaks, CmdGuessWordBreaks},
				{Command.CmdImportWordSet, CmdImportWordSet},
				{Command.CmdInsertHumanApprovedAnalysis, CmdInsertHumanApprovedAnalysis},
				{Command.CmdInsertRecord, CmdInsertRecord},
				{Command.CmdInsertSubrecord, CmdInsertSubrecord},
				{Command.CmdInsertSubsubrecord, CmdInsertSubsubrecord},
				{Command.CmdAddToLexicon, CmdAddToLexicon},
				{Command.CmdInsertPossibility, CmdInsertPossibility},
				{Command.CmdInsertFeatureType, CmdInsertPossibility},
				{Command.CmdDataTree_Insert_Possibility, CmdDataTree_Insert_Possibility},
				{Command.CmdAddCustomList, CmdAddCustomList},
				{Command.CmdDataTree_Insert_POS_AffixTemplate, CmdDataTree_Insert_POS_AffixTemplate},
				{Command.CmdDataTree_Insert_POS_AffixSlot, CmdDataTree_Insert_POS_AffixSlot},
				{Command.CmdDataTree_Insert_POS_InflectionClass, CmdDataTree_Insert_POS_InflectionClass},
				{Command.CmdInsertEndocentricCompound, CmdInsertEndocentricCompound},
				{Command.CmdInsertExocentricCompound, CmdInsertExocentricCompound},
				{Command.CmdInsertExceptionFeature, CmdInsertExceptionFeature},
				{Command.CmdInsertPhonologicalClosedFeature, CmdInsertPhonologicalClosedFeature},
				{Command.CmdInsertClosedFeature, CmdInsertClosedFeature},
				{Command.CmdInsertComplexFeature, CmdInsertComplexFeature},
				{Command.CmdDataTree_Insert_ClosedFeature_Value, CmdDataTree_Insert_ClosedFeature_Value},
				{Command.CmdInsertPhoneme, CmdInsertPhoneme},
				{Command.CmdDataTree_Insert_Phoneme_Code, CmdDataTree_Insert_Phoneme_Code},
				{Command.CmdInsertSegmentNaturalClasses, CmdInsertSegmentNaturalClasses},
				{Command.CmdInsertFeatureNaturalClasses, CmdInsertFeatureNaturalClasses},
				{Command.CmdInsertPhEnvironment, CmdInsertPhEnvironment},
				{Command.CmdInsertPhRegularRule, CmdInsertPhRegularRule},
				{Command.CmdInsertPhMetathesisRule, CmdInsertPhMetathesisRule},
				{Command.CmdInsertMorphemeACP, CmdInsertMorphemeACP},
				{Command.CmdInsertAllomorphACP, CmdInsertAllomorphACP},
				{Command.CmdInsertACPGroup, CmdInsertACPGroup},
				{Command.Separator1, insertMenuSeparator1},
				{Command.CmdShowCharMap, CmdShowCharMap},
				{Command.CmdInsertLinkToFile, CmdInsertLinkToFile}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, separatorCaches);
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheFormatMenuItems()
		{
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{Command.CmdFormatStyle, CmdFormatStyle},
				{Command.CmdFormatApplyStyle, CmdFormatApplyStyle},
				{Command.WritingSystemList, WritingSystemMenu},
				{Command.CmdVernacularWritingSystemProperties, CmdVernacularWritingSystemProperties},
				{Command.CmdAnalysisWritingSystemProperties, CmdAnalysisWritingSystemProperties}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, new List<ISeparatorMenuBundle>());
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheToolsMenuItems()
		{
			var separatorCache1 = new StandAloneSeparatorMenuBundle(toolMenuSeparator1,
				new List<ToolStripMenuItem> { configureToolStripMenuItem },
				new List<ToolStripMenuItem> { CmdMergeEntry, CmdLexiconLookup });
			var separatorCache2 = new SeparatorMenuBundle(toolMenuSeparator2,
				separatorCache1,
				new List<ToolStripMenuItem> { ToolsMenu_SpellingMenu, CmdProjectUtilities });
			var separatorCache3 = new StandAloneSeparatorMenuBundle(toolMenuSeparator3,
				new List<ToolStripMenuItem> { CmdEditSpellingStatus, CmdViewIncorrectWords, CmdUseVernSpellingDictionary },
				new List<ToolStripMenuItem> { CmdChangeSpelling });
			var separatorCache4 = new SeparatorMenuBundle(toolMenuSeparator4,
				separatorCache2, // Skip 3 which is in the sub-menu.
				new List<ToolStripMenuItem> { CmdToolsOptions, CmdMacroF2, CmdMacroF3, CmdMacroF4, CmdMacroF6, CmdMacroF7, CmdMacroF8, CmdMacroF9, CmdMacroF10, CmdMacroF11, CmdMacroF12 });
			var separatorCaches = new List<ISeparatorMenuBundle>
			{
				separatorCache1,
				separatorCache2,
				separatorCache3,
				separatorCache4
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{ Command.Configure, configureToolStripMenuItem },
					{ Command.CmdConfigureDictionary, CmdConfigureDictionary },
					{ Command.CmdConfigureInterlinear, CmdConfigureInterlinear },
					{ Command.CmdConfigureXmlDocView, CmdConfigureXmlDocView },
					{ Command.CmdConfigureList, CmdConfigureList },
					{ Command.CmdConfigureColumns, CmdConfigureColumns },
					{ Command.CmdConfigHeadwordNumbers, CmdConfigHeadwordNumbers },
					{ Command.CmdRestoreDefaults, CmdRestoreDefaults },
					{ Command.CmdAddCustomField, CmdAddCustomField },
				{ Command.Separator1, toolMenuSeparator1 },
				{ Command.CmdMergeEntry, CmdMergeEntry },
				{ Command.CmdLexiconLookup, CmdLexiconLookup },
				{ Command.Separator2, toolMenuSeparator2 },
				{ Command.Spelling, ToolsMenu_SpellingMenu },
					{ Command.CmdEditSpellingStatus, CmdEditSpellingStatus },
					{ Command.CmdViewIncorrectWords, CmdViewIncorrectWords },
					{ Command.CmdUseVernSpellingDictionary, CmdUseVernSpellingDictionary },
					{ Command.Separator3, toolMenuSeparator3 },
					{ Command.CmdChangeSpelling, CmdChangeSpelling },
				{ Command.CmdProjectUtilities, CmdProjectUtilities },
				{ Command.Separator4, toolMenuSeparator4 },
				{ Command.CmdToolsOptions, CmdToolsOptions },
				{ Command.CmdMacroF2, CmdMacroF2 },
				{ Command.CmdMacroF3, CmdMacroF3 },
				{ Command.CmdMacroF4, CmdMacroF4 },
				{ Command.CmdMacroF6, CmdMacroF6 },
				{ Command.CmdMacroF7, CmdMacroF7 },
				{ Command.CmdMacroF8, CmdMacroF8 },
				{ Command.CmdMacroF9, CmdMacroF9 },
				{ Command.CmdMacroF10, CmdMacroF10 },
				{ Command.CmdMacroF11, CmdMacroF11 },
				{ Command.CmdMacroF12, CmdMacroF12 }
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, separatorCaches);
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheParserMenuItems()
		{
			var separatorCache1 = new StandAloneSeparatorMenuBundle(toolStripParserMenuSeparator1,
				new List<ToolStripMenuItem> { CmdParseAllWords, CmdReparseAllWords, CmdReInitializeParser, CmdStopParser },
				new List<ToolStripMenuItem> { CmdTryAWord, CmdParseWordsInCurrentText, CmdParseCurrentWord, CmdClearSelectedWordParserAnalyses });
			var separatorCache2 = new SeparatorMenuBundle(toolStripParserMenuSeparator2,
				separatorCache1,
				new List<ToolStripMenuItem> { ChooseParserMenu, CmdEditParserParameters });
			var separatorCaches = new List<ISeparatorMenuBundle>
			{
				separatorCache1,
				separatorCache2
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{Command.CmdParseAllWords, CmdParseAllWords},
				{Command.CmdReparseAllWords, CmdReparseAllWords},
				{Command.CmdReInitializeParser, CmdReInitializeParser},
				{Command.CmdStopParser, CmdStopParser},
				{Command.Separator1, toolStripParserMenuSeparator1},
				{Command.CmdTryAWord, CmdTryAWord},
				{Command.CmdParseWordsInCurrentText, CmdParseWordsInCurrentText},
				{Command.CmdParseCurrentWord, CmdParseCurrentWord},
				{Command.CmdClearSelectedWordParserAnalyses, CmdClearSelectedWordParserAnalyses},
				{Command.Separator2, toolStripParserMenuSeparator2},
				{Command.ChooseParserMenu, ChooseParserMenu},
					{Command.CmdChooseXAmpleParser, CmdChooseXAmpleParser},
					{Command.CmdChooseHCParser, CmdChooseHCParser},
				{Command.CmdEditParserParameters, CmdEditParserParameters}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, separatorCaches);
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheWindowMenuItems()
		{
			// An ordinary event handler on the window does this.
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{Command.CmdNewWindow,  newWindowToolStripMenuItem}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, new List<ISeparatorMenuBundle>());
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>> CacheHelpMenuItems()
		{
			var separatorCache1 = new StandAloneSeparatorMenuBundle(toolStripHelpMenuSeparator1,
				new List<ToolStripMenuItem> { CmdHelpLanguageExplorer, CmdHelpTraining, HelpMenu_ResourcesMenu },
				new List<ToolStripMenuItem> { CmdHelpReportBug, CmdHelpMakeSuggestion });
			var separatorCache2 = new SeparatorMenuBundle(toolStripHelpMenuSeparator2,
				separatorCache1,
				new List<ToolStripMenuItem> { CmdHelpAbout });
			var separatorCaches = new List<ISeparatorMenuBundle>
			{
				separatorCache1,
				separatorCache2
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{Command.CmdHelpLanguageExplorer,  CmdHelpLanguageExplorer},
				{Command.CmdHelpTraining,  CmdHelpTraining},
				{Command.CmdHelpDemoMovies,  CmdHelpDemoMovies},
				{Command.Resources,  HelpMenu_ResourcesMenu},
					{Command.CmdHelpLexicographyIntro,  CmdHelpLexicographyIntro},
					{Command.CmdHelpMorphologyIntro,  CmdHelpMorphologyIntro},
					{Command.CmdHelpNotesSendReceive,  CmdHelpNotesSendReceive},
					{Command.CmdHelpNotesSFMDatabaseImport,  CmdHelpNotesSFMDatabaseImport},
					{Command.CmdHelpNotesLinguaLinksDatabaseImport,  CmdHelpNotesLinguaLinksDatabaseImport},
					{Command.CmdHelpNotesInterlinearImport,  CmdHelpNotesInterlinearImport},
					{Command.CmdHelpNotesWritingSystems,  CmdHelpNotesWritingSystems},
					{Command.CmdHelpXLingPap,  CmdHelpXLingPap},
				{Command.Separator1,  toolStripHelpMenuSeparator1},
				{Command.CmdHelpReportBug,  CmdHelpReportBug},
				{Command.CmdHelpMakeSuggestion,  CmdHelpMakeSuggestion},
				{Command.Separator2,  toolStripHelpMenuSeparator2},
				{Command.CmdHelpAbout, CmdHelpAbout }
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorMenuBundle>>(commandMap, separatorCaches);
		}

		#endregion Cache main menu items

		#region Cache tool bar items

		private Dictionary<ToolBar, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>>> CacheToolbarItems()
		{
			return new Dictionary<ToolBar, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>>>
			{
				{ ToolBar.Standard,  CacheStandardToolStrip()},
				{ ToolBar.View,  CacheViewToolStrip()},
				{ ToolBar.Insert,  CacheInsertToolStrip()},
				{ ToolBar.Format,  CacheFormatToolStrip()}
			};
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>> CacheStandardToolStrip()
		{
			var separatorCache1 = new StandAloneSeparatorToolStripBundle(standardToolStripSeparator1, new List<ToolStripItem> { CmdHistoryBack, CmdHistoryForward }, new List<ToolStripItem> { Toolbar_CmdDeleteRecord });
			var separatorCache2 = new SeparatorToolStripBundle(standardToolStripSeparator2, separatorCache1, new List<ToolStripItem> { Toolbar_CmdUndo, Toolbar_CmdRedo, Toolbar_CmdRefresh });
			var separatorCache3 = new SeparatorToolStripBundle(standardToolStripSeparator3, separatorCache2, new List<ToolStripItem>
			{
				Toolbar_CmdFirstRecord, Toolbar_CmdPreviousRecord,
				Toolbar_CmdNextRecord, Toolbar_CmdLastRecord, Toolbar_CmdFLExLiftBridge
			});
			var separatorCaches = new List<ISeparatorToolStripBundle>
			{
				separatorCache1,
				separatorCache2,
				separatorCache3
			};
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{Command.CmdHistoryBack, CmdHistoryBack},
				{Command.CmdHistoryForward, CmdHistoryForward},
				{Command.Separator1, standardToolStripSeparator1},
				{Command.CmdDeleteRecord, Toolbar_CmdDeleteRecord},
				{Command.Separator2, standardToolStripSeparator2},
				{Command.CmdUndo, Toolbar_CmdUndo},
				{Command.CmdRedo, Toolbar_CmdRedo},
				{Command.CmdRefresh, Toolbar_CmdRefresh},
				{Command.Separator3, standardToolStripSeparator3},
				{Command.CmdFirstRecord, Toolbar_CmdFirstRecord},
				{Command.CmdPreviousRecord, Toolbar_CmdPreviousRecord},
				{Command.CmdNextRecord, Toolbar_CmdNextRecord},
				{Command.CmdLastRecord, Toolbar_CmdLastRecord},
				{Command.CmdFLExLiftBridge, Toolbar_CmdFLExLiftBridge}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>>(commandMap, separatorCaches);
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>> CacheViewToolStrip()
		{
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{ Command.CmdChooseTexts,  Toolbar_CmdChooseTexts},
				{ Command.CmdChangeFilterClearAll,  Toolbar_CmdChangeFilterClearAll} // Event handled by main window
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>>(commandMap, new List<ISeparatorToolStripBundle>());
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>> CacheInsertToolStrip()
		{
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{ Command.CmdInsertLexEntry, Toolbar_CmdInsertLexEntry},
				{ Command.CmdGoToEntry, Toolbar_CmdGoToEntry},
				{ Command.CmdInsertReversalEntry, Toolbar_CmdInsertReversalEntry},
				{ Command.CmdGoToReversalEntry, Toolbar_CmdGoToReversalEntry},
				{ Command.CmdInsertText, Toolbar_CmdInsertText},
				{ Command.CmdAddNote, Toolbar_CmdAddNote},
				{ Command.CmdApproveAll, Toolbar_CmdApproveAllButton}, // Image is locked in InterlinearImageHolder
				{ Command.CmdInsertHumanApprovedAnalysis, Toolbar_CmdInsertHumanApprovedAnalysis},
				{ Command.CmdGoToWfiWordform, Toolbar_CmdGoToWfiWordform},
				{ Command.CmdFindAndReplaceText, Toolbar_CmdFindAndReplaceText},
				{ Command.CmdBreakPhrase, Toolbar_CmdBreakPhraseButton},
				{ Command.CmdInsertRecord, Toolbar_CmdInsertRecord},
				{ Command.CmdGoToRecord, Toolbar_CmdGoToRecord},
				{ Command.CmdAddToLexicon, Toolbar_CmdAddToLexicon},
				{ Command.CmdLexiconLookup, Toolbar_CmdLexiconLookup},
				{ Command.CmdInsertPossibility, Toolbar_CmdInsertPossibility},
				{ Command.CmdInsertFeatureType, Toolbar_CmdInsertPossibility},
				{ Command.CmdDataTree_Insert_Possibility, Toolbar_CmdDataTree_Insert_Possibility},
				{ Command.CmdInsertEndocentricCompound, Toolbar_CmdInsertEndocentricCompound},
				{ Command.CmdInsertExocentricCompound, Toolbar_CmdInsertExocentricCompound},
				{ Command.CmdInsertExceptionFeature, Toolbar_CmdInsertExceptionFeature},
				{ Command.CmdInsertPhonologicalClosedFeature, Toolbar_CmdInsertPhonologicalClosedFeature},
				{ Command.CmdInsertClosedFeature, Toolbar_CmdInsertClosedFeature},
				{ Command.CmdInsertComplexFeature, Toolbar_CmdInsertComplexFeature},
				{ Command.CmdInsertPhoneme, Toolbar_CmdInsertPhoneme},
				{ Command.CmdInsertSegmentNaturalClasses, Toolbar_CmdInsertSegmentNaturalClasses},
				{ Command.CmdInsertFeatureNaturalClasses, Toolbar_CmdInsertFeatureNaturalClasses},
				{ Command.CmdInsertPhEnvironment, Toolbar_CmdInsertPhEnvironment},
				{ Command.CmdInsertPhRegularRule, Toolbar_CmdInsertPhRegularRule},
				{ Command.CmdInsertPhMetathesisRule, Toolbar_CmdInsertPhMetathesisRule},
				{ Command.CmdInsertMorphemeACP, Toolbar_CmdInsertMorphemeACP},
				{ Command.CmdInsertAllomorphACP, Toolbar_CmdInsertAllomorphACP},
				{ Command.CmdInsertACPGroup, Toolbar_CmdInsertACPGroup}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>>(commandMap, new List<ISeparatorToolStripBundle>());
		}

		private Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>> CacheFormatToolStrip()
		{
			var commandMap = new Dictionary<Command, ToolStripItem>
			{
				{ Command.WritingSystemList,  Toolbar_WritingSystemList},
				{ Command.CombinedStylesList,  Toolbar_CombinedStylesList}
			};
			return new Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>>(commandMap, new List<ISeparatorToolStripBundle>());
		}

		#endregion Cache tool bar items

		/// <summary>
		/// Gets the pathname of the "crash detector" file.
		/// The file is created at app startup, and deleted at app exit,
		/// so if it exists before being created, the app didn't close properly,
		/// and will start without using the saved settings.
		/// </summary>
		private static string CrashOnStartupDetectorPathName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SIL", "FieldWorks", "CrashOnStartupDetector.tmp");

		private void RestoreWindowSettings(bool wasCrashDuringPreviousStartup)
		{
			const string id = "Language Explorer";
			var useExtantPrefs = ModifierKeys != Keys.Shift; // Holding shift key means don't use extant preference file, no matter what.
			if (useExtantPrefs)
			{
				// Tentatively RestoreProperties, but first check for a previous crash...
				if (wasCrashDuringPreviousStartup && (Environment.ExpandEnvironmentVariables("%FwSkipSafeMode%") == "%FwSkipSafeMode%"))
				{
					// Note: The environment variable will not exist at all to get here.
					// A non-developer user.
					// Display a message box asking users if they
					// want to  revert to factory settings.
					ShowInTaskbar = true;
					var activeForm = ActiveForm;
					if (activeForm == null)
					{
						useExtantPrefs = (MessageBox.Show(ActiveForm, LanguageExplorerResources.SomethingWentWrongLastTime,
							id, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes);
					}
					else
					{
						// Make sure as far as possible it comes up in front of any active window, including the splash screen.
						activeForm.Invoke((Action)(() => useExtantPrefs = (MessageBox.Show(LanguageExplorerResources.SomethingWentWrongLastTime,
								id, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)));
					}
				}
			}
			if (useExtantPrefs)
			{
				RestoreProperties();
			}
			else
			{
				DiscardProperties();
			}
			FormWindowState state;
			if (PropertyTable.TryGetValue(LanguageExplorerConstants.windowState, out state, SettingsGroup.GlobalSettings) && state != FormWindowState.Minimized)
			{
				WindowState = state;
			}
			Point persistedLocation;
			if (PropertyTable.TryGetValue(LanguageExplorerConstants.windowLocation, out persistedLocation, SettingsGroup.GlobalSettings))
			{
				Location = persistedLocation;
				//the location restoration only works if the window startposition is set to "manual"
				//because the window is not visible yet, and the location will be changed
				//when it is Show()n.
				StartPosition = FormStartPosition.Manual;
			}
			Size persistedSize;
			if (!PropertyTable.TryGetValue(LanguageExplorerConstants.windowSize, out persistedSize, SettingsGroup.GlobalSettings))
			{
				return;
			}
			_persistWindowSize = false;
			try
			{
				Size = persistedSize;
			}
			finally
			{
				_persistWindowSize = true;
			}
		}

		private void SaveWindowSettings()
		{
			if (!_persistWindowSize)
			{
				return;
			}
			// Don't bother storing anything if we are maximized or minimized.
			// if we did, then when the user exits the application and then runs it again,
			//then switches to the normal state, we would be switching to 0,0 or something.
			if (WindowState != FormWindowState.Normal)
			{
				return;
			}
			PropertyTable.SetProperty(LanguageExplorerConstants.windowState, WindowState, true, settingsGroup: SettingsGroup.GlobalSettings);
			PropertyTable.SetProperty(LanguageExplorerConstants.windowLocation, Location, true, settingsGroup: SettingsGroup.GlobalSettings);
			PropertyTable.SetProperty(LanguageExplorerConstants.windowSize, Size, true, settingsGroup: SettingsGroup.GlobalSettings);
		}

		/// <summary>
		/// Restore properties persisted for the mediator.
		/// </summary>
		private void RestoreProperties()
		{
			PropertyTable.RestoreFromFile(PropertyTable.GlobalSettingsId);
			PropertyTable.RestoreFromFile(PropertyTable.LocalSettingsId);
			var hcSettings = PropertyTable.GetValue<string>(LanguageExplorerConstants.HomographConfiguration);
			if (string.IsNullOrEmpty(hcSettings))
			{
				return;
			}
			var hc = Cache.ServiceLocator.GetInstance<HomographConfiguration>();
			hc.PersistData = hcSettings;
		}

		/// <summary>
		/// If we are discarding saved settings, we must not keep any saved sort sequences,
		/// as they may represent a filter we are not restoring (LT-11647)
		/// </summary>
		private void DiscardProperties()
		{
			var tempDirectory = Path.Combine(Cache.ProjectId.ProjectFolder, LcmFileHelper.ksSortSequenceTempDir);
			RobustIO.DeleteDirectoryAndContents(tempDirectory);
		}

		private void SetupOutlookBar()
		{
			mainContainer.SuspendLayout();
			mainContainer.Tag = SidebarWidthGlobal;
			mainContainer.Panel1MinSize = CollapsingSplitContainer.kCollapsedSize;
			mainContainer.Panel1Collapsed = false;
			mainContainer.Panel2Collapsed = false;
			var sd = PropertyTable.GetValue<int>(SidebarWidthGlobal, SettingsGroup.GlobalSettings);
			if (!mainContainer.Panel1Collapsed)
			{
				SetSplitContainerDistance(mainContainer, sd);
			}
			_sidePane.Initalize(_areaRepository, _viewToolStripMenuItem, View_Area_Tool_Clicked);
			mainContainer.ResumeLayout(false);
		}

		private void View_Area_Tool_Clicked(object sender, EventArgs e)
		{
			var selectInformation = (Tuple<Tab, ITool>)((ToolStripMenuItem)sender).Tag;
			_sidePane.SelectItem(selectInformation.Item1, selectInformation.Item2.MachineName);
		}

		private void Tool_Clicked(Item itemClicked)
		{
			var clickedTool = (ITool)itemClicked.Tag;
			if (_currentTool == clickedTool)
			{
				_currentArea.ActiveTool = clickedTool;
				return;  // Not much else to do.
			}
			using (new IdleProcessingHelper(this))
			{
				ClearDuringTransition();
				// Reset Edit->Find state to default conditions. Each tool can then sort out what to do with it
				CmdFindAndReplaceText.Visible = true;
				CmdFindAndReplaceText.Enabled = false;
				Toolbar_CmdFindAndReplaceText.Visible = Toolbar_CmdFindAndReplaceText.Enabled = false;
				CmdReplaceText.Visible = true;
				CmdReplaceText.Enabled = false;
				if (_currentArea.ActiveTool != null)
				{
					_currentArea.ActiveTool = null;
					_currentTool?.Deactivate(_majorFlexComponentParameters);
				}

				_currentTool = clickedTool;
				_currentArea.ActiveTool = clickedTool;
				var areaName = _currentArea.MachineName;
				var toolName = _currentTool.MachineName;
				PropertyTable.SetProperty($"{AreaServices.ToolForAreaNamed_}{areaName}", toolName, true, settingsGroup: SettingsGroup.LocalSettings);
				PropertyTable.SetProperty(AreaServices.ToolChoice, _currentTool.MachineName, true, settingsGroup: SettingsGroup.LocalSettings);
				// Do some logging.
				Logger.WriteEvent("Switched to " + _currentTool.MachineName);
				// Should we report a tool change?
				if (_lastToolChange.Date != DateTime.Now.Date)
				{
					// New day has dawned (or just started up). Reset tool reporting.
					_toolsReportedToday.Clear();
					_lastToolChange = DateTime.Now;
				}

				if (!_toolsReportedToday.Contains(toolName))
				{
					_toolsReportedToday.Add(toolName);
					UsageReporter.SendNavigationNotice("SwitchToTool/{0}/{1}", areaName, toolName);
				}

				_currentTool.Activate(_majorFlexComponentParameters);
			}
		}

		private void Area_Clicked(Tab tabClicked)
		{
			var clickedArea = (IArea)tabClicked.Tag;
			if (_currentArea == clickedArea)
			{
				_currentArea.ActiveTool = null;
				return; // Not much else to do.
			}
			using (new IdleProcessingHelper(this))
			{
				ClearDuringTransition();
				_currentArea?.Deactivate(_majorFlexComponentParameters);
				_currentArea = clickedArea;
				PropertyTable.SetProperty(AreaServices.AreaChoice, _currentArea.MachineName, true, settingsGroup: SettingsGroup.LocalSettings);
				_currentArea.Activate(_majorFlexComponentParameters);
			}
		}

		private void ClearDuringTransition()
		{
			// NB: If you are ever tempted to not set the record list to null, then be prepared to have each area/tool clear it.
			RecordListServices.SetRecordList(Handle, null);
			StatusBarPanelServices.ClearBasicStatusBars(_statusbar);
		}

		private void SetupPropertyTable()
		{
			LoadPropertyTable();
			SetDefaultProperties();
			RemoveObsoleteProperties();
			PropertyTable.ConvertOldPropertiesToNewIfPresent();
		}

		private void LoadPropertyTable()
		{
			PropertyTable.UserSettingDirectory = LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder);
			PropertyTable.LocalSettingsId = "local";
			if (!Directory.Exists(PropertyTable.UserSettingDirectory))
			{
				Directory.CreateDirectory(PropertyTable.UserSettingDirectory);
			}
			PropertyTable.RestoreFromFile(PropertyTable.GlobalSettingsId);
			PropertyTable.RestoreFromFile(PropertyTable.LocalSettingsId);
		}

		/// <summary>
		/// Just in case some expected property hasn't been reloaded,
		/// see that they are loaded. "SetDefault" doesn't mess with them if they are restored.
		/// </summary>
		/// <remarks>NB: default properties of interest to areas/tools are handled by them.</remarks>
		private void SetDefaultProperties()
		{
			// "UseVernSpellingDictionary" Controls checking the global Tools->Spelling->Show Vernacular Spelling Errors menu.
			PropertyTable.SetDefault(LanguageExplorerConstants.UseVernSpellingDictionary, true, true, settingsGroup: SettingsGroup.GlobalSettings);
			// This "PropertyTableVersion" property will control the behavior of the "ConvertOldPropertiesToNewIfPresent" method.
			PropertyTable.SetDefault(LanguageExplorerConstants.PropertyTableVersion, int.MinValue, true, settingsGroup: SettingsGroup.GlobalSettings);
			// This is the splitter distance for the sidebar/secondary splitter pair of controls.
			PropertyTable.SetDefault(SidebarWidthGlobal, 140, true, settingsGroup: SettingsGroup.GlobalSettings);
			// This is the splitter distance for the record list/main content pair of controls.
			PropertyTable.SetDefault(LanguageExplorerConstants.RecordListWidthGlobal, 200, true, settingsGroup: SettingsGroup.GlobalSettings);
			PropertyTable.SetDefault(AreaServices.InitialArea, AreaServices.InitialAreaMachineName, true);
			PropertyTable.SetDefault(LanguageExplorerConstants.SuspendLoadingRecordUntilOnJumpToRecord, string.Empty);
			PropertyTable.SetDefault(LanguageExplorerConstants.SuspendLoadListUntilOnChangeFilter, string.Empty);
			// This property can be used to set the settingsGroup for context dependent properties. No need to persist it.
			PropertyTable.SetDefault(LanguageExplorerConstants.SliceSplitterBaseDistance, -1);
			// Common to several tools in various areas, so set them here.
			PropertyTable.SetDefault(AllowInsertLinkToFile, false);
			PropertyTable.SetDefault("AllowShowNormalFields", false);
		}

		private void RemoveObsoleteProperties()
		{
			PropertyTable.RemoveProperty("CurrentToolbarVersion", SettingsGroup.GlobalSettings);
			// Get rid of obsolete properties, if they were restored.
			PropertyTable.RemoveProperty(LanguageExplorerConstants.ShowHiddenFields, SettingsGroup.LocalSettings);
			PropertyTable.RemoveProperty("ShowRecordList", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("PreferredUILibrary");
			PropertyTable.RemoveProperty("ShowSidebar", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("SidebarLabel", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("DoingAutomatedTest", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("RecordListLabel", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("MainContentLabel", SettingsGroup.GlobalSettings);
			PropertyTable.RemoveProperty("StatusPanelRecordNumber");
			PropertyTable.RemoveProperty("StatusPanelMessage");
			PropertyTable.RemoveProperty("StatusBarPanelFilter");
			PropertyTable.RemoveProperty("StatusBarPanelSort");
			PropertyTable.RemoveProperty("DialogFilterStatus");
			PropertyTable.RemoveProperty("IgnoreStatusPanel");
			PropertyTable.RemoveProperty("DoLog");
			PropertyTable.RemoveProperty("ShowBalloonHelp");
			PropertyTable.RemoveProperty("ShowMorphBundles");
			PropertyTable.RemoveProperty("ActiveClerk");
			PropertyTable.RemoveProperty("SelectedWritingSystemHvosForCurrentContextMenu");
			PropertyTable.RemoveProperty("SortedFromEnd");
			PropertyTable.RemoveProperty("SortedByLength");
			PropertyTable.RemoveProperty("ActiveListOwningObject");
		}

		private void SetTemporaryProperties()
		{
			_temporaryPropertyNames.Clear();
			_temporaryPropertyNames.AddRange(new[]
			{
				FwUtils.window,
				LanguageExplorerConstants.App,
				FwUtils.cache,
				LanguageExplorerConstants.HelpTopicProvider,
				FwUtils.FlexStyleSheet,
				LanguageExplorerConstants.LinkHandler,
				LanguageExplorerConstants.MajorFlexComponentParameters,
				LanguageExplorerConstants.RecordListRepository
			});
			// Not persisted, but needed at runtime.
			foreach (var key in _temporaryPropertyNames)
			{
				switch (key)
				{
					case FwUtils.window:
						PropertyTable.SetProperty(key, this);
						break;
					case LanguageExplorerConstants.App:
						PropertyTable.SetProperty(key, _flexApp);
						break;
					case FwUtils.cache:
						PropertyTable.SetProperty(key, Cache);
						break;
					case LanguageExplorerConstants.HelpTopicProvider:
						PropertyTable.SetProperty(key, _flexApp);
						break;
					case FwUtils.FlexStyleSheet:
						PropertyTable.SetProperty(key, _stylesheet);
						break;
					case LanguageExplorerConstants.LinkHandler:
						PropertyTable.SetProperty(key, _linkHandler);
						break;
					case LanguageExplorerConstants.MajorFlexComponentParameters:
						PropertyTable.SetProperty(key, _majorFlexComponentParameters, settingsGroup: SettingsGroup.GlobalSettings);
						break;
					case LanguageExplorerConstants.RecordListRepository:
						PropertyTable.SetProperty(key, _recordListRepositoryForTools, settingsGroup: SettingsGroup.GlobalSettings);
						break;
				}
			}
		}

		private void RegisterSubscriptions()
		{
			Subscriber.Subscribe("MigrateOldConfigurations", MigrateOldConfigurations);
			Subscriber.Subscribe(LanguageExplorerConstants.SetToolFromName, SetToolFromName);
		}

		private void SetToolFromName(object newValue)
		{
			var toolName = (string)newValue;
			if (_currentTool.MachineName == toolName)
			{
				// 0. IF _currentTool is the same tool, do nothing.
				return;
			}
			IArea targetArea = null;
			foreach (var area in _areaRepository.AllAreasInOrder.Values)
			{
				var targetTool = area.AllToolsInOrder.Values.FirstOrDefault(tool => tool.MachineName == toolName);
				if (targetTool == null)
				{
					continue;
				}
				targetArea = area;
				break;
			}
			_sidePane.SelectItem(targetArea.MachineName, toolName);
		}

		private void MigrateOldConfigurations(object newValue)
		{
			Subscriber.Unsubscribe("MigrateOldConfigurations", MigrateOldConfigurations);
			DictionaryConfigurationServices.MigrateOldConfigurationsIfNeeded(Cache, PropertyTable);
		}

		private void SetupCustomStatusBarPanels()
		{
			_statusbar.SuspendLayout();
			// Insert first, so it ends up last in the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarTextBox(_statusbar)
			{
				TextForReal = "Filter",
				Name = LanguageExplorerConstants.StatusBarPanelFilter,
				MinWidth = 40,
				AutoSize = StatusBarPanelAutoSize.Contents
			});
			// Insert second, so it ends up in the middle of the three that are inserted.
			_statusbar.Panels.Insert(3, new StatusBarTextBox(_statusbar)
			{
				TextForReal = "Sort",
				Name = LanguageExplorerConstants.StatusBarPanelSort,
				MinWidth = 40,
				AutoSize = StatusBarPanelAutoSize.Contents
			});
			// Insert last, so it ends up first in the three that are inserted.
			var progressPanel = new StatusBarProgressPanel(_statusbar)
			{
				Name = LanguageExplorerConstants.StatusBarPanelProgressBar,
				MinWidth = 150,
				AutoSize = StatusBarPanelAutoSize.Contents
			};
			PropertyTable.SetProperty("ProgressBar", progressPanel);
			_statusbar.Panels.Insert(3, progressPanel);
			_statusbar.ResumeLayout(true);
		}

		private static void SetSplitContainerDistance(SplitContainer splitCont, int pixels)
		{
			if (splitCont.SplitterDistance != pixels)
			{
				splitCont.SplitterDistance = pixels;
			}
		}

		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			if (!_persistWindowSize)
			{
				return;
			}
			// Don't bother storing the size if we are maximized or minimized.
			// If we did, then when the user exits the application and then runs it again,
			//then switches to the normal state, we would be switching to a bizarre size.
			if (WindowState == FormWindowState.Normal)
			{
				PropertyTable.SetProperty(LanguageExplorerConstants.windowSize, Size, true, settingsGroup: SettingsGroup.GlobalSettings);
			}
		}

		protected override void OnMove(EventArgs e)
		{
			base.OnMove(e);
			if (!_persistWindowSize)
			{
				return;
			}
			// Don't bother storing the location if we are maximized or minimized.
			// if we did, then when the user exits the application and then runs it again,
			//then switches to the normal state, we would be switching to 0,0 or something.
			if (WindowState == FormWindowState.Normal)
			{
				PropertyTable.SetProperty(LanguageExplorerConstants.windowLocation, Location, true, settingsGroup: SettingsGroup.GlobalSettings);
			}
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		public void SaveSettings()
		{
			SaveWindowSettings();
			// Have current IArea put any needed properties into the table.
			_currentArea.EnsurePropertiesAreCurrent();
			// Have current ITool put any needed properties into the table.
			_currentTool.EnsurePropertiesAreCurrent();
			// first save global settings, ignoring database specific ones.
			PropertyTable.SaveGlobalSettings();
			// now save database specific settings.
			PropertyTable.SaveLocalSettings();
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call PropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable => _propertyTable;

		#endregion

		#region Implementation of IRecordListOwner

		/// <summary>Find the IRecordListUpdater object with the given name.</summary>
		public IRecordListUpdater FindRecordListUpdater(string name)
		{
			return PropertyTable.GetValue<IRecordListUpdater>(name, null);
		}

		#endregion

		#region Implementation of IFwMainWnd

		/// <summary>
		/// Gets the active view of the window
		/// </summary>
		public IRootSite ActiveView => _viewHelper.ActiveView;

		/// <summary>
		/// Gets the focused control of the window
		/// </summary>
		public Control FocusedControl
		{
			get
			{
				Control focusControl = null;
				if (PropertyTable.PropertyExists("FirstControlToHandleMessages"))
				{
					focusControl = PropertyTable.GetValue<Control>("FirstControlToHandleMessages");
				}
				if (focusControl == null || focusControl.IsDisposed)
				{
					focusControl = FromHandle(Win32.GetFocus());
				}
				return focusControl;
			}
		}

		/// <summary>
		/// Gets the data object cache.
		/// </summary>
		public LcmCache Cache => _flexApp.Cache;

		/// <summary>
		/// Clears out some temporary sort sequences.
		/// </summary>
		public void ClearInvalidatedStoredData()
		{
			RobustIO.DeleteDirectoryAndContents(Path.Combine(Cache.ProjectId.ProjectFolder, LcmFileHelper.ksSortSequenceTempDir));
		}

		/// <summary>
		/// Gets or sets the control that needs keyboard input.  (See FWNX-785.)
		/// </summary>
		public Control DesiredControl { get; set; }

		/// <summary>
		/// Get the specified main menu
		/// </summary>
		/// <returns>Return the specified main menu, or null if not found.</returns>
		/// <remarks>
		/// SimpleRootSite calls this via reflection to get the CmdPrint menu from the File menu.
		/// </remarks>
		public ToolStripMenuItem GetMainMenu(string menuName)
		{
			ToolStripMenuItem retval = null;
			if (_menuStrip.Items.ContainsKey(menuName))
			{
				retval = (ToolStripMenuItem)_menuStrip.Items[menuName];
			}
			return retval;
		}

		/// <summary>
		/// Initialize the window, before being shown.
		/// </summary>
		/// <remarks>
		/// This allows for creating all sorts of things used by the implementation.
		/// </remarks>
		public void Initialize(bool windowIsCopy = false, FwLinkArgs linkArgs = null)
		{
			toolStripStandard.Dock = DockStyle.Top;
			toolStripInsert.Dock = DockStyle.Top;
			toolStripView.Dock = DockStyle.Top;
			toolStripFormat.Dock = DockStyle.Top;
			_windowIsCopy = windowIsCopy;
			_startupLink = linkArgs; // May be null.
			var wasCrashDuringPreviousStartup = SetupCrashDetectorFile();
			var flexComponentParameters = new FlexComponentParameters(PropertyTable, Publisher, Subscriber);
			_recordListRepositoryForTools = new RecordListRepository(Cache, flexComponentParameters);
			DoImageHack();
			SetupCustomStatusBarPanels();
			SetupStylesheet();
			SetupPropertyTable();
			RegisterSubscriptions();
			RestoreWindowSettings(wasCrashDuringPreviousStartup);
			var restoreSize = Size;
			SetupUiWidgets(flexComponentParameters);
			SetupOutlookBar();
			SetWindowTitle();
			SetupWindowSizeIfNeeded(restoreSize);
			if (File.Exists(CrashOnStartupDetectorPathName)) // Have to check again, because unit test check deletes it in the RestoreWindowSettings method.
			{
				File.Delete(CrashOnStartupDetectorPathName);
			}
			this.Closing += OnClosing;
		}

		private void OnClosing(object sender, CancelEventArgs e)
		{
			e.Cancel = false;
			Publisher.Publish(LanguageExplorerConstants.ConsideringClosing, e);
			if (e.Cancel)
			{
				return;
			}
			_parserMenuManager.DisconnectFromParser();
			PropertyTable.SetProperty(LanguageExplorerConstants.windowState, WindowState, true, settingsGroup: SettingsGroup.GlobalSettings);
			SaveSettings();
		}

		private void DoImageHack()
		{
#if RANDYTODO
			// TODO: Some images are locked in InterlinearImageHolder, so dig them out, until someone can reproduce them.
#endif
			using (var imageHolder = new InterlinearImageHolder())
			{
				// Image index: 13
				CmdApproveAll.Image = imageHolder.buttonImages.Images[13];
				Toolbar_CmdApproveAllButton.Image =  imageHolder.buttonImages.Images[13];
			}
		}

		private void SetupUiWidgets(FlexComponentParameters flexComponentParameters)
		{
			_viewHelper = new ActiveViewHelper(this);
			_writingSystemListHandler = new WritingSystemListHandler(this, Cache, Subscriber, Toolbar_WritingSystemList, WritingSystemMenu);
			_combinedStylesListHandler = new CombinedStylesListHandler(this, Subscriber, _stylesheet, Toolbar_CombinedStylesList);
			_sendReceiveToolStripMenuItem.Enabled = FLExBridgeHelper.IsFlexBridgeInstalled();
			var mainMenus = new Dictionary<MainMenu, ToolStripMenuItem>
			{
				{ MainMenu.File, _fileToolStripMenuItem },
				{ MainMenu.SendReceive, _sendReceiveToolStripMenuItem },
				{ MainMenu.Edit, _editToolStripMenuItem },
				{ MainMenu.View, _viewToolStripMenuItem },
				{ MainMenu.Data, _dataToolStripMenuItem },
				{ MainMenu.Insert, _insertToolStripMenuItem },
				{ MainMenu.Format, _formatToolStripMenuItem },
				{ MainMenu.Tools, _toolsToolStripMenuItem },
				{ MainMenu.Parser, _parserToolStripMenuItem },
				{ MainMenu.Window, _windowToolStripMenuItem },
				{ MainMenu.Help, _helpToolStripMenuItem }
			};
			var mainToolBars = new Dictionary<ToolBar, ToolStrip>
			{
				{ ToolBar.Standard, toolStripStandard },
				{ ToolBar.View, toolStripView },
				{ ToolBar.Insert, toolStripInsert },
				{ ToolBar.Format, toolStripFormat }
			};
			var uiWidgetHelper = new UiWidgetController(new MainMenusParameterObject(mainMenus, CacheMenuItems()), new MainToolBarsParameterObject(mainToolBars, CacheToolbarItems()));
			var globalUiWidgetParameterObject = new GlobalUiWidgetParameterObject();
			_linkHandler = new LinkHandler(flexComponentParameters, Cache, globalUiWidgetParameterObject);
			_parserMenuManager = new ParserMenuManager(_sharedEventHandlers, _statusbar.Panels[LanguageExplorerConstants.StatusBarPanelProgress], _parserToolStripMenuItem, uiWidgetHelper.ParserMenuDictionary, globalUiWidgetParameterObject);
			_dataNavigationManager = new DataNavigationManager(globalUiWidgetParameterObject);
			_majorFlexComponentParameters = new MajorFlexComponentParameters(mainContainer, _menuStrip, toolStripContainer, uiWidgetHelper, _statusbar, _parserMenuManager, _dataNavigationManager, flexComponentParameters, Cache, _flexApp, this, _sharedEventHandlers, _sidePane);
			SetTemporaryProperties();
			RecordListServices.Setup(_majorFlexComponentParameters);
			_parserMenuManager.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
			if (_sendReceiveToolStripMenuItem.Enabled)
			{
				// No need for it, if FB isn't even installed.
				_sendReceiveMenuManager = new SendReceiveMenuManager(IdleQueue, this, _flexApp, Cache, globalUiWidgetParameterObject);
				_sendReceiveMenuManager.InitializeFlexComponent(flexComponentParameters);
			}
			Cache.DomainDataByFlid.AddNotification(this);
			CmdUseVernSpellingDictionary.Checked = _propertyTable.GetValue<bool>(LanguageExplorerConstants.UseVernSpellingDictionary);
			if (CmdUseVernSpellingDictionary.Checked)
			{
				EnableVernacularSpelling();
			}
			_macroMenuHandler.Initialize(_majorFlexComponentParameters, globalUiWidgetParameterObject);
			// Add any other global type items where the event handler is defined in this window.
			var fileMenuDictionary = globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.File];
			fileMenuDictionary.Add(Command.CmdNewLangProject, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_New_FieldWorks_Project, () => UiWidgetServices.CanSeeAndDo));
			fileMenuDictionary.Add(Command.CmdChooseLangProject, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_Open, () => UiWidgetServices.CanSeeAndDo));
			fileMenuDictionary.Add(Command.CmdProjectProperties, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_FieldWorks_Project_Properties, () => UiWidgetServices.CanSeeAndDo));
			fileMenuDictionary.Add(Command.CmdBackup, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_Back_up_this_Project, () => UiWidgetServices.CanSeeAndDo));
			fileMenuDictionary.Add(Command.CmdRestoreFromBackup, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_Restore_a_Project, () => UiWidgetServices.CanSeeAndDo));
			fileMenuDictionary.Add(Command.CmdProjectLocation, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_Project_Location, () => CanCmdProjectLocation));
			fileMenuDictionary.Add(Command.CmdDeleteProject, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_Delete_Project, () => UiWidgetServices.CanSeeAndDo));
			fileMenuDictionary.Add(Command.CmdCreateProjectShortcut, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_Create_Shortcut_on_Desktop, () => UiWidgetServices.CanSeeAndDo));
			fileMenuDictionary.Add(Command.CmdArchiveWithRamp, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_Archive_With_RAMP, () => CanCmdArchiveWithRamp));
			fileMenuDictionary.Add(Command.CmdUploadToWebonary, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(UploadToWebonary_Click, () => CanCmdUploadToWebonary));
			fileMenuDictionary.Add(Command.CmdImportSFMLexicon, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_Import_Standard_Format_Marker_Click, () => UiWidgetServices.CanSeeAndDo));
			fileMenuDictionary.Add(Command.CmdImportTranslatedLists, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_Translated_List_Content, () => UiWidgetServices.CanSeeAndDo));
			fileMenuDictionary.Add(Command.CmdClose, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(File_CloseWindow, () => UiWidgetServices.CanSeeAndDo));

			var editMenuDictionary = globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.Edit];
			editMenuDictionary.Add(Command.CmdUndo, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Undo_Click, () => CanCmdUndo));
			editMenuDictionary.Add(Command.CmdRedo, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Redo_Click, () => CanCmdRedo));
			var tuple = new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Cut, () => CanCmdCut);
			editMenuDictionary.Add(Command.CmdCut, tuple);
			_sharedEventHandlers.Add(Command.CmdCut, tuple);
			tuple = new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Copy, () => CanCmdCopy);
			editMenuDictionary.Add(Command.CmdCopy, tuple);
			_sharedEventHandlers.Add(Command.CmdCopy, tuple);
			tuple = new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Paste, () => CanCmdPaste);
			editMenuDictionary.Add(Command.CmdPaste, tuple);
			_sharedEventHandlers.Add(Command.CmdPaste, tuple);
			editMenuDictionary.Add(Command.CmdSelectAll, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Select_All, () => CanCmdSelectAll));
			editMenuDictionary.Add(Command.CmdPasteHyperlink, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Paste_Hyperlink, () => CanCmdPasteHyperlink));
			editMenuDictionary.Add(Command.CmdDeleteRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Delete_Click, () => CanCmdDeleteRecord));

			var viewMenuDictionary = globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.View];
			var standardToolBarDictionary = globalUiWidgetParameterObject.GlobalToolBarItems[ToolBar.Standard];
			UiWidgetServices.InsertPair(standardToolBarDictionary, viewMenuDictionary, Command.CmdRefresh, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(View_Refresh, () => CanCmdRefresh));

			var insertMenuDictionary = globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.Insert];
			insertMenuDictionary.Add(Command.CmdInsertLinkToFile, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(LinkToFileToolStripMenuItem_Click, () => CanCmdInsertLinkToFile));
			insertMenuDictionary.Add(Command.CmdShowCharMap, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(SpecialCharacterToolStripMenuItem_Click, () => CanCmdShowCharMap));

			var formatMenuDictionary = globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.Format];
			formatMenuDictionary.Add(Command.CmdFormatStyle, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Format_Styles_Click, () => UiWidgetServices.CanSeeAndDo));
			formatMenuDictionary.Add(Command.CmdFormatApplyStyle, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(applyStyleToolStripMenuItem_Click, () => CanCmdFormatApplyStyle));
			formatMenuDictionary.Add(Command.CmdVernacularWritingSystemProperties, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdVernacularWritingSystemProperties_Click, () => UiWidgetServices.CanSeeAndDo));
			formatMenuDictionary.Add(Command.CmdAnalysisWritingSystemProperties, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdAnalysisWritingSystemProperties_Click, () => UiWidgetServices.CanSeeAndDo));

			var toolsMenuDictionary = globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.Tools];
			toolsMenuDictionary.Add(Command.CmdRestoreDefaults, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(restoreDefaultsToolStripMenuItem_Click, () => UiWidgetServices.CanSeeAndDo));
			toolsMenuDictionary.Add(Command.CmdUseVernSpellingDictionary, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(showVernacularSpellingErrorsToolStripMenuItem_Click, () => UiWidgetServices.CanSeeAndDo));
			toolsMenuDictionary.Add(Command.CmdProjectUtilities, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(utilitiesToolStripMenuItem_Click, () => UiWidgetServices.CanSeeAndDo));
			toolsMenuDictionary.Add(Command.CmdToolsOptions, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Tools_Options_Click, () => UiWidgetServices.CanSeeAndDo));

			globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.Window].Add(Command.CmdNewWindow, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(NewWindow_Clicked, () => UiWidgetServices.CanSeeAndDo));

			var helpMenuDictionary = globalUiWidgetParameterObject.GlobalMenuItems[MainMenu.Help];
			helpMenuDictionary.Add(Command.CmdHelpLanguageExplorer, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_LanguageExplorer, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpTraining, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_Training, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpDemoMovies, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_DemoMovies, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpLexicographyIntro, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_Introduction_To_Lexicography_Clicked, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpMorphologyIntro, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_Introduction_To_Parsing_Click, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpNotesSendReceive, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_Technical_Notes_on_FieldWorks_Send_Receive, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpNotesSFMDatabaseImport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_Techinical_Notes_On_SFM_Database_Import_Click, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpNotesLinguaLinksDatabaseImport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_Technical_Notes_On_LinguaLinks_Import_Click, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpNotesInterlinearImport, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_Technical_Notes_On_Interlinear_Import_Click, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpNotesWritingSystems, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_Training_Writing_Systems, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpXLingPap, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_XLingPaper, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpReportBug, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_ReportProblem, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpMakeSuggestion, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_Make_a_Suggestion, () => UiWidgetServices.CanSeeAndDo));
			helpMenuDictionary.Add(Command.CmdHelpAbout, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Help_About_Language_Explorer, () => UiWidgetServices.CanSeeAndDo));

			// Standard is set, above: var standardToolBarDictionary = globalUiWidgetParameterObject.GlobalToolBarItems[ToolBar.Standard];
			standardToolBarDictionary.Add(Command.CmdUndo, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Undo_Click, () => CanCmdUndo));
			standardToolBarDictionary.Add(Command.CmdRedo, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Redo_Click, () => CanCmdRedo));
			standardToolBarDictionary.Add(Command.CmdDeleteRecord, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Edit_Delete_Click, () => CanCmdDeleteRecord));

			var viewToolBarDictionary = globalUiWidgetParameterObject.GlobalToolBarItems[ToolBar.View];
			viewToolBarDictionary.Add(Command.CmdChangeFilterClearAll, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(toolStripButtonChangeFilterClearAll_Click, () => CanCmdChangeFilterClearAll));

			uiWidgetHelper.AddGlobalHandlers(globalUiWidgetParameterObject);
		}

		/// <summary>
		/// Gets a Rectangle representing the position and size of the window in its
		/// normal (non-minimized, non-maximized) state.
		/// </summary>
		public Rectangle NormalStateDesktopBounds { get; private set; }

		/// <summary>
		/// Called just before a window synchronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		public void PreSynchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when a window synchronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <returns>true if successful; false results in RefreshAllWindows.</returns>
		public bool Synchronize(SyncMsg sync)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise.  False should keep the main window
		/// from being shown/initialized (maybe even close the window if false is returned)
		/// </returns>
		public bool OnFinishedInit()
		{
			if (_startupLink == null)
			{
				return true;
			}
			var startupLink = _startupLink;
			_startupLink = null;
			LinkHandler.PublishFollowLinkMessage(Publisher, startupLink);
			return true;
		}

		/// <summary>
		/// Prepare to refresh the main window and its IAreas and ITools.
		/// </summary>
		public void PrepareToRefresh()
		{
			_currentArea.PrepareToRefresh();
		}

		/// <summary>
		/// Finish refreshing the main window and its IAreas and ITools.
		/// </summary>
		/// <remarks>
		/// This should call Refresh on real window implementations,
		/// after everything else is done.</remarks>
		public void FinishRefresh()
		{
			Cache.ServiceLocator.GetInstance<IUndoStackManager>().Refresh();
			_currentArea.FinishRefresh();
			Refresh();
		}

		/// <summary>
		/// Refreshes all the views in this window and in all others in the app.
		/// </summary>
		public void RefreshAllViews()
		{
			RefreshDisplay();
			// Susanna asked that refresh affect only the currently active project, which is
			// what the string and List variables below attempt to handle.  See LT-6444.
			var activeWnd = ActiveForm as IFwMainWnd;
			var allMainWindowsExceptActiveWindow = new List<IFwMainWnd>();
			foreach (var otherMainWindow in _flexApp.MainWindows.Where(mw => mw != activeWnd))
			{
				otherMainWindow.PrepareToRefresh();
				allMainWindowsExceptActiveWindow.Add(otherMainWindow);
			}
			// Now that all IFwMainWnds except currently active one have done basic refresh preparation,
			// have them all finish refreshing.
			foreach (var otherMainWindow in allMainWindowsExceptActiveWindow)
			{
				otherMainWindow.FinishRefresh();
			}
			// LT-3963: active IFwMainWnd changes as a result of a refresh.
			// Make sure focus doesn't switch to another FLEx application / window also
			// make sure the application focus isn't lost all together.
			// ALSO, after doing a refresh with just a single application / window,
			// the application would loose focus and you'd have to click into it to
			// get that back, this will reset that too.
			if (activeWnd == null)
			{
				return;
			}
			// Refresh it last, so its saved settings get restored.
			activeWnd.FinishRefresh();
			var activeForm = activeWnd as Form;
			activeForm?.Activate();
		}

		/// <summary>
		/// Get the RecordBar (as a Control), or null if not present.
		/// </summary>
		public IRecordBar RecordBarControl => (mainContainer.SecondControl as CollapsingSplitContainer)?.FirstControl as IRecordBar;

		/// <summary>
		/// Get the TreeView of RecordBarControl, or null if not present, or it is not showing a tree.
		/// </summary>
		public TreeView TreeStyleRecordList
		{
			get
			{
				var recordBarControl = RecordBarControl;
				if (recordBarControl == null)
				{
					return null;
				}
				var retVal = recordBarControl.TreeView;
				if (!retVal.Visible)
				{
					retVal = null;
				}
				return retVal;
			}
		}

		/// <summary>
		/// Get the ListView of RecordBarControl, or null if not present, or it is not showing a list.
		/// </summary>
		public ListView ListStyleRecordList
		{
			get
			{
				var recordBarControl = RecordBarControl;
				if (recordBarControl == null)
				{
					return null;
				}
				var retVal = recordBarControl.ListView;
				if (!retVal.Visible)
				{
					retVal = null;
				}
				return retVal;
			}
		}

		public ProgressState CreateSimpleProgressState()
		{
			var panel = PropertyTable.GetValue<StatusBarProgressPanel>("ProgressBar");
			return panel == null ? new NullProgressState() : new ProgressState(panel);
		}
		#endregion

		#region Implementation of IApplicationIdleEventHandler

		/// <summary>
		/// Call this for the duration of a block of code where we don't want idle events.
		/// (Note that various things outside our control may pump events and cause the
		/// timer that fires the idle events to be triggered when we are not idle, even in the
		/// middle of processing another event.) Call ResumeIdleProcessing when done.
		/// </summary>
		public void SuspendIdleProcessing()
		{
			// This bundle of suspend calls *must* (read: it is imperative that) be resumed in the ResumeIdleProcessing() method.
			_idleProcessingHelpers.Push(new List<IdleProcessingHelper>
			{
				new IdleProcessingHelper(IdleQueue),
				new IdleProcessingHelper(_writingSystemListHandler),
				new IdleProcessingHelper(_combinedStylesListHandler)
			});
		}

		/// <summary>
		/// See SuspendIdleProcessing.
		/// </summary>
		public void ResumeIdleProcessing()
		{
			foreach (var idleProcessingHelper in _idleProcessingHelpers.Pop())
			{
				idleProcessingHelper.Dispose();
			}
		}
		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher => _publisher;
		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber => _subscriber;
		#endregion

		#region IIdleQueueProvider implementation

		/// <summary>
		/// Get the singleton (per window) instance of IdleQueue.
		/// </summary>
		public IdleQueue IdleQueue => _idleQueue;

		#endregion

		#region Implementation of IDisposable

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_sharedEventHandlers.Remove(Command.CmdCut);
				_sharedEventHandlers.Remove(Command.CmdCopy);
				_sharedEventHandlers.Remove(Command.CmdPaste);
				Cache.DomainDataByFlid.RemoveNotification(this);
				foreach (var helper in _idleProcessingHelpers)
				{
					foreach (var handler in helper)
					{
						handler.Dispose();
					}
				}
				_idleProcessingHelpers.Clear();
				// Quit responding to messages early on.
				Subscriber.Unsubscribe("MigrateOldConfigurations", MigrateOldConfigurations);
				RecordListServices.TearDown(Handle);
				_currentArea?.Deactivate(_majorFlexComponentParameters);
				_sendReceiveMenuManager?.Dispose();
				_parserMenuManager?.Dispose();
				_dataNavigationManager?.Dispose();
				_macroMenuHandler?.Dispose();
				IdleQueue?.Dispose();
				_writingSystemListHandler?.Dispose();
				_combinedStylesListHandler?.Dispose();
				_linkHandler?.Dispose();
				// Get rid of known, temporary, properties
				foreach (var temporaryPropertyName in _temporaryPropertyNames)
				{
					PropertyTable.RemoveProperty(temporaryPropertyName);
				}
				_temporaryPropertyNames.Clear();
				components?.Dispose();
				// TODO: Is this comment still relevant?
				// TODO: Seems like FLEx worked well with it in this place (in the original window) for a long time.
				// The removing of the window needs to happen later; after this main window is
				// already disposed of. This is needed for side-effects that require a running
				// message loop.
				_flexApp.FwManager.ExecuteAsync(_flexApp.RemoveWindow, this);
				_dataNavigationManager?.Dispose();
				_sidePane?.Dispose();
				_viewHelper?.Dispose();
			}

			_sendReceiveMenuManager = null;
			_parserMenuManager = null;
			_dataNavigationManager = null;
			_sidePane = null;
			_viewHelper = null;
			_currentArea = null;
			_stylesheet = null;
			_publisher = null;
			_subscriber = null;
			_areaRepository = null;
			_idleQueue = null;
			_majorFlexComponentParameters = null;
			_writingSystemListHandler = null;
			_combinedStylesListHandler = null;
			_linkHandler = null;
			_macroMenuHandler = null;
			_sharedEventHandlers = null;
			_temporaryPropertyNames = null;

			base.Dispose(disposing);

			if (disposing)
			{
				_recordListRepositoryForTools?.Dispose();
			}
			_recordListRepositoryForTools = null;

			// Leave the PropertyTable for last, since the above stuff may still want to access it, while shutting down.
			if (disposing)
			{
				_propertyTable?.Dispose();
				(_publisher as IDisposable)?.Dispose();
			}
			_propertyTable = null;
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FwMainWnd()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
			GC.SuppressFinalize(this);
		}

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag != WfiWordformTags.kflidSpellingStatus)
			{
				// We are only interested in one tag.
				return;
			}
			RestartSpellChecking();
			// This keeps the spelling dictionary in sync with the WFI.
			// Arguably this should be done in FDO. However the spelling dictionary is used to
			// keep the UI showing squiggles, so it's also arguable that it is a UI function.
			// In any case it's easier to do it in PropChanged (which also fires in Undo/Redo)
			// than in a data-change method which does not.
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().GetObject(hvo);
			var text = wf.Form.VernacularDefaultWritingSystem.Text;
			if (!string.IsNullOrEmpty(text))
			{
				SpellingHelper.SetSpellingStatus(text, Cache.DefaultVernWs, Cache.LanguageWritingSystemFactoryAccessor, wf.SpellingStatus == (int)SpellingStatusStates.correct);
			}
		}
		#endregion


		/// <summary>
		/// Collect refreshable caches from every child which has one and refresh them (once each).
		/// Call RefreshDisplay for every child control (recursively) which implements it.
		///
		/// returns true if all the children have been processed by this class.
		/// </summary>
		private void RefreshDisplay()
		{
			Cache.ServiceLocator.GetInstance<IUndoStackManager>().Refresh();
			var cacheCollector = new HashSet<DomainDataByFlidDecoratorBase>();
			var recordListCollector = new HashSet<IRecordList>();
			CollectCachesToRefresh(this, cacheCollector, recordListCollector);
			foreach (var cache in cacheCollector)
			{
				cache.Refresh();
			}
			foreach (var recordList in recordListCollector)
			{
				recordList.ReloadIfNeeded();
			}
			try
			{
				// In many cases ReconstructViews, which calls RefreshViews, will also try to Refresh the same caches.
				// Not only is this a waste, but it may wipe out data that ReloadIfNeeded has carefully re-created.
				// So suspend refresh for the caches we have already refreshed.
				foreach (var cache in cacheCollector)
				{
					cache.SuspendRefresh();
				}
				// Don't be lured into simplifying this to ReconstructViews(this). That has the loop,
				// but also calls this method again, making a stack overflow.
				foreach (Control c in Controls)
				{
					ReconstructViews(c);
				}
			}
			finally
			{
				foreach (var cache in cacheCollector)
				{
					cache.ResumeRefresh();
				}
			}
		}

		/// <summary>
		/// Collect refreshable caches from the specified control and its subcontrols.
		/// We currently handle controls that are rootsites, and check their own SDAs as well
		/// as any base SDAs that those SDAs wrap.
		/// </summary>
		private static void CollectCachesToRefresh(Control c, HashSet<DomainDataByFlidDecoratorBase> cacheCollector, HashSet<IRecordList> recordListCollector)
		{
			var rootSite = c as IVwRootSite;
			if (rootSite?.RootBox != null)
			{
				var sda = rootSite.RootBox.DataAccess;
				while (sda != null)
				{
					var cache = sda as DomainDataByFlidDecoratorBase;
					if (cache != null)
					{
						cacheCollector.Add(cache);
						sda = cache.BaseSda;
					}
					else
					{
						break;
					}
				}
			}
			var recordListView = c as ViewBase;
			if (recordListView?.MyRecordList != null)
			{
				recordListCollector.Add(recordListView.MyRecordList);
			}
			foreach (Control child in c.Controls)
			{
				CollectCachesToRefresh(child, cacheCollector, recordListCollector);
			}
		}

		/// <summary>
		/// Call RefreshDisplay on the passed in control if it has a public (no argument) method of that name.
		/// Recursively call ReconstructViews on each control that the given control contains.
		/// </summary>
		/// <param name="control"></param>
		private static void ReconstructViews(Control control)
		{
			var childrenRefreshed = false;
			var refreshable = control as IRefreshableRoot;
			if (refreshable != null)
			{
				childrenRefreshed = refreshable.RefreshDisplay();
			}
			if (!childrenRefreshed)
			{
				foreach (Control c in control.Controls)
				{
					ReconstructViews(c);
				}
			}
		}

		private void File_CloseWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void Help_LanguageExplorer(object sender, EventArgs e)
		{
			var helpFile = _flexApp.HelpFile;
			try
			{
				// When the help window is closed it will return focus to the window that opened it (see MSDN
				// documentation for HtmlHelp()). We don't want to use the main window as the parent, because if
				// a modal dialog is visible, it will still return focus to the main window, allowing the main window
				// to perform some behaviors (such as refresh by pressing F5) while the modal dialog is visible,
				// which can be bad. So, we just create a dummy control and pass that in as the parent.
				Help.ShowHelp(new Control(), helpFile);
			}
			catch (Exception)
			{
				MessageBox.Show(this, string.Format(LanguageExplorerResources.ksCannotLaunchX, helpFile), LanguageExplorerResources.ksError);
			}
		}

		private static void Help_Training(object sender, EventArgs e)
		{
			using (var process = new Process())
			{
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.FileName = "http://wiki.lingtransoft.info/doku.php?id=tutorials:student_manual";
				process.Start();
				process.Close();
			}
		}

		private void Help_DemoMovies(object sender, EventArgs e)
		{
			try
			{
				var pathMovies = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Movies", "Demo Movies.html");
				OpenDocument<Win32Exception>(pathMovies, win32err =>
				{
					if (win32err.NativeErrorCode == 1155)
					{
						// The user has the movie files, but does not have a file association for .html files.
						// Try to launch Internet Explorer directly:
						using (Process.Start("IExplore.exe", pathMovies))
						{
						}
					}
					else
					{
						// User probably does not have movies. Try to launch the "no movies" web page:
						var pathNoMovies = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Movies", "notfound.html");
						OpenDocument<Win32Exception>(pathNoMovies, win32err2 =>
						{
							if (win32err2.NativeErrorCode == 1155)
							{
								// The user does not have a file association for .html files.
								// Try to launch Internet Explorer directly:
								Process.Start("IExplore.exe", pathNoMovies);
							}
							else
							{
								throw win32err2;
							}
						});
					}
				});
			}
			catch (Exception)
			{
				// Some other unforeseen error:
				MessageBox.Show(null, string.Format(LanguageExplorerResources.ksErrorCannotLaunchMovies, Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Movies")), LanguageExplorerResources.ksError);
			}
		}

		/// <summary>
		/// Uses Process.Start to run path. If running in Linux and path ends in .html or .htm,
		/// surrounds the path in double quotes and opens it with a web browser.
		/// </summary>
		private void OpenDocument(string path)
		{
			OpenDocument(path, err =>
			{
				MessageBox.Show(this, string.Format(LanguageExplorerResources.ksCannotLaunchX, path), LanguageExplorerResources.ksError);
			});
		}

		/// <summary>
		/// Uses Process.Start to run path. If running in Linux and path ends in .html or .htm,
		/// surrounds the path in double quotes and opens it with a web browser.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="exceptionHandler">
		/// Delegate to run if an exception is thrown. Takes the exception as an argument.
		/// </param>
		private void OpenDocument(string path, Action<Exception> exceptionHandler)
		{
			OpenDocument<Exception>(path, exceptionHandler);
		}

		/// <summary>
		/// Like OpenDocument(), but allowing specification of specific exception type T to catch.
		/// </summary>
		private void OpenDocument<T>(string path, Action<T> exceptionHandler) where T : Exception
		{
			try
			{
				if (MiscUtils.IsUnix && (path.EndsWith(".html") || path.EndsWith(".htm")))
				{
					Process.Start(_webBrowserProgramLinux, Enquote(path));
				}
				else
				{
					Process.Start(path);
				}
			}
			catch (T e)
			{
				exceptionHandler?.Invoke(e);
			}
		}

		/// <summary>
		/// Returns str surrounded by double-quotes.
		/// This is useful for paths containing spaces in Linux.
		/// </summary>
		private static string Enquote(string str)
		{
			return "\"" + str + "\"";
		}

		private EditingHelper EditingHelper => ActiveView?.EditingHelper;

		private void Help_Introduction_To_Lexicography_Clicked(object sender, EventArgs e)
		{
			OpenDocument(Path.Combine(FwDirectoryFinder.CodeDirectory, "Lexicography Introduction", "Introduction to Lexicography.htm"));
		}

		private void Help_Introduction_To_Parsing_Click(object sender, EventArgs e)
		{
			OpenDocument(Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps", "WW-ConceptualIntro", "ConceptualIntroduction.htm"));
		}

		private void Help_Technical_Notes_on_FieldWorks_Send_Receive(object sender, EventArgs e)
		{
			OpenDocument(Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps", "Language Explorer", "Training", "Technical Notes on FieldWorks Send-Receive.pdf"));
		}

		private void Help_Techinical_Notes_On_SFM_Database_Import_Click(object sender, EventArgs e)
		{
			OpenDocument(Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps", "Language Explorer", "Training", "Technical Notes on SFM Database Import.pdf"));
		}

		private void Help_Technical_Notes_On_LinguaLinks_Import_Click(object sender, EventArgs e)
		{
			OpenDocument(Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps", "Language Explorer", "Training", "Technical Notes on LinguaLinks Database Import.pdf"));
		}

		private void Help_Technical_Notes_On_Interlinear_Import_Click(object sender, EventArgs e)
		{
			OpenDocument(Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps", "Language Explorer", "Training", "Technical Notes on Interlinear Import.pdf"));
		}

		private void Help_ReportProblem(object sender, EventArgs e)
		{
			ErrorReporter.ReportProblem(FwRegistryHelper.FieldWorksRegistryKey, _flexApp.SupportEmailAddress, this);
		}

		private void Help_Make_a_Suggestion(object sender, EventArgs e)
		{
			ErrorReporter.MakeSuggestion(FwRegistryHelper.FieldWorksRegistryKey, "FLExDevteam@sil.org", this);
		}

		private void Help_About_Language_Explorer(object sender, EventArgs e)
		{
			using (var helpAboutWnd = new FwHelpAbout())
			{
				helpAboutWnd.ProductExecutableAssembly = Assembly.LoadFile(_flexApp.ProductExecutableFile);
				helpAboutWnd.ShowDialog();
			}
		}

		private void File_New_FieldWorks_Project(object sender, EventArgs e)
		{
			if (_flexApp.ActiveMainWindow != this)
			{
				throw new InvalidOperationException("Unexpected active window for app.");
			}
			_flexApp.FwManager.CreateNewProject();
		}

		private void File_Open(object sender, EventArgs e)
		{
			_flexApp.FwManager.ChooseLangProject();
		}

		private void File_FieldWorks_Project_Properties(object sender, EventArgs e)
		{
			var cache = _flexApp.Cache;
			if (!SharedBackendServicesHelper.WarnOnOpeningSingleUserDialog(cache))
			{
				return;
			}
			var fDbRenamed = false;
			var sProject = cache.ProjectId.Name;
			var sLinkedFilesRootDir = cache.LangProject.LinkedFilesRootDir;
			using (var dlg = new FwProjPropertiesDlg(cache, _flexApp, _flexApp))
			{
				dlg.ProjectPropertiesChanged += ProjectProperties_Changed;
				if (dlg.ShowDialog(this) != DialogResult.Abort)
				{
					// NOTE: This code is called, even if the user cancelled the dlg.
					fDbRenamed = dlg.ProjectNameChanged();
					if (fDbRenamed)
					{
						sProject = dlg.ProjectName;
					}
					var fFilesMoved = false;
					if (dlg.LinkedFilesChanged())
					{
						fFilesMoved = _flexApp.UpdateExternalLinks(sLinkedFilesRootDir);
					}
					// no need for any of these refreshes if entire window has been/will be
					// destroyed and recreated.
					if (!fDbRenamed && !fFilesMoved)
					{
						SetWindowTitle();
					}
				}
			}
			if (fDbRenamed)
			{
				_flexApp.FwManager.RenameProject(sProject);
			}
		}

		private void CmdVernacularWritingSystemProperties_Click(object sender, EventArgs e)
		{
			var model = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Vernacular, Cache.ServiceLocator.WritingSystemManager, Cache);
			model.WritingSystemListUpdated += OnWritingSystemListChanged;
			using (var view = new FwWritingSystemSetupDlg(model, _propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), _flexApp))
			{
				view.ShowDialog(this);
			}
			model.WritingSystemListUpdated -= OnWritingSystemListChanged;
		}

		private void CmdAnalysisWritingSystemProperties_Click(object sender, EventArgs e)
		{
			var model = new FwWritingSystemSetupModel(Cache.LangProject, FwWritingSystemSetupModel.ListType.Analysis, Cache.ServiceLocator.WritingSystemManager, Cache);
			model.WritingSystemListUpdated += OnWritingSystemListChanged;
			using (var view = new FwWritingSystemSetupDlg(model, _propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), _flexApp))
			{
				view.ShowDialog(this);
			}
			model.WritingSystemListUpdated -= OnWritingSystemListChanged;
		}

		private void OnWritingSystemListChanged(object sender, EventArgs eventArgs)
		{
			// this event is fired before the WritingSystemProperties dialog is closed, so that we have a chance
			// to refresh everything before Paint events start getting fired, which can cause problems if
			// any writing systems are removed that a rootsite is currently displaying
			var allWindows = new List<IFwMainWnd>();
			foreach (var window in _flexApp.MainWindows)
			{
				window.PrepareToRefresh();
				if (window != this)
				{
					allWindows.Add(window);
				}
			}
			foreach (var window in allWindows)
			{
				window.FinishRefresh();
				window.RefreshAllViews();
			}
			FinishRefresh();
			RefreshAllViews();
			Activate();

			ReversalIndexServices.CreateOrRemoveReversalIndexConfigurationFiles(Cache.ServiceLocator.WritingSystemManager,
				Cache, FwDirectoryFinder.DefaultConfigurations, FwDirectoryFinder.ProjectsDirectory, Cache.ProjectId.Name);
		}

		private void SetWindowTitle()
		{
			Text = $@"{_flexApp.Cache.ProjectId.UiName} - {FwUtils.ksSuiteName}";
		}

		private void ProjectProperties_Changed(object sender, EventArgs eventArgs)
		{
			// this event is fired before the Project Properties dialog is closed, so that we have a chance
			// to refresh everything before Paint events start getting fired, which can cause problems if
			// any writing systems are removed that a rootsite is currently displaying
			var dlg = (FwProjPropertiesDlg)sender;
			if (dlg.WritingSystemsChanged())
			{
				View_Refresh(sender, eventArgs);
				ReversalIndexServices.CreateOrRemoveReversalIndexConfigurationFiles(Cache.ServiceLocator.WritingSystemManager, Cache, FwDirectoryFinder.DefaultConfigurations, FwDirectoryFinder.ProjectsDirectory, dlg.OriginalProjectName);
			}
		}

		private Tuple<bool, bool> CanCmdRefresh => new Tuple<bool, bool>(true, _currentTool.MachineName != AreaServices.GrammarSketchMachineName);

		/// <summary>
		/// This is the one (and should be only) handler for the user Refresh command.
		/// Refresh wants to first clean up the cache, then give things like record lists a
		/// chance to reload stuff (calling the old OnRefresh methods), then give
		/// windows a chance to redisplay themselves.
		/// </summary>
		/// <remarks>
		/// Areas/Tools can decide to disable the F5  menu and toolbar refresh, if they wish. One (GrammarSketchTool) is known to do that.
		/// </remarks>
		private void View_Refresh(object sender, EventArgs e)
		{
			RefreshAllViews();
		}

		private void File_Back_up_this_Project(object sender, EventArgs e)
		{
			SaveSettings();
			_flexApp.FwManager.BackupProject(this);
		}

		private void File_Restore_a_Project(object sender, EventArgs e)
		{
			_flexApp.FwManager.RestoreProject(_flexApp, this);
		}

		private static Tuple<bool, bool> CanCmdProjectLocation => new Tuple<bool, bool>(true, FwRegistryHelper.FieldWorksRegistryKeyLocalMachine.CanWriteKey());

		private void File_Project_Location(object sender, EventArgs e)
		{
			_flexApp.FwManager.FileProjectLocation(_flexApp, this);
		}

		private void File_Delete_Project(object sender, EventArgs e)
		{
			_flexApp.FwManager.DeleteProject(_flexApp, this);
		}

		private void File_Create_Shortcut_on_Desktop(object sender, EventArgs e)
		{
			var directory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			if (!FileUtils.DirectoryExists(directory))
			{
				MessageBoxUtils.Show($"Error: Cannot create project shortcut because destination directory '{directory}' does not exist.");
				return;
			}
			var applicationArguments = "-" + FwAppArgs.kProject + " \"" + _flexApp.Cache.ProjectId.Handle + "\"";
			var description = ResourceHelper.FormatResourceString("kstidCreateShortcutLinkDescription", _flexApp.Cache.ProjectId.UiName, _flexApp.ApplicationName);
			if (MiscUtils.IsUnix)
			{
				var projectName = _flexApp.Cache.ProjectId.UiName;
				const string pathExtension = ".desktop";
				var launcherPath = Path.Combine(directory, projectName + pathExtension);
				// Choose a different name if already in use
				var tailNumber = 2;
				while (FileUtils.SimilarFileExists(launcherPath))
				{
					var tail = "-" + tailNumber;
					launcherPath = Path.Combine(directory, projectName + tail + pathExtension);
					tailNumber++;
				}
				const string applicationExecutablePath = "fieldworks-flex";
				const string iconPath = "fieldworks-flex";
				if (string.IsNullOrEmpty(applicationExecutablePath))
				{
					return;
				}
				var content = string.Format("[Desktop Entry]{0}" + "Version=1.0{0}" + "Terminal=false{0}" + "Exec=" + applicationExecutablePath + " " + applicationArguments + "{0}" +
					"Icon=" + iconPath + "{0}" + "Type=Application{0}" + "Name=" + projectName + "{0}" + "Comment=" + description + "{0}", Environment.NewLine);
				// Don't write a BOM
				using (var launcher = FileUtils.OpenFileForWrite(launcherPath, new UTF8Encoding(false)))
				{
					launcher.Write(content);
					FileUtils.SetExecutable(launcherPath);
				}
			}
			else
			{
				WshShell shell = new WshShellClass();
				var filename = _flexApp.Cache.ProjectId.UiName;
				filename = Path.ChangeExtension(filename, "lnk");
				var linkPath = Path.Combine(directory, filename);
				var link = (IWshShortcut)shell.CreateShortcut(linkPath);
				if (link.FullName != linkPath)
				{
					var msg = string.Format(LanguageExplorerResources.ksCannotCreateShortcut, _flexApp.ProductExecutableFile + " " + applicationArguments);
					MessageBox.Show(ActiveForm, msg, LanguageExplorerResources.ksCannotCreateShortcutCaption, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
				link.TargetPath = _flexApp.ProductExecutableFile;
				link.Arguments = applicationArguments;
				link.Description = description;
				link.IconLocation = link.TargetPath + ",0";
				link.Save();
			}
		}

		private static Tuple<bool, bool> CanCmdArchiveWithRamp => new Tuple<bool, bool>(true, ReapRamp.Installed);

		private void File_Archive_With_RAMP(object sender, EventArgs e)
		{
			List<string> filesToArchive = null;
			using (var dlg = new ArchiveWithRamp(Cache, _flexApp))
			{
				// prompt the user to select or create a FieldWorks backup
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					filesToArchive = dlg.FilesToArchive;
				}
			}
			// if there are no files to archive, return now.
			if ((filesToArchive == null) || (filesToArchive.Count == 0))
			{
				return;
			}
			// show the RAMP dialog
			var ramp = new ReapRamp();
			ramp.ArchiveNow(this, MainMenuStrip.Font, Icon, filesToArchive, PropertyTable, _flexApp, Cache);
		}

		private Tuple<bool, bool> CanCmdUploadToWebonary => new Tuple<bool, bool>(true, _currentArea.MachineName == AreaServices.LexiconAreaMachineName);

		private void UploadToWebonary_Click(object sender, EventArgs e)
		{
			DictionaryConfigurationServices.ShowUploadToWebonaryDialog(_majorFlexComponentParameters);
		}

		private void File_Translated_List_Content(object sender, EventArgs e)
		{
			string filename;
			// ActiveForm can go null (see FWNX-731), so cache its value, and check whether
			// we need to use 'this' instead (which might be a better idea anyway).
			var form = ActiveForm ?? this;
			using (var dlg = new OpenFileDialogAdapter())
			{
				dlg.CheckFileExists = true;
				dlg.RestoreDirectory = true;
				dlg.Title = ResourceHelper.GetResourceString("kstidOpenTranslatedLists");
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = ResourceHelper.FileFilter(FileFilterType.FieldWorksTranslatedLists);
				if (dlg.ShowDialog(form) != DialogResult.OK)
				{
					return;
				}
				filename = dlg.FileName;
			}
			using (new WaitCursor(form, true))
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () =>
				{
					using (var dlg = new ProgressDialogWithTask(this))
					{
						dlg.AllowCancel = true;
						dlg.Maximum = 200;
						dlg.Message = filename;
						dlg.RunTask(true, LcmCache.ImportTranslatedLists, filename, Cache);
					}
				});
			}
		}

		private void NewWindow_Clicked(object sender, EventArgs e)
		{
			SaveSettings();
			_flexApp.FwManager.OpenNewWindowForApp(this);
		}

		private void Help_Training_Writing_Systems(object sender, EventArgs e)
		{
			var pathnameToWritingSystemHelpFile = Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer", "Training", "Technical Notes on Writing Systems.pdf");
			OpenDocument(pathnameToWritingSystemHelpFile, err =>
			{
				MessageBox.Show(this, string.Format(LanguageExplorerResources.ksCannotShowX, pathnameToWritingSystemHelpFile), LanguageExplorerResources.ksError);
			});
		}

		private void Help_XLingPaper(object sender, EventArgs e)
		{
			var xLingPaperPathname = Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps", "XLingPap", "UserDoc.htm");
			OpenDocument(xLingPaperPathname, err =>
			{
				MessageBox.Show(this, string.Format(LanguageExplorerResources.ksCannotShowX, xLingPaperPathname), LanguageExplorerResources.ksError);
			});
		}

		private Tuple<bool, bool> CanCmdCut
		{
			get
			{
				var editHelper = EditingHelper as RootSiteEditingHelper;
				return new Tuple<bool, bool>(true, editHelper != null && editHelper.CanCut());
			}
		}

		private void Edit_Cut(object sender, EventArgs e)
		{
			using (new DataUpdateMonitor(this, "EditCut"))
			{
				_viewHelper.ActiveView.EditingHelper.CutSelection();
			}
		}

		private Tuple<bool, bool> CanCmdCopy
		{
			get
			{
				var editHelper = EditingHelper as RootSiteEditingHelper;
				return new Tuple<bool, bool>(true, editHelper != null && editHelper.CanCopy());
			}
		}

		private void Edit_Copy(object sender, EventArgs e)
		{
			_viewHelper.ActiveView.EditingHelper.CopySelection();
		}

		private Tuple<bool, bool> CanCmdPaste
		{
			get
			{
				var editHelper = EditingHelper as RootSiteEditingHelper;
				return new Tuple<bool, bool>(true, editHelper != null && editHelper.CanPaste());
			}
		}

		private void Edit_Paste(object sender, EventArgs e)
		{
			string stUndo, stRedo;
			ResourceHelper.MakeUndoRedoLabels("kstidEditPaste", out stUndo, out stRedo);
			using (var undoHelper = new UndoableUnitOfWorkHelper(Cache.ServiceLocator.GetInstance<IActionHandler>(), stUndo, stRedo))
			using (new DataUpdateMonitor(this, "EditPaste"))
			{
				if (_viewHelper.ActiveView.EditingHelper.PasteClipboard())
				{
					undoHelper.RollBack = false;
				}
			}
		}

		private void SetupEditUndoAndRedoMenus()
		{
			// Set basic values on the pessimistic side.
			var ah = Cache.DomainDataByFlid.GetActionHandler();
			string rawUndoText;
			var undoEnabled = ah.UndoableSequenceCount > 0;
			var redoEnabled = ah.RedoableSequenceCount > 0;
			string rawRedoText;
			// Q: Is the focused control an instance of IUndoRedoHandler
			var asIUndoRedoHandler = FocusedControl as IUndoRedoHandler;
			if (asIUndoRedoHandler != null)
			{
				// A1: Yes: Let it set up the text and enabled state for both menus.
				// The handler may opt to not fret about, or or the other,
				// so this method may need to deal with in the end, after all.
				rawUndoText = asIUndoRedoHandler.UndoText;
				undoEnabled = asIUndoRedoHandler.UndoEnabled(undoEnabled);
				rawRedoText = asIUndoRedoHandler.RedoText;
				redoEnabled = asIUndoRedoHandler.RedoEnabled(redoEnabled);
			}
			else
			{
				// A2: No: Deal with it here.
				// Normal undo processing.
				var baseUndo = undoEnabled ? ah.GetUndoText() : LanguageExplorerResources.Undo;
				rawUndoText = string.IsNullOrEmpty(baseUndo) ? LanguageExplorerResources.Undo : baseUndo;
				// Normal Redo processing.
				var baseRedo = redoEnabled ? ah.GetRedoText() : LanguageExplorerResources.Redo;
				rawRedoText = string.IsNullOrEmpty(baseRedo) ? LanguageExplorerResources.Redo : baseRedo;
			}
			undoToolStripMenuItem.Text = FwUtils.ReplaceUnderlineWithAmpersand(rawUndoText);
			undoToolStripMenuItem.Enabled = undoEnabled;
			redoToolStripMenuItem.Text = FwUtils.ReplaceUnderlineWithAmpersand(rawRedoText);
			redoToolStripMenuItem.Enabled = redoEnabled;
			// Standard toolstrip buttons.
			Toolbar_CmdUndo.Enabled = undoEnabled;
			Toolbar_CmdRedo.Enabled = redoEnabled;
		}

		private Tuple<bool, bool> CanCmdPasteHyperlink
		{
			get
			{
				var editHelper = EditingHelper as RootSiteEditingHelper;
				return new Tuple<bool, bool>(true, editHelper != null && editHelper.CanPasteUrl() && editHelper.CanInsertLinkToFile());
			}
		}

		private void Edit_Paste_Hyperlink(object sender, EventArgs e)
		{
			((RootSiteEditingHelper)_viewHelper.ActiveView.EditingHelper).PasteUrl(_stylesheet);
		}

		private static Tuple<bool, bool> CanCmdSelectAll => new Tuple<bool, bool>(true, !DataUpdateMonitor.IsUpdateInProgress());

		private void Edit_Select_All(object sender, EventArgs e)
		{
#if JASONTODO
/*
	Jason Naylor's expanded comment on potential issues.
	Things to keep in mind:

	"I think if anything my comment regards a potential design improvement that is
beyond the scope of this current change. You might want to have a quick look at the
LanguageExplorer.Areas.TextsAndWords.Tools.CorpusStatistics.StatisticsView and how it will play into this. You may find nothing
that needs to change. I wrote that class way back before I understood how many tentacles
the xWindow and other 'x' classes had. We needed a very simple view of data that wasn't
in any of our blessed RecordLists. I wrote this view with the idea that it would be tied
into the xBeast as little as possible. This was years ago.

	After your changes I expect that something like this would be easier to do, and easy to
make more functional. The fact that the ActiveView is an IRootSite factors in to what kind
of views we are able to have.

	Just giving you more to think about while you're working on these
very simple minor adjustments. ;)"
*/
#endif
			using (new WaitCursor(this))
			{
				_viewHelper.ActiveView.EditingHelper.SelectAll();
			}
		}

		private void utilitiesToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new UtilityDlg(_flexApp))
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ShowDialog(this);
			}
		}

		#region Overrides of Form

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Load"/> event.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data. </param>
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			var currentArea = _areaRepository.PersistedOrDefaultArea;
			var currentTool = currentArea.PersistedOrDefaultTool;
			_sidePane.TabClicked += Area_Clicked;
			_sidePane.ItemClicked += Tool_Clicked;
			// This call fires Area_Clicked and then Tool_Clicked to make sure the provided tab and item are both selected in the end.
			_sidePane.SelectItem(_sidePane.GetTabByName(currentArea.MachineName), currentTool.MachineName);
		}

		/// <inheritdoc />
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			// Set WM_CLASS on Linux (LT-19085)
			if (Platform.IsLinux && _flexApp != null)
			{
				SIL.Windows.Forms.Miscellaneous.X11.SetWmClass(_flexApp.WindowClassName, Handle);
			}
		}

		#endregion

		private void File_Import_Standard_Format_Marker_Click(object sender, EventArgs e)
		{
			using (var importWizard = new LexImportWizard())
			{
				var wndActive = _majorFlexComponentParameters.MainWindow;
				((IFwExtension)importWizard).Init(_majorFlexComponentParameters.LcmCache, wndActive.PropertyTable, wndActive.Publisher);
				importWizard.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
			}
		}

		private void Tools_Options_Click(object sender, EventArgs e)
		{
			using (var optionsDlg = new LexOptionsDlg())
			{
				AreaServices.HandleDlg(optionsDlg, Cache, _flexApp, this, PropertyTable, Publisher);
			}
		}

		private Tuple<bool, bool> CanCmdUndo
		{
			get
			{
				// Set basic values on the pessimistic side.
				var ah = Cache.DomainDataByFlid.GetActionHandler();
				string rawUndoText;
				var undoEnabled = ah.UndoableSequenceCount > 0;
				// Q: Is the focused control an instance of IUndoRedoHandler
				var asIUndoRedoHandler = FocusedControl as IUndoRedoHandler;
				if (asIUndoRedoHandler != null)
				{
					// A1: Yes: Let it set up the text and enabled state for both menus.
					// The handler may opt to not fret about, or or the other,
					// so this method may need to deal with in the end, after all.
					rawUndoText = asIUndoRedoHandler.UndoText;
					undoEnabled = asIUndoRedoHandler.UndoEnabled(undoEnabled);
				}
				else
				{
					// A2: No: Deal with it here.
					// Normal undo processing.
					var baseUndo = undoEnabled ? ah.GetUndoText() : LanguageExplorerResources.Undo;
					rawUndoText = string.IsNullOrEmpty(baseUndo) ? LanguageExplorerResources.Undo : baseUndo;
				}
				undoToolStripMenuItem.Text = FwUtils.ReplaceUnderlineWithAmpersand(rawUndoText);
				return new Tuple<bool, bool>(true, undoEnabled);
			}
		}

		private void Edit_Undo_Click(object sender, EventArgs e)
		{
			var focusedControl = FocusedControl;
			var asIUndoRedoHandler = focusedControl as IUndoRedoHandler;
			if (asIUndoRedoHandler != null)
			{
				if (asIUndoRedoHandler.HandleUndo(sender, e))
				{
					return;
				}
			}

			// For all the bother, the IUndoRedoHandler impl couldn't be bothered, so do it here.
			var ah = Cache.DomainDataByFlid.GetActionHandler();
			using (new WaitCursor(this))
			{
				try
				{
					_inUndoRedo = true;
					ah.Undo();
				}
				finally
				{
					_inUndoRedo = false;
				}
			}
			// Trigger a selection changed, to force updating of controls like the writing system combo
			// that might be affected, if relevant.
			var focusRootSite = focusedControl as SimpleRootSite;
			if (focusRootSite != null && !focusRootSite.IsDisposed && focusRootSite.RootBox?.Selection != null)
			{
				focusRootSite.SelectionChanged(focusRootSite.RootBox, focusRootSite.RootBox.Selection);
			}
			_recordListRepositoryForTools.ActiveRecordList?.UpdateRecordTreeBar();
		}

		private Tuple<bool, bool> CanCmdRedo
		{
			get
			{
				// Set basic values on the pessimistic side.
				var ah = Cache.DomainDataByFlid.GetActionHandler();
				var redoEnabled = ah.RedoableSequenceCount > 0;
				string rawRedoText;
				// Q: Is the focused control an instance of IUndoRedoHandler
				var asIUndoRedoHandler = FocusedControl as IUndoRedoHandler;
				if (asIUndoRedoHandler != null)
				{
					// A1: Yes: Let it set up the text and enabled state for both menus.
					// The handler may opt to not fret about, or or the other,
					// so this method may need to deal with in the end, after all.
					rawRedoText = asIUndoRedoHandler.RedoText;
					redoEnabled = asIUndoRedoHandler.RedoEnabled(redoEnabled);
				}
				else
				{
					// A2: No: Deal with it here.
					// Normal undo processing.
					// Normal Redo processing.
					var baseRedo = redoEnabled ? ah.GetRedoText() : LanguageExplorerResources.Redo;
					rawRedoText = string.IsNullOrEmpty(baseRedo) ? LanguageExplorerResources.Redo : baseRedo;
				}
				redoToolStripMenuItem.Text = FwUtils.ReplaceUnderlineWithAmpersand(rawRedoText);
				return new Tuple<bool, bool>(true, redoEnabled);
			}
		}

		private void Edit_Redo_Click(object sender, EventArgs e)
		{
			var focusedControl = FocusedControl;
			var asIUndoRedoHandler = focusedControl as IUndoRedoHandler;
			if (asIUndoRedoHandler != null)
			{
				if (asIUndoRedoHandler.HandleRedo(sender, e))
				{
					return;
				}
			}
			// For all the bother, the IUndoRedoHandler impl couldn't be bothered, so do it here.
			var ah = Cache.DomainDataByFlid.GetActionHandler();
			using (new WaitCursor(this))
			{
				try
				{
					_inUndoRedo = true;
					ah.Redo();
				}
				finally
				{
					_inUndoRedo = false;
				}
			}
			_recordListRepositoryForTools.ActiveRecordList?.UpdateRecordTreeBar();
		}

		private void Format_Styles_Click(object sender, EventArgs e)
		{
			string paraStyleName = null;
			string charStyleName = null;
			bool refreshAllViews;
			var stylesXmlAccessor = new FlexStylesXmlAccessor(Cache.LanguageProject.LexDbOA);
			StVc vc = null;
			IVwRootSite activeViewSite = null;
			if (ActiveView != null)
			{
				if (ActiveView.EditingHelper == null) // If the calling location doesn't provide this just bring up the dialog with normal selected
				{
					paraStyleName = "Normal";
				}
				vc = ActiveView.EditingHelper.ViewConstructor as StVc;
				activeViewSite = ActiveView.CastAsIVwRootSite();
				if (paraStyleName == null && ActiveView.EditingHelper.CurrentSelection?.Selection != null)
				{
					// If the caller didn't know the default style, try to figure it out from // the selection.
					GetStyleNames((SimpleRootSite)ActiveView, ActiveView.EditingHelper.CurrentSelection.Selection, ref paraStyleName, ref charStyleName);
				}
			}
			using (var stylesDlg = new FwStylesDlg(activeViewSite, Cache, _stylesheet, vc?.RightToLeft ?? false, Cache.ServiceLocator.WritingSystems.AllWritingSystems.Any(ws => ws.RightToLeftScript),
				_stylesheet.GetDefaultBasedOnStyleName(), _flexApp.MeasurementSystem, paraStyleName, charStyleName, _flexApp, _flexApp))
			{
				stylesDlg.SetPropsToFactorySettings = stylesXmlAccessor.SetPropsToFactorySettings;
				stylesDlg.StylesRenamedOrDeleted += OnStylesRenamedOrDeleted;
				stylesDlg.AllowSelectStyleTypes = true;
				stylesDlg.CanSelectParagraphBackgroundColor = true;
				refreshAllViews = stylesDlg.ShowDialog(this) == DialogResult.OK && ((stylesDlg.ChangeType & StyleChangeType.DefChanged) > 0 || (stylesDlg.ChangeType & StyleChangeType.Added) > 0);
			}
			if (refreshAllViews)
			{
				RefreshAllViews();
			}
		}

		private void GetStyleNames(IVwRootSite rootsite, IVwSelection sel, ref string paraStyleName, ref string charStyleName)
		{
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(sel, out vttp, out vvps, out cttp);
			var fSingleStyle = true;
			string sStyle = null;
			for (var ittp = 0; ittp < cttp; ++ittp)
			{
				var style = vttp[ittp].Style();
				if (ittp == 0)
				{
					sStyle = style;
				}
				else if (sStyle != style)
				{
					fSingleStyle = false;
				}
			}
			if (fSingleStyle && !string.IsNullOrEmpty(sStyle))
			{
				if (_stylesheet.GetType(sStyle) == (int)StyleType.kstCharacter)
				{
					if (sel.CanFormatChar)
					{
						charStyleName = sStyle;
					}
				}
				else
				{
					if (sel.CanFormatPara)
					{
						paraStyleName = sStyle;
					}
				}
			}
			if (paraStyleName != null)
			{
				return;
			}
			// Look at the paragraph (if there is one) to get the paragraph style.
			var helper = SelectionHelper.GetSelectionInfo(sel, rootsite);
			var info = helper.GetLevelInfo(SelLimitType.End);
			if (info.Length == 0)
			{
				return;
			}
			var hvo = info[0].hvo;
			if (hvo == 0)
			{
				return;
			}
			var cmObjectRepository = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (!cmObjectRepository.IsValidObjectId(hvo))
			{
				return;
			}
			var cmo = cmObjectRepository.GetObject(hvo);
			if (cmo is IStPara)
			{
				paraStyleName = (cmo as IStPara).StyleName;
			}
		}

		/// <summary>
		/// Called when styles are renamed or deleted.
		/// </summary>
		private void OnStylesRenamedOrDeleted()
		{
			PrepareToRefresh();
			Refresh();
		}

		private Tuple<bool, bool> CanCmdFormatApplyStyle
		{
			get
			{
				var helper = EditingHelper;
				if (helper == null)
				{
					return new Tuple<bool, bool>(true, false);
				}
				var selectionHelper = helper.CurrentSelection;
				if (selectionHelper == null)
				{
					return new Tuple<bool, bool>(true, false);
				}
				var rootSite = selectionHelper.RootSite as RootSite;
				if (rootSite != null)
				{
					return new Tuple<bool, bool>(true, rootSite.CanApplyStyle);
				}
				var selection = selectionHelper.Selection;
				return new Tuple<bool, bool>(true, selection != null && selection.IsEditable && (selection.CanFormatChar || selection.CanFormatPara));
			}
		}

		private void applyStyleToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// Disabled if there is no ActiveView and it cannot appy the styles.
			SimpleRootSite rootsite = null;
			try
			{
				rootsite = ActiveView as SimpleRootSite;
				if (rootsite != null)
				{
					rootsite.ShowRangeSelAfterLostFocus = true;
				}
				var sel = _viewHelper.ActiveView.EditingHelper.CurrentSelection.Selection;
				// Try to get the style names from the selection.
				string paraStyleName = null;
				string charStyleName = null;
				GetStyleNames(rootsite, sel, ref paraStyleName, ref charStyleName);
				int hvoRoot, frag;
				IVwViewConstructor vc;
				IVwStylesheet ss;
				ActiveView.CastAsIVwRootSite().RootBox.GetRootObject(out hvoRoot, out vc, out frag, out ss);
				using (var applyStyleDlg = new FwApplyStyleDlg(Cache, _stylesheet, paraStyleName, charStyleName, _flexApp))
				{
					applyStyleDlg.ApplicableStyleContexts = new List<ContextValues>(new[] { ContextValues.General });
					applyStyleDlg.CanApplyCharacterStyle = sel.CanFormatChar;
					applyStyleDlg.CanApplyParagraphStyle = sel.CanFormatPara;
					if (applyStyleDlg.ShowDialog(this) != DialogResult.OK)
					{
						return;
					}
					string sUndo, sRedo;
					ResourceHelper.MakeUndoRedoLabels("kstidUndoApplyStyle", out sUndo, out sRedo);
					using (var helper = new UndoTaskHelper(Cache.ActionHandlerAccessor, ActiveView.CastAsIVwRootSite(), sUndo, sRedo))
					{
						_viewHelper.ActiveView.EditingHelper.ApplyStyle(applyStyleDlg.StyleChosen);
						helper.RollBack = false;
					}
				}
			}
			finally
			{
				if (rootsite != null)
				{
					rootsite.ShowRangeSelAfterLostFocus = false;
				}
			}
		}

		/// <summary>
		/// When a new FwMainWnd is a copy of another FwMainWnd and the new FwMainWnd's Show()
		/// method is called, the .Net framework will mysteriously add the height of the menu
		/// bar (and border) to the preset window height.
		/// Aaarghhh! So we intercept the CreateParams in order to set it back to the desired
		/// height.
		/// </summary>
		protected override CreateParams CreateParams
		{
			get
			{
				var cp = base.CreateParams;
				if (_windowIsCopy)
				{
					cp.Height = Height;
				}
				return cp;
			}
		}

		private void restoreDefaultsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (var dlg = new RestoreDefaultsDlg(_flexApp))
			{
				if (dlg.ShowDialog(this) != DialogResult.Yes)
				{
					return;
				}
			}
			_flexApp.InitializePartInventories(null, false);
			_flexApp.ReplaceMainWindow(this);
		}

		private Tuple<bool, bool> CanCmdDeleteRecord
		{
			get
			{
				var activeView = _viewHelper.ActiveView;
				bool enableDelete;
				var activeRecordList = _recordListRepositoryForTools.ActiveRecordList;
				var deleteTextBase = FwUtils.ReplaceUnderlineWithAmpersand(LanguageExplorerResources.DeleteMenu);
				string deleteText;
				var tooltipText = string.Empty;
				if (activeRecordList?.CurrentObject == null)
				{
					// "activeRecordList" may be null. If not, then "activeRecordList.CurrentObject" may be null.
					// Either way, the tool is changing, so disable them.
					enableDelete = false;
					// We don't really want to display "Delete Something", so try to find something more meaningful.
					deleteText = "Delete Something";
					if (activeRecordList != null)
					{
						// Try to dig out the class the hard way from the MDC, if possible.
						var managedMetaDataCache = Cache.GetManagedMetaDataCache();
						if (managedMetaDataCache.FieldExists(activeRecordList.OwningFlid))
						{
							deleteText = string.Format(deleteTextBase, managedMetaDataCache.GetDstClsName(activeRecordList.OwningFlid));
						}
						else if (managedMetaDataCache.FieldExists(activeRecordList.VirtualFlid))
						{
							deleteText = string.Format(deleteTextBase, managedMetaDataCache.GetDstClsName(activeRecordList.VirtualFlid));
						}
					}
				}
				else
				{
					var userFriendlyClassName = activeRecordList.CurrentObject.DisplayNameOfClass();
					tooltipText = string.Format(FwUtils.RemoveUnderline(LanguageExplorerResources.DeleteMenu), userFriendlyClassName);
					deleteText = string.Format(deleteTextBase, userFriendlyClassName);
					// See if a view can do it.
					if (activeView is XmlDocView)
					{
						enableDelete = false;
					}
					else if (activeView is XmlBrowseRDEView)
					{
						enableDelete = ((XmlBrowseRDEView)activeView).SetupDeleteMenu(deleteTextBase, out deleteText);
					}
					else
					{
						// Let record list handle it (maybe).
						enableDelete = activeRecordList.Editable && activeRecordList.CanDelete;
					}
				}
				Toolbar_CmdDeleteRecord.Enabled = deleteToolStripMenuItem.Enabled = enableDelete;
				deleteToolStripMenuItem.Text = deleteText;
				Toolbar_CmdDeleteRecord.ToolTipText = tooltipText;
				return new Tuple<bool, bool>(true, enableDelete);
			}
		}

		private void Edit_Delete_Click(object sender, EventArgs e)
		{
			var activeRecordList = _recordListRepositoryForTools.ActiveRecordList;
			if (activeRecordList.ShouldNotHandleDeletionMessage)
			{
				// Go find some view to handle it.
				var activeView = ActiveView;
				if (activeView is XmlBrowseRDEView)
				{
					// Seems like it is the only main view that can do it.
					((XmlBrowseRDEView)activeView).DeleteRecord();
				}
			}
			else
			{
				// Let the record list do it.
				activeRecordList.DeleteRecord(deleteToolStripMenuItem.Text.Replace("&", null), StatusBarPanelServices.GetStatusBarProgressPanel(_statusbar));
			}
		}

		private Tuple<bool, bool> CanCmdInsertLinkToFile
		{
			get
			{
				var visible = true;
				var enabled = EditingHelper is RootSiteEditingHelper && ((RootSiteEditingHelper)EditingHelper).CanPasteUrl();
				if (PropertyTable.GetValue(AllowInsertLinkToFile, true))
				{
					visible = true;
					enabled = EditingHelper is RootSiteEditingHelper && ((RootSiteEditingHelper)EditingHelper).CanInsertLinkToFile();
				}
				return new Tuple<bool, bool>(visible, enabled);
			}
		}

		private void LinkToFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var helper = EditingHelper as RootSiteEditingHelper;
			if (helper == null || !helper.CanInsertLinkToFile())
			{
				return;
			}
			string pathname;
			using (var fileDialog = new OpenFileDialogAdapter())
			{
				fileDialog.Filter = ResourceHelper.FileFilter(FileFilterType.AllFiles);
				fileDialog.RestoreDirectory = true;
				if (fileDialog.ShowDialog() != DialogResult.OK)
				{
					return;
				}
				pathname = fileDialog.FileName;
			}
			if (string.IsNullOrEmpty(pathname))
			{
				return;
			}
			pathname = MoveOrCopyFilesController.MoveCopyOrLeaveExternalFile(pathname, Cache.LangProject.LinkedFilesRootDir, _flexApp);
			if (string.IsNullOrEmpty(pathname))
			{
				return;
			}
			helper.ConvertSelToLink(pathname, _stylesheet);
		}

		/// <summary>
		/// Linux: Always enable the menu. If it's not installed we display an error message when the user tries to launch it. See FWNX-567 for more info.
		/// Windows: Enable the menu if we can find the CharMap program.
		/// </summary>
		private static Tuple<bool, bool> CanCmdShowCharMap => new Tuple<bool, bool>(true, MiscUtils.IsUnix || File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "charmap.exe")));

		private void SpecialCharacterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var program = MiscUtils.IsUnix ? "gucharmap" : "charmap.exe";
			MiscUtils.RunProcess(program, null, MiscUtils.IsUnix ? exception => { MessageBox.Show(string.Format(DictionaryConfigurationStrings.ksUnableToStartGnomeCharMap, program)); } : (Action<Exception>)null);
		}

		private void showVernacularSpellingErrorsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			var checking = !_propertyTable.GetValue<bool>(LanguageExplorerConstants.UseVernSpellingDictionary);
			if (checking)
			{
				EnableVernacularSpelling();
			}
			else
			{
				WfiWordformServices.DisableVernacularSpellingDictionary(Cache);
			}
			CmdUseVernSpellingDictionary.Checked = checking;
			_propertyTable.SetProperty(LanguageExplorerConstants.UseVernSpellingDictionary, checking, true);
			RestartSpellChecking();
		}

		private Tuple<bool, bool> CanCmdChangeFilterClearAll => new Tuple<bool, bool>(true, _recordListRepositoryForTools.ActiveRecordList?.CanChangeFilterClearAll ?? false);

		private void toolStripButtonChangeFilterClearAll_Click(object sender, EventArgs e)
		{
			_recordListRepositoryForTools.ActiveRecordList.OnChangeFilterClearAll();
		}

		/// <summary>
		/// Enable vernacular spelling.
		/// </summary>
		private void EnableVernacularSpelling()
		{
			// Enable all vernacular spelling dictionaries by changing those that are set to <None>
			// to point to the appropriate Locale ID. Do this BEFORE updating the spelling dictionaries,
			// otherwise, the update won't see that there is any dictionary set to update.
			foreach (var wsObj in Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
			{
				// This allows it to try to find a dictionary, but doesn't force one to exist.
				if (string.IsNullOrEmpty(wsObj.SpellCheckingId) || wsObj.SpellCheckingId == "<None>") // LT-13556 new langs were null here
				{
					wsObj.SpellCheckingId = wsObj.Id.Replace('-', '_');
				}
			}
			// This forces the default vernacular WS spelling dictionary to exist, and updates all existing ones.
			WfiWordformServices.ConformSpellingDictToWordforms(Cache);
		}

		private void RestartSpellChecking()
		{
			_flexApp.RestartSpellChecking();
		}
	}
}