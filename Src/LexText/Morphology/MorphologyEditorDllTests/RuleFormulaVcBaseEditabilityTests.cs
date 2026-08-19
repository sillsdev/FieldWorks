// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.LexText.Controls;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// The rule formula view is modifiable only by chooser-insert and delete, so each formula
	/// marks its outermost table not editable and lets ktptEditable inherit down the box tree.
	/// These tests pin that marking, pin the boundary spans being put back to editable so the
	/// cursor still has a position, and record which fragments bind straight to a referenced
	/// object's own live string field -- the write channel LT-22710 was about.
	/// </summary>
	[TestFixture]
	public class RuleFormulaVcBaseEditabilityTests : MemoryOnlyBackendProviderTestBase
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;

		public override void TestSetup()
		{
			base.TestSetup();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
		}

		public override void TestTearDown()
		{
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}
			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}
			base.TestTearDown();
		}

		private IPhNaturalClass CreateNaturalClass(string abbr)
		{
			IPhNaturalClass nc = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				nc = Cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(nc);
				nc.Name.SetAnalysisDefaultWritingSystem("Test Class");
				nc.Abbreviation.SetAnalysisDefaultWritingSystem(abbr);
			});
			return nc;
		}

		private IPhPhoneme CreatePhoneme(string name)
		{
			IPhPhoneme p = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				p = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(p);
				p.Name.SetVernacularDefaultWritingSystem(name);
			});
			return p;
		}

		/// <summary>
		/// The natural-class abbreviation fragment (kfragNC) is rendered via AddStringAltMember
		/// directly against the natural class's own Abbreviation field.
		/// </summary>
		[Test]
		public void Display_NaturalClassAbbreviationFragment_BindsDirectlyToTheReferencedField()
		{
			IPhNaturalClass nc = CreateNaturalClass("Stp");
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, nc.Hvo, RuleFormulaVcBase.kfragNC);

			Assert.That(env.StringAltMemberCalls, Is.Not.Empty,
				"expected AddStringAltMember to be called for the NC abbreviation fragment");
		}

		/// <summary>
		/// The terminal-unit (phoneme/boundary) fragment (kfragTerminalUnit) is rendered via
		/// AddStringAltMember directly against the terminal unit's own Name field -- a live
		/// write channel into the phoneme's real, project-wide name.
		/// </summary>
		[Test]
		public void Display_TerminalUnitNameFragment_BindsDirectlyToTheReferencedField()
		{
			IPhPhoneme phoneme = CreatePhoneme("p");
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, phoneme.Hvo, RuleFormulaVcBase.kfragTerminalUnit);

			Assert.That(env.StringAltMemberCalls, Is.Not.Empty,
				"expected AddStringAltMember to be called for the terminal unit name fragment");
		}

		/// <summary>Same defect, exercised through the metathesis-rule view
		/// constructor.</summary>
		[Test]
		public void Display_TerminalUnitNameFragment_ViaMetaRuleFormulaVc_BindsDirectlyToTheReferencedField()
		{
			IPhPhoneme phoneme = CreatePhoneme("t");
			var vc = new MetaRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, phoneme.Hvo, RuleFormulaVcBase.kfragTerminalUnit);

			Assert.That(env.StringAltMemberCalls, Is.Not.Empty);
		}

		/// <summary>Same defect, exercised through the affix-process view constructor.</summary>
		[Test]
		public void Display_TerminalUnitNameFragment_ViaAffixRuleFormulaVc_BindsDirectlyToTheReferencedField()
		{
			IPhPhoneme phoneme = CreatePhoneme("k");
			var vc = new AffixRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, phoneme.Hvo, RuleFormulaVcBase.kfragTerminalUnit);

			Assert.That(env.StringAltMemberCalls, Is.Not.Empty);
		}

		/// <summary>
		/// The feature-value line (kfragFeature) is a computed "abbreviation value" string bound
		/// to a fake tag, not free text.
		/// </summary>
		[Test]
		public void Display_FeatureLineFragment_BindsDirectlyToTheReferencedField()
		{
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, 0, RuleFormulaVcBase.kfragFeature);

			Assert.That(env.AddPropCalls, Is.Not.Empty,
				"expected AddProp to be called for the feature-line fragment");
		}

		/// <summary>The plus-variable line (kfragPlusVariable) is a computed string bound to a
		/// fake tag, not free text.</summary>
		[Test]
		public void Display_PlusVariableLineFragment_BindsDirectlyToTheReferencedField()
		{
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, 0, RuleFormulaVcBase.kfragPlusVariable);

			Assert.That(env.AddPropCalls, Is.Not.Empty,
				"expected AddProp to be called for the plus-variable fragment");
		}

		/// <summary>The minus-variable line (kfragMinusVariable) is a computed string bound to a
		/// fake tag, not free text.</summary>
		[Test]
		public void Display_MinusVariableLineFragment_BindsDirectlyToTheReferencedField()
		{
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, 0, RuleFormulaVcBase.kfragMinusVariable);

			Assert.That(env.AddPropCalls, Is.Not.Empty,
				"expected AddProp to be called for the minus-variable fragment");
		}

		/// <summary>
		/// Every rule formula must mark itself not editable on its outermost table, because
		/// ktptEditable inherits down the box tree and that is what covers the referenced
		/// phoneme and natural-class fields, the computed lines, the bracket glyphs and the
		/// fake-tag spans in one place.
		/// </summary>
		/// <remarks>
		/// Display is driven only as far as the first OpenTable; the fixture's IVwEnv throws on
		/// the member reached next, which is the point of the expected exception.
		/// </remarks>
		[TestCase("Reg")]
		[TestCase("Meta")]
		[TestCase("Affix")]
		public void Display_RuleFormulaRootFragment_MarksTheWholeTableNotEditable(string ruleKind)
		{
			var env = new EditabilityRecordingEnv();
			int rootFrag;
			RuleFormulaVcBase vc = BuildVcForRootFragment(ruleKind, out rootFrag, out int rootHvo);

			Assert.Throws<NotImplementedException>(() => vc.Display(env, rootHvo, rootFrag),
				"expected Display to run past the outermost OpenTable and then hit an "
				+ "unimplemented IVwEnv member");

			Assert.That(env.OpenTableCalls, Is.Not.Empty,
				"expected the formula to open its outermost table");
			Assert.That(env.OpenTableCalls[0].EditableAtCallTime,
				Is.EqualTo((int)TptEditable.ktptNotEditable),
				"the outermost table must already be marked not editable, so that every string "
				+ "inside the formula inherits it");
		}

		/// <summary>
		/// The zero-width-space boundary spans must be put back to editable inside a formula
		/// that is not editable as a whole, because clicking an item and every insert and
		/// delete place the cursor with fEditable true and need somewhere to land.
		/// </summary>
		[Test]
		public void OpenAndCloseSingleLinePile_MarkTheBoundarySpansEditable()
		{
			var env = new EditabilityRecordingEnv();
			var vc = new BoundaryProbeVc(Cache, m_propertyTable);

			vc.ProbeBoundaries(env);

			var boundaryCalls = env.AddPropCalls
				.Where(call => call.Tag == PatternVcBase.ktagLeftBoundary
					|| call.Tag == PatternVcBase.ktagRightBoundary)
				.ToList();
			Assert.That(boundaryCalls, Has.Count.EqualTo(2),
				"expected both the left and the right boundary span");
			foreach (var call in boundaryCalls)
			{
				Assert.That(call.EditableAtCallTime, Is.EqualTo((int)TptEditable.ktptIsEditable),
					"the boundary span must stay editable so the cursor has a position in the "
					+ "cell; ktagLeftBoundary and ktagRightBoundary are fake tags, so an edit "
					+ "landing here reaches no model data");
			}
		}

		private RuleFormulaVcBase BuildVcForRootFragment(string ruleKind, out int rootFrag,
			out int rootHvo)
		{
			IPhSegRuleRHS rhs = null;
			IMoAffixProcess affixRule = null;
			IPhMetathesisRule metathesisRule = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				switch (ruleKind)
				{
					case "Reg":
						var regRule = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>().Create();
						Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Add(regRule);
						rhs = Cache.ServiceLocator.GetInstance<IPhSegRuleRHSFactory>().Create();
						regRule.RightHandSidesOS.Add(rhs);
						break;
					case "Meta":
						metathesisRule = Cache.ServiceLocator.GetInstance<IPhMetathesisRuleFactory>().Create();
						Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Add(metathesisRule);
						break;
					default:
						// A MoAffixProcess has to be owned as a lexical entry's form.
						var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
						affixRule = Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create();
						entry.LexemeFormOA = affixRule;
						break;
				}
			});

			switch (ruleKind)
			{
				case "Reg":
					rootFrag = RegRuleFormulaVc.kfragRHS;
					rootHvo = rhs.Hvo;
					return new RegRuleFormulaVc(Cache, m_propertyTable);
				case "Meta":
					rootFrag = MetaRuleFormulaVc.kfragRule;
					rootHvo = metathesisRule.Hvo;
					return new MetaRuleFormulaVc(Cache, m_propertyTable);
				default:
					rootFrag = AffixRuleFormulaVc.kfragRule;
					rootHvo = affixRule.Hvo;
					return new AffixRuleFormulaVc(Cache, m_propertyTable);
			}
		}

		/// <summary>Reaches the protected pile helpers so the boundary spans can be observed
		/// without building a whole rule.</summary>
		private class BoundaryProbeVc : RegRuleFormulaVc
		{
			public BoundaryProbeVc(LcmCache cache, PropertyTable propertyTable)
				: base(cache, propertyTable)
			{
			}

			public void ProbeBoundaries(IVwEnv vwenv)
			{
				OpenSingleLinePile(vwenv, 1);
				CloseSingleLinePile(vwenv);
			}
		}

		/// <summary>
		/// Records enough of IVwEnv's calls to observe which tag a fragment binds to, and what
		/// the ktptEditable property was most recently set to when a table is opened or a
		/// boundary span is added. All other members are unused by the fragments under test and
		/// throw if hit, so a future change that routes through a different IVwEnv member will
		/// fail loudly rather than silently pass.
		/// </summary>
		private class EditabilityRecordingEnv : IVwEnv
		{
			public struct Call
			{
				public int Tag;
				public int Ws;
				public int EditableAtCallTime;
			}

			public struct PropCall
			{
				public int Tag;
				public int Frag;
				public int EditableAtCallTime;
			}

			public struct OpenTableCall
			{
				public int EditableAtCallTime;
			}

			public List<Call> StringAltMemberCalls = new List<Call>();
			public List<PropCall> AddPropCalls = new List<PropCall>();
			public List<OpenTableCall> OpenTableCalls = new List<OpenTableCall>();

			private int m_currentEditable = int.MinValue; // sentinel: never set

			public void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
			{
				StringAltMemberCalls.Add(new Call { Tag = tag, Ws = ws, EditableAtCallTime = m_currentEditable });
			}

			public void AddProp(int tag, IVwViewConstructor _vwvc, int frag)
			{
				AddPropCalls.Add(new PropCall { Tag = tag, Frag = frag, EditableAtCallTime = m_currentEditable });
			}

			public void set_IntProperty(int tpt, int tpv, int nValue)
			{
				if (tpt == (int)FwTextPropType.ktptEditable)
					m_currentEditable = nValue;
			}

			public ITsTextProps Props
			{
				set { /* the recorded editable state comes from set_IntProperty, not from whole props */ }
			}

			public void get_StringWidth(ITsString _tss, ITsTextProps _ttp, out int dmpx, out int dmpy)
			{
				dmpx = 0;
				dmpy = 0;
			}

			public int OpenObject
			{
				get { throw new NotImplementedException(); }
			}

			public int EmbeddingLevel
			{
				get { return 0; }
			}

			public ISilDataAccess DataAccess
			{
				get { throw new NotImplementedException(); }
			}

			public void AddObjProp(int tag, IVwViewConstructor _vwvc, int frag) { throw new NotImplementedException(); }
			public void AddObjVec(int tag, IVwViewConstructor _vwvc, int frag) { throw new NotImplementedException(); }
			public void AddObjVecItems(int tag, IVwViewConstructor _vwvc, int frag) { throw new NotImplementedException(); }
			public void AddReversedObjVecItems(int tag, IVwViewConstructor _vwvc, int frag) { throw new NotImplementedException(); }
			public void AddObj(int hvo, IVwViewConstructor _vwvc, int frag) { throw new NotImplementedException(); }
			public void AddLazyVecItems(int tag, IVwViewConstructor _vwvc, int frag) { throw new NotImplementedException(); }
			public void AddLazyItems(int[] _rghvo, int chvo, IVwViewConstructor _vwvc, int frag) { throw new NotImplementedException(); }
			public void AddDerivedProp(int[] _rgtag, int ctag, IVwViewConstructor _vwvc, int frag) { throw new NotImplementedException(); }
			public void NoteDependency(int[] _rghvo, int[] _rgtag, int chvo) { }
			public void NoteStringValDependency(int hvo, int tag, int ws, ITsString _tssVal) { throw new NotImplementedException(); }
			public void AddStringProp(int tag, IVwViewConstructor _vwvc) { throw new NotImplementedException(); }
			public void AddUnicodeProp(int tag, int ws, IVwViewConstructor _vwvc) { throw new NotImplementedException(); }
			public void AddIntProp(int tag) { throw new NotImplementedException(); }
			public void AddIntPropPic(int tag, IVwViewConstructor _vc, int frag, int nMin, int nMax) { throw new NotImplementedException(); }
			public void AddStringAlt(int tag) { throw new NotImplementedException(); }
			public void AddStringAltSeq(int tag, int[] _rgenc, int cws) { throw new NotImplementedException(); }
			public void AddString(ITsString _ss) { throw new NotImplementedException(); }
			public void AddTimeProp(int tag, uint flags) { throw new NotImplementedException(); }
			public int CurrentObject() { throw new NotImplementedException(); }
			public void GetOuterObject(int ichvoLevel, out int _hvo, out int _tag, out int _ihvo) { throw new NotImplementedException(); }
			public void AddWindow(IVwEmbeddedWindow _ew, int dmpAscent, bool fJustifyRight, bool fAutoShow) { throw new NotImplementedException(); }
			public void AddSeparatorBar() { throw new NotImplementedException(); }
			public void AddSimpleRect(int rgb, int dmpWidth, int dmpHeight, int dmpBaselineOffset) { throw new NotImplementedException(); }
			public void OpenDiv() { throw new NotImplementedException(); }
			public void CloseDiv() { throw new NotImplementedException(); }
			public void OpenParagraph() { }
			public void OpenTaggedPara() { throw new NotImplementedException(); }
			public void OpenMappedPara() { throw new NotImplementedException(); }
			public void OpenMappedTaggedPara() { throw new NotImplementedException(); }
			public void OpenConcPara(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags, int dmpAlign) { throw new NotImplementedException(); }
			public void OpenOverridePara(int cOverrideProperties, DispPropOverride[] _rgOverrideProperties) { throw new NotImplementedException(); }
			public void CloseParagraph() { }
			public void OpenInnerPile() { }
			public void CloseInnerPile() { }
			public void OpenSpan() { throw new NotImplementedException(); }
			public void CloseSpan() { throw new NotImplementedException(); }
			public void OpenTable(int cCols, VwLength vlWidth, int mpBorder, VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule, int mpSpacing, int mpPadding, bool fSelectOneCol)
			{
				OpenTableCalls.Add(new OpenTableCall { EditableAtCallTime = m_currentEditable });
			}
			public void CloseTable() { throw new NotImplementedException(); }
			public void OpenTableRow() { throw new NotImplementedException(); }
			public void CloseTableRow() { throw new NotImplementedException(); }
			public void OpenTableCell(int nRowSpan, int nColSpan) { throw new NotImplementedException(); }
			public void CloseTableCell() { throw new NotImplementedException(); }
			public void OpenTableHeaderCell(int nRowSpan, int nColSpan) { throw new NotImplementedException(); }
			public void CloseTableHeaderCell() { throw new NotImplementedException(); }
			public void MakeColumns(int nColSpan, VwLength vlWidth) { throw new NotImplementedException(); }
			public void MakeColumnGroup(int nColSpan, VwLength vlWidth) { throw new NotImplementedException(); }
			public void OpenTableHeader() { throw new NotImplementedException(); }
			public void CloseTableHeader() { throw new NotImplementedException(); }
			public void OpenTableFooter() { throw new NotImplementedException(); }
			public void CloseTableFooter() { throw new NotImplementedException(); }
			public void OpenTableBody() { throw new NotImplementedException(); }
			public void CloseTableBody() { throw new NotImplementedException(); }
			public void set_StringProperty(int sp, string bstrValue) { }
			public void AddPictureWithCaption(IPicture _pict, int tag, ITsTextProps _ttpCaption, int hvoCmFile, int ws, int dxmpWidth, int dympHeight, IVwViewConstructor _vwvc) { throw new NotImplementedException(); }
			public void AddPicture(IPicture _pict, int tag, int dxmpWidth, int dympHeight) { throw new NotImplementedException(); }
			public void SetParagraphMark(VwBoundaryMark boundaryMark) { throw new NotImplementedException(); }
			public void EmptyParagraphBehavior(int behavior) { throw new NotImplementedException(); }
			public bool IsParagraphOpen() { throw new NotImplementedException(); }
		}
	}
}
