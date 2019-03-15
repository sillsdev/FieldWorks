// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.Lexicon.Tools.Edit;
using LanguageExplorer.Filters;
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
		private const string MyUiName = "Lexical Tools";
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";
		internal const string Entries = "entries";
		internal const string AllReversalEntries = "AllReversalEntries";
		internal const string SemanticDomainList_LexiconArea = "SemanticDomainList_LexiconArea";
		private const string khomographconfiguration = "HomographConfiguration";
		private bool _hasBeenActivated;
		[Import]
		private IPropertyTable _propertyTable;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.LexiconAreaMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => MyUiName;

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
				if (_propertyTable.TryGetValue(khomographconfiguration, out hcSettings))
				{
					var serviceLocator = majorFlexComponentParameters.LcmCache.ServiceLocator;
					var hc = serviceLocator.GetInstance<HomographConfiguration>();
					hc.PersistData = hcSettings;
					_propertyTable.SetDefault("SelectedPublication", "Main Dictionary", true, true);
				}
				_hasBeenActivated = true;
			}
			_propertyTable.SetDefault(LexiconEditToolConstants.Show_DictionaryPubPreview, true, true);
			_propertyTable.SetDefault("Show_reversalIndexEntryList", true);
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
			var serviceLocator = _propertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache).ServiceLocator;
			var hc = serviceLocator.GetInstance<HomographConfiguration>();
			_propertyTable.SetProperty(khomographconfiguration, hc.PersistData, true);
			PersistedOrDefaultTool.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool PersistedOrDefaultTool => _myTools.First(tool => tool.MachineName == _propertyTable.GetValue(PropertyNameForToolName, AreaServices.LexiconAreaDefaultToolMachineName));

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IReadOnlyList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
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
				return myToolsInOrder.Select(toolName => _myTools.First(tool => tool.MachineName == toolName)).ToList();
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
			Require.That(recordListId == Entries, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{Entries}'.");
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
			Require.That(recordListId == SemanticDomainList_LexiconArea, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{SemanticDomainList_LexiconArea}'.");
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
	}
}
