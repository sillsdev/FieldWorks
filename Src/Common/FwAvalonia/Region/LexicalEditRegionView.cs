// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia.Poc;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// A data-driven Avalonia view that renders a <see cref="LexicalEditRegionModel"/> (task 4.8). Unlike
	/// <c>PocLexEntrySlice</c>, which hard-codes three fields over a detached DTO, this view builds one
	/// row per region field from the typed view definition, so the same renderer scales as more fields
	/// are added to the definition. Each field's renderer is chosen from its <see cref="RegionFieldKind"/>.
	/// Stable, nonlocalized automation ids come from the field (falling back to the stable node id).
	/// Editing write-back is intentionally deferred to the LCModel-backed edit session (tasks 6.x): this
	/// view binds and displays values; commit/cancel through <c>IEditSession</c> is the next step.
	/// </summary>
	public sealed class LexicalEditRegionView : UserControl
	{
		public LexicalEditRegionView(LexicalEditRegionModel model)
		{
			Model = model ?? throw new System.ArgumentNullException(nameof(model));

			Name = "LexicalEditRegionView";
			AutomationProperties.SetAutomationId(this, "LexicalEditRegionView");
			AutomationProperties.SetName(this, "Lexical Edit Region");

			var grid = new Grid
			{
				Margin = PocDensity.SliceMargin,
				ColumnDefinitions = new ColumnDefinitions
				{
					new ColumnDefinition(PocDensity.LabelColumnWidth, GridUnitType.Pixel),
					new ColumnDefinition(GridLength.Star)
				}
			};

			for (var i = 0; i < model.Fields.Count; i++)
			{
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
				AddField(grid, i, model.Fields[i]);
			}

			Content = grid;
		}

		/// <summary>The region model this view renders.</summary>
		public LexicalEditRegionModel Model { get; }

		private static void AddField(Grid grid, int row, LexicalEditRegionField field)
		{
			var automationId = string.IsNullOrEmpty(field.AutomationId) ? field.StableId : field.AutomationId;

			var labelBlock = new TextBlock
			{
				Text = field.Label ?? field.Field ?? string.Empty,
				Margin = new Thickness(0, 0, 6, PocDensity.FieldSpacing),
				VerticalAlignment = VerticalAlignment.Top,
				TextAlignment = TextAlignment.Right,
				Foreground = Brushes.Black
			};
			AutomationProperties.SetAutomationId(labelBlock, automationId + ".Label");
			AutomationProperties.SetName(labelBlock, field.Label ?? field.Field ?? string.Empty);
			Grid.SetRow(labelBlock, row);
			Grid.SetColumn(labelBlock, 0);
			grid.Children.Add(labelBlock);

			var editor = BuildEditor(field, automationId);
			editor.Margin = new Thickness(0, 0, 0, PocDensity.FieldSpacing);
			Grid.SetRow(editor, row);
			Grid.SetColumn(editor, 1);
			grid.Children.Add(editor);
		}

		private static Control BuildEditor(LexicalEditRegionField field, string automationId)
		{
			switch (field.Kind)
			{
				case RegionFieldKind.Chooser:
					return BuildChooser(field, automationId);
				case RegionFieldKind.Unsupported:
					return BuildUnsupported(field, automationId);
				default:
					return BuildText(field, automationId);
			}
		}

		private static Control BuildText(LexicalEditRegionField field, string automationId)
		{
			var stack = new StackPanel { Spacing = PocDensity.RowSpacing };
			AutomationProperties.SetAutomationId(stack, automationId);
			AutomationProperties.SetName(stack, field.Label ?? field.Field ?? automationId);

			foreach (var value in field.Values)
			{
				var abbrev = new TextBlock
				{
					Text = value.WsAbbrev,
					Width = PocDensity.WsAbbrevWidth,
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = Brushes.Gray
				};

				var box = new TextBox
				{
					Text = value.Value,
					Padding = PocDensity.EditorPadding,
					MinHeight = 0,
					AcceptsReturn = false
				};
				if (!string.IsNullOrEmpty(value.FontFamily))
					box.FontFamily = new FontFamily(value.FontFamily);
				if (value.FontSize > 0)
					box.FontSize = value.FontSize;
				AutomationProperties.SetAutomationId(box, automationId + "." + value.WsAbbrev);
				AutomationProperties.SetName(box, (field.Label ?? automationId) + " " + value.WsAbbrev);

				var rowPanel = new DockPanel();
				DockPanel.SetDock(abbrev, Dock.Left);
				rowPanel.Children.Add(abbrev);
				rowPanel.Children.Add(box);
				stack.Children.Add(rowPanel);
			}

			return stack;
		}

		private static Control BuildChooser(LexicalEditRegionField field, string automationId)
		{
			var names = field.Options.Select(o => o.Name).ToList();
			var combo = new ComboBox
			{
				ItemsSource = names,
				Padding = PocDensity.EditorPadding,
				MinHeight = 0
			};

			var selected = field.Options.FirstOrDefault(o => o.Key == field.SelectedOptionKey);
			if (selected != null)
				combo.SelectedItem = selected.Name;

			AutomationProperties.SetAutomationId(combo, automationId);
			AutomationProperties.SetName(combo, field.Label ?? field.Field ?? automationId);
			return combo;
		}

		private static Control BuildUnsupported(LexicalEditRegionField field, string automationId)
		{
			var block = new TextBlock
			{
				Text = $"(unsupported editor: {field.EditorClassification})",
				Foreground = Brushes.Gray,
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(block, automationId);
			AutomationProperties.SetName(block, field.Label ?? field.Field ?? automationId);
			return block;
		}
	}
}
