// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Section 11 (viewing parity): scrolling, collapsible sections like the legacy +/- tree boxes
	/// (including initial collapsed state from the layout), and checkbox rendering for booleans.
	/// </summary>
	[TestFixture]
	public class RegionViewingParityTests
	{
		private static LexicalEditRegionField Header(string id, string label, int indent,
			bool expanded = true) => new LexicalEditRegionField(
			id, label, null, null, RegionFieldKind.Header, EditorClassification.GroupingNone,
			null, null, SurfaceRouting.Inherit, null, null, null,
			isEditable: false, indent: indent, isCollapsible: true, isInitiallyExpanded: expanded);

		private static LexicalEditRegionField Text(string id, string label, int indent)
			=> new LexicalEditRegionField(id, label, label, null, RegionFieldKind.Text,
				EditorClassification.Known, id, null, SurfaceRouting.Inherit,
				new List<RegionWsValue> { new RegionWsValue("en", "value") }, null, null,
				isEditable: true, indent: indent);

		private static LexicalEditRegionView Show(params LexicalEditRegionField[] fields)
		{
			var model = new LexicalEditRegionModel("LexEntry", "Normal",
				fields.ToList(), new List<ViewDiagnostic>());
			var view = new LexicalEditRegionView(model);
			var window = new Window { Content = view, Width = 480, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		// 14.3/14.5 — the 1px rule underlines only the value side (the label panel stays clean,
		// like legacy lines between entries), and long values wrap so the row grows vertically.
		[AvaloniaTest]
		public void Rules_UnderlineOnlyTheValueColumn_AndValuesWrap()
		{
			var view = Show(Text("f1", "Field 1", 0), Text("f2", "Field 2", 0));

			var rule = view.GetVisualDescendants().OfType<Border>()
				.First(b => AutomationProperties.GetAutomationId(b) == "SliceRule.0");
			Assert.That(Grid.GetColumn(rule), Is.EqualTo(2), "no line under the label panel (14.3)");
			Assert.That(Grid.GetColumnSpan(rule), Is.EqualTo(1));

			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(box.TextWrapping, Is.EqualTo(Avalonia.Media.TextWrapping.Wrap),
				"long values wrap; the field expands vertically (14.5)");
		}

		[AvaloniaTest]
		public void Region_ScrollsLikeLegacyAutoScroll()
		{
			var many = Enumerable.Range(0, 60).Select(i => Text($"f{i}", $"Field {i}", 0)).ToArray();
			var view = Show(many);

			var scroller = view.GetVisualDescendants().OfType<ScrollViewer>()
				.FirstOrDefault(s => AutomationProperties.GetAutomationId(s) == "LexicalEditRegionView.Scroll");
			Assert.That(scroller, Is.Not.Null, "the region is wrapped in a scroll viewer");
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

			var gloss1 = view.GetVisualDescendants().OfType<TextBox>()
				.First(t => (AutomationProperties.GetAutomationId(t) ?? "").StartsWith("g1"));
			Assert.That(gloss1.IsEffectivelyVisible, Is.True);

			var sense1 = view.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == "h1");
			sense1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(gloss1.IsEffectivelyVisible, Is.False, "collapsing Sense 1 hides its nested rows");
			var gloss2 = view.GetVisualDescendants().OfType<TextBox>()
				.First(t => (AutomationProperties.GetAutomationId(t) ?? "").StartsWith("g2"));
			Assert.That(gloss2.IsEffectivelyVisible, Is.True, "the sibling sense is unaffected");

			sense1.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(gloss1.IsEffectivelyVisible, Is.True, "expanding restores the rows");
		}

		[AvaloniaTest]
		public void InitiallyCollapsedSection_StartsHidden_PerLayoutExpansion()
		{
			var view = Show(
				Header("h1", "Publication Settings", 0, expanded: false),
				Text("p1", "Hidden child", 1));

			var child = view.GetVisualDescendants().OfType<TextBox>()
				.First(t => (AutomationProperties.GetAutomationId(t) ?? "").StartsWith("p1"));
			Assert.That(child.IsEffectivelyVisible, Is.False, "expansion='collapsed' sections start collapsed");
		}

		[AvaloniaTest]
		public void ExpansionState_PersistsThroughTheSuppliedStore_AndAppliesOnRebuild()
		{
			// 11.8: toggles record into the store; a new view (re-show/record switch) applies them.
			var store = new Dictionary<string, bool>();
			var model = new LexicalEditRegionModel("LexEntry", "Normal",
				new List<LexicalEditRegionField> { Header("h1", "Senses", 0), Text("g1", "Gloss", 1) },
				new List<ViewDiagnostic>());

			var first = new LexicalEditRegionView(model, null, null,
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

			var second = new LexicalEditRegionView(model, null, null,
				id => store.TryGetValue(id, out var e) ? e : (bool?)null,
				(id, e) => store[id] = e);
			var w2 = new Window { Content = second, Width = 480, Height = 200 };
			w2.Show();
			Dispatcher.UIThread.RunJobs();
			var child = second.GetVisualDescendants().OfType<TextBox>()
				.First(t => (AutomationProperties.GetAutomationId(t) ?? "").StartsWith("g1"));
			Assert.That(child.IsEffectivelyVisible, Is.False,
				"the persisted collapse state applies to the rebuilt view");
		}

		[AvaloniaTest]
		public void LabelTooltips_Splitter_BoldEmphasis_AndCopyMenu_RenderLikeLegacy()
		{
			var bold = new LexicalEditRegionField("lf", "Lexeme Form", "Form", null, RegionFieldKind.Text,
				EditorClassification.Known, "LexemeRow", null, SurfaceRouting.Inherit,
				new List<RegionWsValue> { new RegionWsValue("seh", "casa", null, 14.4, false, "seh", bold: true) },
				null, null);
			var view = Show(bold);

			var label = view.GetVisualDescendants().OfType<TextBlock>()
				.First(t => AutomationProperties.GetAutomationId(t) == "LexemeRow.Label");
			Assert.That(ToolTip.GetTip(label), Is.EqualTo("Lexeme Form"), "11.17: label tooltips");

			Assert.That(view.GetVisualDescendants().OfType<GridSplitter>()
				.Any(g => AutomationProperties.GetAutomationId(g) == "LexicalEditRegionView.Splitter"),
				Is.True, "11.15: label/value splitter");

			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(box.FontWeight, Is.EqualTo(Avalonia.Media.FontWeight.Bold),
				"11.15: the lexeme form's legacy bold emphasis applies");
			Assert.That(box.FontSize, Is.EqualTo(14.4).Within(0.01), "11.15: the 120% fontsize applies");
			Assert.That(box.ContextFlyout, Is.Not.Null, "11.17: rows carry the Copy context menu");
		}

		[AvaloniaTest]
		public void ImageAndCommandFields_RenderTheirControls()
		{
			// A real PNG produced by the Skia-backed renderer itself.
			var png = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fw-parity-test.png");
			using (var rtb = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(8, 8)))
				rtb.Save(png);
			Assert.That(System.IO.File.Exists(png) && new System.IO.FileInfo(png).Length > 0, Is.True,
				"fixture png was written");
			using (var probe = new Avalonia.Media.Imaging.Bitmap(png))
				Assert.That(probe.PixelSize.Width, Is.EqualTo(8), "fixture png is decodable");

			var image = new LexicalEditRegionField("pic", "A picture", "Pictures", null, RegionFieldKind.Image,
				EditorClassification.Known, "PictureRow", null, SurfaceRouting.Inherit,
				new List<RegionWsValue> { new RegionWsValue("", png) }, null, null, isEditable: false);
			var command = new LexicalEditRegionField("cmd", "Insert Sound", "Cmd", null, RegionFieldKind.Command,
				EditorClassification.Known, "CommandRow", null, SurfaceRouting.Inherit, null, null, null,
				isEditable: false);
			var view = Show(image, command);

			var pictureControl = view.GetVisualDescendants()
				.OfType<Control>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "PictureRow");
			Assert.That(pictureControl, Is.Not.Null, "the picture row rendered something");
			Assert.That(pictureControl, Is.InstanceOf<Image>(),
				"11.6: picture fields render the actual image, not the fallback "
				+ (pictureControl is TextBlock tb ? $"(fallback text: '{tb.Text}')" : pictureControl.GetType().Name));
			var button = view.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == "CommandRow");
			Assert.That(button.Content, Is.EqualTo("Insert Sound"), "11.6: command slices render their button");
			Assert.That(button.IsEnabled, Is.False, "execution waits for command routing (shell phase)");
		}

		[AvaloniaTest]
		public void VisualFidelity_FlatEditors_SliceRules_AndLegacyTokens()
		{
			var view = Show(Text("f1", "Lexeme Form", 0), Text("f2", "Citation Form", 0));

			// 12.2: values are flat like RootSite views — no box.
			var box = view.GetVisualDescendants().OfType<TextBox>().First();
			Assert.That(box.BorderThickness, Is.EqualTo(new Avalonia.Thickness(0)));
			Assert.That(box.Background, Is.EqualTo(Avalonia.Media.Brushes.Transparent));

			// 12.1: a 1px LightGray rule under the slice row (and none inside multistring rows —
			// FwMultiWsTextField stacks rows with no rule elements at all).
			var rule = view.GetVisualDescendants().OfType<Border>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "SliceRule.0");
			Assert.That(rule, Is.Not.Null);
			Assert.That(rule.Background, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.Poc.PocDensity.SliceRuleBrush));

			// 12.3/12.4: WS abbreviation + label use the legacy-sampled tokens.
			var abbrev = view.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "en");
			Assert.That(abbrev.Foreground, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.Poc.PocDensity.WsAbbrevBrush));
			Assert.That(abbrev.FontSize, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.Poc.PocDensity.WsAbbrevFontSize));
			var label = view.GetVisualDescendants().OfType<TextBlock>()
				.First(t => AutomationProperties.GetAutomationId(t) == "f1.Label");
			Assert.That(label.Foreground, Is.EqualTo(SIL.FieldWorks.Common.FwAvalonia.Poc.PocDensity.LabelBrush));
		}

		[AvaloniaTest]
		public void BooleanField_RendersAsCheckbox_AndStagesToggles()
		{
			var boolField = new LexicalEditRegionField("b1", "Exclude as headword", "Exclude", null,
				RegionFieldKind.Boolean, EditorClassification.Known, "ExcludeBox", null,
				SurfaceRouting.Inherit, null, null, "false");
			var model = new LexicalEditRegionModel("LexEntry", "Normal",
				new List<LexicalEditRegionField> { boolField }, new List<ViewDiagnostic>());
			var context = new FakeRegionEditContext();
			var view = new LexicalEditRegionView(model, context);
			var window = new Window { Content = view, Width = 400, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var box = view.GetVisualDescendants().OfType<CheckBox>()
				.First(c => AutomationProperties.GetAutomationId(c) == "ExcludeBox");
			Assert.That(box.IsChecked, Is.False);

			box.IsChecked = true;
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.OptionEdits, Has.Count.EqualTo(1));
			Assert.That(context.OptionEdits[0], Is.EqualTo(("Exclude", "true")));
		}
	}
}
