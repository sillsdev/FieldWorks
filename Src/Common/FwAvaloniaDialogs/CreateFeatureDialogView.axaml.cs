// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The minimal "Create New Feature" / "Create New Feature Value" name-entry dialog body (Phase-1 §19b Stage 3): a
	/// XAML-authored UserControl bound to <see cref="CreateFeatureDialogViewModel"/> (name + abbreviation, OK gated on
	/// a non-empty name). Hosted as Avalonia content inside a WinForms-owned modal Form during coexistence via
	/// <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class CreateFeatureDialogView : UserControl
	{
		public CreateFeatureDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
