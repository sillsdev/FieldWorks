// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using Avalonia.VisualTree;
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
		///
		/// The optional parameters extend the fixed-size default WITHOUT changing it for existing callers:
		///  * <paramref name="resizable"/> — when true the modal gets a sizable border and a minimum size
		///    (defaulting to the initial <paramref name="width"/>/<paramref name="height"/> unless
		///    <paramref name="minWidth"/>/<paramref name="minHeight"/> are supplied). Default false keeps the
		///    legacy <see cref="FormBorderStyle.FixedDialog"/> behavior.
		///  * <paramref name="getRememberedSize"/>/<paramref name="rememberedSizeChanged"/> — an optional
		///    size-persistence hook (mirrors the label-column-width persistence pattern: the caller owns the
		///    remembered value, keyed by dialog identity, so a resized dialog reopens at its last size). The
		///    get-hook (when it returns a value) seeds the initial client size in place of
		///    <paramref name="width"/>/<paramref name="height"/>; the set-hook is invoked on close with the
		///    final client size. Only honored when <paramref name="resizable"/> is true.
		/// </summary>
		public static bool? ShowModal(
			IWin32Window owner,
			AvControl dialogBody,
			IDialogViewModel viewModel,
			string title,
			int width = 420,
			int height = 320,
			bool resizable = false,
			int? minWidth = null,
			int? minHeight = null,
			Func<System.Drawing.Size?> getRememberedSize = null,
			Action<System.Drawing.Size> rememberedSizeChanged = null)
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
					MinimizeBox = false,
					MaximizeBox = false,
					ShowInTaskbar = false
				})
				{
					// Border / min-size / initial (possibly remembered) size — extracted so it is unit-testable
					// without spinning a real modal window.
					ApplySizing(form, width, height, resizable, minWidth, minHeight, getRememberedSize);

					var host = new WinFormsAvaloniaControlHost { Dock = DockStyle.Fill, Content = dialogBody };
					// Headless (test) platform: make the host's Win32 HWND reparent a deliberate no-op (there
					// is no native top-level to reparent). The dialog body still constructs and lays out
					// off-screen. No-op on the real Win32 platform, so the modal behavior is unchanged there.
					FwAvaloniaPlatform.GuardHeadlessEmbed(host);
					form.Controls.Add(host);

					// A11Y-03 (legacy WinForms parity): focus the first input when the dialog opens so a
					// keyboard / screen-reader user lands in a field, not on an unfocused window (and, with
					// the per-view TabIndex, not on the OK/Cancel strip). Done on Shown so the hosted Avalonia
					// content is realized. Best-effort: a no-op if nothing qualifies (never throws).
					form.Shown += (s, e) => FocusInitialControl(dialogBody);

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

					// Persist the final size (only meaningful for resizable dialogs with a set-hook).
					if (resizable)
						rememberedSizeChanged?.Invoke(form.ClientSize);

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
		/// Applies the border style, minimum size and initial (possibly remembered) client size to the hosting
		/// modal <paramref name="form"/>. Factored out of <see cref="ShowModal"/> so the sizing/persistence
		/// contract is unit-testable without spinning a real modal window:
		///  * <paramref name="resizable"/> false → <see cref="FormBorderStyle.FixedDialog"/> and no min-size
		///    (the legacy default; <paramref name="minWidth"/>/<paramref name="minHeight"/>/get-hook ignored);
		///  * <paramref name="resizable"/> true → <see cref="FormBorderStyle.Sizable"/> with a min client size
		///    (<paramref name="minWidth"/>/<paramref name="minHeight"/>, defaulting to the initial
		///    <paramref name="width"/>/<paramref name="height"/>), and the get-hook (when it returns a value)
		///    seeds the initial client size in place of <paramref name="width"/>/<paramref name="height"/>.
		/// The initial size is clamped up to the minimum so a stale remembered size can never open below it.
		/// </summary>
		public static void ApplySizing(
			Form form,
			int width,
			int height,
			bool resizable,
			int? minWidth = null,
			int? minHeight = null,
			Func<System.Drawing.Size?> getRememberedSize = null)
		{
			if (form == null) throw new ArgumentNullException(nameof(form));

			if (!resizable)
			{
				// Legacy fixed-size behavior, unchanged for existing callers.
				form.FormBorderStyle = FormBorderStyle.FixedDialog;
				form.ClientSize = new System.Drawing.Size(width, height);
				return;
			}

			form.FormBorderStyle = FormBorderStyle.Sizable;

			// Minimum size defaults to the initial size so a resizable dialog can never shrink below its
			// design size unless the caller opts into a smaller floor.
			var minW = minWidth ?? width;
			var minH = minHeight ?? height;

			// MinimumSize is an OUTER (window) size in WinForms; converting the client minimum to an outer
			// minimum needs a realized handle. Setting the client size first lets us derive the chrome delta.
			var initial = getRememberedSize?.Invoke() ?? new System.Drawing.Size(width, height);

			// Clamp the (possibly stale/remembered) initial client size up to the client minimum.
			var initialW = Math.Max(initial.Width, minW);
			var initialH = Math.Max(initial.Height, minH);
			form.ClientSize = new System.Drawing.Size(initialW, initialH);

			// Derive the chrome delta from the realized form so MinimumSize (an outer size) corresponds to the
			// requested CLIENT minimum. Falls back to the client minimum if the handle is not yet realized.
			var chromeW = form.Width - form.ClientSize.Width;
			var chromeH = form.Height - form.ClientSize.Height;
			form.MinimumSize = new System.Drawing.Size(minW + Math.Max(0, chromeW), minH + Math.Max(0, chromeH));
		}

		/// <summary>
		/// Focuses the first keyboard-focusable INPUT inside <paramref name="dialogBody"/> so a dialog opens
		/// with the caret in its first field (legacy WinForms parity; A11Y-03). Buttons (OK/Cancel/Help) are
		/// never the initial focus — initial focus belongs to an input, and Enter/Escape already activate the
		/// default/cancel buttons. Returns the control it focused, or null if none qualifies. Factored out
		/// (like <see cref="ApplySizing"/> / <see cref="WireClose"/>) so the selection contract is unit-testable
		/// headlessly without spinning a real WinForms-hosted modal window; <see cref="ShowModal"/> invokes it
		/// on the form's <c>Shown</c> event once the hosted Avalonia content is realized.
		/// </summary>
		public static AvControl FocusInitialControl(AvControl dialogBody)
		{
			if (dialogBody == null) throw new ArgumentNullException(nameof(dialogBody));

			// The first tab stop, honoring the per-view tab order: a focusable control "inherits" the
			// TabIndex of its section (e.g. the bottom button strip carries TabIndex=1, so its OK/Cancel
			// buttons sort AFTER the content at the default 0). OrderBy is stable, so within one section the
			// document/visual order from GetVisualDescendants is preserved. This is robust for dropdown-style
			// pickers too (a ToggleButton is a Button, so a "skip buttons" rule would wrongly skip the only
			// input), and it matches the TabIndex the views set.
			int EffectiveTabIndex(AvControl c)
			{
				var max = 0;
				Avalonia.Visual v = c;
				while (v != null && !ReferenceEquals(v, dialogBody))
				{
					if (v is AvControl ctl && ctl.TabIndex > max)
						max = ctl.TabIndex;
					v = v.GetVisualParent();
				}
				return max;
			}

			// The first focusable INPUT in tab order — never a command button. If a picker-driven dialog
			// exposes no focusable field (the kit's owned FwOptionPicker is deliberately Focusable=false and
			// handles keys directly), focus nothing rather than landing on OK, where Enter would accept the
			// dialog. So this is a no-op for picker dialogs (no regression) and focuses the first text field
			// for text-first dialogs (InsertEntry/EntryGo).
			var candidate = dialogBody.GetVisualDescendants()
				.OfType<AvControl>()
				.Where(c => c.Focusable && c.IsEffectivelyEnabled && c.IsEffectivelyVisible
					&& !(c is Avalonia.Controls.Button))
				.OrderBy(EffectiveTabIndex)
				.FirstOrDefault();
			candidate?.Focus();
			return candidate;
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
