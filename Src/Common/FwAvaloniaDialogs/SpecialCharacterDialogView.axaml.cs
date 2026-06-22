// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The special-character / Unicode insert picker body (Phase-1 §19g) — a filterable character list over a
	/// gated Insert. Net-new (no WinForms truth dialog; the legacy command shells out to the OS charmap). View +
	/// VM + ShowModal; the host reads <see cref="SpecialCharacterDialogViewModel.ChosenCharacter"/> on OK.
	/// </summary>
	public partial class SpecialCharacterDialogView : UserControl
	{
		public SpecialCharacterDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
