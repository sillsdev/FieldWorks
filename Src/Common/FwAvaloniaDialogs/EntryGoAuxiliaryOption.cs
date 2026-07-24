// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// One LCModel-free option row for the entry-search ("go") dialog's dependent auxiliary picker — the kit
	/// replacement for one item of the legacy per-entry combo an EntryGoDlg child showed under the matching list
	/// (LinkMSADlg's grammatical-info combo, LinkAllomorphDlg's allomorph combo). <see cref="Key"/> is the option's
	/// stable identity (the launcher's choice — e.g. a Guid string) returned on OK; <see cref="Text"/> is the display
	/// the picker shows.
	/// </summary>
	public sealed class EntryGoAuxiliaryOption
	{
		public EntryGoAuxiliaryOption(string key, string text)
		{
			Key = key;
			Text = text ?? string.Empty;
		}

		/// <summary>The option's stable identity (launcher-defined, e.g. a Guid string); the dialog returns this on OK.</summary>
		public string Key { get; }

		/// <summary>The display text shown in the auxiliary picker.</summary>
		public string Text { get; }

		/// <summary>The picker binds to <see cref="Text"/>; ToString keeps simple list rendering correct too.</summary>
		public override string ToString() => Text;
	}
}
