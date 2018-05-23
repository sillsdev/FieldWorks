// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.LexText;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// This class handles all interaction for the Lexicon Area common menus.
	/// </summary>
	internal sealed class LexiconAreaMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private AreaWideMenuHelper _areaWideMenuHelper;
		private ToolStripMenuItem _fileImportMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newFileMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();

		internal AreaWideMenuHelper MyAreaWideMenuHelper => _areaWideMenuHelper;

		internal LexiconAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_areaWideMenuHelper = new AreaWideMenuHelper(_majorFlexComponentParameters, recordList);

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		internal void Initialize()
		{
			// Set up File->Export menu, which is visible and enabled in all lexicon area tools, using the default event handler.
			_areaWideMenuHelper.SetupFileExportMenu();

			// Add two lexicon area-wide import options.
			AddFileImportMenuItems();
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

		~LexiconAreaMenuHelper()
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
			_fileImportMenu = null;
			_newFileMenusAndHandlers = null;

			_isDisposed = true;
		}
		#endregion

		private void AddFileImportMenuItems()
		{
			_fileImportMenu = MenuServices.GetFileImportMenu(_majorFlexComponentParameters.MenuStrip);

			// <item command="CmdImportLinguaLinksData" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newFileMenusAndHandlers, _fileImportMenu, ImportLinguaLinksData_Clicked, LexiconResources.ImportLinguaLinksData, insertIndex: 1);

			// <item command="CmdImportLiftData" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newFileMenusAndHandlers, _fileImportMenu, ImportLiftData_Clicked, LexiconResources.ImportLIFTLexicon, insertIndex: 2);
		}

		private void ImportLinguaLinksData_Clicked(object sender, EventArgs e)
		{
			using (var importWizardDlg = new LinguaLinksImportDlg())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}

		private void ImportLiftData_Clicked(object sender, EventArgs e)
		{
			using (var importWizardDlg = new LiftImportDlg())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}
	}
}