// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
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
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private AreaWideMenuHelper _areaWideMenuHelper;
		private IRecordList _recordList;
		private ToolStripMenuItem _fileImportMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newFileMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();

		internal AreaWideMenuHelper MyAreaWideMenuHelper => _areaWideMenuHelper;

		internal NotebookAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_recordList = majorFlexComponentParameters.RecordListRepositoryForTools.GetRecordList(NotebookArea.Records, majorFlexComponentParameters.Statusbar, NotebookArea.NotebookFactoryMethod);
			_areaWideMenuHelper = new AreaWideMenuHelper(_majorFlexComponentParameters, _recordList);

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		internal void Initialize()
		{
			// File->Export menu is visible and enabled in this tool.
			// Add File->Export event handler.
			_areaWideMenuHelper.SetupFileExportMenu(FileExportMenu_Click);

			// Add one notebook area-wide import option.
			_fileImportMenu = MenuServices.GetFileImportMenu(_majorFlexComponentParameters.MenuStrip);
			// <item command="CmdImportSFMNotebook" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newFileMenusAndHandlers, _fileImportMenu, ImportSFMNotebook_Clicked, NotebookResources.Import_Standard_Format_Notebook_data, insertIndex: 1);
		}

		void FileExportMenu_Click(object sender, EventArgs e)
		{
				if (_recordList.AreCustomFieldsAProblem(new[] { RnGenericRecTags.kClassId}))
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
				_areaWideMenuHelper.Dispose();

				foreach (var menuTuple in _newFileMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_fileImportMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newFileMenusAndHandlers.Clear();
			}
			_majorFlexComponentParameters = null;
			_areaWideMenuHelper = null;
			_recordList = null;
			_fileImportMenu = null;
			_newFileMenusAndHandlers = null;

			_isDisposed = true;
		}
		#endregion
	}
}