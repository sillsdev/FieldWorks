// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace LanguageExplorer.Areas.Lexicon.Tools.ClassifiedDictionary
{
	internal sealed class LexiconClassifiedDictionaryToolMenuHelper : IToolUiWidgetManager
	{
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private IArea _area;
		private ITool _tool;
		private XmlDocView _xmlDocView;
		private IAreaUiWidgetManager _lexiconAreaMenuHelper;
		private ToolStripMenuItem _editFindMenu;
		private ToolStripMenuItem _replaceToolStripMenuItem;

		internal LexiconClassifiedDictionaryToolMenuHelper(ITool tool, XmlDocView xmlDocView)
		{
			Guard.AgainstNull(xmlDocView, nameof(xmlDocView));

			_tool = tool;
			_xmlDocView = xmlDocView;
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
			_replaceToolStripMenuItem = MenuServices.GetEditFindAndReplaceMenu(_majorFlexComponentParameters.MenuStrip);
			_replaceToolStripMenuItem.Click += EditFindMenu_Click;
			_replaceToolStripMenuItem.Visible = true;
			_replaceToolStripMenuItem.Enabled = _xmlDocView.CanUseReplaceText();
			_lexiconAreaMenuHelper = new LexiconAreaMenuHelper(_tool);
			_lexiconAreaMenuHelper.Initialize(majorFlexComponentParameters, area, recordList);
		}

		/// <inheritdoc />
		ITool IToolUiWidgetManager.ActiveTool => _area.ActiveTool;

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
			_lexiconAreaMenuHelper.UnwireSharedEventHandlers();
		}
		#endregion

		private void EditFindMenu_Click(object sender, EventArgs e)
		{
			_majorFlexComponentParameters.FlexComponentParameters.PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App).ShowFindReplaceDialog(sender == _editFindMenu, _majorFlexComponentParameters.MainWindow.ActiveView as IVwRootSite, _majorFlexComponentParameters.LcmCache, _majorFlexComponentParameters.MainWindow as Form);
		}

		#region Implementation of IDisposable
		private bool _isDisposed;

		~LexiconClassifiedDictionaryToolMenuHelper()
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
				_replaceToolStripMenuItem.Click -= EditFindMenu_Click;
				_editFindMenu.Click -= EditFindMenu_Click;
				_lexiconAreaMenuHelper.Dispose();
			}
			_majorFlexComponentParameters = null;
			_lexiconAreaMenuHelper = null;
			_editFindMenu = null;
			_replaceToolStripMenuItem = null;

			_isDisposed = true;
		}
		#endregion
	}
}