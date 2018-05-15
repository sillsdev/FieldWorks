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
	internal class LexiconEditToolToolbarManager : IToolUiWidgetManager
	{
		private IRecordList MyRecordList { get; set; }
		private Dictionary<string, EventHandler> _sharedEventHandlers;
		private Dictionary<string, EventHandler> _sharedWithMeEventHandlers;
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ToolStripButton _insertEntryToolStripButton;
		private ToolStripButton _insertGoToEntryToolStripButton;

		#region IToolUiWidgetManager

		/// <inheritdoc />
		void IToolUiWidgetManager.Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList, IReadOnlyDictionary<string, EventHandler> sharedEventHandlers, IReadOnlyList<object> randomParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(recordList, nameof(recordList));
			Guard.AgainstNull(sharedEventHandlers, nameof(sharedEventHandlers));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			MyRecordList = recordList;
			_sharedWithMeEventHandlers = new Dictionary<string, EventHandler>(2)
			{
				{ LexiconEditToolConstants.CmdInsertLexEntry, sharedEventHandlers[LexiconEditToolConstants.CmdInsertLexEntry] },
				{ LexiconEditToolConstants.CmdGoToEntry, sharedEventHandlers[LexiconEditToolConstants.CmdGoToEntry] }
			};

			// <item command="CmdInsertLexEntry" defaultVisible="false" />
			_insertEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertLexEntry], "toolStripButtonInsertEntry", LexiconResources.Major_Entry.ToBitmap(), LexiconResources.Entry_Tooltip);
			// <item command="CmdGoToEntry" defaultVisible="false" />
			_insertGoToEntryToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedWithMeEventHandlers[LexiconEditToolConstants.CmdGoToEntry], "toolStripButtonGoToEntry", LexiconResources.Find_Lexical_Entry.ToBitmap(), LexiconResources.GoToEntryToolTip);

			InsertToolbarManager.AddInsertToolbarItems(_majorFlexComponentParameters, new List<ToolStripButton> { _insertEntryToolStripButton, _insertGoToEntryToolStripButton });
		}

		/// <inheritdoc />
		IReadOnlyDictionary<string, EventHandler> IToolUiWidgetManager.SharedEventHandlers => _sharedEventHandlers ?? (_sharedEventHandlers = new Dictionary<string, EventHandler>());

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
				_insertEntryToolStripButton.Click -= _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdInsertLexEntry];
				_insertGoToEntryToolStripButton.Click -= _sharedWithMeEventHandlers[LexiconEditToolConstants.CmdGoToEntry];
				InsertToolbarManager.ResetInsertToolbar(_majorFlexComponentParameters);
				_insertEntryToolStripButton.Dispose();
				_insertGoToEntryToolStripButton.Dispose();
				_sharedEventHandlers.Clear();
				_sharedWithMeEventHandlers.Clear();
			}
			MyRecordList = null;
			_sharedEventHandlers = null;
			 _sharedWithMeEventHandlers = null;
			_majorFlexComponentParameters = null;
			_insertEntryToolStripButton = null;
			_insertGoToEntryToolStripButton = null;

			_isDisposed = true;
		}

		#endregion
	}
}