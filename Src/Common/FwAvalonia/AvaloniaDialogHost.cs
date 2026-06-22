// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using Avalonia.Win32.Interoperability;
using AvControl = Avalonia.Controls.Control;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Reusable host that shows an Avalonia dialog body (a <c>UserControl</c>) inside a WinForms-owned
	/// modal <see cref="Form"/> during coexistence — the turn-key piece for the MVVM dialog kit. Because
	/// Avalonia modal windows are not supported while WinForms owns the message loop (dialog-ownership.md),
	/// the dialog body is hosted in a WinForms modal window owned by the caller's form; the view-model
	/// closes it by raising <see cref="IDialogViewModel.CloseRequested"/> (no windowing in the VM).
	///
	/// A new dialog is then: build the view + view-model, call <see cref="ShowModal"/>.
	/// </summary>
	public static class AvaloniaDialogHost
	{
		/// <summary>
		/// Shows <paramref name="dialogBody"/> modally over <paramref name="owner"/>. Returns the accepted
		/// result (true = OK, false = Cancel), or null if the window was closed without an OK/Cancel.
		/// </summary>
		public static bool? ShowModal(
			IWin32Window owner,
			AvControl dialogBody,
			IDialogViewModel viewModel,
			string title,
			int width = 420,
			int height = 320)
		{
			if (dialogBody == null) throw new ArgumentNullException(nameof(dialogBody));
			if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

			// Modal hosting + Avalonia share the single WinForms UI thread / message loop during coexistence
			// (dialog-ownership.md). Showing a modal Form or touching Avalonia controls off that thread is a
			// re-entrancy/cross-thread bug; fail fast rather than corrupt the message loop. The owner control
			// (when supplied) is the WinForms host whose thread we must be on.
			if (owner is Control ownerControl && ownerControl.InvokeRequired)
				throw new InvalidOperationException(
					"AvaloniaDialogHost.ShowModal must be called on the UI thread (the WinForms message-loop thread).");

			FwAvaloniaRuntime.EnsureInitialized();

			// Compact density so every kit dialog matches the legacy WinForms dialog density, not the
			// roomy Fluent defaults. Applied here (the single dialog chokepoint) so new dialogs inherit it.
			CompactDialogStyles.Apply(dialogBody);

			// dialog-ownership.md: remember the WinForms focus so it returns to the owner after close.
			var priorFocus = Form.ActiveForm?.ActiveControl;

			try
			{
				using (var form = new Form
				{
					Text = title ?? string.Empty,
					StartPosition = FormStartPosition.CenterParent,
					FormBorderStyle = FormBorderStyle.FixedDialog,
					MinimizeBox = false,
					MaximizeBox = false,
					ShowInTaskbar = false,
					ClientSize = new System.Drawing.Size(width, height)
				})
				{
					var host = new WinFormsAvaloniaControlHost { Dock = DockStyle.Fill, Content = dialogBody };
					form.Controls.Add(host);

					bool? result = null;
					using (WireClose(viewModel, accepted =>
					{
						result = accepted;
						form.DialogResult = accepted ? DialogResult.OK : DialogResult.Cancel;
						form.Close();
					}))
					{
						form.ShowDialog(owner);
					}

					if (priorFocus != null && !priorFocus.IsDisposed)
						priorFocus.Focus();
					return result;
				}
			}
			finally
			{
				// Done in a finally so a failed ShowDialog still releases them.
				DisposeDialogResources(dialogBody, viewModel);
			}
		}

		/// <summary>
		/// Releases the resources <see cref="ShowModal"/> owns once the modal closes: the host owns the
		/// dialog body it was handed and disposes it; the view-model owns its own resources
		/// (<see cref="IDialogViewModel"/>) and is disposed here if it is <see cref="IDisposable"/>. Idempotent
		/// and null-tolerant; factored out so the ownership/disposal contract is unit-testable without
		/// spinning a real modal window.
		/// </summary>
		public static void DisposeDialogResources(AvControl dialogBody, IDialogViewModel viewModel)
		{
			(dialogBody as IDisposable)?.Dispose();
			(viewModel as IDisposable)?.Dispose();
		}

		/// <summary>
		/// Subscribes a dialog view-model's close signal to <paramref name="onClose"/>; dispose the result
		/// to unsubscribe. Exposed (and used by <see cref="ShowModal"/>) so the close wiring is unit-testable
		/// without spinning a real modal window.
		/// </summary>
		public static IDisposable WireClose(IDialogViewModel viewModel, Action<bool> onClose)
		{
			if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
			if (onClose == null) throw new ArgumentNullException(nameof(onClose));

			void Handler(object sender, bool accepted) => onClose(accepted);
			viewModel.CloseRequested += Handler;
			return new Unsubscriber(() => viewModel.CloseRequested -= Handler);
		}

		private sealed class Unsubscriber : IDisposable
		{
			private Action _dispose;
			public Unsubscriber(Action dispose) => _dispose = dispose;
			public void Dispose()
			{
				_dispose?.Invoke();
				_dispose = null;
			}
		}
	}
}
