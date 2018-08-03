// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	internal sealed class LexiconEditToolToolbarManager : IToolUiWidgetManager
	{
		private IRecordList MyRecordList { get; set; }
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ISharedEventHandlers _sharedEventHandlers;
		private ToolStripButton _insertEntryToolStripButton;
		private ToolStripButton _insertGoToEntryToolStripButton;

		#region IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_sharedEventHandlers = majorFlexComponentParameters.SharedEventHandlers;
			MyRecordList = recordList;

			// <item command="CmdInsertLexEntry" defaultVisible="false" />
			_insertEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertLexEntry), "toolStripButtonInsertEntry", LexiconResources.Major_Entry.ToBitmap(), LexiconResources.Entry_Tooltip);
			// <item command="CmdGoToEntry" defaultVisible="false" />
			_insertGoToEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(LexiconEditToolConstants.CmdGoToEntry), "toolStripButtonGoToEntry", LexiconResources.Find_Lexical_Entry.ToBitmap(), LexiconResources.GoToEntryToolTip);

			InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripButton> { _insertEntryToolStripButton, _insertGoToEntryToolStripButton });
		}

		/// <inheritdoc />
		void IToolUiWidgetManager.UnwireSharedEventHandlers()
		{
			_insertEntryToolStripButton.Click -= _sharedEventHandlers.Get(LexiconEditToolConstants.CmdInsertLexEntry);
			_insertGoToEntryToolStripButton.Click -= _sharedEventHandlers.Get(LexiconEditToolConstants.CmdGoToEntry);
		}

		#endregion

		#region IDisposable

		private bool _isDisposed;

		~LexiconEditToolToolbarManager()
		{
			// The base class finalizer is called automatically.
			Dispose(false);
		}

		/// <inheritdoc />
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
				// No need to do it more than once.
				return;
			}

			if (disposing)
			{
				InsertToolbarManager.ResetInsertToolbar(_majorFlexComponentParameters);
				_insertEntryToolStripButton.Dispose();
				_insertGoToEntryToolStripButton.Dispose();
			}
			MyRecordList = null;
			_majorFlexComponentParameters = null;
			_sharedEventHandlers = null;
			_insertEntryToolStripButton = null;
			_insertGoToEntryToolStripButton = null;

			_isDisposed = true;
		}

		#endregion
	}
}