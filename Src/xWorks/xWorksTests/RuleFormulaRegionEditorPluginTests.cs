// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 1.4) — the regular-rule formula plugin lands on the Avalonia
	/// surface. T1: the plugin builds a <see cref="RuleFormulaRegionEditor"/> from a rule's
	/// <c>IPhSegRuleRHS</c>. T2: composing the <c>PhSegRuleRHS</c> "Edit" layout through the real
	/// <see cref="FullEntryRegionComposer"/> + the DEFAULT plugin registry resolves the RegRuleFormulaSlice
	/// to the rule editor — no §20.1.3 "Unsupported" row.
	/// </summary>
	[TestFixture]
	public class RuleFormulaRegionEditorPluginTests : MemoryOnlyBackendProviderTestBase
	{
		private IPhRegularRule m_rule;
		private IPhSegRuleRHS m_rhs;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				var p = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(p);
				p.Name.SetVernacularDefaultWritingSystem("p");
				var vowel = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(vowel);
				vowel.Abbreviation.SetAnalysisDefaultWritingSystem("V");

				m_rule = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Add(m_rule);    // factory seeds RightHandSidesOS[0]
				m_rule.Name.SetAnalysisDefaultWritingSystem("prule");
				var lhs = Cache.ServiceLocator.GetInstance<IPhSimpleContextSegFactory>().Create();
				m_rule.StrucDescOS.Add(lhs);
				lhs.FeatureStructureRA = p;
				m_rhs = m_rule.RightHandSidesOS[0];
				var rhsNc = Cache.ServiceLocator.GetInstance<IPhSimpleContextNCFactory>().Create();
				m_rhs.StrucChangeOS.Add(rhsNc);
				rhsNc.FeatureStructureRA = vowel;
			});
		}

		[Test]
		public void Plugin_BuildsRuleFormulaEditor_FromTheRhs()
		{
			var node = new ViewNode("PhSegRuleRHS/RuleFormula/#0", ViewNodeKind.Field, "Rule Formula", null,
				"RuleFormula", "custom", EditorClassification.Dynamic, null, ViewVisibility.Always,
				ViewExpansion.NotApplicable, false, null, Array.Empty<ViewNode>(),
				customEditorClass: RuleFormulaRegionEditorPlugin.RegRuleFormulaSliceClassName,
				customEditorAssembly: "MorphologyEditorDll.dll");

			var control = new RuleFormulaRegionEditorPlugin()
				.BuildControl(new RegionEditorBuildContext(m_rhs, node, () => null, Cache));

			Assert.That(control, Is.InstanceOf<RuleFormulaRegionEditor>());
			var editor = (RuleFormulaRegionEditor)control;
			Assert.That(editor.Model.ToFormulaString(), Is.EqualTo("p → [V] /  __ "),
				"the projected formula renders LHS phoneme, RHS natural class, and empty contexts");
		}

		[Test]
		public void Plugin_IsRegisteredInTheDefaultRegistry()
		{
			Assert.That(
				RegionEditorPluginRegistry.Default.Resolve(RuleFormulaRegionEditorPlugin.RegRuleFormulaSliceClassName),
				Is.InstanceOf<RuleFormulaRegionEditorPlugin>(),
				"the rule editor is wired into the product plugin registry");
		}

		[Test]
		public void Compose_PhSegRuleRHS_ResolvesTheRuleEditor_NotUnsupported()
		{
			var composed = FullEntryRegionComposer.Compose(m_rhs, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			var custom = composed.Model.Fields.SingleOrDefault(f => f.Kind == RegionFieldKind.Custom);
			Assert.That(custom, Is.Not.Null, "the RHS 'Edit' layout composes the custom RuleFormula slice");

			var control = custom.ControlFactory();
			Assert.That(control, Is.InstanceOf<RuleFormulaRegionEditor>(),
				"the RegRuleFormulaSlice resolves to the Avalonia rule editor (no Unsupported row)");
		}

		[Test]
		public void Compose_PhRegularRule_RecordPath_ResolvesTheRuleEditor()
		{
			// The REAL RecordEditView composes the PhRegularRule (the record), which reaches the formula
			// slice only through <seq field="RightHandSides" layout="Edit"/> + the RuleFormulaSection
			// summary/indent wrapper. Proving the rule-level path here de-risks Block 4 (advisor-flagged).
			var composed = FullEntryRegionComposer.Compose(m_rule, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			var ruleEditors = composed.Model.Fields
				.Where(f => f.Kind == RegionFieldKind.Custom)
				.Select(f => f.ControlFactory?.Invoke())
				.OfType<RuleFormulaRegionEditor>()
				.ToList();
			Assert.That(ruleEditors, Is.Not.Empty,
				"composing the rule record reaches the RuleFormula slice through the RightHandSides seq and "
				+ "resolves the Avalonia rule editor (no Unsupported row)");
		}
	}
}
