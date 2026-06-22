// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The writing-system properties / Add-WS dialog body (Phase-1 §19g) — the bounded managed core (name,
	/// abbreviation, font, direction, sort) of the WinForms <c>FwWritingSystemSetupDlg</c>. View + VM +
	/// ShowModal; the LCModel-aware launcher seeds + applies the edited <see cref="WritingSystemProperties"/>.
	/// </summary>
	public partial class WritingSystemPropertiesDialogView : UserControl
	{
		public WritingSystemPropertiesDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
