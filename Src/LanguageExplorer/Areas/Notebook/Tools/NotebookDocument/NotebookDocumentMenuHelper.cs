// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookDocument
{
	internal sealed class NotebookDocumentMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ToolStripMenuItem _editFindMenu;

		internal NotebookDocumentMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_editFindMenu = MenuServices.GetEditFindMenu(_majorFlexComponentParameters.MenuStrip);
			_editFindMenu.Enabled = _editFindMenu.Visible = true;
			_editFindMenu.Click += EditFindMenu_Click;

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		private void EditFindMenu_Click(object sender, EventArgs e)
		{
			PropertyTable.GetValue<IApp>("App").ShowFindReplaceDialog(false, PropertyTable.GetValue<IFwMainWnd>("window").ActiveView as RootSite);
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
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}
		#endregion

		#region Implementation of IDisposable
		private bool _isDisposed;

		~NotebookDocumentMenuHelper()
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
				_editFindMenu.Click -= EditFindMenu_Click;
			}
			_majorFlexComponentParameters = null;
			_editFindMenu = null;

			_isDisposed = true;
		}
		#endregion
	}
}