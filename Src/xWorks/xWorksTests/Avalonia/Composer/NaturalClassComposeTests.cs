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
	/// Groundwork for the rule-formula editor (a follow-up tool). Despite the composer
	/// machinery proven here, the tool-level flip (naturalClassedit) stays gated off via
	/// UIFrameworkRegistry.Phase1FollowUpTools -- this file describes what is actually
	/// proven, not the tool's live state.
	///
	/// The natural-class editor (tool `naturalClassedit`). A PhNCFeatures's Features slice is unclaimed
	/// and composes as a labeled Unsupported worklist row; PhNCSegments composes
	/// Name/Description/Abbreviation + an editable Segments phoneme reference vector (via the generic
	/// ReferenceTargetCandidates editable-vector path).
	/// </summary>
	[TestFixture]
	public class NaturalClassComposeTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void Compose_PhNCSegments_ComposesWithoutCrash()
		{
			IPhNCSegments nc = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				var p = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(p);
				p.Name.SetVernacularDefaultWritingSystem("p");
				nc = Cache.ServiceLocator.GetInstance<IPhNCSegmentsFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(nc);
				nc.Name.SetAnalysisDefaultWritingSystem("Stops");
				nc.Abbreviation.SetAnalysisDefaultWritingSystem("Stop");
				nc.SegmentsRC.Add(p);
			});

			var composed = DetailComposer.Compose(nc, Cache, layoutName: "Edit",
				plugins: SlicePluginRegistry.Default);

			// The Segments phoneme collection composes as an editable ReferenceVector (the legacy phoneme
			// chooser).
			Assert.That(composed.Model.Fields, Is.Not.Empty, "the natural class composes its detail fields");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.ReferenceVector),
				"the Segments phonemes compose as an editable reference-vector row");
		}

		[Test]
		public void Compose_PhNCFeatures_FeatureSliceComposesAsUnsupportedWorklistRow()
		{
			IPhNCFeatures nc = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				nc = Cache.ServiceLocator.GetInstance<IPhNCFeaturesFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.NaturalClassesOS.Add(nc);
				nc.Name.SetAnalysisDefaultWritingSystem("Voiced");
				nc.Abbreviation.SetAnalysisDefaultWritingSystem("Vd");
				nc.FeaturesOA = Cache.ServiceLocator.GetInstance<IFsFeatStrucFactory>().Create();
			});

			var composed = DetailComposer.Compose(nc, Cache, layoutName: "Edit",
				plugins: SlicePluginRegistry.Default);

			// The phonological-feature dialog-launcher slice is unclaimed, so the Features field composes
			// as a labeled Unsupported worklist row (never a Custom/plugin row).
			Assert.That(composed.Model.Fields, Is.Not.Empty, "the feature-based natural class composes");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Custom), Is.False,
				"nothing claims the phonological-feature slice, so there is no Custom/plugin row");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Unsupported),
				"the phonological-feature slice composes as the labeled Unsupported worklist row");
		}
	}
}
