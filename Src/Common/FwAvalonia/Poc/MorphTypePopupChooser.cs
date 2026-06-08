// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// A morph-type chooser rendered as a button that opens a popup (Avalonia flyout) with the
	/// available options. Choosing an option updates the bound entry and returns focus to the
	/// button (the popup-focus-return behavior the spike must prove). Built in pure C# (no XAML).
	/// </summary>
	public sealed class MorphTypePopupChooser : UserControl
	{
		private readonly PocEntryDto _entry;
		private readonly Button _button;
		private readonly Flyout _flyout;
		private readonly ListBox _list;

		public MorphTypePopupChooser(PocEntryDto entry)
		{
			Name = "MorphTypeChooser";
			_entry = entry;

			_list = new ListBox
			{
				ItemsSource = entry.MorphTypeOptions,
				SelectedItem = entry.SelectedMorphType
			};
			_list.SelectionChanged += (sender, args) =>
			{
				if (_list.SelectedItem is MorphTypeOption option)
				{
					Select(option);
				}
			};

			_flyout = new Flyout { Content = _list };

			_button = new Button
			{
				Name = "MorphTypeButton",
				Content = DisplayText(entry.SelectedMorphType),
				Padding = PocDensity.EditorPadding,
				MinHeight = 0,
				HorizontalAlignment = HorizontalAlignment.Left,
				Flyout = _flyout
			};

			Content = _button;
		}

		/// <summary>The button that launches the chooser popup.</summary>
		public Button Button => _button;

		/// <summary>The chooser popup flyout.</summary>
		public Flyout Flyout => _flyout;

		/// <summary>Opens the chooser popup.</summary>
		public void Open() => _flyout.ShowAt(_button);

		/// <summary>
		/// Selects an option: updates the bound entry and button label, closes the popup, and
		/// returns focus to the button. Exposed so headless tests can drive selection deterministically.
		/// </summary>
		public void Select(MorphTypeOption option)
		{
			if (option == null)
			{
				return;
			}

			_entry.MorphTypeKey = option.Key;
			_button.Content = DisplayText(option);
			_flyout.Hide();
			_button.Focus();
		}

		private static string DisplayText(MorphTypeOption option)
			=> option != null ? option.Name : "(choose)";
	}
}
