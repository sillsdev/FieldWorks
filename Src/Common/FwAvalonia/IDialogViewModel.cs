// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Contract a dialog view-model implements so <see cref="AvaloniaDialogHost"/> can close the hosting
	/// WinForms modal window when the dialog's own OK/Cancel logic decides to. The view-model never
	/// references WinForms or Avalonia windowing — it just raises <see cref="CloseRequested"/> with the
	/// outcome (true = accepted/OK, false = cancelled). This keeps modality out of the view-model and on
	/// the host, matching the dialog-ownership rule (the host owns the modal window during coexistence).
	///
	/// Resource ownership: the view-model owns its own resources. If it also implements
	/// <see cref="System.IDisposable"/>, <see cref="AvaloniaDialogHost.ShowModal"/> disposes it after the
	/// dialog closes (together with the dialog body). A view-model therefore must not assume it outlives the
	/// modal call, and must tolerate disposal even if the dialog was closed without an OK/Cancel.
	/// </summary>
	public interface IDialogViewModel
	{
		/// <summary>Raised when the view-model wants the dialog closed. The bool is the accepted result.</summary>
		event EventHandler<bool> CloseRequested;
	}
}
