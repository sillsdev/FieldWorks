// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 1.2) — T1 parity test for <see cref="RuleFormulaProjector"/>.
	/// Builds known regular rules in an in-memory cache and asserts the projected formula equals a
	/// HAND-WRITTEN oracle string derived from the legacy <c>RegRuleFormulaVc</c>/<c>RuleFormulaVcBase</c>
	/// layout (LHS → RHS / LeftCtx __ RightCtx; phonemes/boundaries bare, natural classes bracketed). The
	/// oracle is independent of the properties the projector reads, so a green test means the projector
	/// genuinely walks the rule structure correctly (advisor-confirmed anti-circular design).
	/// </summary>
	[TestFixture]
	public class RuleFormulaProjectorTests : MemoryOnlyBackendProviderTestBase
	{
		private IPhPhoneme m_p;
		private IPhPhoneme m_t;
		private IPhNCSegments m_vowel;
		private IPhNCSegments m_cons;
		private IPhBdryMarker m_wordBdry;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				m_p = AddPhoneme("p");
				m_t = AddPhoneme("t");
				m_vowel = AddNaturalClass("V");
				m_cons = AddNaturalClass("C");
				m_wordBdry = AddBoundary(LangProjectTags.kguidPhRuleWordBdry, "#");
			});
		}

		[Test]
		public void ProjectRegularRule_RendersCanonicalFormula()
		{
			IPhSegRuleRHS rhs = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var rule = NewRegularRule();             // p → [V] / [C] __ #
				AddSeg(rule.StrucDescOS, m_p);           // LHS
				rhs = rule.RightHandSidesOS[0];
				AddNc(rhs.StrucChangeOS, m_vowel);       // RHS

				// Owning-atomic contexts: assign the property FIRST, then set the reference (HCLoaderTests pattern).
				var leftNc = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
				rhs.LeftContextOA = leftNc;
				leftNc.FeatureStructureRA = m_cons;
				var rightBdry = Cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
				rhs.RightContextOA = rightBdry;
				rightBdry.FeatureStructureRA = m_wordBdry;
			});

			var model = RuleFormulaProjector.ProjectRegularRule(rhs);

			Assert.That(model.RuleKind, Is.EqualTo(RuleFormulaProjector.RegularRuleKind));
			Assert.That(model.ToFormulaString(), Is.EqualTo("p → [V] / [C] __ #"));

			// Structural assertions (kind + carried target), independent of the formula string.
			var lhs = model.SectionFor(RuleSectionRole.Lhs);
			Assert.That(lhs.Cells, Has.Count.EqualTo(1));
			Assert.That(lhs.Cells[0].Kind, Is.EqualTo(RuleCellKind.Phoneme));
			Assert.That(lhs.Cells[0].TargetGuid, Is.EqualTo(m_p.Guid));
			Assert.That(model.SectionFor(RuleSectionRole.Rhs).Cells[0].Kind, Is.EqualTo(RuleCellKind.NaturalClass));
			Assert.That(model.SectionFor(RuleSectionRole.Rhs).Cells[0].TargetGuid, Is.EqualTo(m_vowel.Guid));
			Assert.That(model.SectionFor(RuleSectionRole.RightContext).Cells[0].Kind, Is.EqualTo(RuleCellKind.Boundary));
		}

		[Test]
		public void ProjectRegularRule_FlattensSequenceContext_AndKeepsEmptySections()
		{
			IPhSegRuleRHS rhs = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var rule = NewRegularRule();             // p →  / #t __  (empty RHS + empty right context)
				AddSeg(rule.StrucDescOS, m_p);
				rhs = rule.RightHandSidesOS[0];

				var seq = Cache.ServiceLocator.GetInstance<IPhSequenceContextFactory>().Create();
				rhs.LeftContextOA = seq;
				AddBdry(seq.MembersRS, m_wordBdry);
				AddSeg(seq.MembersRS, m_t);
				// RHS StrucChange + RightContext intentionally left empty.
			});

			var model = RuleFormulaProjector.ProjectRegularRule(rhs);

			Assert.That(model.ToFormulaString(), Is.EqualTo("p →  / #t __ "));
			Assert.That(model.SectionFor(RuleSectionRole.Rhs).Cells, Is.Empty);
			Assert.That(model.SectionFor(RuleSectionRole.RightContext).Cells, Is.Empty);

			var left = model.SectionFor(RuleSectionRole.LeftContext);
			Assert.That(left.Cells, Has.Count.EqualTo(2), "the sequence context flattens into its members");
			Assert.That(left.Cells[0].Kind, Is.EqualTo(RuleCellKind.Boundary));
			Assert.That(left.Cells[1].Kind, Is.EqualTo(RuleCellKind.Phoneme));
		}

		#region construction helpers (mirrors HCLoaderTests)

		private IPhRegularRule NewRegularRule()
		{
			var rule = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Add(rule);   // factory seeds RightHandSidesOS[0]
			return rule;
		}

		private IPhPhoneme AddPhoneme(string name)
		{
			var ph = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(ph);
			ph.Name.SetVernacularDefaultWritingSystem(name);
			return ph;
		}

		private IPhNCSegments AddNaturalClass(string abbr)
		{
			var nc = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(nc);
			nc.Abbreviation.SetAnalysisDefaultWritingSystem(abbr);
			nc.Name.SetAnalysisDefaultWritingSystem(abbr);
			return nc;
		}

		private IPhBdryMarker AddBoundary(Guid guid, string rep)
		{
			var bdry = Cache.ServiceLocator.GetInstance<IPhBdryMarkerFactory>()
				.Create(guid, Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0]);
			bdry.Name.SetVernacularDefaultWritingSystem(rep);
			return bdry;
		}

		private void AddSeg(ILcmOwningSequence<IPhSimpleContext> seq, IPhPhoneme phoneme)
		{
			var ctx = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			seq.Add(ctx);
			ctx.FeatureStructureRA = phoneme;
		}

		private void AddSeg(ILcmReferenceSequence<IPhPhonContext> seq, IPhPhoneme phoneme)
		{
			var ctx = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ctx);
			ctx.FeatureStructureRA = phoneme;
			seq.Add(ctx);
		}

		private void AddNc(ILcmOwningSequence<IPhSimpleContext> seq, IPhNaturalClass nc)
		{
			var ctx = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			seq.Add(ctx);
			ctx.FeatureStructureRA = nc;
		}

		private void AddBdry(ILcmReferenceSequence<IPhPhonContext> seq, IPhBdryMarker bdry)
		{
			var ctx = Cache.ServiceLocator.GetInstance<IPhSimpleContextBdryFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.ContextsOS.Add(ctx);
			ctx.FeatureStructureRA = bdry;
			seq.Add(ctx);
		}

		#endregion
	}
}
