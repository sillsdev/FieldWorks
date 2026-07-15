// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 9.2 (override migrator): the per-project override patch serializes to deterministic canonical
	/// JSON and round-trips losslessly, including its audit diagnostics. Pure logic — no Avalonia runtime.
	/// </summary>
	[TestFixture]
	public class ViewDefinitionOverrideJsonSerializerTests
	{
		private static ViewDefinitionOverride SampleWithAllOpKinds()
		{
			var ops = new List<ViewOverrideOperation>
			{
				new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "a", visibility: ViewVisibility.Never),
				new ViewOverrideOperation(ViewOverrideOperationKind.SetLabel, "b", label: "Headword"),
				new ViewOverrideOperation(ViewOverrideOperationKind.ReorderChildren, "g",
					childOrder: new[] { "g/b", "g/a" }),
				new ViewOverrideOperation(ViewOverrideOperationKind.HideNode, "c")
			};
			var diags = new List<ViewDiagnostic>
			{
				new ViewDiagnostic(ViewDiagnosticSeverity.Info, "override-added-node", "customer-added", "g/x")
			};
			return new ViewDefinitionOverride("LexEntry", "detail", "jtview", ops, diags);
		}

		[Test]
		public void RoundTrip_PreservesHeaderOperationsAndDiagnostics()
		{
			var original = SampleWithAllOpKinds();

			var json = ViewDefinitionOverrideJsonSerializer.Serialize(original);
			var restored = ViewDefinitionOverrideJsonSerializer.Deserialize(json);

			Assert.That(restored.FormatVersion, Is.EqualTo(original.FormatVersion));
			Assert.That(restored.ClassName, Is.EqualTo("LexEntry"));
			Assert.That(restored.LayoutName, Is.EqualTo("detail"));
			Assert.That(restored.LayoutType, Is.EqualTo("jtview"));

			Assert.That(restored.Operations.Count, Is.EqualTo(4));

			var vis = restored.Operations.Single(o => o.Kind == ViewOverrideOperationKind.SetVisibility);
			Assert.That(vis.StableId, Is.EqualTo("a"));
			Assert.That(vis.Visibility, Is.EqualTo(ViewVisibility.Never));

			var label = restored.Operations.Single(o => o.Kind == ViewOverrideOperationKind.SetLabel);
			Assert.That(label.StableId, Is.EqualTo("b"));
			Assert.That(label.Label, Is.EqualTo("Headword"));

			var reorder = restored.Operations.Single(o => o.Kind == ViewOverrideOperationKind.ReorderChildren);
			Assert.That(reorder.StableId, Is.EqualTo("g"));
			Assert.That(reorder.ChildOrder, Is.EqualTo(new[] { "g/b", "g/a" }));

			var hide = restored.Operations.Single(o => o.Kind == ViewOverrideOperationKind.HideNode);
			Assert.That(hide.StableId, Is.EqualTo("c"));

			Assert.That(restored.Diagnostics.Count, Is.EqualTo(1));
			Assert.That(restored.Diagnostics[0].Code, Is.EqualTo("override-added-node"));
			Assert.That(restored.Diagnostics[0].Severity, Is.EqualTo(ViewDiagnosticSeverity.Info));
			Assert.That(restored.Diagnostics[0].NodePath, Is.EqualTo("g/x"));
		}

		[Test]
		public void Serialize_IsDeterministic()
		{
			var patch = SampleWithAllOpKinds();
			Assert.That(ViewDefinitionOverrideJsonSerializer.Serialize(patch),
				Is.EqualTo(ViewDefinitionOverrideJsonSerializer.Serialize(patch)));
		}

		[Test]
		public void Serialize_OmitsDiagnostics_WhenThereAreNone()
		{
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[] { new ViewOverrideOperation(ViewOverrideOperationKind.HideNode, "c") },
				diagnostics: null);

			var json = ViewDefinitionOverrideJsonSerializer.Serialize(patch);

			Assert.That(json, Does.Not.Contain("diagnostics"),
				"a clean override must not carry an empty diagnostics array");
		}

		[Test]
		public void Deserialize_WrongFormatVersion_Throws()
		{
			const string json = "{ \"formatVersion\": 99, \"class\": \"LexEntry\", \"operations\": [] }";
			Assert.That(() => ViewDefinitionOverrideJsonSerializer.Deserialize(json),
				Throws.TypeOf<InvalidDataException>());
		}

		[Test]
		public void RoundTrip_PreservesAddNode_WithParentIndexAndIdentity()
		{
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewOverrideOperation(ViewOverrideOperationKind.AddNode, "g/custom",
						visibility: ViewVisibility.IfData, label: "Custom",
						parentStableId: "g", index: 2, nodeKind: ViewNodeKind.Field,
						field: "Custom", editor: "string")
				},
				diagnostics: null);

			var restored = ViewDefinitionOverrideJsonSerializer.Deserialize(
				ViewDefinitionOverrideJsonSerializer.Serialize(patch));

			var add = restored.Operations.Single(o => o.Kind == ViewOverrideOperationKind.AddNode);
			Assert.That(add.StableId, Is.EqualTo("g/custom"));
			Assert.That(add.ParentStableId, Is.EqualTo("g"));
			Assert.That(add.Index, Is.EqualTo(2));
			Assert.That(add.NodeKind, Is.EqualTo(ViewNodeKind.Field));
			Assert.That(add.Label, Is.EqualTo("Custom"));
			Assert.That(add.Field, Is.EqualTo("Custom"));
			Assert.That(add.Editor, Is.EqualTo("string"));
			Assert.That(add.Visibility, Is.EqualTo(ViewVisibility.IfData));
		}

		[Test]
		public void RoundTrip_PreservesDuplicateNode()
		{
			var patch = new ViewDefinitionOverride("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewOverrideOperation(ViewOverrideOperationKind.DuplicateNode, "g/a-copy",
						parentStableId: "g", index: 1, sourceStableId: "g/a")
				}, null);

			var restored = ViewDefinitionOverrideJsonSerializer.Deserialize(
				ViewDefinitionOverrideJsonSerializer.Serialize(patch));

			var dup = restored.Operations.Single(o => o.Kind == ViewOverrideOperationKind.DuplicateNode);
			Assert.That(dup.StableId, Is.EqualTo("g/a-copy"));
			Assert.That(dup.SourceStableId, Is.EqualTo("g/a"));
			Assert.That(dup.ParentStableId, Is.EqualTo("g"));
			Assert.That(dup.Index, Is.EqualTo(1));
		}

		[Test]
		public void DiffThenSerialize_RoundTrips()
		{
			var shipped = new ViewDefinitionModel("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewNode("a", ViewNodeKind.Field, "A", null, "F", "string",
						EditorClassification.Known, "vern", ViewVisibility.Always, ViewExpansion.NotApplicable,
						false, null, null)
				}, null);
			var overridden = new ViewDefinitionModel("LexEntry", "detail", "jtview",
				new[]
				{
					new ViewNode("a", ViewNodeKind.Field, "A", null, "F", "string",
						EditorClassification.Known, "vern", ViewVisibility.Never, ViewExpansion.NotApplicable,
						false, null, null)
				}, null);

			var diff = ViewDefinitionOverrideDiffer.Diff(shipped, overridden);
			var restored = ViewDefinitionOverrideJsonSerializer.Deserialize(
				ViewDefinitionOverrideJsonSerializer.Serialize(diff));

			Assert.That(restored.Operations.Count, Is.EqualTo(1));
			Assert.That(restored.Operations[0].Kind, Is.EqualTo(ViewOverrideOperationKind.SetVisibility));
			Assert.That(restored.Operations[0].Visibility, Is.EqualTo(ViewVisibility.Never));
		}
	}
}
