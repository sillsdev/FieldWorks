// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// Shared base for the MVVM dialog kit's view-models so a new dialog is "view + VM + ShowModal", not a
	/// copy of the Options dialog's close/OK/Cancel plumbing. It owns the host-close contract
	/// (<see cref="IDialogViewModel.CloseRequested"/>), the generated <c>OkCommand</c>/<c>CancelCommand</c>,
	/// and the <see cref="Accepted"/> result. On OK it runs the <see cref="ApplyChanges"/> convention
	/// (write the edited values back into the product-supplied state DTO) and then raises close(true); on
	/// Cancel it raises close(false). Derived view-models stay <c>partial : DialogViewModelBase</c> so the
	/// CommunityToolkit.Mvvm source generator can emit their own observable properties/commands alongside
	/// the inherited ones.
	/// </summary>
	public abstract partial class DialogViewModelBase : ObservableObject, IDialogViewModel
	{
		/// <summary>Null until the user closes the dialog: true = OK, false = Cancel.</summary>
		public bool? Accepted { get; private set; }

		/// <inheritdoc />
		public event EventHandler<bool> CloseRequested;

		/// <summary>
		/// Writes the view-model's edited values back into the product-supplied state DTO. Called by the OK
		/// command before the dialog closes (the "ApplyTo(state)" convention). Default is a no-op so trivial
		/// confirmation dialogs need not override it.
		/// </summary>
		protected virtual void ApplyChanges()
		{
		}

		/// <summary>
		/// OK-gating convention: when this is false the generated <c>OkCommand</c> reports
		/// <c>CanExecute == false</c>, so a bound OK button disables itself and the command refuses to run.
		/// Default is <c>true</c> so existing dialogs (Options, message box) keep an always-enabled OK.
		///
		/// Derived view-models gate OK in one of two ways:
		///  * override <see cref="GetValidationErrors"/> to supply field-level validation messages (the
		///    default <c>CanOk</c> is "no validation errors"), or
		///  * override <c>CanOk</c> directly for a custom rule.
		/// Either way, whenever the inputs that affect validity change, call <see cref="RefreshCanOk"/> (e.g.
		/// from a generated <c>OnXxxChanged</c> partial) so the OK button re-evaluates. This mirrors the
		/// CommunityToolkit <c>NotifyCanExecuteChangedFor</c> pattern without forcing every derived VM to
		/// name the command in an attribute.
		/// </summary>
		protected virtual bool CanOk => !GetValidationErrors().Any();

		/// <summary>
		/// Overridable hook for derived view-models to report current validation errors (empty = valid). The
		/// default <see cref="CanOk"/> gates OK on this being empty. Default returns no errors. Returning a
		/// non-null, possibly-empty sequence is the contract; callers tolerate an empty result.
		/// </summary>
		protected virtual IEnumerable<string> GetValidationErrors() => Enumerable.Empty<string>();

		/// <summary>
		/// The current validation errors (a materialized snapshot of <see cref="GetValidationErrors"/>), so a
		/// view can bind/show them. Empty when the dialog is valid.
		/// </summary>
		public IReadOnlyList<string> ValidationErrors =>
			(GetValidationErrors() ?? Enumerable.Empty<string>()).ToList();

		/// <summary>True when the dialog is valid and OK may run; the public face of <see cref="CanOk"/>.</summary>
		public bool IsValid => CanOk;

		/// <summary>
		/// Re-evaluates the OK gate after the inputs that affect <see cref="CanOk"/>/validation change. Raises
		/// <c>OkCommand.CanExecuteChanged</c> (so a bound OK button enables/disables) and notifies the
		/// validation-projection properties. Derived view-models call this from their property-changed hooks.
		/// </summary>
		protected void RefreshCanOk()
		{
			OkCommand.NotifyCanExecuteChanged();
			OnPropertyChanged(nameof(IsValid));
			OnPropertyChanged(nameof(ValidationErrors));
		}

		[RelayCommand(CanExecute = nameof(CanOk))]
		private void Ok()
		{
			// Defensive: CanExecute already gates a bound button, but a direct Execute call must also honor
			// the gate so an invalid dialog can never apply/close via OK.
			if (!CanOk)
				return;
			ApplyChanges();
			RequestClose(true);
		}

		[RelayCommand]
		private void Cancel()
		{
			RequestClose(false);
		}

		/// <summary>
		/// Records the outcome (<see cref="Accepted"/>) and raises <see cref="IDialogViewModel.CloseRequested"/>
		/// so the host closes the modal window. Exposed to derived view-models (e.g. the message box, which needs
		/// extra Yes/No buttons beyond the inherited OK/Cancel commands) so they can close with the right
		/// accepted/cancelled outcome without re-implementing the close contract. <paramref name="accepted"/> is
		/// true for an affirmative close (OK/Yes), false for a negative one (Cancel/No).
		/// </summary>
		protected void RequestClose(bool accepted)
		{
			Accepted = accepted;
			CloseRequested?.Invoke(this, accepted);
		}
	}
}
