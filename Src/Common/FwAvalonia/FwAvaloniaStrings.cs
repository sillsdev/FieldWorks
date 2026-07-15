// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Localized product-facing strings for the Avalonia lexical-edit surfaces (task 6.11). Runtime
	/// lookup goes through the existing LocalizationManager/XLIFF lane; the inline English defaults in
	/// this accessor are the current source of truth. Automation ids remain nonlocalized constants in
	/// code, never resource lookups.
	/// </summary>
	public static class FwAvaloniaStrings
	{
		private static string Text(string stringId, string englishText, string comment = null)
			=> FwAvaloniaLocalization.GetPalasoString(stringId, englishText, comment);

		public static string NoEntrySelected => Text("FwAvalonia.NoEntrySelected", "No entry selected");

		// §19e — structured generic-date (GenDate) qualifier editor (legacy GenDateChooserDlg).
		public static string GenDateYear => Text("FwAvalonia.GenDateYear", "Year");

		public static string GenDateEra => Text("FwAvalonia.GenDateEra", "Era");

		public static string GenDateEraAd => Text("FwAvalonia.GenDateEraAd", "AD");

		public static string GenDateEraBc => Text("FwAvalonia.GenDateEraBc", "BC");

		public static string GenDatePrecision => Text("FwAvalonia.GenDatePrecision", "Precision");

		public static string GenDatePrecisionBefore => Text("FwAvalonia.GenDatePrecisionBefore", "Before");

		public static string GenDatePrecisionOn => Text("FwAvalonia.GenDatePrecisionOn", "On");

		public static string GenDatePrecisionAbout => Text("FwAvalonia.GenDatePrecisionAbout", "About");

		public static string GenDatePrecisionAfter => Text("FwAvalonia.GenDatePrecisionAfter", "After");

		public static string EntryTypeUnsupported => Text("FwAvalonia.EntryTypeUnsupported", "This entry type is not yet supported in the new view.");

		public static string UnsupportedEditor => Text("FwAvalonia.UnsupportedEditor", "This field is not yet supported in the new view.");

		/// <summary>
		/// Tooltip on a text row the managed editor holds read-only because a plain-text edit would
		/// corrupt it: the value carries an embedded object (ORC) the runs cannot rebuild, or a run
		/// carries a TsString property the model does not round-trip (e.g. colour, offset, superscript).
		/// The lossless original is preserved for display and stays fully editable in the classic view.
		/// </summary>
		public static string EmbeddedObjectReadOnly => Text("FwAvalonia.EmbeddedObjectReadOnly", "This text contains formatting the new view cannot edit safely yet.");

		public static string Save => Text("FwAvalonia.Save", "Save");

		public static string Cancel => Text("Common.Cancel", "Cancel");

		public static string UndoEditEntry => Text("FwAvalonia.UndoEditEntry", "Undo Edit Entry");

		public static string RedoEditEntry => Text("FwAvalonia.RedoEditEntry", "Redo Edit Entry");

		/// <summary>
		/// "Undo change to {0}" — field-specific undo label for the fenced lexical-edit session when a
		/// single field's edit opened it ({0} = the field label). Falls back to <see cref="UndoEditEntry"/>
		/// for the batch/bulk path where no single field applies.
		/// </summary>
		public static string UndoChangeToFormat => Text("FwAvalonia.UndoChangeToFormat", "Undo change to {0}");

		/// <summary>"Redo change to {0}" — the redo counterpart of <see cref="UndoChangeToFormat"/>.</summary>
		public static string RedoChangeToFormat => Text("FwAvalonia.RedoChangeToFormat", "Redo change to {0}");

		public static string LexemeFormRequired => Text("FwAvalonia.LexemeFormRequired", "Lexeme Form is required.");

		/// <summary>§20.3.1 (LE-4): validation message when a list item (CmPossibility) has neither a Name nor an Abbreviation.</summary>
		public static string PossibilityNameOrAbbreviationRequired => Text("FwAvalonia.PossibilityNameOrAbbreviationRequired", "A name or abbreviation is required.");

		// avalonia-interlinear-editor — the per-line row labels down the left edge of the interlinear editor,
		// mirroring the legacy InterlinVc label pile (Morphemes / Lex. Entries / Lex. Gloss / Lex. Gram. Info.).
		public static string InterlinearMorphemesLabel => Text("FwAvalonia.InterlinearMorphemesLabel", "Morphemes");
		public static string InterlinearLexEntriesLabel => Text("FwAvalonia.InterlinearLexEntriesLabel", "Lex. Entries");
		public static string InterlinearGlossLabel => Text("FwAvalonia.InterlinearGlossLabel", "Lex. Gloss");
		public static string InterlinearGramInfoLabel => Text("FwAvalonia.InterlinearGramInfoLabel", "Lex. Gram. Info.");

		/// <summary>
		/// Warning shown when a pending lexical edit is rolled back on navigate/close because it fails
		/// validation (the edit is not silently lost — the user is told why). {0} is the validation reason(s).
		/// </summary>
		public static string EditDiscardedInvalidFormat => Text("FwAvalonia.EditDiscardedInvalid", "Your edit was discarded because it is invalid: {0}");

		/// <summary>Title of the <see cref="EditDiscardedInvalidFormat"/> warning.</summary>
		public static string EditDiscardedInvalidTitle => Text("FwAvalonia.EditDiscardedInvalidTitle", "Invalid edit discarded");

		/// <summary>
		/// Placeholder shown in a read-only text row for a voice/audio (IsVoice) writing system: the new
		/// view cannot yet play or record audio, so the row stays read-only and visible rather than a blank
		/// editable box that would corrupt the recording on edit. Editing stays available in the classic view.
		/// </summary>
		public static string AudioRecordingReadOnly => Text("FwAvalonia.AudioRecordingReadOnly", "Audio recording (read-only in the new view)");

		public static string LexicalEditRegionName => Text("FwAvalonia.LexicalEditRegionName", "Lexical edit region");

		public static string AvaloniaHostName => Text("FwAvalonia.AvaloniaHostName", "Avalonia host");

		/// <summary>Accessible name for the browse table's check-all checkbox column (3c).</summary>
		public static string SelectAllRows => Text("FwAvalonia.SelectAllRows", "Select all rows");

		/// <summary>"Configure Columns…" — the browse header context-menu entry that opens the Configure-Columns dialog (P1).</summary>
		public static string ConfigureColumnsMenu => Text("FwAvalonia.ConfigureColumnsMenu", "Configure Columns...");

		/// <summary>"Show All" — the browse filter preset that clears a column's blank-aware filter (3c).</summary>
		public static string FilterShowAll => Text("FwAvalonia.FilterShowAll", "Show All");

		/// <summary>"Blanks" — browse filter preset narrowing a column to rows whose cell is blank (3c).</summary>
		public static string FilterBlanks => Text("FwAvalonia.FilterBlanks", "Blanks");

		/// <summary>"Non-blanks" — browse filter preset narrowing a column to rows whose cell is non-blank (3c).</summary>
		public static string FilterNonBlanks => Text("FwAvalonia.FilterNonBlanks", "Non-blanks");

		public static string GhostAddPromptFormat => Text("FwAvalonia.GhostAddPrompt", "Type to add {0}");

		public static string Copy => Text("FwAvalonia.Copy", "Copy");

		/// <summary>"Remove" — reference-vector item context command (6.3).</summary>
		public static string Remove => Text("FwAvalonia.Remove", "Remove");

		/// <summary>"Add item" — reference-vector add-slot launcher name (6.3).</summary>
		public static string AddItem => Text("FwAvalonia.AddItem", "Add item");

		/// <summary>"Type to search" — the search-backed add slot's type-ahead watermark (D3).</summary>
		public static string SearchPrompt => Text("FwAvalonia.SearchPrompt", "Type to search");

		/// <summary>"Add" — confirm button of the multi-select reference-vector add picker; commits the checked set in one undoable step.</summary>
		public static string AddSelected => Text("FwAvalonia.AddSelected", "Add");

		/// <summary>"Add note" — the Chorus notes bar's add affordance (D2).</summary>
		public static string ChorusAddNote => Text("FwAvalonia.ChorusAddNote", "Add note");

		/// <summary>"Add message" — append-message watermark in a Chorus note flyout (D2).</summary>
		public static string ChorusAddMessage => Text("FwAvalonia.ChorusAddMessage", "Add message");

		/// <summary>"OK" — confirm button of the Chorus notes flyouts (D2).</summary>
		public static string ChorusOk => Text("FwAvalonia.ChorusOk", "OK");

		/// <summary>"Resolved" — the resolve toggle of a Chorus note flyout (D2).</summary>
		public static string ChorusResolved => Text("FwAvalonia.ChorusResolved", "Resolved");

		/// <summary>Accessible name of the "..." dialog-launcher button (D4).</summary>
		public static string LaunchDialog => Text("FwAvalonia.LaunchDialog", "Open dialog");

		/// <summary>Tooltip of a disabled launcher button: no host dialog service (D4).</summary>
		public static string LauncherUnavailable => Text("FwAvalonia.LauncherUnavailable", "This dialog is not available here.");

		/// <summary>"{0} settings" — accessible name of a chooser's hover-revealed settings gear.</summary>
		public static string FieldSettingsFormat => Text("FwAvalonia.FieldSettings", "{0} settings");

		/// <summary>
		/// "Edit the {0} list" — label/tooltip of a configure-gear jump derived from the row's
		/// possibility list (the legacy chooser dialog's "Edit the … list" link text).
		/// </summary>
		public static string EditListFormat => Text("FwAvalonia.EditListFormat", "Edit the {0} list");

		/// <summary>"Lexeme Form" — first-slice row label (compiled override and authored fallback).</summary>
		public static string LexemeFormLabel => Text("FwAvalonia.LexemeFormLabel", "Lexeme Form");

		/// <summary>"Morph Type" — first-slice row label (authored fallback).</summary>
		public static string MorphTypeLabel => Text("FwAvalonia.MorphTypeLabel", "Morph Type");

		/// <summary>"Gloss" — first-slice row label (authored fallback).</summary>
		public static string GlossLabel => Text("FwAvalonia.GlossLabel", "Gloss");

		/// <summary>
		/// Accessible name / tooltip of the hover-revealed "⋮" field-options button on each field row
		/// (opens the Field Visibility / Move Field / Help menu — the affordance that replaced right-click).
		/// </summary>
		public static string FieldOptionsMenu => Text("FwAvalonia.FieldOptionsMenu", "Field options");

		/// <summary>
		/// Accessible name / tooltip of the character-style picker affordance on an editable text row
		/// (Phase 3): opens the list of the project's character styles to apply to the current selection.
		/// </summary>
		public static string CharacterStyle => Text("FwAvalonia.CharacterStyle", "Character Style");

		/// <summary>
		/// The "Default/None" entry that leads the character-style picker (Phase 3): selecting it CLEARS
		/// any named character style on the current selection, reverting it to the paragraph's default.
		/// </summary>
		public static string DefaultCharacterStyle => Text("FwAvalonia.DefaultCharacterStyle", "Default");

		/// <summary>
		/// Accessible name / tooltip of the writing-system picker affordance on an editable text row
		/// (Phase 4): opens the list of the project's writing systems to retag the current selection.
		/// </summary>
		public static string WritingSystem => Text("FwAvalonia.WritingSystem", "Writing System");

		/// <summary>"List Choice" — bulk-edit bar tab setting a possibility reference across checked rows (3c).</summary>
		public static string BulkEditListChoice => Text("FwAvalonia.BulkEditListChoice", "List Choice");

		/// <summary>"Target:" — bulk-edit bar label for the target-column dropdown (3c).</summary>
		public static string BulkEditTarget => Text("FwAvalonia.BulkEditTarget", "Target:");

		/// <summary>"Preview" — bulk-edit bar button: overlay the chosen value without mutating data (3c).</summary>
		public static string BulkEditPreview => Text("FwAvalonia.BulkEditPreview", "Preview");

		/// <summary>"Apply" — bulk-edit bar button: commit the chosen value across checked rows in one step (3c).</summary>
		public static string BulkEditApply => Text("FwAvalonia.BulkEditApply", "Apply");

		/// <summary>Accessible name of the bulk-edit bar panel (3c).</summary>
		public static string BulkEditBarName => Text("FwAvalonia.BulkEditBarName", "Bulk edit bar");

		/// <summary>"Bulk Copy" — bulk-edit bar tab copying one column's text into another (Phase 2).</summary>
		public static string BulkCopyTab => Text("FwAvalonia.BulkCopyTab", "Bulk Copy");

		/// <summary>"Source:" — Bulk Copy tab label for the source-column dropdown (Phase 2).</summary>
		public static string BulkCopySource => Text("FwAvalonia.BulkCopySource", "Source:");

		/// <summary>"Append" — Bulk Copy mode: append source to a non-empty target (Phase 2).</summary>
		public static string BulkCopyAppend => Text("FwAvalonia.BulkCopyAppend", "Append");

		/// <summary>"Replace" — Bulk Copy mode: source overwrites target unconditionally (Phase 2).</summary>
		public static string BulkCopyReplace => Text("FwAvalonia.BulkCopyReplace", "Replace");

		/// <summary>"Don't overwrite" — Bulk Copy mode: fill only empty targets (Phase 2).</summary>
		public static string BulkCopyDoNothing => Text("FwAvalonia.BulkCopyDoNothing", "Don't overwrite");

		/// <summary>"Clear" — bulk-edit bar tab emptying a target text column across checked rows (Phase 3).</summary>
		public static string BulkClearTab => Text("FwAvalonia.BulkClearTab", "Clear");

		/// <summary>"Bulk Replace" — bulk-edit bar tab running a find/replace over a target column (Find/Replace P1).</summary>
		public static string BulkReplaceTab => Text("FwAvalonia.BulkReplaceTab", "Bulk Replace");

		/// <summary>"Setup…" — Bulk Replace tab button: open the Find/Replace pattern dialog (Find/Replace P1).</summary>
		public static string BulkReplaceSetup => Text("FwAvalonia.BulkReplaceSetup", "Setup...");

		/// <summary>Bulk Replace summary placeholder shown when no find pattern has been set yet (Find/Replace P1).</summary>
		public static string BulkReplaceNoPattern => Text("FwAvalonia.BulkReplaceNoPattern", "No find/replace pattern configured.");

		/// <summary>"Process" — bulk-edit bar tab running a converter over a source column into a target (Process/Transduce).</summary>
		public static string BulkTransduceTab => Text("FwAvalonia.Bulk.Transduce", "Process");

		/// <summary>"Converter:" — Process tab label for the converter-picker dropdown (Process/Transduce).</summary>
		public static string BulkTransduceConverter => Text("FwAvalonia.Bulk.TransduceConverter", "Converter:");

		/// <summary>"Setup..." — Process tab button: open the EncConverters management dialog (Process/Transduce).</summary>
		public static string BulkTransduceSetup => Text("FwAvalonia.Bulk.TransduceSetup", "Setup...");

		// Click Copy tab: the interactive bulk mode where, with the tab active, clicking a SOURCE cell in the
		// browse table copies that text into the configured TARGET column on the SAME row — per click, no
		// Preview/Apply button. Seed text mirrors the legacy BulkEditBar / XMLViewsStrings wording (the
		// click-copy tab's "Append, separated by:" / "Overwrite" radios, the word/whole-field group, and the
		// ksChooseClickTarget message) so the English fallback matches the classic bar.
		/// <summary>"Click Copy" — bulk-edit bar tab: click a source cell to copy it into a target column.</summary>
		public static string BulkClickCopyTab => Text("FwAvalonia.Bulk.ClickCopy", "Click Copy");

		/// <summary>"Copy what?" — Click Copy group label for the word-vs-whole-field mode radios.</summary>
		public static string BulkClickCopyModeLabel => Text("FwAvalonia.Bulk.ClickCopyModeLabel", "Copy what?");

		/// <summary>"Word" — Click Copy mode: copy just the clicked word from the source cell.</summary>
		public static string BulkClickCopyWord => Text("FwAvalonia.Bulk.ClickCopyWord", "Word");

		/// <summary>"Reorder (whole field)" — Click Copy mode: copy the whole source cell (reorder at the clicked word).</summary>
		public static string BulkClickCopyReorder => Text("FwAvalonia.Bulk.ClickCopyReorder", "Reorder (whole field)");

		/// <summary>"Separator:" — Click Copy label for the text inserted between an existing target and the appended source.</summary>
		public static string BulkClickCopySeparator => Text("FwAvalonia.Bulk.ClickCopySeparator", "Separator:");

		/// <summary>"Append, separated by:" — Click Copy directivity: append the copied text to a non-empty target.</summary>
		public static string BulkClickCopyAppend => Text("FwAvalonia.Bulk.ClickCopyAppend", "Append, separated by:");

		/// <summary>"Overwrite" — Click Copy directivity: the copied text overwrites the target unconditionally.</summary>
		public static string BulkClickCopyOverwrite => Text("FwAvalonia.Bulk.ClickCopyOverwrite", "Overwrite");

		/// <summary>Click Copy message shown when the user clicks a source cell before choosing a target column.</summary>
		public static string BulkClickCopyChooseTarget => Text("FwAvalonia.Bulk.ClickCopyChooseTarget", "Please select a target column for click copy");

		/// <summary>"Sort From End" — browse header context-menu toggle: suffix-oriented sort on the reversed text.</summary>
		public static string SortFromEnd => Text("FwAvalonia.Browse.SortFromEnd", "Sort From End");

		/// <summary>"Sort By Length" — browse header context-menu toggle (only when the column allows it).</summary>
		public static string SortByLength => Text("FwAvalonia.Browse.SortByLength", "Sort By Length");

		// Seed text below matches the canonical legacy wording in XMLViewsStrings (ksMoreThanOneLine,
		// ksExactlyOneLine, ksYes, ksNo, ksZero, ksGreaterThanZero, ksGreaterThanOne) so the English
		// fallback is identical to the classic browse filter combo and translation memory carries over.
		/// <summary>"More than one line" — browse filter preset for multipara columns.</summary>
		public static string FilterMoreThanOneLine => Text("FwAvalonia.Browse.FilterMoreThanOneLine", "More than one line");

		/// <summary>"Exactly one line" — browse filter preset for multipara columns.</summary>
		public static string FilterExactlyOneLine => Text("FwAvalonia.Browse.FilterExactlyOneLine", "Exactly one line");

		/// <summary>"yes" — browse filter preset (exact match) for sortType=YesNo columns.</summary>
		public static string FilterYes => Text("FwAvalonia.Browse.FilterYes", "yes");

		/// <summary>"no" — browse filter preset (exact match) for sortType=YesNo columns.</summary>
		public static string FilterNo => Text("FwAvalonia.Browse.FilterNo", "no");

		/// <summary>"0" — browse filter preset for sortType=integer columns.</summary>
		public static string FilterZero => Text("FwAvalonia.Browse.FilterZero", "0");

		/// <summary>"Greater than 0" — browse filter preset for sortType=integer columns.</summary>
		public static string FilterGreaterThanZero => Text("FwAvalonia.Browse.FilterGreaterThanZero", "Greater than 0");

		/// <summary>"Greater than 1" — browse filter preset for sortType=integer columns.</summary>
		public static string FilterGreaterThanOne => Text("FwAvalonia.Browse.FilterGreaterThanOne", "Greater than 1");

		// Seed text matches the canonical legacy wording in XMLViewsStrings (ksFilterFor_, ksExcludeX) so the
		// English fallback is identical to the classic browse FilterBar and translation memory carries over.
		/// <summary>"Filter For…" — universal browse filter entry that opens the pattern-match dialog (FilterBar's FindComboItem).</summary>
		public static string FilterFor => Text("FwAvalonia.Browse.FilterFor", "Filter for...");

		/// <summary>"Exclude {0}" — browse filter preset format for the inverse of a stringList enumerated value.</summary>
		public static string FilterExcludeFormat => Text("FwAvalonia.Browse.FilterExcludeFormat", "Exclude {0}");

		// Seed text matches the canonical legacy wording in XMLViewsStrings (ksRestrict_, ksChoose_) so the
		// English fallback is identical to the classic browse FilterBar and translation memory carries over.
		/// <summary>"Restrict Date…" — browse filter entry that opens the date-range dialog (FilterBar's RestrictDateComboItem), for date/genDate columns.</summary>
		public static string RestrictDate => Text("FwAvalonia.Browse.RestrictDate", "Restrict...");

		/// <summary>"Choose…" — browse filter entry that opens the list-choice chooser (FilterBar's ListChoiceComboItem), for bulkEdit/chooserFilter columns.</summary>
		public static string FilterChoose => Text("FwAvalonia.Browse.FilterChoose", "Choose...");

		// ----- Delete tab (destructive Delete Rows mode of the legacy Delete tab) -----
		// Seed text matches the canonical legacy wording in XMLViewsStrings (ksDeleteRows label uses
		// "{0} (Rows)"; ksDelete; ksConfirmDeleteMulti/ksConfirmDeleteMultiMsg) and BulkEditBar's dual-mode
		// "Delete what?" combo, so the English fallback is identical to the classic bulk-edit Delete tab and
		// translation memory carries over. APPEND-ONLY: new accessors at the end of the region.

		/// <summary>"Delete what?" — the Delete tab's dual-mode label (Clear Field vs Delete Rows), mirroring BulkEditBar's m_deleteWhatCombo prompt.</summary>
		public static string BulkDeleteWhatLabel => Text("FwAvalonia.Bulk.DeleteWhatLabel", "Delete what?");

		/// <summary>"Clear Field" — the non-destructive mode of the Delete tab (empty a target text column across the checked rows).</summary>
		public static string BulkDeleteModeClearField => Text("FwAvalonia.Bulk.DeleteModeClearField", "Clear Field");

		/// <summary>"Delete Rows" — the destructive mode of the Delete tab (delete the checked objects), mirroring BulkEditBar's ListClassTargetFieldItem "(Rows)" entry.</summary>
		public static string BulkDeleteModeDeleteRows => Text("FwAvalonia.Bulk.DeleteModeDeleteRows", "Delete Rows");

		/// <summary>"Delete" — the Delete-Rows Apply button caption (legacy XMLViewsStrings.ksDelete).</summary>
		public static string BulkDeleteApply => Text("FwAvalonia.Bulk.DeleteApply", "Delete");

		/// <summary>The Delete-Rows preview button caption — shows which checked rows will be deleted vs are blocked.</summary>
		public static string BulkDeletePreview => Text("FwAvalonia.Bulk.DeletePreview", "Preview");

		/// <summary>"Confirm Multiple Deletions" — the bulk-delete confirmation dialog title (legacy XMLViewsStrings.ksConfirmDeleteMulti).</summary>
		public static string BulkDeleteConfirmTitle => Text("FwAvalonia.Bulk.DeleteConfirmTitle", "Confirm Multiple Deletions");

		/// <summary>The bulk-delete confirmation message, "{0}" = the count of objects to delete (legacy XMLViewsStrings.ksConfirmDeleteMultiMsg wording).</summary>
		public static string BulkDeleteConfirmMessageFormat => Text("FwAvalonia.Bulk.DeleteConfirmMessageFormat", "You are about to delete {0} item(s). This cannot be undone if too many objects are affected.\n\nDo you wish to continue?");

		/// <summary>Per-row delete preview marker shown on a row that WILL be deleted (a deletable, checked row).</summary>
		public static string BulkDeleteWillDeleteMarker => Text("FwAvalonia.Bulk.DeleteWillDeleteMarker", "(will be deleted)");

		/// <summary>Per-row delete preview marker shown on a checked row that is BLOCKED from deletion by a guard (e.g. the only sense).</summary>
		public static string BulkDeleteBlockedMarker => Text("FwAvalonia.Bulk.DeleteBlockedMarker", "(cannot be deleted)");

		// Seed text matches the canonical legacy wording in XMLViewsStrings (ksSpellingErrors) so the English
		// fallback is identical to the classic browse FilterBar and translation memory carries over. APPEND-ONLY.
		/// <summary>"Spelling Errors" — browse filter entry that keeps only rows whose cell contains a spelling error (FilterBar's BadSpellingMatcher), offered only on a string column whose writing system has a spelling dictionary.</summary>
		public static string FilterSpellingErrors => Text("FwAvalonia.Browse.FilterSpellingErrors", "Spelling Errors");

		// ----- Part-of-Speech chooser (MSA-port Stage 1: FwPosChooser) -----
		// Seed text mirrors the legacy WinForms POS picker (POSPopupTreeManager / PopupTreeManager): the
		// empty node shows "<Not sure>" by default (or "<Any>" when the host opts in via the empty-label
		// override, as MSAGroupBox does), and the inline create affordance is the tree's "More..." item,
		// reworded to the clearer "Create a new Part of Speech..." for the new view. APPEND-ONLY.

		/// <summary>"&lt;Not sure&gt;" — the default empty / unspecified Part-of-Speech entry (legacy PopupTreeManager "&lt;Not sure&gt;").</summary>
		public static string PosNotSure => Text("FwAvalonia.Pos.NotSure", "<Not sure>");

		/// <summary>"&lt;Any&gt;" — the unspecified Part-of-Speech entry when the host treats unspecified as "any" (legacy MSAGroupBox NotSureIsAny).</summary>
		public static string PosAny => Text("FwAvalonia.Pos.Any", "<Any>");

		/// <summary>"Create a new Part of Speech..." — the inline create affordance at the bottom of the POS tree (legacy "More..." item that launched MasterCategoryListDlg).</summary>
		public static string PosCreateNew => Text("FwAvalonia.Pos.CreateNew", "Create a new Part of Speech...");

		/// <summary>Accessible name of the collapsed Part-of-Speech chooser dropdown.</summary>
		public static string PosChooserName => Text("FwAvalonia.Pos.ChooserName", "Part of Speech");

		// ----- Rule-formula editor cell context menu (avalonia-rule-formula-editor, task 2.2) -----
		// Seed text mirrors the legacy RegRuleFormulaControl cell operations. APPEND-ONLY.

		/// <summary>"Delete" — rule-formula cell context-menu item that removes the cell from its group.</summary>
		public static string RuleCellDelete => Text("FwAvalonia.RuleCell.Delete", "Delete");

		/// <summary>"Insert before" — rule-formula cell context-menu item that opens the chooser to insert a new cell before this one.</summary>
		public static string RuleCellInsertBefore => Text("FwAvalonia.RuleCell.InsertBefore", "Insert before");

		// ----- Editable structured text (StText multi-paragraph fields, §19a) -----
		// Seed text mirrors the legacy StTextSlice rich editor's paragraph operations. APPEND-ONLY.

		/// <summary>Accessible name / tooltip of the per-paragraph "add paragraph" affordance in a structured-text field (§19a).</summary>
		public static string AddParagraph => Text("FwAvalonia.StText.AddParagraph", "Add paragraph");

		/// <summary>Accessible name / tooltip of the per-paragraph "delete paragraph" affordance in a structured-text field (§19a).</summary>
		public static string DeleteParagraph => Text("FwAvalonia.StText.DeleteParagraph", "Delete paragraph");

		/// <summary>
		/// Accessible name / tooltip of the per-paragraph style picker in a structured-text field (§19a):
		/// opens the list of the project's paragraph styles to apply to this paragraph.
		/// </summary>
		public static string ParagraphStyle => Text("FwAvalonia.StText.ParagraphStyle", "Paragraph Style");

		/// <summary>
		/// The "Default" entry that leads the paragraph-style picker (§19a): selecting it CLEARS any named
		/// paragraph style on the paragraph, reverting it to the default.
		/// </summary>
		public static string DefaultParagraphStyle => Text("FwAvalonia.StText.DefaultParagraphStyle", "Default");

		// ----- Rich-text DEPTH: external links + embedded objects (ORC), §19c. APPEND-ONLY. -----

		/// <summary>
		/// Accessible name / tooltip of the external-link affordance on an editable text row (§19c): opens
		/// a small URL prompt that inserts a hyperlink over the selection, or edits an existing link's URL.
		/// </summary>
		public static string Link => Text("FwAvalonia.Link", "Link");

		/// <summary>Watermark / accessible name of the URL entry in the link prompt flyout (§19c).</summary>
		public static string LinkUrlPrompt => Text("FwAvalonia.LinkUrlPrompt", "Address (URL)");

		/// <summary>The confirm button of the link prompt flyout: insert / update the hyperlink (§19c).</summary>
		public static string LinkApply => Text("FwAvalonia.LinkApply", "Apply");

		/// <summary>
		/// Accessible name / tooltip of the delete-embedded-object affordance (§19c): removes the embedded
		/// object (link, picture, footnote, …) under the selection. Any ORC kind is deletable here even
		/// when its insert/edit lane lives elsewhere (picture insert/ORC DONE in §19d via the picture
		/// insert flow; footnote insert deferred).
		/// </summary>
		public static string DeleteEmbeddedObject => Text("FwAvalonia.DeleteEmbeddedObject", "Delete embedded object");

		// ----- Pictures (CmPicture editable parity, §19d). APPEND-ONLY. Seed text mirrors the legacy
		// picture insert/properties/delete affordances (DTMenuHandler.OnInsertPicture / PictureSlice). -----

		/// <summary>Insert-a-picture affordance on an empty picture field / "insert another" on a picture row (legacy Insert Picture).</summary>
		public static string PictureInsert => Text("FwAvalonia.Picture.Insert", "Add a picture...");

		/// <summary>Edit-picture-properties affordance (caption / license / creator + replace file) — legacy PicturePropertiesDialog.</summary>
		public static string PictureProperties => Text("FwAvalonia.Picture.Properties", "Picture properties...");

		/// <summary>Delete-picture affordance on a picture row.</summary>
		public static string PictureDelete => Text("FwAvalonia.Picture.Delete", "Delete picture");

		// ----- Audio (voice writing systems: play + record, §19d). APPEND-ONLY. Seed text mirrors the
		// legacy voice-WS audio control (LabeledMultiStringView ShortSoundFieldControl play/record/delete). -----

		/// <summary>Play affordance for an existing audio recording on a voice-WS row (legacy ShortSoundFieldControl play).</summary>
		public static string AudioPlay => Text("FwAvalonia.Audio.Play", "Play");

		/// <summary>Record affordance for a voice-WS row (legacy ShortSoundFieldControl record).</summary>
		public static string AudioRecord => Text("FwAvalonia.Audio.Record", "Record");

		/// <summary>Clear-the-recording affordance for a voice-WS row (legacy ShortSoundFieldControl delete).</summary>
		public static string AudioClear => Text("FwAvalonia.Audio.Clear", "Clear");

		/// <summary>Tooltip on the record affordance when recording is unavailable on this platform (cross-platform record deferred).</summary>
		public static string AudioRecordUnavailable => Text("FwAvalonia.Audio.RecordUnavailable", "Recording is available on Windows in the new view; use the classic view on this platform.");

		/// <summary>Placeholder shown for a voice-WS row that has no recording yet.</summary>
		public static string AudioNoRecording => Text("FwAvalonia.Audio.NoRecording", "(no recording)");

		// ----- Browse remainders (§19f). APPEND-ONLY. -----

		/// <summary>Menu entry to copy the current browse cell's text to the clipboard (§19f.4, Ctrl+C parity).</summary>
		public static string CellCopy => Text("FwAvalonia.Browse.CellCopy", "Copy");

		/// <summary>Menu entry to paste the clipboard text into an editable browse cell (§19f.4, Ctrl+V parity).</summary>
		public static string CellPaste => Text("FwAvalonia.Browse.CellPaste", "Paste");

		/// <summary>The prompt shown in the empty Rapid-Data-Entry "new row" at the bottom of the browse (§19f.7).</summary>
		public static string RdeNewRowPrompt => Text("FwAvalonia.Browse.RdeNewRowPrompt", "Type here to add a new entry...");

		/// <summary>Menu/affordance label to export the visible browse columns and rows as CSV (§19f.9).</summary>
		public static string ExportCsv => Text("FwAvalonia.Browse.ExportCsv", "Export visible rows (CSV)...");

		// ----- StText (§19a) accessibility names. APPEND-ONLY. -----

		/// <summary>Screen-reader name for one paragraph editor in a multi-paragraph StText field
		/// (§19a). {0} = the field label, {1} = the 1-based paragraph number.</summary>
		public static string StructuredTextParagraphName(string fieldLabel, int paragraphNumber)
			=> string.Format(Text("FwAvalonia.StructuredText.ParagraphName", "{0} paragraph {1}"), fieldLabel, paragraphNumber);

		// ----- Per-tool feature catalog (LexicalEditFeatureCatalog): display metadata for the
		// "Manage Individual Features" dialog's checkbox list. APPEND-ONLY. -----

		/// <summary>Group heading for the entry-editing tool surfaces in the feature-manager dialog.</summary>
		public static string FeatureGroupLexicalEntryDialogs => Text("FwAvalonia.FeatureGroup.LexicalEntryDialogs", "Dialogs (lexical entry)");

		/// <summary>Group heading for the non-entry record-type tool surfaces in the feature-manager dialog.</summary>
		public static string FeatureGroupOtherRecordTypes => Text("FwAvalonia.FeatureGroup.OtherRecordTypes", "Other record types");

		public static string FeatureLexiconEditName => Text("FwAvalonia.Feature.LexiconEditName", "Lexicon Edit");
		public static string FeatureLexiconEditDescription => Text("FwAvalonia.Feature.LexiconEditDescription", "The main entry-editing surface.");

		public static string FeatureLexiconEditPopupName => Text("FwAvalonia.Feature.LexiconEditPopupName", "Lexicon Edit (popup)");
		public static string FeatureLexiconEditPopupDescription => Text("FwAvalonia.Feature.LexiconEditPopupDescription", "The popup variant of entry editing.");

		public static string FeatureNotebookEditName => Text("FwAvalonia.Feature.NotebookEditName", "Notebook");
		public static string FeatureNotebookEditDescription => Text("FwAvalonia.Feature.NotebookEditDescription", "Notebook (RnGenericRec) entries.");

		public static string FeaturePosEditName => Text("FwAvalonia.Feature.PosEditName", "Grammar / Part of Speech");
		public static string FeaturePosEditDescription => Text("FwAvalonia.Feature.PosEditDescription", "The Part of Speech editor.");
	}
}
