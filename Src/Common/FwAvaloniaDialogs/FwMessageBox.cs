// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The button the user clicked to dismiss an <see cref="FwMessageBox"/>, mirroring the legacy
	/// <see cref="System.Windows.Forms.DialogResult"/> values FieldWorks call sites already switch on. Returned by
	/// <see cref="FwMessageBox.Show(IWin32Window,string,string,FwMessageBoxButtons,FwMessageBoxIcon)"/>.
	/// <see cref="None"/> means the dialog was dismissed without a choice (e.g. the window was closed).
	/// </summary>
	public enum FwMessageBoxResult
	{
		/// <summary>The dialog closed without a button choice (window closed / Esc with no Cancel button).</summary>
		None,
		/// <summary>The OK button was clicked.</summary>
		Ok,
		/// <summary>The Cancel button was clicked (or Esc with a Cancel/No present).</summary>
		Cancel,
		/// <summary>The Yes button was clicked.</summary>
		Yes,
		/// <summary>The No button was clicked.</summary>
		No
	}

	/// <summary>
	/// Which buttons an <see cref="FwMessageBox"/> shows — the FieldWorks-relevant subset of
	/// <see cref="System.Windows.Forms.MessageBoxButtons"/>. The default (affirmative) button is the first
	/// listed; the cancel button is the last (Cancel/No), used for Esc/close handling.
	/// </summary>
	public enum FwMessageBoxButtons
	{
		/// <summary>A single OK button (the default button); closing the window returns <see cref="FwMessageBoxResult.Ok"/>.</summary>
		Ok,
		/// <summary>OK (default) and Cancel; closing without a choice returns <see cref="FwMessageBoxResult.Cancel"/>.</summary>
		OkCancel,
		/// <summary>Yes (default) and No; closing without a choice returns <see cref="FwMessageBoxResult.No"/>.</summary>
		YesNo,
		/// <summary>Yes (default), No, and Cancel; closing without a choice returns <see cref="FwMessageBoxResult.Cancel"/>.</summary>
		YesNoCancel
	}

	/// <summary>
	/// Severity/icon shown beside the message, mirroring the FieldWorks-relevant subset of
	/// <see cref="System.Windows.Forms.MessageBoxIcon"/>. Rendered as a glyph + accessible label, not a bitmap,
	/// so it themes with Fluent and carries an automation name.
	/// </summary>
	public enum FwMessageBoxIcon
	{
		/// <summary>No icon.</summary>
		None,
		/// <summary>Informational message.</summary>
		Information,
		/// <summary>Warning message.</summary>
		Warning,
		/// <summary>Error message.</summary>
		Error,
		/// <summary>A question (used with Yes/No prompts).</summary>
		Question
	}

	/// <summary>
	/// Avalonia analog of <see cref="System.Windows.Forms.MessageBox"/> for the FieldWorks dialog kit. Call sites
	/// read like the legacy <c>MessageBox.Show(owner, message, title, buttons, icon)</c> but the dialog renders in
	/// Avalonia (the kit's <see cref="MessageBoxView"/> + <see cref="MessageBoxViewModel"/>) hosted in a
	/// WinForms-owned modal window via <see cref="AvaloniaDialogHost.ShowModal"/> during coexistence
	/// (dialog-ownership.md). Button labels are localized (<see cref="FwAvaloniaDialogsStrings"/>), the default
	/// button takes focus, and compact density is applied by the host. Replaces raw <c>MessageBox.Show</c> in the
	/// migrated lexical-edit/options/browse surfaces so confirmations match the rest of the Avalonia UI.
	/// </summary>
	public static class FwMessageBox
	{
		/// <summary>
		/// Shows a modal message/confirmation dialog over <paramref name="owner"/> and returns the chosen button.
		/// </summary>
		/// <param name="owner">The WinForms host form that owns the modal window (dialog-ownership.md). May be null.</param>
		/// <param name="message">The message body. Wraps; required.</param>
		/// <param name="title">The window title (defaults to empty).</param>
		/// <param name="buttons">Which buttons to show (defaults to a single OK).</param>
		/// <param name="icon">The severity icon (defaults to none).</param>
		/// <returns>The clicked button, or <see cref="FwMessageBoxResult.None"/> if dismissed without a choice.</returns>
		public static FwMessageBoxResult Show(
			IWin32Window owner,
			string message,
			string title = null,
			FwMessageBoxButtons buttons = FwMessageBoxButtons.Ok,
			FwMessageBoxIcon icon = FwMessageBoxIcon.None)
		{
			if (message == null) throw new ArgumentNullException(nameof(message));

			var viewModel = new MessageBoxViewModel(message, buttons, icon);
			var view = new MessageBoxView { DataContext = viewModel };

			AvaloniaDialogHost.ShowModal(owner, view, viewModel, title ?? string.Empty,
				MeasureWidth(message), MeasureHeight(message, icon));

			// The VM is not IDisposable, so it survives ShowModal; read the precise button it recorded
			// (CloseRequested only carries an accepted/cancelled bool, which can't distinguish Yes vs OK).
			return viewModel.Result;
		}

		// Rough auto-size so short prompts get a compact box and long ones get room, without a layout pass.
		// Bounded so a pathological message can't produce a giant window.
		private static int MeasureWidth(string message)
		{
			var longestLine = 0;
			foreach (var line in message.Split('\n'))
				if (line.Length > longestLine)
					longestLine = line.Length;
			return Clamp(120 + longestLine * 6, 280, 560);
		}

		private static int MeasureHeight(string message, FwMessageBoxIcon icon)
		{
			var lines = message.Split('\n').Length + message.Length / 70;
			var body = Clamp(70 + lines * 18, 110, 360);
			return icon == FwMessageBoxIcon.None ? body : Math.Max(body, 130);
		}

		private static int Clamp(int value, int min, int max) =>
			value < min ? min : (value > max ? max : value);
	}
}
