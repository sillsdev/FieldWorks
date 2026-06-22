// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using CommunityToolkit.Mvvm.ComponentModel;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the small "Reference Set Details" dialog (Phase-1 §19g) — the LCModel-free collector
	/// behind <c>LexReferenceMultiSlice.EditReferenceDetails</c>. The Avalonia replacement for the WinForms
	/// <c>LexReferenceDetailsDlg</c>: the user edits a lexical reference's name (its display label / "type"
	/// for this set) and an optional comment/note, over OK/Cancel. The LCModel-aware launcher seeds the
	/// fields from the reference's analysis-default-WS alternatives and, on OK, writes both back in ONE
	/// undoable step.
	///
	/// OK is intentionally NOT gated — parity with the legacy dialog, where a reference set may legitimately
	/// carry an empty name or note (the slice display falls back to the reference type's own name). The
	/// trimmed values the launcher reads are exposed as <see cref="ReferenceName"/> / <see cref="ReferenceComment"/>.
	/// </summary>
	public partial class LexReferenceDetailsDialogViewModel : DialogViewModelBase
	{
		[ObservableProperty] private string _referenceName = string.Empty;
		[ObservableProperty] private string _referenceComment = string.Empty;

		public LexReferenceDetailsDialogViewModel()
			: this(string.Empty, string.Empty)
		{
		}

		public LexReferenceDetailsDialogViewModel(string referenceName, string referenceComment)
		{
			ReferenceName = referenceName ?? string.Empty;
			ReferenceComment = referenceComment ?? string.Empty;
		}

		/// <summary>The label for the name field (localized).</summary>
		public string NameLabel => FwAvaloniaDialogsStrings.LexReferenceDetailsNameLabel;

		/// <summary>The label for the comment/note field (localized).</summary>
		public string CommentLabel => FwAvaloniaDialogsStrings.LexReferenceDetailsCommentLabel;

		/// <summary>The explanatory note shown above the fields (localized).</summary>
		public string Explanation => FwAvaloniaDialogsStrings.LexReferenceDetailsExplanation;

		/// <summary>The name the user entered (read by the launcher on OK). Null-safe.</summary>
		public string ChosenName => ReferenceName ?? string.Empty;

		/// <summary>The comment/note the user entered (read by the launcher on OK). Null-safe; may be empty.</summary>
		public string ChosenComment => ReferenceComment ?? string.Empty;
	}
}
