// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The reusable entry-search ("go") dialog body: the Avalonia replacement for the legacy
	/// <c>EntryGoDlg</c>/<c>BaseGoDlg</c> family. A XAML-authored UserControl bound to
	/// <see cref="EntryGoDialogViewModel"/> with compiled bindings — a search box whose matching-entries list is a
	/// focus-gated, on-top dropdown (a <c>Popup</c> that escapes the dialog bounds, like a filter combo), an extended
	/// description region on the right that hosts rich content for the highlighted entry (degrading to the plain
	/// description string), and Cancel/Help. This is a COMMIT-ON-SELECT picker — there is no OK button: picking a
	/// result (double-click a row, or Enter on the highlighted row) commits + closes accepted via the view-model's
	/// <c>CommitCommand</c>; Cancel / Escape / the window close button cancel. Selection and search are MVVM; the
	/// code-behind only (a) feeds the search box's focus into the view-model so the dropdown shows ONLY while the
	/// field is focused, (b) translates the result list's double-click / Enter gesture into the commit command, and
	/// (c) removes the two-stage OK button from the tree for single-stage consumers (with an auxiliary spec the
	/// dialog is two-stage and OK commits; see <see cref="EntryGoDialogViewModel.HasAuxiliarySelection"/>).
	/// Hosted as Avalonia content inside a WinForms-owned modal Form during coexistence via
	/// <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class EntryGoDialogView : UserControl
	{
		public EntryGoDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();

			// Feed the search box's focus into the view-model's IsSearchFocused so the results dropdown is gated to
			// "the user is in the search field" (the filter-combo behavior). The dropdown is a Popup top-level, so
			// the box keeps logical focus while the user mouses over its rows.
			var searchBox = this.FindControl<TextBox>("PART_SearchBox");
			if (searchBox != null)
			{
				searchBox.GotFocus += OnSearchBoxGotFocus;
				searchBox.LostFocus += OnSearchBoxLostFocus;
			}

			// Commit-on-select gestures on the results list: a double-click of a row, or Enter on the highlighted
			// row, commits the selection + closes accepted (the kit's CommitCommand). The list lives inside the
			// focus-gated Popup, so wire it from code-behind once it is realized.
			var results = this.FindControl<ListBox>("PART_Results");
			if (results != null)
			{
				results.DoubleTapped += OnResultsDoubleTapped;
				results.KeyDown += OnResultsKeyDown;
			}

			// The OK button exists only for two-stage auxiliary consumers; single-stage (commit-on-select) consumers
			// keep their exact OK-less surface, so remove it from the tree (not merely hide it) once the VM arrives.
			DataContextChanged += OnDataContextChangedRemoveOkIfSingleStage;
		}

		private void OnDataContextChangedRemoveOkIfSingleStage(object sender, System.EventArgs e)
		{
			var vm = ViewModel;
			if (vm == null || vm.HasAuxiliarySelection)
				return;
			var okButton = this.FindControl<Button>("PART_OkButton");
			(okButton?.Parent as Avalonia.Controls.Panel)?.Children.Remove(okButton);
		}

		private EntryGoDialogViewModel ViewModel => DataContext as EntryGoDialogViewModel;

		// A double-click on a result row commits that selection (the row the click selected) + closes accepted.
		private void OnResultsDoubleTapped(object sender, TappedEventArgs e)
		{
			TryCommit();
		}

		// Enter on the highlighted result row commits it (the legacy "press Enter on the list to accept" gesture).
		private void OnResultsKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter)
				return;
			if (TryCommit())
				e.Handled = true;
		}

		// Runs the commit-on-select command when it can execute (a row is selected). Returns true when it committed.
		private bool TryCommit()
		{
			var command = ViewModel?.CommitCommand;
			if (command == null || !command.CanExecute(null))
				return false;
			command.Execute(null);
			return true;
		}

		private void OnSearchBoxGotFocus(object sender, GotFocusEventArgs e)
		{
			if (ViewModel != null)
				ViewModel.IsSearchFocused = true;
		}

		private void OnSearchBoxLostFocus(object sender, RoutedEventArgs e)
		{
			// Closing the dropdown when the field loses focus is the filter-combo behavior. Selecting a row inside
			// the Popup (a separate top-level) does not move keyboard focus out of the box in the headless/owned-host
			// path, so this fires only on a real focus change away from the search field.
			if (ViewModel != null)
				ViewModel.IsSearchFocused = false;
		}
	}
}
