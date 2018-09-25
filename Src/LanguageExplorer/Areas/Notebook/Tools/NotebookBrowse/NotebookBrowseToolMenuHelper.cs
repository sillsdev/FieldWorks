// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookBrowse
{
	/// <summary>
	/// This class handles all interaction for the NotebookBrowseTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class NotebookBrowseToolMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private NotebookAreaMenuHelper _notebookAreaMenuHelper;
		internal BrowseViewContextMenuFactory MyBrowseViewContextMenuFactory { get; private set; }
		private ISharedEventHandlers _sharedEventHandlers;

		internal NotebookBrowseToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool currentNotebookTool, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(majorFlexComponentParameters, currentNotebookTool);
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			MyBrowseViewContextMenuFactory = new BrowseViewContextMenuFactory();
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
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			_notebookAreaMenuHelper.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
			MyBrowseViewContextMenuFactory.RegisterBrowseViewContextMenuCreatorMethod(AreaServices.mnuBrowseView, BrowseViewContextMenuCreatorMethod);
		}
		#endregion

		#region Implementation of IDisposable
		private bool _isDisposed;

		~NotebookBrowseToolMenuHelper()
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
				_notebookAreaMenuHelper?.Dispose();
				MyBrowseViewContextMenuFactory?.Dispose();
			}
			MyBrowseViewContextMenuFactory = null;
			_majorFlexComponentParameters = null;
			_notebookAreaMenuHelper = null;

			_isDisposed = true;
		}
		#endregion

		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> BrowseViewContextMenuCreatorMethod(IRecordList recordList, string browseViewMenuId)
		{
			// The actual menu declaration has a gazillion menu items, but only two of them are seen in this tool (plus the separator).
			// Start: <menu id="mnuBrowseView" (partial) >
			var contextMenuStrip = new ContextMenuStrip
			{
				Name = AreaServices.mnuBrowseView
			};
			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);

			// <command id="CmdDeleteSelectedObject" label="Delete selected {0}" message="DeleteSelectedItem"/>
			var menuText = string.Format(AreaResources.Delete_selected_0, StringTable.Table.GetString("RnGenericRec", "ClassNames"));
			var menu = ToolStripMenuItemFactory.CreateToolStripMenuItemForContextMenuStrip(menuItems, contextMenuStrip, _sharedEventHandlers.Get(AreaServices.DeleteSelectedBrowseViewObject), menuText);
			var tagList = new List<object> { recordList, menuText, StatusBarPanelServices.GetStatusBarProgressPanel(_majorFlexComponentParameters.StatusBar) };
			menu.Tag = tagList;

			// End: <menu id="mnuBrowseView" (partial) >

			return new Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, menuItems);
		}
	}
}