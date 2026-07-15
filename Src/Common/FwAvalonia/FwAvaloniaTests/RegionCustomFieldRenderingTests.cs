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
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D1 (wave 1) — a <see cref="RegionFieldKind.Custom"/> row
	/// renders its plugin control factory's Avalonia control in-tree in the value column, at the
	/// slice's real position. The lane is guarded: a missing, null-returning, or throwing factory
	/// degrades to the explicit unsupported row — never a crash, never a silently blank row.
	/// </summary>
	[TestFixture]
	public class RegionCustomFieldRenderingTests
	{
		private static LexicalEditRegionModel Model(Func<Control> factory)
			=> new LexicalEditRegionModel("LexEntry", "Normal",
				new List<LexicalEditRegionField>
				{
					new LexicalEditRegionField("LexEntry/Normal/#0@1", "Messages", "Self", null,
						RegionFieldKind.Custom, EditorClassification.Dynamic, null, null,
						SurfaceRouting.Product, null, null, null, isEditable: true, indent: 0,
						controlFactory: factory)
				},
				new List<ViewDiagnostic>());

		private static LexicalEditRegionView Show(LexicalEditRegionModel model)
		{
			var view = new LexicalEditRegionView(model);
			var window = new Window { Content = view, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		private static TextBlock FindUnsupportedBlock(LexicalEditRegionView view)
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
				"the factory's control renders inside the region view");
			Assert.That(Grid.GetColumn(pluginControl), Is.EqualTo(2),
				"the plugin control occupies the value column; the label stays in the gutter");
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
