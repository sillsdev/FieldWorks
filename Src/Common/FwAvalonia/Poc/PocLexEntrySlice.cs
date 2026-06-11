// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Automation;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// The Avalonia proof-of-concept Lexical Edit slice: a dense, three-field surface
	/// (multi-writing-system lexeme form, morph-type chooser, multi-writing-system sense gloss)
	/// over a detached <see cref="PocEntryDto"/>. This is the candidate compared against the
	/// WinForms DataTree baseline for semantic + density parity. Built in pure C# (no XAML).
	/// </summary>
	public sealed class PocLexEntrySlice : UserControl
	{
		public PocLexEntrySlice(PocEntryDto entry)
		{
			Name = "PocLexEntrySlice";
			AutomationProperties.SetAutomationId(this, "PocLexEntrySlice");
			AutomationProperties.SetName(this, "Lexical Edit POC Slice");
			Entry = entry;

			LexemeFormEditor = new MultiWsTextEditor(entry.LexemeForm, "LexemeFormEditor");
			MorphTypeChooser = new MorphTypePopupChooser(entry);
			SenseGlossEditor = new MultiWsTextEditor(entry.SenseGloss, "SenseGlossEditor");

			var grid = new Grid
			{
				Margin = PocDensity.SliceMargin,
				ColumnDefinitions = new ColumnDefinitions
				{
					new ColumnDefinition(PocDensity.LabelColumnWidth, GridUnitType.Pixel),
					new ColumnDefinition(GridLength.Star)
				},
				RowDefinitions = new RowDefinitions
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Auto)
				}
			};

			AddField(grid, 0, "Lexeme Form", LexemeFormEditor);
			AddField(grid, 1, "Morph Type", MorphTypeChooser);
			AddField(grid, 2, "Gloss", SenseGlossEditor);

			Content = grid;
		}

		/// <summary>The bound entry.</summary>
		public PocEntryDto Entry { get; }

		/// <summary>The lexeme-form multi-writing-system editor.</summary>
		public MultiWsTextEditor LexemeFormEditor { get; }

		/// <summary>The morph-type chooser.</summary>
		public MorphTypePopupChooser MorphTypeChooser { get; }

		/// <summary>The sense-gloss multi-writing-system editor.</summary>
		public MultiWsTextEditor SenseGlossEditor { get; }

		private static void AddField(Grid grid, int row, string label, Control editor)
		{
			var labelBlock = new TextBlock
			{
				Text = label,
				Margin = new Avalonia.Thickness(0, 0, 6, PocDensity.FieldSpacing),
				VerticalAlignment = VerticalAlignment.Top,
				TextAlignment = TextAlignment.Right,
				Foreground = Brushes.Black
			};
			AutomationProperties.SetAutomationId(labelBlock, editor.Name + ".Label");
			AutomationProperties.SetName(labelBlock, label);
			Grid.SetRow(labelBlock, row);
			Grid.SetColumn(labelBlock, 0);
			grid.Children.Add(labelBlock);

			editor.Margin = new Avalonia.Thickness(0, 0, 0, PocDensity.FieldSpacing);
			Grid.SetRow(editor, row);
			Grid.SetColumn(editor, 1);
			grid.Children.Add(editor);
		}
	}
}
