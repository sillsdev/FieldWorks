// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
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
	/// OpenSpec 2.5 — density parity for the integrated (editable-table) browse view: the virtualized
	/// rows must honor the compact FwAvaloniaDensity tokens rather than the bloated Fluent ListBoxItem
	/// defaults, so the table matches the legacy XMLViews row density.
	/// </summary>
	[TestFixture]
	public class LexicalBrowseDensityTests
	{
		private sealed class StubRowSource : IBrowseRowSource
		{
			public int RowCount => 50;
			public IReadOnlyList<string> GetCellValues(int rowIndex) =>
				new[] { $"lexeme {rowIndex}", $"gloss {rowIndex}" };
			public int HvoAt(int rowIndex) => rowIndex + 1;
		}

		private static ViewDefinitionModel TwoColumnDefinition() => new ViewDefinitionModel(
			"LexEntry", "browse", "browse",
			new List<ViewNode>
			{
				new ViewNode("b/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "string",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null),
				new ViewNode("b/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "string",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			},
			new List<ViewDiagnostic>());

		[AvaloniaTest]
		public void BrowseRows_UseCompactDensity_NotTheFluentDefault()
		{
			var view = new LexicalBrowseView(TwoColumnDefinition(), new StubRowSource());
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var item = view.GetVisualDescendants().OfType<ListBoxItem>().FirstOrDefault();
			Assert.That(item, Is.Not.Null, "rows realize");
			Assert.That(item.Padding, Is.EqualTo(FwAvaloniaDensity.BrowseRowPadding),
				"rows use the compact density padding, not the Fluent default");
			Assert.That(item.MinHeight, Is.EqualTo(FwAvaloniaDensity.BrowseRowMinHeight),
				"rows use the compact density row height, not the Fluent min-height floor");
		}

		// The global deterministic CheckBox rule: on a styled surface the rendered checkbox box is the fixed
		// font-proportional size, NOT the Fluent ~20px box on a 32px slot — so it cannot inflate a row.
		[AvaloniaTest]
		public void CheckBox_OnStyledSurface_IsFontProportional_AndDoesNotExceedTheTextRowHeight()
		{
			// A content-less CheckBox (the row/tree select-checkbox shape) carrying ONLY the shared
			// FwCheckBoxStyle (the same style every surface gets), realized and laid out, must render no taller
			// than CheckboxBoxSize (+ a tiny tolerance) — i.e. it does not exceed the compact text-row height
			// (BrowseRowMinHeight = 18). The size is DETERMINISTIC: a fixed function of the token, not a scale.
			var check = new CheckBox();
			var panel = new StackPanel { Children = { check } };
			foreach (var style in FwCheckBoxStyle.Build())
				panel.Styles.Add(style);
			var window = new Window { Content = panel, Width = 200, Height = 100 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			check.Measure(Size.Infinity);
			check.Arrange(new Rect(check.DesiredSize));
			Dispatcher.UIThread.RunJobs();

			const double tolerance = 2.0; // border stroke / layout rounding
			Assert.That(check.Bounds.Height, Is.LessThanOrEqualTo(FwAvaloniaDensity.CheckboxBoxSize + tolerance),
				"a select checkbox renders at the deterministic font-proportional box size, not the Fluent 32px slot");
			Assert.That(check.Bounds.Height, Is.LessThanOrEqualTo(FwAvaloniaDensity.BrowseRowMinHeight),
				"the checkbox is no taller than a compact text row, so it never inflates a list/table/tree row");

			// The box part is DETERMINISTICALLY exactly CheckboxBoxSize regardless of content — the property
			// that makes the size identical on every surface.
			var box = check.GetVisualDescendants().OfType<Border>().FirstOrDefault(b => b.Name == "FwCheckBox_Box");
			Assert.That(box, Is.Not.Null, "the deterministic box part is present");
			Assert.That(box.Bounds.Height, Is.EqualTo(FwAvaloniaDensity.CheckboxBoxSize).Within(0.5),
				"the box itself is pinned to the deterministic CheckboxBoxSize");
			Assert.That(box.Bounds.Width, Is.EqualTo(FwAvaloniaDensity.CheckboxBoxSize).Within(0.5),
				"the box is square at the deterministic CheckboxBoxSize");

			// The checkmark must actually appear when checked (the compact template still renders state) — its
			// glyph opacity goes 0 → 1 on :checked, so the box is not just an empty square.
			var glyph = check.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
				.FirstOrDefault(p => p.Name == "FwCheckBox_CheckGlyph");
			Assert.That(glyph, Is.Not.Null, "the checkmark glyph part is present");
			Assert.That(glyph.Opacity, Is.EqualTo(0d), "the checkmark is hidden while unchecked");
			check.IsChecked = true;
			Dispatcher.UIThread.RunJobs();
			Assert.That(glyph.Opacity, Is.EqualTo(1d), "the checkmark is revealed when checked");
		}

		// The global deterministic RadioButton rule (the checkbox's counterpart): on a styled surface the
		// rendered radio circle is the fixed font-proportional size, NOT the Fluent ~20px ellipse on a tall
		// slot — so, like the checkbox, it cannot inflate a row past the text line.
		[AvaloniaTest]
		public void RadioButton_OnStyledSurface_IsFontProportional_AndDoesNotExceedTheTextRowHeight()
		{
			// A content-less RadioButton carrying ONLY the shared FwRadioButtonStyle (the same style every
			// surface gets), realized and laid out, must render no taller than RadioBoxSize (+ a tiny tolerance)
			// — i.e. it does not exceed the compact text-row height (BrowseRowMinHeight = 18). The size is
			// DETERMINISTIC: a fixed function of the token, not a scale.
			var radio = new RadioButton();
			var panel = new StackPanel { Children = { radio } };
			foreach (var style in FwRadioButtonStyle.Build())
				panel.Styles.Add(style);
			var window = new Window { Content = panel, Width = 200, Height = 100 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			radio.Measure(Size.Infinity);
			radio.Arrange(new Rect(radio.DesiredSize));
			Dispatcher.UIThread.RunJobs();

			const double tolerance = 2.0; // stroke / layout rounding
			Assert.That(radio.Bounds.Height, Is.LessThanOrEqualTo(FwAvaloniaDensity.RadioBoxSize + tolerance),
				"a radio renders at the deterministic font-proportional box size, not the Fluent tall slot");
			Assert.That(radio.Bounds.Height, Is.LessThanOrEqualTo(FwAvaloniaDensity.BrowseRowMinHeight),
				"the radio is no taller than a compact text row, so it never inflates a list/table/row past the text line");

			// The ring part is DETERMINISTICALLY exactly RadioBoxSize regardless of content — the property that
			// makes the size identical on every surface (mirrors the checkbox box assertion).
			var ring = radio.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Ellipse>()
				.FirstOrDefault(e => e.Name == "FwRadio_Box");
			Assert.That(ring, Is.Not.Null, "the deterministic ring part is present");
			Assert.That(ring.Bounds.Height, Is.EqualTo(FwAvaloniaDensity.RadioBoxSize).Within(0.5),
				"the ring itself is pinned to the deterministic RadioBoxSize");
			Assert.That(ring.Bounds.Width, Is.EqualTo(FwAvaloniaDensity.RadioBoxSize).Within(0.5),
				"the ring is square at the deterministic RadioBoxSize");

			// The dot must actually appear when checked (the compact template still renders state) — its opacity
			// goes 0 → 1 on :checked, so the ring is not just an empty circle.
			var dot = radio.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Ellipse>()
				.FirstOrDefault(e => e.Name == "FwRadio_Dot");
			Assert.That(dot, Is.Not.Null, "the dot part is present");
			Assert.That(dot.Opacity, Is.EqualTo(0d), "the dot is hidden while unchecked");
			radio.IsChecked = true;
			Dispatcher.UIThread.RunJobs();
			Assert.That(dot.Opacity, Is.EqualTo(1d), "the dot is revealed when checked");
		}

		// No-row-inflation: a browse row that carries a select checkbox is the SAME height as a row without
		// one — the checkbox adds zero vertical space to the table.
		[AvaloniaTest]
		public void BrowseRow_WithCheckbox_IsNotTallerThan_RowWithoutCheckbox()
		{
			var withCheck = FirstRealizedRowHeight(showCheckboxColumn: true, checkAll: true);
			var withoutCheck = FirstRealizedRowHeight(showCheckboxColumn: false, checkAll: false);

			Assert.That(withCheck, Is.GreaterThan(0), "the checkbox row realizes");
			Assert.That(withoutCheck, Is.GreaterThan(0), "the plain row realizes");
			Assert.That(withCheck, Is.EqualTo(withoutCheck).Within(0.5),
				"a checked row is exactly as tall as a non-checkbox row — the checkbox adds no vertical space");
		}

		private static double FirstRealizedRowHeight(bool showCheckboxColumn, bool checkAll)
		{
			var view = new LexicalBrowseView(TwoColumnDefinition(), new StubRowSource(),
				showCheckboxColumn: showCheckboxColumn);
			if (checkAll)
				view.CheckAll();
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			var item = view.GetVisualDescendants().OfType<ListBoxItem>().FirstOrDefault();
			return item?.Bounds.Height ?? 0;
		}
	}
}
