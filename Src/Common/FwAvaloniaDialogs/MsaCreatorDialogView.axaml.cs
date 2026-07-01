// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The reusable "Create New Grammatical Info." dialog body (MSA-port Stage 5): a XAML-authored UserControl bound
	/// to <see cref="MsaCreatorDialogViewModel"/> with compiled bindings for the read-only lexical entry / senses +
	/// OK/Cancel/Help, plus the owned grammatical-info group box hosted as a code-behind child (it is a native
	/// composite, not an MVVM-bindable control — the same pattern InsertEntryDialogView uses). Hosted as Avalonia
	/// content inside a WinForms-owned modal Form during coexistence via <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class MsaCreatorDialogView : UserControl
	{
		private Border _msaHost;

		public MsaCreatorDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
			_msaHost = this.FindControl<Border>("PART_MsaSection");
			DataContextChanged += (s, e) => InjectControls();
			InjectControls();
		}

		/// <summary>
		/// Inserts the view-model's owned grammatical-info group box into its host border. The control is created and
		/// driven by the view-model; the view only mounts it.
		/// </summary>
		private void InjectControls()
		{
			var vm = DataContext as MsaCreatorDialogViewModel;
			if (_msaHost != null)
				_msaHost.Child = vm?.MsaGroupBox;
		}
	}
}
