// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// avalonia-interlinear-editor (task 3.1) — the per-bundle editing surface the xWorks/Morphology adapter
	/// supplies for one morpheme, as an LCModel-free DTO: the candidate SENSE options (the lex-gloss line) and
	/// candidate MSA/grammatical-info options (the grammatical-info line), each with a callback the control
	/// invokes (with the chosen option key) when the user picks. These mirror the two independent interlinear
	/// lines the legacy Sandbox renders as their own combos. The control reuses <see cref="FwOptionPicker"/>
	/// to render them; the adapter's callbacks stage the write-back + MSA prune on the region's fenced UOW
	/// (the view never touches LCModel — design Decision 1).
	/// <para>PARITY (deferred): changing a bundle's MORPH (re-segmentation / a different lex entry) is the
	/// legacy morpheme-breaker path and is NOT offered here — the morph cell stays read-only. Only the
	/// sense and grammatical-info lines are editable this wave.</para>
	/// </summary>
	public sealed class InterlinearBundleEditChoices
	{
		public InterlinearBundleEditChoices(
			IReadOnlyList<RegionChoiceOption> senseOptions, Action<string> onSenseChosen,
			IReadOnlyList<RegionChoiceOption> msaOptions, Action<string> onMsaChosen,
			IReadOnlyList<RegionChoiceOption> morphOptions = null, Action<string> onMorphChosen = null)
		{
			SenseOptions = senseOptions ?? Array.Empty<RegionChoiceOption>();
			OnSenseChosen = onSenseChosen;
			MsaOptions = msaOptions ?? Array.Empty<RegionChoiceOption>();
			OnMsaChosen = onMsaChosen;
			MorphOptions = morphOptions ?? Array.Empty<RegionChoiceOption>();
			OnMorphChosen = onMorphChosen;
		}

		/// <summary>The candidate morphs/entries for this morpheme (same surface form, different lex entry;
		/// option key = MoForm GUID, display = entry headword) — the legacy "choose a different entry" combo
		/// on the Lex. Entries line.</summary>
		public IReadOnlyList<RegionChoiceOption> MorphOptions { get; }

		/// <summary>Invoked with the chosen MoForm key when the user picks a different morph/entry; written back.</summary>
		public Action<string> OnMorphChosen { get; }

		/// <summary>True when the Lex. Entries line is editable (a different entry shares this surface form).</summary>
		public bool HasMorphChooser => OnMorphChosen != null && MorphOptions.Count > 0;

		/// <summary>The candidate senses for this morpheme (option key = sense GUID, display = gloss).</summary>
		public IReadOnlyList<RegionChoiceOption> SenseOptions { get; }

		/// <summary>Invoked with the chosen option key when the user picks a sense; the adapter writes it back.</summary>
		public Action<string> OnSenseChosen { get; }

		/// <summary>The candidate MSAs for this morpheme (option key = MSA GUID, display = interlinear abbr).</summary>
		public IReadOnlyList<RegionChoiceOption> MsaOptions { get; }

		/// <summary>Invoked with the chosen option key when the user picks a grammatical-info/MSA; written back.</summary>
		public Action<string> OnMsaChosen { get; }

		/// <summary>True when the gloss/sense line is editable (has options + a write-back callback).</summary>
		public bool HasSenseChooser => OnSenseChosen != null && SenseOptions.Count > 0;

		/// <summary>True when the grammatical-info/MSA line is editable.</summary>
		public bool HasMsaChooser => OnMsaChosen != null && MsaOptions.Count > 0;
	}

	/// <summary>
	/// avalonia-interlinear-editor (task 2.1/3.1) — the Avalonia interlinear control that renders a wordform's
	/// analysis as aligned interlinear: the wordform line over per-morpheme columns of morph form / lex-gloss
	/// / grammatical-info. Shipped READ-ONLY first (W-4) to de-risk the layout/alignment; W-5 (task 3) adds
	/// per-bundle sense editing (an <see cref="FwOptionPicker"/> in the gloss cell) when the adapter supplies
	/// edit choices for a cell.
	/// <para>Alignment is layout, not a native table (design Decision 5): each morpheme is a <see cref="Grid"/>
	/// column whose width auto-sizes to the widest of its stacked cells (morph / gloss / gram), so the lines
	/// stay column-aligned under a shared measured width — a pure managed Avalonia layout pass, Skia-rendered
	/// (the TextBlocks/pickers paint through Skia/HarfBuzz) and headless-testable. The view is LCModel-free:
	/// it binds only to the <see cref="InterlinearAnalysisModel"/> DTO + <see cref="InterlinearBundleEditChoices"/>
	/// (design Decision 1), which the xWorks/Morphology adapter projects and writes back.</para>
	/// </summary>
	public sealed class InterlinearRegionEditor : Border
	{
		// Row indices within the morpheme grid (the four interlinear lines under the wordform, matching the
		// legacy InterlinVc line choices: Morphemes, Lex. Entries, Lex. Gloss, Lex. Gram. Info.).
		private const int MorphRow = 0;
		private const int LexEntryRow = 1;
		private const int GlossRow = 2;
		private const int GrammaticalInfoRow = 3;
		private const int RowCount = 4;

		// The per-column gutter that keeps adjacent morphemes from touching (managed layout, not a table border).
		private static readonly Avalonia.Thickness ColumnGutter = new Avalonia.Thickness(0, 0, 18, 0);
		private static readonly Avalonia.Thickness LineSpacing = new Avalonia.Thickness(0, 1, 0, 1);

		private readonly string _vernacularFont;
		private readonly string _analysisFont;
		private readonly string _automationId;
		// Per-(line,bundle) editing surface; null in read-only mode. Returns null for a cell that is not editable.
		private readonly Func<int, int, InterlinearBundleEditChoices> _editChoices;

		/// <summary>
		/// Builds the interlinear control over <paramref name="model"/>. <paramref name="rightToLeft"/> flips
		/// the flow direction for RTL vernacular scripts (the plugin supplies it from the wordform's vernacular
		/// writing system — the model itself stays LCModel/WS-free). <paramref name="vernacularFont"/> /
		/// <paramref name="analysisFont"/> are optional preferred font families. <paramref name="editChoices"/>,
		/// when supplied, makes the gloss cells editable: for each (lineIndex, bundleIndex) it returns the
		/// sense chooser to render, or null for a read-only cell. Null overall = fully read-only (W-4).
		/// </summary>
		public InterlinearRegionEditor(InterlinearAnalysisModel model, string automationId = "InterlinearEditor",
			bool rightToLeft = false, string vernacularFont = null, string analysisFont = null,
			Func<int, int, InterlinearBundleEditChoices> editChoices = null)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			_vernacularFont = vernacularFont;
			_analysisFont = analysisFont;
			_automationId = automationId ?? "InterlinearEditor";
			_editChoices = editChoices;

			Background = Brushes.Transparent;
			BorderThickness = new Avalonia.Thickness(0);
			Padding = new Avalonia.Thickness(2);
			FlowDirection = rightToLeft
				? Avalonia.Media.FlowDirection.RightToLeft
				: Avalonia.Media.FlowDirection.LeftToRight;
			AutomationProperties.SetAutomationId(this, _automationId);
			AutomationProperties.SetName(this, model.Wordform);

			Child = BuildContent(model);
		}

		/// <summary>True when this control was built with an edit-choices resolver (W-5 editable mode).</summary>
		public bool IsEditable => _editChoices != null;

		private Control BuildContent(InterlinearAnalysisModel model)
		{
			var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 6 };

			if (!model.HasAnalysis)
			{
				// Bare-wordform state: the wordform with no morpheme breakdown (no approved/parsed analysis
				// yet). Render the wordform line alone so the surface is never empty.
				stack.Children.Add(WordformLine(model.Wordform, _vernacularFont));
				return stack;
			}

			for (var lineIndex = 0; lineIndex < model.Lines.Count; lineIndex++)
				stack.Children.Add(BuildLine(model.Lines[lineIndex], lineIndex, model.Wordform));

			return stack;
		}

		private Control BuildLine(InterlinearLine line, int lineIndex, string fallbackWordform)
		{
			var lineStack = new StackPanel { Orientation = Orientation.Vertical };

			var wordformText = string.IsNullOrEmpty(line.Wordform) ? fallbackWordform : line.Wordform;
			lineStack.Children.Add(WordformLine(wordformText, _vernacularFont));

			if (line.Bundles.Count == 0)
				return lineStack;

			var grid = new Grid();
			for (var r = 0; r < RowCount; r++)
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

			// Column 0 is the left-edge per-line label pile (legacy InterlinVc AddLabelPile); the morpheme
			// bundles occupy columns 1..N. With FlowDirection=RTL the label column flips to the right edge.
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			PlaceLabel(grid, MorphRow, FwAvaloniaStrings.InterlinearMorphemesLabel);
			PlaceLabel(grid, LexEntryRow, FwAvaloniaStrings.InterlinearLexEntriesLabel);
			PlaceLabel(grid, GlossRow, FwAvaloniaStrings.InterlinearGlossLabel);
			PlaceLabel(grid, GrammaticalInfoRow, FwAvaloniaStrings.InterlinearGramInfoLabel);

			for (var i = 0; i < line.Bundles.Count; i++)
			{
				var col = i + 1; // column 0 is the label pile
				// Auto column: its width is the widest of the stacked cells (Decision 5 shared width).
				grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
				var bundle = line.Bundles[i];
				var isLast = i == line.Bundles.Count - 1;

				var choices = _editChoices?.Invoke(lineIndex, i);

				// PARITY: the Morphemes line (the surface form) stays read-only — changing it is
				// re-segmentation (the deferred morpheme-breaker path). The Lex. Entries line is editable:
				// re-point this morpheme to a different entry/allomorph that shares the same surface form.
				PlaceCell(grid, MorphRow, col, bundle.Morph, _vernacularFont, FontStyle.Normal,
					FontWeight.Normal, isLast);

				if (choices != null && choices.HasMorphChooser)
					PlaceCellPicker(grid, LexEntryRow, col, bundle.LexEntry, choices.MorphOptions,
						choices.OnMorphChosen, isLast, $"{_automationId}.Entry{i}", FontStyle.Normal);
				else
					PlaceCell(grid, LexEntryRow, col, bundle.LexEntry, _vernacularFont, FontStyle.Normal,
						FontWeight.Normal, isLast);

				if (choices != null && choices.HasSenseChooser)
					PlaceCellPicker(grid, GlossRow, col, bundle.Gloss, choices.SenseOptions, choices.OnSenseChosen,
						isLast, $"{_automationId}.Gloss{i}", FontStyle.Italic);
				else
					PlaceCell(grid, GlossRow, col, bundle.Gloss, _analysisFont, FontStyle.Italic,
						FontWeight.Normal, isLast);

				if (choices != null && choices.HasMsaChooser)
					PlaceCellPicker(grid, GrammaticalInfoRow, col, bundle.GrammaticalInfo, choices.MsaOptions,
						choices.OnMsaChosen, isLast, $"{_automationId}.Gram{i}", FontStyle.Normal);
				else
					PlaceCell(grid, GrammaticalInfoRow, col, bundle.GrammaticalInfo, _analysisFont, FontStyle.Normal,
						FontWeight.Normal, isLast, isGrammaticalInfo: true);
			}

			lineStack.Children.Add(grid);
			return lineStack;
		}

		private static Control WordformLine(string wordform, string vernacularFont)
		{
			var block = new TextBlock
			{
				Text = wordform ?? string.Empty,
				FontWeight = FontWeight.Bold,
				Margin = LineSpacing,
				TextWrapping = TextWrapping.NoWrap
			};
			ApplyFont(block, vernacularFont);
			AutomationProperties.SetAutomationId(block, "Interlinear.Wordform");
			return block;
		}

		// W-5: an editable interlinear line cell — a compact button showing the current value that opens the
		// shared FwOptionPicker in a flyout (the same value+picker affordance the launcher rows use). Picking
		// invokes the adapter callback, which stages the write-back + MSA prune on the region's fenced UOW,
		// and updates the button label. Used for both the gloss (sense) and grammatical-info (MSA) lines.
		private void PlaceCellPicker(Grid grid, int row, int col, string currentValue,
			IReadOnlyList<RegionChoiceOption> options, Action<string> onChosen, bool isLastColumn,
			string automationId, FontStyle fontStyle)
		{
			var button = new Button
			{
				Content = CellLabel(currentValue),
				FontStyle = fontStyle,
				Padding = new Avalonia.Thickness(2, 0, 2, 0),
				MinHeight = 0,
				Background = Brushes.Transparent,
				BorderThickness = new Avalonia.Thickness(0, 0, 0, 1),
				BorderBrush = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = isLastColumn ? LineSpacing : ColumnGutter,
				HorizontalContentAlignment = HorizontalAlignment.Left
			};
			if (!string.IsNullOrWhiteSpace(_analysisFont))
				button.FontFamily = new FontFamily(_analysisFont);
			AutomationProperties.SetAutomationId(button, automationId);

			var picker = new FwOptionPicker(options, searchOptions: null, automationId: automationId + ".Picker");
			var flyout = FwOptionPicker.CreateOptionFlyout(picker, Avalonia.Controls.PlacementMode.BottomEdgeAlignedLeft);
			button.Flyout = flyout;
			picker.OptionCommitted += option =>
			{
				onChosen?.Invoke(option?.Key);
				button.Content = CellLabel(option?.Name);
				flyout.Hide();
			};

			Grid.SetRow(button, row);
			Grid.SetColumn(button, col);
			grid.Children.Add(button);
		}

		private static string CellLabel(string value)
			=> string.IsNullOrEmpty(value) ? "—" : value;

		// The left-edge line label (legacy InterlinVc label pile): a de-emphasized caption naming each
		// interlinear line, separated from the first morpheme column by a gutter.
		private static void PlaceLabel(Grid grid, int row, string text)
		{
			var block = new TextBlock
			{
				Text = text ?? string.Empty,
				FontStyle = FontStyle.Italic,
				// Navy line labels, matching the legacy InterlinVc label pile's blue captions.
				Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x8C)),
				Margin = new Avalonia.Thickness(0, 1, 24, 1),
				TextWrapping = TextWrapping.NoWrap,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			AutomationProperties.SetAutomationId(block, "Interlinear.Label." + row);
			Grid.SetRow(block, row);
			Grid.SetColumn(block, 0);
			grid.Children.Add(block);
		}

		private static void PlaceCell(Grid grid, int row, int col, string text, string font, FontStyle style,
			FontWeight weight, bool isLastColumn, bool isGrammaticalInfo = false)
		{
			var block = new TextBlock
			{
				Text = text ?? string.Empty,
				FontStyle = style,
				FontWeight = weight,
				Margin = isLastColumn ? LineSpacing : ColumnGutter,
				TextWrapping = TextWrapping.NoWrap,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			if (isGrammaticalInfo)
				block.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
			ApplyFont(block, font);
			Grid.SetRow(block, row);
			Grid.SetColumn(block, col);
			grid.Children.Add(block);
		}

		private static void ApplyFont(TextBlock block, string font)
		{
			if (!string.IsNullOrWhiteSpace(font))
				block.FontFamily = new FontFamily(font);
		}
	}
}
