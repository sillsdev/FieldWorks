// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The reusable Add New Sense dialog body (MSA-port Stage 5): a XAML-authored UserControl bound to
	/// <see cref="AddNewSenseDialogViewModel"/> with compiled bindings for the read-only citation form +
	/// Create/Cancel/Help, plus the owned gloss field and grammatical-info group box hosted as code-behind children
	/// (each is a native composite, not an MVVM-bindable control — the same pattern InsertEntryDialogView uses).
	/// Hosted as Avalonia content inside a WinForms-owned modal Form during coexistence via
	/// <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class AddNewSenseDialogView : UserControl
	{
		private Border _glossHost;
		private Border _msaHost;

		public AddNewSenseDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
			_glossHost = this.FindControl<Border>("PART_GlossHost");
			_msaHost = this.FindControl<Border>("PART_MsaSection");
			DataContextChanged += (s, e) => InjectControls();
			InjectControls();
		}

		/// <summary>
		/// Inserts the view-model's owned controls (gloss field, grammatical-info group box) into their host borders.
		/// The controls are created and driven by the view-model; the view only mounts them.
		/// </summary>
		private void InjectControls()
		{
			var vm = DataContext as AddNewSenseDialogViewModel;
			if (_glossHost != null)
				_glossHost.Child = vm?.GlossField;
			if (_msaHost != null)
				_msaHost.Child = vm?.MsaGroupBox;
		}
	}
}
