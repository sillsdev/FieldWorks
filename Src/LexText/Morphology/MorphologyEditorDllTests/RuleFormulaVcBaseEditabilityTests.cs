// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.Common.ViewsInterfaces;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// The rule formula view is modifiable only by chooser-insert and delete, so
	/// RuleFormulaVcBase must mark the natural-class abbreviation and terminal-unit
	/// (phoneme/boundary) name fragments non-editable; both bind directly to the
	/// referenced object's own live string field.
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
		public void Display_NaturalClassAbbreviationFragment_IsMarkedNotEditable()
		{
			IPhNaturalClass nc = CreateNaturalClass("Stp");
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, nc.Hvo, RuleFormulaVcBase.kfragNC);

			Assert.That(env.StringAltMemberCalls, Is.Not.Empty,
				"expected AddStringAltMember to be called for the NC abbreviation fragment");
			foreach (var call in env.StringAltMemberCalls)
			{
				Assert.That(call.EditableAtCallTime, Is.EqualTo((int)TptEditable.ktptNotEditable),
					"the natural class abbreviation must not be directly editable in the rule formula view");
			}
		}

		/// <summary>
		/// The terminal-unit (phoneme/boundary) fragment (kfragTerminalUnit) is rendered via
		/// AddStringAltMember directly against the terminal unit's own Name field -- a live
		/// write channel into the phoneme's real, project-wide name.
		/// </summary>
		[Test]
		public void Display_TerminalUnitNameFragment_IsMarkedNotEditable()
		{
			IPhPhoneme phoneme = CreatePhoneme("p");
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, phoneme.Hvo, RuleFormulaVcBase.kfragTerminalUnit);

			Assert.That(env.StringAltMemberCalls, Is.Not.Empty,
				"expected AddStringAltMember to be called for the terminal unit name fragment");
			foreach (var call in env.StringAltMemberCalls)
			{
				Assert.That(call.EditableAtCallTime, Is.EqualTo((int)TptEditable.ktptNotEditable),
					"the phoneme/boundary name must not be directly editable in the rule formula view");
			}
		}

		/// <summary>Same defect, exercised through the metathesis-rule view constructor.</summary>
		[Test]
		public void Display_TerminalUnitNameFragment_ViaMetaRuleFormulaVc_IsMarkedNotEditable()
		{
			IPhPhoneme phoneme = CreatePhoneme("t");
			var vc = new MetaRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, phoneme.Hvo, RuleFormulaVcBase.kfragTerminalUnit);

			Assert.That(env.StringAltMemberCalls, Is.Not.Empty);
			foreach (var call in env.StringAltMemberCalls)
			{
				Assert.That(call.EditableAtCallTime, Is.EqualTo((int)TptEditable.ktptNotEditable),
					"metathesis rule formula view shares the same defect as the base class");
			}
		}

		/// <summary>Same defect, exercised through the affix-process view constructor.</summary>
		[Test]
		public void Display_TerminalUnitNameFragment_ViaAffixRuleFormulaVc_IsMarkedNotEditable()
		{
			IPhPhoneme phoneme = CreatePhoneme("k");
			var vc = new AffixRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, phoneme.Hvo, RuleFormulaVcBase.kfragTerminalUnit);

			Assert.That(env.StringAltMemberCalls, Is.Not.Empty);
			foreach (var call in env.StringAltMemberCalls)
			{
				Assert.That(call.EditableAtCallTime, Is.EqualTo((int)TptEditable.ktptNotEditable),
					"affix process rule formula view shares the same defect as the base class");
			}
		}

		/// <summary>
		/// The feature-value line (kfragFeature) is a computed "abbreviation value" string bound
		/// to a fake tag, not free text.
		/// </summary>
		[Test]
		public void Display_FeatureLineFragment_IsMarkedNotEditable()
		{
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, 0, RuleFormulaVcBase.kfragFeature);

			Assert.That(env.AddPropCalls, Is.Not.Empty,
				"expected AddProp to be called for the feature-line fragment");
			foreach (var call in env.AddPropCalls)
			{
				Assert.That(call.EditableAtCallTime, Is.EqualTo((int)TptEditable.ktptNotEditable),
					"the feature-value line must not be directly editable in the rule formula view");
			}
		}

		/// <summary>The plus-variable line (kfragPlusVariable) is a computed string bound to a
		/// fake tag, not free text.</summary>
		[Test]
		public void Display_PlusVariableLineFragment_IsMarkedNotEditable()
		{
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, 0, RuleFormulaVcBase.kfragPlusVariable);

			Assert.That(env.AddPropCalls, Is.Not.Empty,
				"expected AddProp to be called for the plus-variable fragment");
			foreach (var call in env.AddPropCalls)
			{
				Assert.That(call.EditableAtCallTime, Is.EqualTo((int)TptEditable.ktptNotEditable),
					"the plus-variable line must not be directly editable in the rule formula view");
			}
		}

		/// <summary>The minus-variable line (kfragMinusVariable) is a computed string bound to a
		/// fake tag, not free text.</summary>
		[Test]
		public void Display_MinusVariableLineFragment_IsMarkedNotEditable()
		{
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new EditabilityRecordingEnv();

			vc.Display(env, 0, RuleFormulaVcBase.kfragMinusVariable);

			Assert.That(env.AddPropCalls, Is.Not.Empty,
				"expected AddProp to be called for the minus-variable fragment");
			foreach (var call in env.AddPropCalls)
			{
				Assert.That(call.EditableAtCallTime, Is.EqualTo((int)TptEditable.ktptNotEditable),
					"the minus-variable line must not be directly editable in the rule formula view");
			}
		}

		/// <summary>
		/// Records enough of IVwEnv's calls to observe, at the moment AddStringAltMember binds a
		/// fragment to a real domain-object field, whether the view constructor had most recently
		/// set the ktptEditable property to NotEditable. All other members are unused by the
		/// fragments under test and throw if hit, so a future change that routes through a
		/// different IVwEnv member will fail loudly rather than silently pass.
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

			public List<Call> StringAltMemberCalls = new List<Call>();
			public List<PropCall> AddPropCalls = new List<PropCall>();

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
				set { /* not relevant to editability of these two fragments */ }
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
			public void OpenParagraph() { throw new NotImplementedException(); }
			public void OpenTaggedPara() { throw new NotImplementedException(); }
			public void OpenMappedPara() { throw new NotImplementedException(); }
			public void OpenMappedTaggedPara() { throw new NotImplementedException(); }
			public void OpenConcPara(int ichMinItem, int ichLimItem, VwConcParaOpts cpoFlags, int dmpAlign) { throw new NotImplementedException(); }
			public void OpenOverridePara(int cOverrideProperties, DispPropOverride[] _rgOverrideProperties) { throw new NotImplementedException(); }
			public void CloseParagraph() { throw new NotImplementedException(); }
			public void OpenInnerPile() { throw new NotImplementedException(); }
			public void CloseInnerPile() { throw new NotImplementedException(); }
			public void OpenSpan() { throw new NotImplementedException(); }
			public void CloseSpan() { throw new NotImplementedException(); }
			public void OpenTable(int cCols, VwLength vlWidth, int mpBorder, VwAlignment vwalign, VwFramePosition frmpos, VwRule vwrule, int mpSpacing, int mpPadding, bool fSelectOneCol) { throw new NotImplementedException(); }
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
			public void set_StringProperty(int sp, string bstrValue) { throw new NotImplementedException(); }
			public void AddPictureWithCaption(IPicture _pict, int tag, ITsTextProps _ttpCaption, int hvoCmFile, int ws, int dxmpWidth, int dympHeight, IVwViewConstructor _vwvc) { throw new NotImplementedException(); }
			public void AddPicture(IPicture _pict, int tag, int dxmpWidth, int dympHeight) { throw new NotImplementedException(); }
			public void SetParagraphMark(VwBoundaryMark boundaryMark) { throw new NotImplementedException(); }
			public void EmptyParagraphBehavior(int behavior) { throw new NotImplementedException(); }
			public bool IsParagraphOpen() { throw new NotImplementedException(); }
		}
	}
}
