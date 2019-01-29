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
		private MajorFlexComponentParameters _majorFlexComponentParameters;
		private ISharedEventHandlers _sharedEventHandlers;
		private ToolStripMenuItem _dataMenu;
		private ToolStripSeparator _insertToolStripSeparator;
		private ToolStripButton _insertBreakPhraseToolStripButton;
		private const int BreakPhraseImage = 7;

		internal FocusBoxMenuManager(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			Guard.AgainstNull(majorFlexComponentParameters, nameof(majorFlexComponentParameters));

			_majorFlexComponentParameters = majorFlexComponentParameters;
			_sharedEventHandlers = _majorFlexComponentParameters.SharedEventHandlers;
			SetupMnuFocusBoxContextMenu();
			SetupDataMenu();
			SetupInsertToolbar();
		}
		/*
			<command id="CmdApproveAndMoveNext" label="_Approve and Move Next" message="ApproveAndMoveNext" shortcut="Enter" icon="approveAndMoveNext" />
				// Tooltip: <item id="CmdApproveAndMoveNext">Approve the suggested analysis and move to the next word.</item>
			<command id="CmdApproveForWholeTextAndMoveNext" label="Approve _Throughout this Text" message="ApproveForWholeTextAndMoveNext" shortcut="Ctrl+E" icon="browseAndMoveNext" />
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
			<command id="CmdRepeatLastMoveLeft" label="Move _Left (last thing moved)" message="RepeatLastMoveLeft" shortcut="Ctrl+Left" />
				// Tooltip: NA
			<command id="CmdRepeatLastMoveRight" label="Move _Right (last thing moved)" message="RepeatLastMoveRight" shortcut="Ctrl+Right" />
				// Tooltip: NA
			<command id="CmdApproveAll" label="Approve All" message="ApproveAll" icon="approveAll" />
				// Tooltip: <item id="CmdApproveAll">Approve all the suggested analyses in this text.</item>
		*/

		private void SetupInsertToolbar()
		{
			var newToolbarItems = new List<ToolStripItem>(5);

			/*
				<toolbar id="Insert" >
					// NA: <item command="CmdInsertText" defaultVisible="false" />
					DONE: <item label="-" translate="do not translate" />
					// NA: <item command="CmdAddNote" defaultVisible="false" />
					<item command="CmdApproveAll" defaultVisible="false" />
					// NA: <item command="CmdInsertHumanApprovedAnalysis" defaultVisible="false" />
					// NA: <item command="CmdGoToWfiWordform" defaultVisible="false" />
					DONE: <item command="CmdBreakPhrase" defaultVisible="false" />
			*/
			using (var imageHolder = new InterlinearImageHolder())
			{
				// <item label="-" translate="do not translate" />
				_insertToolStripSeparator = ToolStripButtonFactory.CreateToolStripSeparator();
				newToolbarItems.Add(_insertToolStripSeparator);
				// <item command="CmdBreakPhrase" defaultVisible="false" />
				_insertBreakPhraseToolStripButton = ToolStripButtonFactory.CreateToolStripButton(_sharedEventHandlers.Get(InterlinearConstants.CmdBreakPhrase), "toolStripButtonBreakPhrase", imageHolder.buttonImages.Images[BreakPhraseImage], ITextStrings.Break_selected_phrase_into_words);
				newToolbarItems.Add(_insertBreakPhraseToolStripButton);
				var breakPhraseToolStripButtonStatus = _sharedEventHandlers.GetStatusChecker(InterlinearConstants.CmdBreakPhrase).Invoke();
				_insertBreakPhraseToolStripButton.Visible = breakPhraseToolStripButtonStatus.Item1;
				_insertBreakPhraseToolStripButton.Enabled = breakPhraseToolStripButtonStatus.Item2;
			}

			ToolbarServices.AddInsertToolbarItems(_majorFlexComponentParameters, newToolbarItems);
			Application.Idle += Application_Idle;
		}

		private void Application_Idle(object sender, EventArgs e)
		{
			var breakPhraseToolStripButtonStatus = _sharedEventHandlers.GetStatusChecker(InterlinearConstants.CmdBreakPhrase).Invoke();
			_insertBreakPhraseToolStripButton.Visible = breakPhraseToolStripButtonStatus.Item1;
			_insertBreakPhraseToolStripButton.Enabled = breakPhraseToolStripButtonStatus.Item2;
		}

		private void SetupDataMenu()
		{
			/*
				<menu id="Data" label="_Data" >
				  <!-- START include: "Words/areaConfiguration.xml" query="root/menuAddOn/menu[@id='Data']/*" -->
				  <item label="-" translate="do not translate" />
				  <!-- From here to the CmdApproveAll command, the menu items are the same as the popup menu "mnuFocusBox" -->
				  <item command="CmdApproveAndMoveNext" defaultVisible="false" />
				  <item command="CmdApproveForWholeTextAndMoveNext" defaultVisible="false" />
				  <item command="CmdNextIncompleteBundle" defaultVisible="false" />
				  <item command="CmdApprove" defaultVisible="false" />
				  <menu id="ApproveAnalysisMovementMenu" label="_Approve suggestion and" defaultVisible="false">
					<item command="CmdApproveAndMoveNextSameLine" />
					<item command="CmdMoveFocusBoxRight" />
					<item command="CmdMoveFocusBoxLeft" />
				  </menu>
				  <menu id="BrowseMovementMenu" label="Leave _suggestion and" defaultVisible="false">
					<item command="CmdBrowseMoveNext" />
					<item command="CmdNextIncompleteBundleNc" />
					<item command="CmdBrowseMoveNextSameLine" />
					<item command="CmdMoveFocusBoxRightNc" />
					<item command="CmdMoveFocusBoxLeftNc" />
				  </menu>
				  <item command="CmdMakePhrase" defaultVisible="false" />
				  <item command="CmdBreakPhrase" defaultVisible="false" />
				  <item label="-" translate="do not translate" />
				  <item command="CmdRepeatLastMoveLeft" defaultVisible="false" />
				  <item command="CmdRepeatLastMoveRight" defaultVisible="false" />
				  <item command="CmdApproveAll" defaultVisible="false" />
				  <!-- END include: "Words/areaConfiguration.xml" query="root/menuAddOn/menu[@id='Data']/*" -->
				</menu>
			*/
			_dataMenu = MenuServices.GetDataMenu(_majorFlexComponentParameters.MenuStrip);
		}

		private void SetupMnuFocusBoxContextMenu()
		{
			/*
				<menu id="mnuFocusBox">
				  <item command="CmdApproveAndMoveNext" />
				  <item command="CmdApproveForWholeTextAndMoveNext" />
				  <item command="CmdNextIncompleteBundle" />
				  <item command="CmdApprove">Approve the suggested analysis and stay on this word</item>
				  <menu id="ApproveAnalysisMovementMenu" label="_Approve suggestion and" defaultVisible="false">
					<item command="CmdApproveAndMoveNextSameLine" />
					<item command="CmdMoveFocusBoxRight" />
					<item command="CmdMoveFocusBoxLeft" />
				  </menu>
				  <menu id="BrowseMovementMenu" label="Leave _suggestion and" defaultVisible="false">
					<item command="CmdBrowseMoveNext" />
					<item command="CmdNextIncompleteBundleNc" />
					<item command="CmdBrowseMoveNextSameLine" />
					<item command="CmdMoveFocusBoxRightNc" />
					<item command="CmdMoveFocusBoxLeftNc" />
				  </menu>
				  <item command="CmdMakePhrase" defaultVisible="false" />
				  <item command="CmdBreakPhrase" defaultVisible="false" />
				  <item label="-" translate="do not translate" />
				  <item command="CmdRepeatLastMoveLeft" defaultVisible="false" />
				  <item command="CmdRepeatLastMoveRight" defaultVisible="false" />
				  <item command="CmdApproveAll">Approve all the suggested analyses and stay on this word</item>
				</menu>
			*/
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
				ToolbarServices.ResetInsertToolbar(_majorFlexComponentParameters);
				_insertToolStripSeparator.Dispose();
				_insertBreakPhraseToolStripButton.Click -= _sharedEventHandlers.Get(InterlinearConstants.CmdBreakPhrase);
				_insertBreakPhraseToolStripButton.Dispose();
			}

			_sharedEventHandlers = null;
			_insertToolStripSeparator = null;
			_insertBreakPhraseToolStripButton = null;
			_dataMenu = null;
			_majorFlexComponentParameters = null;
			_isDisposed = true;
		}
		#endregion
	}
}