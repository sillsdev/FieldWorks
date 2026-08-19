// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Viewing parity: scrolling, collapsible sections like the legacy +/- tree boxes
	/// (including initial collapsed state from the layout), and checkbox rendering for booleans.
	/// </summary>
	[TestFixture]
	public class DetailViewingParityTests
	{
		private static DetailField Header(string id, string label, int indent,
			bool expanded = true) => new DetailField(
			id, label, null, null, DetailFieldKind.Header, EditorClassification.GroupingNone,
			null, null, HostRouting.Inherit, null, null, null,
			isEditable: false, indent: indent, isCollapsible: true, isInitiallyExpanded: expanded);

		private static DetailField Text(string id, string label, int indent)
			=> new DetailField(id, label, label, null, DetailFieldKind.Text,
				EditorClassification.Known, id, null, HostRouting.Inherit,
				new List<DetailWsValue> { new DetailWsValue("en", "value") }, null, null,
				isEditable: true, indent: indent);

		private static DataTree Show(params DetailField[] fields)
		{
			var model = new DetailModel("LexEntry", "Normal",
				fields.ToList(), new List<ViewDiagnostic>());
			var view = new DataTree(model);
			var window = new Window { Content = view, Width = 480, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		// 14.3/14.5 -- the 1px rule underlines only the value side (the label panel stays clean,
		// like legacy lines between entries), and long values wrap so the row grows vertically.
		[AvaloniaTest]
		public void Rules_UnderlineOnlyTheValueColumn_AndValuesWrap()
		{
			var view = Show(Text("f1", "Field 1", 0), Text("f2", "Field 2", 0));
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			// The 1px rule is a DockPanel-bottom border inside the field's value content;
			// asserting
			// its origin lands at/after the label column is a geometry claim (a rule under the
			// label would start at 0).
			var rule = view.GetVisualDescendants().OfType<Border>()
				.First(b => AutomationProperties.GetAutomationId(b) == "SliceRule.0");
			var origin = rule.TranslatePoint(new Avalonia.Point(0, 0), view) ?? new Avalonia.Point(0, 0);
			Assert.That(origin.X,
				Is.GreaterThanOrEqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.LabelColumnWidth),
				"no line under the label panel (14.3): the rule must start at/after the label column");

			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(box.TextWrapping, Is.EqualTo(Avalonia.Media.TextWrapping.Wrap),
				"long values wrap; the field expands vertically (14.5)");
		}

		// Regression: a long label must wrap inside the label column instead of measuring to its
		// full unwrapped width and painting over the value column (the reported overlap bug).
		[AvaloniaTest]
		public void LongFieldLabel_WrapsInsideTheLabelColumn_AndNeverOverlapsTheValue()
		{
			var view = Show(Text("f1", "Grammatical Information Category", 0));
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			var label = view.GetVisualDescendants().OfType<TextBlock>()
				.First(t => AutomationProperties.GetAutomationId(t) == "f1.Label");
			var origin = label.TranslatePoint(new Avalonia.Point(0, 0), view) ?? new Avalonia.Point(0, 0);
			var rightEdge = origin.X + label.Bounds.Width;

			Assert.That(rightEdge,
				Is.LessThanOrEqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.LabelColumnWidth + 2),
				"a long label must wrap inside the label column, never overlap the value column");
		}

		[AvaloniaTest]
		public void Detail_ScrollsLikeLegacyAutoScroll()
		{
			var many = Enumerable.Range(0, 60).Select(i => Text($"f{i}", $"Field {i}", 0)).ToArray();
			var view = Show(many);

			var scroller = view.GetVisualDescendants().OfType<ScrollViewer>()
				.FirstOrDefault(s => AutomationProperties.GetAutomationId(s) == "DataTree.Scroll");
			Assert.That(scroller, Is.Not.Null, "the detail view is wrapped in a scroll viewer");
			Assert.That(scroller.Extent.Height, Is.GreaterThan(scroller.Viewport.Height),
				"60 rows overflow the viewport so the scrollbar engages");
		}

		[AvaloniaTest]
		public void CollapsibleHeader_TogglesItsNestedRows_LikeLegacyTreeBoxes()
		{
			var view = Show(
				Header("h1", "Sense 1", 0),
				Text("g1", "Gloss", 1),
				Text("d1", "Definition", 1),
				Header("h2", "Sense 2", 0),
				Text("g2", "Gloss2", 1));

			// Collapsing rebuilds the Form's Items from the visible-field subsequence, so each
			// check below re-queries the live tree rather than caching a control reference across
			// a toggle.
			bool GlossPresent(string idPrefix) => view.HasDescendant<TextBox>(
				t => (AutomationProperties.GetAutomationId(t) ?? "").StartsWith(idPrefix));

			Assert.That(GlossPresent("g1"), Is.True);

			var sense1 = view.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == "h1");
			sense1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(GlossPresent("g1"), Is.False, "collapsing Sense 1 removes its nested rows");
			Assert.That(GlossPresent("g2"), Is.True, "the sibling sense is unaffected");

			sense1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(GlossPresent("g1"), Is.True, "expanding restores the rows");
		}

		[AvaloniaTest]
		public void NestedCollapse_SurvivesParentCollapseAndReExpand_LikeLegacy()
		{
			// Three indent levels: parent (0) -> child header (1) -> grandchild rows (2), plus a
			// sibling row directly under the parent (indent 1) that is NOT under the collapsed child.
			var view = Show(
				Header("parent", "Sense 1", 0),
				Header("child", "Examples", 1),
				Text("grand1", "Example sentence", 2),
				Text("grand2", "Translation", 2),
				Text("sibling", "Gloss", 1));

			// Absent-not-hidden while collapsed, same as above; every lookup here is a fresh
			// query too,
			// since collapsing/expanding "child" or "parent" also rebuilds every OTHER realized
			// row.
			bool BoxPresent(string idPrefix) => view.HasDescendant<TextBox>(
				t => (AutomationProperties.GetAutomationId(t) ?? "").StartsWith(idPrefix));
			bool ButtonPresent(string id) => view.HasDescendant<Button>(
				b => AutomationProperties.GetAutomationId(b) == id);
			void Click(string id)
			{
				view.GetVisualDescendants().OfType<Button>()
					.First(b => AutomationProperties.GetAutomationId(b) == id)
					.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
				Dispatcher.UIThread.RunJobs();
			}

			// All present to start.
			Assert.That(BoxPresent("grand1"), Is.True);
			Assert.That(BoxPresent("sibling"), Is.True);
			Assert.That(ButtonPresent("child"), Is.True);

			// (1) Collapse the child -> grandchild rows disappear; the sibling under the parent
			// is unaffected.
			Click("child");
			Assert.That(BoxPresent("grand1"), Is.False, "collapsing the child removes grandchildren from the tree");
			Assert.That(BoxPresent("grand2"), Is.False);
			Assert.That(BoxPresent("sibling"), Is.True, "the parent-level sibling stays present");

			// (2) Collapse the parent -> everything under it disappears, including the child
			// header row.
			Click("parent");
			Assert.That(ButtonPresent("child"), Is.False, "collapsing the parent removes the child header too");
			Assert.That(BoxPresent("grand1"), Is.False);
			Assert.That(BoxPresent("sibling"), Is.False);

			// (3) Re-expand the parent -> the child header row and the sibling reappear, but the
			// grandchild rows STAY absent because the child is still collapsed (nested-collapse
			// fidelity).
			Click("parent");
			Assert.That(ButtonPresent("child"), Is.True, "re-expanding the parent restores the child header");
			Assert.That(BoxPresent("sibling"), Is.True, "the parent-level sibling reappears");
			Assert.That(BoxPresent("grand1"), Is.False,
				"the grandchildren stay absent: the child is still collapsed (this fails the old blanket Apply)");
			Assert.That(BoxPresent("grand2"), Is.False);
		}

		[AvaloniaTest]
		public void InitiallyCollapsedSection_StartsHidden_PerLayoutExpansion()
		{
			var view = Show(
				Header("h1", "Publication Settings", 0, expanded: false),
				Text("p1", "Hidden child", 1));

			// A collapsed-at-construction row is never added to the Form's Items, so it is absent
			// from
			// the tree from the first build, not merely hidden within it.
			Assert.That(view.HasDescendant<TextBox>(t => (AutomationProperties.GetAutomationId(t) ?? "").StartsWith("p1")),
				Is.False, "expansion='collapsed' sections start collapsed");
		}

		[AvaloniaTest]
		public void ExpansionState_PersistsThroughTheSuppliedStore_AndAppliesOnRebuild()
		{
			// 11.8: toggles record into the store; a new view (re-show/record switch) applies them.
			var store = new Dictionary<string, bool>();
			var model = new DetailModel("LexEntry", "Normal",
				new List<DetailField> { Header("h1", "Senses", 0), Text("g1", "Gloss", 1) },
				new List<ViewDiagnostic>());

			var first = new DataTree(model, null, null,
				id => store.TryGetValue(id, out var e) ? e : (bool?)null,
				(id, e) => store[id] = e);
			var w1 = new Window { Content = first, Width = 480, Height = 200 };
			w1.Show();
			Dispatcher.UIThread.RunJobs();

			first.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == "h1")
				.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(store, Does.ContainKey("h1"));
			Assert.That(store["h1"], Is.False, "the collapse was recorded");
			w1.Close();

			var second = new DataTree(model, null, null,
				id => store.TryGetValue(id, out var e) ? e : (bool?)null,
				(id, e) => store[id] = e);
			var w2 = new Window { Content = second, Width = 480, Height = 200 };
			w2.Show();
			Dispatcher.UIThread.RunJobs();
			// The rebuilt view applies the persisted collapse before its first paint, so the
			// collapsed
			// row's controls never get built at all -- absent from the tree, not merely hidden in
			// it.
			Assert.That(second.HasDescendant<TextBox>(t => (AutomationProperties.GetAutomationId(t) ?? "").StartsWith("g1")),
				Is.False, "the persisted collapse state applies to the rebuilt view");
		}

		[AvaloniaTest]
		public void LabelTooltips_Splitter_BoldEmphasis_AndCopyMenu_RenderLikeLegacy()
		{
			var bold = new DetailField("lf", "Lexeme Form", "Form", null, DetailFieldKind.Text,
				EditorClassification.Known, "LexemeRow", null, HostRouting.Inherit,
				new List<DetailWsValue> { new DetailWsValue("seh", "casa", null, 14.4, false, "seh", bold: true) },
				null, null);
			var view = Show(bold);

			var label = view.GetVisualDescendants().OfType<TextBlock>()
				.First(t => AutomationProperties.GetAutomationId(t) == "LexemeRow.Label");
			Assert.That(ToolTip.GetTip(label), Is.EqualTo("Lexeme Form"), "11.17: label tooltips");

			Assert.That(view.GetVisualDescendants().OfType<GridSplitter>()
				.Any(g => AutomationProperties.GetAutomationId(g) == "DataTree.Splitter"),
				Is.True, "11.15: label/value splitter");

			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(box.FontWeight, Is.EqualTo(Avalonia.Media.FontWeight.Bold),
				"11.15: the lexeme form's legacy bold emphasis applies");
			Assert.That(box.FontSize, Is.EqualTo(14.4).Within(0.01), "11.15: the 120% fontsize applies");
			Assert.That(box.ContextFlyout, Is.Not.Null, "11.17: rows carry the Copy context menu");
		}

		[AvaloniaTest]
		public void VisualFidelity_FlatEditors_SliceRules_AndLegacyTokens()
		{
			var view = Show(Text("f1", "Lexeme Form", 0), Text("f2", "Citation Form", 0));

			// 12.2: values are flat like RootSite views -- no box.
			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(box.BorderThickness, Is.EqualTo(new Avalonia.Thickness(0)));
			Assert.That(box.Background, Is.EqualTo(Avalonia.Media.Brushes.Transparent));

			// 12.1: a 1px LightGray rule under the slice row (and none inside multistring rows --
			// FwMultiWsTextField stacks rows with no rule elements at all).
			var rule = view.GetVisualDescendants().OfType<Border>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "SliceRule.0");
			Assert.That(rule, Is.Not.Null);
			Assert.That(rule.Background, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.SliceRuleBrush));

			// 12.3/12.4: WS abbreviation + label use the legacy-sampled tokens.
			var abbrev = view.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "en");
			Assert.That(abbrev.Foreground, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.WsAbbrevBrush));
			Assert.That(abbrev.FontSize, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.WsAbbrevFontSize));
			var label = view.GetVisualDescendants().OfType<TextBlock>()
				.First(t => AutomationProperties.GetAutomationId(t) == "f1.Label");
			Assert.That(label.Foreground, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.LabelBrush));
		}

		// The view wraps the grid identically in read-only and editable -- an
		// unwrapped vs StackPanel-wrapped grid rounds Auto row heights 1px
		// differently, shifting every row's height/offset on the edit toggle.
		[AvaloniaTest]
		public void RowHeights_AreIdentical_ReadOnlyAndEditable()
		{
			var fields = new[]
			{
				MultiWsText("d0", "Lexeme Form", ("seh", "casa"), ("pt", "casa")),
				MultiWsText("d1", "Citation Form", ("seh", "casa")),
				MultiWsText("d2", "Gloss", ("en", "house"), ("pt", "casa")),
			};
			var model = new DetailModel("LexEntry", "detail", fields.ToList(),
				new List<ViewDiagnostic>());

			FwMultiWsTextField Editor(DataTree v, string id)
				=> v.GetVisualDescendants().OfType<FwMultiWsTextField>()
					.First(f => AutomationProperties.GetAutomationId(f) == id);

			var roView = new DataTree(model);
			var w1 = new Window { Content = roView, Width = 520, Height = 420 };
			w1.Show();
			Dispatcher.UIThread.RunJobs();
			w1.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			var edView = new DataTree(model, new FakeDetailEditContext());
			var w2 = new Window { Content = edView, Width = 520, Height = 420 };
			w2.Show();
			Dispatcher.UIThread.RunJobs();
			w2.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			foreach (var id in new[] { "d0", "d1", "d2" })
			{
				var ro = Editor(roView, id);
				var ed = Editor(edView, id);
				Assert.That(ro.Bounds.Height, Is.GreaterThan(0), $"{id}: the read-only row must realize");
				Assert.That(ed.Bounds.Height, Is.EqualTo(ro.Bounds.Height),
					$"{id}: an editable field row must be the SAME height as the read-only row");
				Assert.That(ed.Bounds.Y, Is.EqualTo(ro.Bounds.Y),
					$"{id}: an editable field row must sit at the SAME vertical offset as the read-only row");
			}
		}

		// 16.x regression guard: dropped WS-abbrev-width wiring silently falls back to the fixed
		// floor, clipping a long abbreviation like "MbuOriginalOrthography".
		[AvaloniaTest]
		public void LongWsAbbreviation_WidensTheGutterColumn_PastTheFloor_ButNotPastTheCap()
		{
			var fields = new[]
			{
				MultiWsText("d0", "Lexeme Form", ("MbuOriginalOrthography", "casa"), ("en", "house")),
			};
			var model = new DetailModel("LexEntry", "detail", fields.ToList(),
				new List<ViewDiagnostic>());
			var view = new DataTree(model);
			var window = new Window { Content = view, Width = 520, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			var abbrev = view.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "MbuOriginalOrthography");

			Assert.That(abbrev.Bounds.Width,
				Is.GreaterThan(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.WsAbbrevWidth),
				"a long abbreviation must widen the gutter beyond the fixed floor -- an upper-bound-only " +
				"assertion here would still pass if the width wiring silently fell back to the floor");
			Assert.That(abbrev.Bounds.Width,
				Is.LessThanOrEqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.WsAbbrevMaxWidth),
				"the adaptive gutter still clamps to the max-width cap");
		}

		private static DetailField MultiWsText(string id, string label,
			params (string abbrev, string value)[] values)
		{
			var wsValues = new List<DetailWsValue>();
			foreach (var v in values)
				wsValues.Add(new DetailWsValue(v.abbrev, v.value, wsTag: v.abbrev));
			return new DetailField(id, label, label, null, DetailFieldKind.Text,
				EditorClassification.Known, id, null, HostRouting.Product, wsValues, null, null,
				isEditable: true);
		}
	}
}
