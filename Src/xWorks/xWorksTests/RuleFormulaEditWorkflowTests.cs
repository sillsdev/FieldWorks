// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 2.1/2.6) — T4 editable workflow over a real in-memory cache: a
	/// cell gesture on the LHS/RHS owning sequences routes through <see cref="RuleFormulaEditSink"/> into the
	/// region's fenced edit session, commits as ONE undoable step, re-projects (the handler's onModelChanged
	/// fires), and round-trips when re-projected fresh from domain truth — and a single Undo reverts the
	/// whole gesture. Proves the editing seam end-to-end, not merely that a setter returned true.
	/// </summary>
	[TestFixture]
	public class RuleFormulaEditWorkflowTests : MemoryOnlyBackendProviderTestBase
	{
		private IPhRegularRule m_rule;
		private IPhSegRuleRHS m_rhs;
		private IPhPhoneme m_p;
		private IPhPhoneme m_t;
		private IPhPhoneme m_k;
		private IPhNCSegments m_vowel;
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
				m_k = AddPhoneme("k");
				m_vowel = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(m_vowel);
				m_vowel.Abbreviation.SetAnalysisDefaultWritingSystem("V");
				m_wordBdry = Cache.ServiceLocator.GetInstance<IPhBdryMarkerFactory>()
					.Create(LangProjectTags.kguidPhRuleWordBdry, Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0]);
				m_wordBdry.Name.SetVernacularDefaultWritingSystem("#");

				m_rule = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Add(m_rule);
				AddSeg(m_rule.StrucDescOS, m_p);   // LHS = [p, t]
				AddSeg(m_rule.StrucDescOS, m_t);
				m_rhs = m_rule.RightHandSidesOS[0];
				AddNc(m_rhs.StrucChangeOS, m_vowel); // RHS = [V]
			});
		}

		private RuleFormulaEditSink NewSink(out List<RuleFormulaModel> reshows)
		{
			var captured = new List<RuleFormulaModel>();
			reshows = captured;
			// A real fenced host context (RegionEditContextBase) over the rule — empty setter dicts: the
			// sink stages through Stage()/Commit() directly, not the typed setters.
			var host = new ComposedRegionEditContext(Cache, m_rule,
				new Dictionary<string, Func<string, string, bool>>(),
				new Dictionary<string, Func<string, bool>>());
			return new RuleFormulaEditSink(m_rhs, Cache, host, m => captured.Add(m));
		}

		private string Lhs() => RuleFormulaProjector.ProjectRegularRule(m_rhs)
			.SectionFor(RuleSectionRole.Lhs).Cells.Aggregate("", (s, c) => s + c.DisplayText);

		[Test]
		public void DeleteCell_RemovesIt_CommitsOneUndoStep_AndUndoRestores()
		{
			var before = Cache.ActionHandlerAccessor.UndoableSequenceCount;
			var sink = NewSink(out var reshows);

			Assert.That(sink.DeleteCell(RuleSectionRole.Lhs, 0), Is.True);

			Assert.That(Lhs(), Is.EqualTo("t"), "the first LHS cell is gone (re-projected from domain truth)");
			Assert.That(reshows, Has.Count.EqualTo(1), "the gesture re-shows once");
			Assert.That(reshows[0].SectionFor(RuleSectionRole.Lhs).Cells.Single().DisplayText, Is.EqualTo("t"));
			Assert.That(Cache.ActionHandlerAccessor.UndoableSequenceCount, Is.EqualTo(before + 1),
				"the gesture is exactly one undoable step");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(Lhs(), Is.EqualTo("pt"), "one Undo restores the deleted cell");
		}

		[Test]
		public void InsertCell_AddsIt_AndRoundTrips()
		{
			var sink = NewSink(out _);

			Assert.That(sink.InsertCell(RuleSectionRole.Lhs, 1, new RuleCellSpec(RuleCellKind.Phoneme, m_k.Guid)),
				Is.True);

			Assert.That(Lhs(), Is.EqualTo("pkt"), "the inserted phoneme lands at index 1, re-projected from domain");
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(Lhs(), Is.EqualTo("pt"), "Undo removes the inserted cell");
		}

		[Test]
		public void InsertCell_RejectsUnresolvableTarget_StagesNothing()
		{
			var sink = NewSink(out var reshows);
			var before = Cache.ActionHandlerAccessor.UndoableSequenceCount;

			Assert.That(sink.InsertCell(RuleSectionRole.Lhs, 0, new RuleCellSpec(RuleCellKind.Phoneme, Guid.NewGuid())),
				Is.False, "an unknown target GUID is rejected");
			Assert.That(reshows, Is.Empty, "a rejected gesture re-shows nothing");
			Assert.That(Cache.ActionHandlerAccessor.UndoableSequenceCount, Is.EqualTo(before),
				"a rejected gesture opens no undo step");
			Assert.That(Lhs(), Is.EqualTo("pt"));
		}

		[Test]
		public void MoveCell_ReordersWithinTheSection_AndUndoRestores()
		{
			var sink = NewSink(out _);

			Assert.That(sink.MoveCell(RuleSectionRole.Lhs, 0, 1), Is.True);
			Assert.That(Lhs(), Is.EqualTo("tp"), "p moves after t");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(Lhs(), Is.EqualTo("pt"), "one Undo restores the original order");
		}

		private string Section(RuleSectionRole role) => RuleFormulaProjector.ProjectRegularRule(m_rhs)
			.SectionFor(role).Cells.Aggregate("", (s, c) => s + c.DisplayText);

		[Test]
		public void LeftContext_AtomicToSequenceAndBack_RoundTrips()
		{
			var sink = NewSink(out _);
			// 0 → 1: empty context accepts a single cell (stored as the atomic OA).
			Assert.That(sink.InsertCell(RuleSectionRole.LeftContext, 0, new RuleCellSpec(RuleCellKind.Phoneme, m_p.Guid)), Is.True);
			Assert.That(Section(RuleSectionRole.LeftContext), Is.EqualTo("p"));
			Assert.That(m_rhs.LeftContextOA, Is.InstanceOf<IPhSimpleContextSeg>(), "one cell is the atomic single context");

			// 1 → 2: the second cell promotes the atomic into a PhSequenceContext.
			Assert.That(sink.InsertCell(RuleSectionRole.LeftContext, 1, new RuleCellSpec(RuleCellKind.Phoneme, m_t.Guid)), Is.True);
			Assert.That(Section(RuleSectionRole.LeftContext), Is.EqualTo("pt"));
			Assert.That(m_rhs.LeftContextOA, Is.InstanceOf<IPhSequenceContext>(), "two cells promote to a sequence");

			// 2 → 1: deleting a member leaves the (1-member) sequence — projector still flattens to one cell.
			Assert.That(sink.DeleteCell(RuleSectionRole.LeftContext, 0), Is.True);
			Assert.That(Section(RuleSectionRole.LeftContext), Is.EqualTo("t"));

			// 1 → 0: deleting the last cell clears the context.
			Assert.That(sink.DeleteCell(RuleSectionRole.LeftContext, 0), Is.True);
			Assert.That(Section(RuleSectionRole.LeftContext), Is.Empty);
		}

		[Test]
		public void RightContext_Insert_ThenUndo_Restores()
		{
			var sink = NewSink(out _);
			Assert.That(sink.InsertCell(RuleSectionRole.RightContext, 0, new RuleCellSpec(RuleCellKind.Boundary, m_wordBdry.Guid)), Is.True);
			Assert.That(Section(RuleSectionRole.RightContext), Is.EqualTo("#"));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(Section(RuleSectionRole.RightContext), Is.Empty, "one Undo clears the inserted context cell");
		}

		// ----- construction helpers -----

		private IPhPhoneme AddPhoneme(string name)
		{
			var ph = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(ph);
			ph.Name.SetVernacularDefaultWritingSystem(name);
			return ph;
		}

		private void AddSeg(ILcmOwningSequence<IPhSimpleContext> seq, IPhPhoneme phoneme)
		{
			var ctx = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			seq.Add(ctx);
			ctx.FeatureStructureRA = phoneme;
		}

		private void AddNc(ILcmOwningSequence<IPhSimpleContext> seq, IPhNaturalClass nc)
		{
			var ctx = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			seq.Add(ctx);
			ctx.FeatureStructureRA = nc;
		}
	}
}
