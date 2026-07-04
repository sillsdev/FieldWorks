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
	/// avalonia-rule-formula-editor (task 2.5, read-only) — T1 parity + T2 compose for the compound /
	/// affix-process rule editor. A compound rule is input columns ⇒ output mappings; the projector renders
	/// each input column and maps a copy-from-input to its 1-based column number and an insert-phones to its
	/// phoneme tokens. Hand-written oracle "[C][V] ⇒ 1a" is independent of the projector reads.
	/// </summary>
	[TestFixture]
	public class RuleFormulaCompoundTests : MemoryOnlyBackendProviderTestBase
	{
		private IMoAffixProcess m_rule;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				var a = AddPhoneme("a");
				var cons = AddNc("C");
				var vowel = AddNc("V");

				var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				m_rule = Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create();
				entry.LexemeFormOA = m_rule;
				m_rule.InputOS.Clear();

				// Two input columns: [C] [V].
				var cInput = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
				m_rule.InputOS.Add(cInput);
				cInput.FeatureStructureRA = cons;
				var vInput = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
				m_rule.InputOS.Add(vInput);
				vInput.FeatureStructureRA = vowel;

				// Output: copy input column 1, then insert phoneme "a".
				var copy = Cache.ServiceLocator.GetInstance<IMoCopyFromInputFactory>().Create();
				m_rule.OutputOS.Add(copy);
				copy.ContentRA = m_rule.InputOS[0];
				var insert = Cache.ServiceLocator.GetInstance<IMoInsertPhonesFactory>().Create();
				m_rule.OutputOS.Add(insert);
				insert.ContentRS.Add(a);
			});
		}

		[Test]
		public void ProjectCompoundRule_RendersInputColumnsAndOutputMappings()
		{
			var model = RuleFormulaProjector.ProjectCompoundRule(m_rule);

			Assert.That(model.RuleKind, Is.EqualTo(RuleFormulaProjector.CompoundRuleKind));
			Assert.That(model.ToFormulaString(), Is.EqualTo("[C][V] ⇒ 1a"));

			var input = model.SectionFor(RuleSectionRole.Input);
			Assert.That(input.Cells.Select(c => c.DisplayText), Is.EqualTo(new[] { "C", "V" }));
			var output = model.SectionFor(RuleSectionRole.Output);
			Assert.That(output.Cells[0].DisplayText, Is.EqualTo("1"), "copy-from-input shows its 1-based column number");
			Assert.That(output.Cells[1].DisplayText, Is.EqualTo("a"), "insert-phones shows the inserted phoneme");
		}

		[Test]
		public void Compose_MoAffixProcess_ResolvesTheRuleEditor_NotUnsupported()
		{
			var composed = FullEntryRegionComposer.Compose(m_rule, Cache, layoutName: "Normal",
				plugins: RegionEditorPluginRegistry.Default);

			var editors = composed.Model.Fields
				.Where(f => f.Kind == RegionFieldKind.Custom)
				.Select(f => f.ControlFactory?.Invoke())
				.OfType<RuleFormulaRegionEditor>()
				.ToList();
			Assert.That(editors, Is.Not.Empty,
				"the AffixRuleFormulaSlice resolves to the Avalonia rule editor (no Unsupported row)");
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
	}
}
