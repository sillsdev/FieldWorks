// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Override load side: applying a sparse patch to the shipped definition reproduces the customized
	/// definition, and is the inverse of the differ for representable changes. Pure logic.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionOverrideApplierTests
	{
		private static ViewNode FieldNode(string id, string label,
			ViewVisibility vis = ViewVisibility.Always, string field = "F", string editor = "string")
			=> new ViewNode(id, ViewNodeKind.Field, label, null, field, editor,
				EditorClassification.Known, "vern", vis, ViewExpansion.NotApplicable, false, null, null);

		private static ViewNode GroupNode(string id, string label, params ViewNode[] children)
			=> new ViewNode(id, ViewNodeKind.Group, label, null, null, null,
				EditorClassification.GroupingNone, null, ViewVisibility.Always, ViewExpansion.Expanded,
				false, null, children);

		private static ViewNode WsFieldNode(string id, string[] visibleWritingSystems)
			=> new ViewNode(id, ViewNodeKind.Field, "A", null, "Form", "multistring",
				EditorClassification.Known, "all vernacular", ViewVisibility.Always,
				ViewExpansion.NotApplicable, false, null, null,
				visibleWritingSystems: visibleWritingSystems);

		private static ViewDefinitionModel Model(params ViewNode[] roots)
			=> new ViewDefinitionModel("LexEntry", "detail", "jtview", roots, null);

		private static ViewDefinitionOverride Empty()
			=> new ViewDefinitionOverride("LexEntry", "detail", "jtview", null, null);

		[Test]
		public void Apply_EmptyPatch_ReproducesBaseExactly()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/b", "B")));

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, Empty());

			Assert.That(applied.ToSnapshot(), Is.EqualTo(shipped.ToSnapshot()));
		}

		// Every node is rebuilt on apply, so a clone that omits a field strips it tree-wide once
		// any override exists. These three fields are outside ToSnapshot() and aren't covered by
		// the EmptyPatch test.
		[Test]
		public void Apply_PreservesNodeFieldsNoOperationTouches()
		{
			var writingSystems = new[] { "fr", "seh" };
			var options = new ViewStringList(new[] { "IsElsewhereForm", "IsAbstractForm" }, "AllomorphStatus");
			var shipped = Model(GroupNode("g", "Group",
				new ViewNode("g/a", ViewNodeKind.Field, "A", null, "F", "multistring",
					EditorClassification.Known, "vern", ViewVisibility.Always,
					ViewExpansion.NotApplicable, false, null, null,
					enumStringList: options, visibleWritingSystems: writingSystems, toggleValue: true)));
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "g/a",
						visibility: ViewVisibility.Never)
				}, null);

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			var rebuilt = applied.Roots[0].Children[0];
			Assert.That(rebuilt.Visibility, Is.EqualTo(ViewVisibility.Never), "the operation still applies");
			Assert.That(rebuilt.VisibleWritingSystems, Is.EqualTo(writingSystems),
				"a per-field writing-system subset survives the rebuild");
			Assert.That(rebuilt.ToggleValue, Is.True, "a toggle value survives the rebuild");
			Assert.That(rebuilt.EnumStringList?.Ids, Is.EqualTo(options.Ids),
				"an enum option list survives the rebuild");
		}

		// The writing-system subset a user picks for one field, recorded against its stable id.
		[Test]
		public void Apply_SetVisibleWritingSystems_RestrictsThatFieldOnly()
		{
			var shipped = Model(GroupNode("g", "Group",
				WsFieldNode("g/a", new[] { "fr", "seh", "aka" }),
				FieldNode("g/b", "B")));
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibleWritingSystems, "g/a",
						writingSystems: new[] { "fr" })
				}, null);

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Roots[0].Children[0].VisibleWritingSystems, Is.EqualTo(new[] { "fr" }),
				"the patched field is restricted to the chosen writing systems");
			Assert.That(applied.Roots[0].Children[1].VisibleWritingSystems, Is.Null,
				"a field the patch does not name is untouched");
		}

		[Test]
		public void Apply_SetVisibleWritingSystems_PreservesTheChosenOrder()
		{
			var shipped = Model(GroupNode("g", "Group", WsFieldNode("g/a", new[] { "fr", "seh" })));
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibleWritingSystems, "g/a",
						writingSystems: new[] { "seh", "fr" })
				}, null);

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Roots[0].Children[0].VisibleWritingSystems,
				Is.EqualTo(new[] { "seh", "fr" }), "display order is the user's order, not the shipped one");
		}

		[Test]
		public void Apply_AddNode_InsertsAtParentIndex()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewOverrideOperation(ViewOverrideOperationKind.AddNode, "g/new",
						label: "New", parentStableId: "g", index: 1, nodeKind: ViewNodeKind.Field,
						field: "F", editor: "string", visibility: ViewVisibility.Always)
				}, null);

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			var children = applied.Roots[0].Children;
			Assert.That(children.Select(c => c.StableId), Is.EqualTo(new[] { "g/a", "g/new" }));
			Assert.That(children[1].Label, Is.EqualTo("New"));
		}

		[Test]
		public void Apply_DuplicateNode_CopiesLeafUnderNewId()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewOverrideOperation(ViewOverrideOperationKind.DuplicateNode, "g/a-copy",
						parentStableId: "g", index: 1, sourceStableId: "g/a")
				}, null);

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			var children = applied.Roots[0].Children;
			Assert.That(children.Select(c => c.StableId), Is.EqualTo(new[] { "g/a", "g/a-copy" }));
			Assert.That(children[1].Label, Is.EqualTo("A"), "the duplicate copies the source's content");
			Assert.That(children[1].Field, Is.EqualTo("F"));
		}

		[Test]
		public void Apply_DuplicateNode_SourceWithChildren_ReportsDiagnostic_AndDoesNotInsert()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			// Try to duplicate the group 'g' (which has a child) under the root -- not yet
			// supported.
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewOverrideOperation(ViewOverrideOperationKind.DuplicateNode, "g-copy",
						parentStableId: null, index: 1, sourceStableId: "g")
				}, null);

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Diagnostics.Any(d => d.Code == "duplicate-with-children-unsupported"), Is.True);
			Assert.That(applied.Roots.Select(r => r.StableId), Is.EqualTo(new[] { "g" }), "the unsupported duplicate is not inserted");
		}

		[Test]
		public void Apply_StalePatchTarget_IsReportedAsDiagnostic_NotFatal()
		{
			var shipped = Model(FieldNode("a", "A"));
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[] { new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "ghost",
					visibility: ViewVisibility.Never) }, null);

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Diagnostics.Any(d => d.Code == "override-stale-target"), Is.True);
			// The real node is untouched.
			Assert.That(applied.Roots.Single().Visibility, Is.EqualTo(ViewVisibility.Always));
		}

		[Test]
		public void RoundTrip_DiffThenApply_ReproducesCustomized_VisibilityLabelHide()
		{
			var shipped = Model(GroupNode("g", "Group",
				FieldNode("g/a", "A"), FieldNode("g/b", "B"), FieldNode("g/c", "C")));
			// Customer: relabel + hide one + change visibility -- all representable, all fully
			// captured.
			var customized = Model(GroupNode("g", "Group",
				FieldNode("g/a", "Headword", ViewVisibility.Never), FieldNode("g/c", "C")));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);
			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.ToSnapshot(), Is.EqualTo(customized.ToSnapshot()));
		}

		[Test]
		public void RoundTrip_DiffThenApply_ReproducesCustomized_Reorder()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/b", "B")));
			var customized = Model(GroupNode("g", "Group", FieldNode("g/b", "B"), FieldNode("g/a", "A")));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);
			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.ToSnapshot(), Is.EqualTo(customized.ToSnapshot()));
		}

		// VisibleWritingSystems is outside ToSnapshot(), so these round trips assert it directly.
		[Test]
		public void RoundTrip_DiffThenApply_ReproducesCustomized_WritingSystemRestriction()
		{
			var shipped = Model(GroupNode("g", "Group", WsFieldNode("g/a", new[] { "fr", "seh" })));
			var customized = Model(GroupNode("g", "Group", WsFieldNode("g/a", new[] { "seh" })));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);
			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Roots[0].Children[0].VisibleWritingSystems,
				Is.EqualTo(new[] { "seh" }), "the differ captures the restriction and apply replays it");
		}

		[Test]
		public void RoundTrip_DiffThenApply_ClearedRestrictionComesBackNull()
		{
			var shipped = Model(GroupNode("g", "Group", WsFieldNode("g/a", new[] { "fr", "seh" })));
			var customized = Model(GroupNode("g", "Group", WsFieldNode("g/a", null)));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);
			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Roots[0].Children[0].VisibleWritingSystems, Is.Null,
				"clearing normalizes to null, the model's own \"unrestricted\"");
		}

		[Test]
		public void Diff_CaseOnlyWritingSystemDifference_EmitsNoOp()
		{
			var shipped = Model(GroupNode("g", "Group", WsFieldNode("g/a", new[] { "fr-FR" })));
			var customized = Model(GroupNode("g", "Group", WsFieldNode("g/a", new[] { "fr-fr" })));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);

			Assert.That(patch.Operations, Is.Empty,
				"the composer matches tags case-insensitively, so these render identically");
		}

		[Test]
		public void Diff_AddedNodeWithWsRestriction_ReportsDiagnostic()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			var customized = Model(GroupNode("g", "Group", FieldNode("g/a", "A"),
				WsFieldNode("g/new", new[] { "fr" })));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);

			Assert.That(patch.Operations.Single(o =>
					o.Kind == ViewOverrideOperationKind.AddNode).StableId,
				Is.EqualTo("g/new"), "the added node itself is still representable");
			Assert.That(patch.Diagnostics.Any(
					d => d.Code == "override-added-ws-restriction-dropped"), Is.True,
				"the dropped restriction is reported, never silent");
		}
	}
}
