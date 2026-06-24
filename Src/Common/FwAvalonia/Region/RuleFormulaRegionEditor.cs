// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 1.3/2.1/2.2) — the Avalonia rule-formula grid, the cross-platform
	/// parity of the legacy <c>RegRuleFormulaControl</c>/<c>RegRuleFormulaVc</c> root-site grid. It renders a
	/// <see cref="RuleFormulaModel"/> as a horizontal run of cell groups (LHS → RHS / LeftCtx __ RightCtx),
	/// each group a sequence of phoneme/natural-class/boundary/slot cells, with the role chrome glyphs
	/// (arrow/slash/underscore) drawn as muted separators between groups.
	///
	/// <para>The view stays LCModel-free (design Decision 3 + the engine-isolation audit): it binds to the
	/// DTO the xWorks/Morphology projector produces. When a <see cref="Sink"/> and <see cref="Options"/> are
	/// wired (task 2.1/2.2) the grid is EDITABLE: each cell is a chooser button (click = replace; context
	/// menu = delete / insert-before) and each group gets a trailing "+" insert button. A committed option's
	/// codec key (<see cref="RuleCellSpec.FromOptionKey"/>) becomes the spec the sink applies in one undo
	/// step; the sink re-projects and calls <see cref="SetModel"/> to refresh. With no sink it is read-only.</para>
	/// </summary>
	public sealed class RuleFormulaRegionEditor : Border
	{
		/// <summary>Stable automation id for the whole rule-formula surface.</summary>
		public const string RuleFormulaAutomationId = "RuleFormulaEditor";

		/// <summary>Muted color for the role separator glyphs (legacy Vc draws them gray).</summary>
		private static readonly IBrush SeparatorBrush = new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80));

		/// <summary>Faint placeholder/cell border for empty groups and editable cells.</summary>
		private static readonly IBrush PlaceholderBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0));

		private readonly StackPanel m_row;
		private RuleFormulaModel m_model;

		/// <summary>
		/// avalonia-rule-formula-editor (task 2.1) — the cell-mutation seam. When non-null the grid is
		/// editable: cell gestures route here, the xWorks handler applies them to the rule in one undoable
		/// step and re-projects, calling <see cref="SetModel"/> to refresh. Null = read-only (task 1.3).
		/// </summary>
		public IRuleCellCommandSink Sink { get; set; }

		/// <summary>avalonia-rule-formula-editor (task 2.2) — the insertable-cell options (phonemes / natural
		/// classes / boundaries) the chooser flyouts list; their keys decode to <see cref="RuleCellSpec"/>.
		/// The grid only offers editing affordances when BOTH this and <see cref="Sink"/> are set.</summary>
		public IReadOnlyList<RegionChoiceOption> Options { get; set; }

		/// <summary>True when an edit sink + options are wired (the grid accepts cell gestures).</summary>
		public bool IsEditable => Sink != null && Options != null;

		public RuleFormulaRegionEditor()
		{
			FwSurfaceStyles.Apply(this);
			Background = Brushes.White;
			Padding = new Thickness(6, 3, 6, 3);
			m_row = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				VerticalAlignment = VerticalAlignment.Center
			};
			Child = m_row;
			AutomationProperties.SetAutomationId(this, RuleFormulaAutomationId);
		}

		public RuleFormulaRegionEditor(RuleFormulaModel model) : this()
		{
			SetModel(model);
		}

		/// <summary>The model the grid renders. Setting it rebuilds the cell run.</summary>
		public RuleFormulaModel Model
		{
			get => m_model;
			set => SetModel(value);
		}

		/// <summary>Rebuild the cell run from <paramref name="model"/> (null clears the grid).</summary>
		public void SetModel(RuleFormulaModel model)
		{
			m_model = model;
			m_row.Children.Clear();
			if (model == null)
			{
				AutomationProperties.SetName(this, string.Empty);
				return;
			}

			var editable = IsEditable;
			foreach (var section in model.Sections)
			{
				var glyph = RuleFormulaModel.RoleSeparatorGlyph(section.Role);
				if (glyph.Length > 0)
					m_row.Children.Add(BuildSeparator(glyph));

				for (var i = 0; i < section.Cells.Count; i++)
					m_row.Children.Add(editable
						? BuildEditableCell(section.Role, i, section.Cells[i])
						: BuildCell(section.Cells[i]));

				if (section.Cells.Count == 0 && !editable)
					m_row.Children.Add(BuildPlaceholder());

				if (editable)
					m_row.Children.Add(BuildInsertButton(section.Role, section.Cells.Count));
			}

			// Accessibility text: the whole formula reads as one string (matches the parity oracle).
			AutomationProperties.SetName(this, model.ToFormulaString());
		}

		// ----- read-only cell rendering (task 1.3) -----

		private static Control BuildCell(RuleCell cell)
			=> new TextBlock
			{
				Text = RuleFormulaModel.Token(cell),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(2, 0, 2, 0)
			};

		private static Control BuildSeparator(string glyph)
			=> new TextBlock
			{
				Text = glyph,
				Foreground = SeparatorBrush,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(4, 0, 4, 0)
			};

		// A thin faint box so an empty group still shows the formula's structure (the legacy empty bracket pile).
		private static Control BuildPlaceholder()
			=> new Border
			{
				MinWidth = 12,
				MinHeight = 12,
				Margin = new Thickness(2, 0, 2, 0),
				BorderThickness = new Thickness(1),
				BorderBrush = PlaceholderBrush,
				VerticalAlignment = VerticalAlignment.Center
			};

		// ----- editable cell rendering (task 2.2) -----

		// A cell is a chooser button: click opens the replace picker; the context menu offers delete and
		// insert-before. The bare/bracketed token reads exactly as the read-only render.
		private Control BuildEditableCell(RuleSectionRole role, int index, RuleCell cell)
		{
			var button = new Button
			{
				Content = RuleFormulaModel.Token(cell),
				Padding = new Thickness(4, 1, 4, 1),
				Margin = new Thickness(2, 0, 2, 0),
				MinWidth = 16,
				Background = Brushes.White,
				BorderBrush = PlaceholderBrush,
				BorderThickness = new Thickness(1),
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(button, $"RuleCell-{role}-{index}");
			button.Flyout = BuildChooserFlyout(role, index, replace: true);

			// Context menu: Delete this cell, and Insert-before (opens the chooser at this index). The
			// insert-before item carries the picker as its OWN sub-flyout target via a click that shows it.
			var deleteItem = new MenuItem { Header = FwAvaloniaStrings.RuleCellDelete };
			deleteItem.Click += (s, e) => Sink?.DeleteCell(role, index);
			var insertItem = new MenuItem { Header = FwAvaloniaStrings.RuleCellInsertBefore };
			insertItem.Click += (s, e) => ShowChooser(button, role, index, replace: false);
			button.ContextFlyout = new MenuFlyout { Items = { deleteItem, insertItem } };
			return button;
		}

		// The trailing "+" appends a new cell at the end of a group.
		private Control BuildInsertButton(RuleSectionRole role, int endIndex)
		{
			var add = new Button
			{
				Content = "+",
				Padding = new Thickness(4, 1, 4, 1),
				Margin = new Thickness(2, 0, 2, 0),
				MinWidth = 16,
				Foreground = SeparatorBrush,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(add, $"RuleInsert-{role}");
			add.Flyout = BuildChooserFlyout(role, endIndex, replace: false);
			return add;
		}

		// One option picker, hosted in a flyout, that applies the committed cell to (role, index): replace
		// the cell or insert before it. Decoding the option key to a spec is the shared codec; the sink
		// re-projects + refreshes the grid on success.
		private FlyoutBase BuildChooserFlyout(RuleSectionRole role, int index, bool replace)
		{
			var picker = new FwOptionPicker(Options, null, "RuleCellPicker");
			var flyout = FwOptionPicker.CreateOptionFlyout(picker, PlacementMode.Bottom);

			Action<RegionChoiceOption> committed = option =>
			{
				flyout.Hide();
				var spec = RuleCellSpec.FromOptionKey(option?.Key);
				if (spec == null || Sink == null)
					return;
				if (replace)
					Sink.SetCell(role, index, spec);
				else
					Sink.InsertCell(role, index, spec);
			};
			EventHandler dismissed = (s, e) => flyout.Hide();
			picker.OptionCommitted += committed;
			picker.Dismissed += dismissed;
			return flyout;
		}

		// Show a one-shot chooser anchored at a control (used by the context-menu "Insert before").
		private void ShowChooser(Control anchor, RuleSectionRole role, int index, bool replace)
			=> BuildChooserFlyout(role, index, replace).ShowAt(anchor);
	}
}
