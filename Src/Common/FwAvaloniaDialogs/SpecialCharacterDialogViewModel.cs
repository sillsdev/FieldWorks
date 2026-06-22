// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FwAvaloniaDialogs
{
	/// <summary>One insertable character in the special-character picker: the character itself, its code point label, and its name.</summary>
	public sealed class SpecialCharacterItem
	{
		public SpecialCharacterItem(string character, string codeLabel, string name)
		{
			Character = character;
			CodeLabel = codeLabel;
			Name = name;
		}

		/// <summary>The character string to insert.</summary>
		public string Character { get; }

		/// <summary>The "U+00E9"-style code-point label shown in the list.</summary>
		public string CodeLabel { get; }

		/// <summary>The human-readable character name shown in the list.</summary>
		public string Name { get; }

		/// <summary>The combined display row (character + code + name).</summary>
		public string Display => $"{Character}    {CodeLabel}    {Name}";
	}

	/// <summary>
	/// View-model for the special-character / Unicode insert picker (Phase-1 §19g). Unlike the other §19g
	/// dialogs there is no WinForms truth dialog to port — the legacy <c>Format &gt; Special character</c> command
	/// shells out to the OS character map (<c>charmap.exe</c>/<c>gucharmap</c>). This is a net-new in-app Avalonia
	/// picker: a curated, filterable list of commonly-needed characters (combining diacritics, punctuation,
	/// IPA-ish symbols, arrows) over OK/Cancel. OK is gated on a selection; the chosen character is read by the
	/// host and inserted into the focused field. The legacy OS-charmap shellout is preserved for the legacy path.
	/// </summary>
	public partial class SpecialCharacterDialogViewModel : DialogViewModelBase
	{
		private readonly IReadOnlyList<SpecialCharacterItem> _all;

		[ObservableProperty] private string _filter = string.Empty;
		[ObservableProperty] private SpecialCharacterItem _selected;

		public SpecialCharacterDialogViewModel()
			: this(DefaultCharacters())
		{
		}

		/// <param name="characters">The candidate characters (the default curated set, or a host-supplied list).</param>
		public SpecialCharacterDialogViewModel(IReadOnlyList<SpecialCharacterItem> characters)
		{
			_all = characters ?? DefaultCharacters();
			VisibleCharacters = new ObservableCollection<SpecialCharacterItem>(_all);
		}

		/// <summary>The currently-visible (filtered) characters.</summary>
		public ObservableCollection<SpecialCharacterItem> VisibleCharacters { get; }

		/// <summary>The watermark prompt for the filter box (localized).</summary>
		public string FilterPrompt => FwAvaloniaDialogsStrings.SpecialCharacterFilterPrompt;

		/// <summary>The OK-gate message shown when nothing is selected (localized).</summary>
		public string MustSelectMessage => FwAvaloniaDialogsStrings.SpecialCharacterMustSelect;

		/// <summary>The chosen character string (read by the host on OK); empty when nothing is selected.</summary>
		public string ChosenCharacter => Selected?.Character ?? string.Empty;

		partial void OnFilterChanged(string value) => ApplyFilter();

		partial void OnSelectedChanged(SpecialCharacterItem value) => RefreshCanOk();

		private void ApplyFilter()
		{
			var text = (Filter ?? string.Empty).Trim();
			IEnumerable<SpecialCharacterItem> matches = _all;
			if (text.Length > 0)
			{
				matches = _all.Where(c =>
					c.Name.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
					c.CodeLabel.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
					c.Character == text);
			}

			VisibleCharacters.Clear();
			foreach (var item in matches)
				VisibleCharacters.Add(item);

			// Drop a selection that filtered out, so OK can't insert a now-hidden character.
			if (Selected != null && !VisibleCharacters.Contains(Selected))
				Selected = null;
		}

		/// <summary>OK is gated on a current selection (you must pick a character to insert).</summary>
		protected override IEnumerable<string> GetValidationErrors()
		{
			if (Selected == null)
				yield return MustSelectMessage;
		}

		/// <summary>
		/// Builds an insertable item from a single code point, formatting the "U+XXXX" label and using the
		/// supplied display name.
		/// </summary>
		public static SpecialCharacterItem FromCodePoint(int codePoint, string name)
		{
			var ch = char.ConvertFromUtf32(codePoint);
			var label = "U+" + codePoint.ToString("X4", CultureInfo.InvariantCulture);
			return new SpecialCharacterItem(ch, label, name);
		}

		// A small curated set covering the most common lexicography insert needs: combining diacritics, IPA-ish
		// letters, punctuation, and arrows. Data, not UI strings — left unlocalized (the names are stable Unicode
		// names). The host may supply its own list for a project-specific inventory.
		private static IReadOnlyList<SpecialCharacterItem> DefaultCharacters()
		{
			return new[]
			{
				FromCodePoint(0x0301, "Combining Acute Accent"),
				FromCodePoint(0x0300, "Combining Grave Accent"),
				FromCodePoint(0x0302, "Combining Circumflex Accent"),
				FromCodePoint(0x0303, "Combining Tilde"),
				FromCodePoint(0x0304, "Combining Macron"),
				FromCodePoint(0x030C, "Combining Caron"),
				FromCodePoint(0x0308, "Combining Diaeresis"),
				FromCodePoint(0x0327, "Combining Cedilla"),
				FromCodePoint(0x0259, "Latin Small Letter Schwa"),
				FromCodePoint(0x025B, "Latin Small Letter Open E"),
				FromCodePoint(0x0254, "Latin Small Letter Open O"),
				FromCodePoint(0x014B, "Latin Small Letter Eng"),
				FromCodePoint(0x0294, "Latin Letter Glottal Stop"),
				FromCodePoint(0x02BC, "Modifier Letter Apostrophe"),
				FromCodePoint(0x2019, "Right Single Quotation Mark"),
				FromCodePoint(0x2018, "Left Single Quotation Mark"),
				FromCodePoint(0x201C, "Left Double Quotation Mark"),
				FromCodePoint(0x201D, "Right Double Quotation Mark"),
				FromCodePoint(0x2013, "En Dash"),
				FromCodePoint(0x2014, "Em Dash"),
				FromCodePoint(0x2026, "Horizontal Ellipsis"),
				FromCodePoint(0x00A0, "No-Break Space"),
				FromCodePoint(0x200B, "Zero Width Space"),
				FromCodePoint(0x2192, "Rightwards Arrow"),
				FromCodePoint(0x2190, "Leftwards Arrow")
			};
		}
	}
}
