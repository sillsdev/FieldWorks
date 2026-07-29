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
	/// (AdhocCoprohibEdit) stays gated off via EditSurfaceRegistry.Phase1FollowUpSurfaceTools
	/// regardless of what the composer can already do — this file only proves the composer machinery, not that
	/// the tool is live.
	///
	/// Pins how the ad-hoc co-prohibition records compose on the Avalonia surface, to scope the remaining
	/// work for the `AdhocCoprohibEdit` tool. Its Key/Others rows are custom slices; with the lexical-edit
	/// region limited to string, list-choice, and the one native plugin, an unclaimed custom slice composes
	/// as a labeled Unsupported worklist row.
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

			var composed = DetailComposer.Compose(rule, Cache, layoutName: "Edit",
				plugins: SlicePluginRegistry.Default);

			var kinds = composed.Model.Fields.Select(f => f.Kind.ToString()).ToList();
			TestContext.WriteLine("MoMorphAdhocProhib composed field kinds: " + string.Join(", ", kinds));
			Assert.That(composed.Model.Fields, Is.Not.Empty, "the ad-hoc co-prohibition composes its detail");
			// The Key (FirstMorpheme) and Others (RestOfMorphs) are custom slices. With the region limited
			// to string / list-choice / one native plugin, an unclaimed custom slice composes as a labeled
			// Unsupported worklist row rather than a Chooser/ReferenceVector.
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Unsupported),
				"the ad-hoc co-prohibition's custom slices compose as Unsupported worklist rows");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Chooser
				|| f.Kind == DetailFieldKind.ReferenceVector), Is.False,
				"no custom slice composes a list-choice editor now (that path is the plugin's job)");
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

			var composed = DetailComposer.Compose(group, Cache, layoutName: "Edit",
				plugins: SlicePluginRegistry.Default);

			var kinds = composed.Model.Fields.Select(f => f.Kind.ToString()).ToList();
			TestContext.WriteLine("MoAdhocProhibGr composed field kinds: " + string.Join(", ", kinds));
			// The group's own scalar fields compose editably (Name/Description Text + Active checkbox).
			// PARITY: the nested Members rows (recursive sub-prohibitions) are not composed.
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Text), "Name/Description compose");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Unsupported),
				"the Active boolean flag composes as a labeled Unsupported worklist row (checkbox editing dropped)");
		}
	}
}
