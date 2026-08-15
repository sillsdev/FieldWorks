// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The reusable Insert Entry dialog body: a XAML-authored UserControl bound to
	/// <see cref="InsertEntryDlgViewModel"/> with compiled bindings for the prompt + Create/Cancel/Help, plus
	/// the owned lexeme-form field, morph-type picker, and gloss field hosted as code-behind children (each is a
	/// native composite, not an MVVM-bindable control, so it cannot be set through a compiled
	/// binding -- the same
	/// pattern ChooserDialogView uses). Hosted as Avalonia content inside a WinForms-owned modal Form during
	/// coexistence via <c>AvaloniaDialogHost.ShowModal</c>.
	/// </summary>
	public partial class InsertEntryDlgView : UserControl
	{
		private Border _lexemeFormHost;
		private Border _morphTypeHost;
		private Border _complexFormTypeHost;
		private Border _glossHost;
		private Border _msaHost;

		public InsertEntryDlgView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();
			_lexemeFormHost = this.FindControl<Border>("PART_LexemeFormHost");
			_morphTypeHost = this.FindControl<Border>("PART_MorphTypeHost");
			_complexFormTypeHost = this.FindControl<Border>("PART_ComplexFormTypeHost");
			_glossHost = this.FindControl<Border>("PART_GlossHost");
			_msaHost = this.FindControl<Border>("PART_MsaSection");
			DataContextChanged += (s, e) => InjectControls();
			InjectControls();
			// Initial focus (legacy SetInitialFocus :514): the lexeme form normally, the gloss when the dialog was
			// seeded from an analysis-WS initial string. Best-effort once the visual tree is realized; any failure is
			// swallowed (focus must never take down the dialog).
			AttachedToVisualTree += (s, e) => Dispatcher.UIThread.Post(SetInitialFocus);
		}

		private void SetInitialFocus()
		{
			try
			{
				var vm = DataContext as InsertEntryDlgViewModel;
				var target = vm != null && vm.InitialFocus == InsertEntryInitialFocus.Gloss
					? vm.GlossField
					: vm?.LexemeFormField;
				target?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault()?.Focus();
			}
			catch
			{
				// Focus is cosmetic; never let it crash the dialog.
			}
		}

		/// <summary>
		/// Inserts the view-model's owned controls (lexeme-form field, morph-type picker, gloss field) into their
		/// host borders. The controls are created and driven by the view-model; the view only mounts them.
		/// </summary>
		private void InjectControls()
		{
			var vm = DataContext as InsertEntryDlgViewModel;
			if (_lexemeFormHost != null)
				_lexemeFormHost.Child = vm?.LexemeFormField;
			if (_morphTypeHost != null)
				_morphTypeHost.Child = vm?.MorphTypePicker;
			if (_complexFormTypeHost != null)
				_complexFormTypeHost.Child = vm?.ComplexFormTypePicker;
			if (_glossHost != null)
				_glossHost.Child = vm?.GlossField;
			if (_msaHost != null)
				_msaHost.Child = vm?.MsaGroupBox;
		}
	}
}
