// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.Grammar.Tools.GrammarSketch
{
	/// <summary>
	/// Handle creation and use of the grammar sketch tool menus.
	/// </summary>
	internal sealed class GrammarSketchToolMenuHelper : IFlexComponent, IDisposable
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private GrammarAreaMenuHelper _grammarAreaWideMenuHelper;
		private bool _refreshOriginalValue;
		private ToolStripItem _refreshMenu;
		private ToolStripItem _refreshToolBarBtn;

		internal GrammarSketchToolMenuHelper(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_grammarAreaWideMenuHelper = new GrammarAreaMenuHelper(_majorFlexComponentParameters, null, FileExportMenu_Click);

			InitializeFlexComponent(_majorFlexComponentParameters.FlexComponentParameters);
		}

		internal void Initialize()
		{
			// F5 refresh is disabled in this tool.
			_refreshMenu = MenuServices.GetViewRefreshMenu(_majorFlexComponentParameters.MenuStrip);
			_refreshOriginalValue = _refreshMenu.Enabled;
			_refreshMenu.Enabled = false;
			_refreshToolBarBtn = ToolbarServices.GetStandardToolStripRefreshButton(_majorFlexComponentParameters.ToolStripContainer);
			_refreshToolBarBtn.Enabled = false;
		}

		void FileExportMenu_Click(object sender, EventArgs e)
		{
			using (var dlg = new ExportDialog(_majorFlexComponentParameters.Statusbar))
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.ShowDialog(PropertyTable.GetValue<Form>("window"));
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

		~GrammarSketchToolMenuHelper()
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
				_refreshMenu.Enabled = _refreshOriginalValue;
				_refreshToolBarBtn.Enabled = _refreshOriginalValue;
				_grammarAreaWideMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			_grammarAreaWideMenuHelper = null;
			_refreshMenu = null;
			_refreshToolBarBtn = null;

			_isDisposed = true;
		}
		#endregion
	}
}