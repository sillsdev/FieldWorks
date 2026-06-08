// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// A dense, multi-writing-system text editor: one row per writing-system alternative, each with
	/// a small abbreviation gutter and a text box that uses that writing system's configured font.
	/// Edits write straight back to the bound <see cref="WsAlternative"/> values. Built in pure C#
	/// (no XAML) so the spike does not depend on the Avalonia XAML compiler.
	/// </summary>
	public sealed class MultiWsTextEditor : UserControl
	{
		private readonly List<TextBox> _boxes = new List<TextBox>();

		public MultiWsTextEditor(IList<WsAlternative> alternatives, string editorName)
		{
			Name = editorName;
			Alternatives = alternatives;

			var stack = new StackPanel { Spacing = PocDensity.RowSpacing };

			foreach (var alt in alternatives)
			{
				var captured = alt;

				var abbrev = new TextBlock
				{
					Text = alt.WsAbbrev,
					Width = PocDensity.WsAbbrevWidth,
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = Brushes.Gray
				};

				var box = new TextBox
				{
					Text = alt.Value,
					FontFamily = new FontFamily(alt.FontFamily),
					FontSize = alt.FontSize,
					Padding = PocDensity.EditorPadding,
					MinHeight = 0,
					AcceptsReturn = false
				};
				box.TextChanged += (sender, args) => captured.Value = box.Text;
				_boxes.Add(box);

				var row = new DockPanel();
				DockPanel.SetDock(abbrev, Dock.Left);
				row.Children.Add(abbrev);
				row.Children.Add(box);

				stack.Children.Add(row);
			}

			Content = stack;
		}

		/// <summary>The writing-system alternatives this editor is bound to.</summary>
		public IList<WsAlternative> Alternatives { get; }

		/// <summary>The text boxes, one per writing-system alternative, in order.</summary>
		public IReadOnlyList<TextBox> Boxes => _boxes;
	}
}
