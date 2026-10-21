// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
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

		private static void Press(Window window, PhysicalKey key)
		{
			window.KeyPressQwerty(key, RawInputModifiers.None);
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

		private static DetailField MultiWsField()
			=> Field("Multi", values: new List<DetailWsValue>
			{
				new DetailWsValue("vern", "one"),
				new DetailWsValue("en", "two"),
				new DetailWsValue("fr", "three")
			});

		[AvaloniaTest]
		public void TabThroughAMultiWsRow_StopsOnlyAtTheFirstWritingSystem()
		{
			var (window, view) = Show(480, 300, Field("Before"), MultiWsField(), Field("After"));

			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"),
				"Tab first reaches the row's first writing system");

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("After.vern"),
				"the other writing systems are not Tab stops, so Tab advances to the next row");

			ShiftTab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"),
				"Shift+Tab re-enters the row at its first writing system");

			ShiftTab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Before.vern"),
				"and the next Shift+Tab leaves the row for the previous one");
			DialogSnapshot.Capture(window, "DataTree-TabNavigation-09-multi-ws-single-tab-stop");
		}

		[AvaloniaTest]
		public void ShiftTabFromALaterWritingSystem_LeavesTheRow()
		{
			var (window, view) = Show(480, 300, Field("Before"), MultiWsField(), Field("After"));

			Find<TextBox>(view, "Multi.en").Focus();
			Dispatcher.UIThread.RunJobs();

			ShiftTab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Before.vern"),
				"Shift+Tab goes to the previous row, not back to the row's first writing system");
		}

		[AvaloniaTest]
		public void TabFromALaterWritingSystem_LeavesTheRow()
		{
			var (window, view) = Show(480, 300, Field("Before"), MultiWsField(), Field("After"));

			Find<TextBox>(view, "Multi.en").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("After.vern"),
				"Tab goes to the next row, not on to the row's last writing system");
		}

		[AvaloniaTest]
		public void UpAndDownInAMultiWsRow_MoveBetweenWritingSystems()
		{
			var (window, view) = Show(480, 300, Field("Before"), MultiWsField(), Field("After"));

			Find<TextBox>(view, "Multi.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Press(window, PhysicalKey.ArrowDown);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.en"));
			Press(window, PhysicalKey.ArrowDown);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.fr"));
			Press(window, PhysicalKey.ArrowDown);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.fr"),
				"Down on the last writing system stays put; it never leaves the row");

			Press(window, PhysicalKey.ArrowUp);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.en"));
			Press(window, PhysicalKey.ArrowUp);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"));
			Press(window, PhysicalKey.ArrowUp);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"),
				"Up on the first writing system stays put; it never leaves the row");
		}

		private static DetailField WrappingMultiWsField()
			=> Field("Wrap", values: new List<DetailWsValue>
			{
				new DetailWsValue("vern", string.Join(" ", Enumerable.Repeat("alpha beta gamma", 12))),
				new DetailWsValue("en", string.Join(" ", Enumerable.Repeat("delta epsilon zeta", 10)))
			});

		private static TextPresenter PresenterOf(TextBox box)
			=> box.GetVisualDescendants().OfType<TextPresenter>().First();

		// The laid-out top of the line holding the character at index, rounded so equal lines
		// compare equal.
		private static double LineTopAt(TextBox box, int index)
			=> Math.Round(PresenterOf(box).TextLayout.HitTestTextPosition(index).Top, 1);

		private static double CaretX(Visual root, TextBox box)
		{
			var presenter = PresenterOf(box);
			var caret = presenter.TextLayout.HitTestTextPosition(box.CaretIndex);
			return presenter.TranslatePoint(new Point(caret.X, 0), root).Value.X;
		}

		[AvaloniaTest]
		public void UpAndDownInWrappedText_MoveWithinTheValueBeforeLeavingIt()
		{
			// Window is chosen small enough to force vern and en values to wrap
			// to at least three lines.
			var (window, view) = Show(240, 400, Field("Before"), WrappingMultiWsField(),
				Field("After"));
			var vern = Find<TextBox>(view, "Wrap.vern");
			var en = Find<TextBox>(view, "Wrap.en");
			var length = vern.Text.Length;
			var tops = Enumerable.Range(0, length + 1).Select(i => LineTopAt(vern, i))
				.Distinct().OrderBy(t => t).ToList();
			Assert.That(tops.Count, Is.GreaterThanOrEqualTo(3),
				"the value must wrap to at least three lines for this test to mean anything");

			vern.Focus();
			vern.CaretIndex = 0;
			Dispatcher.UIThread.RunJobs();
			Press(window, PhysicalKey.ArrowDown);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Wrap.vern"),
				"Down from the first line of a wrapped value stays in it");
			Assert.That(LineTopAt(vern, vern.CaretIndex), Is.EqualTo(tops[1]).Within(1),
				"and moves the caret to the second line");

			var lastLineStart = Enumerable.Range(0, length + 1)
				.First(i => LineTopAt(vern, i) == tops[tops.Count - 1]);
			vern.CaretIndex = Math.Min(lastLineStart + 4, length);
			var x = CaretX(view, vern);
			Press(window, PhysicalKey.ArrowDown);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Wrap.en"),
				"Down from the last line moves to the next writing system");
			Assert.That(LineTopAt(en, en.CaretIndex), Is.EqualTo(LineTopAt(en, 0)).Within(1),
				"landing on its first line");

			// Check that caret is within 15 px of the horizontal position it left in previous
			// value. The 15 px allowance is chosen to be roughly one character width.
			Assert.That(CaretX(view, en), Is.EqualTo(x).Within(15),
				"near the horizontal position the caret left");

			x = CaretX(view, en);
			Press(window, PhysicalKey.ArrowUp);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Wrap.vern"),
				"Up from the first line moves to the previous writing system");
			Assert.That(LineTopAt(vern, vern.CaretIndex), Is.EqualTo(tops[tops.Count - 1]).Within(1),
				"landing on its last line");

			// Check that caret is within 15 px of the horizontal position it left in previous
			// value. The 15 px allowance is chosen to be roughly one character width.
			Assert.That(CaretX(view, vern), Is.EqualTo(x).Within(15),
				"near the horizontal position the caret left");
		}

		[AvaloniaTest]
		public void LeftAndRightAtTheTextEdges_MoveBetweenWritingSystems()
		{
			var (window, view) = Show(480, 300, Field("Before"), MultiWsField(), Field("After"));
			var vern = Find<TextBox>(view, "Multi.vern");
			var en = Find<TextBox>(view, "Multi.en");

			vern.Focus();
			vern.CaretIndex = 1;
			Dispatcher.UIThread.RunJobs();
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"),
				"Right inside the text moves the caret and stays in the same writing system");

			vern.CaretIndex = vern.Text.Length;
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.en"),
				"Right at the end of the text moves on to the next writing system");
			Assert.That(en.CaretIndex, Is.EqualTo(0), "at its start");

			Press(window, PhysicalKey.ArrowLeft);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"),
				"Left at the start of the text moves back to the previous writing system");
			Assert.That(vern.CaretIndex, Is.EqualTo(vern.Text.Length), "at its end");
		}

		[AvaloniaTest]
		public void LeftAndRightAtTheTextEdges_MoveBetweenWritingSystems_InEditableRows()
		{
			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), MultiWsField(), Field("After"));
			var vern = Find<TextBox>(view, "Multi.vern");
			var en = Find<TextBox>(view, "Multi.en");

			vern.Focus();
			vern.CaretIndex = 1;
			Dispatcher.UIThread.RunJobs();
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"),
				"Right inside the text moves the caret within the same writing system");
			Assert.That(vern.CaretIndex, Is.EqualTo(2));

			vern.CaretIndex = vern.Text.Length;
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.en"),
				"Right at the end of an editable value moves on to the next writing system");
			Assert.That(en.CaretIndex, Is.EqualTo(0));

			Press(window, PhysicalKey.ArrowLeft);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"),
				"Left at the start of an editable value moves back");
			Assert.That(vern.CaretIndex, Is.EqualTo(vern.Text.Length));
		}

		[AvaloniaTest]
		public void ArrowsInAMultiWsRow_DoNotLeaveItsFirstOrLastEdge()
		{
			var (window, view) = Show(480, 300, Field("Before"), MultiWsField(), Field("After"));
			var vern = Find<TextBox>(view, "Multi.vern");

			vern.Focus();
			vern.CaretIndex = 0;
			Dispatcher.UIThread.RunJobs();
			Press(window, PhysicalKey.ArrowLeft);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"),
				"Left at the start of the first writing system has nowhere to go");

			var fr = Find<TextBox>(view, "Multi.fr");
			fr.Focus();
			fr.CaretIndex = fr.Text.Length;
			Dispatcher.UIThread.RunJobs();
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.fr"),
				"Right at the end of the last writing system has nowhere to go");
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

		private static DetailField StructuredField(params DetailParagraph[] paragraphs)
			=> new DetailField("Multi", "Multi", "Multi", null,
				DetailFieldKind.StructuredText, EditorClassification.Known, "Multi", null,
				HostRouting.Inherit, null, null, null, isEditable: true, objectHvo: 1234,
				paragraphs: paragraphs.ToList());

		private static DetailParagraph RichParagraph()
			=> new DetailParagraph(DetailRichTextEditAlgorithms.FromRuns(
				"OneTwo", new List<DetailTextRun>
				{
					new DetailTextRun("One", fontFamily: "Arial"),
					new DetailTextRun("Two", fontFamily: "Times New Roman")
				}));

		private static DetailParagraph PlainParagraph()
			=> new DetailParagraph(DetailRichTextEditAlgorithms.FromRuns(
				"Plain", new List<DetailTextRun> { new DetailTextRun("Plain") }));

		private static DetailField RtlMiddleParagraphField(bool mixedRuns)
		{
			var runs = mixedRuns
				? new List<DetailTextRun>
				{
					new DetailTextRun("abc", "ar", fontFamily: "Arial"),
					new DetailTextRun("de", "ar", fontFamily: "Times New Roman")
				}
				: new List<DetailTextRun> { new DetailTextRun("abcde", "ar") };
			var field = StructuredField(PlainParagraph(),
				new DetailParagraph(DetailRichTextEditAlgorithms.FromRuns("abcde", runs)),
				PlainParagraph());
			field.WritingSystemFonts = new Dictionary<string, DetailRunFont>
			{
				["ar"] = new DetailRunFont("Arial", rightToLeft: true)
			};
			return field;
		}

		private static void AssertRtlParagraphCrossesAtItsOwnEdges(bool mixedRuns)
		{
			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), RtlMiddleParagraphField(mixedRuns), Field("After"));
			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();
			Tab(window);
			Press(window, PhysicalKey.ArrowDown);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.1"));
			var rtl = Find<TextBox>(view, "Multi.Para.1");
			Assert.That(rtl.FlowDirection, Is.EqualTo(Avalonia.Media.FlowDirection.RightToLeft),
				"the editor takes its first run's direction");

			rtl.CaretIndex = 0;
			Press(window, PhysicalKey.ArrowLeft);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.1"),
				"Left at the logical start of an RTL paragraph does not leave it");
			rtl.CaretIndex = 0;
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.0"),
				"Right at the logical start of an RTL paragraph moves to the previous one");

			Press(window, PhysicalKey.ArrowDown);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.1"));
			rtl.CaretIndex = rtl.Text.Length;
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.1"),
				"Right at the logical end of an RTL paragraph does not leave it");
			rtl.CaretIndex = rtl.Text.Length;
			Press(window, PhysicalKey.ArrowLeft);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.2"),
				"Left at the logical end of an RTL paragraph moves to the next one");
		}

		[AvaloniaTest]
		public void RtlStructuredTextParagraph_CrossesAtItsOwnEdges()
			=> AssertRtlParagraphCrossesAtItsOwnEdges(mixedRuns: false);

		[AvaloniaTest]
		public void RtlStructuredTextParagraphWithDisplay_CrossesAtItsOwnEdges()
			=> AssertRtlParagraphCrossesAtItsOwnEdges(mixedRuns: true);

		private static DetailField RtlFirstRunParagraphField(params DetailTextRun[] runs)
		{
			var text = string.Concat(runs.Select(r => r.Text));
			var field = StructuredField(PlainParagraph(),
				new DetailParagraph(DetailRichTextEditAlgorithms.FromRuns(text, runs.ToList())),
				PlainParagraph());
			field.WritingSystemFonts = new Dictionary<string, DetailRunFont>
			{
				["ar"] = new DetailRunFont("Arial", rightToLeft: true),
				["en"] = new DetailRunFont("Arial")
			};
			return field;
		}

		private static (Window Window, DataTree View, TextBox Box) ShowMiddleParagraph(
			DetailField field)
		{
			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), field, Field("After"));
			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();
			Tab(window);
			Press(window, PhysicalKey.ArrowDown);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.1"));
			return (window, view, Find<TextBox>(view, "Multi.Para.1"));
		}

		[AvaloniaTest]
		public void EditingAwayTheRtlFirstRun_FlipsTheParagraphToTheNewFirstRunsDirection()
		{
			var (window, view, box) = ShowMiddleParagraph(RtlFirstRunParagraphField(
				new DetailTextRun("abc", "ar"), new DetailTextRun("def", "en")));
			Assert.That(box.FlowDirection, Is.EqualTo(Avalonia.Media.FlowDirection.RightToLeft));

			box.Text = "def";
			Dispatcher.UIThread.RunJobs();

			Assert.That(box.FlowDirection, Is.EqualTo(Avalonia.Media.FlowDirection.LeftToRight),
				"the paragraph takes the direction of its new first run");
			box.CaretIndex = 0;
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.1"),
				"Right at the start of a left-to-right paragraph moves within it");
		}

		[AvaloniaTest]
		public void EmptyingAnRtlParagraph_KeepsItsDirection()
		{
			var (_, _, box) = ShowMiddleParagraph(
				RtlFirstRunParagraphField(new DetailTextRun("abcde", "ar")));

			box.Text = string.Empty;
			Dispatcher.UIThread.RunJobs();
			Assert.That(box.FlowDirection, Is.EqualTo(Avalonia.Media.FlowDirection.RightToLeft),
				"an empty paragraph has no first run to take a direction from");

			box.Text = "x";
			Dispatcher.UIThread.RunJobs();
			Assert.That(box.FlowDirection, Is.EqualTo(Avalonia.Media.FlowDirection.RightToLeft),
				"text typed into an emptied paragraph is untagged, so the direction stays");
		}

		[AvaloniaTest]
		public void ReplacingTextInAnRtlFirstRun_KeepsTheParagraphRtl()
		{
			var (_, _, box) = ShowMiddleParagraph(
				RtlFirstRunParagraphField(new DetailTextRun("abcde", "ar")));

			box.Text = "abXde";
			Dispatcher.UIThread.RunJobs();

			Assert.That(box.FlowDirection, Is.EqualTo(Avalonia.Media.FlowDirection.RightToLeft));
		}

		[AvaloniaTest]
		public void TabThroughStructuredTextParagraphs_StopsOnlyAtTheFirst()
		{
			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), StructuredField(RichParagraph(), PlainParagraph()),
				Field("After"));

			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.0"));

			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("After.vern"),
				"later paragraphs are not Tab stops, so Tab advances to the next row");

			ShiftTab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.0"));
			ShiftTab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Before.vern"));
		}

		[AvaloniaTest]
		public void UpAndDownInStructuredText_MoveBetweenParagraphs()
		{
			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), StructuredField(RichParagraph(), PlainParagraph()),
				Field("After"));

			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();
			Tab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.0"));

			Press(window, PhysicalKey.ArrowDown);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.1"));

			Press(window, PhysicalKey.ArrowUp);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.0"),
				"Up re-enters a per-run-font paragraph through its swap, landing in the editor");
		}

		[AvaloniaTest]
		public void TabIntoAFieldWithText_PlacesTheCaretAtTheStartWithoutSelecting()
		{
			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), MultiWsField(), Field("After"));

			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);
			var box = Find<TextBox>(view, "Multi.vern");
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.vern"));
			Assert.That(box.Text, Is.EqualTo("one"));
			Assert.That(box.SelectionStart, Is.EqualTo(0));
			Assert.That(box.SelectionEnd, Is.EqualTo(0),
				"Tab entry leaves a caret at the start, not the whole text selected");
			Assert.That(box.CaretIndex, Is.EqualTo(0));
		}

		[AvaloniaTest]
		public void TabIntoStructuredTextParagraphs_PlacesTheCaretAtTheStart()
		{
			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), StructuredField(PlainParagraph(), RichParagraph()),
				Field("After"));

			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);
			var plain = Find<TextBox>(view, "Multi.Para.0");
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.0"));
			Assert.That(plain.SelectionEnd - plain.SelectionStart, Is.EqualTo(0),
				"a plain paragraph is entered with a caret, not a selection");
			Assert.That(plain.CaretIndex, Is.EqualTo(0));
		}

		[AvaloniaTest]
		public void TabIntoARichStructuredTextParagraph_PlacesTheCaretAtTheStart()
		{
			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), StructuredField(RichParagraph()), Field("After"));

			Find<TextBox>(view, "Before.vern").Focus();
			Dispatcher.UIThread.RunJobs();

			Tab(window);
			var rich = Find<TextBox>(view, "Multi.Para.0");
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Multi.Para.0"));
			Assert.That(rich.SelectionEnd - rich.SelectionStart, Is.EqualTo(0));
			Assert.That(rich.CaretIndex, Is.EqualTo(0),
				"entering through the per-run-font display also lands at the start");
		}

		[AvaloniaTest]
		public void ShiftTabFromALaterParagraph_LeavesTheField()
		{
			var (window, view) = ShowWithEditContext(480, 300, new FakeDetailEditContext(),
				Field("Before"), StructuredField(RichParagraph(), PlainParagraph()),
				Field("After"));

			Find<TextBox>(view, "Multi.Para.1").Focus();
			Dispatcher.UIThread.RunJobs();

			ShiftTab(window);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Before.vern"),
				"Shift+Tab goes to the previous row, not back to the first paragraph");
		}
	}
}
