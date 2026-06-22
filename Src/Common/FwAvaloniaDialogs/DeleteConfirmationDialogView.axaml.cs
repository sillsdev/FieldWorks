// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The delete-confirmation dialog body (Phase-1 §19g) — the Avalonia replacement for the WinForms
	/// <c>ConfirmDeleteObjectDlg</c> (affected-object summary + optional orphan note + gated Delete). View +
	/// VM + ShowModal; the LCModel-aware <c>LcmDeleteObjectLauncher</c> populates it and runs the removal.
	/// </summary>
	public partial class DeleteConfirmationDialogView : UserControl
	{
		public DeleteConfirmationDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
