// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaTests.VisualChecks;
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

		// A multistring row, so the per-writing-system abbreviation gutter renders: the view
		// draws it only for a MultiStringSlice row, as legacy does.
		private static DetailField Text(string id, string label, int indent)
		{
			var field = new DetailField(id, label, label, null, DetailFieldKind.Text,
				EditorClassification.Known, id, null, HostRouting.Inherit,
				new List<DetailWsValue> { new DetailWsValue("en", "value") }, null, null,
				isEditable: true, indent: indent);
			field.IsMultiStringRow = true;
			return field;
		}

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

		// The 1px rule underlines only the value side, leaving the label panel clean, and
		// long values wrap so the row grows vertically.
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
			// Toggles record into the store; a new view (re-show/record switch) applies them.
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

		// The section header with the given automation id.
		private static DetailSectionHeader SectionHeader(Control view, string id)
			=> view.GetVisualDescendants().OfType<DetailSectionHeader>()
				.First(h => AutomationProperties.GetAutomationId(h) == id);

		private static bool TextBoxPresent(Control view, string idPrefix) => view.HasDescendant<TextBox>(
			t => (AutomationProperties.GetAutomationId(t) ?? "").StartsWith(idPrefix));

		private static void ClickHeader(Button button)
		{
			button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
		}

		private static DataTree ShowWithStore(DetailModel model, Dictionary<string, bool> store)
		{
			var view = new DataTree(model, null, null,
				key => store.TryGetValue(key, out var e) ? e : (bool?)null,
				(key, e) => store[key] = e);
			new Window { Content = view, Width = 480, Height = 300 }.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		[AvaloniaTest]
		public void CollapsibleHeader_DrawsAPlusMinusBox_ThatFlipsWithTheToggle()
		{
			var view = Show(Header("h1", "Sense 1", 0), Text("g1", "Gloss", 1),
				Header("h2", "Variants", 0));
			var window = (Window)TopLevel.GetTopLevel(view);
			DialogSnapshot.Capture(window, "Detail-SectionExpander-01-expanded");

			var header = SectionHeader(view, "h1");
			Assert.That(header.Box.Opacity, Is.EqualTo(1), "a header with rows under it shows its box");
			Assert.That(header.Box.IsExpanded, Is.True, "an expanded section shows a minus");
			Assert.That(header.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text),
				Is.EqualTo(new[] { "Sense 1" }), "the label is the header's only text, with no glyph");

			ClickHeader(header);

			var collapsed = SectionHeader(view, "h1");
			Assert.That(collapsed.IsExpanded, Is.False);
			Assert.That(collapsed.Box.IsExpanded, Is.False, "a collapsed section shows a plus");
			Assert.That(TextBoxPresent(view, "g1"), Is.False);
			DialogSnapshot.Capture(window, "Detail-SectionExpander-02-collapsed");

			ClickHeader(collapsed);
			Assert.That(SectionHeader(view, "h1").Box.IsExpanded, Is.True);
			Assert.That(TextBoxPresent(view, "g1"), Is.True);
		}

		// LT-22689: header and "Field Options" text take the slice label size, and header text
		// takes the section header colour.
		[AvaloniaTest]
		public void SectionHeaderAndFieldOptionsText_TakeTheirOwnSizeAndColour()
		{
			var collapsible = new DetailField("hdr", "Allomorphs", null, null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null,
				isEditable: false, indent: 0, isCollapsible: true, hotlinksId: "mnu-hotlinks");
			var plain = new DetailField("plain", "Grammatical Info.", null, null, DetailFieldKind.Header,
				EditorClassification.GroupingNone, null, null, HostRouting.Inherit, null, null, null,
				isEditable: false, indent: 0, isCollapsible: false);
			var view = new DataTree(new DetailModel("LexEntry", "Normal",
				new List<DetailField> { collapsible, Text("f1", "Form", 1), plain }, new List<ViewDiagnostic>()),
				menuRequested: request => { });
			var window = new Window { Content = view, Width = 480, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, "Detail-SectionExpander-03-header-and-link-text");

			TextBlock TextOf(string text) => view.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == text);
			var headerSize = SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.SectionHeaderFontSize;
			var linkSize = SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.HotlinkFontSize;

			Assert.That(TextOf("Allomorphs").FontSize, Is.EqualTo(headerSize), "a collapsible header");
			Assert.That(TextOf("Grammatical Info.").FontSize, Is.EqualTo(headerSize),
				"a header that cannot collapse matches one that can");
			var headerInk = SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.SectionHeaderBrush;
			Assert.That(TextOf("Allomorphs").Foreground, Is.SameAs(headerInk),
				"a collapsible header is not the button theme's blue");
			Assert.That(TextOf("Grammatical Info.").Foreground, Is.SameAs(headerInk));
			var link = TextOf(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaStrings.FieldOptionsMenu);
			Assert.That(link.FontSize, Is.EqualTo(linkSize));
			Assert.That(link.FontWeight, Is.EqualTo(Avalonia.Media.FontWeight.Normal),
				"the link is not the button theme's semibold");
		}

		// LT-22689: a header with a collapsed line (a sense, etymology, extended note or
		// subrecord) shows it while collapsed and its own label while expanded.
		[AvaloniaTest]
		public void CollapsibleHeader_ShowsItsCollapsedLine_OnlyWhileCollapsed()
		{
			var sense = Header("sense", "Sense 1", 0);
			sense.CollapsedLabel = "Sense 1 - house - n";
			var note = Header("note", "Extended Note", 0, expanded: false);
			note.CollapsedLabel = "Extended Note - use - formal";
			var view = Show(sense, Text("g1", "Gloss", 1), note, Text("d1", "Discussion", 1));
			string Shown(string id) => SectionHeader(view, id).GetVisualDescendants().OfType<TextBlock>().Single().Text;

			Assert.That(Shown("sense"), Is.EqualTo("Sense 1"));
			Assert.That(Shown("note"), Is.EqualTo("Extended Note - use - formal"),
				"a header that starts collapsed shows its collapsed line");

			ClickHeader(SectionHeader(view, "sense"));
			Assert.That(Shown("sense"), Is.EqualTo("Sense 1 - house - n"));
			Assert.That(AutomationProperties.GetName(SectionHeader(view, "sense")), Is.EqualTo("Sense 1 - house - n"),
				"assistive technology reads the line that is shown");

			ClickHeader(SectionHeader(view, "sense"));
			Assert.That(Shown("sense"), Is.EqualTo("Sense 1"));
		}

		[AvaloniaTest]
		public void HeaderWithNothingUnderIt_DrawsNoBox_AndIgnoresClicks()
		{
			var store = new Dictionary<string, bool>();
			var view = ShowWithStore(new DetailModel("LexEntry", "Normal",
				new List<DetailField> { Header("empty", "Variants", 0), Header("h2", "Allomorphs", 0), Text("a1", "Form", 1) },
				new List<ViewDiagnostic>()), store);

			var empty = SectionHeader(view, "empty");
			Assert.That(empty.CanToggle, Is.False);
			Assert.That(empty.Box.Opacity, Is.EqualTo(0), "nothing to collapse, so no box is drawn");
			Assert.That(empty.Box.Bounds.Width, Is.GreaterThan(0),
				"the blank box keeps its width, so the label lines up with its siblings'");

			ClickHeader(empty);
			Assert.That(store, Is.Empty, "a header with nothing under it records no state");
			Assert.That(SectionHeader(view, "h2").CanToggle, Is.True);
		}

		[AvaloniaTest]
		public void Toggling_KeepsFocusOnTheHeader()
		{
			var view = Show(Header("h1", "Sense 1", 0), Text("g1", "Gloss", 1));
			var header = SectionHeader(view, "h1");
			header.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(header.IsFocused, Is.True, "precondition: the header has focus");

			ClickHeader(header);

			Assert.That(SectionHeader(view, "h1"), Is.SameAs(header),
				"a toggle adds or removes only the rows under the header, never the header itself");
			Assert.That(header.IsFocused, Is.True);
		}

		// LT-22689: each click of a double-click toggles, as each click of the +/- box does in
		// the WinForms DataTree, so a double-click expands and collapses again.
		[AvaloniaTest]
		public void DoubleClickingAHeader_TogglesItTwice()
		{
			var view = Show(Header("h1", "Sense 1", 0), Text("g1", "Gloss", 1));
			var window = (Window)TopLevel.GetTopLevel(view);
			var point = SectionHeader(view, "h1").TranslatePoint(new Point(4, 4), window);
			Assert.That(point, Is.Not.Null, "precondition: the header is laid out");

			window.MouseDown(point.Value, Avalonia.Input.MouseButton.Left);
			window.MouseUp(point.Value, Avalonia.Input.MouseButton.Left);
			Assert.That(SectionHeader(view, "h1").IsExpanded, Is.False, "the first click collapses");
			window.MouseDown(point.Value, Avalonia.Input.MouseButton.Left);
			window.MouseUp(point.Value, Avalonia.Input.MouseButton.Left);
			Dispatcher.UIThread.RunJobs();

			Assert.That(SectionHeader(view, "h1").IsExpanded, Is.True, "the second click expands again");
			Assert.That(TextBoxPresent(view, "g1"), Is.True);
		}

		// LT-22689: a toggle leaves the clicked header where it was in the viewport.
		[AvaloniaTest]
		public void Toggling_KeepsTheHeaderWhereItWasInTheViewport()
		{
			var fields = new List<DetailField>();
			for (var i = 0; i < 30; i++)
				fields.Add(Text($"above{i}", $"Above {i}", 0));
			fields.Add(Header("h1", "Sense 9", 0));
			for (var i = 0; i < 10; i++)
				fields.Add(Text($"gloss{i}", $"Gloss {i}", 1));
			for (var i = 0; i < 30; i++)
				fields.Add(Text($"below{i}", $"Below {i}", 0));
			var view = Show(fields.ToArray());
			var scroller = view.GetVisualDescendants().OfType<ScrollViewer>()
				.First(s => AutomationProperties.GetAutomationId(s) == "DataTree.Scroll");
			double TopOf(Control control) => control.TranslatePoint(new Point(0, 0), scroller)?.Y ?? double.NaN;
			void Settle()
			{
				view.UpdateLayout();
				Dispatcher.UIThread.RunJobs();
			}

			scroller.Offset = new Vector(0, scroller.Offset.Y + TopOf(SectionHeader(view, "h1")) - 100);
			Settle();
			var header = SectionHeader(view, "h1");
			var top = TopOf(header);
			Assert.That(scroller.Offset.Y, Is.GreaterThan(0), "precondition: the view is scrolled");
			Assert.That(top, Is.EqualTo(100).Within(1), "precondition: the header is mid-viewport");

			// A mouse press focuses the header before its Click.
			header.Focus();
			ClickHeader(header);
			Settle();
			Assert.That(TopOf(SectionHeader(view, "h1")), Is.EqualTo(top).Within(1),
				"collapsing leaves the header where it was");

			ClickHeader(SectionHeader(view, "h1"));
			Settle();
			Assert.That(TopOf(SectionHeader(view, "h1")), Is.EqualTo(top).Within(1),
				"expanding leaves the header where it was");
		}

		[AvaloniaTest]
		public void AutomationPeer_ReportsAndDrivesTheExpandCollapseState()
		{
			var view = Show(Header("h1", "Sense 1", 0), Text("g1", "Gloss", 1), Header("leaf", "Variants", 0));
			IExpandCollapseProvider Provider(string id) => ControlAutomationPeer
				.CreatePeerForElement(SectionHeader(view, id)).GetProvider<IExpandCollapseProvider>();

			Assert.That(Provider("h1"), Is.Not.Null, "the header exposes the ExpandCollapse pattern");
			Assert.That(Provider("h1").ExpandCollapseState, Is.EqualTo(ExpandCollapseState.Expanded));
			Assert.That(Provider("leaf").ExpandCollapseState, Is.EqualTo(ExpandCollapseState.LeafNode));

			Provider("h1").Collapse();
			Dispatcher.UIThread.RunJobs();
			Assert.That(Provider("h1").ExpandCollapseState, Is.EqualTo(ExpandCollapseState.Collapsed));
			Assert.That(TextBoxPresent(view, "g1"), Is.False);

			Provider("h1").Collapse();
			Dispatcher.UIThread.RunJobs();
			Assert.That(Provider("h1").ExpandCollapseState, Is.EqualTo(ExpandCollapseState.Collapsed),
				"collapsing a collapsed section leaves it collapsed");

			Provider("h1").Expand();
			Dispatcher.UIThread.RunJobs();
			Assert.That(TextBoxPresent(view, "g1"), Is.True);
		}

		[AvaloniaTest]
		public void ExpansionState_IsRecordedUnderTheExpansionKey()
		{
			var header = Header("h1@101", "Senses", 0);
			header.ExpansionKey = "h1@guid";
			var model = new DetailModel("LexEntry", "Normal",
				new List<DetailField> { header, Text("g1", "Gloss", 1) }, new List<ViewDiagnostic>());
			var store = new Dictionary<string, bool>();

			ClickHeader(SectionHeader(ShowWithStore(model, store), "h1@101"));

			Assert.That(store, Is.EqualTo(new Dictionary<string, bool> { ["h1@guid"] = false }));
		}

		private static DetailField ObjectHeader(string id, int hvo) => new DetailField(
			id, id, null, null, DetailFieldKind.Header, EditorClassification.GroupingNone,
			id, null, HostRouting.Inherit, null, null, null,
			isEditable: false, indent: 0, isCollapsible: true, objectHvo: hvo);

		private static DetailField ObjectRow(string id, int hvo) => new DetailField(
			id, id, id, null, DetailFieldKind.Text, EditorClassification.Known, id, null,
			HostRouting.Inherit, new List<DetailWsValue> { new DetailWsValue("en", "value") },
			null, null, indent: 1, objectHvo: hvo);

		// LT-22689: collapsing removes the rows under a header, so a removed reference vector's
		// current item must not stay the target of the item-relative commands.
		[AvaloniaTest]
		public void CollapsingTheSectionOfTheCurrentVectorItem_LeavesNoCurrentItem()
		{
			var vectorField = new DetailField("LexEntry/x/#7", "Subentries", "Subentries", null,
				DetailFieldKind.ReferenceVector, EditorClassification.Known, "Subentries", null,
				HostRouting.Inherit, null, null, null, isEditable: false, indent: 1,
				items: new List<DetailChoiceOption> { new DetailChoiceOption("a", "A"), new DetailChoiceOption("b", "B") });
			var view = Show(Header("h1", "Sense 1", 0), vectorField);
			var vector = view.GetVisualDescendants().OfType<FwReferenceVectorField>().Single();
			Assert.That(vector.SelectItem("b"), Is.True);
			Assert.That(DetailFocusMemory.Capture(view).VectorItemKey, Is.EqualTo("b"),
				"precondition: the vector row has a current item");

			ClickHeader(SectionHeader(view, "h1"));

			Assert.That(DetailFocusMemory.Capture(view).VectorItemKey, Is.Null);
		}

		// An entry with one sense, then the same entry after a second sense (hvo 11) was added.
		private static DetailModel SensesBefore() => new DetailModel("LexEntry", "Normal", new List<DetailField>
		{
			ObjectHeader("senses", 1), ObjectRow("g10", 10),
			ObjectHeader("allomorphs", 1), ObjectRow("f20", 20)
		}, new List<ViewDiagnostic>());

		private static DetailModel SensesAfterInsert() => new DetailModel("LexEntry", "Normal", new List<DetailField>
		{
			ObjectHeader("senses", 1), ObjectRow("g10", 10), ObjectRow("g11", 11),
			ObjectHeader("allomorphs", 1), ObjectRow("f20", 20)
		}, new List<ViewDiagnostic>());

		[AvaloniaTest]
		public void RevealObjectsAddedSince_ExpandsTheCollapsedSectionHidingANewObject()
		{
			var store = new Dictionary<string, bool> { ["senses"] = false, ["allomorphs"] = false };
			var view = ShowWithStore(SensesAfterInsert(), store);
			Assert.That(TextBoxPresent(view, "g11"), Is.False, "precondition: the new sense is hidden");

			view.RevealObjectsAddedSince(new DataTree(SensesBefore()));
			Dispatcher.UIThread.RunJobs();

			Assert.That(TextBoxPresent(view, "g11"), Is.True, "the inserted sense is shown");
			Assert.That(store["senses"], Is.True, "the expansion is recorded like a click");
			Assert.That(TextBoxPresent(view, "f20"), Is.False, "a section that gained nothing stays collapsed");
			Assert.That(store["allomorphs"], Is.False);
		}

		// A shown view of SensesBefore() that has saved an edit of its own, and then collapsed
		// the senses section when collapseSensesAfterward is true.
		private static DataTree PreviousViewThatSavedAnEdit(Dictionary<string, bool> store,
			bool collapseSensesAfterward)
		{
			var context = new FakeDetailEditContext();
			var previous = new DataTree(SensesBefore(), context,
				getExpansionState: key => store.TryGetValue(key, out var e) ? e : (bool?)null,
				expansionChanged: (key, e) => store[key] = e);
			new Window { Content = previous, Width = 480, Height = 300 }.Show();
			Dispatcher.UIThread.RunJobs();
			previous.RaiseEvent(new Avalonia.Input.KeyEventArgs
			{
				RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
				Key = Avalonia.Input.Key.Enter
			});
			Assert.That(context.CommitCount, Is.EqualTo(1), "precondition: the previous view saved an edit");
			if (collapseSensesAfterward)
				ClickHeader(SectionHeader(previous, "senses"));
			return previous;
		}

		// LT-22689: typing into a section and then clicking its +/- commits the text on the
		// press and collapses on the release. The re-show that commit causes leaves that section
		// collapsed.
		[AvaloniaTest]
		public void RevealObjectsAddedSince_LeavesASectionCollapsedAfterTheOwnEditCollapsed()
		{
			var store = new Dictionary<string, bool>();
			var previous = PreviousViewThatSavedAnEdit(store, collapseSensesAfterward: true);
			Assert.That(store["senses"], Is.False, "precondition: senses was collapsed after the edit");
			var view = ShowWithStore(SensesAfterInsert(), store);

			view.RevealObjectsAddedSince(previous);
			Dispatcher.UIThread.RunJobs();

			Assert.That(TextBoxPresent(view, "g11"), Is.False, "the section stays collapsed");
			Assert.That(store["senses"], Is.False, "the user's collapse stays recorded");
		}

		// LT-22689: a re-show after the view's own edit can also carry an insert from another
		// window or a menu command. A section collapsed before the edit still opens to show it.
		[AvaloniaTest]
		public void RevealObjectsAddedSince_ExpandsASectionCollapsedBeforeTheOwnEdit()
		{
			var store = new Dictionary<string, bool> { ["senses"] = false };
			var previous = PreviousViewThatSavedAnEdit(store, collapseSensesAfterward: false);
			var view = ShowWithStore(SensesAfterInsert(), store);

			view.RevealObjectsAddedSince(previous);
			Dispatcher.UIThread.RunJobs();

			Assert.That(TextBoxPresent(view, "g11"), Is.True, "the inserted sense is shown");
			Assert.That(store["senses"], Is.True);
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

			// Values are flat, as RootSite views are -- no box.
			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(box.BorderThickness, Is.EqualTo(new Avalonia.Thickness(0)));
			Assert.That(box.Background, Is.EqualTo(Avalonia.Media.Brushes.Transparent));

			// A 1px LightGray rule under the slice row (and none inside multistring rows --
			// FwMultiWsTextField stacks rows with no rule elements at all).
			var rule = view.GetVisualDescendants().OfType<Border>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "SliceRule.0");
			Assert.That(rule, Is.Not.Null);
			Assert.That(rule.Background, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaDensity.SliceRuleBrush));

			// The writing-system abbreviation and label use the sampled tokens.
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
			var field = new DetailField(id, label, label, null, DetailFieldKind.Text,
				EditorClassification.Known, id, null, HostRouting.Product, wsValues, null, null,
				isEditable: true);
			// A multi-alternative row is a multistring row, which is what earns the gutter.
			field.IsMultiStringRow = true;
			return field;
		}
	}
}
