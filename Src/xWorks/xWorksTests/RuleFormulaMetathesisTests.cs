// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 2.3) — T1 parity + T2 compose for the metathesis rule editor.
	/// A metathesis rule partitions one <c>StrucDescOS</c> into four cells (left env | left switch ↔ right
	/// switch | right env); the projector buckets by <c>GetStrucChangeIndex</c>. The hand-written oracle
	/// "[V] | a ↔ t | [C]" is independent of the projector's reads (anti-circular).
	/// </summary>
	[TestFixture]
	public class RuleFormulaMetathesisTests : MemoryOnlyBackendProviderTestBase
	{
		private IPhMetathesisRule m_rule;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				var a = AddPhoneme("a");
				var t = AddPhoneme("t");
				var vowel = AddNc("V");
				var cons = AddNc("C");

				m_rule = Cache.ServiceLocator.GetInstance<IPhMetathesisRuleFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Add(m_rule);
				m_rule.Name.SetAnalysisDefaultWritingSystem("meta");

				// Four cells, one context each (HCLoaderTests.MetathesisRule pattern).
				AddNcAt(vowel, PhMetathesisRuleTags.kidxLeftEnv, 0);
				AddSegAt(a, PhMetathesisRuleTags.kidxLeftSwitch, 1);
				AddSegAt(t, PhMetathesisRuleTags.kidxRightSwitch, 2);
				AddNcAt(cons, PhMetathesisRuleTags.kidxRightEnv, 3);
			});
		}

		[Test]
		public void ProjectMetathesisRule_BucketsTheFourCells()
		{
			var model = RuleFormulaProjector.ProjectMetathesisRule(m_rule);

			Assert.That(model.RuleKind, Is.EqualTo(RuleFormulaProjector.MetathesisRuleKind));
			Assert.That(model.ToFormulaString(), Is.EqualTo("[V] | a ↔ t | [C]"));
			Assert.That(model.SectionFor(RuleSectionRole.LeftEnv).Cells.Single().DisplayText, Is.EqualTo("V"));
			Assert.That(model.SectionFor(RuleSectionRole.LeftSwitch).Cells.Single().DisplayText, Is.EqualTo("a"));
			Assert.That(model.SectionFor(RuleSectionRole.RightSwitch).Cells.Single().DisplayText, Is.EqualTo("t"));
			Assert.That(model.SectionFor(RuleSectionRole.RightEnv).Cells.Single().DisplayText, Is.EqualTo("C"));
		}

		[Test]
		public void Compose_PhMetathesisRule_ResolvesTheRuleEditor_NotUnsupported()
		{
			var composed = FullEntryRegionComposer.Compose(m_rule, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			var editors = composed.Model.Fields
				.Where(f => f.Kind == RegionFieldKind.Custom)
				.Select(f => f.ControlFactory?.Invoke())
				.OfType<RuleFormulaRegionEditor>()
				.ToList();
			Assert.That(editors, Is.Not.Empty,
				"the MetaRuleFormulaSlice resolves to the Avalonia rule editor (no Unsupported row)");
		}

		private string Section(RuleSectionRole role) => RuleFormulaProjector.ProjectMetathesisRule(m_rule)
			.SectionFor(role).Cells.Aggregate("", (s, c) => s + c.DisplayText);

		private MetaRuleFormulaEditSink NewSink()
		{
			var host = new ComposedRegionEditContext(Cache, m_rule,
				new System.Collections.Generic.Dictionary<string, System.Func<string, string, bool>>(),
				new System.Collections.Generic.Dictionary<string, System.Func<string, bool>>());
			return new MetaRuleFormulaEditSink(m_rule, Cache, host, m => { });
		}

		[Test]
		public void EditLeftEnv_Insert_Delete_RoundTripsAndMaintainsPartitions()
		{
			var sink = NewSink();
			var p = Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.First(ph =>
				ph.Name.BestVernacularAlternative.Text == "a");

			// Insert a phoneme into the (1-cell) left env at index 1 → "Va" ... wait LeftEnv started as [V].
			Assert.That(sink.InsertCell(RuleSectionRole.LeftEnv, 1, new RuleCellSpec(RuleCellKind.Phoneme, p.Guid)), Is.True);
			Assert.That(Section(RuleSectionRole.LeftEnv), Is.EqualTo("Va"), "inserted phoneme appends to the left env");
			// The switch cells are untouched by the env edit (partitions shifted correctly).
			Assert.That(Section(RuleSectionRole.LeftSwitch), Is.EqualTo("a"));
			Assert.That(Section(RuleSectionRole.RightSwitch), Is.EqualTo("t"));

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(Section(RuleSectionRole.LeftEnv), Is.EqualTo("V"), "one Undo restores the env");
		}

		[Test]
		public void EditRightSwitch_SetCell_ReplacesTarget()
		{
			var sink = NewSink();
			var a = Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.First(ph =>
				ph.Name.BestVernacularAlternative.Text == "a");

			// Replace the right-switch cell (t) with phoneme a.
			Assert.That(sink.SetCell(RuleSectionRole.RightSwitch, 0, new RuleCellSpec(RuleCellKind.Phoneme, a.Guid)), Is.True);
			Assert.That(Section(RuleSectionRole.RightSwitch), Is.EqualTo("a"));
			Assert.That(Section(RuleSectionRole.LeftSwitch), Is.EqualTo("a"), "the left switch is unchanged");
			Assert.That(Section(RuleSectionRole.RightEnv), Is.EqualTo("C"), "the right env is unchanged");
		}

		private IPhPhoneme AddPhoneme(string name)
		{
			var ph = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(ph);
			ph.Name.SetVernacularDefaultWritingSystem(name);
			return ph;
		}

		private IPhNCSegments AddNc(string abbr)
		{
			var nc = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(nc);
			nc.Abbreviation.SetAnalysisDefaultWritingSystem(abbr);
			return nc;
		}

		private void AddSegAt(IPhPhoneme phoneme, int kidx, int index)
		{
			var ctx = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
			m_rule.StrucDescOS.Add(ctx);
			ctx.FeatureStructureRA = phoneme;
			m_rule.UpdateStrucChange(kidx, index, true);
		}

		private void AddNcAt(IPhNaturalClass nc, int kidx, int index)
		{
			var ctx = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
			m_rule.StrucDescOS.Add(ctx);
			ctx.FeatureStructureRA = nc;
			m_rule.UpdateStrucChange(kidx, index, true);
		}
	}
}
