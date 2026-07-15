// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The browse "Filter For…" pattern-setup dialog body — a XAML-authored UserControl bound to
	/// <see cref="FilterForDialogViewModel"/> with compiled bindings: a match string, the match-style radios
	/// + regex/match-case options, over OK/Cancel. LCModel-free like the other dialog-kit views; hosted as
	/// Avalonia content inside a WinForms-owned modal Form during coexistence via
	/// <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class FilterForDialogView : UserControl
	{
		public FilterForDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
