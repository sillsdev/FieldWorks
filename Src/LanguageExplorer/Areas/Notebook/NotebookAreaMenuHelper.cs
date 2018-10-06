// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.LexText.DataNotebook;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Notebook
{
	/// <summary>
	/// Handle creation and use of Notebook area menus.
	/// </summary>
	internal sealed class NotebookAreaMenuHelper : IFlexComponent, IDisposable
	{
		internal const string CmdGoToRecord = "CmdGoToRecord";
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private DataTree MyDataTree { get; set; }
		private ISharedEventHandlers _sharedEventHandlers;
		private ITool _currentNotebookTool;
		private IRecordList _recordList;
		private ToolStripMenuItem _editMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newEditMenusAndHandlers;
		private ToolStripMenuItem _fileImportMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newFileMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();
		internal AreaWideMenuHelper MyAreaWideMenuHelper { get; private set; }

		internal NotebookAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool currentNotebookTool, DataTree dataTree)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(currentNotebookTool, nameof(currentNotebookTool));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			_currentNotebookTool = currentNotebookTool;
			MyDataTree = dataTree; // May be null.
			_recordList = majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IRecordListRepositoryForTools>(LanguageExplorerConstants.RecordListRepository).GetRecordList(NotebookArea.Records, majorFlexComponentParameters.StatusBar, NotebookArea.NotebookFactoryMethod);
			MyAreaWideMenuHelper = new AreaWideMenuHelper(_majorFlexComponentParameters, _recordList);

			_sharedEventHandlers.Add(CmdGoToRecord, GotoRecord_Clicked);
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

			// Add Edit menu item that is available in all Notebook tools.
			AddEditMenuItems();

			// File->Export menu is visible and enabled in this tool.
			// Add File->Export event handler.
			MyAreaWideMenuHelper.SetupFileExportMenu(FileExportMenu_Click);

			// Add one notebook area-wide import option.
			_fileImportMenu = MenuServices.GetFileImportMenu(_majorFlexComponentParameters.MenuStrip);
			// <item command="CmdImportSFMNotebook" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newFileMenusAndHandlers, _fileImportMenu, ImportSFMNotebook_Clicked, NotebookResources.Import_Standard_Format_Notebook_data, insertIndex: 1);
		}

		#endregion

		#region IDisposable
		private bool _isDisposed;

		~NotebookAreaMenuHelper()
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
				MyAreaWideMenuHelper.Dispose();

				foreach (var menuTuple in _newFileMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_fileImportMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newFileMenusAndHandlers.Clear();

				foreach (var menuTuple in _newEditMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_editMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newEditMenusAndHandlers.Clear();

				_sharedEventHandlers.Remove(CmdGoToRecord);
			}
			_majorFlexComponentParameters = null;
			MyDataTree = null;
			_sharedEventHandlers = null;
			_currentNotebookTool = null;
			MyAreaWideMenuHelper = null;
			_recordList = null;
			_fileImportMenu = null;
			_newFileMenusAndHandlers = null;
			_editMenu = null;
			_newEditMenusAndHandlers = null;

			_isDisposed = true;
		}
		#endregion

		private void AddEditMenuItems()
		{
			//< item command = "CmdGoToRecord" />
			_editMenu = MenuServices.GetEditMenu(_majorFlexComponentParameters.MenuStrip);
			_newEditMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>(1);
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newEditMenusAndHandlers, _editMenu, GotoRecord_Clicked, NotebookResources.Find_Record, NotebookResources.Find_a_Record_in_your_Notebook, Keys.Control | Keys.F, NotebookResources.goToRecord, 10);
		}

		private void GotoRecord_Clicked(object sender, EventArgs e)
		{
			/*
			    <command id="CmdGoToRecord" label="_Find Record..." message="GotoRecord" icon="goToRecord" shortcut="Ctrl+F" >
			      <parameters title="Go To Record" formlabel="Go _To..." okbuttonlabel="_Go" />
			    </command>
			*/
			using (var dlg = new RecordGoDlg())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				var cache = PropertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
				dlg.SetDlgInfo(cache, null);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					LinkHandler.PublishFollowLinkMessage(Publisher, new FwLinkArgs(_currentNotebookTool.MachineName, dlg.SelectedObject.Guid));
				}
			}
		}

		void FileExportMenu_Click(object sender, EventArgs e)
		{
			if (_recordList.AreCustomFieldsAProblem(new[] { RnGenericRecTags.kClassId }))
			{
				return;
			}
			using (var dlg = new NotebookExportDialog())
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
			}
		}

		private void ImportSFMNotebook_Clicked(object sender, EventArgs e)
		{
			using (var importWizardDlg = new NotebookImportWiz())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}
	}
}