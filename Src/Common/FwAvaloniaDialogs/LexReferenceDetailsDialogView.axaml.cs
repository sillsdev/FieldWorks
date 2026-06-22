// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The "Reference Set Details" dialog body (Phase-1 §19g) — name + comment over OK/Cancel; the Avalonia
	/// replacement for the WinForms <c>LexReferenceDetailsDlg</c>. View + VM + ShowModal; the LCModel-aware
	/// <c>LcmLexReferenceDetailsLauncher</c> seeds/applies through a UOW.
	/// </summary>
	public partial class LexReferenceDetailsDialogView : UserControl
	{
		public LexReferenceDetailsDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
