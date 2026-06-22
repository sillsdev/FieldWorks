// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// Localized product-facing strings for the Avalonia MVVM dialogs. Runtime lookup goes through the
	/// existing LocalizationManager/XLIFF lane; the inline English defaults in this accessor are the
	/// current source of truth. Automation ids stay nonlocalized constants in XAML, never resource lookups.
	/// </summary>
	public static class FwAvaloniaDialogsStrings
	{
		private static string Text(string stringId, string englishText, string comment = null)
			=> FwAvaloniaLocalization.GetPalasoString(stringId, englishText, comment);

		private static string ChorusText(string stringId, string englishText, string comment = null)
			=> FwAvaloniaLocalization.GetChorusString(stringId, englishText, comment);

		public static string OptionsTitle => Text("FwAvaloniaDialogs.OptionsTitle", "Options");

		// Tab headers (the four real Options tabs).
		public static string GeneralTab => Text("FwAvaloniaDialogs.GeneralTab", "General");
		public static string PluginsTab => Text("FwAvaloniaDialogs.PluginsTab", "Plugins");
		public static string PrivacyTab => Text("FwAvaloniaDialogs.PrivacyTab", "Privacy");
		public static string UpdatesTab => Text("FwAvaloniaDialogs.UpdatesTab", "Updates");

		// General tab.
		public static string UiLanguageLabel => Text("FwAvaloniaDialogs.UiLanguageLabel", "UI language");
		public static string UiLanguageNote => Text("FwAvaloniaDialogs.UiLanguageNote", "Changing the UI language requires restarting FieldWorks.");
		public static string LexicalEditUiLabel => Text("FwAvaloniaDialogs.LexicalEditUiLabel", "Lexical Edit UI");
		public static string UiModeLegacy => Text("FwAvaloniaDialogs.UiModeLegacy", "Legacy");
		public static string UiModeNew => Text("FwAvaloniaDialogs.UiModeNew", "New");
		public static string Apply => Text("FwAvaloniaDialogs.Apply", "Apply");
		public static string AutoOpenLastProject => Text("FwAvaloniaDialogs.AutoOpenLastProject", "Open the last project automatically");

		// Plugins tab.
		public static string PluginsUnavailableNote => Text("FwAvaloniaDialogs.PluginsUnavailableNote", "Plugin management is not available in the new dialog yet.");

		// Privacy tab.
		public static string PrivacyNote => Text("FwAvaloniaDialogs.PrivacyNote", "Choose whether FieldWorks may send anonymous usage data.");
		public static string OkToPing => Text("FwAvaloniaDialogs.OkToPing", "Allow anonymous usage reporting");

		// Updates tab.
		public static string AutoUpdate => Text("FwAvaloniaDialogs.AutoUpdate", "Install updates automatically");
		public static string UpdateChannelLabel => Text("FwAvaloniaDialogs.UpdateChannelLabel", "Update channel");

		public static string Ok => Text("Common.OK", "OK");
		public static string Cancel => Text("Common.Cancel", "Cancel");
		public static string Help => ChorusText("Common.Help", "Help");

		// Reusable chooser dialog (Phase 1).
		public static string ChooserEmptyOption => Text("FwAvaloniaDialogs.ChooserEmptyOption", "(none)");
		public static string ChooserMustSelect => Text("FwAvaloniaDialogs.ChooserMustSelect", "Select an item.");
		public static string ChooserSearchPrompt => Text("FwAvaloniaDialogs.ChooserSearchPrompt", "Type to search");

		// Reusable Insert Entry dialog (Phase 1).
		public static string InsertEntryTitle => Text("FwAvaloniaDialogs.InsertEntryTitle", "Insert Entry");
		public static string InsertEntryCreate => Text("FwAvaloniaDialogs.InsertEntryCreate", "Create");
		public static string InsertEntryLexemeFormLabel => Text("FwAvaloniaDialogs.InsertEntryLexemeFormLabel", "Lexeme Form");
		public static string InsertEntryMorphTypeLabel => Text("FwAvaloniaDialogs.InsertEntryMorphTypeLabel", "Morph Type");
		public static string InsertEntryGlossLabel => Text("FwAvaloniaDialogs.InsertEntryGlossLabel", "Gloss");
		public static string InsertEntryLexFormNotEmpty => Text("FwAvaloniaDialogs.InsertEntryLexFormNotEmpty", "Lexeme Form is required.");
		// Insert Entry duplicate-detection "matching entries" pane (P2). Seeded from the canonical legacy
		// InsertEntryDlg.resx wording (the m_matchingEntriesGroupBox caption "Similar Entries" + the
		// m_linkSimilarEntry link "Go to similar entry").
		public static string InsertEntryMatchingEntriesLabel => Text("FwAvaloniaDialogs.InsertEntryMatchingEntriesLabel", "Similar Entries");
		public static string InsertEntryUseSelectedEntry => Text("FwAvaloniaDialogs.InsertEntryUseSelectedEntry", "Go to similar entry");

		// Find/Replace pattern-setup dialog (Find/Replace Phase 1, bulk replace).
		public static string FindReplaceTitle => Text("FwAvaloniaDialogs.FindReplaceTitle", "Find and Replace");
		public static string FindReplaceFindLabel => Text("FwAvaloniaDialogs.FindReplaceFindLabel", "Find");
		public static string FindReplaceReplaceLabel => Text("FwAvaloniaDialogs.FindReplaceReplaceLabel", "Replace with");
		public static string FindReplaceMatchCase => Text("FwAvaloniaDialogs.FindReplaceMatchCase", "Match case");
		public static string FindReplaceMatchDiacritics => Text("FwAvaloniaDialogs.FindReplaceMatchDiacritics", "Match diacritics");
		public static string FindReplaceMatchWholeWord => Text("FwAvaloniaDialogs.FindReplaceMatchWholeWord", "Match whole word");
		public static string FindReplaceMatchWritingSystem => Text("FwAvaloniaDialogs.FindReplaceMatchWritingSystem", "Match writing system");
		public static string FindReplaceUseRegex => Text("FwAvaloniaDialogs.FindReplaceUseRegex", "Use regular expressions");
		public static string FindReplaceFindEmpty => Text("FwAvaloniaDialogs.FindReplaceFindEmpty", "Enter text to find.");
		public static string FindReplaceInvalidRegex => Text("FwAvaloniaDialogs.FindReplaceInvalidRegex", "The regular expression is invalid.");

		// Configure-Columns dialog (Avalonia browse, P1: show/hide/reorder).
		public static string ConfigureColumnsTitle => Text("FwAvaloniaDialogs.ConfigureColumnsTitle", "Configure Columns");
		public static string ConfigureColumnsAvailableLabel => Text("FwAvaloniaDialogs.ConfigureColumnsAvailableLabel", "Available columns");
		public static string ConfigureColumnsShownLabel => Text("FwAvaloniaDialogs.ConfigureColumnsShownLabel", "Shown columns");
		public static string ConfigureColumnsAdd => Text("FwAvaloniaDialogs.ConfigureColumnsAdd", "Add");
		public static string ConfigureColumnsRemove => Text("FwAvaloniaDialogs.ConfigureColumnsRemove", "Remove");
		public static string ConfigureColumnsMoveUp => Text("FwAvaloniaDialogs.ConfigureColumnsMoveUp", "Move up");
		public static string ConfigureColumnsMoveDown => Text("FwAvaloniaDialogs.ConfigureColumnsMoveDown", "Move down");
		public static string ConfigureColumnsNeedsAColumn => Text("FwAvaloniaDialogs.ConfigureColumnsNeedsAColumn", "At least one column must remain visible.");

		// Message-box buttons (shared by FwMessageBox / MessageBoxViewModel) and severity-icon accessible names.
		public static string Yes => Text("FwAvaloniaDialogs.Yes", "Yes");
		public static string No => Text("FwAvaloniaDialogs.No", "No");
		public static string IconInformation => Text("FwAvaloniaDialogs.IconInformation", "Information");
		public static string IconWarning => Text("FwAvaloniaDialogs.IconWarning", "Warning");
		public static string IconError => ChorusText("Common.Error", "Error");
		public static string IconQuestion => Text("FwAvaloniaDialogs.IconQuestion", "Question");

		// Reusable entry-search ("go") dialog — the EntryGoDlg/BaseGoDlg family (Merge Entry is the first consumer).
		public static string EntryGoMustSelect => Text("FwAvaloniaDialogs.EntryGoMustSelect", "Select an entry.");
		public static string EntryGoSearchWatermark => Text("FwAvaloniaDialogs.EntryGoSearchWatermark", "Type to search");
		public static string EntryGoResultsLabel => Text("FwAvaloniaDialogs.EntryGoResultsLabel", "Lexical Entries");

		// Merge Entry consumer of the entry-search dialog (legacy MergeEntryDlg / EntryGoDlg wording).
		public static string MergeTitle => Text("FwAvaloniaDialogs.Merge.Title", "Merge Entry");
		public static string MergeOkButton => Text("FwAvaloniaDialogs.Merge.OkButton", "Merge");

		// Add Allomorph consumer of the entry-search dialog (legacy AddAllomorphDlg / LexTextControls wording).
		public static string AddAllomorphTitle => Text("FwAvaloniaDialogs.AddAllomorph.Title", "Find Entry to Add Allomorph");
		public static string AddAllomorphOkButton => Text("FwAvaloniaDialogs.AddAllomorph.OkButton", "Add Allomorph...");
		public static string AddAllomorphUndo => Text("FwAvaloniaDialogs.AddAllomorph.Undo", "Undo add allomorph");
		public static string AddAllomorphRedo => Text("FwAvaloniaDialogs.AddAllomorph.Redo", "Redo add allomorph");

		// Link Entry or Sense consumer of the entry-search dialog (legacy LinkEntryOrSenseDlg wording).
		public static string LinkEntryOrSenseTitle => Text("FwAvaloniaDialogs.LinkEntryOrSense.Title", "Choose Lexical Entry or Sense");

		// Link Allomorph consumer of the entry-search dialog (legacy LinkAllomorphDlg wording).
		public static string LinkAllomorphTitle => Text("FwAvaloniaDialogs.LinkAllomorph.Title", "Choose Allomorph");

		// Link MSA consumer of the entry-search dialog (legacy LinkMSADlg wording).
		public static string LinkMsaTitle => Text("FwAvaloniaDialogs.LinkMsa.Title", "Choose Morpheme and Grammatical Info.");

		// "Filter For…" pattern-match dialog (browse column filter parity). Seed text matches the canonical
		// legacy SimpleMatchDlg wording so the English fallback is identical and translation memory carries over.
		public static string FilterForTitle => Text("FwAvaloniaDialogs.FilterFor.Title", "Filter for items containing...");
		public static string FilterForMatchLabel => Text("FwAvaloniaDialogs.FilterFor.MatchLabel", "Enter text to search for:");
		public static string FilterForAnywhere => Text("FwAvaloniaDialogs.FilterFor.Anywhere", "Anywhere");
		public static string FilterForAtStart => Text("FwAvaloniaDialogs.FilterFor.AtStart", "At start");
		public static string FilterForAtEnd => Text("FwAvaloniaDialogs.FilterFor.AtEnd", "At end");
		public static string FilterForWholeItem => Text("FwAvaloniaDialogs.FilterFor.WholeItem", "Whole item");
		public static string FilterForRegex => Text("FwAvaloniaDialogs.FilterFor.Regex", "Match for regular expression");
		public static string FilterForMatchCase => Text("FwAvaloniaDialogs.FilterFor.MatchCase", "Match case");
		public static string FilterForEmpty => Text("FwAvaloniaDialogs.FilterFor.Empty", "Enter text to filter for.");
		public static string FilterForInvalidRegex => Text("FwAvaloniaDialogs.FilterFor.InvalidRegex", "The regular expression is not valid.");

		// "Restrict Date…" date-range dialog (browse date/genDate column filter parity). The relation labels seed
		// from the canonical legacy SimpleDateMatchDlg type-combo wording so the English fallback is identical and
		// translation memory carries over.
		public static string RestrictDateTitle => Text("FwAvaloniaDialogs.RestrictDate.Title", "Restrict Date");
		public static string RestrictDateRelationLabel => Text("FwAvaloniaDialogs.RestrictDate.RelationLabel", "Show items where the date is:");
		public static string RestrictDateDateLabel => Text("FwAvaloniaDialogs.RestrictDate.DateLabel", "Date");
		public static string RestrictDateEndLabel => Text("FwAvaloniaDialogs.RestrictDate.EndLabel", "and");
		public static string RestrictDateOn => Text("FwAvaloniaDialogs.RestrictDate.On", "on");
		public static string RestrictDateNotOn => Text("FwAvaloniaDialogs.RestrictDate.NotOn", "not on");
		public static string RestrictDateOnOrBefore => Text("FwAvaloniaDialogs.RestrictDate.OnOrBefore", "on or before");
		public static string RestrictDateOnOrAfter => Text("FwAvaloniaDialogs.RestrictDate.OnOrAfter", "on or after");
		public static string RestrictDateBetween => Text("FwAvaloniaDialogs.RestrictDate.Between", "between");
		public static string RestrictDateNoDate => Text("FwAvaloniaDialogs.RestrictDate.NoDate", "Choose a date.");
		public static string RestrictDateRangeInverted => Text("FwAvaloniaDialogs.RestrictDate.RangeInverted", "The end date must be on or after the start date.");

		// "Choose…" list-choice chooser title (browse chooser column filter parity).
		public static string FilterChooseTitle => Text("FwAvaloniaDialogs.FilterChoose.Title", "Choose Items");
		public static string FilterChoosePrompt => Text("FwAvaloniaDialogs.FilterChoose.Prompt", "Choose the items to filter for:");

		// Link Entry or Sense entry/sense toggle (the legacy LinkEntryOrSenseDlg m_rbEntry / m_rbSense radios). Seed
		// text matches the canonical legacy LinkEntryOrSenseDlg.resx wording (ampersand accelerators dropped — the
		// Avalonia toggle does not use WinForms-style mnemonics) so the translation memory carries over.
		public static string LinkEntryOrSenseEntryRadio => Text("FwAvaloniaDialogs.LinkEntryOrSense.EntryRadio", "Entry");
		public static string LinkEntryOrSenseSenseRadio => Text("FwAvaloniaDialogs.LinkEntryOrSense.SenseRadio", "Specific Sense");

		// Commit-on-select confirmation for the (semi-destructive) Merge consumer: shown AFTER the user commits a
		// survivor selection and BEFORE the merge runs (the other Add/Link* consumers act immediately, no confirm).
		// Seeded from the canonical legacy LexTextControls.ksEntryXMergedIntoY wording ("Entry \"{0}\" will be merged
		// into \"{1}\",{2}resulting in one entry.") so the English fallback matches and the translation memory carries
		// over; {0} is the current entry, {1} the chosen survivor, {2} a newline.
		public static string MergeConfirm => Text("FwAvaloniaDialogs.Merge.Confirm",
			"Entry \"{0}\" will be merged into \"{1}\",\nresulting in one entry.");
	}
}
