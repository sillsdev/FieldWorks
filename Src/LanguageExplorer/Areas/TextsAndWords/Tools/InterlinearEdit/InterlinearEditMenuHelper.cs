// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.InterlinearEdit
{
	internal sealed class InterlinearEditMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ToolStripMenuItem _editMenu;
		private ToolStripMenuItem _editFindMenu;
		private InterlinMaster _interlinMaster;

		internal InterlinearEditMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, InterlinMaster interlinMaster, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(interlinMaster, nameof(interlinMaster));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_interlinMaster = interlinMaster;
			_editMenu = MenuServices.GetEditMenu(_majorFlexComponentParameters.MenuStrip);
			_editFindMenu = MenuServices.GetEditFindMenu(_majorFlexComponentParameters.MenuStrip);
			_editFindMenu.Enabled = _editFindMenu.Visible = true;
			_editFindMenu.Click += EditFindMenu_Click;
			_editMenu.DropDownOpening += EditMenuOnDropDownOpening;

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		private void EditMenuOnDropDownOpening(object sender, EventArgs eventArgs)
		{
			// Sort out whether to display/enable the _editFindMenu.
			// NB: This will work the same for the Edit-Replace menu.
			bool enabled;
			_editFindMenu.Visible = _interlinMaster.CanDisplayFindTexMenutOrFindAndReplaceTextMenu(out enabled);
			_editFindMenu.Enabled = enabled;
		}

		private void EditFindMenu_Click(object sender, EventArgs e)
		{
			_interlinMaster.HandleFindAndReplace(false);
		}

		internal void Initialize()
		{
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

		~InterlinearEditMenuHelper()
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
				_editMenu.DropDownOpening -= EditMenuOnDropDownOpening;
				_editFindMenu.Click -= EditFindMenu_Click;
			}
			_majorFlexComponentParameters = null;
			_editFindMenu = null;
			_interlinMaster = null;
			_editMenu = null;

			_isDisposed = true;
		}
		#endregion
	}
}