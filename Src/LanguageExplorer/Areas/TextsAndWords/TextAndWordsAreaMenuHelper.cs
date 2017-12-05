// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Works;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.TextsAndWords
{
	internal sealed class TextAndWordsAreaMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		#region Area-wide
		private ToolStripMenuItem _insertMenuItem;
		private ToolStripSeparator _separator2ToolStripMenuItem;
		private ToolStripMenuItem _importWordSetToolStripMenuItem;
		private ToolStripMenuItem _addApprovedAnalysisToolStripMenuItem;
		#endregion Area-wide
		#region Tool-specific
		private ToolStripMenuItem _importMenuItem;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _importMenuItems;
		#endregion Tool-specific

		internal TextAndWordsAreaMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_importMenuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		internal void InitializeAreaWideMenus()
		{
			/*
			These are all possible menu items (Insert menu) for the Text & Words area.
			<menu id="Insert">
						<item command="CmdInsertText" defaultVisible="false"/>
						<item label="-" translate="do not translate"/>
						<item command="CmdAddNote" defaultVisible="false"/>
						<item command="CmdAddWordGlossesToFreeTrans" defaultVisible="false"/>
						<item label="Click Inserts Invisible Space" boolProperty="ClickInvisibleSpace" defaultVisible="false" settingsGroup="local" icon="zeroWidth"/>
						<item command="CmdGuessWordBreaks" defaultVisible="false"/>
						<item label="-" translate="do not translate"/>
DONE:					<item command="CmdImportWordSet" defaultVisible="false"/>
						<item command="CmdInsertHumanApprovedAnalysis" defaultVisible="false"/>
			</menu>
			All of the above menus go above the global separator named "insertMenuLastGlobalSeparator"
			*/
			_insertMenuItem = MenuServices.GetInsertMenu(_majorFlexComponentParameters.MenuStrip);

			// Add Approved Analysis...
			_addApprovedAnalysisToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_insertMenuItem, InsertHumanApprovedAnalysis_Click, "PH: " + TextAndWordsResources.ksAddApprovedAnalysis, string.Empty, Keys.None, LanguageExplorerResources.Add_New_Analysis.ToBitmap(), 0);
			_addApprovedAnalysisToolStripMenuItem.Enabled = false;
			_importWordSetToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_insertMenuItem, ImportWordSetToolStripMenuItemOnClick, TextAndWordsResources.ksImportWordSet, string.Empty, Keys.None, null, 0);
			_separator2ToolStripMenuItem = ToolStripMenuItemFactory.CreateToolStripSeparatorForToolStripMenuItem(_insertMenuItem, 0);
		}

		private void InsertHumanApprovedAnalysis_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO: Adding new human approved analysis here.");
		}

		private void ImportWordSetToolStripMenuItemOnClick(object sender, EventArgs eventArgs)
		{
			using (var dlg = new ImportWordSetDlg(_majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, RecordList.ActiveRecordClerkRepository.ActiveRecordClerk, _majorFlexComponentParameters.ParserMenuManager))
			{
				dlg.ShowDialog((Form)_majorFlexComponentParameters.MainWindow);
			}
		}

		/// <summary>
		/// The Concordance tool doesn't want these menus, but all other area tools do want them.
		/// </summary>
		internal void AddMenusForAllButConcordanceTool()
		{
			_importMenuItem = MenuServices.GetFileImportMenu(_majorFlexComponentParameters.MenuStrip);
			// "CmdImportInterlinearSfm"
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_importMenuItems, _importMenuItem, ImportInterlinearSfm_Click, TextAndWordsResources.Import_Standard_Format_Interlinear, string.Empty, Keys.None, null, _importMenuItem.DropDownItems.Count - 1);

			// "CmdImportWordsAndGlossesSfm"
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_importMenuItems, _importMenuItem, ImportWordsAndGlossesSfm_Click, TextAndWordsResources.Import_Standard_Format_Words_and_Glosses, string.Empty, Keys.None, null, _importMenuItem.DropDownItems.Count - 1);

			// "CmdImportInterlinearData"
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_importMenuItems, _importMenuItem, ImportInterlinearData_Click, TextAndWordsResources.Import_FLExText_Interlinear_Data, string.Empty, Keys.None, null, _importMenuItem.DropDownItems.Count - 1);
		}

		private void ImportInterlinearSfm_Click(object sender, EventArgs e)
		{
			using (var importWizardDlg = new InterlinearSfmImportWizard())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}

		private void ImportWordsAndGlossesSfm_Click(object sender, EventArgs e)
		{
			using (var importWizardDlg = new WordsSfmImportWizard())
			{
				AreaServices.HandleDlg(importWizardDlg, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.FlexApp, _majorFlexComponentParameters.MainWindow, _majorFlexComponentParameters.FlexComponentParameters.PropertyTable, _majorFlexComponentParameters.FlexComponentParameters.Publisher);
			}
		}

		private void ImportInterlinearData_Click(object sender, EventArgs e)
		{
			using (var importWizardDlg = new InterlinearImportDlg())
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
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region IDisposable
		private bool _isDisposed;

		~TextAndWordsAreaMenuHelper()
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
				if (_insertMenuItem != null)
				{
					// Setup by area.
					_insertMenuItem.DropDownItems.Remove(_separator2ToolStripMenuItem);
					_separator2ToolStripMenuItem.Dispose();

					_insertMenuItem.DropDownItems.Remove(_addApprovedAnalysisToolStripMenuItem);
					_addApprovedAnalysisToolStripMenuItem.Dispose();

					_insertMenuItem.DropDownItems.Remove(_importWordSetToolStripMenuItem);
					_importWordSetToolStripMenuItem.Dispose();
				}
				if (_importMenuItem != null)
				{
					foreach (var tuple in _importMenuItems)
					{
						tuple.Item1.Click -= tuple.Item2;
						_importMenuItem.DropDownItems.Remove(tuple.Item1);
						tuple.Item1.Dispose();
					}
					_importMenuItems.Clear();
				}
			}
			_majorFlexComponentParameters = null;
			_insertMenuItem = null;
			_separator2ToolStripMenuItem = null;
			_importWordSetToolStripMenuItem = null;
			_addApprovedAnalysisToolStripMenuItem = null;
			_importMenuItem = null;
			_importMenuItems = null;

		_isDisposed = true;
		}
		#endregion
	}
}