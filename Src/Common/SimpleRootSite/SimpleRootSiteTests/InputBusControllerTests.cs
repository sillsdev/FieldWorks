// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InputBusControllerTests.cs
// Responsibility:
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for InputBusController
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[Platform(Include = "Linux", Reason="InputBusController is Linux only")]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test. Variable disposed in Teardown method")]
	public class InputBusControllerTests: BaseTest
	{
		// some lparam values representing keypress that we use for testing.
		Dictionary<char, uint> lparams = new Dictionary<char, uint>();
		protected DummySimpleRootSite m_dummySimpleRootSite;
		protected IIBusCommunicator m_dummyIBusCommunicator;
		protected InputBusController m_inputBusController;

		/// <summary/>
		public InputBusControllerTests()
		{
			lparams.Add('A', 0x40260001);
			lparams.Add('B', 0x40380001);
			lparams.Add('C', 0x40360001);
			lparams.Add('D', 0x40280001);
			lparams.Add('E', 0x401A0001);
			lparams.Add('F', 0x40290001);
//			lparams.Add('G', );
//			lparams.Add('H', );
			lparams.Add('I', 0x401F0001);
//			lparams.Add('J', );
//			lparams.Add('K', );
//			lparams.Add('L', );
			lparams.Add('M', 0x403A0001);
			lparams.Add('N', 0x40390001);
			lparams.Add('O', 0x40200001);
			lparams.Add('P', 0x40210001);
			lparams.Add('Q', 0x40180001);
			lparams.Add('R', 0x401B0001);
			lparams.Add('S', 0x40270001);
			lparams.Add('T', 0x401C0001);
			lparams.Add('U', 0x401E0001);
			lparams.Add('V', 0x40370001);
			lparams.Add('W', 0x40190001);
			lparams.Add('X', 0x40350001);
			lparams.Add('Y', 0x401D0001);
			lparams.Add('Z', 0x40340001);
			lparams.Add(' ', 0x40410001); // space
			lparams.Add('\b', 0x40160001); // backspace
		}

		/// <summary></summary>
		[SetUp]
		public virtual void TestSetup()
		{
			m_dummySimpleRootSite = new DummySimpleRootSite();
			Assert.NotNull(m_dummySimpleRootSite.RootBox);
		}

		[TearDown]
		public void TestTearDown()
		{
			if (m_inputBusController != null)
				m_inputBusController.Dispose();

			if (m_dummyIBusCommunicator != null)
				m_dummyIBusCommunicator.Dispose();

			m_dummySimpleRootSite.Visible = false;
			m_dummySimpleRootSite.Close();
			m_dummySimpleRootSite.Dispose();

			m_inputBusController = null;
			m_dummyIBusCommunicator = null;
			m_dummySimpleRootSite = null;
		}

		public void ChooseSimulatedKeyboard(IIBusCommunicator ibusCommunicator)
		{
			m_dummyIBusCommunicator = ibusCommunicator;
			m_inputBusController = new InputBusController(m_dummySimpleRootSite,
				m_dummyIBusCommunicator);
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="NoPreeditDummyIBusCommunicator is disposed in TestTearDown()")]
		public void NotifyKeyPress_EmptyStateSendSingleKeyPress_SelectionIsInsertionPoint()
		{
			ChooseSimulatedKeyboard(new NoPreeditDummyIBusCommunicator());

			m_inputBusController.NotifyKeyPress('T', lparams['T'], Keys.Shift);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			// SimpleRootSite should contain a 'T'
			Assert.AreEqual("T", dummyRootBox.Text);

			// Current Selection should be an insertion point, at position 1.
			Assert.AreEqual(String.Empty, dummySelection.SelectionText);
			Assert.AreEqual(dummySelection.Anchor, dummySelection.End);
			Assert.AreEqual(1, dummySelection.Anchor);
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="NoPreeditDummyIBusCommunicator is disposed in TestTearDown()")]
		public void NotifyKeyPress_EmptyStateSendTwoKeyPresses_SelectionIsInsertionPoint()
		{
			ChooseSimulatedKeyboard(new NoPreeditDummyIBusCommunicator());

			m_inputBusController.NotifyKeyPress('T', lparams['T'], Keys.Shift);
			m_inputBusController.NotifyKeyPress('U', lparams['U'], Keys.Shift);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			// SimpleRootSite should contain "TU"
			Assert.AreEqual("TU", dummyRootBox.Text);

			// Current Selection should be an insertion point, at position 2.
			Assert.AreEqual(String.Empty, dummySelection.SelectionText);
			Assert.AreEqual(dummySelection.Anchor, dummySelection.End);
			Assert.AreEqual(2, dummySelection.Anchor);
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="NoPreeditDummyIBusCommunicator is disposed in TestTearDown()")]
		public void NotifyKeyPress_EmptyStateSendSingleControlCharacter_SelectionIsInsertionPoint()
		{
			ChooseSimulatedKeyboard(new NoPreeditDummyIBusCommunicator());

			// Send a Control char Backspace; 0x0008
			m_inputBusController.NotifyKeyPress('\b', lparams['\b'], 0);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual(String.Empty, dummyRootBox.Text);

			Assert.AreEqual(String.Empty, dummySelection.SelectionText);
			Assert.AreEqual(dummySelection.Anchor, dummySelection.End);
			Assert.AreEqual(0, dummySelection.Anchor);
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="PreeditDummyIBusCommunicator is disposed in TestTearDown()")]
		public void NotifyKeyPress_SimplePreeditEmptyStateSendSingleKeyPress_SelectionIsRange()
		{
			ChooseSimulatedKeyboard(new PreeditDummyIBusCommunicator());

			m_inputBusController.NotifyKeyPress('T', lparams['T'], Keys.Shift);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual("T", dummyRootBox.Text);

			// Current Selection (the preedit 0 -> 1)
			Assert.AreEqual("T", dummySelection.SelectionText);
			Assert.AreEqual(0, dummySelection.Anchor);
			Assert.AreEqual(1, dummySelection.End);
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="PreeditDummyIBusCommunicator is disposed in TestTearDown()")]
		public void NotifyKeyPress_SimplePreeditEmptyStateSendTwoKeyPresses_SelectionIsRange()
		{
			ChooseSimulatedKeyboard(new PreeditDummyIBusCommunicator());

			m_inputBusController.NotifyKeyPress('T', lparams['T'], Keys.Shift);
			m_inputBusController.NotifyKeyPress('U', lparams['U'], Keys.Shift);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual("TU", dummyRootBox.Text);

			// Current Selection (the preedit 1 -> 2)
			Assert.AreEqual("U", dummySelection.SelectionText);
			Assert.AreEqual(1, dummySelection.Anchor);
			Assert.AreEqual(2, dummySelection.End);
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="PreeditDummyIBusCommunicator is disposed in TestTearDown()")]
		public void NotifyKeyPress_SimplePreeditEmptyStateSendThreeKeyPresses_SelectionIsRange()
		{
			ChooseSimulatedKeyboard(new PreeditDummyIBusCommunicator());

			m_inputBusController.NotifyKeyPress('S', lparams['S'], Keys.Shift);
			m_inputBusController.NotifyKeyPress('T', lparams['T'], Keys.Shift);
			m_inputBusController.NotifyKeyPress('U', lparams['U'], Keys.Shift);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual("STU", dummyRootBox.Text);

			// Current Selection (the preedit 2 -> 3)
			Assert.AreEqual("U", dummySelection.SelectionText);
			Assert.AreEqual(2, dummySelection.Anchor);
			Assert.AreEqual(3, dummySelection.End);
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="PreeditDummyIBusCommunicator is disposed in TestTearDown()")]
		public void KillFocus_ShowingPreedit_PreeditIsNotCommitedAndSelectionIsInsertionPoint()
		{
			ChooseSimulatedKeyboard(new PreeditDummyIBusCommunicator());

			m_inputBusController.NotifyKeyPress((uint)'T', lparams['T'], Keys.Shift);

			m_inputBusController.KillFocus();

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual(String.Empty, dummyRootBox.Text);

			Assert.AreEqual(String.Empty, dummySelection.SelectionText);
			Assert.AreEqual(0, dummySelection.Anchor);
			Assert.AreEqual(0, dummySelection.End);
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="PreeditDummyIBusCommunicator is disposed in TestTearDown()")]
		public void Focus_Unfocused_KeypressAcceptedAsNormal()
		{
			ChooseSimulatedKeyboard(new PreeditDummyIBusCommunicator());

			m_inputBusController.NotifyKeyPress('S', lparams['S'], Keys.Shift);

			m_inputBusController.KillFocus();

			m_inputBusController.Focus();

			m_inputBusController.NotifyKeyPress('T', lparams['T'], Keys.Shift);

			m_inputBusController.NotifyKeyPress('U', lparams['U'], Keys.Shift);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual("TU", dummyRootBox.Text);

			Assert.AreEqual("U", dummySelection.SelectionText);
			Assert.AreEqual(1, dummySelection.Anchor);
			Assert.AreEqual(2, dummySelection.End);
		}

		#region KeyboardThatCommitsPreeditOnSpace
		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardThatCommitsPreeditOnSpace is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardThatCommitsPreeditOnSpace_OneCharNoSpace_PreeditContainsChar()
		{
			ChooseSimulatedKeyboard(new KeyboardThatCommitsPreeditOnSpace());

			m_inputBusController.NotifyKeyPress('t', lparams['T'], Keys.None);

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.That(document.Text, Is.EqualTo("t"));

			Assert.That(preedit.SelectionText, Is.EqualTo("t"));
			Assert.That(preedit.Anchor, Is.EqualTo(0));
			Assert.That(preedit.End, Is.EqualTo(preedit.SelectionText.Length)); // 1
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardThatCommitsPreeditOnSpace is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardThatCommitsPreeditOnSpace_TwoCharsNoSpace_PreeditContainsChars()
		{
			ChooseSimulatedKeyboard(new KeyboardThatCommitsPreeditOnSpace());

			m_inputBusController.NotifyKeyPress('t', lparams['T'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.That(document.Text, Is.EqualTo("tu"));

			Assert.That(preedit.SelectionText, Is.EqualTo("tu"));
			Assert.That(preedit.Anchor, Is.EqualTo(0));
			Assert.That(preedit.End, Is.EqualTo(preedit.SelectionText.Length)); // 2
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardThatCommitsPreeditOnSpace is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardThatCommitsPreeditOnSpace_TwoCharsAndSpace_PreeditIsCommitted()
		{
			ChooseSimulatedKeyboard(new KeyboardThatCommitsPreeditOnSpace());

			m_inputBusController.NotifyKeyPress('t', lparams['T'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.That(document.Text, Is.EqualTo("TU"));

			Assert.That(preedit.SelectionText, Is.EqualTo(string.Empty));
			Assert.That(preedit.Anchor, Is.EqualTo(2));
			Assert.That(preedit.End, Is.EqualTo(2));
		}

		/// <summary/>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardThatCommitsPreeditOnSpace is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardThatCommitsPreeditOnSpace_TwoCharsSpaceTwoChars_PreeditIsLastHalf()
		{
			ChooseSimulatedKeyboard(new KeyboardThatCommitsPreeditOnSpace());

			m_inputBusController.NotifyKeyPress('t', lparams['T'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);
			m_inputBusController.NotifyKeyPress('s', lparams['S'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Don't commit

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			var expectedDocument = "TUsu";
			var expectedPreedit = "su";

			Assert.That(document.Text, Is.EqualTo(expectedDocument));

			Assert.That(preedit.SelectionText, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(2));
			Assert.That(preedit.End, Is.EqualTo(4));
		}

		/// <summary/>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardThatCommitsPreeditOnSpace is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardThatCommitsPreeditOnSpace_TwoCharsSpaceTwoCharsSpace_PreeditIsEmpty()
		{
			ChooseSimulatedKeyboard(new KeyboardThatCommitsPreeditOnSpace());

			m_inputBusController.NotifyKeyPress('t', lparams['T'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);
			m_inputBusController.NotifyKeyPress('s', lparams['S'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			var expectedDocument = "TUSU";
			var expectedPreedit = "";

			Assert.That(document.Text, Is.EqualTo(expectedDocument));
			Assert.That(preedit.SelectionText, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(4));
			Assert.That(preedit.End, Is.EqualTo(4));
		}
		#endregion KeyboardThatCommitsPreeditOnSpace

		#region KeyboardWithGlyphSubstitution
		/// <summary/>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardWithGlyphSubstitution is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardWithGlyphSubstitution_Space_JustAddsToDocument()
		{
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());

			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			var expectedDocument = " ";
			var expectedPreedit = "";

			Assert.That(document.Text, Is.EqualTo(expectedDocument));
			Assert.That(preedit.SelectionText, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(1));
			Assert.That(preedit.End, Is.EqualTo(1));
		}

		/// <summary/>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardWithGlyphSubstitution is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardWithGlyphSubstitution_TwoChars_OnlyPreedit()
		{
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());

			m_inputBusController.NotifyKeyPress('t', lparams['T'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Don't commit

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			var expectedDocument = "tu";
			var expectedPreedit = "tu";

			Assert.That(document.Text, Is.EqualTo(expectedDocument));
			Assert.That(preedit.SelectionText, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(0));
			Assert.That(preedit.End, Is.EqualTo(2));
		}

		/// <summary/>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardWithGlyphSubstitution is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardWithGlyphSubstitution_TwoCharsSpace_SubstitutionWorkedAndPreeditIsEmpty()
		{
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());

			m_inputBusController.NotifyKeyPress('t', lparams['T'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			var expectedDocument = "T";
			var expectedPreedit = "";

			Assert.That(document.Text, Is.EqualTo(expectedDocument));
			Assert.That(preedit.SelectionText, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(1));
			Assert.That(preedit.End, Is.EqualTo(1));
		}

		/// <summary/>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardWithGlyphSubstitution is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardWithGlyphSubstitution_TwoCharsSpaceTwoCharsSpace_SubstitutionWorkedAndPreeditIsEmpty()
		{
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());

			m_inputBusController.NotifyKeyPress('t', lparams['T'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);
			m_inputBusController.NotifyKeyPress('s', lparams['S'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			var expectedDocument = "TS";
			var expectedPreedit = "";

			Assert.That(document.Text, Is.EqualTo(expectedDocument));
			Assert.That(preedit.SelectionText, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(2));
			Assert.That(preedit.End, Is.EqualTo(2));
		}

		/// <summary/>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardWithGlyphSubstitution is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardWithGlyphSubstitution_TwoCharsSpaceTwoChars_SubstitutionWorkedAndPreeditIsLastHalf()
		{
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());

			m_inputBusController.NotifyKeyPress('t', lparams['T'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);
			m_inputBusController.NotifyKeyPress('s', lparams['S'], Keys.None);
			m_inputBusController.NotifyKeyPress('u', lparams['U'], Keys.None);
			// Don't commit

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			var expectedDocument = "Tsu";
			var expectedPreedit = "su";

			Assert.That(document.Text, Is.EqualTo(expectedDocument));
			Assert.That(preedit.SelectionText, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(1));
			Assert.That(preedit.End, Is.EqualTo(3));
		}

		#region KeyboardWithGlyphSubstitution_ReplacingSelection
		/// <summary>
		/// Common helper.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardWithGlyphSubstitution is disposed in TestTearDown()")]
		private void KeyboardWithGlyphSubstitution_CreateDocumentABC()
		{
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());

			m_inputBusController.NotifyKeyPress('a', lparams['A'], Keys.None);
			m_inputBusController.NotifyKeyPress('a', lparams['A'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);
			m_inputBusController.NotifyKeyPress('b', lparams['B'], Keys.None);
			m_inputBusController.NotifyKeyPress('b', lparams['B'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);
			m_inputBusController.NotifyKeyPress('c', lparams['C'], Keys.None);
			m_inputBusController.NotifyKeyPress('c', lparams['C'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);
		}

		/// <summary>
		/// Verify the common helper method works as expected.
		/// </summary>
		[Test]
		public void NotifyKeyPress_KeyboardWithGlyphSubstitution_CreatesDocument()
		{
			KeyboardWithGlyphSubstitution_CreateDocumentABC();

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			var expectedDocument = "ABC";
			var expectedPreedit = "";

			Assert.That(document.Text, Is.EqualTo(expectedDocument));
			Assert.That(preedit.SelectionText, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(3));
			Assert.That(preedit.End, Is.EqualTo(3));
		}

		/// <summary>
		/// FWNX-674
		/// </summary>
		[Test]
		public void
		NotifyKeyPress_KeyboardWithGlyphSubstitution_ReplaceForwardSelectedChar_Replaced()
		{
			KeyboardWithGlyphSubstitution_CreateDocumentABC();

			// Select B, from left to right side.
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;
			preedit.Anchor = 1;
			preedit.End = 2;

			KeyboardWithGlyphSubstitution_ReplaceSelectionAndVerify();
		}

		/// <summary>
		/// FWNX-674
		/// </summary>
		[Test]
		public void
		NotifyKeyPress_KeyboardWithGlyphSubstitution_ReplaceBackwardSelectedChar_Replaced()
		{
			KeyboardWithGlyphSubstitution_CreateDocumentABC();

			// Select B, but from right side to left side
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;
			preedit.Anchor = 2;
			preedit.End = 1;

			KeyboardWithGlyphSubstitution_ReplaceSelectionAndVerify();
		}

		/// <summary>
		/// Common helper.
		/// </summary>
		private void KeyboardWithGlyphSubstitution_ReplaceSelectionAndVerify()
		{
			m_inputBusController.NotifyKeyPress('d', lparams['D'], Keys.None);
			m_inputBusController.NotifyKeyPress('d', lparams['D'], Keys.None);
			// Commit by pressing space
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], Keys.None);

			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			var expectedDocument = "ADC";
			var expectedPreedit = "";

			Assert.That(document.Text, Is.EqualTo(expectedDocument));
			Assert.That(preedit.SelectionText, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(2));
			Assert.That(preedit.End, Is.EqualTo(2));
		}
		#endregion KeyboardWithGlyphSubstitution_ReplacingSelection
		#endregion KeyboardWithGlyphSubstitution

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardThatSendsDeletesAsCommitsDummyIBusCommunicator is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardThatSendsBackspacesInItsCommits_BackspacesShouldNotBeIngored()
		{
			ChooseSimulatedKeyboard(new KeyboardThatSendsDeletesAsCommitsDummyIBusCommunicator());

			m_inputBusController.NotifyKeyPress('S', lparams['S'], Keys.Shift);
			m_inputBusController.NotifyKeyPress('T', lparams['T'], Keys.Shift);
			m_inputBusController.NotifyKeyPress('U', lparams['U'], Keys.Shift);
			// Send a Space 0x0020
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], 0);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual("stu", dummyRootBox.Text);

			Assert.AreEqual(String.Empty, dummySelection.SelectionText);
			Assert.AreEqual(3, dummySelection.Anchor);
			Assert.AreEqual(3, dummySelection.End);
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="KeyboardThatSendsDeletesAsCommitsDummyIBusCommunicator is disposed in TestTearDown()")]
		public void NotifyKeyPress_KeyboardThatSendsBackspacesInItsForwardKeyEvent_BackspacesShouldNotBeIngored()
		{
			ChooseSimulatedKeyboard(new KeyboardThatSendsBackspacesAsForwardKeyEvents());

			m_inputBusController.NotifyKeyPress('S', lparams['S'], 0);
			m_inputBusController.NotifyKeyPress('T', lparams['T'], 0);
			m_inputBusController.NotifyKeyPress('U', lparams['U'], 0);
			// Send a Space 0x0020
			m_inputBusController.NotifyKeyPress(' ', lparams[' '], 0);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual("stu", dummyRootBox.Text);

			Assert.AreEqual(String.Empty, dummySelection.SelectionText);
			Assert.AreEqual(3, dummySelection.Anchor);
			Assert.AreEqual(3, dummySelection.End);
		}
	}

	#region Mock classes used for testing InputBusController

	/// <summary></summary>
	public class DummyVwSelection : IVwSelection
	{
		public int Anchor;
		public int End;
		public string SelectionText;
		private readonly DummyRootBox m_rootBox;

		public DummyVwSelection(DummyRootBox rootbox, int anchor, int end)
		{
			m_rootBox = rootbox;
			Anchor = anchor;
			End = end;

			if (anchor >= m_rootBox.Text.Length)
				SelectionText = String.Empty;
			else
				SelectionText = m_rootBox.Text.Substring(anchor, end - anchor);
		}

		#region IVwSelection implementation
		public void GetSelectionProps(int cttpMax, ArrayPtr _rgpttp,
			ArrayPtr _rgpvps, out int _cttp)
		{
			_cttp = 0;
		}

		public void GetHardAndSoftCharProps(int cttpMax, ArrayPtr _rgpttpSel,
			ArrayPtr _rgpvpsSoft, out int _cttp)
		{
			_cttp = 0;
		}

		public void GetParaProps(int cttpMax, ArrayPtr _rgpvps, out int _cttp)
		{
			_cttp = 0;
		}

		public void GetHardAndSoftParaProps(int cttpMax, ITsTextProps[] _rgpttpPara,
			ArrayPtr _rgpttpHard, ArrayPtr _rgpvpsSoft, out int _cttp)
		{
			_cttp = 0;
		}

		public void SetSelectionProps(int cttp, ITsTextProps[] _rgpttp)
		{
		}

		public void TextSelInfo(bool fEndPoint, out ITsString _ptss, out int _ich,
			out bool _fAssocPrev, out int _hvoObj, out int _tag, out int _ws)
		{
			_ptss = null;
			_ich = 0;
			_fAssocPrev = false;
			_hvoObj = 0;
			_tag = 0;
			_ws = 0;
		}

		public int CLevels(bool fEndPoint)
		{
			return 0;
		}

		public void PropInfo(bool fEndPoint, int ilev, out int _hvoObj, out int _tag, out int _ihvo,
			out int _cpropPrevious, out IVwPropertyStore _pvps)
		{
			_hvoObj = 0;
			_tag = 0;
			_ihvo = 0;
			_cpropPrevious = 0;
			_pvps = null;
		}

		public void AllTextSelInfo(out int _ihvoRoot, int cvlsi, ArrayPtr _rgvsli,
			out int _tagTextProp, out int _cpropPrevious, out int _ichAnchor, out int _ichEnd,
			out int _ws, out bool _fAssocPrev, out int _ihvoEnd, out ITsTextProps _pttp)
		{
			_ihvoRoot = 0;
			_tagTextProp = 0;
			_cpropPrevious = 0;
			_ichAnchor = 0;
			_ichEnd = 0;
			_ws = 0;
			_fAssocPrev = false;
			_ihvoEnd = 0;
			_pttp = null;
		}

		public void AllSelEndInfo(bool fEndPoint, out int _ihvoRoot, int cvlsi, ArrayPtr _rgvsli,
			out int _tagTextProp, out int _cpropPrevious, out int _ich, out int _ws,
			out bool _fAssocPrev, out ITsTextProps _pttp)
		{
			_ihvoRoot = 0;
			_tagTextProp = 0;
			_cpropPrevious = 0;
			if (fEndPoint)
				_ich = End;
			else
				_ich = Anchor;
			_ws = 0;
			_fAssocPrev = false;
			_pttp = null;
		}

		public bool CompleteEdits(out VwChangeInfo _ci)
		{
			_ci = default(VwChangeInfo);
			return true;
		}

		public void ExtendToStringBoundaries()
		{
		}

		public void Location(IVwGraphics _vg, Utils.Rect rcSrc, Utils.Rect rcDst,
			out Utils.Rect _rdPrimary, out Utils.Rect _rdSecondary, out bool _fSplit,
			out bool _fEndBeforeAnchor)
		{
			_rdPrimary = default(Utils.Rect);
			_rdSecondary = default(Utils.Rect);
			_fSplit = false;
			_fEndBeforeAnchor = false;
		}

		public void GetParaLocation(out Utils.Rect _rdLoc)
		{
			_rdLoc = default(Utils.Rect);
		}

		public void ReplaceWithTsString(ITsString _tss)
		{
			SelectionText = _tss != null ? _tss.Text : String.Empty;
			if (SelectionText == null)
				SelectionText = String.Empty;
			var begin = Math.Min(Anchor, End);
			var end = Math.Max(Anchor, End);
			if (begin < m_rootBox.Text.Length)
				m_rootBox.Text = m_rootBox.Text.Remove(begin, end - begin);
			if (begin < m_rootBox.Text.Length)
				m_rootBox.Text = m_rootBox.Text.Insert(begin, SelectionText);
			else
				m_rootBox.Text += SelectionText;
		}

		public void GetSelectionString(out ITsString _ptss, string bstrSep)
		{
			_ptss = TsStringHelper.MakeTSS(SelectionText,
				m_rootBox.m_dummySimpleRootSite.WritingSystemFactory.UserWs);
		}

		public void GetFirstParaString(out ITsString _ptss, string bstrSep, out bool _fGotItAll)
		{
			throw new NotImplementedException();
		}

		public void SetIPLocation(bool fTopLine, int xdPos)
		{
			throw new NotImplementedException();
		}

		public void Install()
		{
			throw new NotImplementedException();
		}

		public bool get_Follows(IVwSelection _sel)
		{
			throw new NotImplementedException();
		}

		public int get_ParagraphOffset(bool fEndPoint)
		{
			throw new NotImplementedException();
		}

		public IVwSelection GrowToWord()
		{
			throw new NotImplementedException();
		}

		public IVwSelection EndPoint(bool fEndPoint)
		{
			throw new NotImplementedException();
		}

		public void SetTypingProps(ITsTextProps _ttp)
		{
			throw new NotImplementedException();
		}

		public int get_BoxDepth(bool fEndPoint)
		{
			throw new NotImplementedException();
		}

		public int get_BoxIndex(bool fEndPoint, int iLevel)
		{
			throw new NotImplementedException();
		}

		public int get_BoxCount(bool fEndPoint, int iLevel)
		{
			throw new NotImplementedException();
		}

		public VwBoxType get_BoxType(bool fEndPoint, int iLevel)
		{
			throw new NotImplementedException();
		}

		public bool IsRange
		{
			get
			{
				return true;
			}
		}

		public bool EndBeforeAnchor
		{
			get
			{
				if (End < Anchor)
					return true;
				return false;
			}
		}

		public bool CanFormatPara
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool CanFormatChar
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool CanFormatOverlay
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool IsValid
		{
			get
			{
				return true;
			}
		}

		public bool AssocPrev
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public VwSelType SelType
		{
			get
			{
				return VwSelType.kstText;
			}
		}

		public IVwRootBox RootBox
		{
			get
			{
				return m_rootBox;
			}
		}

		public bool IsEditable
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool IsEnabled
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		#endregion
	}

	public class NullOpActionHandler : IActionHandler
	{
		#region IActionHandler implementation
		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
		}

		public void EndUndoTask()
		{
		}

		public void ContinueUndoTask()
		{
		}

		public void EndOuterUndoTask()
		{
		}

		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
		}

		public void BeginNonUndoableTask()
		{
		}

		public void EndNonUndoableTask()
		{
		}

		public void CreateMarkIfNeeded(bool fCreateMark)
		{
		}

		public void StartSeq(string bstrUndo, string bstrRedo, IUndoAction _uact)
		{
		}

		public void AddAction(IUndoAction _uact)
		{
		}

		public string GetUndoText()
		{
			return String.Empty;
		}

		public string GetUndoTextN(int iAct)
		{
			return String.Empty;
		}

		public string GetRedoText()
		{
			return String.Empty;
		}

		public string GetRedoTextN(int iAct)
		{
			return String.Empty;
		}

		public bool CanUndo()
		{
			return false;
		}

		public bool CanRedo()
		{
			return false;
		}

		public UndoResult Undo()
		{
			return default(UndoResult);
		}

		public UndoResult Redo()
		{
			return default(UndoResult);
		}

		public void Rollback(int nDepth)
		{
		}

		public void Commit()
		{
		}

		public void Close()
		{
		}

		public int Mark()
		{
			return 0;
		}

		public bool CollapseToMark(int hMark, string bstrUndo, string bstrRedo)
		{
			return false;
		}

		public void DiscardToMark(int hMark)
		{
		}

		public bool get_TasksSinceMark(bool fUndo)
		{
			return false;
		}

		public int CurrentDepth { get { return 0; } }

		public int TopMarkHandle  { get { return 0; } }

		public int UndoableActionCount  { get { return 0; } }

		public int UndoableSequenceCount { get { return 0; } }

		public int RedoableSequenceCount  { get { return 0; } }

		public IUndoGrouper UndoGrouper
		{
			get { return null;}
			set {}
		}

		public bool IsUndoOrRedoInProgress { get { return false; } }

		public bool SuppressSelections  { get { return false; } }
		#endregion
	}

	public class DummyDataAccess : ISilDataAccess
	{
		IActionHandler m_actionHandler = new NullOpActionHandler();

		#region ISilDataAccess implementation
		public int get_ObjectProp(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public int get_VecItem(int hvo, int tag, int index)
		{
			throw new System.NotImplementedException();
		}

		public int get_VecSize(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public int get_VecSizeAssumeCached(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public void VecProp(int hvo, int tag, int chvoMax, out int _chvo, ArrayPtr _rghvo)
		{
			throw new System.NotImplementedException();
		}

		public void BinaryPropRgb(int obj, int tag, ArrayPtr _rgb, int cbMax, out int _cb)
		{
			throw new System.NotImplementedException();
		}

		public System.Guid get_GuidProp(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public int get_ObjFromGuid(System.Guid uid)
		{
			throw new System.NotImplementedException();
		}

		public int get_IntProp(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public long get_Int64Prop(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public bool get_BooleanProp(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			throw new System.NotImplementedException();
		}

		public ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public object get_Prop(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public ITsString get_StringProp(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public long get_TimeProp(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public string get_UnicodeProp(int obj, int tag)
		{
			throw new System.NotImplementedException();
		}

		public void set_UnicodeProp(int obj, int tag, string bstr)
		{
			throw new System.NotImplementedException();
		}

		public void UnicodePropRgch(int obj, int tag, ArrayPtr _rgch, int cchMax, out int _cch)
		{
			throw new System.NotImplementedException();
		}

		public object get_UnknownProp(int hvo, int tag)
		{
			throw new System.NotImplementedException();
		}

		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			throw new System.NotImplementedException();
		}

		public void EndUndoTask()
		{
			throw new System.NotImplementedException();
		}

		public void ContinueUndoTask()
		{
			throw new System.NotImplementedException();
		}

		public void EndOuterUndoTask()
		{
			throw new System.NotImplementedException();
		}

		public void Rollback()
		{
			throw new System.NotImplementedException();
		}

		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			throw new System.NotImplementedException();
		}

		public void BeginNonUndoableTask()
		{
			throw new System.NotImplementedException();
		}

		public void EndNonUndoableTask()
		{
			throw new System.NotImplementedException();
		}

		public IActionHandler GetActionHandler()
		{
			return m_actionHandler;
		}

		public void SetActionHandler(IActionHandler _acth)
		{
			throw new System.NotImplementedException();
		}

		public void DeleteObj(int hvoObj)
		{
			throw new System.NotImplementedException();
		}

		public void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			throw new System.NotImplementedException();
		}

		public void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
		{
			throw new System.NotImplementedException();
		}

		public int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			throw new System.NotImplementedException();
		}

		public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd,
			int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			throw new System.NotImplementedException();
		}

		public void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst,
			int ihvoDstStart)
		{
			throw new System.NotImplementedException();
		}

		public void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			throw new System.NotImplementedException();
		}

		public void SetObjProp(int hvo, int tag, int hvoObj)
		{
			throw new System.NotImplementedException();
		}

		public void RemoveObjRefs(int hvo)
		{
			throw new System.NotImplementedException();
		}

		public void SetBinary(int hvo, int tag, byte[] _rgb, int cb)
		{
			throw new System.NotImplementedException();
		}

		public void SetGuid(int hvo, int tag, System.Guid uid)
		{
			throw new System.NotImplementedException();
		}

		public void SetInt(int hvo, int tag, int n)
		{
			throw new System.NotImplementedException();
		}

		public void SetInt64(int hvo, int tag, long lln)
		{
			throw new System.NotImplementedException();
		}

		public void SetBoolean(int hvo, int tag, bool n)
		{
			throw new System.NotImplementedException();
		}

		public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
		{
			throw new System.NotImplementedException();
		}

		public void SetString(int hvo, int tag, ITsString _tss)
		{
			throw new System.NotImplementedException();
		}

		public void SetTime(int hvo, int tag, long lln)
		{
			throw new System.NotImplementedException();
		}

		public void SetUnicode(int hvo, int tag, string _rgch, int cch)
		{
			throw new System.NotImplementedException();
		}

		public void SetUnknown(int hvo, int tag, object _unk)
		{
			throw new System.NotImplementedException();
		}

		public void AddNotification(IVwNotifyChange _nchng)
		{
			throw new System.NotImplementedException();
		}

		public void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin,
			int cvIns, int cvDel)
		{
			throw new System.NotImplementedException();
		}

		public void RemoveNotification(IVwNotifyChange _nchng)
		{
			throw new System.NotImplementedException();
		}

		public int GetDisplayIndex(int hvoOwn, int tag, int ihvo)
		{
			throw new System.NotImplementedException();
		}

		public int get_WritingSystemsOfInterest(int cwsMax, ArrayPtr _ws)
		{
			throw new System.NotImplementedException();
		}

		public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			throw new System.NotImplementedException();
		}

		public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			throw new System.NotImplementedException();
		}

		public string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			throw new System.NotImplementedException();
		}

		public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			throw new System.NotImplementedException();
		}

		public bool IsDirty()
		{
			throw new System.NotImplementedException();
		}

		public void ClearDirty()
		{
			throw new System.NotImplementedException();
		}

		public bool get_IsValidObject(int hvo)
		{
			throw new System.NotImplementedException();
		}

		public bool get_IsDummyId(int hvo)
		{
			throw new System.NotImplementedException();
		}

		public int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			throw new System.NotImplementedException();
		}

		public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			throw new System.NotImplementedException();
		}

		public void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim,
			int hvoDst, int flidDst, int wsDst, int ichDest, bool fDstIsNew)
		{
			throw new System.NotImplementedException();
		}

		public ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				throw new System.NotImplementedException();
			}
			set
			{
				throw new System.NotImplementedException();
			}
		}

		public IFwMetaDataCache MetaDataCache
		{
			get
			{
				throw new System.NotImplementedException();
			}
			set
			{
				throw new System.NotImplementedException();
			}
		}
		#endregion
	}

	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="reference only")]
	public class DummyRootBox : IVwRootBox
	{
		internal ISilDataAccess m_dummyDataAccess = new DummyDataAccess();
		internal DummyVwSelection m_dummySelection;
		internal SimpleRootSite m_dummySimpleRootSite;

		// current total text.
		public string Text = String.Empty;

		public DummyRootBox(SimpleRootSite srs)
		{
			m_dummySimpleRootSite = srs;
			m_dummySelection = new DummyVwSelection(this, 0, 0);
		}

		#region IVwRootBox implementation
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			throw new System.NotImplementedException();
		}

		public void SetSite(IVwRootSite _vrs)
		{
			throw new System.NotImplementedException();
		}

		public void SetRootObjects(int[] _rghvo, IVwViewConstructor[] _rgpvwvc, int[] _rgfrag,
			IVwStylesheet _ss, int chvo)
		{
			throw new System.NotImplementedException();
		}

		public void SetRootObject(int hvo, IVwViewConstructor _vwvc, int frag, IVwStylesheet _ss)
		{
			throw new System.NotImplementedException();
		}

		public void SetRootVariant(object v, IVwStylesheet _ss, IVwViewConstructor _vwvc, int frag)
		{
			throw new System.NotImplementedException();
		}

		public void SetRootString(ITsString _tss, IVwStylesheet _ss, IVwViewConstructor _vwvc,
			int frag)
		{
			throw new System.NotImplementedException();
		}

		public object GetRootVariant()
		{
			throw new System.NotImplementedException();
		}

		public void Serialize(System.Runtime.InteropServices.ComTypes.IStream _strm)
		{
			throw new System.NotImplementedException();
		}

		public void Deserialize(System.Runtime.InteropServices.ComTypes.IStream _strm)
		{
			throw new System.NotImplementedException();
		}

		public void WriteWpx(System.Runtime.InteropServices.ComTypes.IStream _strm)
		{
			throw new System.NotImplementedException();
		}

		public void DestroySelection()
		{
			throw new System.NotImplementedException();
		}

		public IVwSelection MakeTextSelection(int ihvoRoot, int cvlsi, SelLevInfo[] _rgvsli,
			int tagTextProp, int cpropPrevious, int ichAnchor, int ichEnd, int ws, bool fAssocPrev,
			int ihvoEnd, ITsTextProps _ttpIns, bool fInstall)
		{
			return new DummyVwSelection(this, ichAnchor, ichEnd);
		}

		public IVwSelection MakeRangeSelection(IVwSelection _selAnchor, IVwSelection _selEnd,
			bool fInstall)
		{
			m_dummySelection = new DummyVwSelection(this,
				(_selAnchor as DummyVwSelection).Anchor, (_selEnd as DummyVwSelection).End);
			return m_dummySelection;
		}

		public IVwSelection MakeSimpleSel(bool fInitial, bool fEdit, bool fRange, bool fInstall)
		{
			throw new System.NotImplementedException();
		}

		public IVwSelection MakeTextSelInObj(int ihvoRoot, int cvsli, SelLevInfo[] _rgvsli,
			int cvsliEnd, SelLevInfo[] _rgvsliEnd, bool fInitial, bool fEdit, bool fRange,
			bool fWholeObj, bool fInstall)
		{
			throw new System.NotImplementedException();
		}

		public IVwSelection MakeSelInObj(int ihvoRoot, int cvsli, SelLevInfo[] _rgvsli, int tag,
			bool fInstall)
		{
			throw new System.NotImplementedException();
		}

		public IVwSelection MakeSelAt(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst, bool fInstall)
		{
			throw new System.NotImplementedException();
		}

		public IVwSelection MakeSelInBox(IVwSelection _selInit, bool fEndPoint, int iLevel, int iBox,
			bool fInitial, bool fRange, bool fInstall)
		{
			throw new System.NotImplementedException();
		}

		public bool get_IsClickInText(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new System.NotImplementedException();
		}

		public bool get_IsClickInObject(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst, out int _odt)
		{
			throw new System.NotImplementedException();
		}

		public bool get_IsClickInOverlayTag(int xd, int yd, Utils.Rect rcSrc1, Utils.Rect rcDst1,
			out int _iGuid, out string _bstrGuids, out Utils.Rect _rcTag, out Utils.Rect _rcAllTags,
			out bool _fOpeningTag)
		{
			throw new System.NotImplementedException();
		}

		public void OnTyping(IVwGraphics _vg, string input, VwShiftStatus shiftStatus,
			ref int _wsPending)
		{
			const string BackSpace = "\b";

			if (input == BackSpace)
			{
				if (this.Text.Length <= 0)
					return;

				m_dummySelection.Anchor -= 1;
				m_dummySelection.End -= 1;
				this.Text = this.Text.Substring(0, this.Text.Length - 1);
				return;
			}

			var ws = m_dummySimpleRootSite.WritingSystemFactory.UserWs;
			m_dummySelection.ReplaceWithTsString(TsStringHelper.MakeTSS(input, ws));
		}

		public void DeleteRangeIfComplex(IVwGraphics _vg, out bool _fWasComplex)
		{
			_fWasComplex = false;
		}

		public void OnChar(int chw)
		{
			throw new System.NotImplementedException();
		}

		public void OnSysChar(int chw)
		{
			throw new System.NotImplementedException();
		}

		public int OnExtendedKey(int chw, VwShiftStatus ss, int nFlags)
		{
			throw new System.NotImplementedException();
		}

		public void FlashInsertionPoint()
		{
			throw new System.NotImplementedException();
		}

		public void MouseDown(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new System.NotImplementedException();
		}

		public void MouseDblClk(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new System.NotImplementedException();
		}

		public void MouseMoveDrag(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new System.NotImplementedException();
		}

		public void MouseDownExtended(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new System.NotImplementedException();
		}

		public void MouseUp(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new System.NotImplementedException();
		}

		public void Activate(VwSelectionState vss)
		{
			throw new System.NotImplementedException();
		}

		public VwPrepDrawResult PrepareToDraw(IVwGraphics _vg, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new System.NotImplementedException();
		}

		public void DrawRoot(IVwGraphics _vg, Utils.Rect rcSrc, Utils.Rect rcDst, bool fDrawSel)
		{
			throw new System.NotImplementedException();
		}

		public void Layout(IVwGraphics _vg, int dxsAvailWidth)
		{
			throw new System.NotImplementedException();
		}

		public void InitializePrinting(IVwPrintContext _vpc)
		{
			throw new System.NotImplementedException();
		}

		public int GetTotalPrintPages(IVwPrintContext _vpc)
		{
			throw new System.NotImplementedException();
		}

		public void PrintSinglePage(IVwPrintContext _vpc, int nPageNo)
		{
			throw new System.NotImplementedException();
		}

		public bool LoseFocus()
		{
			throw new System.NotImplementedException();
		}

		public void Close()
		{
			throw new System.NotImplementedException();
		}

		public void Reconstruct()
		{
			throw new System.NotImplementedException();
		}

		public void OnStylesheetChange()
		{
			throw new System.NotImplementedException();
		}

		public void DrawingErrors(IVwGraphics _vg)
		{
			throw new System.NotImplementedException();
		}

		public void SetTableColWidths(VwLength[] _rgvlen, int cvlen)
		{
			throw new System.NotImplementedException();
		}

		public bool IsDirty()
		{
			throw new System.NotImplementedException();
		}

		public void GetRootObject(out int _hvo, out IVwViewConstructor _pvwvc, out int _frag,
			out IVwStylesheet _pss)
		{
			throw new System.NotImplementedException();
		}

		public void DrawRoot2(IVwGraphics _vg, Utils.Rect rcSrc, Utils.Rect rcDst, bool fDrawSel,
			int ysTop, int dysHeight)
		{
			throw new System.NotImplementedException();
		}

		public void SetKeyboardForWs(ILgWritingSystem _ws, ref string _bstrActiveKeymanKbd,
			ref int _nActiveLangId, ref int _hklActive, ref bool _fSelectLangPending)
		{
			throw new System.NotImplementedException();
		}

		public bool DoSpellCheckStep()
		{
			throw new System.NotImplementedException();
		}

		public bool IsSpellCheckComplete()
		{
			throw new NotImplementedException();
		}

		public void RestartSpellChecking()
		{
			throw new System.NotImplementedException();
		}

		public ISilDataAccess DataAccess
		{
			get
			{
				return m_dummyDataAccess;
			}
			set
			{
				throw new System.NotImplementedException();
			}
		}

		public IVwOverlay Overlay
		{
			get
			{
				throw new System.NotImplementedException();
			}
			set
			{
				throw new System.NotImplementedException();
			}
		}

		public IVwSelection Selection
		{
			get
			{
				return m_dummySelection;
			}
		}

		public VwSelectionState SelectionState
		{
			get
			{
				throw new System.NotImplementedException();
			}
		}

		public int Height
		{
			get
			{
				return 0;
			}
		}

		public int Width
		{
			get
			{
				return 0;
			}
		}

		public IVwRootSite Site
		{
			get
			{
				return m_dummySimpleRootSite;
			}
		}

		public IVwStylesheet Stylesheet
		{
			get
			{
				throw new System.NotImplementedException();
			}
		}

		public int XdPos
		{
			get
			{
				throw new System.NotImplementedException();
			}
		}

		public IVwSynchronizer Synchronizer
		{
			get
			{
				throw new System.NotImplementedException();
			}
		}

		public int MaxParasToScan
		{
			get
			{
				throw new System.NotImplementedException();
			}
			set
			{
				throw new System.NotImplementedException();
			}
		}

		public bool IsCompositionInProgress
		{
			get
			{
				throw new System.NotImplementedException();
			}
		}

		public bool IsPropChangedInProgress
		{
			get
			{
				throw new System.NotImplementedException();
			}
		}
		#endregion
	}

	public class DummySimpleRootSite : SimpleRootSite
	{
		public DummySimpleRootSite()
		{
			m_rootb = new DummyRootBox(this);
			WritingSystemFactory = new PalasoWritingSystemManager();
		}

		public void Close()
		{
			m_rootb = null;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && !IsDisposed)
			{
				var disposable = WritingSystemFactory as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			WritingSystemFactory = null;
			base.Dispose(disposing);
		}

		protected override void CreateInputBusController()
		{
			// don't call base.
		}
	}

	/// <summary>
	/// Mock IBusCommunicator implementation that shows the latest charater as a preedit.
	/// Commits last char BEFORE showing the next preedit.
	/// </summary>
	public sealed class PreeditDummyIBusCommunicator : IIBusCommunicator
	{
		private string m_preedit = string.Empty;

		#region IIBusCommunicator implementation
		public event System.Action<string> CommitText;
		public event System.Action<string, uint, bool> UpdatePreeditText;
		public event System.Action HidePreeditText;
#pragma warning disable 67
		public event System.Action<uint, uint, uint> ForwardKeyEvent;

#pragma warning restore 67

		public void FocusIn()
		{
		}

		public void FocusOut()
		{
			Reset();
		}

		public bool ProcessKeyEvent(uint keyval, uint keycode, uint state)
		{
			const uint shift = 0x1;
			const uint capslock = 0x2;

			if (m_preedit != String.Empty)
				CommitText(m_preedit);

			m_preedit = ((char)keyval).ToString();
			if ((state & shift) != 0 || (state & capslock) != 0)
				m_preedit = m_preedit.ToUpper();
			UpdatePreeditText(m_preedit, 0, true);
			return true;
		}

		public void Reset()
		{
			m_preedit = String.Empty;
			HidePreeditText();
		}

		public void CreateInputContext(string name)
		{

		}

		public bool Connected
		{
			get
			{
				return true;
			}
		}

		public void Dispose()
		{

		}

		public void SetCursorLocation(int x, int y, int width, int height)
		{
		}
		#endregion
	}

	/// <summary>
	/// Mock IBusCommunicatior implementation. Typing is performed in a preedit. Upon pressing
	/// Space, the preedit is committed all at once (and in upper case).
	/// (cf PreeditDummyIBusCommunicator which commits each keystroke separately.)
	/// </summary>
	public class KeyboardThatCommitsPreeditOnSpace : IIBusCommunicator
	{
		protected string m_preedit = string.Empty;

		protected char ToggleCase(char input)
		{
			if (char.IsLower(input))
				return char.ToUpperInvariant(input);
			return char.ToLowerInvariant(input);
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~KeyboardThatCommitsPreeditOnSpace()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects

			}
			IsDisposed = true;
		}
		#endregion
		#region IIBusCommunicator implementation
		public event Action<string> CommitText;
		public event Action<string, uint, bool> UpdatePreeditText;
#pragma warning disable 67
		public event Action HidePreeditText;
		public event Action<uint, uint, uint> ForwardKeyEvent;

#pragma warning restore 67

		public void FocusIn()
		{
			throw new NotImplementedException();
		}

		public void FocusOut()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Commit on space. Otherwise append to preedit.
		/// </summary>
		public virtual bool ProcessKeyEvent(uint keyval, uint keycode, uint state)
		{
			const uint shift = 0x1;
			const uint capslock = 0x2;

			var input = (char)keyval;

			if (input == ' ')
			{
				Commit(input);
				return true;
			}

			if ((state & shift) != 0)
				input = ToggleCase(input);
			if ((state & capslock) != 0)
				input = ToggleCase(input);

			m_preedit += input;
			CallUpdatePreeditText(m_preedit, 0, true);

			return true;
		}

		public void Reset()
		{
			throw new NotImplementedException();
		}

		public void CreateInputContext(string name)
		{
		}

		public bool Connected
		{
			get
			{
				return true;
			}
		}

		#endregion

		protected void CallCommitText(string text)
		{
			CommitText(text);
		}

		protected void CallUpdatePreeditText(string text, uint cursor_pos, bool visible)
		{
			UpdatePreeditText(text, cursor_pos, visible);
		}

		protected virtual void Commit(char lastCharacterTyped)
		{
			CallUpdatePreeditText(string.Empty, 0, true);
			CallCommitText(m_preedit.ToUpperInvariant());
			m_preedit = string.Empty;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void SetCursorLocation(int x, int y, int width, int height)
		{
		}
	}

	/// <summary>
	/// Mock IBusCommunicatior implementation. Typing is performed in a preedit. Upon pressing
	/// Space, text is committed.
	/// "abc def ghi " becomes "ADG".
	/// Similar to KeyboardThatCommitsPreeditOnSpace.
	/// </summary>
	public sealed class KeyboardWithGlyphSubstitution : KeyboardThatCommitsPreeditOnSpace
	{
		protected override void Commit(char lastCharacterTyped)
		{
			if (m_preedit == string.Empty)
			{
				CallCommitText(lastCharacterTyped.ToString());
			}
			else
			{
				CallUpdatePreeditText(string.Empty, 0, true);
				CallCommitText(m_preedit[0].ToString().ToUpperInvariant());
				m_preedit = string.Empty;
			}
		}
	}

	/// <summary>
	/// Mock IBusCommunicator implementation that just echos back any sent
	/// keypresses.(Doesn't show preedit)
	/// </summary>
	public sealed class NoPreeditDummyIBusCommunicator : IIBusCommunicator
	{
		#region IIBusCommunicator implementation
		public event System.Action<string> CommitText;
#pragma warning disable 67
		public event System.Action<string, uint, bool> UpdatePreeditText;
		public event System.Action HidePreeditText;
		public event System.Action<uint, uint, uint> ForwardKeyEvent;
#pragma warning restore 67

		public void FocusIn()
		{
			throw new System.NotImplementedException();
		}

		public void FocusOut()
		{
			throw new System.NotImplementedException();
		}

		public bool ProcessKeyEvent(uint keyval, uint keycode, uint state)
		{
			const uint shift = 0x1;
			const uint capslock = 0x2;

			// ignore backspace
			if (keyval == 0xff08)
				return true;

			string str = ((char)keyval).ToString();
			if ((state & shift) != 0 || (state & capslock) != 0)
				str = str.ToUpper();
			CommitText(str);
			return true;
		}

		public void Reset()
		{
			throw new System.NotImplementedException();
		}

		public void CreateInputContext(string name)
		{

		}

		public bool Connected
		{
			get
			{
				return true;
			}
		}

		public void Dispose()
		{

		}

		public void SetCursorLocation(int x, int y, int width, int height)
		{
		}
	#endregion
	}

	/// <summary>
	/// Mock IBusCommunicator implementation that deletes current word when space is pressed,
	/// by sending backspaces. It then resends the word in lower case.
	/// </summary>
	public sealed class KeyboardThatSendsDeletesAsCommitsDummyIBusCommunicator : IIBusCommunicator
	{
		private string buffer = string.Empty;

		#region IIBusCommunicator implementation
		public event System.Action<string> CommitText;
#pragma warning disable 67
		public event System.Action<string, uint, bool> UpdatePreeditText;
		public event System.Action HidePreeditText;
		public event System.Action<uint, uint, uint> ForwardKeyEvent;

#pragma warning restore 67

		public void FocusIn()
		{
			throw new System.NotImplementedException();
		}

		public void FocusOut()
		{
			throw new System.NotImplementedException();
		}

		public bool ProcessKeyEvent(uint keyval, uint keycode, uint state)
		{
			const uint shift = 0x1;
			const uint capslock = 0x2;

			// if space.
			if (keyval == (uint)' ') // 0x0020
			{
				foreach (char c in buffer)
				{
					CommitText("\b"); // 0x0008
				}

				foreach (char c in buffer.ToLowerInvariant())
				{
					CommitText(c.ToString());
				}

				buffer = String.Empty;
				return true;
			}
			string str = ((char)keyval).ToString();
			if ((state & shift) != 0 || (state & capslock) != 0)
				str = str.ToUpper();
			buffer += str;
			CommitText(str);
			return true;
		}

		public void Reset()
		{
			throw new System.NotImplementedException();
		}

		public void CreateInputContext(string name)
		{

		}

		public bool Connected
		{
			get
			{
				return true;
			}
		}

		public void Dispose()
		{

		}

		public void SetCursorLocation(int x, int y, int width, int height)
		{
		}
		#endregion
	}

	/// <summary>
	/// Mock IBusCommunicator implementation that deletes current word when space is pressed,
	/// by sending backspaces as ForwardKeyEvents. It then resends the word in lower case.
	/// </summary>
	public sealed class KeyboardThatSendsBackspacesAsForwardKeyEvents : IIBusCommunicator
	{
		private string buffer = string.Empty;

		#region IIBusCommunicator implementation
		public event System.Action<string> CommitText;
#pragma warning disable 67
		public event System.Action<string, uint, bool> UpdatePreeditText;
		public event System.Action HidePreeditText;
#pragma warning restore 67

		public event System.Action<uint, uint, uint> ForwardKeyEvent;

		public void FocusIn()
		{
			throw new System.NotImplementedException();
		}

		public void FocusOut()
		{
			throw new System.NotImplementedException();
		}

		public bool ProcessKeyEvent(uint keyval, uint keycode, uint state)
		{
			const uint shift = 0x1;
			const uint capslock = 0x2;

			// if space.
			if (keyval == (uint)' ') // 0x0020
			{
				foreach (char c in buffer)
				{
					uint mysteryValue = 22;
					ForwardKeyEvent(0xFF00 | '\b', mysteryValue, 0); // 0x0008
				}

				foreach (char c in buffer.ToLowerInvariant())
				{
					CommitText(c.ToString());
				}

				buffer = String.Empty;
				return true;
			}
			string str = ((char)keyval).ToString();
			if ((state & shift) != 0 || (state & capslock) != 0)
				str = str.ToUpper();
			buffer += str;
			CommitText(str);
			return true;
		}

		public void Reset()
		{
			throw new System.NotImplementedException();
		}

		public void CreateInputContext(string name)
		{

		}

		public bool Connected
		{
			get
			{
				return true;
			}
		}

		public void Dispose()
		{

		}

		public void SetCursorLocation(int x, int y, int width, int height)
		{
		}
		#endregion
	}

	#endregion
}
