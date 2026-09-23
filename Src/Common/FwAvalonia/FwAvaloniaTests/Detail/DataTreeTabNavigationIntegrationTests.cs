// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot -- the PNG harness

namespace FwAvaloniaTests.Detail
{
	/// <summary>
	/// Headless proof for LT-22688's DataTree Tab/Shift+Tab row navigation, one test per
	/// navigation scenario: tab order, row-boundary containment, collapsed-row skipping, kebab
	/// exclusion, scroll-into-view, native focus state, multi-writing-system rows, and rich
	/// structured-text paragraphs. Every test drives Tab/Shift+Tab through the real headless
	/// input pipeline (never by calling internal handlers directly), so a regression in
	/// Avalonia's own tab-walk or in the host's key-claiming would fail these the same way a
	/// live user would notice it.
	/// </summary>
	[TestFixture]
	public class DataTreeTabNavigationIntegrationTests
	{
		private static DetailField Field(string id, DetailFieldKind kind = DetailFieldKind.Text,
			int indent = 0, string menuId = null, bool isCollapsible = false,
			bool isInitiallyExpanded = true, IReadOnlyList<DetailWsValue> values = null)
			=> new DetailField(id, id, id, null, kind,
				EditorClassification.Known, id, null, HostRouting.Inherit,
				kind == DetailFieldKind.Text
					? values ?? new List<DetailWsValue> { new DetailWsValue("vern", "value") }
					: null,
				null, null,
				isEditable: kind == DetailFieldKind.Text, indent: indent,
				isCollapsible: isCollapsible, isInitiallyExpanded: isInitiallyExpanded,
				menuId: menuId, objectHvo: 1234);

		// A non-null menuRequested is what makes WrapWithFieldMenu render a field's kebab at
		// all (a null host bridge means no menu button to test); the request content itself is
		// irrelevant here.
		private static (Window Window, DataTree View) Show(double width, double height,
			params DetailField[] fields)
		{
			var model = new DetailModel("LexEntry", "Normal", fields.ToList(), new List<ViewDiagnostic>());
			var view = new DataTree(model, menuRequested: request => { });
			var window = new Window { Content = view, Width = width, Height = height };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (window, view);
		}

		// Same as Show(), but threads a real edit context through -- a structured-text row
		// only wires up paragraph editing, and so the focus-swap, with one (LT-22688).
		private static (Window Window, DataTree View) ShowWithEditContext(double width, double height,
			IDetailEditContext editContext, params DetailField[] fields)
		{
			var model = new DetailModel("LexEntry", "Normal", fields.ToList(), new List<ViewDiagnostic>());
			var view = new DataTree(model, editContext: editContext, menuRequested: request => { });
			var window = new Window { Content = view, Width = width, Height = height };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (window, view);
		}

		private static T Find<T>(Visual root, string automationId) where T : Visual
			=> root.GetVisualDescendants().OfType<T>()
				.First(c => AutomationProperties.GetAutomationId(c) == automationId);

		private static Control FindFocused(Visual root)
			=> root.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.IsFocused);

		private static string FocusedAutomationId(Visual root)
		{
			var focused = FindFocused(root);
			return focused == null ? null : AutomationProperties.GetAutomationId(focused);
		}

		private static void Tab(Window window)
		{
			window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
			Dispatcher.UIThread.RunJobs();
		}

		private static void ShiftTab(Window window)
		{
			window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Shift);
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void TabOrderForward_VisitsEveryRowInModelOrder_NeverTheKebab()
		{
			var (window, view) = Show(480, 300,
				Field("Row0"),
				Field("Row1", menuId: "mnuDataTree-Help"),
				Field("Row2"),
				Field("Row3"));

			Find<TextBox>(view, "Row0.vern").Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Row0.vern"));

			var visited = new List<string>();
			for (var i = 0; i < 3; i++)
			{
				Tab(window);
				visited.Add(FocusedAutomationId(view));
			}

			Assert.That(visited, Is.EqualTo(new[] { "Row1.vern", "Row2.vern", "Row3.vern" }),
				"Tab visits every row once, in model order, and never the kebab");
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-01-tab-order-forward");
		}

		[AvaloniaTest]
		public void TabOrderReverse_MirrorsForwardExactly()
		{
			var (window, view) = Show(480, 300,
				Field("Row0"),
				Field("Row1", menuId: "mnuDataTree-Help"),
				Field("Row2"),
				Field("Row3"));

			Find<TextBox>(view, "Row3.vern").Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Row3.vern"));

			var visited = new List<string>();
			for (var i = 0; i < 3; i++)
			{
				ShiftTab(window);
				visited.Add(FocusedAutomationId(view));
			}

			Assert.That(visited, Is.EqualTo(new[] { "Row2.vern", "Row1.vern", "Row0.vern" }),
				"Shift+Tab retraces the forward order exactly in reverse");
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-02-tab-order-reverse");
		}

		[AvaloniaTest]
		public void ShiftTabAtFirstRow_StaysContained()
		{
			var (window, view) = Show(480, 300, Field("Row0"), Field("Row1"));

			Find<TextBox>(view, "Row0.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			ShiftTab(window);

			Assert.That(FocusedAutomationId(view), Is.EqualTo("Row0.vern"),
				"Contained navigation keeps Shift+Tab from leaving the view at the first row");
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-03-first-row-contained");
		}

		[AvaloniaTest]
		public void TabAtLastRow_StaysContained()
		{
			var (window, view) = Show(480, 300, Field("Row0"), Field("Row1"));

			Find<TextBox>(view, "Row1.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);

			Assert.That(FocusedAutomationId(view), Is.EqualTo("Row1.vern"),
				"Contained navigation keeps Tab from leaving the view at the last row");
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-04-last-row-contained");
		}

		[AvaloniaTest]
		public void Tab_SkipsRowsOwnedByACollapsedHeader()
		{
			var (window, view) = Show(480, 300,
				Field("Before"),
				Field("Section", kind: DetailFieldKind.Header, isCollapsible: true,
					isInitiallyExpanded: false),
				Field("Inner0", indent: 1),
				Field("Inner1", indent: 1),
				Field("After"));

			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);

			Assert.That(FocusedAutomationId(view), Is.EqualTo("After.vern"),
				"a collapsed section's own header and every row it owns are unreachable by Tab");
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-05-skips-collapsed-rows");
		}

		[AvaloniaTest]
		public void FieldMenuKebab_IsNeverATabStop()
		{
			var (window, view) = Show(480, 300, Field("Row0", menuId: "mnuDataTree-Help"));

			var kebab = Find<Button>(view, "Row0.FieldMenu");

			Assert.That(KeyboardNavigation.GetIsTabStop(kebab), Is.False,
				"the field-options kebab opts out of Tab explicitly, not just by accident of order");
			// Captures the hosting window, not `kebab` directly -- it is already parented
			// there, and DialogSnapshot would otherwise try to reparent it into a second window.
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-06-kebab-not-a-tab-stop");
		}

		[AvaloniaTest]
		public void TabToAnOffscreenRow_ScrollsItIntoView()
		{
			var fields = Enumerable.Range(0, 30).Select(i => Field("Row" + i)).ToArray();
			var (window, view) = Show(420, 180, fields);

			var scroller = view.GetVisualDescendants().OfType<ScrollViewer>()
				.First(s => AutomationProperties.GetAutomationId(s) == "DataTree.Scroll");
			Assert.That(scroller.Offset.Y, Is.EqualTo(0),
				"precondition: the view starts scrolled to the top");

			Find<TextBox>(view, "Row0.vern").Focus();
			Dispatcher.UIThread.RunJobs();
			for (var i = 0; i < 20; i++)
				Tab(window);

			Assert.That(FocusedAutomationId(view), Is.EqualTo("Row20.vern"));
			Assert.That(scroller.Offset.Y, Is.GreaterThan(0),
				"the far-below-the-fold row's own GotFocus handler scrolled it into view");
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-07-scroll-into-view");
		}

		[AvaloniaTest]
		public void TabThroughEveryRow_ReportsNativeFocusAtEachStop()
		{
			var (window, view) = Show(480, 300,
				Field("Row0"), Field("Row1"), Field("Row2"));

			var first = Find<TextBox>(view, "Row0.vern");
			first.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(first.IsFocused, Is.True);

			foreach (var id in new[] { "Row1.vern", "Row2.vern" })
			{
				Tab(window);
				var focused = Find<TextBox>(view, id);
				Assert.That(focused.IsFocused, Is.True,
					$"native Avalonia focus state is set on {id} without any DataTree-level highlight");
			}
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-08-focus-visual-native");
		}

		[AvaloniaTest]
		public void TabIntoAMultiWsRow_VisitsBothWritingSystemsBeforeAdvancing()
		{
			var (window, view) = Show(480, 300,
				Field("Before"),
				Field("Multi", values: new List<DetailWsValue>
				{
					new DetailWsValue("vern", "one"),
					new DetailWsValue("en", "two")
				}),
				Field("After"));

			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"),
				"Tab first reaches the row's own first writing-system sub-editor");

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.en"),
				"a second Tab stays inside the row, moving to its second writing system, " +
				"before any DataTree-level row-to-row navigation applies");

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("After.vern"),
				"once every writing system in the row is visited, Tab advances to the next row");
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-09-multi-ws-internal-tab-regression");
		}

		[AvaloniaTest]
		public void TabIntoARichStructuredTextParagraph_FocusesItsEditor()
		{
			// Two runs in different fonts force the per-run-font display path (LT-22688) --
			// the same swap-on-focus pattern used by FwMultiWsTextField.
			var richParagraph = new DetailParagraph(DetailRichTextEditAlgorithms.FromRuns(
				"OneTwo", new List<DetailTextRun>
				{
					new DetailTextRun("One", fontFamily: "Arial"),
					new DetailTextRun("Two", fontFamily: "Times New Roman")
				}));

			var structuredField = new DetailField("Multi", "Multi", "Multi", null,
				DetailFieldKind.StructuredText, EditorClassification.Known, "Multi", null,
				HostRouting.Inherit, null, null, null, isEditable: true, objectHvo: 1234,
				paragraphs: new List<DetailParagraph> { richParagraph });

			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), structuredField, Field("After"));

			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.0"),
				"Tab reaches the rich paragraph's editable box, not its per-run-font display");

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("After.vern"),
				"once the paragraph is visited, Tab advances to the next row");
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-10-rich-structured-text-tab");
		}
	}
}
