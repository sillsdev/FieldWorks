// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// An LCModel-free snapshot of a Find/Replace pattern (Find/Replace Phase 1): the find text, the
	/// replacement text, and the legacy match-option flags. It is the result the Find/Replace pattern-setup
	/// dialog (<see cref="FindReplaceDialogViewModel"/>) hands back on OK, and the spec the bulk-replace
	/// producer (<c>ClerkBrowseRowSource.PreviewBulkReplace</c>/<c>ApplyBulkReplace</c>) applies over a
	/// column's checked-row cell strings. A plain value object — no behavior — so it crosses the FwAvalonia /
	/// FwAvaloniaDialogs / xWorks layers without dragging the dialog or the COM <c>IVwPattern</c> with it.
	///
	/// P1 SCOPE: the producer applies this pattern over single-WS plain-text cells in managed code
	/// (System.Text.RegularExpressions when <see cref="UseRegularExpressions"/>, else a literal
	/// case/whole-word-aware replace). <see cref="MatchDiacritics"/> and <see cref="MatchWritingSystem"/>
	/// are CARRIED but are P1 no-ops (a faithful diacritic/WS-collation match needs the full
	/// <c>IVwPattern</c> + TsString round-trip, deferred to P2); <see cref="UseRegularExpressions"/> is
	/// mutually exclusive with the literal-only options (the dialog clears them when regex is on).
	/// </summary>
	public sealed class FindReplacePattern
	{
		/// <summary>The text (or regex pattern, when <see cref="UseRegularExpressions"/>) to find.</summary>
		public string FindText { get; set; } = string.Empty;

		/// <summary>The text (or regex replacement) to substitute for each match.</summary>
		public string ReplaceText { get; set; } = string.Empty;

		/// <summary>Match case-sensitively (literal mode only; ignored under <see cref="UseRegularExpressions"/>).</summary>
		public bool MatchCase { get; set; }

		/// <summary>
		/// Match diacritics exactly (legacy <c>MatchDiacritics</c>). P1 NO-OP: carried for parity/persistence
		/// but not honored by the managed literal/regex replace — diacritic-insensitive collation needs the
		/// P2 <c>IVwPattern</c> path. The dialog grays this so the user is not misled.
		/// </summary>
		public bool MatchDiacritics { get; set; }

		/// <summary>Match whole words only (literal mode only; ignored under <see cref="UseRegularExpressions"/>).</summary>
		public bool MatchWholeWord { get; set; }

		/// <summary>
		/// Match the writing system (legacy <c>MatchOldWritingSystem</c>). P1 NO-OP: the bulk-replace cells are
		/// single-WS plain text, so there is nothing to constrain; carried for parity/P2. The dialog grays it.
		/// </summary>
		public bool MatchWritingSystem { get; set; }

		/// <summary>
		/// Treat <see cref="FindText"/> as a .NET regular expression. When set the literal-only options
		/// (<see cref="MatchCase"/>/<see cref="MatchWholeWord"/>) do not apply — case sensitivity etc. are
		/// expressed inside the pattern — so the dialog disables and clears them.
		/// </summary>
		public bool UseRegularExpressions { get; set; }

		/// <summary>A field-by-field copy, so a caller can stage edits without mutating the shared snapshot.</summary>
		public FindReplacePattern Clone() => new FindReplacePattern
		{
			FindText = FindText,
			ReplaceText = ReplaceText,
			MatchCase = MatchCase,
			MatchDiacritics = MatchDiacritics,
			MatchWholeWord = MatchWholeWord,
			MatchWritingSystem = MatchWritingSystem,
			UseRegularExpressions = UseRegularExpressions
		};
	}
}
