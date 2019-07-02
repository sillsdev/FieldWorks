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
using SIL.LCModel;

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
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";
		private TextAndWordsAreaMenuHelper _textAndWordsAreaMenuHelper;
		private bool _hasBeenActivated;
		[Import]
		private IPropertyTable _propertyTable;
		private Dictionary<string, ITool> _dictionaryOfAllTools;

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

				_propertyTable.SetDefault(InterlinDocForAnalysis.ITexts_AddWordsToLexicon, false, true);
				_propertyTable.SetDefault("ITexts_ShowAddWordsToLexiconDlg", true, true);
				_propertyTable.SetDefault("ITexts-ScriptureIds", string.Empty, true);
				_hasBeenActivated = true;
			}
			var areaUiWidgetParameterObject = new AreaUiWidgetParameterObject(this);
			_textAndWordsAreaMenuHelper = new TextAndWordsAreaMenuHelper(majorFlexComponentParameters);
			_textAndWordsAreaMenuHelper.InitializeAreaWideMenus(areaUiWidgetParameterObject);
			majorFlexComponentParameters.UiWidgetController.AddHandlers(areaUiWidgetParameterObject);
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
		public ITool ActiveTool { get; set; }

		#endregion

		internal static IRecordList ConcordanceWordsFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == ConcordanceWords, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{ConcordanceWords}'.");
			/*
            <clerk id="concordanceWords">
              <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.InterlinearTextsRecordClerk" />
              <recordList owner="LangProject" property="Wordforms">
                <dynamicloaderinfo assemblyPath="ITextDll.dll" class="SIL.FieldWorks.IText.ConcordanceWordList" />
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
			return new ConcordanceWordList(statusBar, cache.LanguageProject, concDecorator);
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

			internal TextAndWordsAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

				_majorFlexComponentParameters = majorFlexComponentParameters;
			}

			internal void InitializeAreaWideMenus(AreaUiWidgetParameterObject areaUiWidgetParameterObject)
			{
				_customFieldsMenuHelper = new CustomFieldsMenuHelper(_majorFlexComponentParameters, areaUiWidgetParameterObject.Area);
				_customFieldsMenuHelper.SetupToolsCustomFieldsMenu(areaUiWidgetParameterObject);
				/*
					<item label="Click Inserts Invisible Space" boolProperty="ClickInvisibleSpace" defaultVisible="false" settingsGroup="local" icon="zeroWidth"/> // Only Insert menu
				*/
				var insertMenuDictionary = areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.Insert];
				insertMenuDictionary.Add(Command.CmdInsertText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertText_Click, () => CanCmdInsertText));
				insertMenuDictionary.Add(Command.CmdImportWordSet, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportWordSetToolStripMenuItemOnClick, () => CanCmdImportWordSet));
				insertMenuDictionary.Add(Command.CmdInsertHumanApprovedAnalysis, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertHumanApprovedAnalysis_Click, () => CanCmdInsertHumanApprovedAnalysis));
				var insertToolBarDictionary = areaUiWidgetParameterObject.ToolBarItemsForArea[ToolBar.Insert];
				insertToolBarDictionary.Add(Command.CmdInsertText, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(CmdInsertText_Click, () => CanCmdInsertText));
				insertToolBarDictionary.Add(Command.CmdInsertHumanApprovedAnalysis, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(InsertHumanApprovedAnalysis_Click, () => CanCmdInsertHumanApprovedAnalysis));
			}

			private Tuple<bool, bool> CanCmdInsertText => new Tuple<bool, bool>(true, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList is InterlinearTextsRecordList);

			private void CmdInsertText_Click(object sender, EventArgs e)
			{
				var recordList = (InterlinearTextsRecordList)_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList;
				recordList.OnInsertInterlinText();
			}

#if RANDYTODO
		// TODO: Make the event handler work and be enabled.
#endif
			private Tuple<bool, bool> CanCmdInsertHumanApprovedAnalysis => new Tuple<bool, bool>(true, false);

			private void InsertHumanApprovedAnalysis_Click(object sender, EventArgs e)
			{
				MessageBox.Show(@"TODO: Adding new human approved analysis here.");
			}

			private Tuple<bool, bool> CanCmdImportWordSet => new Tuple<bool, bool>(true, true);

			private void ImportWordSetToolStripMenuItemOnClick(object sender, EventArgs eventArgs)
			{
				using (var dlg = new ImportWordSetDlg(_majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList, _majorFlexComponentParameters.ParserMenuManager))
				{
					dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
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
					_customFieldsMenuHelper.Dispose();
				}
				_majorFlexComponentParameters = null;
				_customFieldsMenuHelper = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
