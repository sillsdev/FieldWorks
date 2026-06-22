// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia.Controls;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The reusable Insert Entry dialog body (Phase 1): a XAML-authored UserControl bound to
	/// <see cref="InsertEntryDialogViewModel"/> with compiled bindings for the prompt + Create/Cancel/Help, plus
	/// the owned lexeme-form field, morph-type picker, and gloss field hosted as code-behind children (each is a
	/// native composite, not an MVVM-bindable control, so it cannot be set through a compiled binding — the same
	/// pattern ChooserDialogView uses). Hosted as Avalonia content inside a WinForms-owned modal Form during
	/// coexistence via <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class InsertEntryDialogView : UserControl
	{
		private Border _lexemeFormHost;
		private Border _morphTypeHost;
		private Border _glossHost;

		public InsertEntryDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
			_lexemeFormHost = this.FindControl<Border>("PART_LexemeFormHost");
			_morphTypeHost = this.FindControl<Border>("PART_MorphTypeHost");
			_glossHost = this.FindControl<Border>("PART_GlossHost");
			DataContextChanged += (s, e) => InjectControls();
			InjectControls();
		}

		/// <summary>
		/// Inserts the view-model's owned controls (lexeme-form field, morph-type picker, gloss field) into their
		/// host borders. The controls are created and driven by the view-model; the view only mounts them.
		/// </summary>
		private void InjectControls()
		{
			var vm = DataContext as InsertEntryDialogViewModel;
			if (_lexemeFormHost != null)
				_lexemeFormHost.Child = vm?.LexemeFormField;
			if (_morphTypeHost != null)
				_morphTypeHost.Child = vm?.MorphTypePicker;
			if (_glossHost != null)
				_glossHost.Child = vm?.GlossField;
		}
	}
}
