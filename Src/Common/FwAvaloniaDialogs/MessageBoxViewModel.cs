// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using CommunityToolkit.Mvvm.Input;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the kit's reusable message/confirmation dialog (<see cref="MessageBoxView"/>), the Avalonia
	/// analog of <see cref="System.Windows.Forms.MessageBox"/>. Carries the message body, the severity icon, and
	/// the button set; exposes which buttons are visible and the localized labels (from
	/// <see cref="FwAvaloniaDialogsStrings"/>), and records the precise <see cref="FwMessageBoxResult"/> the user
	/// chose. Reuses <see cref="DialogViewModelBase"/> for the host-close contract: the affirmative buttons
	/// (OK/Yes) close accepted, the negative ones (No/Cancel) close cancelled; <see cref="Result"/> preserves the
	/// exact button for the caller, since the close signal only carries an accepted/cancelled bool.
	/// </summary>
	public partial class MessageBoxViewModel : DialogViewModelBase
	{
		/// <summary>Parameterless ctor for the XAML designer / preview only.</summary>
		public MessageBoxViewModel()
			: this("Message", FwMessageBoxButtons.Ok, FwMessageBoxIcon.None)
		{
		}

		public MessageBoxViewModel(string message, FwMessageBoxButtons buttons, FwMessageBoxIcon icon)
		{
			Message = message ?? string.Empty;
			Buttons = buttons;
			Icon = icon;
			// Default the "dismissed without a choice" result to the cancel-button semantics of the set, so a
			// closed window matches what WinForms MessageBox returns for the same button set.
			Result = DefaultDismissResult(buttons);
		}

		/// <summary>The message body shown in the dialog.</summary>
		public string Message { get; }

		/// <summary>The button set requested by the caller.</summary>
		public FwMessageBoxButtons Buttons { get; }

		/// <summary>The severity/icon requested by the caller.</summary>
		public FwMessageBoxIcon Icon { get; }

		/// <summary>The button the user clicked (or the dismiss default if the window was closed without one).</summary>
		public FwMessageBoxResult Result { get; private set; }

		// --- Button visibility (bound to IsVisible in the view) ---

		public bool ShowOk => Buttons == FwMessageBoxButtons.Ok || Buttons == FwMessageBoxButtons.OkCancel;
		public bool ShowYes => Buttons == FwMessageBoxButtons.YesNo || Buttons == FwMessageBoxButtons.YesNoCancel;
		public bool ShowNo => Buttons == FwMessageBoxButtons.YesNo || Buttons == FwMessageBoxButtons.YesNoCancel;
		public bool ShowCancel => Buttons == FwMessageBoxButtons.OkCancel || Buttons == FwMessageBoxButtons.YesNoCancel;

		// --- Localized button labels (reused OK/Cancel + new Yes/No strings) ---

		public string OkLabel => FwAvaloniaDialogsStrings.Ok;
		public string CancelLabel => FwAvaloniaDialogsStrings.Cancel;
		public string YesLabel => FwAvaloniaDialogsStrings.Yes;
		public string NoLabel => FwAvaloniaDialogsStrings.No;

		// --- Icon presentation (a themed glyph + accessible name, not a bitmap) ---

		/// <summary>True when an icon should be shown beside the message.</summary>
		public bool ShowIcon => Icon != FwMessageBoxIcon.None;

		/// <summary>A simple glyph for the severity (no icon font dependency); empty when <see cref="ShowIcon"/> is false.</summary>
		public string IconGlyph
		{
			get
			{
				switch (Icon)
				{
					case FwMessageBoxIcon.Information: return "ℹ"; // information source
					case FwMessageBoxIcon.Warning: return "⚠"; // warning sign
					case FwMessageBoxIcon.Error: return "✖"; // heavy multiplication x
					case FwMessageBoxIcon.Question: return "?";
					default: return string.Empty;
				}
			}
		}

		/// <summary>Accessible name for the icon, so the severity is announced (localized).</summary>
		public string IconAccessibleName
		{
			get
			{
				switch (Icon)
				{
					case FwMessageBoxIcon.Information: return FwAvaloniaDialogsStrings.IconInformation;
					case FwMessageBoxIcon.Warning: return FwAvaloniaDialogsStrings.IconWarning;
					case FwMessageBoxIcon.Error: return FwAvaloniaDialogsStrings.IconError;
					case FwMessageBoxIcon.Question: return FwAvaloniaDialogsStrings.IconQuestion;
					default: return string.Empty;
				}
			}
		}

		// --- Buttons. OK/Yes are affirmative (close accepted); No/Cancel are negative (close cancelled). ---

		[RelayCommand]
		private void ConfirmOk()
		{
			Result = FwMessageBoxResult.Ok;
			RequestClose(true);
		}

		[RelayCommand]
		private void Yes()
		{
			Result = FwMessageBoxResult.Yes;
			RequestClose(true);
		}

		[RelayCommand]
		private void No()
		{
			Result = FwMessageBoxResult.No;
			RequestClose(false);
		}

		[RelayCommand]
		private void CloseCancel()
		{
			Result = FwMessageBoxResult.Cancel;
			RequestClose(false);
		}

		private static FwMessageBoxResult DefaultDismissResult(FwMessageBoxButtons buttons)
		{
			switch (buttons)
			{
				case FwMessageBoxButtons.Ok: return FwMessageBoxResult.Ok;
				case FwMessageBoxButtons.OkCancel: return FwMessageBoxResult.Cancel;
				case FwMessageBoxButtons.YesNo: return FwMessageBoxResult.No;
				case FwMessageBoxButtons.YesNoCancel: return FwMessageBoxResult.Cancel;
				default: return FwMessageBoxResult.None;
			}
		}
	}
}
