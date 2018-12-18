// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
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
		private ISharedEventHandlers _sharedEventHandlers;
		private NotebookAreaMenuHelper _notebookAreaMenuHelper;
		private RecordBrowseView _browseView;
		private ToolStripButton _insertRecordToolStripButton;
		private ToolStripButton _insertFindRecordToolStripButton;

		internal NotebookBrowseToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool currentNotebookTool, IRecordList recordList, RecordBrowseView browseView)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(currentNotebookTool, nameof(currentNotebookTool));
			Guard.AgainstNull(browseView, nameof(browseView));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(majorFlexComponentParameters, currentNotebookTool, recordList);
			_browseView = browseView;
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
			_notebookAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsConfigureColumnsMenu(_browseView.BrowseViewer);
			_notebookAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsCustomFieldsMenu();
			_notebookAreaMenuHelper.AddInsertMenuItems(false);

			AddInsertToolbarItems();
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
				_insertRecordToolStripButton.Click -= _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdInsertRecord);
				_insertFindRecordToolStripButton.Click -= _sharedEventHandlers.Get(NotebookAreaMenuHelper.CmdGoToRecord);
				_insertRecordToolStripButton?.Dispose();
				_insertFindRecordToolStripButton?.Dispose();
				_notebookAreaMenuHelper?.Dispose();
			}
			_majorFlexComponentParameters = null;
			_notebookAreaMenuHelper = null;
			_insertRecordToolStripButton = null;
			_insertFindRecordToolStripButton = null;
			_sharedEventHandlers = null;
			_browseView = null;

			_isDisposed = true;
		}
		#endregion

		private void AddInsertToolbarItems()
		{
			var newToolbarItems = new List<ToolStripItem>(2);

			_notebookAreaMenuHelper.AddCommonInsertToolbarItems(newToolbarItems);
			/*
			  <item command="CmdInsertRecord" defaultVisible="false" />
				Tooltip: <item id="CmdInsertRecord">Create a new Record in your Notebook.</item>
					<command id="CmdInsertRecord" label="Record" message="InsertItemInVector" icon="nbkRecord" shortcut="Ctrl+I">
					  <params className="RnGenericRec" />
					</command>
			*/
			_insertRecordToolStripButton = (ToolStripButton)newToolbarItems[0];

			/*
			  <item command="CmdGoToRecord" defaultVisible="false" />
			*/
			_insertFindRecordToolStripButton = (ToolStripButton)newToolbarItems[1]; ;

			InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, newToolbarItems);
		}
	}
}