// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.Filters;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// IArea implementation for the area: "textsWords".
	/// </summary>
	[Export(LanguageExplorerConstants.TextAndWordsAreaMachineName, typeof(IArea))]
	[Export(typeof(IArea))]
	internal sealed class TextAndWordsArea : IArea
	{
		[ImportMany(LanguageExplorerConstants.TextAndWordsAreaMachineName)]
		private IEnumerable<ITool> _myTools;
		private string PropertyNameForToolName => $"{LanguageExplorerConstants.ToolForAreaNamed_}{MachineName}";
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
			_textAndWordsAreaMenuHelper.Dispose();
			var activeTool = ActiveTool;
			ActiveTool = null;
			activeTool?.Deactivate(majorFlexComponentParameters);

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
			_propertyTable.SetDefault(PropertyNameForToolName, LanguageExplorerConstants.TextAndWordsAreaDefaultToolMachineName, true);
			if (!_hasBeenActivated)
			{
				// Respeller dlg uses these.
				_propertyTable.SetDefault("RemoveAnalyses", true, true, settingsGroup: SettingsGroup.GlobalSettings);
				_propertyTable.SetDefault("UpdateLexiconIfPossible", true, true, settingsGroup: SettingsGroup.GlobalSettings);
				_propertyTable.SetDefault("CopyAnalysesToNewSpelling", true, true, settingsGroup: SettingsGroup.GlobalSettings);
				_propertyTable.SetDefault("MaintainCaseOnChangeSpelling", true, true, settingsGroup: SettingsGroup.GlobalSettings);
				_propertyTable.SetDefault(LanguageExplorerConstants.ITexts_AddWordsToLexicon, false, true);
				_propertyTable.SetDefault(LanguageExplorerConstants.ShowHiddenFields_interlinearEdit, false, true);
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
			_propertyTable.SetProperty(LanguageExplorerConstants.InitialArea, MachineName, true, settingsGroup: SettingsGroup.LocalSettings);

			PersistedOrDefaultTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => LanguageExplorerConstants.TextAndWordsAreaMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(LanguageExplorerConstants.TextAndWordsAreaUiName);

		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool PersistedOrDefaultTool => _dictionaryOfAllTools.Values.First(tool => tool.MachineName == _propertyTable.GetValue(PropertyNameForToolName, LanguageExplorerConstants.TextAndWordsAreaDefaultToolMachineName));

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
						LanguageExplorerConstants.InterlinearEditMachineName,
						LanguageExplorerConstants.ConcordanceMachineName,
						LanguageExplorerConstants.ComplexConcordanceMachineName,
						LanguageExplorerConstants.WordListConcordanceMachineName,
						LanguageExplorerConstants.AnalysesMachineName,
						LanguageExplorerConstants.BulkEditWordformsMachineName,
						LanguageExplorerConstants.CorpusStatisticsMachineName
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
			get => _activeTool;
			set
			{
				_activeTool = value;
				_textAndWordsAreaMenuHelper.ActiveTool = value;
			}
		}
		#endregion

		internal static IRecordList ConcordanceWordsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == LanguageExplorerConstants.ConcordanceWords, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{LanguageExplorerConstants.ConcordanceWords}'.");
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

		/// <summary>
		/// This class provides the RecordList for three Text & Words tools. It exists to provide a pre-filtered
		/// list of Wordforms and to prevent jarring reloads of the Wordlist when the user is indirectly modifying the
		/// lists content. (i.e. typing in the baseline pane in the Text & Words\Word Concordance view)
		///
		/// The contents of this list are a result of parsing the texts and passing the results through a decorator.
		/// </summary>
		private sealed class ConcordanceRecordList : InterlinearTextsRecordList, IConcordanceRecordList
		{
			//the ReloadList() on the RecordList class will trigger if this is true
			//set when the index in the list is changed
			private bool _selectionChanged = true;

			/// <summary />
			internal ConcordanceRecordList(StatusBar statusBar, ILangProject languageProject, ConcDecorator decorator)
				: base(LanguageExplorerConstants.ConcordanceWords, statusBar, decorator, false, new VectorPropertyParameterObject(languageProject, "Wordforms", ObjectListPublisher.OwningFlid), new RecordFilterParameterObject(new WordsUsedOnlyElsewhereFilter(languageProject.Cache)))
			{
				_filterProvider = new WfiRecordFilterListProvider();
			}

			public override void OnChangeFilter(FilterChangeEventArgs args)
			{
				RequestRefresh();
				base.OnChangeFilter(args);
			}

			/// <summary>
			/// Provide a means by which the record list can indicate that we should really refresh our contents.
			/// (used for handling events that the record list processes, like the refresh.
			/// </summary>
			private void RequestRefresh()
			{
				//indicate that a refresh is desired so ReloadList would be triggered by an index change
				ReloadRequested = true;
				//indicate that the selection has changed, ReloadList will now actually reload the list
				_selectionChanged = true;
			}

			/// <summary>
			/// Returns the value that indicates if a reload has been requested (and ignored) by the list
			/// If you want to force a re-load call RequestRefresh
			/// </summary>
			private bool ReloadRequested { get; set; }

			/// <summary>
			/// We want to reload the list on an index change if a reload has been
			/// requested (due to an add or remove)
			/// </summary>
			public override int CurrentIndex
			{
				get => base.CurrentIndex;
				set
				{
					_selectionChanged = true;
					base.CurrentIndex = value;
					// if no one has actually asked for the list to be reloaded it would be a waste to do so
					if (ReloadRequested)
					{
						ReloadList();
					}
				}
			}

			/// <summary>
			/// This method should cause all paragraphs in interesting texts which do not have the ParseIsCurrent flag set
			/// to be Parsed.
			/// </summary>
			void IConcordanceRecordList.ParseInterestingTextsIfNeeded()
			{
				ForceReloadList();
			}

			/// <summary>
			/// This is used in situations like switching views. In such cases we should force a reload.
			/// </summary>
			protected override bool NeedToReloadList()
			{
				return base.NeedToReloadList() || ReloadRequested;
			}

			protected override void ChangeSorter(RecordSorter sorter)
			{
				RequestRefresh();
				base.ChangeSorter(sorter);
			}

			protected override void ForceReloadList()
			{
				RequestRefresh();
				base.ForceReloadList();
			}

			/// <summary>
			/// overridden to prevent reloading the list unless it is specifically desired.
			/// </summary>
			protected override void ReloadList()
			{
				if (_selectionChanged || CurrentIndex == -1)
				{
					ReloadRequested = _selectionChanged = false; // BEFORE base call, which could set CurrentIndex and cause stack overflow otherwise
					base.ReloadList();
				}
				else
				{
					ReloadRequested = true;
				}
			}

			// If the source (unfiltered, unsorted) list of objects is being maintained in a private list in the decorator, update it.
			// If this cannot be done at once and the Reload needs to be completed later, return true.
			protected override bool UpdatePrivateList()
			{
				if (m_flid != ObjectListPublisher.OwningFlid)
				{
					return false; // we are not involved in the reload process.
				}
				if (((IActionHandlerExtensions)m_cache.ActionHandlerAccessor).CanStartUow)
				{
					ParseAndUpdate(); // do it now
				}
				else if (m_cache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
				{
					// we're doing an undo or redo action; because of the code below, the UnitOfWork
					// already knows we need to reload the list after we get done with PropChanged.
					// But for the same reasons as in the original action, we aren't ready to do it now.
					return true;
				}
				else // do it as soon as possible. (We might be processing PropChanged)
				{
					// Enhance JohnT: is there some way we can be sure only one of these tasks gets added?
					((IActionHandlerExtensions)m_cache.ActionHandlerAccessor).DoAtEndOfPropChangedAlways(RecordList_PropChangedCompleted);
					return true;
				}
				return false;
			}

			private void ParseAndUpdate()
			{
				var objectListPublisher = (ObjectListPublisher)VirtualListPublisher;
				objectListPublisher.SetOwningPropInfo(WfiWordformTags.kClassId, "WordformInventory", "Wordforms");
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, ParseInterestingTexts);
				objectListPublisher.SetOwningPropValue((m_cache.ServiceLocator.GetInstance<IWfiWordformRepository>().AllInstances().Select(wf => wf.Hvo)).ToArray());
			}

			/// <summary>
			/// Parse (if necessary...ParseIsCurrent will be checked to see) the texts we want in the concordance.
			/// </summary>
			private void ParseInterestingTexts()
			{
				// Also it should be forced to be empty if FwUtils.IsOkToDisplayScriptureIfPresent returns false.
				var scriptureTexts = m_cache.LangProject.TranslatedScriptureOA?.StTexts.Where(IsInterestingScripture) ?? new IStText[0];
				// Enhance JohnT: might eventually want to be more selective here, perhaps a genre filter.
				var vernacularTexts = from st in m_cache.LangProject.Texts select st.ContentsOA;
				// Filtered list that excludes IScrBookAnnotations.
				var texts = vernacularTexts.Concat(scriptureTexts).Where(x => x != null).ToList();
				var count = texts.SelectMany(text => text.ParagraphsOS).Count();
				var done = 0;
				using (var progress = PropertyTable.GetValue<IFwMainWnd>(FwUtilsConstants.window).CreateSimpleProgressState())
				{
					progress.SetMilestone(TextAndWordsResources.ksParsing);
					foreach (var para in texts.SelectMany(text => text.ParagraphsOS.Cast<IStTxtPara>()))
					{
						done++;
						var newPercentDone = done * 100 / count;
						if (newPercentDone != progress.PercentDone)
						{
							progress.PercentDone = newPercentDone;
							progress.Breath();
						}
						if (para.ParseIsCurrent)
						{
							continue;
						}
						ParagraphParser.ParseParagraph(para);
					}
				}
			}

			private bool IsInterestingScripture(IStText text)
			{
				// Typically this question only arises where we have a ConcDecorator involved.
				if (VirtualListPublisher is DomainDataByFlidDecoratorBase domainDataByFlidDecoratorBase)
				{
					if (domainDataByFlidDecoratorBase.BaseSda is ConcDecorator concDecorator)
					{
						return concDecorator.IsInterestingText(text);
					}
				}
				return true; // if by any chance this is used without a conc decorator, assume all Scripture is interesting.
			}

			// This is invoked when there have been significant changes but we can't reload immediately
			// because we need to finish handling PropChanged first. We want to really reload the list,
			// not just record that it might be a nice idea sometime.
			// Enhance: previously we were calling ReloadList. That could leave the list in an invalid
			// state and the UI displaying deleted objects and lead to crashes (LT-18976), since
			// there was no guarantee of ever doing a real reload. (For example, closing the change
			// spelling dialog forces a real reload by calling MasterRefresh; but undoing that change
			// ended up doing no reload at all, even though this method was called.)
			// However, it's possible that using ForceReloadList here
			// will cause more reloads than are needed, slowing things down. If so, one thing to
			// investigate would be making sure that a request to call this is only added once per UnitOfWork.
			// (See the one use of this function.)
			// It's also possible that the MasterRefresh on closing the change spelling dialog
			// can be removed now we're doing a real reload here.
			// I'm not risking either of those changes at a time when we're trying to stabilize for release.
			private void RecordList_PropChangedCompleted()
			{
				ForceReloadList();
			}
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
				_sharedEventHandlers.Add(Command.CmdRespeller, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Respeller_Click, ()=> UiWidgetServices.CanSeeAndDo));
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
				UiWidgetServices.InsertPair(insertToolBarDictionary, insertMenuDictionary,
					Command.CmdInsertText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertText_Click, () => CanCmdInsertText));
				UiWidgetServices.InsertPair(areaUiWidgetParameterObject.ToolBarItemsForArea[ToolBar.View], areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.View],
					Command.CmdChooseTexts, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(AddTexts_Clicked, () => UiWidgetServices.CanSeeAndDo));
				var toolMenuDictionary = areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.Tools];
				toolMenuDictionary.Add(Command.CmdEditSpellingStatus, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(EditSpellingStatus_Clicked, ()=> UiWidgetServices.CanSeeAndDo));
				toolMenuDictionary.Add(Command.CmdViewIncorrectWords, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ViewIncorrectWords_Clicked, () => UiWidgetServices.CanSeeAndDo));
				toolMenuDictionary.Add(Command.CmdChangeSpelling, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(Respeller_Click, () => CanCmdChangeSpelling));

				_majorFlexComponentParameters.UiWidgetController.AddHandlers(areaUiWidgetParameterObject);
			}

			private void ViewIncorrectWords_Clicked(object sender, EventArgs e)
			{
				FwLinkArgs link = new FwAppArgs(_cache.ProjectId.Handle, LanguageExplorerConstants.AnalysesMachineName, ActiveWordform(_wordformRepos, _propertyTable));
				var additionalProps = link.LinkProperties;
				additionalProps.Add(new LinkProperty(LanguageExplorerConstants.SuspendLoadListUntilOnChangeFilter, link.ToolName));
				additionalProps.Add(new LinkProperty("LinkSetupInfo", "CorrectSpelling"));
				LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, link);
			}

			private void EditSpellingStatus_Clicked(object sender, EventArgs e)
			{
				// Without checking both the SpellingStatus and (virtual) FullConcordanceCount
				// fields for the ActiveWordform() result, it's too likely that the user
				// will get a puzzling "Target not found" message popping up.  See LT-8717.
				FwLinkArgs link = new FwAppArgs(_cache.ProjectId.Handle, LanguageExplorerConstants.BulkEditWordformsMachineName, Guid.Empty);
				var additionalProps = link.LinkProperties;
				additionalProps.Add(new LinkProperty(LanguageExplorerConstants.SuspendLoadListUntilOnChangeFilter, link.ToolName));
				additionalProps.Add(new LinkProperty("LinkSetupInfo", "ReviewUndecidedSpelling"));
				LinkHandler.PublishFollowLinkMessage(_majorFlexComponentParameters.FlexComponentParameters.Publisher, link);
			}

			private void AddTexts_Clicked(object sender, EventArgs e)
			{
				((InterlinearTextsRecordList)_propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList).AddTexts();
			}

			private Tuple<bool, bool> CanCmdInsertText => new Tuple<bool, bool>(true, ActiveTool.MachineName == LanguageExplorerConstants.InterlinearEditMachineName);

			private void CmdInsertText_Click(object sender, EventArgs e)
			{
				((InterlinearTextsRecordList)_propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList).OnInsertInterlinText();
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
				return wordformRepos.TryGetObject(word, out var wordform) ? wordform.Guid : Guid.Empty;
			}

			private Tuple<bool, bool> CanCmdChangeSpelling => new Tuple<bool, bool>(true, string.IsNullOrWhiteSpace(ActiveWord?.Text));

			private void Respeller_Click(object sender, EventArgs e)
			{
				if (!InFriendliestTool)
				{
					// See LT-8641.
					using (new ListUpdateHelper(new ListUpdateHelperParameterObject { MyRecordList = _propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList }))
					{
						// Launch the Respeller Dlg.
						using (var dlg = new RespellerDlg())
						{
							dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
							if (dlg.SetDlgInfo(GetWordformForForm(_cache, ActiveWord)))
							{
								dlg.ShowDialog((Form)_propertyTable.GetValue<IFwMainWnd>(FwUtilsConstants.window));
							}
							else
							{
								MessageBox.Show(TextAndWordsResources.ksCannotRespellWordform);
							}
						}
					}
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
							dlg.ShowDialog((Form)_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IFwMainWnd>(FwUtilsConstants.window));
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

			/// <summary>
			/// Find an extant wordform,
			/// or create one (with nonundoable UOW), if one does not exist.
			/// </summary>
			private static IWfiWordform GetWordformForForm(LcmCache cache, ITsString form)
			{
				var servLoc = cache.ServiceLocator;
				var wordformRepos = servLoc.GetInstance<IWfiWordformRepository>();
				if (wordformRepos.TryGetObject(form, false, out var retval))
				{
					return retval;
				}
				// Have to make it.
				var wordformFactory = servLoc.GetInstance<IWfiWordformFactory>();
				NonUndoableUnitOfWorkHelper.Do(servLoc.GetInstance<IActionHandler>(), () =>
				{
					retval = wordformFactory.Create(form);
				});
				return retval;
			}

			private bool InFriendliestTool => _area.ActiveTool.MachineName == LanguageExplorerConstants.AnalysesMachineName;

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
						return (_propertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList?.CurrentObject as IWfiWordform)?.Form?.BestVernacularAlternative;
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
						var cache = _propertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
						var wsObj = cache.ServiceLocator.WritingSystemManager.Get(TsStringUtils.GetWsAtOffset(tssWord, 0));
						if (cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj))
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
					_sharedEventHandlers.Remove(Command.CmdRespeller);
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
