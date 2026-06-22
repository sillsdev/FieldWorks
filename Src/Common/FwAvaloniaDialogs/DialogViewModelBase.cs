// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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

		[RelayCommand]
		private void Ok()
		{
			ApplyChanges();
			Accepted = true;
			CloseRequested?.Invoke(this, true);
		}

		[RelayCommand]
		private void Cancel()
		{
			Accepted = false;
			CloseRequested?.Invoke(this, false);
		}
	}
}
