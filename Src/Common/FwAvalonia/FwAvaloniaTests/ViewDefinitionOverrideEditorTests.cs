// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// advanced-entry-view: the pure "where does the per-field gear-menu command land" logic — strip the
	/// runtime hvo suffix, locate a node's parent + sibling order + visibility in a compiled definition,
	/// compute the moved sibling order, and fold one operation into an existing override (idempotently).
	/// No XCore/LCModel.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionOverrideEditorTests
	{
		private static ViewNode Field(string id, ViewVisibility vis = ViewVisibility.Always)
			=> new ViewNode(id, ViewNodeKind.Field, id, null, "F", "string",
				EditorClassification.Known, "vern", vis, ViewExpansion.NotApplicable, false, null, null);

		private static ViewNode Group(string id, params ViewNode[] children)
			=> new ViewNode(id, ViewNodeKind.Group, id, null, null, null,
				EditorClassification.GroupingNone, null, ViewVisibility.Always, ViewExpansion.Expanded,
				false, null, children);

		private static ViewDefinitionModel Model(params ViewNode[] roots)
			=> new ViewDefinitionModel("LexEntry", "Normal", "detail", roots, null);

		[TestCase("/#0/#1@1234", "/#0/#1")]
		[TestCase("/#0/#1@1234/item3", "/#0/#1/item3")]
		[TestCase("/#0/#1@1234/pic0", "/#0/#1/pic0")]
		[TestCase("/#0/#1", "/#0/#1")] // already a template id (no hvo)
		[TestCase("", "")]
		[TestCase(null, null)]
		public void StripRuntimeSuffix_RemovesHvo_KeepsTrailingPath(string runtime, string expected)
		{
			Assert.That(ViewDefinitionOverrideEditor.StripRuntimeSuffix(runtime), Is.EqualTo(expected));
		}

		[Test]
		public void LocateTarget_ReturnsParentSiblingOrderIndexAndVisibility()
		{
			var model = Model(Group("g", Field("g/a"), Field("g/b", ViewVisibility.IfData), Field("g/c")));

			var loc = ViewDefinitionOverrideEditor.LocateTarget(model, "g/b");

			Assert.That(loc, Is.Not.Null);
			Assert.That(loc.ParentStableId, Is.EqualTo("g"));
			Assert.That(loc.SiblingOrder, Is.EqualTo(new[] { "g/a", "g/b", "g/c" }));
			Assert.That(loc.Index, Is.EqualTo(1));
			Assert.That(loc.Visibility, Is.EqualTo(ViewVisibility.IfData));
			Assert.That(loc.CanMoveUp, Is.True);
			Assert.That(loc.CanMoveDown, Is.True);
		}

		[Test]
		public void LocateTarget_RootLevelNode_HasNullParent()
		{
			var model = Model(Field("r0"), Field("r1"));

			var loc = ViewDefinitionOverrideEditor.LocateTarget(model, "r0");

			Assert.That(loc.ParentStableId, Is.Null);
			Assert.That(loc.Index, Is.EqualTo(0));
			Assert.That(loc.CanMoveUp, Is.False, "the first root node cannot move up");
			Assert.That(loc.CanMoveDown, Is.True);
		}

		[Test]
		public void LocateTarget_UnknownId_ReturnsNull()
		{
			var model = Model(Group("g", Field("g/a")));
			Assert.That(ViewDefinitionOverrideEditor.LocateTarget(model, "nope"), Is.Null);
		}

		[Test]
		public void ComputeMovedOrder_Up_SwapsWithPrevious()
		{
			var order = new[] { "a", "b", "c" };
			var moved = ViewDefinitionOverrideEditor.ComputeMovedOrder(order, 2, up: true);
			Assert.That(moved, Is.EqualTo(new[] { "a", "c", "b" }));
		}

		[Test]
		public void ComputeMovedOrder_Down_SwapsWithNext()
		{
			var order = new[] { "a", "b", "c" };
			var moved = ViewDefinitionOverrideEditor.ComputeMovedOrder(order, 0, up: false);
			Assert.That(moved, Is.EqualTo(new[] { "b", "a", "c" }));
		}

		[Test]
		public void ComputeMovedOrder_FirstUp_LastDown_OnlyChild_AreNull()
		{
			var order = new[] { "a", "b" };
			Assert.That(ViewDefinitionOverrideEditor.ComputeMovedOrder(order, 0, up: true), Is.Null,
				"the first sibling cannot move up");
			Assert.That(ViewDefinitionOverrideEditor.ComputeMovedOrder(order, 1, up: false), Is.Null,
				"the last sibling cannot move down");
			Assert.That(ViewDefinitionOverrideEditor.ComputeMovedOrder(new[] { "solo" }, 0, up: true), Is.Null,
				"a single child cannot move");
		}

		[Test]
		public void MergeOperation_AppendsNewTarget()
		{
			var patch = new ViewDefinitionOverride("LexEntry", "Normal", "detail", null, null);
			var op = new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "g/a",
				visibility: ViewVisibility.Never);

			var merged = ViewDefinitionOverrideEditor.MergeOperation(patch, op);

			Assert.That(merged.Operations.Count, Is.EqualTo(1));
			Assert.That(merged.Operations[0].StableId, Is.EqualTo("g/a"));
		}

		[Test]
		public void MergeOperation_ReplacesSameKindAndTarget_KeepsOthers()
		{
			var first = new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "g/a",
				visibility: ViewVisibility.Never);
			var unrelated = new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "g/b",
				visibility: ViewVisibility.IfData);
			var patch = new ViewDefinitionOverride("LexEntry", "Normal", "detail",
				new[] { first, unrelated }, null);
			var replacement = new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "g/a",
				visibility: ViewVisibility.Always);

			var merged = ViewDefinitionOverrideEditor.MergeOperation(patch, replacement);

			Assert.That(merged.Operations.Count, Is.EqualTo(2), "the same target+kind is replaced, not duplicated");
			var aOp = merged.Operations.Single(o => o.StableId == "g/a");
			Assert.That(aOp.Visibility, Is.EqualTo(ViewVisibility.Always));
			Assert.That(merged.Operations.Single(o => o.StableId == "g/b").Visibility,
				Is.EqualTo(ViewVisibility.IfData), "an unrelated op is preserved");
		}

		[Test]
		public void MergeOperation_DifferentKindSameTarget_BothKept()
		{
			var vis = new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "g",
				visibility: ViewVisibility.Never);
			var reorder = new ViewOverrideOperation(ViewOverrideOperationKind.ReorderChildren, "g",
				childOrder: new[] { "g/b", "g/a" });
			var patch = new ViewDefinitionOverride("LexEntry", "Normal", "detail", new[] { vis }, null);

			var merged = ViewDefinitionOverrideEditor.MergeOperation(patch, reorder);

			Assert.That(merged.Operations.Count, Is.EqualTo(2),
				"a reorder on the same id as a visibility op is a different concern and is kept");
		}

		[Test]
		public void MergeOperation_DoesNotMutateInput()
		{
			var patch = new ViewDefinitionOverride("LexEntry", "Normal", "detail", null, null);
			ViewDefinitionOverrideEditor.MergeOperation(patch,
				new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "x",
					visibility: ViewVisibility.Never));
			Assert.That(patch.Operations.Count, Is.EqualTo(0), "the source override is never mutated");
		}
	}
}
