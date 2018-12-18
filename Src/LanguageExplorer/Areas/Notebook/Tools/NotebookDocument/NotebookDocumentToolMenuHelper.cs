// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookDocument
{
	internal sealed class NotebookDocumentToolMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ISharedEventHandlers _sharedEventHandlers;
		private ToolStripMenuItem _editFindMenu;
		private ToolStripMenuItem _toolsConfigureMenu;
		private ToolStripMenuItem _toolsConfigureDocumentMenu;
		private NotebookAreaMenuHelper _notebookAreaMenuHelper;
		private XmlDocView _docView;
		private ToolStripButton _insertRecordToolStripButton;
		private ToolStripButton _insertFindRecordToolStripButton;
		private ToolStripItem _insertFindAndReplaceButton;

		internal NotebookDocumentToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool currentNotebookTool, IRecordList recordList, XmlDocView docView)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(currentNotebookTool, nameof(currentNotebookTool));
			Guard.AgainstNull(docView, nameof(docView));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
			_docView = docView;
			_editFindMenu = MenuServices.GetEditFindMenu(_majorFlexComponentParameters.MenuStrip);
			_editFindMenu.Enabled = _editFindMenu.Visible = true;
			_editFindMenu.Click += EditFindMenu_Click;
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(majorFlexComponentParameters, currentNotebookTool, recordList);
			_toolsConfigureMenu = MenuServices.GetToolsConfigureMenu(_majorFlexComponentParameters.MenuStrip);

			AddTool_ConfigureItem();
		}

		private void EditFindMenu_Click(object sender, EventArgs e)
		{
			PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App).ShowFindReplaceDialog(false, _majorFlexComponentParameters.MainWindow.ActiveView as IVwRootSite, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.MainWindow as Form);
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

			_notebookAreaMenuHelper.InitializeFlexComponent(flexComponentParameters);
			_notebookAreaMenuHelper.AddInsertMenuItems(false);

			// The find/Replace button is always on the toolbar, but not always visible/enabled.
			_insertFindAndReplaceButton = ToolbarServices.GetInsertFindAndReplaceToolStripItem(_majorFlexComponentParameters.ToolStripContainer);
			_insertFindAndReplaceButton.Click += EditFindMenu_Click;
			_insertFindAndReplaceButton.Enabled = _insertFindAndReplaceButton.Visible = true;

			AddInsertToolbarItems();
		}
		#endregion

		#region Implementation of IDisposable
		private bool _isDisposed;

		~NotebookDocumentToolMenuHelper()
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
				_editFindMenu.Click -= EditFindMenu_Click;
				_insertFindAndReplaceButton.Click -= EditFindMenu_Click;
				_insertFindAndReplaceButton.Enabled = _insertFindAndReplaceButton.Visible = false;
				_toolsConfigureMenu.DropDownItems.Remove(_toolsConfigureDocumentMenu);
				_toolsConfigureDocumentMenu.Click -= _docView.ConfigureXmlDocView_Clicked;
				_toolsConfigureDocumentMenu.Dispose();
				_insertRecordToolStripButton?.Dispose();
				_insertFindRecordToolStripButton?.Dispose();
				_notebookAreaMenuHelper?.Dispose();
			}
			_majorFlexComponentParameters = null;
			_editFindMenu = null;
			_toolsConfigureMenu = null;
			_toolsConfigureDocumentMenu = null;
			_notebookAreaMenuHelper = null;
			_insertRecordToolStripButton = null;
			_insertFindRecordToolStripButton = null;
			_sharedEventHandlers = null;
			_docView = null;
			_insertFindAndReplaceButton = null;

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

		private void AddTool_ConfigureItem()
		{
			/*
				<item label="{0}" command="CmdConfigureXmlDocView" defaultVisible="false" />
					<command id="CmdConfigureXmlDocView" label="{0}" message="ConfigureXmlDocView" />
			*/
			_toolsConfigureDocumentMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_toolsConfigureMenu, _docView.ConfigureXmlDocView_Clicked, AreaResources.ConfigureDocument, insertIndex: 0);
		}
	}
}