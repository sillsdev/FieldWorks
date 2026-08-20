// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using Ursa.Controls;

namespace FwAvaloniaTests
{
	/// <summary>
	/// A <see cref="DetailFieldKind.Custom"/> row
	/// renders its plugin control factory's Avalonia control in-tree in the value column, at the
	/// slice's real position. The path is guarded: a missing, null-returning, or throwing factory
	/// degrades to the explicit unsupported row -- never a crash, never a silently blank row.
	/// </summary>
	[TestFixture]
	public class DetailCustomFieldRenderingTests
	{
		private static DetailModel Model(Func<Control> factory)
			=> new DetailModel("LexEntry", "Normal",
				new List<DetailField>
				{
					new DetailField("LexEntry/Normal/#0@1", "Messages", "Self", null,
						DetailFieldKind.Custom, EditorClassification.Dynamic, null, null,
						HostRouting.Product, null, null, null, isEditable: true, indent: 0,
						controlFactory: factory)
				},
				new List<ViewDiagnostic>());

		private static DataTree Show(DetailModel model)
		{
			var view = new DataTree(model);
			var window = new Window { Content = view, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		private static TextBlock FindUnsupportedBlock(DataTree view)
			=> view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => t.Text == FwAvaloniaStrings.UnsupportedEditor);

		[AvaloniaTest]
		public void CustomField_RendersTheFactoryControl_InTheValueColumn()
		{
			var pluginControl = new TextBlock { Text = "plugin notes bar" };
			AutomationProperties.SetAutomationId(pluginControl, "PluginNotesBar");

			var view = Show(Model(() => pluginControl));

			var rendered = view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == "PluginNotesBar");
			Assert.That(rendered, Is.SameAs(pluginControl),
				"the factory's control renders inside the detail view");

			// The plugin control IS the Form item's value content; Ursa reads the label from
			// FormItem.Label on that same control, so the label lives in the Form's own label
			// slot, not inside the plugin's content.
			var label = FormItem.GetLabel(pluginControl) as TextBlock;
			Assert.That(label, Is.Not.Null,
				"the field's label rides the Form item's label slot, not the plugin control's content");
			Assert.That(label.Text, Is.EqualTo("Messages"),
				"the label slot carries the field's own label text, distinct from the plugin control");
			Assert.That(FindUnsupportedBlock(view), Is.Null,
				"a working factory never shows the unsupported text");
		}

		[AvaloniaTest]
		public void CustomField_WithThrowingFactory_FallsBackToTheUnsupportedRow()
		{
			var view = Show(Model(() => throw new InvalidOperationException("plugin exploded")));

			Assert.That(FindUnsupportedBlock(view), Is.Not.Null,
				"a throwing factory degrades to the explicit unsupported row");
		}

		[AvaloniaTest]
		public void CustomField_WithoutAFactory_FallsBackToTheUnsupportedRow()
		{
			var view = Show(Model(null));

			Assert.That(FindUnsupportedBlock(view), Is.Not.Null,
				"a Custom row without a factory degrades to the explicit unsupported row");
		}

		[AvaloniaTest]
		public void CustomField_WithNullReturningFactory_FallsBackToTheUnsupportedRow()
		{
			var view = Show(Model(() => null));

			Assert.That(FindUnsupportedBlock(view), Is.Not.Null,
				"a null-returning factory degrades to the explicit unsupported row");
		}
	}
}
