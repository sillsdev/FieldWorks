// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Provides menu adjustments for areas/tools that cross those boundaries, and that areas/tools can be more selective in what to use.
	/// One might think of these as more 'global', but not quite to the level of 'universal' across all areaas/tools,
	/// which events are handled by the main window.
	/// </summary>
	internal sealed class AreaWideMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IRecordList _recordList;
		private ToolStripMenuItem _fileExportMenu;
		private EventHandler _foreignFileExportHandler;
		private bool _usingLocalFileExportEventHandler;
		private ToolStripMenuItem _toolsConfigureMenu;
		private ToolStripSeparator _toolsCustomFieldsSeparatorMenu;
		private ToolStripMenuItem _toolsCustomFieldsMenu;

		internal AreaWideMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_majorFlexComponentParameters = majorFlexComponentParameters;

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		internal AreaWideMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
			: this(majorFlexComponentParameters)
		{
			Guard.AgainstNull(recordList, nameof(recordList));

			_recordList = recordList;
		}

		/// <summary>
		/// Setup the File->Export menu.
		/// </summary>
		/// <param name="handler">The handler to use, or null to use the more globel one.</param>
		internal void SetupFileExportMenu(EventHandler handler = null)
		{
			_foreignFileExportHandler = handler;
			// File->Export menu is visible and enabled in this tool.
			// Add File->Export event handler.
			_fileExportMenu = MenuServices.GetFileExportMenu(_majorFlexComponentParameters.MenuStrip);
			_fileExportMenu.Visible = true;
			_fileExportMenu.Enabled = true;
			_fileExportMenu.Click += _foreignFileExportHandler ?? CommonFileExportMenu_Click;
			_usingLocalFileExportEventHandler = _foreignFileExportHandler == null;
		}

		private void CommonFileExportMenu_Click(object sender, EventArgs e)
		{
			// This handles the general case, if nobody else is handling it.
			// Areas/Tools that uses this code:
			// A. lexicon area: all 8 tools
			// B. textsWords area: Analyses, bulkEditWordforms, wordListConcordance
			// C. grammar area: all tools, except grammarSketch, which goes its own way
			// D. lists area: all 27 tools
			if (_recordList.AreCustomFieldsAProblem(new[] { LexEntryTags.kClassId, LexSenseTags.kClassId, LexExampleSentenceTags.kClassId, MoFormTags.kClassId }))
			{
				return;
			}
			using (var dlg = new ExportDialog(_majorFlexComponentParameters.Statusbar))
			{
				dlg.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
				dlg.ShowDialog(PropertyTable.GetValue<Form>("window"));
			}
		}

		/// <summary>
		/// Setup the File->Export menu.
		/// </summary>
		internal void SetupToolsCustomFieldsMenu()
		{
			// Tools->CustomFields menu is visible and enabled in this tool.
			_toolsConfigureMenu = MenuServices.GetToolsConfigureMenu(_majorFlexComponentParameters.MenuStrip);
			_toolsCustomFieldsSeparatorMenu = ToolStripMenuItemFactory.CreateToolStripSeparatorForToolStripMenuItem(_toolsConfigureMenu);
			_toolsCustomFieldsMenu = ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_toolsConfigureMenu, AddCustomField_Click, AreaResources.CustomFields, AreaResources.CustomFieldsTooltip);
		}

		private void AddCustomField_Click(object sender, EventArgs e)
		{
			var activeForm = PropertyTable.GetValue<Form>("window");
			if (SharedBackendServices.AreMultipleApplicationsConnected(PropertyTable.GetValue<LcmCache>("cache")))
			{
				MessageBoxUtils.Show(activeForm, AreaResources.ksCustomFieldsCanNotBeAddedDueToOtherAppsText, AreaResources.ksCustomFieldsCanNotBeAddedDueToOtherAppsCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			var locationType = CustomFieldLocationType.Lexicon;
			var areaChoice = PropertyTable.GetValue<string>(AreaServices.AreaChoice);
			switch (areaChoice)
			{
				case AreaServices.LexiconAreaMachineName:
					locationType = CustomFieldLocationType.Lexicon;
					break;
				case AreaServices.NotebookAreaMachineName:
					locationType = CustomFieldLocationType.Notebook;
					break;
				case AreaServices.TextAndWordsAreaMachineName:
					locationType = CustomFieldLocationType.Interlinear;
					break;
			}
			using (var dlg = new AddCustomFieldDlg(PropertyTable, Publisher, locationType))
			{
				if (dlg.ShowCustomFieldWarning(activeForm))
				{
					dlg.ShowDialog(activeForm);
				}
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

		~AreaWideMenuHelper()
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
				if (_fileExportMenu != null)
				{
					if (_usingLocalFileExportEventHandler)
					{
						_fileExportMenu.Click -= CommonFileExportMenu_Click;
					}
					else
					{
						_fileExportMenu.Click -= _foreignFileExportHandler;
					}
					_fileExportMenu.Visible = false;
					_fileExportMenu.Enabled = false;
				}
				if (_toolsConfigureMenu != null)
				{
					_toolsCustomFieldsMenu.Click -= AddCustomField_Click;
					_toolsConfigureMenu.DropDownItems.Remove(_toolsCustomFieldsMenu);
					_toolsConfigureMenu.DropDownItems.Remove(_toolsCustomFieldsSeparatorMenu);
					_toolsCustomFieldsMenu.Dispose();
					_toolsCustomFieldsSeparatorMenu.Dispose();
				}
			}
			_majorFlexComponentParameters = null;
			_recordList = null;
			_fileExportMenu = null;
			_foreignFileExportHandler = null;
			_toolsConfigureMenu = null;
			_toolsCustomFieldsSeparatorMenu = null;
			_toolsCustomFieldsMenu = null;

			_isDisposed = true;
		}
		#endregion
	}
}