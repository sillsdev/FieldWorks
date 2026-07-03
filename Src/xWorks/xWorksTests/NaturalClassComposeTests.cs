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
	/// Groundwork note: this composer-level test landed in the phase1-base spine PR, one PR ahead of where the
	/// stack description says it belongs (avalonia-rule-formula-editor is a follow-up PR). Despite the composer
	/// machinery proven here, the tool-level flip (naturalClassedit) stays correctly gated off via
	/// LexicalEditSurfaceRegistry.Phase1FollowUpSurfaceTools — the line below overstated this ("is flipped to
	/// Avalonia"); corrected to describe what's actually proven, not the tool's live state.
	///
	/// avalonia-rule-formula-editor (task 3.3) — the natural-class editor (tool `naturalClassedit`). A
	/// PhNCFeatures composes its Features through the already-claimed phonological-feature launcher plugin;
	/// PhNCSegments composes Name/Description/Abbreviation + an editable Segments phoneme reference vector
	/// (via the generic ReferenceTargetCandidates editable-vector path). Both natural-class subclasses now
	/// compose fully editably at the composer level, though the tool itself remains gated to Legacy pending
	/// the follow-up PR.
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

			var composed = FullEntryRegionComposer.Compose(nc, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			// Post generic-editable-vector fix: the Segments phoneme collection composes as an editable
			// ReferenceVector (the legacy phoneme chooser), unblocking the naturalClassedit tool flip.
			Assert.That(composed.Model.Fields, Is.Not.Empty, "the natural class composes its detail fields");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == RegionFieldKind.ReferenceVector),
				"the Segments phonemes compose as an editable reference-vector row");
		}

		[Test]
		public void Compose_PhNCFeatures_ComposesViaFeatureLauncher()
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

			var composed = FullEntryRegionComposer.Compose(nc, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			Assert.That(composed.Model.Fields, Is.Not.Empty, "the feature-based natural class composes");
			var custom = composed.Model.Fields.FirstOrDefault(f => f.Kind == RegionFieldKind.Custom);
			Assert.That(custom, Is.Not.Null, "the Features field composes through the phonological-feature launcher plugin");
			Assert.That(custom.ControlFactory?.Invoke(), Is.Not.Null, "the launcher builds a real control (no Unsupported)");
		}
	}
}
