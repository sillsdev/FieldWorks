// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Works;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lists
{
	/// <summary>
	/// This class handles all interaction for the Lists Area common menus.
	/// </summary>
	internal sealed class ListsAreaMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private AreaWideMenuHelper _areaWideMenuHelper;
		private IListArea _listArea;
		private RecordClerk _recordClerk;
		private ToolStripMenuItem _editMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newEditMenusAndHandlers;
		private ToolStripMenuItem _deleteCustomListToolMenu;
		private ToolStripMenuItem _insertMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newInsertMenusAndHandlers;
		private ToolStripMenuItem _toolConfigureMenu;
		private ToolStripMenuItem _configureListMenu;

		internal ListsAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IListArea listArea, RecordClerk recordClerk)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(listArea, nameof(listArea));
			Guard.AgainstNull(recordClerk, nameof(recordClerk));

			_newEditMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			_newInsertMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			_majorFlexComponentParameters = majorFlexComponentParameters;
			_listArea = listArea;
			_recordClerk = recordClerk;
			_areaWideMenuHelper = new AreaWideMenuHelper(_majorFlexComponentParameters, recordClerk);
			// Set up File->Export menu, which is visible and enabled in all list area tools, using the default event handler.
			_areaWideMenuHelper.SetupFileExportMenu();

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);

			AddCommonEditMenus();
			// Add common Lists are Insert menus
			AddCommonInsertMenus();
			/*
			Insert toolbar
				<item command="CmdInsertSemDom" defaultVisible="false" />
				<item command="CmdDataTree-Insert-SemanticDomain" defaultVisible="false" label="Subdomain" />

				<item command="CmdInsertCustomItem" defaultVisible="false" />
				<item command="CmdDataTree-Insert-CustomItem" defaultVisible="false" label="Subitem" />

				<item command="CmdInsertPossibility" defaultVisible="false" />
				<item command="CmdDataTree-Insert-Possibility" defaultVisible="false" label="Subitem" />

				<item command="CmdInsertLexEntryInflType" defaultVisible="false" />
				<item command="CmdDataTree-Insert-LexEntryInflType" defaultVisible="false" label="Subtype" />

				<item command="CmdInsertLocation" defaultVisible="false" />
				<item command="CmdDataTree-Insert-Location" defaultVisible="false" label="Subitem" />

				<item command="CmdInsertLexEntryType" defaultVisible="false" />
				<item command="CmdDataTree-Insert-LexEntryType" defaultVisible="false" label="Subtype" />

				<item command="CmdInsertAnthroCategory" defaultVisible="false" />
				<item command="CmdDataTree-Insert-AnthroCategory" defaultVisible="false" label="Subcategory" />

				<item command="CmdInsertAnnotationDef" defaultVisible="false" />
				<item command="CmdInsertMorphType" defaultVisible="false" />
				<item command="CmdInsertPerson" defaultVisible="false" />
				<item command="CmdInsertLexRefType" defaultVisible="false" />
				<item command="CmdInsertFeatureType" defaultVisible="false" />
			*/
			/*
				Tools->Configure: <command id = "CmdConfigureList" label="List..." message="ConfigureList" />
			*/
			_toolConfigureMenu = MenuServices.GetToolsConfigureMenu(_majorFlexComponentParameters.MenuStrip);
			_configureListMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_toolConfigureMenu, ConfigureList_Click, ListResources.ConfigureList, ListResources.ConfigureListTooltip, Keys.None, null, 0);

			Application.Idle += Application_Idle;
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region IDisposable
		private bool _isDisposed;

		~ListsAreaMenuHelper()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
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
				return; // No need to do it more than once.
			}

			if (disposing)
			{
				Application.Idle -= Application_Idle;
				_areaWideMenuHelper.Dispose();
				foreach (var tuple in _newEditMenusAndHandlers)
				{
					_editMenu.DropDownItems.Remove(tuple.Item1);
					tuple.Item1.Click -= tuple.Item2;
					tuple.Item1.Dispose();
				}
				_newEditMenusAndHandlers.Clear();
				foreach (var tuple in _newInsertMenusAndHandlers)
				{
					_insertMenu.DropDownItems.Remove(tuple.Item1);
					tuple.Item1.Click -= tuple.Item2;
					tuple.Item1.Dispose();
				}
				_newInsertMenusAndHandlers.Clear();
				_toolConfigureMenu.DropDownItems.Remove(_configureListMenu);
				_configureListMenu.Click -= ConfigureList_Click;
				_configureListMenu.Dispose();
			}
			_majorFlexComponentParameters = null;
			_areaWideMenuHelper = null;
			_listArea = null;
			_recordClerk = null;
			_editMenu = null;
			_newEditMenusAndHandlers = null;
			_deleteCustomListToolMenu = null;
			_insertMenu = null;
			_newInsertMenusAndHandlers = null;
			_toolConfigureMenu = null;
			_configureListMenu = null;

			_isDisposed = true;
		}
		#endregion

		private void AddCommonEditMenus()
		{
			_editMenu = MenuServices.GetEditMenu(_majorFlexComponentParameters.MenuStrip);
			// End of Edit menu: <command id = "CmdDeleteCustomList" label="Delete Custom _List" message="DeleteCustomList" />
			_deleteCustomListToolMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newEditMenusAndHandlers, _editMenu, DeleteCustomList_Click, ListResources.DeleteCustomList);
		}

		private void AddCommonInsertMenus()
		{
			_insertMenu = MenuServices.GetInsertMenu(_majorFlexComponentParameters.MenuStrip);

			var insertIndex = 0;
/*
These all go on the "Insert" menu, but they are tool-specific. Start at 0.
      <item command="CmdInsertSemDom" defaultVisible="false" />
      <item command="CmdDataTree-Insert-SemanticDomain" defaultVisible="false" label="Subdomain" />

      <item command="CmdInsertCustomItem" defaultVisible="false" />
      <item command="CmdDataTree-Insert-CustomItem" defaultVisible="false" label="Subitem" />

      <item command="CmdInsertPossibility" defaultVisible="false" />
      <item command="CmdDataTree-Insert-Possibility" defaultVisible="false" label="Subitem" />

      <item command="CmdInsertLexEntryInflType" defaultVisible="false" />
      <item command="CmdDataTree-Insert-LexEntryInflType" defaultVisible="false" label="Subtype" />

      <item command="CmdInsertLocation" defaultVisible="false" />
      <item command="CmdDataTree-Insert-Location" defaultVisible="false" label="Subitem" />

      <item command="CmdInsertLexEntryType" defaultVisible="false" />
      <item command="CmdDataTree-Insert-LexEntryType" defaultVisible="false" label="Subtype" />

      <item command="CmdInsertAnthroCategory" defaultVisible="false" />
      <item command="CmdDataTree-Insert-AnthroCategory" defaultVisible="false" label="Subcategory" />

      <item command="CmdInsertAnnotationDef" defaultVisible="false" />
      <item command="CmdInsertMorphType" defaultVisible="false" />
      <item command="CmdInsertPerson" defaultVisible="false" />
      <item command="CmdInsertLexRefType" defaultVisible="false" />
      <item command="CmdInsertFeatureType" defaultVisible="false" />
*/
			// <item label="-" translate="do not translate" />
			ToolStripMenuItemFactory.CreateToolStripSeparatorForToolStripMenuItem(_insertMenu, insertIndex++);
			// <item command="CmdAddCustomList" defaultVisible="false" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newInsertMenusAndHandlers, _insertMenu, AddCustomList_Click, ListResources.AddCustomList, ListResources.AddCustomListTooltip, Keys.None, null, insertIndex);
		}

		private void AddCustomList_Click(object sender, EventArgs e)
		{
			using (var dlg = new AddCustomListDlg(PropertyTable, Publisher, _majorFlexComponentParameters.LcmCache))
			{
				if (dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow) == DialogResult.OK)
				{
					_listArea.AddCustomList(dlg.NewList);
				}
			}
		}

		private void DeleteCustomList_Click(object sender, EventArgs e)
		{
			UndoableUnitOfWorkHelper.Do(xWorksStrings.ksUndoDeleteCustomList, xWorksStrings.ksRedoDeleteCustomList, _majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () => new DeleteCustomList(_majorFlexComponentParameters.LcmCache).Run((ICmPossibilityList)_recordClerk.OwningObject));
			_listArea.RemoveCustomListTool(_listArea.ActiveTool);
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			var inDeletingTerritory = false;
			var clerkOwningObject = _recordClerk.OwningObject as ICmPossibilityList;
			if (clerkOwningObject != null && clerkOwningObject.Owner == null)
			{
				inDeletingTerritory = true;
			}
			// Only see and use the delete button for the currently selected tool
			_deleteCustomListToolMenu.Visible = _deleteCustomListToolMenu.Enabled = inDeletingTerritory;
		}

		private void ConfigureList_Click(object sender, EventArgs e)
		{
			var list = (ICmPossibilityList)_recordClerk.OwningObject;
			var originalUiName = list.Name.BestAnalysisAlternative.Text;
			using (var dlg = new ConfigureListDlg(PropertyTable, Publisher, _majorFlexComponentParameters.LcmCache, list))
			{
				if (dlg.ShowDialog((Form) _majorFlexComponentParameters.MainWindow) == DialogResult.OK && originalUiName != list.Name.BestAnalysisAlternative.Text)
				{
					_listArea.ModifiedCustomList(_listArea.ActiveTool);
				}
			}
		}
	}
}
