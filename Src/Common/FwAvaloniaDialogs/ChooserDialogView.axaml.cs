// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The reusable chooser dialog body: the Avalonia replacement for the legacy
	/// <c>ReallySimpleListChooser</c>/<c>SimpleListChooser</c>. A XAML-authored UserControl bound to
	/// <see cref="ChooserDialogViewModel"/> with compiled bindings for the prompt + OK/Cancel/Help.
	///
	/// FLAT mode (Phase 1): the shared <c>FwOptionPicker</c> is hosted as a code-behind child (the picker is a
	/// native composite, not an MVVM-bindable control, so it cannot be set through a compiled binding).
	///
	/// HIERARCHICAL mode (Phase 2): a XAML-authored search box over a virtualizing <see cref="TreeView"/> (the
	/// candidates folded from their Depth sequence) plus a flat filtered results <see cref="ListBox"/> shown while a
	/// search term is active. Single-select commits the clicked node/row's key (via TreeView/ListBox selection);
	/// multi-select toggles per-node checkboxes (two-way bound to the node, independent per node — legacy default),
	/// with Space toggling the focused node's check for keyboard parity.
	///
	/// Hosted as Avalonia content inside a WinForms-owned modal Form during coexistence via
	/// <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class ChooserDialogView : UserControl
	{
		private Border _pickerHost;

		public ChooserDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
			_pickerHost = this.FindControl<Border>("PART_PickerHost");
			DataContextChanged += (s, e) => InjectPicker();
			InjectPicker();
			// Space toggles the focused tree node's check (multi-select keyboard parity); the TreeView's own
			// Up/Down/Left/Right already drive navigation + expand/collapse.
			AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
		}

		/// <summary>
		/// Inserts the view-model's owned <c>FwOptionPicker</c> into the picker-host border (FLAT mode only; the
		/// picker is null in hierarchical mode). The picker is created and driven by the view-model; the view only
		/// mounts it.
		/// </summary>
		private void InjectPicker()
		{
			if (_pickerHost == null)
				return;
			_pickerHost.Child = (DataContext as ChooserDialogViewModel)?.Picker;
		}

		private void OnTreeSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!(DataContext is ChooserDialogViewModel vm) || vm.IsMultiSelect)
				return; // multi-select uses checkboxes, not selection
			if ((sender as TreeView)?.SelectedItem is ChooserTreeNode node)
				vm.SelectSingle(node.Key);
		}

		private void OnSearchResultSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!(DataContext is ChooserDialogViewModel vm) || vm.IsMultiSelect)
				return; // multi-select uses checkboxes, not selection
			if ((sender as ListBox)?.SelectedItem is ChooserTreeNode node)
				vm.SelectSingle(node.Key);
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Space)
				return;
			if (!(DataContext is ChooserDialogViewModel vm) || !vm.IsMultiSelect)
				return;
			// Toggle the check of the focused tree node / search-result row. Route through the VM (a plain toggle)
			// so it (re)establishes the range anchor exactly like a plain click — keeping keyboard + mouse in step.
			if (e.Source is Control c && c.DataContext is ChooserTreeNode node)
			{
				vm.ToggleChecked(node.Key);
				e.Handled = true;
			}
		}

		/// <summary>
		/// Whole-row click toggle for the multi-select tree / flat search list: a click anywhere on a row (the label
		/// area, not only the checkbox) toggles that item's check; holding Shift ranges from the anchor (the last
		/// plainly-clicked row) to this row. The CheckBox in the row template is display-only (IsHitTestVisible=False),
		/// so a click on the box bubbles to this same handler and toggles exactly ONCE (no double-toggle). Single-select
		/// is unaffected (the VM toggle no-ops outside multi-select; selection still drives the single pick).
		/// </summary>
		private void OnRowPointerReleased(object sender, PointerReleasedEventArgs e)
		{
			if (e.InitialPressMouseButton != MouseButton.Left)
				return;
			if (!(DataContext is ChooserDialogViewModel vm) || !vm.IsMultiSelect)
				return;
			if (!((sender as Control)?.DataContext is ChooserTreeNode node))
				return;
			var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
			vm.ToggleChecked(node.Key, shift);
			e.Handled = true;
		}
	}
}
