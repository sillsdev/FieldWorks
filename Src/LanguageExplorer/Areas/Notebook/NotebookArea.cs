// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;

namespace LanguageExplorer.Areas.Notebook
{
	internal sealed class NotebookArea : IArea
	{
		private readonly IToolRepository m_toolRepository;
		private ToolStripItem _fileExportMenu;
		private bool _fileExportOriginalValue;

		/// <summary>
		/// Contructor used by Reflection to feed the tool repository to the area.
		/// </summary>
		/// <param name="toolRepository"></param>
		internal NotebookArea(IToolRepository toolRepository)
		{
			m_toolRepository = toolRepository;
		}

		void FileExportMenu_Click(object sender, EventArgs e)
		{
#if RANDYTODO
			// TODO: Have to move NotebookExportDialog (and a lot more) from xWorks to this project,
			// TODO: before this can be enabled.
			// TODO: RecordClerk's "AreCustomFieldsAProblem" method will also need a new home: maybe FDO is a better place for it.
				if (AreCustomFieldsAProblem(new int[] { RnGenericRecTags.kClassId}))
					return true;
				using (var dlg = new NotebookExportDialog())
				{
					dlg.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
					dlg.ShowDialog();
				}
#else
			MessageBox.Show(PropertyTable.GetValue<Form>("window"), @"Grammar Sketch export not yet implemented. Stay tuned.", @"Export not ready", MessageBoxButtons.OK);
#endif
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

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(ICollapsingSplitContainer mainCollapsingSplitContainer,
			MenuStrip menuStrip, ToolStripContainer toolStripContainer, StatusBar statusbar)
		{
			_fileExportMenu.Click -= FileExportMenu_Click;
			_fileExportMenu.Enabled = _fileExportOriginalValue;
			_fileExportMenu.Visible = _fileExportOriginalValue;
			_fileExportMenu = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "See TODO-Linux comment")]
		public void Activate(ICollapsingSplitContainer mainCollapsingSplitContainer,
			MenuStrip menuStrip, ToolStripContainer toolStripContainer, StatusBar statusbar)
		{
			// File->Export menu is visible and enabled in this tool.
			// TODO-Linux: boolean 'searchAllChildren' parameter is marked with "MonoTODO".
			_fileExportMenu = menuStrip.Items.Find("exportToolStripMenuItem", true)[0];
			_fileExportOriginalValue = _fileExportMenu.Enabled;
			_fileExportMenu.Visible = true;
			_fileExportMenu.Enabled = true;
			_fileExportMenu.Click += FileExportMenu_Click;
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
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
		public string MachineName
		{
			get { return "notebook"; }
		}

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName
		{
			get { return "Notebook"; }
		}

		#endregion

		#region Implementation of IArea

		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		public ITool GetPersistedOrDefaultToolForArea()
		{
			return m_toolRepository.GetPersistedOrDefaultToolForArea(this);
		}

		/// <summary>
		/// Get the machine name of the area's default tool.
		/// </summary>
		public string DefaultToolMachineName
		{
			get { return "notebookEdit"; }
		}

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		public IList<ITool> AllToolsInOrder
		{
			get
			{
				var myToolsInOrder = new List<string>
				{
					"notebookEdit",
					"notebookBrowse",
					"notebookDocument"
				};
				return m_toolRepository.AllToolsForAreaInOrder(myToolsInOrder, MachineName);
			}
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon
		{
			get { return LanguageExplorerResources.Notebook.ToBitmap(); }
		}

		#endregion
	}
}
