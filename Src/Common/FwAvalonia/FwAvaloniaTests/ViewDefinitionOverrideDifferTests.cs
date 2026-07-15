// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 9.2 (override migrator step 3): diffing a shipped definition against a project-customized
	/// copy yields a sparse, stable-id-keyed override; non-representable customizations surface as
	/// diagnostics, never silent drops. Pure logic — no Avalonia runtime.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionOverrideDifferTests
	{
		private static ViewNode FieldNode(string id, string label,
			ViewVisibility vis = ViewVisibility.Always, string field = "F", string editor = "string")
			=> new ViewNode(id, ViewNodeKind.Field, label, null, field, editor,
				EditorClassification.Known, "vern", vis, ViewExpansion.NotApplicable, false, null, null);

		private static ViewNode GroupNode(string id, string label, params ViewNode[] children)
			=> new ViewNode(id, ViewNodeKind.Group, label, null, null, null,
				EditorClassification.GroupingNone, null, ViewVisibility.Always, ViewExpansion.Expanded,
				false, null, children);

		private static ViewDefinitionModel Model(params ViewNode[] roots)
			=> new ViewDefinitionModel("LexEntry", "detail", "jtview", roots, null);

		[Test]
		public void Diff_IdenticalDefinitions_IsEmpty()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/b", "B")));
			var overridden = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/b", "B")));

			var diff = ViewDefinitionOverrideDiffer.Diff(shipped, overridden);

			Assert.That(diff.IsEmpty, Is.True);
			Assert.That(diff.Operations, Is.Empty);
			Assert.That(diff.Diagnostics, Is.Empty);
			Assert.That(diff.FormatVersion, Is.EqualTo(ViewDefinitionOverride.CurrentFormatVersion));
		}

		[Test]
		public void Diff_VisibilityChange_EmitsSetVisibility()
		{
			var shipped = Model(FieldNode("a", "A", ViewVisibility.Always));
			var overridden = Model(FieldNode("a", "A", ViewVisibility.Never));

			var diff = ViewDefinitionOverrideDiffer.Diff(shipped, overridden);

			Assert.That(diff.Operations.Count, Is.EqualTo(1));
			var op = diff.Operations[0];
			Assert.That(op.Kind, Is.EqualTo(ViewOverrideOperationKind.SetVisibility));
			Assert.That(op.StableId, Is.EqualTo("a"));
			Assert.That(op.Visibility, Is.EqualTo(ViewVisibility.Never));
			Assert.That(diff.Diagnostics, Is.Empty);
		}

		[Test]
		public void Diff_LabelChange_EmitsSetLabel()
		{
			var shipped = Model(FieldNode("a", "Lexeme Form"));
			var overridden = Model(FieldNode("a", "Headword"));

			var diff = ViewDefinitionOverrideDiffer.Diff(shipped, overridden);

			Assert.That(diff.Operations.Count, Is.EqualTo(1));
			Assert.That(diff.Operations[0].Kind, Is.EqualTo(ViewOverrideOperationKind.SetLabel));
			Assert.That(diff.Operations[0].Label, Is.EqualTo("Headword"));
		}

		[Test]
		public void Diff_ChildReorder_EmitsReorderChildren_WithNewOrder()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/b", "B")));
			var overridden = Model(GroupNode("g", "Group", FieldNode("g/b", "B"), FieldNode("g/a", "A")));

			var diff = ViewDefinitionOverrideDiffer.Diff(shipped, overridden);

			var reorder = diff.Operations.Single(o => o.Kind == ViewOverrideOperationKind.ReorderChildren);
			Assert.That(reorder.StableId, Is.EqualTo("g"));
			Assert.That(reorder.ChildOrder, Is.EqualTo(new[] { "g/b", "g/a" }));
			// The children themselves are unchanged, so they must not generate spurious ops.
			Assert.That(diff.Operations.Count, Is.EqualTo(1));
		}

		[Test]
		public void Diff_NodeOnlyInOverride_EmitsAddNode_WithParentAndIndex()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));
			var overridden = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/custom", "Custom")));

			var diff = ViewDefinitionOverrideDiffer.Diff(shipped, overridden);

			var add = diff.Operations.Single(o => o.Kind == ViewOverrideOperationKind.AddNode);
			Assert.That(add.StableId, Is.EqualTo("g/custom"));
			Assert.That(add.ParentStableId, Is.EqualTo("g"), "the added node records its parent");
			Assert.That(add.Index, Is.EqualTo(1), "the added node records its insertion index among siblings");
			Assert.That(add.NodeKind, Is.EqualTo(ViewNodeKind.Field));
			Assert.That(add.Label, Is.EqualTo("Custom"));
			// A customer addition is now representable, not a lossy diagnostic.
			Assert.That(diff.Diagnostics.Any(d => d.Code == "override-added-node"), Is.False);
		}

		[Test]
		public void Diff_NodeOnlyInShipped_EmitsHideNode()
		{
			var shipped = Model(GroupNode("g", "Group", FieldNode("g/a", "A"), FieldNode("g/b", "B")));
			var overridden = Model(GroupNode("g", "Group", FieldNode("g/a", "A")));

			var diff = ViewDefinitionOverrideDiffer.Diff(shipped, overridden);

			var hide = diff.Operations.Single(o => o.Kind == ViewOverrideOperationKind.HideNode);
			Assert.That(hide.StableId, Is.EqualTo("g/b"));
		}

		[Test]
		public void Diff_BindingOrEditorChange_IsReportedUnrepresentable_NotSilentlyPatched()
		{
			// Same StableId, but the override changed the editor AND the label. The editor change is not a
			// representable sparse patch, so the whole node is reported and NO label op is emitted for it.
			var shipped = Model(FieldNode("a", "A", editor: "string"));
			var overridden = Model(FieldNode("a", "A-renamed", editor: "integer"));

			var diff = ViewDefinitionOverrideDiffer.Diff(shipped, overridden);

			Assert.That(diff.Operations, Is.Empty, "an unrepresentable change must not produce a (wrong) sparse patch");
			var diag = diff.Diagnostics.Single(d => d.Code == "override-unrepresentable-change");
			Assert.That(diag.NodePath, Is.EqualTo("a"));
			Assert.That(diag.Severity, Is.EqualTo(ViewDiagnosticSeverity.Warning));
		}

		[Test]
		public void Diff_Operations_AreDeterministicallyOrderedByStableId()
		{
			var shipped = Model(
				FieldNode("zeta", "Z", ViewVisibility.Always),
				FieldNode("alpha", "A", ViewVisibility.Always));
			var overridden = Model(
				FieldNode("zeta", "Z", ViewVisibility.Never),
				FieldNode("alpha", "A", ViewVisibility.Never));

			var diff = ViewDefinitionOverrideDiffer.Diff(shipped, overridden);

			var ids = diff.Operations.Select(o => o.StableId).ToList();
			Assert.That(ids, Is.EqualTo(new[] { "alpha", "zeta" }), "operations must be ordered by StableId");
		}
	}
}
