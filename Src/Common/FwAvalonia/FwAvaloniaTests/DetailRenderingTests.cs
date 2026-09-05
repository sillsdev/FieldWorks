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
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	[TestFixture]
	public class DetailRenderingTests
	{
		private static DetailField TextField(string id, string label)
			=> new DetailField(id, label, label, null, DetailFieldKind.Text,
				EditorClassification.Known, id, null, HostRouting.Inherit,
				new List<DetailWsValue> { new DetailWsValue("en", "value") },
				null, null, isEditable: true, indent: 0, objectHvo: 1234);

		private static DataTree Render(params DetailField[] fields)
		{
			var model = new DetailModel("LexEntry", "Normal", fields.ToList(),
				new List<ViewDiagnostic>());
			var view = new DataTree(model, null, null, null, null, null);
			var window = new Window { Content = view, Width = 480, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		private static List<string> RenderedLabelIds(DataTree view)
			=> view.GetVisualDescendants().OfType<TextBlock>()
				.Select(t => AutomationProperties.GetAutomationId(t))
				.Where(id => !string.IsNullOrEmpty(id) && id.EndsWith(".Label"))
				.ToList();

		[AvaloniaTest]
		public void DetailView_RendersOnlyTheRowsInTheModel()
		{
			var view = Render(TextField("a", "Alpha"), TextField("c", "Gamma"));

			var labels = RenderedLabelIds(view);
			Assert.That(labels, Has.Member("a.Label"));
			Assert.That(labels, Has.Member("c.Label"));
			Assert.That(labels, Has.No.Member("b.Label"),
				"a row omitted from the model does not render");
		}

		[AvaloniaTest]
		public void DetailView_RendersRowsInModelOrder_SoAReorderIsVisible()
		{
			var view = Render(TextField("c", "Gamma"), TextField("a", "Alpha"),
				TextField("b", "Beta"));

			var order = RenderedLabelIds(view);
			Assert.That(order.IndexOf("c.Label"), Is.LessThan(order.IndexOf("a.Label")));
			Assert.That(order.IndexOf("a.Label"), Is.LessThan(order.IndexOf("b.Label")),
				"rows render in model order");
		}
	}
}
