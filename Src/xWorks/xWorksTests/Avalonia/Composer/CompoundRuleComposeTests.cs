// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Groundwork for the rule-formula editor (a follow-up surface). The tool-level flip
	/// (compoundRuleAdvancedEdit) stays gated off via EditSurfaceRegistry.Phase1FollowUpSurfaceTools
	/// regardless of what the composer can already do — this file only proves the composer machinery, not that
	/// the tool is live.
	///
	/// The headed/non-headed compound rules (MoEndoCompound /
	/// MoExoCompound) are the category-based "Compound Rules" the user sees (Name/Description/Active +
	/// Left/Right Member + Result categories) — NOT the MoAffixProcess formula grid. They compose from
	/// STANDARD slices. Both branches of a part's `<if Disabled=true/false>` pair import and the active
	/// branch renders, so the detail composes Name/Description as editable Text and each member/result
	/// CATEGORY as an editable Chooser; the Active boolean composes as a labeled Unsupported worklist row
	/// (checkbox editing dropped).
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

			var composed = RegionComposer.Compose(rule, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			Assert.That(composed.Model.Fields, Is.Not.Empty, "the non-headed compound rule composes its detail fields");
			// Report the composed field kinds so the editable-vs-readonly state is pinned (Name/Description
			// multistring + Active + the member/result category references). This documents the current
			// behavior for the compoundRuleAdvancedEdit tool readiness assessment.
			var kinds = composed.Model.Fields.Select(f => f.Kind.ToString()).ToList();
			TestContext.WriteLine("MoExoCompound composed field kinds: " + string.Join(", ", kinds));

			// The full non-headed compound detail composes editably.
			Assert.That(composed.Model.Fields.Count(f => f.Kind == DetailFieldKind.Text), Is.GreaterThanOrEqualTo(2),
				"Name and Description compose as editable text rows (the <if Disabled> active branch now imports)");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Unsupported),
				"the Active boolean flag composes as a labeled Unsupported worklist row (checkbox editing dropped)");
			Assert.That(composed.Model.Fields.Count(f => f.Kind == DetailFieldKind.Chooser), Is.GreaterThanOrEqualTo(3),
				"the Left Member / Right Member / Result CATEGORY pickers compose as editable choosers");
		}
	}
}
