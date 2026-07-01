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
	/// avalonia-rule-formula-editor (task 2.5) — the headed/non-headed compound rules (MoEndoCompound /
	/// MoExoCompound) are the category-based "Compound Rules" the user sees (Name/Description/Active +
	/// Left/Right Member + Result categories) — NOT the MoAffixProcess formula grid. They compose from
	/// STANDARD slices. After the multi-child-part importer fix (a part's `<if Disabled=true/false>` pair both
	/// import; the active branch renders), the full detail composes editably: Name/Description as Text, Active
	/// as a checkbox, and each member/result CATEGORY as an editable Chooser.
	/// </summary>
	[TestFixture]
	public class CompoundRuleComposeTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void Compose_MoExoCompound_ComposesItsDetail()
		{
			IMoExoCompound rule = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				rule = Cache.ServiceLocator.GetInstance<IMoExoCompoundFactory>().Create();
				Cache.LangProject.MorphologicalDataOA.CompoundRulesOS.Add(rule);
				rule.Name.SetAnalysisDefaultWritingSystem("ndi+ppron");
				rule.Description.SetAnalysisDefaultWritingSystem("a fusion of ndi + a personal pronoun");
			});

			var composed = FullEntryRegionComposer.Compose(rule, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			Assert.That(composed.Model.Fields, Is.Not.Empty, "the non-headed compound rule composes its detail fields");
			// Report the composed field kinds so the editable-vs-readonly state is pinned (Name/Description
			// multistring + Active + the member/result category references). This documents the current
			// behavior for the compoundRuleAdvancedEdit tool readiness assessment.
			var kinds = composed.Model.Fields.Select(f => f.Kind.ToString()).ToList();
			TestContext.WriteLine("MoExoCompound composed field kinds: " + string.Join(", ", kinds));

			// Post-fix: the full non-headed compound detail composes editably.
			Assert.That(composed.Model.Fields.Count(f => f.Kind == RegionFieldKind.Text), Is.GreaterThanOrEqualTo(2),
				"Name and Description compose as editable text rows (the <if Disabled> active branch now imports)");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == RegionFieldKind.Boolean),
				"the Active flag composes as an editable checkbox");
			Assert.That(composed.Model.Fields.Count(f => f.Kind == RegionFieldKind.Chooser), Is.GreaterThanOrEqualTo(3),
				"the Left Member / Right Member / Result CATEGORY pickers compose as editable choosers");
		}
	}
}
