// Copyright (c) 2013, SIL International.
// Distributable under the terms of the MIT license (http://opensource.org/licenses/MIT).
#if __MonoCS__
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using IBusDotNet;
using NUnit.Framework;
using X11.XKlavier;
using Palaso.UI.WindowsForms.Keyboarding;
using Palaso.UI.WindowsForms.Keyboarding.InternalInterfaces;
using Palaso.UI.WindowsForms.Keyboarding.Linux;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils.Attributes;
using NMock;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for InputBusController
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[InitializeRealKeyboardController(InitDummyAfterTests = true)]
	[Platform(Include = "Linux", Reason="IbusRootSiteEventHandlerTests is Linux only")]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Unit test. Variable disposed in Teardown method")]
	public class IbusRootSiteEventHandlerTests: BaseTest
	{
		// some lparam values representing keypress that we use for testing.
		private static readonly Dictionary<char, int> lparams = new Dictionary<char, int>();
		protected DummySimpleRootSite m_dummySimpleRootSite;
		protected ITestableIbusCommunicator m_dummyIBusCommunicator;

		/// <summary/>
		static IbusRootSiteEventHandlerTests()
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
			KeyboardController.Unregister(m_dummySimpleRootSite);

			if (m_dummyIBusCommunicator != null)
				m_dummyIBusCommunicator.Dispose();

			m_dummySimpleRootSite.Visible = false;
			m_dummySimpleRootSite.Dispose();

			m_dummyIBusCommunicator = null;
			m_dummySimpleRootSite = null;
		}

		public void ChooseSimulatedKeyboard(ITestableIbusCommunicator ibusCommunicator)
		{
			m_dummyIBusCommunicator = ibusCommunicator;
			var ibusKeyboardAdapter = new IbusKeyboardAdaptorDouble(ibusCommunicator);
			var xklEngineMock = new DynamicMock(typeof(IXklEngine));
			var xkbKeyboardAdapter = new XkbKeyboardAdaptorDouble((IXklEngine)xklEngineMock.MockInstance);
			KeyboardController.Manager.SetKeyboardAdaptors(new IKeyboardAdaptor[] { xkbKeyboardAdapter, ibusKeyboardAdapter});
			KeyboardController.Register(m_dummySimpleRootSite, new IbusRootSiteEventHandler(m_dummySimpleRootSite));
		}

		/// <summary>Simulate multiple keypresses.</summary>
		[TestCase(typeof(CommitOnlyIbusCommunicator),
			/* input: */new[] {"\b"},
			/* expected: */"", "", 0, 0, TestName = "EmptyStateSendSingleControlCharacter_SelectionIsInsertionPoint")]
		[TestCase(typeof(CommitOnlyIbusCommunicator),
			/* input: */ new[] {"T"},
			/* expected: */ "T", "",  1, 1, TestName="EmptyStateSendSingleKeyPress_SelectionIsInsertionPoint")]
		[TestCase(typeof(CommitOnlyIbusCommunicator),
			/* input: */ new[] { "T", "U" },
			/* expected: */ "TU", "",  2, 2, TestName="EmptyStateSendTwoKeyPresses_SelectionIsInsertionPoint")]
		[TestCase(typeof(KeyboardThatSendsDeletesAsCommitsDummyIBusCommunicator),
			/* input: */ new[] {"S", "T", "U", " "},
			/* expected: */ "stu", "", 3, 3, TestName="KeyboardThatSendsBackspacesInItsCommits_BackspacesShouldNotBeIngored")]
		[TestCase(typeof(KeyboardThatSendsBackspacesAsForwardKeyEvents),
			/* input: */ new[] {"S", "T", "U", " "},
			/* expected: */ "stu", "", 3, 3, TestName="KeyboardThatSendsBackspacesInItsForwardKeyEvent_BackspacesShouldNotBeIngored")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */new[] {"t"},
			/* expected: */"t", "t", 1, 1, TestName = "OneCharNoSpace_PreeditContainsChar")]
		[TestCase(typeof(CommitBeforeUpdateIbusCommunicator),
			/* input: */ new[] {"T"},
			/* expected: */ "T", "T", 1, 1, TestName="SimplePreeditEmptyStateSendSingleKeyPress")]
		[TestCase(typeof(CommitBeforeUpdateIbusCommunicator),
			/* input: */ new[] {"S", "T", "U"},
			/* expected: */ "STU", "U", 3, 3, TestName="SimplePreeditEmptyStateSendThreeKeyPresses")]
		[TestCase(typeof(CommitBeforeUpdateIbusCommunicator),
			/* input: */ new[] {"T", "U"},
			/* expected: */ "TU", "U", 2, 2, TestName="SimplePreeditEmptyStateSendTwoKeyPresses")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] {" "},
			/* expected: */ " ", "", 1, 1, TestName="Space_JustAddsToDocument")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] {"t", "u"},
			/* expected: */ "tu", "tu", 2, 2, TestName="TwoChars_OnlyPreedit")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */ new[] {"t", "u", /*commit:*/" "},
			/* expected: */ "TU", "", 2, 2, TestName="TwoCharsAndSpace_PreeditIsCommitted")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */ new[] {"t", "u"},
			/* expected: */ "tu", "tu", 2, 2, TestName="TwoCharsNoSpace_PreeditContainsChars")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] {"t", "u", /*commit*/" "},
			/* expected: */ "T", "", 1, 1, TestName="TwoCharsSpace_SubstitutionWorkedAndPreeditIsEmpty")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */ new[] {"t", "u", /*commit:*/" ", "s", "u", /* commit*/" "},
			/* expected: */ "TUSU", "", 4, 4, TestName="TwoCharsSpaceTwoChars_PreeditIsEmpty")]
		[TestCase(typeof(KeyboardThatCommitsPreeditOnSpace),
			/* input: */ new[] {"t", "u", /*commit:*/" ", "s", "u" /* don't commit*/},
			/* expected: */ "TUsu", "su", 4, 4, TestName="TwoCharsSpaceTwoChars_PreeditIsLastHalf")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] {"t", "u", /*commit*/" ", "s", "u" /*don't commit*/},
			/* expected: */ "Tsu", "su", 3, 3, TestName="TwoCharsSpaceTwoChars_SubstitutionWorkedAndPreeditIsLastHalf")]
		[TestCase(typeof(KeyboardWithGlyphSubstitution),
			/* input: */ new[] {"t", "u", /*commit*/" ", "s", "u", /*commit*/" "},
			/* expected: */ "TS", "", 2, 2, TestName="TwoCharsSpaceTwoCharsSpace_SubstitutionWorkedAndPreeditIsEmpty")]
		public void SimulateKeypress(Type ibusCommunicator,
			string[] keys,
			string expectedDocument, string expectedSelectionText, int expectedAnchor, int expectedEnd)
		{
			// Setup
			ChooseSimulatedKeyboard(Activator.CreateInstance(ibusCommunicator) as ITestableIbusCommunicator);

			// Exercise
			for (int i = 0; i < keys.Length; i++)
			{
				m_dummyIBusCommunicator.ProcessKeyEvent(keys[i][0], lparams[keys[i].ToUpper()[0]],
					char.IsUpper(keys[i][0]) ? Keys.Shift : Keys.None);
			}

			// Verify
			var dummyRootBox = (DummyRootBox)m_dummySimpleRootSite.RootBox;
			var dummySelection = (DummyVwSelection)m_dummySimpleRootSite.RootBox.Selection;

			Assert.AreEqual(expectedDocument, dummyRootBox.Text, "RootSite text");
			Assert.AreEqual(expectedSelectionText, m_dummyIBusCommunicator.PreEdit, "Preedit text");
			Assert.AreEqual(expectedAnchor, dummySelection.Anchor, "Selection anchor");
			Assert.AreEqual(expectedEnd, dummySelection.End, "Selection end");
		}

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Dummy IBusCommunicator is disposed in TestTearDown()")]
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

		/// <summary></summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Dummy IBusCommunicator is disposed in TestTearDown()")]
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

			Assert.AreEqual("TU", dummyRootBox.Text);

			Assert.AreEqual("U", m_dummyIBusCommunicator.PreEdit);
			Assert.AreEqual(2, dummySelection.Anchor);
			Assert.AreEqual(2, dummySelection.End);
		}

		/// <summary>Test cases for FWNX-674</summary>
		[Test]
		[TestCase(1, 2, TestName="ReplaceForwardSelectedChar_Replaced")]
		[TestCase(2, 1, TestName="ReplaceBackwardSelectedChar_Replaced")]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Dummy IBusCommunicator is disposed in TestTearDown()")]
		public void CorrectPlacementOfTypedChars(int anchor, int end)
		{
			// Setup
			ChooseSimulatedKeyboard(new KeyboardWithGlyphSubstitution());

			m_dummyIBusCommunicator.ProcessKeyEvent('a', lparams['A'], Keys.None);
			m_dummyIBusCommunicator.ProcessKeyEvent('a', lparams['A'], Keys.None);
			// Commit by pressing space
			m_dummyIBusCommunicator.ProcessKeyEvent(' ', lparams[' '], Keys.None);
			m_dummyIBusCommunicator.ProcessKeyEvent('b', lparams['B'], Keys.None);
			m_dummyIBusCommunicator.ProcessKeyEvent('b', lparams['B'], Keys.None);
			// Commit by pressing space
			m_dummyIBusCommunicator.ProcessKeyEvent(' ', lparams[' '], Keys.None);
			m_dummyIBusCommunicator.ProcessKeyEvent('c', lparams['C'], Keys.None);
			m_dummyIBusCommunicator.ProcessKeyEvent('c', lparams['C'], Keys.None);
			// Commit by pressing space
			m_dummyIBusCommunicator.ProcessKeyEvent(' ', lparams[' '], Keys.None);

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

	}

	#region Mock classes used for testing InputBusController

	/// <summary></summary>
	public class DummyVwSelection : IVwSelection
	{
		public int Anchor;
		public int End;
		private readonly DummyRootBox m_rootBox;

		public DummyVwSelection(DummyRootBox rootbox, int anchor, int end)
		{
			m_rootBox = rootbox;
			Anchor = anchor;
			End = end;
		}

		public string SelectionText
		{
			get
			{
				if (Anchor >= m_rootBox.Text.Length)
					return String.Empty;
				var begin = Math.Min(Anchor, End);
				var end = Math.Max(Anchor, End);
				return m_rootBox.Text.Substring(begin, end - begin);
			}
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
			var selectionText = _tss != null ? _tss.Text : String.Empty;
			if (selectionText == null)
				selectionText = String.Empty;
			var begin = Math.Min(Anchor, End);
			var end = Math.Max(Anchor, End);
			if (begin < m_rootBox.Text.Length)
				m_rootBox.Text = m_rootBox.Text.Remove(begin, end - begin);
			if (begin < m_rootBox.Text.Length)
				m_rootBox.Text = m_rootBox.Text.Insert(begin, selectionText);
			else
				m_rootBox.Text += selectionText;
			Anchor = End = begin + selectionText.Length;
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
			get { return End != Anchor; }
		}

		public bool EndBeforeAnchor
		{
			get { return End < Anchor; }
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
			throw new NotImplementedException();
		}

		public int get_VecItem(int hvo, int tag, int index)
		{
			throw new NotImplementedException();
		}

		public int get_VecSize(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public int get_VecSizeAssumeCached(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public void VecProp(int hvo, int tag, int chvoMax, out int _chvo, ArrayPtr _rghvo)
		{
			throw new NotImplementedException();
		}

		public void BinaryPropRgb(int obj, int tag, ArrayPtr _rgb, int cbMax, out int _cb)
		{
			throw new NotImplementedException();
		}

		public Guid get_GuidProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public int get_ObjFromGuid(Guid uid)
		{
			throw new NotImplementedException();
		}

		public int get_IntProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public long get_Int64Prop(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public bool get_BooleanProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public ITsString get_MultiStringAlt(int hvo, int tag, int ws)
		{
			throw new NotImplementedException();
		}

		public ITsMultiString get_MultiStringProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public object get_Prop(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public ITsString get_StringProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public long get_TimeProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public string get_UnicodeProp(int obj, int tag)
		{
			throw new NotImplementedException();
		}

		public void set_UnicodeProp(int obj, int tag, string bstr)
		{
			throw new NotImplementedException();
		}

		public void UnicodePropRgch(int obj, int tag, ArrayPtr _rgch, int cchMax, out int _cch)
		{
			throw new NotImplementedException();
		}

		public object get_UnknownProp(int hvo, int tag)
		{
			throw new NotImplementedException();
		}

		public void BeginUndoTask(string bstrUndo, string bstrRedo)
		{
			throw new NotImplementedException();
		}

		public void EndUndoTask()
		{
			throw new NotImplementedException();
		}

		public void ContinueUndoTask()
		{
			throw new NotImplementedException();
		}

		public void EndOuterUndoTask()
		{
			throw new NotImplementedException();
		}

		public void Rollback()
		{
			throw new NotImplementedException();
		}

		public void BreakUndoTask(string bstrUndo, string bstrRedo)
		{
			throw new NotImplementedException();
		}

		public void BeginNonUndoableTask()
		{
			throw new NotImplementedException();
		}

		public void EndNonUndoableTask()
		{
			throw new NotImplementedException();
		}

		public IActionHandler GetActionHandler()
		{
			return m_actionHandler;
		}

		public void SetActionHandler(IActionHandler _acth)
		{
			throw new NotImplementedException();
		}

		public void DeleteObj(int hvoObj)
		{
			throw new NotImplementedException();
		}

		public void DeleteObjOwner(int hvoOwner, int hvoObj, int tag, int ihvo)
		{
			throw new NotImplementedException();
		}

		public void InsertNew(int hvoObj, int tag, int ihvo, int chvo, IVwStylesheet _ss)
		{
			throw new NotImplementedException();
		}

		public int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
		{
			throw new NotImplementedException();
		}

		public void MoveOwnSeq(int hvoSrcOwner, int tagSrc, int ihvoStart, int ihvoEnd,
			int hvoDstOwner, int tagDst, int ihvoDstStart)
		{
			throw new NotImplementedException();
		}

		public void MoveOwn(int hvoSrcOwner, int tagSrc, int hvo, int hvoDstOwner, int tagDst,
			int ihvoDstStart)
		{
			throw new NotImplementedException();
		}

		public void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
		{
			throw new NotImplementedException();
		}

		public void SetObjProp(int hvo, int tag, int hvoObj)
		{
			throw new NotImplementedException();
		}

		public void RemoveObjRefs(int hvo)
		{
			throw new NotImplementedException();
		}

		public void SetBinary(int hvo, int tag, byte[] _rgb, int cb)
		{
			throw new NotImplementedException();
		}

		public void SetGuid(int hvo, int tag, Guid uid)
		{
			throw new NotImplementedException();
		}

		public void SetInt(int hvo, int tag, int n)
		{
			throw new NotImplementedException();
		}

		public void SetInt64(int hvo, int tag, long lln)
		{
			throw new NotImplementedException();
		}

		public void SetBoolean(int hvo, int tag, bool n)
		{
			throw new NotImplementedException();
		}

		public void SetMultiStringAlt(int hvo, int tag, int ws, ITsString _tss)
		{
			throw new NotImplementedException();
		}

		public void SetString(int hvo, int tag, ITsString _tss)
		{
			throw new NotImplementedException();
		}

		public void SetTime(int hvo, int tag, long lln)
		{
			throw new NotImplementedException();
		}

		public void SetUnicode(int hvo, int tag, string _rgch, int cch)
		{
			throw new NotImplementedException();
		}

		public void SetUnknown(int hvo, int tag, object _unk)
		{
			throw new NotImplementedException();
		}

		public void AddNotification(IVwNotifyChange _nchng)
		{
			throw new NotImplementedException();
		}

		public void PropChanged(IVwNotifyChange _nchng, int _ct, int hvo, int tag, int ivMin,
			int cvIns, int cvDel)
		{
			throw new NotImplementedException();
		}

		public void RemoveNotification(IVwNotifyChange _nchng)
		{
			throw new NotImplementedException();
		}

		public int GetDisplayIndex(int hvoOwn, int tag, int ihvo)
		{
			throw new NotImplementedException();
		}

		public int get_WritingSystemsOfInterest(int cwsMax, ArrayPtr _ws)
		{
			throw new NotImplementedException();
		}

		public void InsertRelExtra(int hvoSrc, int tag, int ihvo, int hvoDst, string bstrExtra)
		{
			throw new NotImplementedException();
		}

		public void UpdateRelExtra(int hvoSrc, int tag, int ihvo, string bstrExtra)
		{
			throw new NotImplementedException();
		}

		public string GetRelExtra(int hvoSrc, int tag, int ihvo)
		{
			throw new NotImplementedException();
		}

		public bool get_IsPropInCache(int hvo, int tag, int cpt, int ws)
		{
			throw new NotImplementedException();
		}

		public bool IsDirty()
		{
			throw new NotImplementedException();
		}

		public void ClearDirty()
		{
			throw new NotImplementedException();
		}

		public bool get_IsValidObject(int hvo)
		{
			throw new NotImplementedException();
		}

		public bool get_IsDummyId(int hvo)
		{
			throw new NotImplementedException();
		}

		public int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			throw new NotImplementedException();
		}

		public string GetOutlineNumber(int hvo, int flid, bool fFinPer)
		{
			throw new NotImplementedException();
		}

		public void MoveString(int hvoSource, int flidSrc, int wsSrc, int ichMin, int ichLim,
			int hvoDst, int flidDst, int wsDst, int ichDest, bool fDstIsNew)
		{
			throw new NotImplementedException();
		}

		public ILgWritingSystemFactory WritingSystemFactory
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

		public IFwMetaDataCache MetaDataCache
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
			throw new NotImplementedException();
		}

		public void SetSite(IVwRootSite _vrs)
		{
			throw new NotImplementedException();
		}

		public void SetRootObjects(int[] _rghvo, IVwViewConstructor[] _rgpvwvc, int[] _rgfrag,
			IVwStylesheet _ss, int chvo)
		{
			throw new NotImplementedException();
		}

		public void SetRootObject(int hvo, IVwViewConstructor _vwvc, int frag, IVwStylesheet _ss)
		{
			throw new NotImplementedException();
		}

		public void SetRootVariant(object v, IVwStylesheet _ss, IVwViewConstructor _vwvc, int frag)
		{
			throw new NotImplementedException();
		}

		public void SetRootString(ITsString _tss, IVwStylesheet _ss, IVwViewConstructor _vwvc,
			int frag)
		{
			throw new NotImplementedException();
		}

		public object GetRootVariant()
		{
			throw new NotImplementedException();
		}

		public void Serialize(System.Runtime.InteropServices.ComTypes.IStream _strm)
		{
			throw new NotImplementedException();
		}

		public void Deserialize(System.Runtime.InteropServices.ComTypes.IStream _strm)
		{
			throw new NotImplementedException();
		}

		public void WriteWpx(System.Runtime.InteropServices.ComTypes.IStream _strm)
		{
			throw new NotImplementedException();
		}

		public void DestroySelection()
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		public IVwSelection MakeTextSelInObj(int ihvoRoot, int cvsli, SelLevInfo[] _rgvsli,
			int cvsliEnd, SelLevInfo[] _rgvsliEnd, bool fInitial, bool fEdit, bool fRange,
			bool fWholeObj, bool fInstall)
		{
			throw new NotImplementedException();
		}

		public IVwSelection MakeSelInObj(int ihvoRoot, int cvsli, SelLevInfo[] _rgvsli, int tag,
			bool fInstall)
		{
			throw new NotImplementedException();
		}

		public IVwSelection MakeSelAt(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst, bool fInstall)
		{
			throw new NotImplementedException();
		}

		public IVwSelection MakeSelInBox(IVwSelection _selInit, bool fEndPoint, int iLevel, int iBox,
			bool fInitial, bool fRange, bool fInstall)
		{
			throw new NotImplementedException();
		}

		public bool get_IsClickInText(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new NotImplementedException();
		}

		public bool get_IsClickInObject(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst, out int _odt)
		{
			throw new NotImplementedException();
		}

		public bool get_IsClickInOverlayTag(int xd, int yd, Utils.Rect rcSrc1, Utils.Rect rcDst1,
			out int _iGuid, out string _bstrGuids, out Utils.Rect _rcTag, out Utils.Rect _rcAllTags,
			out bool _fOpeningTag)
		{
			throw new NotImplementedException();
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
			throw new NotImplementedException();
		}

		public void OnSysChar(int chw)
		{
			throw new NotImplementedException();
		}

		public int OnExtendedKey(int chw, VwShiftStatus ss, int nFlags)
		{
			throw new NotImplementedException();
		}

		public void FlashInsertionPoint()
		{
			throw new NotImplementedException();
		}

		public void MouseDown(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new NotImplementedException();
		}

		public void MouseDblClk(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new NotImplementedException();
		}

		public void MouseMoveDrag(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new NotImplementedException();
		}

		public void MouseDownExtended(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new NotImplementedException();
		}

		public void MouseUp(int xd, int yd, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new NotImplementedException();
		}

		public void Activate(VwSelectionState vss)
		{
			throw new NotImplementedException();
		}

		public VwPrepDrawResult PrepareToDraw(IVwGraphics _vg, Utils.Rect rcSrc, Utils.Rect rcDst)
		{
			throw new NotImplementedException();
		}

		public void DrawRoot(IVwGraphics _vg, Utils.Rect rcSrc, Utils.Rect rcDst, bool fDrawSel)
		{
			throw new NotImplementedException();
		}

		public void Layout(IVwGraphics _vg, int dxsAvailWidth)
		{
			throw new NotImplementedException();
		}

		public void InitializePrinting(IVwPrintContext _vpc)
		{
			throw new NotImplementedException();
		}

		public int GetTotalPrintPages(IVwPrintContext _vpc)
		{
			throw new NotImplementedException();
		}

		public void PrintSinglePage(IVwPrintContext _vpc, int nPageNo)
		{
			throw new NotImplementedException();
		}

		public bool LoseFocus()
		{
			throw new NotImplementedException();
		}

		public void Close()
		{
		}

		public void Reconstruct()
		{
			throw new NotImplementedException();
		}

		public void OnStylesheetChange()
		{
			throw new NotImplementedException();
		}

		public void DrawingErrors(IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public void SetTableColWidths(VwLength[] _rgvlen, int cvlen)
		{
			throw new NotImplementedException();
		}

		public bool IsDirty()
		{
			throw new NotImplementedException();
		}

		public void GetRootObject(out int _hvo, out IVwViewConstructor _pvwvc, out int _frag,
			out IVwStylesheet _pss)
		{
			throw new NotImplementedException();
		}

		public void DrawRoot2(IVwGraphics _vg, Utils.Rect rcSrc, Utils.Rect rcDst, bool fDrawSel,
			int ysTop, int dysHeight)
		{
			throw new NotImplementedException();
		}

		public bool DoSpellCheckStep()
		{
			throw new NotImplementedException();
		}

		public bool IsSpellCheckComplete()
		{
			throw new NotImplementedException();
		}

		public void RestartSpellChecking()
		{
			throw new NotImplementedException();
		}

		public void SetSpellingRepository(IGetSpellChecker _gsp)
		{
		}

		public ISilDataAccess DataAccess
		{
			get
			{
				return m_dummyDataAccess;
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public IVwOverlay Overlay
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
				throw new NotImplementedException();
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
				throw new NotImplementedException();
			}
		}

		public int XdPos
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public IVwSynchronizer Synchronizer
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public int MaxParasToScan
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

		public bool IsCompositionInProgress
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public bool IsPropChangedInProgress
		{
			get { return false; }
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

		public override bool Focused
		{
			get { return true; }
		}
	}

	public interface ITestableIbusCommunicator: IIbusCommunicator
	{
		string PreEdit { get; }
	}

	/// <summary>
	/// Mock IBusCommunicator implementation that shows the latest charater as a preedit.
	/// Commits last char BEFORE showing the next preedit.
	/// </summary>
	public sealed class CommitBeforeUpdateIbusCommunicator : ITestableIbusCommunicator
	{
		private string m_preedit = string.Empty;

		#region IIbusCommunicator implementation
		public event Action<string> CommitText;
		public event Action<string, int> UpdatePreeditText;
		public event Action HidePreeditText;
#pragma warning disable 67
		public event Action<int, int> DeleteSurroundingText;
		public event Action<int, int, int> KeyEvent;

#pragma warning restore 67

		public bool IsDisposed { get; private set; }

		public IBusConnection Connection
		{
			get { throw new NotImplementedException(); }
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

			if (m_preedit != String.Empty)
			{
				// Delete the pre-edit first. This is necessary because we use a no-op action
				// handler, so the rollback doesn't do anything.
				UpdatePreeditText(string.Empty, 0);
				CommitText(m_preedit);
			}

			m_preedit = ((char)keySym).ToString();
			if (((uint)state & shift) != 0 || ((uint)state & capslock) != 0)
				m_preedit = m_preedit.ToUpper();
			UpdatePreeditText(m_preedit, 0);
			return true;
		}

		public void Reset()
		{
			m_preedit = String.Empty;
			// Delete the pre-edit first. This is necessary because we use a no-op action
			// handler, so the rollback doesn't do anything.
			if (UpdatePreeditText != null)
				UpdatePreeditText(String.Empty, 0);
			if (HidePreeditText != null)
				HidePreeditText();
		}

		public void CreateInputContext()
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
			IsDisposed = true;
		}

		public void NotifySelectionLocationAndHeight(int x, int y, int height)
		{
		}
		#endregion

		public string PreEdit
		{
			get { return m_preedit; }
		}
	}

	/// <summary>
	/// Mock IBusCommunicatior implementation. Typing is performed in a preedit. Upon pressing
	/// Space, the preedit is committed all at once (and in upper case).
	/// (cf PreeditDummyIBusCommunicator which commits each keystroke separately.)
	/// </summary>
	public class KeyboardThatCommitsPreeditOnSpace : ITestableIbusCommunicator
	{
		public KeyboardThatCommitsPreeditOnSpace()
		{

		}
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
		#region IIbusCommunicator implementation
		public event Action<string> CommitText;
		public event Action<string, int> UpdatePreeditText;
#pragma warning disable 67
		public event Action<int, int> DeleteSurroundingText;
		public event Action HidePreeditText;
		public event Action<int, int, int> KeyEvent;

#pragma warning restore 67

		public IBusConnection Connection
		{
			get { throw new NotImplementedException(); }
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
				input = ToggleCase(input);
			if (((uint)state & capslock) != 0)
				input = ToggleCase(input);

			m_preedit += input;
			CallUpdatePreeditText(m_preedit, 0);

			return true;
		}

		public void Reset()
		{
			// nothing we need to do
		}

		public void CreateInputContext()
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

		public string PreEdit
		{
			get { return m_preedit; }
		}

		protected void CallCommitText(string text)
		{
			CallUpdatePreeditText(string.Empty, 0);
			CommitText(text);
		}

		protected void CallUpdatePreeditText(string text, int cursor_pos)
		{
			UpdatePreeditText(text, cursor_pos);
		}

		protected virtual void Commit(char lastCharacterTyped)
		{
			CallCommitText(m_preedit.ToUpperInvariant());
			m_preedit = string.Empty;
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
				CallUpdatePreeditText(string.Empty, 0);
				CallCommitText(m_preedit[0].ToString().ToUpperInvariant());
				m_preedit = string.Empty;
			}
		}
	}

	class XkbKeyboardAdaptorDouble: XkbKeyboardAdaptor
	{
		public XkbKeyboardAdaptorDouble(IXklEngine engine): base(engine)
		{
		}

		protected override void InitLocales()
		{
		}
	}

	class IbusKeyboardAdaptorDouble: IbusKeyboardAdaptor
	{
		public IbusKeyboardAdaptorDouble(IIbusCommunicator ibusCommunicator): base(ibusCommunicator)
		{
		}

		protected override void InitKeyboards()
		{
		}
	}

	/// <summary>
	/// Mock IBusCommunicator implementation that just echos back any sent
	/// keypresses.(Doesn't show preedit)
	/// </summary>
	public sealed class CommitOnlyIbusCommunicator : ITestableIbusCommunicator
	{
		#region IIbusCommunicator implementation
		public event Action<string> CommitText;
#pragma warning disable 67
		public event Action<string, int> UpdatePreeditText;
		public event Action<int, int> DeleteSurroundingText;
		public event Action HidePreeditText;
		public event Action<int, int, int> KeyEvent;
#pragma warning restore 67

		public bool IsDisposed { get; private set; }

		public IBusConnection Connection
		{
			get { throw new NotImplementedException(); }
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
				return true;

			string str = ((char)keySym).ToString();
			if (((uint)state & shift) != 0 || ((uint)state & capslock) != 0)
				str = str.ToUpper();
			if (CommitText != null)
				CommitText(str);
			return true;
		}

		public void Reset()
		{
			// nothing we need to do
		}

		public void CreateInputContext()
		{

		}

		public bool Connected
		{
			get { return true; }
		}

		public void Dispose()
		{
			IsDisposed = true;
		}

		public void NotifySelectionLocationAndHeight(int x, int y, int height)
		{
		}
	#endregion

		public string PreEdit
		{
			get { return string.Empty; }
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
		public event Action<string> CommitText;
#pragma warning disable 67
		public event Action<string, int> UpdatePreeditText;
		public event Action<int, int> DeleteSurroundingText;
		public event Action HidePreeditText;
		public event Action<int, int, int> KeyEvent;
#pragma warning restore 67

		public bool IsDisposed { get; private set; }

		public IBusConnection Connection
		{
			get { throw new NotImplementedException(); }
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
					CommitText("\b"); // 0x0008
				}

				foreach (char c in buffer.ToLowerInvariant())
				{
					CommitText(c.ToString());
				}

				buffer = String.Empty;
				return true;
			}
			string str = ((char)keySym).ToString();
			if (((uint)state & shift) != 0 || ((uint)state & capslock) != 0)
				str = str.ToUpper();
			buffer += str;
			CommitText(str);
			return true;
		}

		public void Reset()
		{
			// nothing we need to do
		}

		public void CreateInputContext()
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
			IsDisposed = true;
		}

		public void NotifySelectionLocationAndHeight(int x, int y, int height)
		{
		}
		#endregion

		public string PreEdit
		{
			get { return string.Empty;}
		}
	}

	/// <summary>
	/// Mock IBusCommunicator implementation that deletes current word when space is pressed,
	/// by sending backspaces as ForwardKeyEvents. It then resends the word in lower case.
	/// </summary>
	public sealed class KeyboardThatSendsBackspacesAsForwardKeyEvents : ITestableIbusCommunicator
	{
		private string buffer = string.Empty;

		#region IIbusCommunicator implementation
		public event Action<string> CommitText;
#pragma warning disable 67
		public event Action<string, int> UpdatePreeditText;
		public event Action<int, int> DeleteSurroundingText;
		public event Action HidePreeditText;
#pragma warning restore 67

		public event Action<int, int, int> KeyEvent;

		public bool IsDisposed { get; private set; }

		public IBusConnection Connection
		{
			get { throw new NotImplementedException(); }
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
					int mysteryValue = 22;
					KeyEvent(0xFF00 | '\b', mysteryValue, 0); // 0x0008
				}

				foreach (char c in buffer.ToLowerInvariant())
				{
					CommitText(c.ToString());
				}

				buffer = String.Empty;
				return true;
			}
			string str = ((char)keySym).ToString();
			if (((uint)state & shift) != 0 || ((uint)state & capslock) != 0)
				str = str.ToUpper();
			buffer += str;
			CommitText(str);
			return true;
		}

		public void Reset()
		{
			// nothing we need to do
		}

		public void CreateInputContext()
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
			IsDisposed = true;
		}

		public void NotifySelectionLocationAndHeight(int x, int y, int height)
		{
		}
		#endregion

		public string PreEdit
		{
			get { return string.Empty; }
		}
	}

	#endregion
}
#endif
