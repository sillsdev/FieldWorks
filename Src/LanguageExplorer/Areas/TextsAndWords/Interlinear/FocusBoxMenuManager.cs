// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Class that produces the "mnuFocusBox" popup menu for the FocusBoxController class,
	/// and for the same menus on the main Data menu.
	/// </summary>
	/// <remarks>
	/// The menu items are the same for both.
	/// </remarks>
	internal class FocusBoxMenuManager : IDisposable
	{
		private ISharedEventHandlers _privatelySharedEventHandlers;
		private ToolStrip _insertToolStrip;
		private ToolStripSeparator _insertToolStripSeparator;
		private ToolStripButton _insertBreakPhraseToolStripButton;
		private bool _isActive;

		internal FocusBoxMenuManager(MajorFlexComponentParameters majorFlexComponentParameters, ISharedEventHandlers privatelySharedEventHandlers)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));
			Guard.AgainstNull(privatelySharedEventHandlers, nameof(privatelySharedEventHandlers));

			_privatelySharedEventHandlers = privatelySharedEventHandlers;
			_insertToolStrip = ToolbarServices.GetInsertToolStrip(majorFlexComponentParameters.ToolStripContainer);
		}

		/// <summary>
		/// Set up UI widgets.
		/// </summary>
		internal void Activate()
		{
			if (_isActive)
			{
				// Nothing to do.
				return;
			}
			//SetupInsertToolbar();
			//Application.Idle += Application_Idle;
			_isActive = true;
		}

		/// <summary>
		/// Tear down UI widgets.
		/// </summary>
		internal void Deactivate()
		{
			if (!_isActive)
			{
				// Nothing to do.
				return;
			}
			//Application.Idle -= Application_Idle;
			//TearDownInsertToolbar();
			_isActive = false;
		}
		/*
			<command id="CmdApproveAndMoveNext" label="_Approve and Move Next" message="ApproveAndMoveNext" shortcut="Enter" icon="approveAndMoveNext" />
				// Tooltip: <item id="CmdApproveAndMoveNext">Approve the suggested analysis and move to the next word.</item>
			<command id="CmdApproveForWholeTextAndMoveNext" label="Approve _Throughout this Text" message="ApproveForWholeTextAndMoveNext" shortcut="Ctrl+E" icon="browseAndMoveNext" /> Approve_Throughout_this_Text
				// Tooltip: <item id="CmdApproveForWholeTextAndMoveNext">Approve the suggested analysis throughout this text, and move to the next word.</item>
			<command id="CmdNextIncompleteBundle" label="Approve and _Jump to Next Incomplete" message="NextIncompleteBundle" shortcut="Ctrl+J" />
				// Tooltip: <item id="CmdNextIncompleteBundle">Approve the suggested analysis, and jump to the next word with a suggested or incomplete analysis.</item>
			<command id="CmdApprove" label="Approve and _Stay" message="ApproveAndStayPut" shortcut="Ctrl+S" />
				// Tooltip: <item id="CmdApprove">Approve the suggested analysis and stay on this word</item>
			<command id="CmdApproveAndMoveNextSameLine" label="Move Next, _Same Line" message="ApproveAndMoveNextSameLine" shortcut="Ctrl+Enter" />
				// Tooltip: <item id="CmdApproveAndMoveNextSameLine">Approve the suggested analysis and move to the next word, to the same interlinear line.</item>
			<command id="CmdMoveFocusBoxRight" label="Move _Right" message="MoveFocusBoxRight" shortcut="Ctrl+Right" />
				// Tooltip: <item id="CmdMoveFocusBoxRight">Approve the suggested analysis and move to the word on the right.</item>
			<command id="CmdMoveFocusBoxLeft" label="Move _Left" message="MoveFocusBoxLeft" shortcut="Ctrl+Left" />
				// Tooltip: <item id="CmdMoveFocusBoxLeft">Approve the suggested analysis and move to the word on the left,</item>
			<command id="CmdBrowseMoveNext" label="Move _Next" message="BrowseMoveNext" shortcut="Shift+Enter" />
				// Tooltip: <item id="CmdBrowseMoveNext">Leave the suggested analysis as a suggestion, and move to the next word.</item>
			<command id="CmdNextIncompleteBundleNc" label="_Jump to Next" message="NextIncompleteBundleNc" shortcut="Shift+Ctrl+J" />
				// Tooltip: <item id="CmdNextIncompleteBundleNc">Leave the suggested analysis as a suggestion, and jump to the next word with a suggested or incomplete analysis.</item>
			<command id="CmdBrowseMoveNextSameLine" label="Move Next, _Same Line" message="BrowseMoveNextSameLine" shortcut="Shift+Ctrl+Enter" />
				// Tooltip: <item id="CmdBrowseMoveNextSameLine">Leave the suggested analysis and move to the next word, to the same interlinear line.</item>
			<command id="CmdMoveFocusBoxRightNc" label="Move _Right" message="MoveFocusBoxRightNc" shortcut="Shift+Ctrl+Right" />
				// Tooltip: <item id="CmdMoveFocusBoxRightNc">Leave the suggested analysis as a suggestion, and move to the word on the right.</item>
			<command id="CmdMoveFocusBoxLeftNc" label="Move _Left" message="MoveFocusBoxLeftNc" shortcut="Shift+Ctrl+Left" />
				// Tooltip: <item id="CmdMoveFocusBoxLeftNc">Leave the suggested analysis as a suggestion, and move to the word on the left.</item>
			<command id="CmdMakePhrase" label="_Make phrase with next word" message="JoinWords" icon="linkWords" shortcut="Ctrl+M" />
				// Tooltip: NA
			<command id="CmdBreakPhrase" label="_Break phrase into words" message="BreakPhrase" icon="breakPhrase" shortcut="Ctrl+W" />
				// Tooltip: <item id="CmdBreakPhrase">Break selected phrase into words.</item>
			<command id="CmdRepeatLastMoveLeft" label="Move _Left (last thing moved)" message="RepeatLastMoveLeft" shortcut="Ctrl+Left" /> ConstituentChart impl
				// Tooltip: NA
			<command id="CmdRepeatLastMoveRight" label="Move _Right (last thing moved)" message="RepeatLastMoveRight" shortcut="Ctrl+Right" /> ConstituentChart impl
				// Tooltip: NA
			<command id="CmdApproveAll" label="Approve All" message="ApproveAll" icon="approveAll" />
				// Tooltip: <item id="CmdApproveAll">Approve all the suggested analyses in this text.</item>
		*/

		private void SetupInsertToolbar()
		{
			var newToolbarItems = new List<ToolStripItem>(5);
			/*
				<toolbar id="Insert" >
					// Not my worry: <item command="CmdInsertText" defaultVisible="false" />
					DONE: <item label="-" translate="do not translate" />
					// Not my worry: <item command="CmdAddNote" defaultVisible="false" />
					// Not my worry (but DONE by FocusBoxController): <item command="CmdApproveAll" defaultVisible="false" />
					// Not my worry (but DONE by TextAndWordsAreaMenuHelper): <item command="CmdInsertHumanApprovedAnalysis" defaultVisible="false" />
					// Not my worry: <item command="CmdGoToWfiWordform" defaultVisible="false" />
					DONE: <item command="CmdBreakPhrase" defaultVisible="false" />
			*/
			using (var imageHolder = new InterlinearImageHolder())
			{
				// <item label="-" translate="do not translate" />
				_insertToolStripSeparator = ToolStripButtonFactory.CreateToolStripSeparator();
				newToolbarItems.Add(_insertToolStripSeparator);
				// <item command="CmdBreakPhrase" defaultVisible="false" />
				_insertBreakPhraseToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_privatelySharedEventHandlers.Get(LanguageExplorerConstants.CmdBreakPhrase), "toolStripButtonBreakPhrase", imageHolder.buttonImages.Images[InterlinearConstants.CmdBreakPhraseImageIndex], ITextStrings.Break_selected_phrase_into_words);
				newToolbarItems.Add(_insertBreakPhraseToolStripButton);
				var breakPhraseToolStripButtonStatus = _privatelySharedEventHandlers.GetStatusChecker(LanguageExplorerConstants.CmdBreakPhrase).Invoke();
				_insertBreakPhraseToolStripButton.Visible = breakPhraseToolStripButtonStatus.Item1;
				_insertBreakPhraseToolStripButton.Enabled = breakPhraseToolStripButtonStatus.Item2;
			}
			ToolbarServices.AddInsertToolbarItems(_insertToolStrip, newToolbarItems);
		}

		private void TearDownInsertToolbar()
		{
			ToolbarServices.ResetInsertToolbar(_insertToolStrip);
			_insertToolStripSeparator.Dispose();
			_insertBreakPhraseToolStripButton.Click -= _privatelySharedEventHandlers.Get(LanguageExplorerConstants.CmdBreakPhrase);
			_insertBreakPhraseToolStripButton.Dispose();
			_insertToolStripSeparator = null;
			_insertBreakPhraseToolStripButton = null;
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			var breakPhraseToolStripButtonStatus = _privatelySharedEventHandlers.GetStatusChecker(LanguageExplorerConstants.CmdBreakPhrase).Invoke();
			_insertBreakPhraseToolStripButton.Visible = breakPhraseToolStripButtonStatus.Item1;
			_insertBreakPhraseToolStripButton.Enabled = breakPhraseToolStripButtonStatus.Item2;
		}

		#region IDisposable
		private bool _isDisposed;
		~FocusBoxMenuManager()
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
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				if (_isActive)
				{
					Deactivate();
				}
			}

			_privatelySharedEventHandlers = null;
			_insertToolStrip = null;
			_isDisposed = true;
		}
		#endregion
	}
}