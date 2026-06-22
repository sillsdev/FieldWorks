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
		// The grammatical-info section caption in the Insert Entry dialog (the legacy InsertEntryDlg m_msaGroupBox
		// group-box caption "Grammatical Information").
		public static string InsertEntryGrammaticalInfoLabel => Text("FwAvaloniaDialogs.InsertEntryGrammaticalInfoLabel", "Grammatical Information");

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

		// MSA (grammatical-info) group box — the FwMsaGroupBox composite mirroring the WinForms MSAGroupBox.
		// Seed text matches the canonical legacy LexTextControls.resx / MSAGroupBox.resx wording (ampersand
		// mnemonics dropped — the Avalonia controls don't use WinForms-style accelerators) so the English
		// fallback is identical and translation memory carries over.

		// The Affix Type picker label + its three options (MSAGroupBox.resx m_lAfxType "Affix &Type:" and the
		// combo items seeded from LexTextControls.ksNotSure / ksInflectional / ksDerivational).
		public static string MsaAffixTypeLabel => Text("FwAvaloniaDialogs.Msa.AffixTypeLabel", "Affix Type");
		public static string MsaAffixTypeNotSure => Text("FwAvaloniaDialogs.Msa.AffixTypeNotSure", "Not Sure");
		public static string MsaAffixTypeInflectional => Text("FwAvaloniaDialogs.Msa.AffixTypeInflectional", "Inflectional");
		public static string MsaAffixTypeDerivational => Text("FwAvaloniaDialogs.Msa.AffixTypeDerivational", "Derivational");

		// The Main-POS field label, which the WinForms box retitles per MsaType (LexTextControls.ksCategor_y for
		// stem/root, ksAttachesToCategor_y for every affix type).
		public static string MsaCategoryLabel => Text("FwAvaloniaDialogs.Msa.CategoryLabel", "Category");
		public static string MsaAttachesToCategoryLabel => Text("FwAvaloniaDialogs.Msa.AttachesToCategoryLabel", "Attaches to Category");

		// The Slot/Secondary-POS field label, which the WinForms box retitles per MsaType
		// (LexTextControls.ks_FillsSlot for inflectional, ksC_hangesToCategory for derivational).
		public static string MsaFillsSlotLabel => Text("FwAvaloniaDialogs.Msa.FillsSlotLabel", "Fills Slot");
		public static string MsaChangesToCategoryLabel => Text("FwAvaloniaDialogs.Msa.ChangesToCategoryLabel", "Changes to Category");

		// Create-a-new-Part-of-Speech catalog chooser (MSA-port Stage 4) — the inline "Create a new Part of
		// Speech..." affordance opens the master-category (GOLDEtic) catalog as a hierarchical single-select
		// ChooserDialog. Seed text matches the canonical legacy MasterCategoryListDlg.resx wording ($this.Text
		// "Add from Catalog" and the label1 instruction prompt) so the English fallback is identical and the
		// translation memory carries over. APPEND-ONLY.
		public static string CreatePosTitle => Text("FwAvaloniaDialogs.CreatePos.Title", "Add from Catalog");
		public static string CreatePosPrompt => Text("FwAvaloniaDialogs.CreatePos.Prompt",
			"Choose a grammatical category (Part of Speech) from the following Catalog. The category you choose will be added to the list of categories for this FieldWorks Project.");

		// Add New Sense dialog (MSA-port Stage 5) — the Avalonia replacement for the WinForms AddNewSenseDlg
		// (a read-only citation form + an editable gloss + the grammatical-info group box -> a new ILexSense).
		// Seed text matches the canonical legacy AddNewSenseDlg.resx wording (label2 "Citation Form:" and label1
		// "Gloss:", ampersand mnemonics + trailing colons dropped — the Avalonia labels carry neither) and the
		// shared LexTextControls.ksFillInGloss / ksMissingInformation OK-gate messages, so the English fallback is
		// identical and the translation memory carries over. APPEND-ONLY.
		public static string AddNewSenseTitle => Text("FwAvaloniaDialogs.AddNewSense.Title", "Add New Sense");
		public static string AddNewSenseCitationFormLabel => Text("FwAvaloniaDialogs.AddNewSense.CitationFormLabel", "Citation Form");
		public static string AddNewSenseGlossLabel => Text("FwAvaloniaDialogs.AddNewSense.GlossLabel", "Gloss");
		public static string AddNewSenseGrammaticalInfoLabel => Text("FwAvaloniaDialogs.AddNewSense.GrammaticalInfoLabel", "Grammatical Information");
		// The legacy OK gate when the gloss is empty (LexTextControls.ksFillInGloss "Please fill in the gloss.").
		public static string AddNewSenseFillInGloss => Text("FwAvaloniaDialogs.AddNewSense.FillInGloss", "Please fill in the gloss.");

		// Create New Grammatical Info. dialog (MSA-port Stage 5) — the Avalonia replacement for the WinForms
		// MsaCreatorDlg (a read-only lexical entry + read-only senses + the grammatical-info group box -> an MSA).
		// Seed text matches the canonical legacy MsaCreatorDlg.resx wording ($this.Text "Create New Grammatical
		// Info.", label1 "Lexical Entry:" and label2 "Senses:", trailing colons dropped) so the English fallback is
		// identical and the translation memory carries over. APPEND-ONLY.
		public static string MsaCreatorTitle => Text("FwAvaloniaDialogs.MsaCreator.Title", "Create New Grammatical Info.");
		public static string MsaCreatorLexicalEntryLabel => Text("FwAvaloniaDialogs.MsaCreator.LexicalEntryLabel", "Lexical Entry");
		public static string MsaCreatorSensesLabel => Text("FwAvaloniaDialogs.MsaCreator.SensesLabel", "Senses");
		public static string MsaCreatorGrammaticalInfoLabel => Text("FwAvaloniaDialogs.MsaCreator.GrammaticalInfoLabel", "Grammatical Information");
		public static string MsaCreatorCreate => Text("FwAvaloniaDialogs.MsaCreator.Create", "Create");

		// The inflection-class picker label (MSA-port Stage 6) shown for the stem/root MSA — the inflection class
		// of the selected main POS (the legacy InsertEntryDlg inflection-class affordance, IMoStemMsa.InflectionClassRA,
		// driven by InflectionClassPopupTreeManager). Seed text matches the canonical field label "Inflection Class"
		// (the m3 InflectionClass field label / DataTree "Inflection Class" slice). APPEND-ONLY.
		public static string MsaInflectionClassLabel => Text("FwAvaloniaDialogs.Msa.InflectionClassLabel", "Inflection Class");
		// The "<None>" row in the inflection-class picker (empty selection is valid). Seeded from the shared
		// "<None>" / not-sure wording the WinForms inflection-class tree uses (AddNotSureItem).
		public static string MsaInflectionClassNone => Text("FwAvaloniaDialogs.Msa.InflectionClassNone", "<None>");

		// Complex Form Type picker in the Insert Entry dialog — the Avalonia parity of the WinForms
		// InsertEntryDlg m_cbComplexFormType combo (LT-21666). Seed text matches the canonical legacy wording:
		// the field caption (InsertEntryDlg.resx m_complexTypeLabel "Complex Form Type") and the leading
		// "<Not Applicable>" item (LexTextControls.ksNotApplicable). APPEND-ONLY.
		public static string InsertEntryComplexFormTypeLabel => Text("FwAvaloniaDialogs.InsertEntry.ComplexFormTypeLabel", "Complex Form Type");
		public static string InsertEntryComplexFormTypeNotApplicable => Text("FwAvaloniaDialogs.InsertEntry.ComplexFormTypeNotApplicable", "<Not Applicable>");

		// Feature-structure editor (FsFeatStruc tree editor, Phase-1 §19b Stage 1) — the Avalonia replacement
		// for the WinForms FeatureStructureTreeView / MsaInflectionFeatureListDlg / PhonologicalFeatureChooserDlg.
		// Seed text matches the canonical legacy wording so the English fallback is identical and the
		// translation memory carries over: the unspecified value radio mirrors the legacy "None of the above"
		// (LexTextControls.ksNoneOfTheAbove), shortened to "<None>" for the compact new-view tree; the create
		// affordances mirror the MasterInflectionFeatureListDlg "create a feature" / feature-system "add value"
		// flows. APPEND-ONLY.

		/// <summary>The unspecified value radio leading/trailing a closed feature's values (legacy "None of the above" / AddNotSureItem). Selecting it clears that feature's assignment.</summary>
		public static string FeatureEditorNone => Text("FwAvaloniaDialogs.FeatureEditor.None", "<None>");

		/// <summary>Inline affordance at the bottom of the feature tree that raises CreateNewFeatureRequested (legacy MasterInflectionFeatureListDlg create-feature link).</summary>
		public static string FeatureEditorCreateFeature => Text("FwAvaloniaDialogs.FeatureEditor.CreateFeature", "Create a new feature...");

		/// <summary>"Add a value to {0}..." — the per-closed-feature affordance that raises CreateNewValueRequested ({0} = the feature name).</summary>
		public static string FeatureEditorCreateValueFormat => Text("FwAvaloniaDialogs.FeatureEditor.CreateValueFormat", "Add a value to {0}...");

		/// <summary>Accessible name of the feature-structure editor control.</summary>
		public static string FeatureEditorName => Text("FwAvaloniaDialogs.FeatureEditor.Name", "Feature structure");

		// MSA inflection-feature editor (Phase-1 §19b Stage 2) — the inflection-feature column the FwMsaGroupBox
		// shows for inflectional/derivational MSAs (where the WinForms box's "Inflection Features" affordance opens
		// MsaInflectionFeatureListDlg over IMoInflAffMsa.InflFeatsOA / IMoDerivAffMsa.FromMsFeaturesOA). Seed text
		// matches the canonical legacy field caption ("Inflection Features"). APPEND-ONLY.
		public static string MsaInflectionFeaturesLabel => Text("FwAvaloniaDialogs.Msa.InflectionFeaturesLabel", "Inflection Features");

		// Standalone feature-structure chooser dialogs (Phase-1 §19b Stage 3) — the Avalonia replacements for the
		// WinForms MsaInflectionFeatureListDlg (assign inflection feature values to an MSA's IFsFeatStruc) and
		// PhonologicalFeatureChooserDlg (the phonological feature system). Each hosts the shared
		// FwFeatureStructureEditor over OK/Cancel/Help. Seed text matches the canonical legacy wording (the dialog
		// captions + prompts from the StringTable FeatureChooser group) so the English fallback is identical and the
		// translation memory carries over. APPEND-ONLY.
		public static string InflectionFeatureChooserTitle => Text("FwAvaloniaDialogs.FeatureChooser.InflectionTitle", "Inflection Feature Information");
		public static string InflectionFeatureChooserPrompt => Text("FwAvaloniaDialogs.FeatureChooser.InflectionPrompt", "Choose the inflection feature values for this item.");
		public static string PhonologicalFeatureChooserTitle => Text("FwAvaloniaDialogs.FeatureChooser.PhonologicalTitle", "Phonological Feature Information");
		public static string PhonologicalFeatureChooserPrompt => Text("FwAvaloniaDialogs.FeatureChooser.PhonologicalPrompt", "Choose the phonological feature values for this item.");

		// Create-a-new-feature dialog (Phase-1 §19b Stage 3) — the Avalonia replacement for the
		// MasterInflectionFeatureListDlg / MasterPhonologicalFeatureListDlg blank-create affordance (create a new
		// closed feature in the feature system, naming it; the phonological variant auto-creates the +/- values).
		// Seed text matches the canonical legacy create-feature dialog wording. APPEND-ONLY.
		public static string CreateFeatureTitle => Text("FwAvaloniaDialogs.CreateFeature.Title", "Create New Feature");
		public static string CreateFeatureNameLabel => Text("FwAvaloniaDialogs.CreateFeature.NameLabel", "Name");
		public static string CreateFeatureAbbrLabel => Text("FwAvaloniaDialogs.CreateFeature.AbbrLabel", "Abbreviation");
		public static string CreateFeatureNameRequired => Text("FwAvaloniaDialogs.CreateFeature.NameRequired", "Enter a name for the feature.");

		// Create-a-new-value dialog (Phase-1 §19b Stage 3) — the Avalonia replacement for adding a symbolic value to
		// an existing closed feature (the feature-system value editor flow the WinForms "add value" reached).
		// APPEND-ONLY.
		public static string CreateValueTitle => Text("FwAvaloniaDialogs.CreateValue.Title", "Create New Feature Value");
		public static string CreateValueNameLabel => Text("FwAvaloniaDialogs.CreateValue.NameLabel", "Name");
		public static string CreateValueAbbrLabel => Text("FwAvaloniaDialogs.CreateValue.AbbrLabel", "Abbreviation");
		public static string CreateValueNameRequired => Text("FwAvaloniaDialogs.CreateValue.NameRequired", "Enter a name for the value.");

		// Picture-properties dialog (§19d) — the Avalonia replacement for the WinForms PicturePropertiesDialog
		// (file pick + caption/description/license/creator). Seed text mirrors the canonical legacy picture
		// metadata labels. APPEND-ONLY.
		public static string PicturePropertiesTitle => Text("FwAvaloniaDialogs.PictureProperties.Title", "Picture Properties");
		public static string PicturePropertiesCaptionLabel => Text("FwAvaloniaDialogs.PictureProperties.CaptionLabel", "Caption");
		public static string PicturePropertiesDescriptionLabel => Text("FwAvaloniaDialogs.PictureProperties.DescriptionLabel", "Description");
		public static string PicturePropertiesLicenseLabel => Text("FwAvaloniaDialogs.PictureProperties.LicenseLabel", "License");
		public static string PicturePropertiesCreatorLabel => Text("FwAvaloniaDialogs.PictureProperties.CreatorLabel", "Creator");
		public static string PicturePropertiesImageLabel => Text("FwAvaloniaDialogs.PictureProperties.ImageLabel", "Image file");
		public static string PicturePropertiesChooseImage => Text("FwAvaloniaDialogs.PictureProperties.ChooseImage", "Choose image...");
		public static string PicturePropertiesNoFile => Text("FwAvaloniaDialogs.PictureProperties.NoFile", "(no file chosen)");
		public static string PicturePropertiesInsert => Text("FwAvaloniaDialogs.PictureProperties.Insert", "Insert");

		// Reference Set Details dialog (§19g) — the Avalonia replacement for the WinForms LexReferenceDetailsDlg
		// (edit a lexical reference's name + comment/note). Seed text mirrors the canonical legacy resx
		// (label1 "&Name", label3 "&Comment", lblExplanation). APPEND-ONLY.
		public static string LexReferenceDetailsTitle => Text("FwAvaloniaDialogs.LexReferenceDetails.Title", "Reference Set Details");
		public static string LexReferenceDetailsNameLabel => Text("FwAvaloniaDialogs.LexReferenceDetails.NameLabel", "Name");
		public static string LexReferenceDetailsCommentLabel => Text("FwAvaloniaDialogs.LexReferenceDetails.CommentLabel", "Comment");
		public static string LexReferenceDetailsExplanation => Text("FwAvaloniaDialogs.LexReferenceDetails.Explanation",
			"Enter a name or comment about this specific reference set. For now it is visible only in this dialog.");

		// Delete-confirmation dialog (§19g) — the Avalonia replacement for the WinForms ConfirmDeleteObjectDlg
		// (confirm deleting an entry/sense/reference, with the affected-object summary + optional orphan note).
		// Seed text mirrors the canonical legacy ConfirmDeleteObjectDlg.resx (label1 top message, label2 bottom
		// question) and the m_deleteButton "&Delete" caption. APPEND-ONLY.
		public static string DeleteConfirmationTopMessage => Text("FwAvaloniaDialogs.DeleteConfirmation.TopMessage",
			"You are deleting the following item:");
		public static string DeleteConfirmationBottomQuestion => Text("FwAvaloniaDialogs.DeleteConfirmation.BottomQuestion",
			"Do you want to continue with the deletion?");
		public static string DeleteConfirmationDelete => Text("FwAvaloniaDialogs.DeleteConfirmation.Delete", "Delete");

		// Special-character / Unicode insert picker (§19g) — a net-new Avalonia picker (no WinForms truth dialog;
		// the legacy Format > Special character shells out to the OS charmap). APPEND-ONLY.
		public static string SpecialCharacterTitle => Text("FwAvaloniaDialogs.SpecialCharacter.Title", "Insert Special Character");
		public static string SpecialCharacterFilterPrompt => Text("FwAvaloniaDialogs.SpecialCharacter.FilterPrompt", "Type to filter by name or code");
		public static string SpecialCharacterInsert => Text("FwAvaloniaDialogs.SpecialCharacter.Insert", "Insert");
		public static string SpecialCharacterMustSelect => Text("FwAvaloniaDialogs.SpecialCharacter.MustSelect", "Select a character to insert.");

		// Writing System properties / Add-WS core (§19g) — the Avalonia bounded core of the WinForms
		// FwWritingSystemSetupDlg (name, abbreviation, font, direction, sort). Full SLDR/converters/merge is a
		// documented PARITY deferral. APPEND-ONLY.
		public static string WritingSystemPropertiesTitle => Text("FwAvaloniaDialogs.WritingSystemProperties.Title", "Writing System Properties");
		public static string WritingSystemPropertiesNameLabel => Text("FwAvaloniaDialogs.WritingSystemProperties.NameLabel", "Name");
		public static string WritingSystemPropertiesAbbrLabel => Text("FwAvaloniaDialogs.WritingSystemProperties.AbbrLabel", "Abbreviation");
		public static string WritingSystemPropertiesFontLabel => Text("FwAvaloniaDialogs.WritingSystemProperties.FontLabel", "Default font");
		public static string WritingSystemPropertiesRightToLeft => Text("FwAvaloniaDialogs.WritingSystemProperties.RightToLeft", "Right-to-left script");
		public static string WritingSystemPropertiesSortLabel => Text("FwAvaloniaDialogs.WritingSystemProperties.SortLabel", "Sorting");
		public static string WritingSystemPropertiesNameRequired => Text("FwAvaloniaDialogs.WritingSystemProperties.NameRequired", "Enter a name for the writing system.");
		public static string WritingSystemPropertiesAbbrRequired => Text("FwAvaloniaDialogs.WritingSystemProperties.AbbrRequired", "Enter an abbreviation.");
		public static string WritingSystemPropertiesInvalidTag => Text("FwAvaloniaDialogs.WritingSystemProperties.InvalidTag", "The writing system tag is not valid.");
	}
}
