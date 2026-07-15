// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The Avalonia browse Configure-Columns dialog body (P1: show/hide/reorder) — a XAML-authored
	/// UserControl bound to <see cref="ConfigureColumnsDialogViewModel"/> with compiled bindings: two list
	/// boxes (available / shown) and Add / Remove / Move Up / Move Down, over OK/Cancel. LCModel-free, so it
	/// is pure compiled binding like the other dialog-kit views; hosted as Avalonia content inside a
	/// WinForms-owned modal Form during coexistence via <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class ConfigureColumnsDialogView : UserControl
	{
		public ConfigureColumnsDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
