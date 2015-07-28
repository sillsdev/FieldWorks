// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Test the functionality of the MacroListener class. A few minor methods are not tested, because testing would
	/// require setting up major objects like XWindows or major architectural changes so these objects make more use of interfaces
	/// to support mocking.
	/// </summary>
	[TestFixture]
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="m_Mediator gets disposed in TestTearDown method")]
	public class MacroListenerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Mediator m_Mediator;

		public override void TestTearDown()
		{
			if (m_Mediator != null)
			{
				m_Mediator.Dispose();
				m_Mediator = null;
			}
			base.TestTearDown();
		}

		[Test]
		public void InitMacro_PutsItemAtProperPosition()
		{
			var ml = new MacroListener();
			var macroImplementors = new List<IFlexMacro>(new IFlexMacro[] {new MacroF4()});
			var macros = ml.AssignMacrosToSlots(macroImplementors);
			Assert.That(macros[0], Is.Null);
			Assert.That(macros[2], Is.EqualTo(macroImplementors[0]));
		}
		[Test]
		public void InitMacro_PutsConflictAtFirstAvailableSpot()
		{
			var ml = new MacroListener();
			var macroImplementors = new List<IFlexMacro>(new IFlexMacro[] { new MacroF4(), new MacroF4(), new MacroF2() });
			var macros = ml.AssignMacrosToSlots(macroImplementors);
			Assert.That(macros[0], Is.EqualTo(macroImplementors[2])); // the only one that wants to be at F2
			Assert.That(macros[2], Is.EqualTo(macroImplementors[0])); // first one that wants to be a F4
			Assert.That(macros[1], Is.EqualTo(macroImplementors[1])); // can't put where it wants to be, put it in first free slot.
		}

		/// <summary>
		/// Makes sure nothing too drastic happens if someone installs too many macro DLLs.
		/// </summary>
		[Test]
		public void InitMacro_SurvivesTooManyMacros()
		{
			var ml = new MacroListener();
			var macroImplementors = new List<IFlexMacro>();
			for (int i = 0; i < 20; i++)
				macroImplementors.Add(new MacroF4());
			var macros = ml.AssignMacrosToSlots(macroImplementors);
			Assert.That(macros[0], Is.EqualTo(macroImplementors[1])); // first free slot gets the second one
			Assert.That(macros[2], Is.EqualTo(macroImplementors[0])); // first one that wants to be a F4
			Assert.That(macros[1], Is.EqualTo(macroImplementors[2])); // can't put where it wants to be, put it in next free slot.
			Assert.That(macros[3], Is.EqualTo(macroImplementors[3])); // from here on they line up.
		}

		[Test]
		public void DoMacro_NormalPathSucceeds()
		{
			var ml = MakeMacroListenerWithCache();
			SetupF4Implementation(ml);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			m_actionHandler.EndUndoTask(); // running macro wants to make a new one.
			var sel = GetValidMockSelection(entry);
			using (var command = GetF4CommandObject())
			{
				ml.DoMacro(command, sel);

				Assert.That(entry.Restrictions.AnalysisDefaultWritingSystem.Text, Is.EqualTo("test succeeded"));
				Assert.That(m_actionHandler.GetUndoText(), Is.EqualTo("Undo F4test"));
			}
		}

		private MacroListener MakeMacroListenerWithCache()
		{
			m_Mediator = new Mediator();
			m_Mediator.PropertyTable.SetProperty("cache", Cache);
			var ml = new MacroListener();
			ml.Init(m_Mediator, null);
			return ml;
		}

		private static MockSelection GetValidMockSelection(ILexEntry entry)
		{
			var sel = new MockSelection();
			sel.TypeToReturn = VwSelType.kstText;
			sel.EndHvo = sel.AnchorHvo = entry.Hvo;
			sel.EndTag = sel.AnchorTag = LexEntryTags.kflidRestrictions; // arbitrary in this case
			sel.EndIch = 2;
			sel.AnchorIch = 5;
			return sel;
		}

		/// <summary>
		/// Initialize the listener into a state where there is a MacroF4 mock available to implement that command.
		/// </summary>
		/// <param name="ml"></param>
		/// <returns></returns>
		private static MacroF4 SetupF4Implementation(MacroListener ml)
		{
			var macroF4 = new MacroF4();
			var macroImplementors = new List<IFlexMacro>(new IFlexMacro[] {macroF4});
			ml.Macros = ml.AssignMacrosToSlots(macroImplementors);
			macroF4.BeEnabled = true;
			return macroF4;
		}

		/// <summary>
		/// Get a command object with the required XML to indicate the F4 key.
		/// </summary>
		/// <returns></returns>
		private static Command GetF4CommandObject()
		{
			var doc = new XmlDocument();
			doc.LoadXml(@"<command><params key='4'/></command>");
			var command = new Command(null, doc.DocumentElement);
			return command;
		}

		[Test]
		public void SafeToDoMacro_WithNoSelection_ReturnsFalse()
		{
			var ml = new MacroListener();
			int ichA, hvoA, flid, ws, ichE, start, length;
			ICmObject obj;
			Assert.That(ml.SafeToDoMacro(null, out obj, out flid, out ws, out start, out length), Is.False);
		}

		[Test]
		public void SafeToDoMacro_WithUnsuitableSelection_ReturnsFalse()
		{
			var ml = new MacroListener();
			var sel = new MockSelection();
			sel.EndHvo = sel.AnchorHvo = 317;
			sel.EndTag = sel.AnchorTag = LexEntryTags.kflidRestrictions; // arbitrary in this case
			sel.EndIch = 2;
			sel.AnchorIch = 5;
			int ichA, hvoA, flid, ws, ichE, start, length;
			ICmObject obj;

			Assert.That(ml.SafeToDoMacro(sel, out obj, out flid, out ws, out start, out length), Is.False); // wrong type of selection

			sel.TypeToReturn = VwSelType.kstText;
			sel.EndHvo = 316;
			Assert.That(ml.SafeToDoMacro(sel, out obj, out flid, out ws, out start, out length), Is.False); // different objects

			sel.EndHvo = sel.AnchorHvo;
			sel.EndTag = 3;
			Assert.That(ml.SafeToDoMacro(sel, out obj, out flid, out ws, out start, out length), Is.False); // different tags
		}

		[Test]
		public void DoDisplayMacro_NoMacro_HidesCommand()
		{
			var props = new UIItemDisplayProperties(null, "SomeMacro", true, null, false);
			var ml = new MacroListener();
			using (var command = GetF4CommandObject())
			{
				ml.DoDisplayMacro(command, props, null);
				Assert.That(props.Visible, Is.False); // no implementation of F4, hide it altogether.
			}
		}

		[Test]
		public void DoDisplayMacro_NoSelection_ShowsDisabledCommand()
		{
			var props = new UIItemDisplayProperties(null, "SomeMacro", true, null, false);
			var ml = new MacroListener();
			SetupF4Implementation(ml);
			using (var command = GetF4CommandObject())
			{
				ml.DoDisplayMacro(command, props, null);
				Assert.That(props.Visible, Is.True);
				Assert.That(props.Enabled, Is.False); // can't do it without a selection
				Assert.That(props.Text, Is.EqualTo("F4test"));
			}
		}

		[Test]
		public void DoDisplayMacro_WithSafeToDo_ResultDependsOnMacro()
		{
			var props = new UIItemDisplayProperties(null, "SomeMacro", true, null, false);
			var ml = MakeMacroListenerWithCache();
			var macro = SetupF4Implementation(ml);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var sel = GetValidMockSelection(entry);
			using (var command = GetF4CommandObject())
			{
				ml.DoDisplayMacro(command, props, sel);
				Assert.That(props.Visible, Is.True);
				Assert.That(props.Enabled, Is.True);
				Assert.That(props.Text, Is.EqualTo("F4test"));

				props = new UIItemDisplayProperties(null, "SomeMacro", true, null, false);
				macro.BeEnabled = false;
				ml.DoDisplayMacro(command, props, sel);
				Assert.That(props.Visible, Is.True);
				Assert.That(props.Enabled, Is.False); // can't do it if the macro says no good.
				Assert.That(props.Text, Is.EqualTo("F4test"));
			}
		}
	}

	/// <summary>
	/// Mock macro for F2 key. In this one we only use the PreferredFunctionKey.
	/// </summary>
	class MacroF2 : IFlexMacro
	{
		public string CommandName
		{
			get { throw new NotImplementedException(); }
		}

		public bool BeEnabled;
		public bool Enabled(ICmObject target, int targetField, int wsId, int start, int length)
		{
			return BeEnabled;
		}

		public void RunMacro(ICmObject target, int targetField, int wsId, int startOffset, int length)
		{
			throw new NotImplementedException();
		}

		public Keys PreferredFunctionKey
		{
			get { return Keys.F2; }
		}
	}

	/// <summary>
	/// A more complete mock for the F4 key. RunMacro sets a string property. This verifies that we are getting the unit of work
	/// into the required state.
	/// </summary>
	class MacroF4 : IFlexMacro
	{
		public string CommandName
		{
			get { return "F4test"; }
		}

		public bool BeEnabled;
		public bool Enabled(ICmObject target, int targetField, int wsId, int start, int length)
		{
			return BeEnabled;
		}

		public void RunMacro(ICmObject target, int targetField, int wsId, int startOffset, int length)
		{
			((ILexEntry)target).Restrictions.set_String(target.Cache.DefaultAnalWs,"test succeeded");
		}

		public Keys PreferredFunctionKey
		{
			get { return Keys.F4; }
		}
	}

	/// <summary>
	/// This class mocks the (unfortunately huge) IVwSelection. Only the first couple of methods are actually used by MacroListener,
	/// others are left unimplemented. Enhance JohnT: this would be better done with some sort of dynamic mock, so that changes to
	/// parts of the interface we don't care about won't have to be made here. But we haven't settled on a mock framework for FLEx.
	/// </summary>
	class MockSelection : IVwSelection
	{
		public VwSelType TypeToReturn;
		public VwSelType SelType
		{
			get { return TypeToReturn; }
		}

		public int EndHvo;
		public int AnchorHvo;
		public int EndTag;
		public int AnchorTag;
		public int EndWs;
		public int AnchorWs;
		public int EndIch;
		public int AnchorIch;

		public void TextSelInfo(bool fEndPoint, out ITsString ptss, out int ich, out bool fAssocPrev, out int hvoObj, out int tag, out int ws)
		{
			ptss = null;
			fAssocPrev = false;
			if (fEndPoint)
			{
				ich = EndIch;
				hvoObj = EndHvo;
				tag = EndTag;
				ws = EndWs;
			}
			else
			{
				ich = AnchorIch;
				hvoObj = AnchorHvo;
				tag = AnchorTag;
				ws = AnchorWs;
			}
		}

		public void GetSelectionProps(int cttpMax, ArrayPtr _rgpttp, ArrayPtr _rgpvps, out int _cttp)
		{
			throw new NotImplementedException();
		}

		public void GetHardAndSoftCharProps(int cttpMax, ArrayPtr _rgpttpSel, ArrayPtr _rgpvpsSoft, out int _cttp)
		{
			throw new NotImplementedException();
		}

		public void GetParaProps(int cttpMax, ArrayPtr _rgpvps, out int _cttp)
		{
			throw new NotImplementedException();
		}

		public void GetHardAndSoftParaProps(int cttpMax, ITsTextProps[] _rgpttpPara, ArrayPtr _rgpttpHard, ArrayPtr _rgpvpsSoft, out int _cttp)
		{
			throw new NotImplementedException();
		}

		public void SetSelectionProps(int cttp, ITsTextProps[] _rgpttp)
		{
			throw new NotImplementedException();
		}

		public int CLevels(bool fEndPoint)
		{
			throw new NotImplementedException();
		}

		public void PropInfo(bool fEndPoint, int ilev, out int _hvoObj, out int _tag, out int _ihvo, out int _cpropPrevious, out IVwPropertyStore _pvps)
		{
			throw new NotImplementedException();
		}

		public void AllTextSelInfo(out int _ihvoRoot, int cvlsi, ArrayPtr _rgvsli, out int _tagTextProp, out int _cpropPrevious, out int _ichAnchor, out int _ichEnd, out int _ws, out bool _fAssocPrev, out int _ihvoEnd, out ITsTextProps _pttp)
		{
			throw new NotImplementedException();
		}

		public void AllSelEndInfo(bool fEndPoint, out int _ihvoRoot, int cvlsi, ArrayPtr _rgvsli, out int _tagTextProp, out int _cpropPrevious, out int _ich, out int _ws, out bool _fAssocPrev, out ITsTextProps _pttp)
		{
			throw new NotImplementedException();
		}

		public bool CompleteEdits(out VwChangeInfo _ci)
		{
			throw new NotImplementedException();
		}

		public void ExtendToStringBoundaries()
		{
			throw new NotImplementedException();
		}

		public void Location(IVwGraphics _vg, Rect rcSrc, Rect rcDst, out Rect _rdPrimary, out Rect _rdSecondary, out bool _fSplit, out bool _fEndBeforeAnchor)
		{
			throw new NotImplementedException();
		}

		public void GetParaLocation(out Rect _rdLoc)
		{
			throw new NotImplementedException();
		}

		public void ReplaceWithTsString(ITsString _tss)
		{
			throw new NotImplementedException();
		}

		public void GetSelectionString(out ITsString _ptss, string bstrSep)
		{
			throw new NotImplementedException();
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
			get { throw new NotImplementedException(); }
		}

		public bool EndBeforeAnchor
		{
			get { throw new NotImplementedException(); }
		}

		public bool CanFormatPara
		{
			get { throw new NotImplementedException(); }
		}

		public bool CanFormatChar
		{
			get { throw new NotImplementedException(); }
		}

		public bool CanFormatOverlay
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsValid
		{
			get { throw new NotImplementedException(); }
		}

		public bool AssocPrev
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}


		public IVwRootBox RootBox
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsEditable
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsEnabled
		{
			get { throw new NotImplementedException(); }
		}
	}
}
