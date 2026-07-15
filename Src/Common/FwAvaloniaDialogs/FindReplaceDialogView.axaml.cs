// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The Find/Replace pattern-setup dialog body (Find/Replace Phase 1): a XAML-authored UserControl bound to
	/// <see cref="FindReplaceDialogViewModel"/> with compiled bindings — find/replace text boxes, the legacy
	/// match-option checkboxes, over OK/Cancel. A spec-only modal (OK snapshots the FindReplacePattern); no
	/// owned controls, so it is pure compiled binding like OptionsDialogView. Hosted as Avalonia content inside
	/// a WinForms-owned modal Form during coexistence via <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class FindReplaceDialogView : UserControl
	{
		public FindReplaceDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
		}
	}
}
