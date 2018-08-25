// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.TextsAndWords.Tools
{
	/// <summary>
	/// This menu helper is shared between these tools: ConcordanceTool, ComplexConcordanceTool, and WordListConcordanceTool.
	/// </summary>
	internal sealed class PartiallySharedMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ToolStripMenuItem _editFindMenu;
		private ToolStripMenuItem _editFindAndReplaceMenu;
		private ToolStripMenuItem _replaceToolStripMenuItem;
		private ToolStripItem _insertFindAndReplaceButton;
		private InterlinMasterNoTitleBar _interlinMasterNoTitleBar;

		internal PartiallySharedMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, InterlinMasterNoTitleBar interlinMasterNoTitleBar, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(interlinMasterNoTitleBar, nameof(interlinMasterNoTitleBar));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_interlinMasterNoTitleBar = interlinMasterNoTitleBar;

			_insertFindAndReplaceButton = ToolbarServices.GetInsertFindAndReplaceToolStripItem(_majorFlexComponentParameters.ToolStripContainer);
			_insertFindAndReplaceButton.Click += EditFindMenu_Click;

			_editFindMenu = MenuServices.GetEditFindMenu(_majorFlexComponentParameters.MenuStrip);
			_editFindMenu.Click += EditFindMenu_Click;

			_replaceToolStripMenuItem = MenuServices.GetEditFindAndReplaceMenu(_majorFlexComponentParameters.MenuStrip);
			_replaceToolStripMenuItem.Click += EditFindMenu_Click;

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
			Application.Idle += Application_Idle;
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			//Debug.WriteLine($"Start: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
			// Sort out whether to display/enable the _editFindMenu.
			// NB: This will work the same for the Edit-Replace menu.
			var oldVisible = _editFindMenu.Visible;
			var oldEnabled = _editFindMenu.Enabled;
			bool newEnabled;
			var newVisible = _interlinMasterNoTitleBar.CanDisplayFindTexMenutOrFindAndReplaceTextMenu(out newEnabled);
			if (oldVisible != newVisible)
			{
				_editFindMenu.Visible = newVisible;
				_replaceToolStripMenuItem.Visible = newVisible;
				_insertFindAndReplaceButton.Visible = newVisible;
			}
			if (oldEnabled != newEnabled)
			{
				_editFindMenu.Enabled = newEnabled;
				_replaceToolStripMenuItem.Enabled = newEnabled;
				_insertFindAndReplaceButton.Enabled = newEnabled;
			}
			//Debug.WriteLine($"End: Application.Idle run at: '{DateTime.Now:HH:mm:ss.ffff}': on '{GetType().Name}'.");
		}

		private void EditFindMenu_Click(object sender, EventArgs e)
		{
			_interlinMasterNoTitleBar.HandleFindAndReplace(sender == _insertFindAndReplaceButton || sender == _replaceToolStripMenuItem);
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

		#region Implementation of IDisposable
		private bool _isDisposed;

		~PartiallySharedMenuHelper()
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
				_editFindMenu.Click -= EditFindMenu_Click;
				_insertFindAndReplaceButton.Click -= EditFindMenu_Click;
			}
			_majorFlexComponentParameters = null;
			_editFindMenu = null;
			_insertFindAndReplaceButton = null;
			_interlinMasterNoTitleBar = null;

			_isDisposed = true;
		}
		#endregion
	}
}
