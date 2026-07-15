// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The "Manage Individual Features" dialog body — a XAML-authored UserControl bound to
	/// <see cref="LexicalEditFeatureManagerDialogViewModel"/> with compiled bindings and ordinary Avalonia
	/// controls throughout (TextBox, ItemsControl, CheckBox); no owned native control is injected, so
	/// there is nothing to wire up beyond the base bootstrap. Hosted as Avalonia content inside a
	/// WinForms-owned modal Form during coexistence via <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class LexicalEditFeatureManagerDialogView : UserControl
	{
		public LexicalEditFeatureManagerDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
