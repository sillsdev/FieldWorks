// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lists.Tools.CustomListEdit
{
	internal sealed class CustomListEditTool : ITool
	{
		private ListsAreaMenuHelper _listsAreaMenuHelper;
		private readonly IListArea _area;
		private readonly ICmPossibilityList _customList;

		/// <summary>
		/// Main control to the right of the side bar control. This holds a RecordBar on the left and a PaneBarContainer on the right.
		/// The RecordBar has no top PaneBar for information, menus, etc.
		/// </summary>
		private CollapsingSplitContainer _collapsingSplitContainer;
		private IRecordList _recordList;

		internal CustomListEditTool(IListArea area, ICmPossibilityList customList)
		{
			Guard.AgainstNull(area, nameof(area));
			Guard.AgainstNull(customList, nameof(customList));

			_area = area;
			_customList = customList;
		}

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to another component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			CollapsingSplitContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _collapsingSplitContainer);

			// Dispose after the main UI stuff.
			_listsAreaMenuHelper.Dispose();

			_listsAreaMenuHelper = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			if (_recordList == null)
			{
				_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(MachineName, majorFlexComponentParameters.StatusBar, _customList, FactoryMethod);
			}

#if RANDYTODO
			// TODO: See if custom lists really use ListResources.PositionsEditParameters.
#endif
			var dataTree = new DataTree(majorFlexComponentParameters.SharedEventHandlers);
			_listsAreaMenuHelper = new ListsAreaMenuHelper(majorFlexComponentParameters, dataTree, (IListArea)_area, _recordList);
			_collapsingSplitContainer = CollapsingSplitContainerFactory.Create(majorFlexComponentParameters.FlexComponentParameters, majorFlexComponentParameters.MainCollapsingSplitContainer,
				true, XDocument.Parse(ListResources.PositionsEditParameters).Root, XDocument.Parse(ListResources.ListToolsSliceFilters), MachineName,
				majorFlexComponentParameters.LcmCache, _recordList, dataTree, MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));

			// Too early before now.
			_listsAreaMenuHelper.Initialize();
			if (majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue(PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), false, SettingsGroup.LocalSettings))
			{
				majorFlexComponentParameters.FlexComponentParameters.Publisher.Publish("ShowHiddenFields", true);
			}
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName => GetMachineName(_customList);

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => GetUIName(_customList);

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area for the tool.
		/// </summary>
		public IArea Area => _area;

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion

		private static string GetMachineName(ICmPossibilityList customList)
		{
			return $"CustomList_{customList.Guid}_Edit";
		}

		private static string GetUIName(ICmPossibilityList customList)
		{
			return customList.Name.BestAnalysisAlternative.Text;
		}

		private static IRecordList FactoryMethod(ICmPossibilityList customList, LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			var customListRecordListName = GetMachineName(customList);
			Require.That(recordListId == customListRecordListName, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{customListRecordListName}'.");

			return new TreeBarHandlerAwarePossibilityRecordList(recordListId, statusBar, cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(),
				customList, new PossibilityTreeBarHandler(flexComponentParameters.PropertyTable, false, customList.Depth > 1, customList.DisplayOption == (int)PossNameType.kpntName, customList.GetWsString()));
		}
	}
}