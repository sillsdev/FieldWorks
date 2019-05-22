// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using LanguageExplorer.Areas.Lists.Tools.CustomListEdit;
using LanguageExplorer.Controls.SilSidePane;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lists
{
	[Export(AreaServices.ListsAreaMachineName, typeof(IArea))]
	[Export(typeof(IArea))]
	internal sealed class ListsArea : IListArea
	{
		[ImportMany(AreaServices.ListsAreaMachineName)]
		private IEnumerable<ITool> _myBuiltinTools;
		private const string MyUiName = "Lists";
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";
		[Import]
		private IPropertyTable _propertyTable;
		private readonly SortedList<string, ITool> _sortedListOfCustomTools = new SortedList<string, ITool>();
		private SidePane _sidePane;
		private HashSet<ITool> _allTools;

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
			_sidePane = null;
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
			_propertyTable.SetDefault(PropertyNameForToolName, AreaServices.ListsAreaDefaultToolMachineName, true);
			_sidePane = majorFlexComponentParameters.SidePane;
			// Do nothing registration, but required, before a list tool can be registered.
			majorFlexComponentParameters.UiWidgetController.AddHandlers(new AreaUiWidgetParameterObject(this));
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
		public string MachineName => AreaServices.ListsAreaMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => MyUiName;
		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool PersistedOrDefaultTool
		{
			get
			{
				var persistedToolName = _propertyTable.GetValue(PropertyNameForToolName, AreaServices.ListsAreaDefaultToolMachineName);
				return _allTools.First(tool => tool.MachineName == persistedToolName);
			}
		}

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IReadOnlyList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
				{
					AreaServices.DomainTypeEditMachineName,
					AreaServices.AnthroEditMachineName,
					AreaServices.ComplexEntryTypeEditMachineName,
					AreaServices.ConfidenceEditMachineName,
					AreaServices.DialectsListEditMachineName,
					AreaServices.EducationEditMachineName,
					AreaServices.ExtNoteTypeEditMachineName,
					AreaServices.FeatureTypesAdvancedEditMachineName,
					AreaServices.GenresEditMachineName,
					AreaServices.LanguagesListEditMachineName,
					AreaServices.LexRefEditMachineName,
					AreaServices.LocationsEditMachineName,
					AreaServices.MorphTypeEditMachineName,
					AreaServices.RecTypeEditMachineName,
					AreaServices.PeopleEditMachineName,
					AreaServices.PositionsEditMachineName,
					AreaServices.PublicationsEditMachineName,
					AreaServices.RestrictionsEditMachineName,
					AreaServices.ReversalToolReversalIndexPOSMachineName,
					AreaServices.RoleEditMachineName,
					AreaServices.SemanticDomainEditMachineName,
					AreaServices.SenseTypeEditMachineName,
					AreaServices.StatusEditMachineName,
					AreaServices.ChartmarkEditMachineName,
					AreaServices.CharttempEditMachineName,
					AreaServices.TextMarkupTagsEditMachineName,
					AreaServices.TimeOfDayEditMachineName,
					AreaServices.TranslationTypeEditMachineName,
					AreaServices.UsageTypeEditMachineName,
					AreaServices.VariantEntryTypeEditMachineName
				};
				var retval = myToolsInOrder.Select(toolName => _myBuiltinTools.First(tool => tool.MachineName == toolName)).ToList();
				_allTools = new HashSet<ITool>(_myBuiltinTools);
				// Load tools for custom lists.
				var cache = _propertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
				var customLists = cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().AllInstances().Where(list => list.Owner == null).ToList();
				if (!_sortedListOfCustomTools.Any())
				{
					foreach (var customList in customLists)
					{
						var customTool = new CustomListEditTool(this, customList);
						_sortedListOfCustomTools.Add(customTool.MachineName, customTool);
					}
					_allTools.UnionWith(_sortedListOfCustomTools.Values);
				}
				retval.AddRange(_sortedListOfCustomTools.Values);
				return retval;
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => LanguageExplorerResources.Lists.ToBitmap();

		/// <summary>
		/// Set the active tool for the area, or null, if no tool is active.
		/// </summary>
		public ITool ActiveTool { get; set; }

		#endregion

		#region Implementation of IListArea

		/// <summary>
		/// Set the list area sidebar tab, so it can be updated as custom lists gets added/removed, or get names changed.
		/// </summary>
		public Tab ListAreaTab { get; set; }

		/// <summary>
		/// Add a new custom list to the area, and to the Tab.
		/// </summary>
		public void AddCustomList(ICmPossibilityList newList)
		{
			Guard.AgainstNull(newList, nameof(newList));

			// Theory has it that the client ensures the name is unique, so we don't have to worry about that here.
			// Create new tool and add it to sorted list.
			var customTool = new CustomListEditTool(this, newList);
			_sortedListOfCustomTools.Add(customTool.MachineName, customTool);
			_allTools.Add(customTool);
			// Add it to the sidebar.
			var item = new Item(StringTable.Table.LocalizeLiteralValue(customTool.UiName))
			{
				Icon = customTool.Icon,
				Tag = customTool,
				Name = customTool.MachineName
			};
			_sidePane.SuspendLayout();
			_sidePane.AddItem(ListAreaTab, item);
			_sidePane.ResumeLayout();
		}

		/// <summary>
		/// Remove a custom list's tool from the area and from the Tab
		/// </summary>
		public void RemoveCustomListTool(ITool gonerTool)
		{
			_sortedListOfCustomTools.Remove(gonerTool.MachineName);
			_allTools.Remove(gonerTool);
			_sidePane.SuspendLayout();
			_sidePane.RemoveItem(ListAreaTab, gonerTool);
			_sidePane.ResumeLayout();
		}

		/// <summary>
		/// Change the display name of the list in the Tab.
		/// </summary>
		public void UpdateListDisplayName(ITool tool)
		{
			_sidePane.SuspendLayout();
			var isCustomList = false;
			foreach (var kvp in _sortedListOfCustomTools)
			{
				if (tool != kvp.Value)
				{
					continue;
				}
				isCustomList = true;
				_sortedListOfCustomTools.Remove(kvp.Key);
				break;
			}
			if (isCustomList)
			{
				_sortedListOfCustomTools.Add(tool.MachineName, tool);
			}
			_sidePane.RenameItem(ListAreaTab, tool, tool.UiName);
			_sidePane.ResumeLayout();
		}

		#endregion
	}
}