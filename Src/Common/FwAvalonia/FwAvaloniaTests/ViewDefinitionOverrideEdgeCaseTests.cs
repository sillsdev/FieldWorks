// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Boundary/edge-case hardening for the override pipeline: malformed-JSON enum handling (controlled
	/// InvalidDataException, never a raw NRE/ArgumentException), id-collision rejection on insert ops,
	/// the AddNode round-trip the existing suite lacked, and AddNode index clamping.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionOverrideEdgeCaseTests
	{
		private static ViewNode FieldNode(string id, string label, string field = "F")
			=> new ViewNode(id, ViewNodeKind.Field, label, null, field, "string",
				EditorClassification.Known, "vern", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null);

		private static ViewNode GroupNode(string id, string label, params ViewNode[] children)
			=> new ViewNode(id, ViewNodeKind.Group, label, null, null, null,
				EditorClassification.GroupingNone, null, ViewVisibility.Always, ViewExpansion.Expanded,
				false, null, children);

		private static ViewDefinitionModel Model(params ViewNode[] roots)
			=> new ViewDefinitionModel("LexEntry", "detail", "jtview", roots, null);

		private static ViewDefinitionOverride Patch(params ViewOverrideOperation[] ops)
			=> new ViewDefinitionOverride("LexEntry", "detail", "jtview", ops, null);

		// ----- malformed-JSON enum handling -----

		[Test]
		public void Deserialize_GarbageVisibility_ThrowsControlledInvalidData()
		{
			var json = ViewDefinitionOverrideJsonSerializer.Serialize(
				Patch(new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "a", visibility: ViewVisibility.Never)));
			var bad = json.Replace("\"Never\"", "\"Bogus\"");
			Assert.That(() => ViewDefinitionOverrideJsonSerializer.Deserialize(bad),
				Throws.InstanceOf<InvalidDataException>(), "an unknown enum token is a controlled data error");
		}

		[Test]
		public void Deserialize_NullVisibility_ThrowsControlledInvalidData_NotNullRef()
		{
			var json = ViewDefinitionOverrideJsonSerializer.Serialize(
				Patch(new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "a", visibility: ViewVisibility.Never)));
			var bad = json.Replace("\"Never\"", "null");
			Assert.That(() => ViewDefinitionOverrideJsonSerializer.Deserialize(bad),
				Throws.InstanceOf<InvalidDataException>(), "a null enum token must not throw a raw NullReferenceException");
		}

		[Test]
		public void Deserialize_UnknownOpKind_ThrowsInvalidData()
		{
			var json = ViewDefinitionOverrideJsonSerializer.Serialize(
				Patch(new ViewOverrideOperation(ViewOverrideOperationKind.SetLabel, "a", label: "X")));
			// Replace the wire op name (whatever it is) with a bogus one.
			var bad = System.Text.RegularExpressions.Regex.Replace(json, "\"op\"\\s*:\\s*\"[^\"]+\"", "\"op\": \"frobnicate\"");
			Assert.That(() => ViewDefinitionOverrideJsonSerializer.Deserialize(bad),
				Throws.InstanceOf<InvalidDataException>());
		}

		// ----- id-collision rejection on insert ops -----

		[Test]
		public void Apply_AddNode_CollidingId_IsRejectedWithDiagnostic_NotInserted()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			var patch = Patch(new ViewOverrideOperation(ViewOverrideOperationKind.AddNode, "g/a",
				label: "Dup", parentStableId: "g", index: 1, nodeKind: ViewNodeKind.Field,
				field: "F", editor: "string", visibility: ViewVisibility.Always));

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Diagnostics.Any(d => d.Code == "override-duplicate-id"), Is.True);
			Assert.That(applied.Roots[0].Children.Select(c => c.StableId), Is.EqualTo(new[] { "g/a" }),
				"a colliding addNode id is not inserted, preserving id uniqueness");
		}

		[Test]
		public void Apply_DuplicateNode_CollidingId_IsRejectedWithDiagnostic()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			var patch = Patch(new ViewOverrideOperation(ViewOverrideOperationKind.DuplicateNode, "g/a",
				parentStableId: "g", index: 1, sourceStableId: "g/a"));

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Diagnostics.Any(d => d.Code == "override-duplicate-id"), Is.True);
			Assert.That(applied.Roots[0].Children.Count, Is.EqualTo(1));
		}

		// ----- AddNode round-trip (was missing) + index clamping -----

		[Test]
		public void RoundTrip_DiffThenApply_ReproducesCustomized_WithAddedNode()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			var customized = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/new", "New", "NewField")));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);
			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.ToSnapshot(), Is.EqualTo(customized.ToSnapshot()),
				"the add-node round-trip reproduces the customized model exactly");
		}

		[TestCase(0, new[] { "g/new", "g/a", "g/b" })]
		[TestCase(1, new[] { "g/a", "g/new", "g/b" })]
		[TestCase(99, new[] { "g/a", "g/b", "g/new" })] // clamped to count
		[TestCase(-5, new[] { "g/new", "g/a", "g/b" })] // clamped to 0
		public void Apply_AddNode_IndexIsClampedToBounds(int index, string[] expectedOrder)
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/b", "B")));
			var patch = Patch(new ViewOverrideOperation(ViewOverrideOperationKind.AddNode, "g/new",
				label: "New", parentStableId: "g", index: index, nodeKind: ViewNodeKind.Field,
				field: "F", editor: "string", visibility: ViewVisibility.Always));

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Roots[0].Children.Select(c => c.StableId), Is.EqualTo(expectedOrder));
		}

		[Test]
		public void RoundTrip_AddedNode_SurvivesTheJsonWireLane_IncludingWritingSystem()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			var customized = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/new", "New", "NewField")));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);
			var reloaded = ViewDefinitionOverrideJsonSerializer.Deserialize(
				ViewDefinitionOverrideJsonSerializer.Serialize(patch));
			var applied = ViewDefinitionOverrideApplier.Apply(shipped, reloaded);

			Assert.That(applied.ToSnapshot(), Is.EqualTo(customized.ToSnapshot()),
				"the added node (with its writing system) survives serialize → deserialize → apply");
		}

		[Test]
		public void RoundTrip_RootLevelReorder_IsReproduced()
		{
			// Reordering the top-level fields is a common customization; it must round-trip, not be dropped.
			var shipped = Model(FieldNode("r1", "R1"), FieldNode("r2", "R2"), FieldNode("r3", "R3"));
			var customized = Model(FieldNode("r3", "R3"), FieldNode("r1", "R1"), FieldNode("r2", "R2"));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);
			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Roots.Select(r => r.StableId), Is.EqualTo(new[] { "r3", "r1", "r2" }));
			Assert.That(applied.ToSnapshot(), Is.EqualTo(customized.ToSnapshot()));
			Assert.That(applied.Diagnostics.Any(d => d.Code == "override-stale-target"), Is.False,
				"the root-level reorder op is not falsely reported as a stale target");
		}

		[Test]
		public void Reparent_IsReportedAsDiagnostic_NotSilentlyDropped()
		{
			// Moving a node to a different parent is not representable as a sparse patch — but it must be
			// reported, never silently lost.
			var shipped = Model(GroupNode("g1", "G1", FieldNode("a", "A")), GroupNode("g2", "G2"));
			var customized = Model(GroupNode("g1", "G1"), GroupNode("g2", "G2", FieldNode("a", "A")));

			var patch = ViewDefinitionOverrideDiffer.Diff(shipped, customized);

			Assert.That(patch.Diagnostics.Any(d => d.Code == "override-reparent-unrepresentable"), Is.True,
				"a reparented node is reported, not dropped");
		}

		[Test]
		public void Diff_IdenticalModels_ProducesEmptyPatch()
		{
			var model = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			var patch = ViewDefinitionOverrideDiffer.Diff(model, model);
			Assert.That(patch.Operations, Is.Empty);
		}

		[Test]
		public void Apply_ReorderChildren_PartialOrder_KeepsUnnamedAtEnd()
		{
			var shipped = Model(GroupNode("g", "Group",
				FieldNode("g/a", "A"), FieldNode("g/b", "B"), FieldNode("g/c", "C")));
			var patch = Patch(new ViewOverrideOperation(ViewOverrideOperationKind.ReorderChildren, "g",
				childOrder: new[] { "g/c" }));

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			Assert.That(applied.Roots[0].Children.Select(c => c.StableId), Is.EqualTo(new[] { "g/c", "g/a", "g/b" }),
				"named ids move first; the rest keep their original relative order");
		}
	}
}
