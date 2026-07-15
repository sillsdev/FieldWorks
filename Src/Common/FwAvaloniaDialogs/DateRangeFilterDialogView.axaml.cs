// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The browse "Restrict Date…" date-range dialog body — a XAML-authored UserControl bound to
	/// <see cref="DateRangeFilterDialogViewModel"/> with compiled bindings: a relation picker, a start date
	/// (and an end date for the "between" relation), over OK/Cancel. LCModel-free like the other dialog-kit
	/// views; hosted as Avalonia content inside a WinForms-owned modal Form during coexistence via
	/// <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class DateRangeFilterDialogView : UserControl
	{
		public DateRangeFilterDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
