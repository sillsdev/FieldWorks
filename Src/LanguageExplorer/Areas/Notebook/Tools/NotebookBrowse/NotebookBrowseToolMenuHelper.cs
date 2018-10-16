// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Notebook.Tools.NotebookBrowse
{
	/// <summary>
	/// This class handles all interaction for the NotebookBrowseTool for its menus, toolbars, plus all context menus that are used in Slices and PaneBars.
	/// </summary>
	internal sealed class NotebookBrowseToolMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private NotebookAreaMenuHelper _notebookAreaMenuHelper;
		private RecordBrowseView _browseView;

		internal NotebookBrowseToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters, ITool currentNotebookTool, IRecordList recordList, RecordBrowseView browseView)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(currentNotebookTool, nameof(currentNotebookTool));
			Guard.AgainstNull(browseView, nameof(browseView));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_notebookAreaMenuHelper = new NotebookAreaMenuHelper(majorFlexComponentParameters, currentNotebookTool, recordList);
			_browseView = browseView;
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

			_notebookAreaMenuHelper.InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
			_notebookAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsConfigureColumnsMenu(_browseView.BrowseViewer);
			_notebookAreaMenuHelper.MyAreaWideMenuHelper.SetupToolsCustomFieldsMenu();
			_notebookAreaMenuHelper.AddInsertMenuItems(false);
		}
		#endregion

		#region Implementation of IDisposable
		private bool _isDisposed;

		~NotebookBrowseToolMenuHelper()
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
				_notebookAreaMenuHelper?.Dispose();
			}
			_majorFlexComponentParameters = null;
			_notebookAreaMenuHelper = null;

			_isDisposed = true;
		}
		#endregion
	}
}