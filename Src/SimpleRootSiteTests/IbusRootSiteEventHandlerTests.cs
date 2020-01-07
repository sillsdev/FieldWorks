// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using IBusDotNet;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.Keyboarding;
using SIL.LCModel.Core.WritingSystems;
using SIL.Windows.Forms.Keyboarding;
using SIL.Windows.Forms.Keyboarding.Linux;
using X11.XKlavier;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Tests for InputBusController
	/// </summary>
	[TestFixture]
	[Platform(Include = "Linux", Reason = "IbusRootSiteEventHandlerTests is Linux only")]
	public class IbusRootSiteEventHandlerTests
	{
		// some lparam values representing keypress that we use for testing.
		private static readonly Dictionary<char, int> lparams = new Dictionary<char, int>();
		private DummySimpleRootSite m_dummySimpleRootSite;
		private ITestableIbusCommunicator m_dummyIBusCommunicator;

		/// <summary />
		static IbusRootSiteEventHandlerTests()
		{
			lparams.Add('A', 0x40260001);
			lparams.Add('B', 0x40380001);
			lparams.Add('C', 0x40360001);
			lparams.Add('D', 0x40280001);
			lparams.Add('E', 0x401A0001);
			lparams.Add('F', 0x40290001);
			lparams.Add('I', 0x401F0001);
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

		/// <summary />
		[SetUp]
		public virtual void TestSetup()
		{
			m_dummySimpleRootSite = new DummySimpleRootSite();
			Assert.NotNull(m_dummySimpleRootSite.RootBox);
			Keyboard.Controller = new DefaultKeyboardController();
		}

		[TearDown]
		public void TestTearDown()
		{
			KeyboardController.UnregisterControl(m_dummySimpleRootSite);
			KeyboardController.Shutdown();
			Keyboard.Controller = new DefaultKeyboardController();

			m_dummyIBusCommunicator?.Dispose();
			m_dummySimpleRootSite.Visible = false;
			m_dummySimpleRootSite.Dispose();
			m_dummyIBusCommunicator = null;
			m_dummySimpleRootSite = null;
		}

		private void ChooseSimulatedKeyboard(ITestableIbusCommunicator ibusCommunicator)
		{
			m_dummyIBusCommunicator = ibusCommunicator;
			var ibusKeyboardRetrievingAdaptor = new IbusKeyboardRetrievingAdaptorDouble(ibusCommunicator);
			var xklEngineMock = MockRepository.GenerateStub<IXklEngine>();
			var xkbKeyboardRetrievingAdaptor = new XkbKeyboardRetrievingAdaptorDouble(xklEngineMock);
			KeyboardController.Initialize(xkbKeyboardRetrievingAdaptor, ibusKeyboardRetrievingAdaptor);
			KeyboardController.RegisterControl(m_dummySimpleRootSite, new IbusRootSiteEventHandler(m_dummySimpleRootSite));
		}

		/// <summary>Simulate multiple keypresses.</summary>
		[TestCase(typeof(CommitOnlyIbusCommunicator),
			/* input: */new[] { "\b" },
			/* expected: */"", "", 0, 0, TestName = "EmptyStateSendSingleControlCharacter_SelectionIsInsertionPoint")]
		[TestCase(typeof(CommitOnlyIbusCommunicator),
			/* input: */ new[] { "T" },
			/* expected: */ "T", "", 1, 1, TestName = "EmptyStateSendSingleKeyPress_SelectionIsInsertionPoint")]
		[TestCase(typeof(CommitOnlyIbusCommunicator),
			/* input: */ new[] { "T", "U" },
			/* expected: */ "TU", "", 2, 2, TestName = "EmptyStateSendTwoKeyPresses_SelectionIsInsertionPoint")]
		[TestCase(typeof(KeyboardThatSendsDeletesAsCommitsDummyIBusCommunicator),
			/* input: */ new[] { "S", "T", "U", " " },
			/* expected: */ "stu", "", 3, 3, TestName = "KeyboardThatSendsBackspacesInItsCommits_BackspacesShouldNotBeIngored")]
		[TestCase(typeof(KeyboardThatSendsBackspacesAsForwardKeyEvents),
			/* input: */ new[] { "S", "T", "U", " " },
			/* expected: */ "stu", "", 3, 3, TestName = "KeyboardThatSendsBackspacesInItsForwardKeyEvent_BackspacesShouldNotBeIngored")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */new[] { "t" },
			/* expected: */"t", "t", 1, 1, TestName = "OneCharNoSpace_PreeditContainsChar")]
		[TestCase(typeof(CommitBeforeUpdateIbusCommunicator),
			/* input: */ new[] { "T" },
			/* expected: */ "T", "T", 1, 1, TestName = "SimplePreeditEmptyStateSendSingleKeyPress")]
		[TestCase(typeof(CommitBeforeUpdateIbusCommunicator),
			/* input: */ new[] { "S", "T", "U" },
			/* expected: */ "STU", "U", 3, 3, TestName = "SimplePreeditEmptyStateSendThreeKeyPresses")]
		[TestCase(typeof(CommitBeforeUpdateIbusCommunicator),
			/* input: */ new[] { "T", "U" },
			/* expected: */ "TU", "U", 2, 2, TestName = "SimplePreeditEmptyStateSendTwoKeyPresses")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] { " " },
			/* expected: */ " ", "", 1, 1, TestName = "Space_JustAddsToDocument")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] { "t", "u" },
			/* expected: */ "tu", "tu", 2, 2, TestName = "TwoChars_OnlyPreedit")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */ new[] { "t", "u", /*commit:*/" " },
			/* expected: */ "TU", "", 2, 2, TestName = "TwoCharsAndSpace_PreeditIsCommitted")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */ new[] { "t", "u" },
			/* expected: */ "tu", "tu", 2, 2, TestName = "TwoCharsNoSpace_PreeditContainsChars")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] { "t", "u", /*commit*/" " },
			/* expected: */ "T", "", 1, 1, TestName = "TwoCharsSpace_SubstitutionWorkedAndPreeditIsEmpty")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */ new[] { "t", "u", /*commit:*/" ", "s", "u", /* commit*/" " },
			/* expected: */ "TUSU", "", 4, 4, TestName = "TwoCharsSpaceTwoChars_PreeditIsEmpty")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */ new[] { "t", "u", /*commit:*/" ", "s", "u" /* don't commit*/},
			/* expected: */ "TUsu", "su", 4, 4, TestName = "TwoCharsSpaceTwoChars_PreeditIsLastHalf")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] { "t", "u", /*commit*/" ", "s", "u" /*don't commit*/},
			/* expected: */ "Tsu", "su", 3, 3, TestName = "TwoCharsSpaceTwoChars_SubstitutionWorkedAndPreeditIsLastHalf")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] { "t", "u", /*commit*/" ", "s", "u", /*commit*/" " },
			/* expected: */ "TS", "", 2, 2, TestName = "TwoCharsSpaceTwoCharsSpace_SubstitutionWorkedAndPreeditIsEmpty")]
		public void SimulateKeypress(Type ibusCommunicator, string[] keys, string expectedDocument, string expectedSelectionText, int expectedAnchor, int expectedEnd)
		{
			// Setup
			ChooseSimulatedKeyboard(Activator.CreateInstance(ibusCommunicator, true) as ITestableIbusCommunicator);

			// Exercise
			foreach (var key in keys)
			{
				m_dummyIBusCommunicator.ProcessKeyEvent(key[0], lparams[key.ToUpper()[0]], char.IsUpper(key[0]) ? Keys.Shift : Keys.None);
			}

			// Verify
			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual(expectedDocument, dummyRootBox.Text, "RootSite text");
			Assert.AreEqual(expectedSelectionText, m_dummyIBusCommunicator.PreEdit, "Preedit text");
			Assert.AreEqual(expectedAnchor, dummySelection.Anchor, "Selection anchor");
			Assert.AreEqual(expectedEnd, dummySelection.End, "Selection end");
		}

		/// <summary />
		[Test]
		public void KillFocus_ShowingPreedit_PreeditIsNotCommitedAndSelectionIsInsertionPoint()
		{
			ChooseSimulatedKeyboard(new CommitBeforeUpdateIbusCommunicator());

			m_dummyIBusCommunicator.ProcessKeyEvent('T', lparams['T'], Keys.Shift);

			m_dummyIBusCommunicator.FocusOut();

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual(string.Empty, dummyRootBox.Text);

			Assert.AreEqual(string.Empty, m_dummyIBusCommunicator.PreEdit);
			Assert.AreEqual(0, dummySelection.Anchor);
			Assert.AreEqual(0, dummySelection.End);
		}

		/// <summary />
		[Test]
		public void Focus_Unfocused_KeypressAcceptedAsNormal()
		{
			ChooseSimulatedKeyboard(new CommitBeforeUpdateIbusCommunicator());

			m_dummyIBusCommunicator.ProcessKeyEvent('S', lparams['S'], Keys.Shift);

			m_dummyIBusCommunicator.FocusOut();

			m_dummyIBusCommunicator.FocusIn();

			m_dummyIBusCommunicator.ProcessKeyEvent('T', lparams['T'], Keys.Shift);

			m_dummyIBusCommunicator.ProcessKeyEvent('U', lparams['U'], Keys.Shift);

			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual("TU", dummyRootBox.Text, "Rootbox text");

			Assert.AreEqual("U", m_dummyIBusCommunicator.PreEdit, "pre-edit text");
			Assert.AreEqual(2, dummySelection.Anchor, "Selection anchor");
			Assert.AreEqual(2, dummySelection.End, "Selection end");
		}

		/// <summary>Test cases for FWNX-674</summary>
		[Test]
		[TestCase(1, 2, TestName = "ReplaceForwardSelectedChar_Replaced")]
		[TestCase(2, 1, TestName = "ReplaceBackwardSelectedChar_Replaced")]
		public void CorrectPlacementOfTypedChars(int anchor, int end)
		{
			// Setup
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());
			((DummyRootBox)m_dummySimpleRootSite.RootBox).Text = "ABC";

			// Select B
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;
			preedit.Anchor = anchor;
			preedit.End = end;

			// Exercise
			m_dummyIBusCommunicator.ProcessKeyEvent('d', lparams['D'], Keys.None);
			m_dummyIBusCommunicator.ProcessKeyEvent('d', lparams['D'], Keys.None);
			// Commit by pressing space
			m_dummyIBusCommunicator.ProcessKeyEvent(' ', lparams[' '], Keys.None);

			// Verify
			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;
			Assert.That(document.Text, Is.EqualTo("ADC"));
			Assert.That(m_dummyIBusCommunicator.PreEdit, Is.EqualTo(string.Empty));
			Assert.That(preedit.Anchor, Is.EqualTo(2));
			Assert.That(preedit.End, Is.EqualTo(2));
		}

		/// <summary>Test case for FWNX-1305</summary>
		[Test]
		public void HandleNullActionHandler()
		{
			// Setup
			m_dummySimpleRootSite.DataAccess.SetActionHandler(null);
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());
			((DummyRootBox)m_dummySimpleRootSite.RootBox).Text = "ABC";

			// Select A
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;
			preedit.Anchor = 1;
			preedit.End = 0;

			// Exercise
			m_dummyIBusCommunicator.ProcessKeyEvent('d', lparams['D'], Keys.None);
			m_dummyIBusCommunicator.ProcessKeyEvent('d', lparams['D'], Keys.None);
			// Commit by pressing space
			m_dummyIBusCommunicator.ProcessKeyEvent(' ', lparams[' '], Keys.None);

			// Verify
			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;
			Assert.That(document.Text, Is.EqualTo("DBC"));
			Assert.That(m_dummyIBusCommunicator.PreEdit, Is.EqualTo(string.Empty));
			Assert.That(preedit.Anchor, Is.EqualTo(1));
			Assert.That(preedit.End, Is.EqualTo(1));
		}

		private void PressKeys(string input)
		{
			foreach (var c in input)
			{
				m_dummyIBusCommunicator.ProcessKeyEvent(c, lparams[c.ToString().ToUpper()[0]], Keys.None);
			}
		}

		[Test]
		[TestCase("d", 1, 2, "ABdC", "d", 1, 2, TestName = "OneKey_ForwardSelection_PreeditPlacedAfter")]
		[TestCase("d", 2, 1, "AdBC", "d", 3, 2, TestName = "OneKey_BackwardSelection_PreeditPlacedBefore")]
		[TestCase("dd", 1, 2, "ABddC", "dd", 1, 2, TestName = "TwoKeys_ForwardSelection_PreeditPlacedAfter")]
		[TestCase("dd", 2, 1, "AddBC", "dd", 4, 3, TestName = "TwoKeys_BackwardSelection_PreeditPlacedBefore")]
		[TestCase("dd", 2, 3, "ABCdd", "dd", 2, 3, TestName = "TwoKeysEnd_ForwardSelection_PreeditPlacedAfter")]
		[TestCase("dd", 3, 2, "ABddC", "dd", 5, 4, TestName = "TwoKeysEnd_BackwardSelection_PreeditPlacedBefore")]
		[TestCase("dd ", 2, 3, "ABD", "", 3, 3, TestName = "Commit_ForwardSelection_IPAfter")]
		[TestCase("dd ", 3, 2, "ABD", "", 3, 3, TestName = "Commit_BackwardSelection_IPAfter")]
		public void CorrectPlacementOfPreedit(string input, int anchor, int end, string expectedText, string expectedPreedit, int expectedAnchor, int expectedEnd)
		{
			// Setup
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());
			((DummyRootBox)m_dummySimpleRootSite.RootBox).Text = "ABC";

			// Make range selection from anchor to end
			var preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;
			preedit.Anchor = anchor;
			preedit.End = end;

			// Exercise
			PressKeys(input);

			// Verify
			var document = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			preedit = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;
			Assert.That(document.Text, Is.EqualTo(expectedText));
			Assert.That(m_dummyIBusCommunicator.PreEdit, Is.EqualTo(expectedPreedit));
			Assert.That(preedit.Anchor, Is.EqualTo(expectedAnchor), "Anchor");
			Assert.That(preedit.End, Is.EqualTo(expectedEnd), "End");
		}

		private sealed class DummySimpleRootSite : SimpleRootSite
		{
			public DummySimpleRootSite()
			{
				RootBox = new DummyRootBox(this);
				WritingSystemFactory = new WritingSystemManager();
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing && !IsDisposed)
				{
					var disposable = WritingSystemFactory as IDisposable;
					disposable?.Dispose();
				}
				WritingSystemFactory = null;
				base.Dispose(disposing);
			}

			public override bool Focused => true;
		}

		/// <summary>
		/// Mock IBusCommunicator implementation that just echos back any sent
		/// keypresses.(Doesn't show preedit)
		/// </summary>
		private sealed class CommitOnlyIbusCommunicator : ITestableIbusCommunicator
		{
			#region IIbusCommunicator implementation
			public event Action<object> CommitText;
#pragma warning disable 67
			public event Action<object, int> UpdatePreeditText;
			public event Action<int, int> DeleteSurroundingText;
			public event Action HidePreeditText;
			public event Action<int, int, int> KeyEvent;
#pragma warning restore 67

			~CommitOnlyIbusCommunicator()
			{
				Dispose(false);
			}

			public bool IsDisposed { get; private set; }

			public IBusConnection Connection
			{
				get { throw new NotSupportedException(); }
			}

			public void FocusIn()
			{
				// nothing we need to do
			}

			public void FocusOut()
			{
				// nothing we need to do
			}

			public bool ProcessKeyEvent(int keySym, int scanCode, Keys state)
			{
				const uint shift = 0x1;
				const uint capslock = 0x2;

				// ignore backspace
				if (keySym == 0xff08)
				{
					return true;
				}
				var str = ((char)keySym).ToString();
				if (((uint)state & shift) != 0 || ((uint)state & capslock) != 0)
				{
					str = str.ToUpper();
				}
				CommitText?.Invoke(new IBusText(str));
				return true;
			}

			public void Reset()
			{
				// nothing we need to do
			}

			public void CreateInputContext()
			{
			}

			public bool Connected => true;

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool fDisposing)
			{
				Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
				IsDisposed = true;
			}

			public void NotifySelectionLocationAndHeight(int x, int y, int height)
			{
			}
			#endregion

			public string PreEdit => string.Empty;
		}

		/// <summary>
		/// Mock IBusCommunicator implementation that shows the latest character as a preedit.
		/// Commits last char BEFORE showing the next preedit.
		/// </summary>
		private sealed class CommitBeforeUpdateIbusCommunicator : ITestableIbusCommunicator
		{

			#region IIbusCommunicator implementation
			public event Action<object> CommitText;
			public event Action<object, int> UpdatePreeditText;
			public event Action HidePreeditText;
#pragma warning disable 67
			public event Action<int, int> DeleteSurroundingText;
			public event Action<int, int, int> KeyEvent;
#pragma warning restore 67

			~CommitBeforeUpdateIbusCommunicator()
			{
				Dispose(false);
			}

			public bool IsDisposed { get; private set; }

			public IBusConnection Connection
			{
				get { throw new NotSupportedException(); }
			}

			public void FocusIn()
			{
			}

			public void FocusOut()
			{
				Reset();
			}

			public bool ProcessKeyEvent(int keySym, int scanCode, Keys state)
			{
				const uint shift = 0x1;
				const uint capslock = 0x2;

				if (PreEdit != string.Empty)
				{
					// Delete the pre-edit first. This is necessary because we use a no-op action
					// handler, so the rollback doesn't do anything.
					UpdatePreeditText(new IBusText(string.Empty), 0);
					CommitText(new IBusText(PreEdit));
				}

				PreEdit = ((char)keySym).ToString();
				if (((uint)state & shift) != 0 || ((uint)state & capslock) != 0)
				{
					PreEdit = PreEdit.ToUpper();
				}
				UpdatePreeditText(new IBusText(PreEdit), PreEdit.Length);
				return true;
			}

			public void Reset()
			{
				PreEdit = string.Empty;
				// Delete the pre-edit first. This is necessary because we use a no-op action
				// handler, so the rollback doesn't do anything.
				UpdatePreeditText?.Invoke(new IBusText(String.Empty), 0);
				HidePreeditText?.Invoke();
			}

			public void CreateInputContext()
			{
			}

			public bool Connected => true;

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool fDisposing)
			{
				Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
				IsDisposed = true;
			}

			public void NotifySelectionLocationAndHeight(int x, int y, int height)
			{
			}
			#endregion

			public string PreEdit { get; private set; } = string.Empty;
		}

		private interface ITestableIbusCommunicator : IIbusCommunicator
		{
			string PreEdit { get; }
		}

		/// <summary>
		/// Mock IBusCommunicator implementation. Typing is performed in a preedit. Upon pressing
		/// Space, the preedit is committed all at once (and in upper case).
		/// (cf PreeditDummyIBusCommunicator which commits each keystroke separately.)
		/// </summary>
		private class KeyboardThatCommitsPreeditOnSpace : ITestableIbusCommunicator
		{
			protected KeyboardThatCommitsPreeditOnSpace()
			{
			}

			private char ToggleCase(char input)
			{
				return char.IsLower(input) ? char.ToUpperInvariant(input) : char.ToLowerInvariant(input);
			}

			#region Disposable stuff
			/// <summary/>
			~KeyboardThatCommitsPreeditOnSpace()
			{
				Dispose(false);
			}

			/// <summary />
			public bool IsDisposed { get; private set; }

			/// <summary />
			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". *******");
				if (IsDisposed)
				{
					// No need to run it more than once.
					return;
				}
				if (disposing)
				{
					// dispose managed objects
				}
				IsDisposed = true;
			}
			#endregion
			#region IIbusCommunicator implementation
			public event Action<object> CommitText;
			public event Action<object, int> UpdatePreeditText;
#pragma warning disable 67
			public event Action<int, int> DeleteSurroundingText;
			public event Action HidePreeditText;
			public event Action<int, int, int> KeyEvent;
#pragma warning restore 67

			public IBusConnection Connection
			{
				get { throw new NotSupportedException(); }
			}

			public void FocusIn()
			{
				// nothing we need to do.
			}

			public void FocusOut()
			{
				// nothing we need to do.
			}

			/// <summary>
			/// Commit on space. Otherwise append to preedit.
			/// </summary>
			public virtual bool ProcessKeyEvent(int keySym, int scanCode, Keys state)
			{
				const uint shift = 0x1;
				const uint capslock = 0x2;
				var input = (char)keySym;
				if (input == ' ')
				{
					Commit(input);
					return true;
				}
				if (((uint)state & shift) != 0)
				{
					input = ToggleCase(input);
				}
				if (((uint)state & capslock) != 0)
				{
					input = ToggleCase(input);
				}
				PreEdit += input;
				CallUpdatePreeditText(PreEdit, PreEdit.Length);

				return true;
			}

			public void Reset()
			{
				// nothing we need to do
			}

			public void CreateInputContext()
			{
			}

			public bool Connected => true;
			#endregion

			public string PreEdit { get; protected set; } = string.Empty;

			protected void CallCommitText(string text)
			{
				CallUpdatePreeditText(string.Empty, 0);
				CommitText(new IBusText(text));
			}

			protected void CallUpdatePreeditText(string text, int cursor_pos)
			{
				UpdatePreeditText(new IBusText(text), cursor_pos);
			}

			protected virtual void Commit(char lastCharacterTyped)
			{
				CallCommitText(PreEdit.ToUpperInvariant());
				PreEdit = string.Empty;
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			public void NotifySelectionLocationAndHeight(int x, int y, int height)
			{
			}
		}

		/// <summary>
		/// Mock IBusCommunicator implementation. Typing is performed in a preedit. Upon pressing
		/// Space, text is committed.
		/// "abc def ghi " becomes "ADG".
		/// Similar to KeyboardThatCommitsPreeditOnSpace.
		/// </summary>
		private sealed class KeyboardWithGlyphSubstitution : KeyboardThatCommitsPreeditOnSpace
		{
			protected override void Commit(char lastCharacterTyped)
			{
				if (PreEdit == string.Empty)
				{
					CallCommitText(lastCharacterTyped.ToString());
				}
				else
				{
					CallUpdatePreeditText(string.Empty, 0);
					CallCommitText(PreEdit[0].ToString().ToUpperInvariant());
					PreEdit = string.Empty;
				}
			}
		}

		/// <summary>
		/// Mock IBusCommunicator implementation that deletes current word when space is pressed,
		/// by sending backspaces. It then resends the word in lower case.
		/// </summary>
		public sealed class KeyboardThatSendsDeletesAsCommitsDummyIBusCommunicator : ITestableIbusCommunicator
		{
			private string buffer = string.Empty;

			#region IIbusCommunicator implementation
			public event Action<object> CommitText;
#pragma warning disable 67
			public event Action<object, int> UpdatePreeditText;
			public event Action<int, int> DeleteSurroundingText;
			public event Action HidePreeditText;
			public event Action<int, int, int> KeyEvent;
#pragma warning restore 67

			~KeyboardThatSendsDeletesAsCommitsDummyIBusCommunicator()
			{
				Dispose(false);
			}

			public bool IsDisposed { get; private set; }

			public IBusConnection Connection
			{
				get { throw new NotSupportedException(); }
			}

			public void FocusIn()
			{
				// nothing we need to do
			}

			public void FocusOut()
			{
				// nothing we need to do
			}

			public bool ProcessKeyEvent(int keySym, int scanCode, Keys state)
			{
				const uint shift = 0x1;
				const uint capslock = 0x2;
				// if space.
				if (keySym == (uint)' ') // 0x0020
				{
					foreach (char c in buffer)
					{
						CommitText(new IBusText("\b")); // 0x0008
					}

					foreach (char c in buffer.ToLowerInvariant())
					{
						CommitText(new IBusText(c.ToString()));
					}

					buffer = string.Empty;
					return true;
				}
				var str = ((char)keySym).ToString();
				if (((uint)state & shift) != 0 || ((uint)state & capslock) != 0)
				{
					str = str.ToUpper();
				}
				buffer += str;
				CommitText(new IBusText(str));
				return true;
			}

			public void Reset()
			{
				// nothing we need to do
			}

			public void CreateInputContext()
			{
			}

			public bool Connected => true;

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool fDisposing)
			{
				Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
				IsDisposed = true;
			}

			public void NotifySelectionLocationAndHeight(int x, int y, int height)
			{
			}
			#endregion

			public string PreEdit => string.Empty;
		}

		/// <summary>
		/// Mock IBusCommunicator implementation that deletes current word when space is pressed,
		/// by sending backspaces as ForwardKeyEvents. It then resends the word in lower case.
		/// </summary>
		private sealed class KeyboardThatSendsBackspacesAsForwardKeyEvents : ITestableIbusCommunicator
		{
			private string buffer = string.Empty;

			#region IIbusCommunicator implementation
			public event Action<object> CommitText;
#pragma warning disable 67
			public event Action<object, int> UpdatePreeditText;
			public event Action<int, int> DeleteSurroundingText;
			public event Action HidePreeditText;
#pragma warning restore 67

			~KeyboardThatSendsBackspacesAsForwardKeyEvents()
			{
				Dispose(false);
			}

			public event Action<int, int, int> KeyEvent;

			public bool IsDisposed { get; private set; }

			public IBusConnection Connection
			{
				get { throw new NotSupportedException(); }
			}

			public void FocusIn()
			{
				// nothing we need to do
			}

			public void FocusOut()
			{
				// nothing we need to do
			}

			public bool ProcessKeyEvent(int keySym, int scanCode, Keys state)
			{
				const uint shift = 0x1;
				const uint capslock = 0x2;
				// if space.
				if (keySym == (uint)' ') // 0x0020
				{
					foreach (var c in buffer)
					{
						var mysteryValue = 22;
						KeyEvent(0xFF00 | '\b', mysteryValue, 0); // 0x0008
					}
					foreach (var c in buffer.ToLowerInvariant())
					{
						CommitText(new IBusText(c.ToString()));
					}
					buffer = string.Empty;
					return true;
				}
				var str = ((char)keySym).ToString();
				if (((uint)state & shift) != 0 || ((uint)state & capslock) != 0)
				{
					str = str.ToUpper();
				}
				buffer += str;
				CommitText(new IBusText(str));
				return true;
			}

			public void Reset()
			{
				// nothing we need to do
			}

			public void CreateInputContext()
			{
			}

			public bool Connected => true;

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			private void Dispose(bool fDisposing)
			{
				Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
				IsDisposed = true;
			}

			public void NotifySelectionLocationAndHeight(int x, int y, int height)
			{
			}
			#endregion

			public string PreEdit => string.Empty;
		}

		private sealed class XkbKeyboardRetrievingAdaptorDouble : XkbKeyboardRetrievingAdaptor
		{
			public XkbKeyboardRetrievingAdaptorDouble(IXklEngine engine) : base(engine)
			{
			}

			protected override void InitLocales()
			{
			}
		}

		private sealed class IbusKeyboardRetrievingAdaptorDouble : IbusKeyboardRetrievingAdaptor
		{
			public IbusKeyboardRetrievingAdaptorDouble(IIbusCommunicator ibusCommunicator) : base(ibusCommunicator)
			{
			}

			protected override void InitKeyboards()
			{
			}

			public override bool IsApplicable => true;
		}
	}
}