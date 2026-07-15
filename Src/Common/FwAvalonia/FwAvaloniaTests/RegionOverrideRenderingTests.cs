// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// advanced-entry-view (view layer): the per-field gear-menu commands work by changing the composed
	/// model the surface renders — hiding a Never row, showing a non-empty IfData row, and reordering
	/// siblings. These headless tests prove the <see cref="LexicalEditRegionView"/> renders EXACTLY the
	/// rows the (patched) model carries, in model order. The composer's filtering/reorder semantics are
	/// covered in xWorksTests; here we prove the visible surface follows the model so the round trip is
	/// closed at the rendering edge.
	/// </summary>
	[TestFixture]
	public class RegionOverrideRenderingTests
	{
		private static LexicalEditRegionField TextField(string id, string label)
			=> new LexicalEditRegionField(id, label, label, null, RegionFieldKind.Text,
				EditorClassification.Known, id, null, SurfaceRouting.Inherit,
				new List<RegionWsValue> { new RegionWsValue("en", "value") },
				null, null, isEditable: true, indent: 0, objectHvo: 1234);

		private static LexicalEditRegionView Render(params LexicalEditRegionField[] fields)
		{
			var model = new LexicalEditRegionModel("LexEntry", "Normal", fields.ToList(),
				new List<ViewDiagnostic>());
			var view = new LexicalEditRegionView(model, null, null, null, null, null);
			var window = new Window { Content = view, Width = 480, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		private static List<string> RenderedLabelIds(LexicalEditRegionView view)
			=> view.GetVisualDescendants().OfType<TextBlock>()
				.Select(t => AutomationProperties.GetAutomationId(t))
				.Where(id => !string.IsNullOrEmpty(id) && id.EndsWith(".Label"))
				.ToList();

		[AvaloniaTest]
		public void Surface_RendersOnlyTheRowsInTheModel()
		{
			// A model with "B" hidden (as a Never visibility override would drop it from compose) shows
			// only A and C.
			var view = Render(TextField("a", "Alpha"), TextField("c", "Gamma"));

			var labels = RenderedLabelIds(view);
			Assert.That(labels, Has.Member("a.Label"));
			Assert.That(labels, Has.Member("c.Label"));
			Assert.That(labels, Has.No.Member("b.Label"),
				"a row omitted from the model (a hidden field) does not render");
		}

		[AvaloniaTest]
		public void Surface_RendersRowsInModelOrder_SoAReorderIsVisible()
		{
			// The reorder override produces a model whose fields are in the new order; the surface must
			// follow that order top-to-bottom.
			var view = Render(TextField("c", "Gamma"), TextField("a", "Alpha"), TextField("b", "Beta"));

			var order = RenderedLabelIds(view);
			Assert.That(order.IndexOf("c.Label"), Is.LessThan(order.IndexOf("a.Label")));
			Assert.That(order.IndexOf("a.Label"), Is.LessThan(order.IndexOf("b.Label")),
				"rows render in model order, so a reordered model reorders the surface");
		}

		// The applier (Layer 2 → Layer 3) is what the composer runs at CompileForObject: prove the
		// patched IR the surface is built from carries the visibility/order the menu wrote, end to end
		// from the override operations to the model the composer would walk.
		[Test]
		public void Applier_AppliesVisibilityAndReorder_ToTheCompiledIR()
		{
			var shipped = new ViewDefinitionModel("LexEntry", "Normal", "detail", new[]
			{
				Group("g", FieldNode("g/a"), FieldNode("g/b"), FieldNode("g/c"))
			}, null);
			var patch = new ViewDefinitionOverride("LexEntry", "Normal", "detail", new[]
			{
				new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, "g/a",
					visibility: ViewVisibility.Never),
				new ViewOverrideOperation(ViewOverrideOperationKind.ReorderChildren, "g",
					childOrder: new[] { "g/c", "g/b", "g/a" })
			}, null);

			var applied = ViewDefinitionOverrideApplier.Apply(shipped, patch);

			var children = applied.Roots[0].Children;
			Assert.That(children.Select(c => c.StableId), Is.EqualTo(new[] { "g/c", "g/b", "g/a" }),
				"the reorder op reorders the children the composer walks");
			Assert.That(children.Single(c => c.StableId == "g/a").Visibility,
				Is.EqualTo(ViewVisibility.Never), "the visibility op flips the node's visibility");
		}

		private static ViewNode FieldNode(string id)
			=> new ViewNode(id, ViewNodeKind.Field, id, null, "F", "string",
				EditorClassification.Known, "vern", ViewVisibility.Always, ViewExpansion.NotApplicable,
				false, null, null);

		private static ViewNode Group(string id, params ViewNode[] children)
			=> new ViewNode(id, ViewNodeKind.Group, id, null, null, null,
				EditorClassification.GroupingNone, null, ViewVisibility.Always, ViewExpansion.Expanded,
				false, null, children);
	}
}
