// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Localized product-facing strings for the Avalonia lexical-edit surfaces (task 6.11). Strings
	/// resolve through ResourceManager over FwAvaloniaStrings.resx — the neutral resx is the English
	/// source of truth and translations ship as satellite assemblies (the FieldWorks .resx
	/// localization strategy). Automation ids remain nonlocalized constants in code, never resource
	/// lookups.
	/// </summary>
	public static class FwAvaloniaStrings
	{
		private static readonly System.Resources.ResourceManager Resources =
			new System.Resources.ResourceManager("FwAvalonia.FwAvaloniaStrings", typeof(FwAvaloniaStrings).Assembly);

		// Falls back to the id so a missing resx entry is visible in the UI instead of blank
		// (AvaloniaLocalizationTests pins every accessor against the neutral resx).
		private static string Text(string stringId) => Resources.GetString(stringId) ?? stringId;

		public static string NoEntrySelected => Text("FwAvalonia.NoEntrySelected");

		// §19e — structured generic-date (GenDate) qualifier editor (legacy GenDateChooserDlg).
		public static string GenDateYear => Text("FwAvalonia.GenDateYear");

		public static string GenDateEra => Text("FwAvalonia.GenDateEra");

		public static string GenDateEraAd => Text("FwAvalonia.GenDateEraAd");

		public static string GenDateEraBc => Text("FwAvalonia.GenDateEraBc");

		public static string GenDatePrecision => Text("FwAvalonia.GenDatePrecision");

		public static string GenDatePrecisionBefore => Text("FwAvalonia.GenDatePrecisionBefore");

		public static string GenDatePrecisionOn => Text("FwAvalonia.GenDatePrecisionOn");

		public static string GenDatePrecisionAbout => Text("FwAvalonia.GenDatePrecisionAbout");

		public static string GenDatePrecisionAfter => Text("FwAvalonia.GenDatePrecisionAfter");

		public static string EntryTypeUnsupported => Text("FwAvalonia.EntryTypeUnsupported");

		public static string UnsupportedEditor => Text("FwAvalonia.UnsupportedEditor");

		/// <summary>
		/// Tooltip on a text row the managed editor holds read-only because a plain-text edit would
		/// corrupt it: the value carries an embedded object (ORC) the runs cannot rebuild, or a run
		/// carries a TsString property the model does not round-trip (e.g. colour, offset, superscript).
		/// The lossless original is preserved for display and stays fully editable in the classic view.
		/// </summary>
		public static string EmbeddedObjectReadOnly => Text("FwAvalonia.EmbeddedObjectReadOnly");

		public static string Save => Text("FwAvalonia.Save");

		public static string Cancel => Text("FwAvalonia.Cancel");

		public static string UndoEditEntry => Text("FwAvalonia.UndoEditEntry");

		public static string RedoEditEntry => Text("FwAvalonia.RedoEditEntry");

		/// <summary>
		/// "Undo change to {0}" — field-specific undo label for the fenced lexical-edit session when a
		/// single field's edit opened it ({0} = the field label). Falls back to <see cref="UndoEditEntry"/>
		/// for the batch/bulk path where no single field applies.
		/// </summary>
		public static string UndoChangeToFormat => Text("FwAvalonia.UndoChangeToFormat");

		/// <summary>"Redo change to {0}" — the redo counterpart of <see cref="UndoChangeToFormat"/>.</summary>
		public static string RedoChangeToFormat => Text("FwAvalonia.RedoChangeToFormat");

		public static string LexemeFormRequired => Text("FwAvalonia.LexemeFormRequired");

		/// <summary>§20.3.1 (LE-4): validation message when a list item (CmPossibility) has neither a Name nor an Abbreviation.</summary>
		public static string PossibilityNameOrAbbreviationRequired => Text("FwAvalonia.PossibilityNameOrAbbreviationRequired");

		// avalonia-interlinear-editor — the per-line row labels down the left edge of the interlinear editor,
		// mirroring the legacy InterlinVc label pile (Morphemes / Lex. Entries / Lex. Gloss / Lex. Gram. Info.).
		public static string InterlinearMorphemesLabel => Text("FwAvalonia.InterlinearMorphemesLabel");
		public static string InterlinearLexEntriesLabel => Text("FwAvalonia.InterlinearLexEntriesLabel");
		public static string InterlinearGlossLabel => Text("FwAvalonia.InterlinearGlossLabel");
		public static string InterlinearGramInfoLabel => Text("FwAvalonia.InterlinearGramInfoLabel");

		/// <summary>
		/// Warning shown when a pending lexical edit is rolled back on navigate/close because it fails
		/// validation (the edit is not silently lost — the user is told why). {0} is the validation reason(s).
		/// </summary>
		public static string EditDiscardedInvalidFormat => Text("FwAvalonia.EditDiscardedInvalid");

		/// <summary>Title of the <see cref="EditDiscardedInvalidFormat"/> warning.</summary>
		public static string EditDiscardedInvalidTitle => Text("FwAvalonia.EditDiscardedInvalidTitle");

		/// <summary>
		/// Placeholder shown in a read-only text row for a voice/audio (IsVoice) writing system: the new
		/// view cannot yet play or record audio, so the row stays read-only and visible rather than a blank
		/// editable box that would corrupt the recording on edit. Editing stays available in the classic view.
		/// </summary>
		public static string AudioRecordingReadOnly => Text("FwAvalonia.AudioRecordingReadOnly");

		public static string LexicalEditRegionName => Text("FwAvalonia.LexicalEditRegionName");

		public static string AvaloniaHostName => Text("FwAvalonia.AvaloniaHostName");

		/// <summary>Accessible name for the browse table's check-all checkbox column (3c).</summary>
		public static string SelectAllRows => Text("FwAvalonia.SelectAllRows");

		/// <summary>"Configure Columns…" — the browse header context-menu entry that opens the Configure-Columns dialog (P1).</summary>
		public static string ConfigureColumnsMenu => Text("FwAvalonia.ConfigureColumnsMenu");

		/// <summary>"Show All" — the browse filter preset that clears a column's blank-aware filter (3c).</summary>
		public static string FilterShowAll => Text("FwAvalonia.FilterShowAll");

		/// <summary>"Blanks" — browse filter preset narrowing a column to rows whose cell is blank (3c).</summary>
		public static string FilterBlanks => Text("FwAvalonia.FilterBlanks");

		/// <summary>"Non-blanks" — browse filter preset narrowing a column to rows whose cell is non-blank (3c).</summary>
		public static string FilterNonBlanks => Text("FwAvalonia.FilterNonBlanks");

		public static string GhostAddPromptFormat => Text("FwAvalonia.GhostAddPrompt");

		public static string Copy => Text("FwAvalonia.Copy");

		/// <summary>"Remove" — reference-vector item context command (6.3).</summary>
		public static string Remove => Text("FwAvalonia.Remove");

		/// <summary>"Add item" — reference-vector add-slot launcher name (6.3).</summary>
		public static string AddItem => Text("FwAvalonia.AddItem");

		/// <summary>"Type to search" — the search-backed add slot's type-ahead watermark (D3).</summary>
		public static string SearchPrompt => Text("FwAvalonia.SearchPrompt");

		/// <summary>"Add" — confirm button of the multi-select reference-vector add picker; commits the checked set in one undoable step.</summary>
		public static string AddSelected => Text("FwAvalonia.AddSelected");

		/// <summary>"Add note" — the Chorus notes bar's add affordance (D2).</summary>
		public static string ChorusAddNote => Text("FwAvalonia.ChorusAddNote");

		/// <summary>"Add message" — append-message watermark in a Chorus note flyout (D2).</summary>
		public static string ChorusAddMessage => Text("FwAvalonia.ChorusAddMessage");

		/// <summary>"OK" — confirm button of the Chorus notes flyouts (D2).</summary>
		public static string ChorusOk => Text("FwAvalonia.ChorusOk");

		/// <summary>"Resolved" — the resolve toggle of a Chorus note flyout (D2).</summary>
		public static string ChorusResolved => Text("FwAvalonia.ChorusResolved");

		/// <summary>Accessible name of the "..." dialog-launcher button (D4).</summary>
		public static string LaunchDialog => Text("FwAvalonia.LaunchDialog");

		/// <summary>Tooltip of a disabled launcher button: no host dialog service (D4).</summary>
		public static string LauncherUnavailable => Text("FwAvalonia.LauncherUnavailable");

		/// <summary>"{0} settings" — accessible name of a chooser's hover-revealed settings gear.</summary>
		public static string FieldSettingsFormat => Text("FwAvalonia.FieldSettings");

		/// <summary>
		/// "Edit the {0} list" — label/tooltip of a configure-gear jump derived from the row's
		/// possibility list (the legacy chooser dialog's "Edit the … list" link text).
		/// </summary>
		public static string EditListFormat => Text("FwAvalonia.EditListFormat");

		/// <summary>"Lexeme Form" — first-slice row label (compiled override and authored fallback).</summary>
		public static string LexemeFormLabel => Text("FwAvalonia.LexemeFormLabel");

		/// <summary>"Morph Type" — first-slice row label (authored fallback).</summary>
		public static string MorphTypeLabel => Text("FwAvalonia.MorphTypeLabel");

		/// <summary>"Gloss" — first-slice row label (authored fallback).</summary>
		public static string GlossLabel => Text("FwAvalonia.GlossLabel");

		/// <summary>
		/// Accessible name / tooltip of the hover-revealed "⋮" field-options button on each field row
		/// (opens the Field Visibility / Move Field / Help menu — the affordance that replaced right-click).
		/// </summary>
		public static string FieldOptionsMenu => Text("FwAvalonia.FieldOptionsMenu");

		/// <summary>
		/// Accessible name / tooltip of the character-style picker affordance on an editable text row
		/// (Phase 3): opens the list of the project's character styles to apply to the current selection.
		/// </summary>
		public static string CharacterStyle => Text("FwAvalonia.CharacterStyle");

		/// <summary>
		/// The "Default/None" entry that leads the character-style picker (Phase 3): selecting it CLEARS
		/// any named character style on the current selection, reverting it to the paragraph's default.
		/// </summary>
		public static string DefaultCharacterStyle => Text("FwAvalonia.DefaultCharacterStyle");

		/// <summary>
		/// Accessible name / tooltip of the writing-system picker affordance on an editable text row
		/// (Phase 4): opens the list of the project's writing systems to retag the current selection.
		/// </summary>
		public static string WritingSystem => Text("FwAvalonia.WritingSystem");

		/// <summary>"List Choice" — bulk-edit bar tab setting a possibility reference across checked rows (3c).</summary>
		public static string BulkEditListChoice => Text("FwAvalonia.BulkEditListChoice");

		/// <summary>"Target:" — bulk-edit bar label for the target-column dropdown (3c).</summary>
		public static string BulkEditTarget => Text("FwAvalonia.BulkEditTarget");

		/// <summary>"Preview" — bulk-edit bar button: overlay the chosen value without mutating data (3c).</summary>
		public static string BulkEditPreview => Text("FwAvalonia.BulkEditPreview");

		/// <summary>"Apply" — bulk-edit bar button: commit the chosen value across checked rows in one step (3c).</summary>
		public static string BulkEditApply => Text("FwAvalonia.BulkEditApply");

		/// <summary>Accessible name of the bulk-edit bar panel (3c).</summary>
		public static string BulkEditBarName => Text("FwAvalonia.BulkEditBarName");

		/// <summary>"Bulk Copy" — bulk-edit bar tab copying one column's text into another (Phase 2).</summary>
		public static string BulkCopyTab => Text("FwAvalonia.BulkCopyTab");

		/// <summary>"Source:" — Bulk Copy tab label for the source-column dropdown (Phase 2).</summary>
		public static string BulkCopySource => Text("FwAvalonia.BulkCopySource");

		/// <summary>"Append" — Bulk Copy mode: append source to a non-empty target (Phase 2).</summary>
		public static string BulkCopyAppend => Text("FwAvalonia.BulkCopyAppend");

		/// <summary>"Replace" — Bulk Copy mode: source overwrites target unconditionally (Phase 2).</summary>
		public static string BulkCopyReplace => Text("FwAvalonia.BulkCopyReplace");

		/// <summary>"Don't overwrite" — Bulk Copy mode: fill only empty targets (Phase 2).</summary>
		public static string BulkCopyDoNothing => Text("FwAvalonia.BulkCopyDoNothing");

		/// <summary>"Clear" — bulk-edit bar tab emptying a target text column across checked rows (Phase 3).</summary>
		public static string BulkClearTab => Text("FwAvalonia.BulkClearTab");

		/// <summary>"Bulk Replace" — bulk-edit bar tab running a find/replace over a target column (Find/Replace P1).</summary>
		public static string BulkReplaceTab => Text("FwAvalonia.BulkReplaceTab");

		/// <summary>"Setup…" — Bulk Replace tab button: open the Find/Replace pattern dialog (Find/Replace P1).</summary>
		public static string BulkReplaceSetup => Text("FwAvalonia.BulkReplaceSetup");

		/// <summary>Bulk Replace summary placeholder shown when no find pattern has been set yet (Find/Replace P1).</summary>
		public static string BulkReplaceNoPattern => Text("FwAvalonia.BulkReplaceNoPattern");

		/// <summary>"Process" — bulk-edit bar tab running a converter over a source column into a target (Process/Transduce).</summary>
		public static string BulkTransduceTab => Text("FwAvalonia.Bulk.Transduce");

		/// <summary>"Converter:" — Process tab label for the converter-picker dropdown (Process/Transduce).</summary>
		public static string BulkTransduceConverter => Text("FwAvalonia.Bulk.TransduceConverter");

		/// <summary>"Setup..." — Process tab button: open the EncConverters management dialog (Process/Transduce).</summary>
		public static string BulkTransduceSetup => Text("FwAvalonia.Bulk.TransduceSetup");

		// Click Copy tab: the interactive bulk mode where, with the tab active, clicking a SOURCE cell in the
		// browse table copies that text into the configured TARGET column on the SAME row — per click, no
		// Preview/Apply button. Seed text mirrors the legacy BulkEditBar / XMLViewsStrings wording (the
		// click-copy tab's "Append, separated by:" / "Overwrite" radios, the word/whole-field group, and the
		// ksChooseClickTarget message) so the English fallback matches the classic bar.
		/// <summary>"Click Copy" — bulk-edit bar tab: click a source cell to copy it into a target column.</summary>
		public static string BulkClickCopyTab => Text("FwAvalonia.Bulk.ClickCopy");

		/// <summary>"Copy what?" — Click Copy group label for the word-vs-whole-field mode radios.</summary>
		public static string BulkClickCopyModeLabel => Text("FwAvalonia.Bulk.ClickCopyModeLabel");

		/// <summary>"Word" — Click Copy mode: copy just the clicked word from the source cell.</summary>
		public static string BulkClickCopyWord => Text("FwAvalonia.Bulk.ClickCopyWord");

		/// <summary>"Reorder (whole field)" — Click Copy mode: copy the whole source cell (reorder at the clicked word).</summary>
		public static string BulkClickCopyReorder => Text("FwAvalonia.Bulk.ClickCopyReorder");

		/// <summary>"Separator:" — Click Copy label for the text inserted between an existing target and the appended source.</summary>
		public static string BulkClickCopySeparator => Text("FwAvalonia.Bulk.ClickCopySeparator");

		/// <summary>"Append, separated by:" — Click Copy directivity: append the copied text to a non-empty target.</summary>
		public static string BulkClickCopyAppend => Text("FwAvalonia.Bulk.ClickCopyAppend");

		/// <summary>"Overwrite" — Click Copy directivity: the copied text overwrites the target unconditionally.</summary>
		public static string BulkClickCopyOverwrite => Text("FwAvalonia.Bulk.ClickCopyOverwrite");

		/// <summary>Click Copy message shown when the user clicks a source cell before choosing a target column.</summary>
		public static string BulkClickCopyChooseTarget => Text("FwAvalonia.Bulk.ClickCopyChooseTarget");

		/// <summary>"Sort From End" — browse header context-menu toggle: suffix-oriented sort on the reversed text.</summary>
		public static string SortFromEnd => Text("FwAvalonia.Browse.SortFromEnd");

		/// <summary>"Sort By Length" — browse header context-menu toggle (only when the column allows it).</summary>
		public static string SortByLength => Text("FwAvalonia.Browse.SortByLength");

		// Seed text below matches the canonical legacy wording in XMLViewsStrings (ksMoreThanOneLine,
		// ksExactlyOneLine, ksYes, ksNo, ksZero, ksGreaterThanZero, ksGreaterThanOne) so the English
		// fallback is identical to the classic browse filter combo and translation memory carries over.
		/// <summary>"More than one line" — browse filter preset for multipara columns.</summary>
		public static string FilterMoreThanOneLine => Text("FwAvalonia.Browse.FilterMoreThanOneLine");

		/// <summary>"Exactly one line" — browse filter preset for multipara columns.</summary>
		public static string FilterExactlyOneLine => Text("FwAvalonia.Browse.FilterExactlyOneLine");

		/// <summary>"yes" — browse filter preset (exact match) for sortType=YesNo columns.</summary>
		public static string FilterYes => Text("FwAvalonia.Browse.FilterYes");

		/// <summary>"no" — browse filter preset (exact match) for sortType=YesNo columns.</summary>
		public static string FilterNo => Text("FwAvalonia.Browse.FilterNo");

		/// <summary>"0" — browse filter preset for sortType=integer columns.</summary>
		public static string FilterZero => Text("FwAvalonia.Browse.FilterZero");

		/// <summary>"Greater than 0" — browse filter preset for sortType=integer columns.</summary>
		public static string FilterGreaterThanZero => Text("FwAvalonia.Browse.FilterGreaterThanZero");

		/// <summary>"Greater than 1" — browse filter preset for sortType=integer columns.</summary>
		public static string FilterGreaterThanOne => Text("FwAvalonia.Browse.FilterGreaterThanOne");

		// Seed text matches the canonical legacy wording in XMLViewsStrings (ksFilterFor_, ksExcludeX) so the
		// English fallback is identical to the classic browse FilterBar and translation memory carries over.
		/// <summary>"Filter For…" — universal browse filter entry that opens the pattern-match dialog (FilterBar's FindComboItem).</summary>
		public static string FilterFor => Text("FwAvalonia.Browse.FilterFor");

		/// <summary>"Exclude {0}" — browse filter preset format for the inverse of a stringList enumerated value.</summary>
		public static string FilterExcludeFormat => Text("FwAvalonia.Browse.FilterExcludeFormat");

		// Seed text matches the canonical legacy wording in XMLViewsStrings (ksRestrict_, ksChoose_) so the
		// English fallback is identical to the classic browse FilterBar and translation memory carries over.
		/// <summary>"Restrict Date…" — browse filter entry that opens the date-range dialog (FilterBar's RestrictDateComboItem), for date/genDate columns.</summary>
		public static string RestrictDate => Text("FwAvalonia.Browse.RestrictDate");

		/// <summary>"Choose…" — browse filter entry that opens the list-choice chooser (FilterBar's ListChoiceComboItem), for bulkEdit/chooserFilter columns.</summary>
		public static string FilterChoose => Text("FwAvalonia.Browse.FilterChoose");

		// ----- Delete tab (destructive Delete Rows mode of the legacy Delete tab) -----
		// Seed text matches the canonical legacy wording in XMLViewsStrings (ksDeleteRows label uses
		// "{0} (Rows)"; ksDelete; ksConfirmDeleteMulti/ksConfirmDeleteMultiMsg) and BulkEditBar's dual-mode
		// "Delete what?" combo, so the English fallback is identical to the classic bulk-edit Delete tab and
		// translation memory carries over. APPEND-ONLY: new accessors at the end of the region.

		/// <summary>"Delete what?" — the Delete tab's dual-mode label (Clear Field vs Delete Rows), mirroring BulkEditBar's m_deleteWhatCombo prompt.</summary>
		public static string BulkDeleteWhatLabel => Text("FwAvalonia.Bulk.DeleteWhatLabel");

		/// <summary>"Clear Field" — the non-destructive mode of the Delete tab (empty a target text column across the checked rows).</summary>
		public static string BulkDeleteModeClearField => Text("FwAvalonia.Bulk.DeleteModeClearField");

		/// <summary>"Delete Rows" — the destructive mode of the Delete tab (delete the checked objects), mirroring BulkEditBar's ListClassTargetFieldItem "(Rows)" entry.</summary>
		public static string BulkDeleteModeDeleteRows => Text("FwAvalonia.Bulk.DeleteModeDeleteRows");

		/// <summary>"Delete" — the Delete-Rows Apply button caption (legacy XMLViewsStrings.ksDelete).</summary>
		public static string BulkDeleteApply => Text("FwAvalonia.Bulk.DeleteApply");

		/// <summary>The Delete-Rows preview button caption — shows which checked rows will be deleted vs are blocked.</summary>
		public static string BulkDeletePreview => Text("FwAvalonia.Bulk.DeletePreview");

		/// <summary>"Confirm Multiple Deletions" — the bulk-delete confirmation dialog title (legacy XMLViewsStrings.ksConfirmDeleteMulti).</summary>
		public static string BulkDeleteConfirmTitle => Text("FwAvalonia.Bulk.DeleteConfirmTitle");

		/// <summary>The bulk-delete confirmation message, "{0}" = the count of objects to delete (legacy XMLViewsStrings.ksConfirmDeleteMultiMsg wording).</summary>
		public static string BulkDeleteConfirmMessageFormat => Text("FwAvalonia.Bulk.DeleteConfirmMessageFormat");

		/// <summary>Per-row delete preview marker shown on a row that WILL be deleted (a deletable, checked row).</summary>
		public static string BulkDeleteWillDeleteMarker => Text("FwAvalonia.Bulk.DeleteWillDeleteMarker");

		/// <summary>Per-row delete preview marker shown on a checked row that is BLOCKED from deletion by a guard (e.g. the only sense).</summary>
		public static string BulkDeleteBlockedMarker => Text("FwAvalonia.Bulk.DeleteBlockedMarker");

		// Seed text matches the canonical legacy wording in XMLViewsStrings (ksSpellingErrors) so the English
		// fallback is identical to the classic browse FilterBar and translation memory carries over. APPEND-ONLY.
		/// <summary>"Spelling Errors" — browse filter entry that keeps only rows whose cell contains a spelling error (FilterBar's BadSpellingMatcher), offered only on a string column whose writing system has a spelling dictionary.</summary>
		public static string FilterSpellingErrors => Text("FwAvalonia.Browse.FilterSpellingErrors");

		// ----- Part-of-Speech chooser (MSA-port Stage 1: FwPosChooser) -----
		// Seed text mirrors the legacy WinForms POS picker (POSPopupTreeManager / PopupTreeManager): the
		// empty node shows "<Not sure>" by default (or "<Any>" when the host opts in via the empty-label
		// override, as MSAGroupBox does), and the inline create affordance is the tree's "More..." item,
		// reworded to the clearer "Create a new Part of Speech..." for the new view. APPEND-ONLY.

		/// <summary>"&lt;Not sure&gt;" — the default empty / unspecified Part-of-Speech entry (legacy PopupTreeManager "&lt;Not sure&gt;").</summary>
		public static string PosNotSure => Text("FwAvalonia.Pos.NotSure");

		/// <summary>"&lt;Any&gt;" — the unspecified Part-of-Speech entry when the host treats unspecified as "any" (legacy MSAGroupBox NotSureIsAny).</summary>
		public static string PosAny => Text("FwAvalonia.Pos.Any");

		/// <summary>"Create a new Part of Speech..." — the inline create affordance at the bottom of the POS tree (legacy "More..." item that launched MasterCategoryListDlg).</summary>
		public static string PosCreateNew => Text("FwAvalonia.Pos.CreateNew");

		/// <summary>Accessible name of the collapsed Part-of-Speech chooser dropdown.</summary>
		public static string PosChooserName => Text("FwAvalonia.Pos.ChooserName");

		// ----- Rule-formula editor cell context menu (avalonia-rule-formula-editor, task 2.2) -----
		// Seed text mirrors the legacy RegRuleFormulaControl cell operations. APPEND-ONLY.

		/// <summary>"Delete" — rule-formula cell context-menu item that removes the cell from its group.</summary>
		public static string RuleCellDelete => Text("FwAvalonia.RuleCell.Delete");

		/// <summary>"Insert before" — rule-formula cell context-menu item that opens the chooser to insert a new cell before this one.</summary>
		public static string RuleCellInsertBefore => Text("FwAvalonia.RuleCell.InsertBefore");

		// ----- Editable structured text (StText multi-paragraph fields, §19a) -----
		// Seed text mirrors the legacy StTextSlice rich editor's paragraph operations. APPEND-ONLY.

		/// <summary>Accessible name / tooltip of the per-paragraph "add paragraph" affordance in a structured-text field (§19a).</summary>
		public static string AddParagraph => Text("FwAvalonia.StText.AddParagraph");

		/// <summary>Accessible name / tooltip of the per-paragraph "delete paragraph" affordance in a structured-text field (§19a).</summary>
		public static string DeleteParagraph => Text("FwAvalonia.StText.DeleteParagraph");

		/// <summary>
		/// Accessible name / tooltip of the per-paragraph style picker in a structured-text field (§19a):
		/// opens the list of the project's paragraph styles to apply to this paragraph.
		/// </summary>
		public static string ParagraphStyle => Text("FwAvalonia.StText.ParagraphStyle");

		/// <summary>
		/// The "Default" entry that leads the paragraph-style picker (§19a): selecting it CLEARS any named
		/// paragraph style on the paragraph, reverting it to the default.
		/// </summary>
		public static string DefaultParagraphStyle => Text("FwAvalonia.StText.DefaultParagraphStyle");

		// ----- Rich-text DEPTH: external links + embedded objects (ORC), §19c. APPEND-ONLY. -----

		/// <summary>
		/// Accessible name / tooltip of the external-link affordance on an editable text row (§19c): opens
		/// a small URL prompt that inserts a hyperlink over the selection, or edits an existing link's URL.
		/// </summary>
		public static string Link => Text("FwAvalonia.Link");

		/// <summary>Watermark / accessible name of the URL entry in the link prompt flyout (§19c).</summary>
		public static string LinkUrlPrompt => Text("FwAvalonia.LinkUrlPrompt");

		/// <summary>The confirm button of the link prompt flyout: insert / update the hyperlink (§19c).</summary>
		public static string LinkApply => Text("FwAvalonia.LinkApply");

		/// <summary>
		/// Accessible name / tooltip of the delete-embedded-object affordance (§19c): removes the embedded
		/// object (link, picture, footnote, …) under the selection. Any ORC kind is deletable here even
		/// when its insert/edit path lives elsewhere (picture insert/ORC DONE in §19d via the picture
		/// insert flow; footnote insert deferred).
		/// </summary>
		public static string DeleteEmbeddedObject => Text("FwAvalonia.DeleteEmbeddedObject");

		// ----- Pictures (CmPicture editable parity, §19d). APPEND-ONLY. Seed text mirrors the legacy
		// picture insert/properties/delete affordances (DTMenuHandler.OnInsertPicture / PictureSlice). -----

		/// <summary>Insert-a-picture affordance on an empty picture field / "insert another" on a picture row (legacy Insert Picture).</summary>
		public static string PictureInsert => Text("FwAvalonia.Picture.Insert");

		/// <summary>Edit-picture-properties affordance (caption / license / creator + replace file) — legacy PicturePropertiesDialog.</summary>
		public static string PictureProperties => Text("FwAvalonia.Picture.Properties");

		/// <summary>Delete-picture affordance on a picture row.</summary>
		public static string PictureDelete => Text("FwAvalonia.Picture.Delete");

		// ----- Audio (voice writing systems: play + record, §19d). APPEND-ONLY. Seed text mirrors the
		// legacy voice-WS audio control (LabeledMultiStringView ShortSoundFieldControl play/record/delete). -----

		/// <summary>Play affordance for an existing audio recording on a voice-WS row (legacy ShortSoundFieldControl play).</summary>
		public static string AudioPlay => Text("FwAvalonia.Audio.Play");

		/// <summary>Record affordance for a voice-WS row (legacy ShortSoundFieldControl record).</summary>
		public static string AudioRecord => Text("FwAvalonia.Audio.Record");

		/// <summary>Clear-the-recording affordance for a voice-WS row (legacy ShortSoundFieldControl delete).</summary>
		public static string AudioClear => Text("FwAvalonia.Audio.Clear");

		/// <summary>Tooltip on the record affordance when recording is unavailable on this platform (cross-platform record deferred).</summary>
		public static string AudioRecordUnavailable => Text("FwAvalonia.Audio.RecordUnavailable");

		/// <summary>Placeholder shown for a voice-WS row that has no recording yet.</summary>
		public static string AudioNoRecording => Text("FwAvalonia.Audio.NoRecording");

		// ----- Browse remainders (§19f). APPEND-ONLY. -----

		/// <summary>Menu entry to copy the current browse cell's text to the clipboard (§19f.4, Ctrl+C parity).</summary>
		public static string CellCopy => Text("FwAvalonia.Browse.CellCopy");

		/// <summary>Menu entry to paste the clipboard text into an editable browse cell (§19f.4, Ctrl+V parity).</summary>
		public static string CellPaste => Text("FwAvalonia.Browse.CellPaste");

		/// <summary>The prompt shown in the empty Rapid-Data-Entry "new row" at the bottom of the browse (§19f.7).</summary>
		public static string RdeNewRowPrompt => Text("FwAvalonia.Browse.RdeNewRowPrompt");

		/// <summary>Menu/affordance label to export the visible browse columns and rows as CSV (§19f.9).</summary>
		public static string ExportCsv => Text("FwAvalonia.Browse.ExportCsv");

		// ----- StText (§19a) accessibility names. APPEND-ONLY. -----

		/// <summary>Screen-reader name for one paragraph editor in a multi-paragraph StText field
		/// (§19a). {0} = the field label, {1} = the 1-based paragraph number.</summary>
		public static string StructuredTextParagraphName(string fieldLabel, int paragraphNumber)
			=> string.Format(Text("FwAvalonia.StructuredText.ParagraphName"), fieldLabel, paragraphNumber);

		// ----- Per-tool feature catalog (LexicalEditFeatureCatalog): display metadata for the
		// "Manage Individual Features" dialog's checkbox list. APPEND-ONLY. -----

		/// <summary>Group heading for the entry-editing tool surfaces in the feature-manager dialog.</summary>
		public static string FeatureGroupLexicalEntryDialogs => Text("FwAvalonia.FeatureGroup.LexicalEntryDialogs");

		/// <summary>Group heading for the non-entry record-type tool surfaces in the feature-manager dialog.</summary>
		public static string FeatureGroupOtherRecordTypes => Text("FwAvalonia.FeatureGroup.OtherRecordTypes");

		public static string FeatureLexiconEditName => Text("FwAvalonia.Feature.LexiconEditName");
		public static string FeatureLexiconEditDescription => Text("FwAvalonia.Feature.LexiconEditDescription");

		public static string FeatureLexiconEditPopupName => Text("FwAvalonia.Feature.LexiconEditPopupName");
		public static string FeatureLexiconEditPopupDescription => Text("FwAvalonia.Feature.LexiconEditPopupDescription");

		public static string FeatureNotebookEditName => Text("FwAvalonia.Feature.NotebookEditName");
		public static string FeatureNotebookEditDescription => Text("FwAvalonia.Feature.NotebookEditDescription");

		public static string FeaturePosEditName => Text("FwAvalonia.Feature.PosEditName");
		public static string FeaturePosEditDescription => Text("FwAvalonia.Feature.PosEditDescription");
	}
}
