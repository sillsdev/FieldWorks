// Copyright (c) 2018-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Implementation that supports the addition(s) to FLEx's main Edit menu for the Lexicon Edit tool.
	/// </summary>
	internal sealed class LexiconEditToolEditMenuManager : IToolUiWidgetManager
	{
		private IRecordList MyRecordList { get; set; }
		private FlexComponentParameters _flexComponentParameters;
		private LcmCache _cache;
		private IFwMainWnd _mainWnd;
		private ISharedEventHandlers _sharedEventHandlers;
		private ToolStripMenuItem _editMenu;
		private List<Tuple<ToolStripMenuItem, EventHandler>> _newEditMenusAndHandlers = new List<Tuple<ToolStripMenuItem, EventHandler>>();

		#region IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_flexComponentParameters = majorFlexComponentParameters.FlexComponentParameters;
			_cache = majorFlexComponentParameters.LcmCache;
			_mainWnd = majorFlexComponentParameters.MainWindow;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			_sharedEventHandlers.Add(LexiconEditToolConstants.CmdGoToEntry, GoToEntry_Clicked);
			MyRecordList = recordList;

			_editMenu = MenuServices.GetEditMenu(majorFlexComponentParameters.MenuStrip);
			// Insert before third separator menu
			// <item command="CmdGoToEntry" />
			ToolStripMenuItemFactory.CreateToolStripMenuItemForToolStripMenuItem(_newEditMenusAndHandlers, _editMenu, GoToEntry_Clicked, LexiconResources.Find_Entry, LexiconResources.GoToEntryToolTip, Keys.Control | Keys.F, LexiconResources.Find_Lexical_Entry.ToBitmap(), 10);
		}

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
		}

		#endregion

		#region IDisposable

		private bool _isDisposed;

		~LexiconEditToolEditMenuManager()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
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
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
				_sharedEventHandlers.Remove(LexiconEditToolConstants.CmdGoToEntry);
				foreach (var menuTuple in _newEditMenusAndHandlers)
				{
					menuTuple.Item1.Click -= menuTuple.Item2;
					_editMenu.DropDownItems.Remove(menuTuple.Item1);
					menuTuple.Item1.Dispose();
				}
				_newEditMenusAndHandlers.Clear();
			}
			MyRecordList = null;
			_sharedEventHandlers = null;
			_flexComponentParameters = null;
			_cache = null;
			_mainWnd = null;
			_editMenu = null;
			_newEditMenusAndHandlers = null;

			_isDisposed = true;
		}

		#endregion

		private void GoToEntry_Clicked(object sender, EventArgs e)
		{
			using (var dlg = new EntryGoDlg())
			{
				dlg.InitializeFlexComponent(_flexComponentParameters);
				var windowParameters = new WindowParams
				{
					m_btnText = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go),
					m_label = FwUtils.ReplaceUnderlineWithAmpersand(LexiconResources.Go_To),
					m_title = LexiconResources.Go_To_Entry_Dlg_Title
				};
				dlg.SetDlgInfo(_cache, windowParameters);
				dlg.SetHelpTopic("khtpFindLexicalEntry");
				if (dlg.ShowDialog((Form)_mainWnd) == DialogResult.OK)
				{
					MyRecordList.JumpToRecord(dlg.SelectedObject.Hvo);
				}
			}
		}
	}
}