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
using LanguageExplorer.Areas.Lists.Tools.CustomListEdit;
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
		private string PropertyNameForToolName => $"{AreaServices.ToolForAreaNamed_}{MachineName}";
		[Import]
		private IPropertyTable _propertyTable;
		private SortedDictionary<string, ITool> _sortedDictionaryOfAllTools;
		private event EventHandler ListAreaToolsChanged;
		private ListAreaMenuHelper _listAreaMenuHelper;

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
			_listAreaMenuHelper.Dispose();
			_listAreaMenuHelper = null;
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
			_listAreaMenuHelper = new ListAreaMenuHelper(majorFlexComponentParameters, this);
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

			var toolToPersist = ActiveTool ?? _sortedDictionaryOfAllTools.Values.First(tool => tool.MachineName == AreaServices.ListsAreaDefaultToolMachineName);
			toolToPersist.EnsurePropertiesAreCurrent();
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => AreaServices.ListsAreaMachineName;

		/// <summary>
		/// User-visible localized component name.
		/// </summary>
		public string UiName => StringTable.Table.LocalizeLiteralValue(AreaServices.ListsAreaUiName);

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
				var propertyNameForToolName = PropertyNameForToolName;
				var propertyValue = _propertyTable.GetValue(propertyNameForToolName, AreaServices.ListsAreaDefaultToolMachineName);
				var retVal = _sortedDictionaryOfAllTools.Values.FirstOrDefault(tool => tool.MachineName == propertyValue);
				if (retVal != null)
				{
					return retVal;
				}
				// How can this be? I've seen a case where the Guid in the name is not in any custom tool.
				propertyValue = AreaServices.ListsAreaDefaultToolMachineName;
				return _sortedDictionaryOfAllTools.Values.First(tool => tool.MachineName == propertyValue);
			}
		}

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IReadOnlyDictionary<string, ITool> AllToolsInOrder
		{
			get
			{
				if (_sortedDictionaryOfAllTools == null)
				{
					var cache = _propertyTable.GetValue<LcmCache>(FwUtils.cache);
					var coreWritingSystemDefinition = cache.ServiceLocator.WritingSystemManager.Get(cache.DefaultAnalWs);
					_sortedDictionaryOfAllTools = new SortedDictionary<string, ITool>(coreWritingSystemDefinition.DefaultCollation.Collator);
					foreach (var builtinTool in _myBuiltinTools)
					{
						// This will include any user-defined tools that were imported via MEF.
						// Since all list area tools are sorted, the user defined ones need not be appended to the end,
						// as is done for all other areas.
						_sortedDictionaryOfAllTools.Add(builtinTool.UiName, builtinTool);
					}
					foreach (var customList in cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().AllInstances().Where(list => list.Owner == null))
					{
						var customTool = new CustomListEditTool(this, customList);
						_sortedDictionaryOfAllTools.Add(customTool.UiName, customTool);
					}
				}
				return _sortedDictionaryOfAllTools;
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

		/// <inheritdoc />
		event EventHandler IListArea.ListAreaToolsChanged
		{
			add { ListAreaToolsChanged += value; }
			remove { ListAreaToolsChanged -= value; }
		}

		/// <summary>
		/// Add a new custom list to the area, and to the Tab.
		/// </summary>
		void IListArea.OnAddCustomList(ICmPossibilityList customList)
		{
			AddCustomTool(customList);
			ListAreaToolsChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Remove a custom list's tool from the area and from the Tab
		/// </summary>
		void IListArea.OnRemoveCustomListTool(ITool gonerTool)
		{
			RemoveCustomTool(gonerTool);
			ListAreaToolsChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Change the display name of the list in the Tab.
		/// </summary>
		void IListArea.OnUpdateListDisplayName(ITool gonerTool, ICmPossibilityList customList)
		{
			RemoveCustomTool(gonerTool);
			AddCustomTool(customList);
			ListAreaToolsChanged?.Invoke(this, EventArgs.Empty);
		}

		#endregion

		private void AddCustomTool(ICmPossibilityList customList)
		{
			Guard.AgainstNull(customList, nameof(customList));

			var customTool = new CustomListEditTool(this, customList);
			_sortedDictionaryOfAllTools.Add(StringTable.Table.LocalizeLiteralValue(customTool.UiName), customTool);
		}

		private void RemoveCustomTool(ITool gonerTool)
		{
			Guard.AgainstNull(gonerTool, nameof(gonerTool));

			var toolKvp = _sortedDictionaryOfAllTools.First(kvp => kvp.Value == gonerTool);
			_sortedDictionaryOfAllTools.Remove(toolKvp.Key);
		}

		/// <summary>
		/// Handle creation and use of List area menus.
		/// </summary>
		private sealed class ListAreaMenuHelper : IDisposable
		{
			private IListArea _area;
			private MajorFlexComponentParameters _majorFlexComponentParameters;

			internal ListAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IListArea area)
			{
				Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
				Guard.AgainstNull(area, nameof(area));

				_majorFlexComponentParameters = majorFlexComponentParameters;
				_area = area;
				SetupAreaUiWidgets();
			}

			private void SetupAreaUiWidgets()
			{
				var areaUiWidgetParameterObject = new AreaUiWidgetParameterObject(_area);
				areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.Insert].Add(Command.CmdAddCustomList, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(AddCustomList_Click, () => UiWidgetServices.CanSeeAndDo));
				areaUiWidgetParameterObject.MenuItemsForArea[MainMenu.Tools].Add(Command.CmdConfigureList, new Tuple<EventHandler, Func<Tuple<bool, bool>>>(ConfigureList_Click, () => CanCmdConfigureList));
				_majorFlexComponentParameters.UiWidgetController.AddHandlers(areaUiWidgetParameterObject);
			}

			private void AddCustomList_Click(object sender, EventArgs e)
			{
				using (var dlg = new AddCustomListDlg(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher, _majorFlexComponentParameters.LcmCache))
				{
					if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK)
					{
						_area.OnAddCustomList(dlg.NewList);
					}
				}
			}

			private Tuple<bool, bool> CanCmdConfigureList => new Tuple<bool, bool>(true, ((IListTool)_area.ActiveTool).MyList != null);

			private void ConfigureList_Click(object sender, EventArgs e)
			{
				var list = ((IListTool)_area.ActiveTool).MyList;
				var originalUiName = list.Name.BestAnalysisAlternative.Text;
				using (var dlg = new ConfigureListDlg(_majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher, _majorFlexComponentParameters.LcmCache, list))
				{
					if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK && originalUiName != list.Name.BestAnalysisAlternative.Text)
					{
						_area.OnUpdateListDisplayName(_area.ActiveTool, list);
					}
				}
			}

			#region IDisposable
			private bool _isDisposed;

			~ListAreaMenuHelper()
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
				}
				_area = null;
				_majorFlexComponentParameters = null;

				_isDisposed = true;
			}
			#endregion
		}
	}
}