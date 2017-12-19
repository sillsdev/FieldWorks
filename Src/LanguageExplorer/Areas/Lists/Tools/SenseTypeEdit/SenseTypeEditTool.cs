// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition;
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

namespace LanguageExplorer.Areas.Lists.Tools.SenseTypeEdit
{
	/// <summary>
	/// ITool implementation for the "senseTypeEdit" tool in the "lists" area.
	/// </summary>
	[Export(AreaServices.ListsAreaMachineName, typeof(ITool))]
	internal sealed class SenseTypeEditTool : ITool
	{
		private ListsAreaMenuHelper _listsAreaMenuHelper;
		private const string SenseTypeList = "SenseTypeList";
		/// <summary>
		/// Main control to the right of the side bar control. This holds a RecordBar on the left and a PaneBarContainer on the right.
		/// The RecordBar has no top PaneBar for information, menus, etc.
		/// </summary>
		private CollapsingSplitContainer _collapsingSplitContainer;
		private IRecordList _recordList;
		[Import(AreaServices.ListsAreaMachineName)]
		private IArea _area;

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_listsAreaMenuHelper.Dispose();
			CollapsingSplitContainerFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _collapsingSplitContainer);
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
				_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(SenseTypeList, majorFlexComponentParameters.Statusbar, FactoryMethod);
			}
			_listsAreaMenuHelper = new ListsAreaMenuHelper(majorFlexComponentParameters, (IListArea)_area, _recordList);

#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var dataTree = new DataTree();
			_collapsingSplitContainer = CollapsingSplitContainerFactory.Create(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				true,
				XDocument.Parse(ListResources.SenseTypeEditParameters).Root, XDocument.Parse(ListResources.ListToolsSliceFilters),
				MachineName,
				majorFlexComponentParameters.LcmCache,
				_recordList,
				dataTree,
				MenuServices.GetFilePrintMenu(majorFlexComponentParameters.MenuStrip));
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		/// <remarks>
		/// One might expect this method to pass this call into the area's current tool.
		/// </remarks>
		public void FinishRefresh()
		{
			_recordList.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordList.VirtualListPublisher).Refresh();
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
		public string MachineName => AreaServices.SenseTypeEditMachineName;

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Sense Types";
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

		private static IRecordList FactoryMethod(LcmCache cache, FlexComponentParameters flexComponentParameters, string recordListId, StatusBar statusBar)
		{
			Require.That(recordListId == SenseTypeList, $"I don't know how to create a record list with an ID of '{recordListId}', as I can only create on with an id of '{SenseTypeList}'.");
			/*
            <clerk id="SenseTypeList">
              <recordList owner="LexDb" property="SenseTypes">
                <dynamicloaderinfo assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.PossibilityRecordList" />
              </recordList>
              <treeBarHandler assemblyPath="xWorks.dll" includeAbbr="false" ws="best analysis" class="SIL.FieldWorks.XWorks.PossibilityTreeBarHandler" />
              <filters />
              <sortMethods>
                <sortMethod label="Default" assemblyPath="Filters.dll" class="SIL.FieldWorks.Filters.PropertyRecordSorter" sortProperty="ShortName" />
              </sortMethods>
            </clerk>
			*/
			return new TreeBarHandlerAwarePossibilityRecordList(recordListId, statusBar,
				cache.ServiceLocator.GetInstance<ISilDataAccessManaged>(),
				cache.LanguageProject.LexDbOA.SenseTypesOA,
				new PossibilityTreeBarHandler(flexComponentParameters.PropertyTable, false, false, false, "best analysis"), new RecordFilterParameterObject(null, true, true));
		}
	}
}
