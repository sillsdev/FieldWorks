// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.Poc
{
	/// <summary>
	/// A morph-type chooser rendered as a button that opens a popup (Avalonia flyout) with the
	/// available options. The popup content is the shared <see cref="FwOptionPicker"/> used by the
	/// product region controls, so the POC and product paths exercise the same Avalonia-native
	/// select-from-list pattern. Choosing an option updates the bound entry and returns focus to the
	/// button (the popup-focus-return behavior the spike must prove). Built in pure C# (no XAML).
	/// </summary>
	public sealed class MorphTypePopupChooser : UserControl
	{
		private readonly PocEntryDto _entry;
		private readonly Button _button;
		private readonly Flyout _flyout;
		private readonly FwOptionPicker _picker;
		private readonly IReadOnlyDictionary<string, MorphTypeOption> _optionsByKey;

		public MorphTypePopupChooser(PocEntryDto entry)
		{
			Name = "MorphTypeChooser";
			_entry = entry;
			_optionsByKey = entry.MorphTypeOptions.ToDictionary(option => option.Key);

			_picker = new FwOptionPicker(
				entry.MorphTypeOptions.Select(option => new RegionChoiceOption(option.Key, option.Name)).ToList(),
				null,
				"MorphTypeChooser");
			_picker.OptionCommitted += option =>
			{
				if (option != null && _optionsByKey.TryGetValue(option.Key, out var resolved))
					Select(resolved);
			};
			_picker.Dismissed += (sender, args) =>
			{
				_flyout.Hide();
				_button.Focus();
			};

			_flyout = FwOptionPicker.CreateOptionFlyout(_picker, PlacementMode.Bottom);

			_button = new Button
			{
				Name = "MorphTypeButton",
				Content = DisplayText(entry.SelectedMorphType),
				Padding = PocDensity.EditorPadding,
				MinHeight = 0,
				HorizontalAlignment = HorizontalAlignment.Left,
				Flyout = _flyout
			};
			AutomationProperties.SetAutomationId(_button, "MorphTypeChooser.Button");
			AutomationProperties.SetName(_button, "Morph Type");

			Content = _button;
		}

		/// <summary>The button that launches the chooser popup.</summary>
		public Button Button => _button;

		/// <summary>The chooser popup flyout.</summary>
		public Flyout Flyout => _flyout;

		/// <summary>The shared select-from-list popup content.</summary>
		public FwOptionPicker Picker => _picker;

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
