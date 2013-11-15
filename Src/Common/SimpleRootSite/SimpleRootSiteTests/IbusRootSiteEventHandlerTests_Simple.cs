// Copyright (c) 2013, SIL International.
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
#if __MonoCS__
using System;
using System.Drawing;
using NUnit.Framework;
using IBusDotNet;
using Palaso.UI.WindowsForms.Keyboarding;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils.Attributes;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Simple tests for IbusRootSiteEventHandler. This tests are similar to Palaso's
	/// IbusDefaultEventHandlerTests but use a SimpleRootSite instead of a TextBox.
	/// </summary>
	/// <remarks>Note that we have slightly different parameters on the tests: we define a
	/// selection by anchor and end whereas in Palaso we use anchor and length!</remarks>
	[TestFixture]
	[InitializeRealKeyboardController(InitDummyAfterTests = true)]
	public class IbusRootSiteEventHandlerTests_Simple: SimpleRootsiteTestsBase<UndoableRealDataCache>
	{
		private IbusRootSiteEventHandler m_Handler;

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_cache.SetActionHandler(new SimpleActionHandler());
		}

		public override void TestSetup()
		{
			base.TestSetup();
			m_hvoRoot = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStText, 0, -1, -1);

			m_Handler = new IbusRootSiteEventHandler(m_basicView);
			KeyboardController.Register(m_basicView, m_Handler);
			m_basicView.Visible = true;
		}

		public override void TestTearDown()
		{
			KeyboardController.Unregister(m_basicView);
			base.TestTearDown();
		}

		private void ShowThisForm()
		{
			m_basicView.DisplayType = SimpleViewVc.DisplayType.kNormal;

			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			m_basicView.Width = 300;
			m_basicView.Height = 307 - 25;
			m_basicView.MakeRoot(m_hvoRoot, SimpleRootsiteTestsConstants.kflidTextParas, 3, m_wsFrn);
			m_basicView.CallLayout();
			m_basicView.AutoScrollPosition = new Point(0, 0);
			((SimpleActionHandler)m_cache.GetActionHandler()).RootBox = m_basicView.RootBox;
		}

		private void SetSelection(int selectionStart, int selectionEnd)
		{
			var selHelper = m_basicView.EditingHelper.CurrentSelection;
			selHelper.IchAnchor = selectionStart;
			selHelper.IchEnd = selectionEnd;
			selHelper.SetSelection(true);
		}

		private int SetupInitialText(string text)
		{
			int cParas = m_cache.get_VecSize(m_hvoRoot, SimpleRootsiteTestsConstants.kflidTextParas);
			ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
			int hvoPara = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStTxtPara, m_hvoRoot, SimpleRootsiteTestsConstants.kflidTextParas, cParas);
			m_cache.CacheStringProp(hvoPara, SimpleRootsiteTestsConstants.kflidParaContents, tsStrFactory.MakeString(string.Empty, m_wsFrn));
			var propFact = TsPropsFactoryClass.Create();
			var runStyle = propFact.MakeProps(null, m_wsFrn, 0);
			ITsString contents = m_cache.get_StringProp(hvoPara, SimpleRootsiteTestsConstants.kflidParaContents);
			var bldr = contents.GetBldr();
			bldr.Replace(bldr.Length, bldr.Length, text, runStyle);
			m_cache.SetString(hvoPara, SimpleRootsiteTestsConstants.kflidParaContents, bldr.GetString());
			ShowThisForm();
			m_basicView.Show();
			m_basicView.RefreshDisplay();
			m_basicView.Focus();
			return hvoPara;
		}

		private string GetTextFromView(int hvoPara)
		{
			return m_cache.get_StringProp(hvoPara, SimpleRootsiteTestsConstants.kflidParaContents).Text;
		}

		/// <summary>Unit tests for the OnUpdatePreeditText method. We test this separately from
		/// CommitText since we expect a slightly different behavior, e.g. the range selection
		/// should remain.</summary>
		/// <remarks>The test runner built-in to MonoDevelop gets confused when multiple test cases
		/// in different tests have the same name, therefore we prefix the name with "U".</remarks>
		[Test]
		[TestCase("",  0, 0, /* Input: */ "e", 1, /* expected: */ "e",  1, 1, TestName="UEmptyTextbox_AddsText")]
		[TestCase("",  0, 0, /* Input: */ "\u00EE", 1, /* expected: */ "i\u0302",  2, 2, TestName="UEmptyTextBox_NfcToNfd")]
		[TestCase("",  0, 0, /* Input: */ "i\u0302", 1, /* expected: */ "i\u0302",  2, 2, TestName="UEmptyTextBox_NfdStaysNfd")]
		[TestCase("b", 1, 1, /* Input: */ "e", 1, /* expected: */ "be", 2, 2, TestName="UExistingText_AddsText")]
		[TestCase("b", 0, 0, /* Input: */ "e", 1, /* expected: */ "eb", 1, 1, TestName="UExistingText_InsertInFront")]

		[TestCase("abc", 0, 1, /* Input: */ "e", 1,/* expected: */ "aebc", 0, 1, TestName="UExistingText_RangeSelection")]
		[TestCase("abc", 1, 0, /* Input: */ "e", 1,/* expected: */ "aebc", 1, 0, TestName="UExistingText_RangeSelection_Backwards")]
		[TestCase("abc", 0, 3, /* Input: */ "e", 1,/* expected: */ "abce", 0, 3, TestName="UReplaceAll")]

		// Chinese Pinyin ibus keyboard for some reason uses a 0-based index
		[TestCase("b", 1, 1, /* Input: */ "\u4FDD\u989D", 0, /* expected: */ "b\u4FDD\u989D", 3, 3, TestName="UCursorPos0")]
		[TestCase("b", 0, 1, /* Input: */ "\u4FDD\u989D", 0, /* expected: */ "b\u4FDD\u989D", 0, 1, TestName="UCursorPos0_RangeSelection")]
		public void UpdatePreedit(
			string text, int selectionStart, int selectionEnd,
			string composition, int insertPos,
			string expectedText, int expectedSelectionStart, int expectedSelectionEnd)
		{
			Console.WriteLine("Test/UpdatePreedit(\"{0}\", {1}, {2}, \"{3}\", {4}, \"{5}\", {6}, {7}",
				text, selectionStart, selectionEnd, composition, insertPos, expectedText, expectedSelectionStart, expectedSelectionEnd);
			for (int i = 0; i < composition.Length; ++i)
				Console.WriteLine(" - composition[{0}] = '{1}' = {2}", i, composition[i], (int)composition[i]);
			// Setup
			var hvoPara = SetupInitialText(text);
			SetSelection(selectionStart, selectionEnd);

			// Exercise
			m_Handler.OnUpdatePreeditText(new IBusText(composition), insertPos);

			// Verify
			var selHelper = m_basicView.EditingHelper.CurrentSelection;
			Assert.That(GetTextFromView(hvoPara), Is.EqualTo(expectedText));
			Assert.That(selHelper.IchAnchor, Is.EqualTo(expectedSelectionStart), "SelectionStart");
			Assert.That(selHelper.IchEnd, Is.EqualTo(expectedSelectionEnd), "SelectionEnd");
		}

		[Test]
		// This tests the scenario where we get a second OnUpdatePreeditText that should replace
		// the composition of the first one.
		[TestCase("bc", 1, 1, "a", 1, /* Input: */ "e", 1, /* expected: */ "bec", 2, 2, TestName="ExistingText_ReplaceFirstChar")]
		// This test tests the scenario where the textbox has one character, b. The user
		// positions the IP in front of the b and then types a and e with ibus (e.g. Danish keyboard).
		// This test simulates typing the e.
		[TestCase("b",  0, 0, "a", 1, /* Input: */ "e",  2, /* expected: */ "aeb",  2, 2, TestName="ExistingText_InsertSecondChar")]
		[TestCase("bc", 0, 1, "a", 1, /* Input: */ "e",  2, /* expected: */ "baec", 0, 1, TestName="ExistingText_RangeSelection")]
		[TestCase("bc", 0, 1, "a", 1, /* Input: */ "ae", 1, /* expected: */ "baec", 0, 1, TestName="ExistingText_RangeSelection_TwoChars")]
		public void UpdatePreedit_SecondUpdatePreedit(
			string text, int selectionStart, int selectionEnd,
			string firstComposition, int firstInsertPos,
			string composition, int insertPos,
			string expectedText, int expectedSelectionStart, int expectedSelectionEnd)
		{
			Console.WriteLine("Test/UpdatePreedit_SecondUpdatePreedit(\"{0}\", {1}, {2}, \"{3}\", {4}, \"{5}\", {6}, \"{7}\", {8}, {9}",
				text, selectionStart, selectionEnd, firstComposition, firstInsertPos, composition, insertPos, expectedText, expectedSelectionStart, expectedSelectionEnd);
			for (int i = 0; i < composition.Length; ++i)
				Console.WriteLine(" - composition[{0}] = '{1}' = {2}", i, composition[i], (int)composition[i]);
			// Setup
			var hvoPara = SetupInitialText(text);
			SetSelection(selectionStart, selectionEnd);
			m_Handler.OnUpdatePreeditText(new IBusText(firstComposition), firstInsertPos);

			// Exercise
			m_Handler.OnUpdatePreeditText(new IBusText(composition), insertPos);

			// Verify
			var selHelper = m_basicView.EditingHelper.CurrentSelection;
			Assert.That(GetTextFromView(hvoPara), Is.EqualTo(expectedText));
			Assert.That(selHelper.IchAnchor, Is.EqualTo(expectedSelectionStart), "SelectionStart");
			Assert.That(selHelper.IchEnd, Is.EqualTo(expectedSelectionEnd), "SelectionEnd");
		}

		/// <summary>Unit tests for the CommitOrReset method</summary>
		[Test]
		[TestCase(IBusAttrUnderline.None,   /* expected: */ false, "ab", 2, 2, TestName="CommitOrReset_Commits")]
		[TestCase(IBusAttrUnderline.Single, /* expected: */ true,   "a", 1, 1, TestName="CommitOrReset_Resets")]
		public void CommitOrReset(IBusAttrUnderline underline,
			bool expectedRetVal, string expectedText, int expectedSelStart, int expectedSelEnd)
		{
			// Setup
			var hvoPara = SetupInitialText("a");
			SetSelection(1, 1);
			m_Handler.OnUpdatePreeditText(new IBusText("b",
				new [] { new IBusUnderlineAttribute(underline, 0, 1)}), 1);

			// Exercise
			var ret = m_Handler.CommitOrReset();

			// Verify
			Assert.That(ret, Is.EqualTo(expectedRetVal));
			var selHelper = m_basicView.EditingHelper.CurrentSelection;
			Assert.That(GetTextFromView(hvoPara), Is.EqualTo(expectedText));
			Assert.That(selHelper.IchAnchor, Is.EqualTo(expectedSelStart), "SelectionStart");
			Assert.That(selHelper.IchEnd, Is.EqualTo(expectedSelEnd), "SelectionEnd");
		}

		/// <summary>Unit tests for the OnCommitText method. These tests are very similar to
		/// the tests for UpdatePreedit, but there are some important differences in the behavior,
		/// e.g. range selections should be replaced by the composition string.</summary>
		/// <remarks>The test runner built-in to MonoDevelop gets confused when multiple test cases
		/// in different tests have the same name, therefore we prefix the name with "U".</remarks>
		[Test]
		[TestCase("",  0, 0, "e", 1, /* Input: */ "e", /* expected: */ "e", 1, 1, TestName="CEmptyTextbox_AddsText")]
		[TestCase("",  0, 0, "\u00EE", 1, /* Input: */ "\u00EE", /* expected: */ "i\u0302", 2, 2, TestName="CEmptyTextBox_NfcToNfd")]
		[TestCase("",  0, 0, "i\u0302", 1, /* Input: */ "i\u0302", /* expected: */ "i\u0302", 2, 2, TestName="CEmptyTextBox_NfdStaysNfd")]
		[TestCase("b", 1, 1, "e", 1, /* Input: */ "e", /* expected: */ "be", 2, 2, TestName="CExistingText_AddsText")]
		[TestCase("b", 0, 0, "e", 1, /* Input: */ "e", /* expected: */ "eb", 1, 1, TestName="CExistingText_InsertInFront")]
		[TestCase("abc", 0, 1, "e", 1,/* Input: */ "e", /* expected: */ "ebc", 1, 1, TestName="CExistingText_RangeSelection")]
		[TestCase("abc", 1, 0, "e", 1,/* Input: */ "e", /* expected: */ "ebc", 1, 1, TestName="CExistingText_RangeSelection_Backwards")]
		[TestCase("abc", 0, 3, "e", 1,/* Input: */ "e", /* expected: */ "e",   1, 1, TestName="CReplaceAll")]
		public void CommitText(
			string text, int selectionStart, int selectionEnd,
			string composition, int insertPos,
			string commitText,
			string expectedText, int expectedSelectionStart, int expectedSelectionEnd)
		{
			// Setup
			var hvoPara = SetupInitialText(text);
			SetSelection(selectionStart, selectionEnd);
			m_Handler.OnUpdatePreeditText(new IBusText(composition), insertPos);

			// Exercise
			m_Handler.OnCommitText(new IBusText(commitText));

			// Verify
			var selHelper = m_basicView.EditingHelper.CurrentSelection;
			Assert.That(GetTextFromView(hvoPara), Is.EqualTo(expectedText));
			Assert.That(selHelper.IchAnchor, Is.EqualTo(expectedSelectionStart), "SelectionStart");
			Assert.That(selHelper.IchEnd, Is.EqualTo(expectedSelectionEnd), "SelectionEnd");
		}

		/// <summary>
		/// This test simulates a kind of keyboard similar to the IPA ibus keyboard which calls
		/// commit after each character. This test simulates the first commit call without a
		/// preceding OnUpdatePreeditText.
		/// </summary>
		[Test]
		public void Commit_Ipa()
		{
			// Setup
			var hvoPara = SetupInitialText("a");
			SetSelection(1, 1);

			// Exercise
			m_Handler.OnCommitText(new IBusText("\u014B"));

			// Verify
			var selHelper = m_basicView.EditingHelper.CurrentSelection;
			Assert.That(GetTextFromView(hvoPara), Is.EqualTo("a\u014B"));
			Assert.That(selHelper.IchAnchor, Is.EqualTo(2), "SelectionStart");
			Assert.That(selHelper.IchEnd, Is.EqualTo(2), "SelectionEnd");
		}

		/// <summary>
		/// This test simulates a kind of keyboard similar to the IPA ibus keyboard which calls
		/// commit after earch character. This test simulates the callbacks we get from the IPA
		/// keyboard when the user presses 'n' + '>'. The IPA ibus keyboard commits the 'n',
		/// sends us a backspace and then commits the 'ŋ'.
		/// </summary>
		[Test]
		public void Commit_IpaTwoCommits()
		{
			// Setup
			const int KeySymBackspace = 65288;
			const int ScanCodeBackspace = 14;
			var hvoPara = SetupInitialText("a");
			SetSelection(1, 1);

			// Exercise
			m_Handler.OnCommitText(new IBusText("n"));
			m_Handler.OnIbusKeyPress(KeySymBackspace, ScanCodeBackspace, 0);
			m_Handler.OnCommitText(new IBusText("\u014B"));

			// Verify
			var selHelper = m_basicView.EditingHelper.CurrentSelection;
			Assert.That(GetTextFromView(hvoPara), Is.EqualTo("a\u014B"));
			Assert.That(selHelper.IchAnchor, Is.EqualTo(2), "SelectionStart");
			Assert.That(selHelper.IchEnd, Is.EqualTo(2), "SelectionEnd");
		}

		/// <summary>
		/// Unit tests for the OnDeleteSurroundingText method. These tests assume that offset
		/// is 0-based, however the IBus docs don't say and I haven't found a keyboard in the
		/// wild that uses positive offsets.
		/// </summary>
		[Test]
		[TestCase(1, /*		 Input: */ -1, 1, /*		 expected: */ "bc", 0, TestName="DeleteSurroundingText_Before")]
		[TestCase(1, /*		 Input: */  0, 1, /*		 expected: */ "ac", 1, TestName="DeleteSurroundingText_After")]
		[TestCase(1, /*		 Input: */ -2, 1, /*		 expected: */ "abc",1, TestName="DeleteSurroundingText_IllegalBeforeIgnores")]
		[TestCase(1, /*		 Input: */ -2, 2, /*		 expected: */ "c",  0, TestName="DeleteSurroundingText_ToManyBefore")]
		[TestCase(2, /*		 Input: */ -1, 1, /*		 expected: */ "ac", 1, TestName="DeleteSurroundingText_BeforeUpdatesSelection")]
		[TestCase(2, /*		 Input: */ -2, 2, /*		 expected: */ "c",  0, TestName="DeleteSurroundingText_MultipleBefore")]
		[TestCase(1, /*		 Input: */  1, 1, /*		 expected: */ "ab", 1, TestName="DeleteSurroundingText_AfterWithOffset")]
		[TestCase(1, /*		 Input: */  2, 1, /*		 expected: */ "abc",1, TestName="DeleteSurroundingText_IllegalAfterIgnores")]
		[TestCase(1, /*		 Input: */  0, 2, /*		 expected: */ "a",  1, TestName="DeleteSurroundingText_MultipleAfter")]
		[TestCase(1, /*		 Input: */  0, 3, /*		 expected: */ "a",  1, TestName="DeleteSurroundingText_ToManyAfterIgnoresRest")]
		[TestCase(1, /*		 Input: */  0,-1, /*		 expected: */ "abc",1, TestName="DeleteSurroundingText_IllegalNumberOfChars")]
		[TestCase(1, /*		 Input: */  0, 0, /*		 expected: */ "abc",1, TestName="DeleteSurroundingText_ZeroNumberOfChars")]
		public void DeleteSurroundingText(int cursorPos, int offset, int nChars,
			string expectedText, int expectedCursorPos)
		{
			// Setup
			var hvoPara = SetupInitialText("abc");
			SetSelection(cursorPos, cursorPos);

			// Exercise
			m_Handler.OnDeleteSurroundingText(offset, nChars);

			// Verify
			var selHelper = m_basicView.EditingHelper.CurrentSelection;
			Assert.That(GetTextFromView(hvoPara), Is.EqualTo(expectedText));
			Assert.That(selHelper.IchAnchor, Is.EqualTo(expectedCursorPos), "SelectionStart");
			Assert.That(selHelper.IchEnd, Is.EqualTo(expectedCursorPos), "SelectionEnd");
		}

		[Test]
		[TestCase(1, 1, TestName = "CancelPreedit_IP")]
		[TestCase(0, 1, TestName = "CancelPreedit_RangeSelection")]
		public void CancelPreedit(int selStart, int selEnd)
		{
			// Setup
			var hvoPara = SetupInitialText("b");
			SetSelection(selStart, selEnd);
			m_Handler.OnUpdatePreeditText(new IBusText("\u4FDD\u989D"), 0);

			// Exercise
			m_Handler.Reset();

			// Verify
			var selHelper = m_basicView.EditingHelper.CurrentSelection;
			Assert.That(GetTextFromView(hvoPara), Is.EqualTo("b"));
			Assert.That(selHelper.IchAnchor, Is.EqualTo(selStart), "SelectionStart");
			Assert.That(selHelper.IchEnd, Is.EqualTo(selEnd), "SelectionEnd");
		}
	}
}
#endif
