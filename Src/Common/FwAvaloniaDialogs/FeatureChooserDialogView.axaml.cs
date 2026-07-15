// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The standalone feature-structure chooser dialog body (Phase-1 §19b Stage 3): a XAML-authored UserControl bound
	/// to <see cref="FeatureChooserDialogViewModel"/> with compiled bindings for the prompt + OK/Cancel/Help, plus the
	/// owned <see cref="FwFeatureStructureEditor"/> hosted as a code-behind child (it is a native control, not an
	/// MVVM-bindable one — the same pattern <see cref="MsaCreatorDialogView"/> uses for the MSA box). Hosted as
	/// Avalonia content inside a WinForms-owned modal Form during coexistence via <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class FeatureChooserDialogView : UserControl
	{
		private Border _featureHost;

		public FeatureChooserDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
			_featureHost = this.FindControl<Border>("PART_FeatureSection");
			DataContextChanged += (s, e) => InjectControls();
			InjectControls();
		}

		/// <summary>
		/// Inserts the view-model's owned feature editor into its host border. The control is created and driven by
		/// the view-model; the view only mounts it.
		/// </summary>
		private void InjectControls()
		{
			var vm = DataContext as FeatureChooserDialogViewModel;
			if (_featureHost != null)
				_featureHost.Child = vm?.Editor;
		}
	}
}
