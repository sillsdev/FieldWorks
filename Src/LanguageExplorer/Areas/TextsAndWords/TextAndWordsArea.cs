// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// IArea implementation for the area: "textsWords".
	/// </summary>
	[Export(AreaServices.TextAndWordsAreaMachineName, typeof(IArea))]
	[Export(typeof(IArea))]
	internal sealed class TextAndWordsArea : IArea
	{
		[ImportMany(AreaServices.TextAndWordsAreaMachineName)]
		private IEnumerable<ITool> _myTools;
		internal const string ConcordanceWords = "concordanceWords";
		internal const string InterlinearTexts = "interlinearTexts";
		internal const string Respeller = "Respeller";
		internal const string ITexts_AddWordsToLexicon = "ITexts_AddWordsToLexicon";
		internal const string ShowHiddenFields_interlinearEdit = "ShowHiddenFields_interlinearEdit";
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";
		private TextAndWordsAreaMenuHelper _textAndWordsAreaMenuHelper;
		private bool _hasBeenActivated;
		[Import]
		private IPropertyTable _propertyTable;
		private Dictionary<string, ITool> _dictionaryOfAllTools;
		private ITool _activeTool;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			// This will also remove any event handlers set up by the active tool,
			// and any of the tool's UserControl instances that may have registered event handlers.
			majorFlexComponentParameters.UiWidgetController.RemoveAreaHandlers();
			var activeTool = ActiveTool;
			ActiveTool = null;
			activeTool?.Deactivate(majorFlexComponentParameters);
			_textAndWordsAreaMenuHelper.Dispose();

			_textAndWordsAreaMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_propertyTable.SetDefault(PropertyNameForToolName, AreaServices.TextAndWordsAreaDefaultToolMachineName, true);
			if (!_hasBeenActivated)
			{
				// Respeller dlg uses these.
				_propertyTable.SetDefault("RemoveAnalyses", true, true, settingsGroup: SettingsGroup.GlobalSettings);
				_propertyTable.SetDefault("UpdateLexiconIfPossible", true, true, settingsGroup: SettingsGroup.GlobalSettings);
				_propertyTable.SetDefault("CopyAnalysesToNewSpelling", true, true, settingsGroup: SettingsGroup.GlobalSettings);
				_propertyTable.SetDefault("MaintainCaseOnChangeSpelling", true, true, settingsGroup: SettingsGroup.GlobalSettings);

				_propertyTable.SetDefault(ITexts_AddWordsToLexicon, false, true);
				_propertyTable.SetDefault(ShowHiddenFields_interlinearEdit, false, true);
				_propertyTable.SetDefault("ITexts_ShowAddWordsToLexiconDlg", true, true);
				_propertyTable.SetDefault("ITexts-ScriptureIds", string.Empty, true);
				_hasBeenActivated = true;
			}
			_textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper(this, majorFlexComponentParameters);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			PersistedOrDefaultTool.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
			PersistedOrDefaultTool.FinishRefresh();
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
			_propertyTable.SetProperty(AreaServices.InitialArea, MachineName, true, settingsGroup: SettingsGroup.LocalSettings);

			PersistedOrDefaultTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.TextAndWordsAreaMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.TextAndWordsAreaUiName);

		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool PersistedOrDefaultTool => _dictionaryOfAllTools.Values.First(tool => tool.MachineName == _propertyTable.GetValue(PropertyNameForToolName, AreaServices.TextAndWordsAreaDefaultToolMachineName));

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IReadOnlyDictionary<string, ITool> AllToolsInOrder
		{
			get
			{
				if (_dictionaryOfAllTools == null)
				{
					_dictionaryOfAllTools = new Dictionary<string, ITool>();
					var myBuiltinToolsInOrder = new List<string>
					{
						AreaServices.InterlinearEditMachineName,
						AreaServices.ConcordanceMachineName,
						AreaServices.ComplexConcordanceMachineName,
						AreaServices.WordListConcordanceMachineName,
						AreaServices.AnalysesMachineName,
						AreaServices.BulkEditWordformsMachineName,
						AreaServices.CorpusStatisticsMachineName
					};
					foreach (var toolName in myBuiltinToolsInOrder)
					{
						var currentBuiltinTool = _myTools.First(tool => tool.MachineName == toolName);
						_dictionaryOfAllTools.Add(currentBuiltinTool.UiName, currentBuiltinTool);
					}
					// Add user-defined tools in unspecified order, but after the fully supported tools.
					foreach (var userDefinedTool in _myTools.Where(tool => !myBuiltinToolsInOrder.Contains(tool.MachineName)))
					{
						_dictionaryOfAllTools.Add(userDefinedTool.UiName, userDefinedTool);
					}
				}
				return _dictionaryOfAllTools;
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => LanguageExplorerResources.Text_And_Words.ToBitmap();

		/// <summary>
		/// Set the active tool for the area, or null, if no tool is active.
		/// </summary>
		public ITool ActiveTool
		{
			get { return _activeTool; }
			set
			{
				_activeTool = value;
				_textAndWordsAreaMenuHelper.ActiveTool = value;
			}
		}
		#endregion

		internal static IRecordList ConcordanceWordsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == ConcordanceWords, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{ConcordanceWords}'.");
			/*
            <clerk id="concordanceWords">
              <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.InterlinearTextsRecordClerk" />
              <recordList owner="LangProject" property="Wordforms">
                <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.ConcordanceRecordList" />
                <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.ConcDecorator" />
              </recordList>
              <filters>
                <filter label="Default" assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.WordsUsedOnlyElsewhereFilter" />
              </filters>
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
              <recordFilterListProvider assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.WfiRecordFilterListProvider" />
            </clerk>
			*/
			// NB: The constructor supplies the id, so no need to pass it on.
			var concDecorator = new ConcDecorator(cache.ServiceLocator);
			concDecorator.InitializeFlexComponent(flexComponentParameters);
			return new ConcordanceRecordList(statusBar, cache.LanguageProject, concDecorator);
		}

		internal static IRecordList InterlinearTextsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == InterlinearTexts, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{InterlinearTexts}'.");
			/*
            <clerk id="interlinearTexts">
              <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.InterlinearTextsRecordClerk" />
              <recordList owner="LangProject" property="InterestingTexts">
                <!-- We use a decorator here so it can override certain virtual properties and limit occurrences to interesting texts. -->
                <decoratorClass assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.InterestingTextsDecorator" />
              </recordList>
              <filterMethods />
              <sortMethods />
            </clerk>
			*/
			return new InterlinearTextsRecordList(InterlinearTexts, statusBar, new InterestingTextsDecorator(cache.ServiceLocator, flexComponentParameters.PropertyTable), false, new VectorPropertyParameterObject(cache.LanguageProject, "InterestingTexts", InterestingTextsDecorator.kflidInterestingTexts));
		}

		private sealed class TextAndWordsAreaMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private CustomFieldsMenuHelper _customFieldsMenuHelper;
			private IWfiWordformRepository _wordformRepos;
			private IPropertyTable _propertyTable;
			private LcmCache _cache;
			private IArea _area;
			private ISharedEventHandlers _sharedEventHandlers;

			internal ITool ActiveTool { private get; set; }

			internal TextAndWordsAreaMenuHelper(IArea area, MajorFlexComponentParameters majorFlexComponentParameters)
			{
				Guard.AgainstNull(area, nameof(area));
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_cache = _majorFlexComponentParameters.LcmCache;
				_propertyTable = _majorFlexComponentParameters.FlexComponentParameters.PropertyTable;
				_wordformRepos = _cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
				_area = area;
				_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
				_sharedEventHandlers.Add(Respeller, Respeller_Click);

				SetupUiWidgets();
			}

			private void SetupUiWidgets()
			{
				var areaUiWidgetParameterObject = new AreaUiWidgetParameterObject(_area);
				_customFieldsMenuHelper = new CustomFieldsMenuHelper(_majorFlexComponentParameters, _area, areaUiWidgetParameterObject);
				/*
					<item label="Click Inserts Invisible Space" boolProperty="ClickInvisibleSpace" defaultVisible="false" settingsGroup="local" icon="zeroWidth"/> // Only Insert menu
				*/
				var insertMenuDictionary = areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.Insert];
				insertMenuDictionary.Add(Command.CmdImportWordSet, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportWordSetToolStripMenuItemOnClick, () => UiWidgetServices.CanSeeAndDo));
				var insertToolBarDictionary = areaUiWidgetParameterObject.ToolBarItemsForArea[ToolBar.Insert];
				AreaServices.InsertPair(insertToolBarDictionary, insertMenuDictionary,
					Command.CmdInsertText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertText_Click, () => CanCmdInsertText));
				AreaServices.InsertPair(areaUiWidgetParameterObject.ToolBarItemsForArea[ToolBar.View], areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.View],
					Command.CmdChooseTexts, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(AddTexts_Clicked, () => UiWidgetServices.CanSeeAndDo));
				var toolMenuDictionary = areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.Tools];
				toolMenuDictionary.Add(Command.CmdEditSpellingStatus, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditSpellingStatus_Clicked, ()=> UiWidgetServices.CanSeeAndDo));
				toolMenuDictionary.Add(Command.CmdViewIncorrectWords, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ViewIncorrectWords_Clicked, () => UiWidgetServices.CanSeeAndDo));
				toolMenuDictionary.Add(Command.CmdChangeSpelling, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Respeller_Click, () => CanCmdChangeSpelling));

				_majorFlexComponentParameters.UiWidgetController.AddHandlers(areaUiWidgetParameterObject);
			}

			private void ViewIncorrectWords_Clicked(object sender, EventArgs e)
			{
				FwLinkArgs link = new FwAppArgs(_cache.ProjectId.Handle, AreaServices.AnalysesMachineName, ActiveWordform(_wordformRepos, _propertyTable));
				var additionalProps = link.LinkProperties;
				additionalProps.Add(new LinkProperty("SuspendLoadListUntilOnChangeFilter", link.ToolName));
				additionalProps.Add(new LinkProperty("LinkSetupInfo", "CorrectSpelling"));
				LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, link);
			}

			private void EditSpellingStatus_Clicked(object sender, EventArgs e)
			{
				// Without checking both the SpellingStatus and (virtual) FullConcordanceCount
				// fields for the ActiveWordform() result, it's too likely that the user
				// will get a puzzling "Target not found" message popping up.  See LT-8717.
				FwLinkArgs link = new FwAppArgs(_cache.ProjectId.Handle, AreaServices.BulkEditWordformsMachineName, Guid.Empty);
				var additionalProps = link.LinkProperties;
				additionalProps.Add(new LinkProperty("SuspendLoadListUntilOnChangeFilter", link.ToolName));
				additionalProps.Add(new LinkProperty("LinkSetupInfo", "ReviewUndecidedSpelling"));
				LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, link);
			}

			private void AddTexts_Clicked(object sender, EventArgs e)
			{
				var recordList = (InterlinearTextsRecordList)_propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList;
				recordList.AddTexts();
			}

			private Tuple<bool, bool> CanCmdInsertText => new Tuple<bool, bool>(true, ActiveTool.MachineName == AreaServices.InterlinearEditMachineName);

			private void CmdInsertText_Click(object sender, EventArgs e)
			{
				var recordList = (InterlinearTextsRecordList)_propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList;
				recordList.OnInsertInterlinText();
			}

			private void ImportWordSetToolStripMenuItemOnClick(object sender, EventArgs eventArgs)
			{
				using (var dlg = new ImportWordSetDlg(_cache, _majorFlexComponentParameters.FlexApp, _propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList, _majorFlexComponentParameters.ParserMenuManager))
				{
					dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
				}
			}

			/// <summary>
			/// Try to find a WfiWordform object corresponding the the focus selection.
			/// If successful return its guid, otherwise, return Guid.Empty.
			/// </summary>
			/// <returns></returns>
			private static Guid ActiveWordform(IWfiWordformRepository wordformRepos, IPropertyTable propertyTable)
			{
				var app = propertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
				var window = app?.ActiveMainWindow as IFwMainWnd;
				var activeView = window?.ActiveView;
				if (activeView == null)
				{
					return Guid.Empty;
				}
				var roots = activeView.AllRootBoxes();
				if (!roots.Any())
				{
					return Guid.Empty;
				}
				var helper = SelectionHelper.Create(roots[0].Site);
				var word = helper?.SelectedWord;
				if (word == null || word.Length == 0)
				{
					return Guid.Empty;
				}
				IWfiWordform wordform;
				return wordformRepos.TryGetObject(word, out wordform) ? wordform.Guid : Guid.Empty;
			}

			private Tuple<bool, bool> CanCmdChangeSpelling => new Tuple<bool, bool>(true, string.IsNullOrWhiteSpace(ActiveWord?.Text));

			private void Respeller_Click(object sender, EventArgs e)
			{
				if (!InFriendliestTool)
				{
					// See LT-8641.
					LaunchRespellerDlgOnWord(ActiveWord);
					return;
				}
				var recordList = (IRecordList)((ToolStripMenuItem)sender).Tag;
				using (var luh = new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = recordList }))
				{
					var changesWereMade = false;
					using (var dlg = new RespellerDlg())
					{
						dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
						if (dlg.SetDlgInfo(_majorFlexComponentParameters.StatusBar))
						{
							dlg.ShowDialog((Form)_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IFwMainWnd>(FwUtils.window));
							changesWereMade = dlg.ChangesWereMade;
						}
						else
						{
							MessageBox.Show(TextAndWordsResources.ksCannotRespellWordform);
						}
					}
					// The Respeller dialog can't make all necessary updates, since things like occurrence
					// counts depend on which texts are included, not just the data. So make sure we reload.
					luh.TriggerPendingReloadOnDispose = changesWereMade;
					if (changesWereMade)
					{
						// further try to refresh occurrence counts.
						var sda = recordList.VirtualListPublisher;
						while (sda != null)
						{
							if (sda is ConcDecorator)
							{
								((ConcDecorator)sda).Refresh();
								break;
							}
							if (!(sda is DomainDataByFlidDecoratorBase))
							{
								break;
							}
							sda = ((DomainDataByFlidDecoratorBase)sda).BaseSda;
						}
					}
				}
			}

			private void LaunchRespellerDlgOnWord(ITsString tss)
			{
				using (new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = _propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList }))
				{
					// Launch the Respeller Dlg.
					using (var dlg = new RespellerDlg())
					{
						dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
						if (dlg.SetDlgInfo(WordformApplicationServices.GetWordformForForm(_cache, tss)))
						{
							dlg.ShowDialog((Form)_propertyTable.GetValue<IFwMainWnd>(FwUtils.window));
						}
						else
						{
							MessageBox.Show(TextAndWordsResources.ksCannotRespellWordform);
						}
					}
				}
			}

			private bool InFriendliestTool => _area.ActiveTool.MachineName == AreaServices.AnalysesMachineName;

			/// <summary>
			/// Try to find a WfiWordform object corresponding the the focus selection.
			/// If successful return its guid, otherwise, return Guid.Empty.
			/// </summary>
			/// <returns></returns>
			private ITsString ActiveWord
			{
				get
				{
					if (InFriendliestTool)
					{
						// we should be able to get our info from the current record list.
						// but return null if we can't get the info, otherwise we allow the user to
						// bring up the change spelling dialog and crash because no wordform can be found (LT-8766).
						var recordList = _propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList;
						var tssVern = (recordList?.CurrentObject as IWfiWordform)?.Form?.BestVernacularAlternative;
						return tssVern;
					}
					var window = _majorFlexComponentParameters.MainWindow;
					var roots = window.ActiveView?.AllRootBoxes();
					if (roots == null || roots.Count < 1 || roots[0] == null)
					{
						return null;
					}
					var tssWord = SelectionHelper.Create(roots[0].Site)?.SelectedWord;
					if (tssWord != null)
					{
						// Check for a valid vernacular writing system.  (See LT-8892.)
						var ws = TsStringUtils.GetWsAtOffset(tssWord, 0);
						var cache = _propertyTable.GetValue<LcmCache>(FwUtils.cache);
						var wsObj = cache.ServiceLocator.WritingSystemManager.Get(ws);
						if (cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(
							wsObj))
						{
							return tssWord;
						}
					}
					return null;
				}
			}

			#region IDisposable
			private bool _isDisposed;

			~TextAndWordsAreaMenuHelper()
			{
				// The base class finalizer is called automatically.
				Dispose(false);
			}

			/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SuppressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (_isDisposed)
				{
					// No need to run it more than once.
					return;
				}

				if (disposing)
				{
					_sharedEventHandlers.Remove(Respeller);
					_customFieldsMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_customFieldsMenuHelper = null;
				_wordformRepos = null;
				_propertyTable = null;
				_cache = null;
				_area = null;
				_sharedEventHandlers = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
