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
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Filters;
using LanguageExplorer.LIFT;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainImpl;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// IArea implementation for the main, and thus only required, Area: "lexicon".
	/// </summary>
	[Export(AreaServices.LexiconAreaMachineName, typeof(IArea))]
	[Export(typeof(IArea))]
	internal sealed class LexiconArea : IArea
	{
		[ImportMany(AreaServices.LexiconAreaMachineName)]
		private IEnumerable<ITool> _myTools;
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";
		internal const string SemanticDomainList_LexiconArea = "SemanticDomainList_LexiconArea";
		private bool _hasBeenActivated;
		[Import]
		private IPropertyTable _propertyTable;
		private LexiconAreaMenuHelper _lexiconAreaMenuHelper;
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
			_lexiconAreaMenuHelper.Dispose();
			var activeTool = ActiveTool;
			ActiveTool = null;
			activeTool?.Deactivate(majorFlexComponentParameters);

			_lexiconAreaMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_propertyTable.SetDefault(PropertyNameForToolName, AreaServices.LexiconAreaDefaultToolMachineName, true);
			if (!_hasBeenActivated)
			{
				// Restore HomographConfiguration settings.
				string hcSettings;
				if (_propertyTable.TryGetValue(LanguageExplorerConstants.HomographConfiguration, out hcSettings))
				{
					var serviceLocator = majorFlexComponentParameters.LcmCache.ServiceLocator;
					var hc = serviceLocator.GetInstance<HomographConfiguration>();
					hc.PersistData = hcSettings;
					_propertyTable.SetDefault(LanguageExplorerConstants.SelectedPublication, "Main Dictionary", true, true);
				}
				_hasBeenActivated = true;
			}
			_propertyTable.SetDefault("Show_reversalIndexEntryList", true);
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper(majorFlexComponentParameters, this);
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
			var serviceLocator = _propertyTable.GetValue<LcmCache>(FwUtils.cache).ServiceLocator;
			var hc = serviceLocator.GetInstance<HomographConfiguration>();
			_propertyTable.SetProperty(LanguageExplorerConstants.HomographConfiguration, hc.PersistData, true);
			PersistedOrDefaultTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.LexiconAreaMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.LexiconAreaUiName);

		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		/// <remarks>Note to self: Use 'true', if i want to force it to pick the lex edit tool, because the current persisted one crashes.</remarks>
		public ITool PersistedOrDefaultTool => _dictionaryOfAllTools.Values.First(tool => tool.MachineName == (false ? "lexiconEdit" : _propertyTable.GetValue(PropertyNameForToolName, AreaServices.LexiconAreaDefaultToolMachineName)));

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
						AreaServices.LexiconEditMachineName,
						AreaServices.LexiconBrowseMachineName,
						AreaServices.LexiconDictionaryMachineName,
						AreaServices.RapidDataEntryMachineName,
						AreaServices.LexiconClassifiedDictionaryMachineName,
						AreaServices.BulkEditEntriesOrSensesMachineName,
						AreaServices.ReversalEditCompleteMachineName,
						AreaServices.ReversalBulkEditReversalEntriesMachineName
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
		public Image Icon => LanguageExplorerResources.Lexicon32.ToBitmap();

		/// <summary>
		/// Set the active tool for the area, or null, if no tool is active.
		/// </summary>
		public ITool ActiveTool { get; set; }

		#endregion

		internal static IRecordList EntriesFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == LanguageExplorerConstants.Entries, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{LanguageExplorerConstants.Entries}'.");
			/*
			<clerk id="entries">
				<recordList owner="LexDb" property="Entries" />
				<filters />
				<sortMethods>
				<sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
				<sortMethod label="Primary Gloss" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="PrimaryGloss" />
				</sortMethods>
			</clerk>
			*/
			var recordList = new RecordList(recordListId, statusBar,
				cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), false,
				new VectorPropertyParameterObject(cache.LanguageProject.LexDbOA, "Entries", cache.MetaDataCacheAccessor.GetFieldId2(cache.LanguageProject.LexDbOA.ClassID, "Entries", false)),
				new Dictionary<string, PropertyRecordSorter>
				{
					{ AreaServices.Default, new PropertyRecordSorter(AreaServices.ShortName) },
					{ "PrimaryGloss", new PropertyRecordSorter("PrimaryGloss") }
				});
			return recordList;
		}

		internal static IRecordList SemanticDomainList_LexiconAreaFactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == SemanticDomainList_LexiconArea, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create one with an id of '{SemanticDomainList_LexiconArea}'.");
			/*
            <clerk id="SemanticDomainList">
              <recordList owner="LangProject" property="SemanticDomainList">
                <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.PossibilityRecordList" />
              </recordList>
              <treeBarHandler assemblyPath="xWorks.dll" expand="false" hierarchical="true" includeAbbr="true" ws="best analorvern" class="SIL.FieldWorks.XWorks.SemanticDomainRdeTreeBarHandler" altTitleId="SemanticDomain-Plural" />
              <filters />
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
            </clerk>
			*/
			return new TreeBarHandlerAwarePossibilityRecordList(recordListId, statusBar,
				new DictionaryPublicationDecorator(cache, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(), CmPossibilityListTags.kflidPossibilities), cache.LanguageProject.SemanticDomainListOA,
				new SemanticDomainRdeTreeBarHandler(flexComponentParameters.PropertyTable), new RecordFilterParameterObject(allowDeletions: false));
		}

		/// <summary>
		/// This class handles all interaction for the Lexicon Area common menus.
		/// </summary>
		private sealed class LexiconAreaMenuHelper : IDisposable
		{
			private MajorFlexComponentParameters _majorFlexComponentParameters;
			private IArea _area;
			private CustomFieldsMenuHelper _customFieldsMenuHelper;
			private FileExportMenuHelper _fileExportMenuHelper;

			internal LexiconAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IArea area)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(area, nameof(area));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_area = area;

				var areaUiWidgetParameterObject = new AreaUiWidgetParameterObject(_area);
				_customFieldsMenuHelper = new CustomFieldsMenuHelper(_majorFlexComponentParameters, _area, areaUiWidgetParameterObject);
				_fileExportMenuHelper = new FileExportMenuHelper(majorFlexComponentParameters);
				// Set up File->Export menu, which is visible and enabled in all lexicon area tools, using the default event handler.
				_fileExportMenuHelper.SetupFileExportMenu(areaUiWidgetParameterObject);
				var fileMenuItemsForTool = areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.File];
				// Add two lexicon area-wide import options.
				// <item command="CmdImportLinguaLinksData" />
				fileMenuItemsForTool.Add(Command.CmdImportLinguaLinksData, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportLinguaLinksData_Clicked, () => UiWidgetServices.CanSeeAndDo));
				// <item command="CmdImportLiftData" />
				fileMenuItemsForTool.Add(Command.CmdImportLiftData, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ImportLiftData_Clicked, () => UiWidgetServices.CanSeeAndDo));
				majorFlexComponentParameters.UiWidgetController.AddHandlers(areaUiWidgetParameterObject);
			}

			private void ImportLinguaLinksData_Clicked(object sender, EventArgs e)
			{
				using (var importWizardDlg = new LinguaLinksImportDlg())
				{
					AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
				}
			}

			private void ImportLiftData_Clicked(object sender, EventArgs e)
			{
				using (var importWizardDlg = new LiftImportDlg())
				{
					AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
				}
			}

			#region IDisposable
			private bool _isDisposed;

			~LexiconAreaMenuHelper()
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
					_fileExportMenuHelper.Dispose();
				}
				_customFieldsMenuHelper = null;
				_fileExportMenuHelper = null;
				_majorFlexComponentParameters = null;
				_area = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}
