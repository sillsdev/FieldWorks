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
	/// Groundwork for the rule-formula editor (a follow-up tool). The tool-level flip
	/// (AdhocCoprohibEdit) stays gated off via UIFrameworkRegistry.Phase1FollowUpTools
	/// regardless of what the composer can already do — this file only proves the composer machinery, not that
	/// the tool is live.
	///
	/// Pins how the ad-hoc co-prohibition records compose on the Avalonia detail view, to scope the remaining
	/// work for the `AdhocCoprohibEdit` tool. Its Key/Others rows are custom slices; with the lexical-edit
	/// detail view limited to string, list-choice, and the one native plugin, an unclaimed custom slice composes
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
			// The Key (FirstMorpheme) and Others (RestOfMorphs) are custom slices. With the detail view limited
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

			var kinds = composed.Model.Fields.Select(f => $"{f.Field}/{f.Label}:{f.Kind}").ToList();
			TestContext.WriteLine("MoAdhocProhibGr composed field/label:kind: " + string.Join(", ", kinds));
			// The group's own scalar fields compose editably (Name/Description Text + Active checkbox).
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Text), "Name/Description compose");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == DetailFieldKind.Unsupported),
				"the Active boolean flag composes as a labeled Unsupported worklist row (checkbox editing dropped)");
		}

		/// <summary>
		/// The group's "Edit" layout ends in a MembersSection whose indented body is
		/// <c>&lt;seq field="Members" layout="EditAdHocGroup"/&gt;</c> (Morphology.fwlayout:429/MorphologyParts.xml:2629),
		/// so each member composes with its own EditAdHocGroup layout as rows bound to that member.
		/// </summary>
		[Test]
		public void Compose_MoAdhocProhibGr_ComposesMemberRuleRows()
		{
			IMoAdhocProhibGr group = null;
			IMoMorphAdhocProhib member = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				// A candidate morpheme for the member's FirstMorpheme/RestOfMorphs, as the MoMorphAdhocProhib
				// test above sets up: a lex entry with a stem MSA is a valid target.
				var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				entry.MorphoSyntaxAnalysesOC.Add(msa);

				group = Cache.ServiceLocator.GetInstance<IMoAdhocProhibGrFactory>().Create();
				Cache.LangProject.MorphologicalDataOA.AdhocCoProhibitionsOC.Add(group);
				group.Name.SetAnalysisDefaultWritingSystem("Group A");

				// A Morpheme Rule member ("Insert Morpheme Rule", DataTreeInclude.xml), populated the way its
				// EditAdHocGroup layout renders it: FirstMorpheme, Adjacency, RestOfMorphs, Active.
				member = Cache.ServiceLocator.GetInstance<IMoMorphAdhocProhibFactory>().Create();
				group.MembersOC.Add(member);
				member.FirstMorphemeRA = msa;
				member.RestOfMorphsRS.Add(msa);
			});

			var composed = DetailComposer.Compose(group, Cache, layoutName: "Edit",
				plugins: SlicePluginRegistry.Default);

			var rows = composed.Model.Fields
				.Select(f => $"{f.Field}/{f.Label}:{f.Kind}@{f.Indent}#{f.ObjectHvo}").ToList();
			TestContext.WriteLine("MoAdhocProhibGr+member composed field/label:kind@indent#hvo: "
				+ string.Join(", ", rows));

			var memberRows = composed.Model.Fields.Where(f => f.ObjectHvo == member.Hvo).ToList();
			Assert.That(memberRows, Is.Not.Empty, "the Members sequence composes rows bound to the member rule");
			// Every part of MoMorphAdhocProhib's EditAdHocGroup layout composes (Morphology.fwlayout:447): the
			// Active part's field is Disabled, labelled "Active".
			foreach (var field in new[] { "FirstMorpheme", "Adjacency", "RestOfMorphs", "Disabled" })
				Assert.That(memberRows.Any(f => f.Field == field), $"the member's {field} part composes");
			// Key/Others/Adjacency are custom slices, so the member's parts compose as labeled Unsupported
			// worklist rows rather than editors — the structure composes, the editors do not.
			Assert.That(memberRows.Any(f => f.Kind != DetailFieldKind.Header
				&& f.Kind != DetailFieldKind.Unsupported), Is.False,
				"the member's parts compose as Unsupported worklist rows");
			// <seq field="Members" ... indent="true"> nests the member below the group's own rows.
			var groupIndent = composed.Model.Fields
				.Where(f => f.ObjectHvo == group.Hvo && f.Kind != DetailFieldKind.Header).Max(f => f.Indent);
			Assert.That(memberRows.Min(f => f.Indent), Is.GreaterThan(groupIndent),
				"the member's rows nest below the group's own rows");
		}
	}
}
