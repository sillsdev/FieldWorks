// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Areas.Lexicon.DictionaryConfiguration;
using LanguageExplorer.Areas.Lexicon.Reversals;
using SIL.Code;

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	internal sealed class ReversalEditCompleteToolMenuHelper : IToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IArea _area;
		private IToolUiWidgetManager _commonReversalIndexMenuHelper;
		private ToolStripMenuItem _editFindMenu;
		private XhtmlDocView _docView;

		internal ReversalEditCompleteToolMenuHelper(XhtmlDocView docView)
		{
			Guard.AgainstNull(docView, nameof(docView));

			_docView = docView;
		}

		#region Implementation of IToolUiWidgetManager
		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(area, nameof(area));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_area = area;
			_editFindMenu = MenuServices.GetEditFindMenu(_majorFlexComponentParameters.MenuStrip);
			_editFindMenu.Enabled = _editFindMenu.Visible = true;
			_editFindMenu.Click += EditFindMenu_Click;
			_commonReversalIndexMenuHelper = new CommonReversalIndexMenuHelper();
			_commonReversalIndexMenuHelper.Initialize(majorFlexComponentParameters, area, recordList);
		}

		/// <inheritdoc />
		ITool IToolUiWidgetManager.ActiveTool => _area.ActiveTool;

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
			_commonReversalIndexMenuHelper.UnwireSharedEventHandlers();
		}
		#endregion

		#region Implementation of IDisposable
		private bool _isDisposed;

		~ReversalEditCompleteToolMenuHelper()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
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
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_editFindMenu.Click -= EditFindMenu_Click;
				_commonReversalIndexMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			_commonReversalIndexMenuHelper = null;
			_editFindMenu = null;
			_docView = null;

			_isDisposed = true;
		}
		#endregion

		private void EditFindMenu_Click(object sender, EventArgs e)
		{
			_docView.FindText();
		}
	}
}