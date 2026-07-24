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
	/// stack description says it belongs (avalonia-rule-formula-editor is a follow-up PR). The tool-level flip
	/// (AdhocCoprohibEdit) stays correctly gated off via LexicalEditSurfaceRegistry.Phase1FollowUpSurfaceTools
	/// regardless of what this composer can already do — this file only proves the composer machinery, not that
	/// the tool is live.
	///
	/// avalonia-rule-formula-editor (task 3.4) — pins how the ad-hoc co-prohibition records compose on the
	/// Avalonia surface, to scope the remaining work for the `AdhocCoprohibEdit` tool. The "Others" reference
	/// vector should be editable (generic editable-vector fix); the "Key" (FirstMorpheme/FirstAllomorph) is a
	/// custom slice.
	/// </summary>
	[TestFixture]
	public class AdhocCoProhibComposeTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void Compose_MoMorphAdhocProhib_ReportsFieldKinds()
		{
			IMoMorphAdhocProhib rule = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				// Candidate morphemes must exist for the Key/Others choosers to materialize: a lex entry
				// with a stem MSA (an IMorpheme) is a valid FirstMorpheme/RestOfMorphs target.
				var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				entry.MorphoSyntaxAnalysesOC.Add(msa);

				rule = Cache.ServiceLocator.GetInstance<IMoMorphAdhocProhibFactory>().Create();
				Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Add(rule);
			});

			var composed = FullEntryRegionComposer.Compose(rule, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			var kinds = composed.Model.Fields.Select(f => f.Kind.ToString()).ToList();
			TestContext.WriteLine("MoMorphAdhocProhib composed field kinds: " + string.Join(", ", kinds));
			Assert.That(composed.Model.Fields, Is.Not.Empty, "the ad-hoc co-prohibition composes its detail");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == RegionFieldKind.Chooser),
				"the Key (FirstMorpheme) composes as an editable atomic chooser (generic atomic-chooser path)");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == RegionFieldKind.ReferenceVector),
				"the Others (RestOfMorphs) composes as an editable reference vector (generic vector path)");
		}

		[Test]
		public void Compose_MoAdhocProhibGr_ComposesNameDescriptionActive()
		{
			IMoAdhocProhibGr group = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				group = Cache.ServiceLocator.GetInstance<IMoAdhocProhibGrFactory>().Create();
				Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Add(group);
				group.Name.SetAnalysisDefaultWritingSystem("Group A");
			});

			var composed = FullEntryRegionComposer.Compose(group, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			var kinds = composed.Model.Fields.Select(f => f.Kind.ToString()).ToList();
			TestContext.WriteLine("MoAdhocProhibGr composed field kinds: " + string.Join(", ", kinds));
			// The group's own scalar fields compose editably (Name/Description Text + Active checkbox).
			// // PARITY: the nested Members rows (recursive sub-prohibitions) are not yet composed.
			Assert.That(composed.Model.Fields.Any(f => f.Kind == RegionFieldKind.Text), "Name/Description compose");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == RegionFieldKind.Boolean), "Active composes");
		}
	}
}
