// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The edited result of the writing-system properties dialog (the bounded §19g core): the LCModel-free
	/// snapshot the launcher applies on OK.
	/// </summary>
	public sealed class WritingSystemProperties
	{
		public string Name { get; set; } = string.Empty;
		public string Abbreviation { get; set; } = string.Empty;
		public string FontName { get; set; } = string.Empty;
		public bool RightToLeft { get; set; }
		public string SortLabel { get; set; } = string.Empty;

		/// <summary>The language tag (e.g. "fr", "seh-fonipa"); not edited here but carried for validation/round-trip.</summary>
		public string Tag { get; set; } = string.Empty;
	}

	/// <summary>
	/// View-model for the writing-system properties / Add-WS dialog (Phase-1 §19g) — the BOUNDED Avalonia core
	/// of the WinForms <c>FwWritingSystemSetupDlg</c>: edit a writing system's name, abbreviation, default font,
	/// right-to-left direction, and sort label, over OK/Cancel. LCModel-free; the launcher seeds it from the
	/// writing-system definition and applies the edited <see cref="WritingSystemProperties"/> on OK.
	///
	/// PARITY (§19g): this is the managed name/abbr/font/direction/sort core only. The full
	/// <c>FwWritingSystemSetupModel</c> surface — SLDR sharing, encoding converters, merge, the
	/// advanced script/region/variant editor, keyboard assignment, and the numbering/character-inventory tabs —
	/// is NOT ported here; those remain the legacy <c>FwWritingSystemSetupDlg</c>. OK is gated on a non-empty
	/// name + abbreviation and a valid (non-duplicate) tag, mirroring the legacy model's <c>IsListValid</c>
	/// /duplicate guard at this granularity.
	/// </summary>
	public partial class WritingSystemPropertiesDialogViewModel : DialogViewModelBase
	{
		private readonly IReadOnlyCollection<string> _existingTags;

		[ObservableProperty] private string _name = string.Empty;
		[ObservableProperty] private string _abbreviation = string.Empty;
		[ObservableProperty] private string _selectedFont = string.Empty;
		[ObservableProperty] private bool _rightToLeft;
		[ObservableProperty] private string _sortLabel = string.Empty;

		private readonly string _tag;

		public WritingSystemPropertiesDialogViewModel()
			: this(new WritingSystemProperties(), System.Array.Empty<string>(), System.Array.Empty<string>())
		{
		}

		/// <param name="seed">Initial values (name/abbr/font/RTL/sort/tag).</param>
		/// <param name="availableFonts">The managed font-family names to offer (no native font enumeration).</param>
		/// <param name="existingTags">Tags already in use, for the duplicate-tag guard (excluding this WS's own tag).</param>
		public WritingSystemPropertiesDialogViewModel(WritingSystemProperties seed,
			IReadOnlyList<string> availableFonts, IReadOnlyCollection<string> existingTags)
		{
			seed = seed ?? new WritingSystemProperties();
			_existingTags = existingTags ?? System.Array.Empty<string>();
			_tag = seed.Tag ?? string.Empty;

			Name = seed.Name ?? string.Empty;
			Abbreviation = seed.Abbreviation ?? string.Empty;
			SelectedFont = seed.FontName ?? string.Empty;
			RightToLeft = seed.RightToLeft;
			SortLabel = seed.SortLabel ?? string.Empty;
			AvailableFonts = availableFonts ?? System.Array.Empty<string>();
		}

		/// <summary>The managed font-family names offered in the font combo.</summary>
		public IReadOnlyList<string> AvailableFonts { get; }

		public string NameLabel => FwAvaloniaDialogsStrings.WritingSystemPropertiesNameLabel;
		public string AbbreviationLabel => FwAvaloniaDialogsStrings.WritingSystemPropertiesAbbrLabel;
		public string FontLabel => FwAvaloniaDialogsStrings.WritingSystemPropertiesFontLabel;
		public string RightToLeftLabel => FwAvaloniaDialogsStrings.WritingSystemPropertiesRightToLeft;
		public string SortLabelCaption => FwAvaloniaDialogsStrings.WritingSystemPropertiesSortLabel;

		/// <summary>The first validation message (name/abbr/tag), shown beneath the fields; empty when valid.</summary>
		public string ValidationMessage
		{
			get
			{
				foreach (var error in GetValidationErrors())
					return error;
				return string.Empty;
			}
		}

		/// <summary>The edited result the launcher applies on OK.</summary>
		public WritingSystemProperties ToResult() => new WritingSystemProperties
		{
			Name = (Name ?? string.Empty).Trim(),
			Abbreviation = (Abbreviation ?? string.Empty).Trim(),
			FontName = SelectedFont ?? string.Empty,
			RightToLeft = RightToLeft,
			SortLabel = (SortLabel ?? string.Empty).Trim(),
			Tag = _tag
		};

		partial void OnNameChanged(string value) => RefreshValidation();
		partial void OnAbbreviationChanged(string value) => RefreshValidation();

		private void RefreshValidation()
		{
			RefreshCanOk();
			OnPropertyChanged(nameof(ValidationMessage));
		}

		/// <summary>OK is gated on a non-empty name + abbreviation and a non-duplicate, valid tag.</summary>
		protected override IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrWhiteSpace(Name))
				yield return FwAvaloniaDialogsStrings.WritingSystemPropertiesNameRequired;
			if (string.IsNullOrWhiteSpace(Abbreviation))
				yield return FwAvaloniaDialogsStrings.WritingSystemPropertiesAbbrRequired;
			if (!string.IsNullOrEmpty(_tag) && IsDuplicateTag(_tag))
				yield return FwAvaloniaDialogsStrings.WritingSystemPropertiesInvalidTag;
		}

		private bool IsDuplicateTag(string tag)
		{
			foreach (var existing in _existingTags)
			{
				if (string.Equals(existing, tag, System.StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}
	}
}
