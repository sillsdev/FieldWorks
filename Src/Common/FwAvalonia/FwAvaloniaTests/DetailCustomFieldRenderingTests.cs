// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
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
		private static DetailModel Model(Func<SliceFactoryContext, Control> factory)
			=> new DetailModel("LexEntry", "Normal",
				new List<DetailField>
				{
					new DetailField("LexEntry/Normal/#0@1", "Messages", "Self", null,
						DetailFieldKind.Custom, EditorClassification.Dynamic, null, null,
						HostRouting.Product, null, null, null, isEditable: true, indent: 0,
						controlFactory: factory)
				},
				new List<ViewDiagnostic>());

		private static DataTree Show(DetailModel model, Action<DetailLinkRequest> linkRequested = null)
		{
			var view = new DataTree(model, linkRequested: linkRequested);
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

			var view = Show(Model(_ => pluginControl));

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
			var view = Show(Model(_ => throw new InvalidOperationException("plugin exploded")));

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
			var view = Show(Model(_ => null));

			Assert.That(FindUnsupportedBlock(view), Is.Not.Null,
				"a null-returning factory degrades to the explicit unsupported row");
		}

		[AvaloniaTest]
		public void CustomField_FactoryReceivesTheViewsLinkCallback()
		{
			var requests = new List<DetailLinkRequest>();
			Action<DetailLinkRequest> received = null;
			var model = Model(render =>
			{
				received = render.LinkRequested;
				return new TextBlock { Text = "plugin" };
			});
			Show(model, requests.Add);

			Assert.That(received, Is.Not.Null, "the plugin can reach the host's jump");
			received(new DetailLinkRequest(null, new DetailChooserLink("Show", "someTool")));
			Assert.That(requests, Has.Count.EqualTo(1), "the callback is the one the view was given");
		}

		// A collapse rebuilds the rows with new editors, so the ones it removes are disposed, and
		// expanding again builds fresh ones rather than reviving them.
		[AvaloniaTest]
		public void CollapsingASection_DisposesTheEditorsItRemoves()
		{
			var built = new List<DisposalSpy>();
			var model = new DetailModel("LexEntry", "Normal", new List<DetailField>
				{
					new DetailField("h", "Sense 1", null, null, DetailFieldKind.Header,
						EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null,
						isEditable: false, indent: 0, isCollapsible: true, isInitiallyExpanded: true),
					new DetailField("c", "Messages", "Self", null, DetailFieldKind.Custom,
						EditorClassification.Dynamic, null, null, HostRouting.Product, null, null, null,
						isEditable: true, indent: 1, controlFactory: _ =>
						{
							var spy = new DisposalSpy();
							built.Add(spy);
							return spy;
						})
				},
				new List<ViewDiagnostic>());
			var view = Show(model);
			var header = view.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == "h");

			header.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(built.Single().Disposals, Is.EqualTo(1), "the collapse disposed the editor it removed");

			header.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(built, Has.Count.EqualTo(2), "expanding builds a fresh editor");
			Assert.That(built[1].Disposals, Is.Zero, "and leaves the one it shows alone");
		}

		[AvaloniaTest]
		public void DisposingTheView_DisposesEachOfItsEditorsOnce()
		{
			var spy = new DisposalSpy();
			var view = Show(Model(_ => spy));

			view.Dispose();
			view.Dispose();

			Assert.That(spy.Disposals, Is.EqualTo(1));
		}

		// A plugin that cannot finish a write cancels through the view, not the session itself,
		// so the host re-shows every field from the domain.
		[AvaloniaTest]
		public void CustomField_FactoryReceivesTheViewsCancel_WhichAlsoCompletesTheEdit()
		{
			Action cancel = null;
			var model = Model(render =>
			{
				cancel = render.Cancel;
				return new TextBlock { Text = "plugin" };
			});
			var context = new FakeDetailEditContext();
			var view = new DataTree(model, editContext: context);
			var completed = 0;
			view.EditCompleted += (s, e) => completed++;
			var window = new Window { Content = view, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(cancel, Is.Not.Null, "an editable view hands its controls a cancel");
			cancel();

			Assert.That(context.CancelCount, Is.EqualTo(1), "the view's session was cancelled");
			Assert.That(completed, Is.EqualTo(1), "and the host is told to re-show");
		}
	}
}
