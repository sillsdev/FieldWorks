// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwAvalonia;
using AvControl = Avalonia.Controls.Control;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// What a dialog launcher returns to its caller: whether the user accepted (OK vs Cancel/closed) plus a
	/// typed <see cref="Payload"/> of follow-up signals the caller acts on (e.g. "writing system changed",
	/// "plugins updated"). Replaces the per-dialog hand-rolled result struct so every launcher reports its
	/// outcome the same shape. <typeparamref name="TPayload"/> is the dialog-specific signal bag; use a
	/// trivial payload (or <see cref="DialogOutcome"/>) when there is nothing beyond Accepted to report.
	/// </summary>
	public struct DialogOutcome<TPayload>
	{
		/// <summary>True when the user closed via OK; false on Cancel or a close without OK.</summary>
		public bool Accepted { get; }

		/// <summary>Dialog-specific follow-up signals; meaningful only when <see cref="Accepted"/> is true.</summary>
		public TPayload Payload { get; }

		public DialogOutcome(bool accepted, TPayload payload)
		{
			Accepted = accepted;
			Payload = payload;
		}

		/// <summary>A not-accepted outcome (Cancel/closed) carrying the default payload.</summary>
		public static DialogOutcome<TPayload> Cancelled => new DialogOutcome<TPayload>(false, default(TPayload));
	}

	/// <summary>Non-generic <see cref="DialogOutcome{TPayload}"/> helpers for dialogs with no extra payload.</summary>
	public static class DialogOutcome
	{
		/// <summary>An accepted outcome carrying <paramref name="payload"/>.</summary>
		public static DialogOutcome<TPayload> Accept<TPayload>(TPayload payload) =>
			new DialogOutcome<TPayload>(true, payload);
	}

	/// <summary>
	/// Generic scaffold for a "launch one Avalonia dialog and apply the result to the live settings"
	/// product launcher. Captures the shape every dialog-edge shares — the part OUTSIDE the view-model that
	/// the dialog-kit (DialogViewModelBase / AvaloniaDialogHost.ShowModal) does not cover:
	///
	///   1. populate a product-supplied state DTO from the live settings (<see cref="BuildState"/>),
	///   2. build the view-model + view from that state (<see cref="CreateViewModel"/> / <see cref="CreateView"/>),
	///   3. show it modally via <see cref="AvaloniaDialogHost.ShowModal"/> (owned/disposed by the host),
	///   4. on OK, apply the edited DTO back to the live settings (<see cref="Apply"/>) and return a typed
	///      <see cref="DialogOutcome{TPayload}"/>; on Cancel/close, return <see cref="DialogOutcome{TPayload}.Cancelled"/>
	///      without applying.
	///
	/// A second dialog becomes "implement BuildState + CreateViewModel + CreateView + Apply", not "copy the
	/// launch/host/dispose/return plumbing". The scaffold lives in FwAvaloniaDialogs (the dialog-kit layer)
	/// because it depends only on the kit + <see cref="AvaloniaDialogHost"/> and has no LCModel/PropertyTable
	/// dependency; the LCModel-aware concrete launcher lives in the product layer (LexText).
	/// </summary>
	/// <typeparam name="TState">The product-supplied state DTO the view-model edits (e.g. OptionsState).</typeparam>
	/// <typeparam name="TViewModel">The dialog view-model (carries the host-close contract via the kit base).</typeparam>
	/// <typeparam name="TPayload">The dialog-specific follow-up signals returned on OK.</typeparam>
	public abstract class AvaloniaDialogLauncher<TState, TViewModel, TPayload>
		where TViewModel : class, IDialogViewModel
	{
		/// <summary>Title for the hosting modal window (typically a localized resource string).</summary>
		protected abstract string DialogTitle { get; }

		/// <summary>Initial client width of the hosting modal window.</summary>
		protected virtual int DialogWidth => 420;

		/// <summary>Initial client height of the hosting modal window.</summary>
		protected virtual int DialogHeight => 320;

		/// <summary>
		/// When true the modal is user-resizable (sizable border + minimum size + optional size persistence).
		/// Default false keeps the legacy fixed-size dialog. Resizable dialogs (choosers, Configure Columns,
		/// Find/Replace) override this and may also override <see cref="MinDialogWidth"/>/
		/// <see cref="MinDialogHeight"/> and the size-persistence hooks.
		/// </summary>
		protected virtual bool Resizable => false;

		/// <summary>Minimum client width when <see cref="Resizable"/>; null = use <see cref="DialogWidth"/>.</summary>
		protected virtual int? MinDialogWidth => null;

		/// <summary>Minimum client height when <see cref="Resizable"/>; null = use <see cref="DialogHeight"/>.</summary>
		protected virtual int? MinDialogHeight => null;

		/// <summary>
		/// Size-persistence get-hook (mirrors the label-column-width pattern): returns the remembered client
		/// size for this dialog identity, or null for none. Only honored when <see cref="Resizable"/>. Override
		/// to read from the product settings/PropertyTable; default returns null (no persistence).
		/// </summary>
		protected virtual System.Drawing.Size? GetRememberedSize() => null;

		/// <summary>
		/// Size-persistence set-hook: records the final client <paramref name="size"/> after the dialog closes,
		/// keyed by dialog identity. Only invoked when <see cref="Resizable"/>. Default is a no-op.
		/// </summary>
		protected virtual void OnRememberedSizeChanged(System.Drawing.Size size)
		{
		}

		/// <summary>Builds the state DTO from the live settings bus (the product edge populate step).</summary>
		protected abstract TState BuildState();

		/// <summary>Builds the dialog view-model over the freshly built <paramref name="state"/>.</summary>
		protected abstract TViewModel CreateViewModel(TState state);

		/// <summary>Builds the dialog body (UserControl) and binds it to <paramref name="viewModel"/>.</summary>
		protected abstract AvControl CreateView(TViewModel viewModel);

		/// <summary>
		/// Applies the edited <paramref name="state"/> back to the live settings (only called on OK) and
		/// returns the dialog-specific follow-up signals. Runs after the view-model has written its edits into
		/// the state via the kit's ApplyChanges convention.
		/// </summary>
		protected abstract TPayload Apply(TState state);

		/// <summary>
		/// The sealed run loop shared by every dialog: build state → VM → view, show modally over
		/// <paramref name="owner"/>, and on OK apply + return the typed payload. The host
		/// (<see cref="AvaloniaDialogHost.ShowModal"/>) owns and disposes the view + view-model after close,
		/// so this method does not dispose them. Returns <see cref="DialogOutcome{TPayload}.Cancelled"/> when
		/// the user cancels or closes without OK (no apply runs).
		/// </summary>
		public DialogOutcome<TPayload> Run(IWin32Window owner)
		{
			var state = BuildState();
			var viewModel = CreateViewModel(state);
			var view = CreateView(viewModel);

			var accepted = AvaloniaDialogHost.ShowModal(owner, view, viewModel, DialogTitle, DialogWidth, DialogHeight,
				resizable: Resizable,
				minWidth: MinDialogWidth,
				minHeight: MinDialogHeight,
				getRememberedSize: Resizable ? GetRememberedSize : (Func<System.Drawing.Size?>)null,
				rememberedSizeChanged: Resizable ? OnRememberedSizeChanged : (Action<System.Drawing.Size>)null);
			if (accepted != true)
				return DialogOutcome<TPayload>.Cancelled;

			return DialogOutcome.Accept(Apply(state));
		}
	}
}
