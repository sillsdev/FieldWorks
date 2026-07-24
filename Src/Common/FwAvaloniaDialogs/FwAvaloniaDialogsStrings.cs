// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// Localized product-facing strings for the Avalonia MVVM dialogs. Strings resolve through
	/// ResourceManager over FwAvaloniaDialogsStrings.resx — the neutral resx is the English source of
	/// truth and translations ship as satellite assemblies (the FieldWorks .resx localization
	/// strategy). Automation ids stay nonlocalized constants in XAML, never resource lookups.
	/// </summary>
	public static class FwAvaloniaDialogsStrings
	{
		private static readonly System.Resources.ResourceManager Resources =
			new System.Resources.ResourceManager("FwAvaloniaDialogs.FwAvaloniaDialogsStrings", typeof(FwAvaloniaDialogsStrings).Assembly);

		// Falls back to the id so a missing resx entry is visible in the UI instead of blank
		// (AvaloniaLocalizationTests pins every accessor against the neutral resx).
		private static string Text(string stringId) => Resources.GetString(stringId) ?? stringId;

		public static string OptionsTitle => Text("FwAvaloniaDialogs.OptionsTitle");

		// Tab headers (the four real Options tabs).
		public static string GeneralTab => Text("FwAvaloniaDialogs.GeneralTab");
		public static string PluginsTab => Text("FwAvaloniaDialogs.PluginsTab");
		public static string PrivacyTab => Text("FwAvaloniaDialogs.PrivacyTab");
		public static string UpdatesTab => Text("FwAvaloniaDialogs.UpdatesTab");

		// General tab.
		public static string UiLanguageLabel => Text("FwAvaloniaDialogs.UiLanguageLabel");
		public static string UiLanguageNote => Text("FwAvaloniaDialogs.UiLanguageNote");
		public static string LexicalEditUiLabel => Text("FwAvaloniaDialogs.LexicalEditUiLabel");
		public static string UiModeLegacy => Text("FwAvaloniaDialogs.UiModeLegacy");
		public static string UiModeNew => Text("FwAvaloniaDialogs.UiModeNew");
		// Beta warning under the UI-mode chooser — parity with WinForms LexOptionsDlg m_uiModeBetaWarning.
		public static string UiModeBetaWarning => Text("FwAvaloniaDialogs.UiModeBetaWarning");
		// The "Manage Individual Features..." button shown in the Lexical Edit UI section when New is
		// selected — parity with the WinForms LexOptionsDlg m_manageFeaturesButton. Opens the
		// LexicalEditFeatureManagerDialog. (Replaces the former live "Apply" button, removed to keep the
		// Avalonia Options dialog in parity with LexOptionsDlg — the mode applies on OK, not a live button.)
		public static string ManageIndividualFeatures => Text("FwAvaloniaDialogs.ManageIndividualFeatures");
		public static string AutoOpenLastProject => Text("FwAvaloniaDialogs.AutoOpenLastProject");

		// Plugins tab.
		public static string PluginsUnavailableNote => Text("FwAvaloniaDialogs.PluginsUnavailableNote");

		// Privacy tab.
		public static string PrivacyNote => Text("FwAvaloniaDialogs.PrivacyNote");
		public static string OkToPing => Text("FwAvaloniaDialogs.OkToPing");

		// Updates tab.
		public static string AutoUpdate => Text("FwAvaloniaDialogs.AutoUpdate");
		public static string UpdateChannelLabel => Text("FwAvaloniaDialogs.UpdateChannelLabel");

		public static string Ok => Text("FwAvaloniaDialogs.OK");
		public static string Cancel => Text("FwAvaloniaDialogs.Cancel");
		public static string Help => Text("FwAvaloniaDialogs.Help");

		// Button mnemonics (A11Y-01): dialog-local strings carrying the Avalonia '_' access-key marker, kept
		// SEPARATE from the shared Common.OK/Cancel/Help ids above (which the WinForms UI consumes with '&').
		// The mnemonic letter is part of the localizable string, so translators control it; '__' renders a
		// literal underscore. Consumed via an explicit <AccessText> in the button so the access key is parsed
		// regardless of the control theme.
		public static string OkMnemonic => Text("FwAvaloniaDialogs.OkMnemonic");
		public static string CancelMnemonic => Text("FwAvaloniaDialogs.CancelMnemonic");
		public static string HelpMnemonic => Text("FwAvaloniaDialogs.HelpMnemonic");

		// Reusable chooser dialog (Phase 1).
		public static string ChooserEmptyOption => Text("FwAvaloniaDialogs.ChooserEmptyOption");
		public static string ChooserMustSelect => Text("FwAvaloniaDialogs.ChooserMustSelect");
		public static string ChooserSearchPrompt => Text("FwAvaloniaDialogs.ChooserSearchPrompt");

		// Manage Individual Features dialog (opened from Options' Lexical Edit UI section when New is selected).
		public static string FeatureManagerTitle => Text("FwAvaloniaDialogs.FeatureManagerTitle");
		public static string FeatureManagerSearchWatermark => Text("FwAvaloniaDialogs.FeatureManagerSearchWatermark");
		public static string FeatureManagerSelectAll => Text("FwAvaloniaDialogs.FeatureManagerSelectAll");
		public static string FeatureManagerDeselectAll => Text("FwAvaloniaDialogs.FeatureManagerDeselectAll");

		// Reusable Insert Entry dialog (Phase 1).
		public static string InsertEntryTitle => Text("FwAvaloniaDialogs.InsertEntryTitle");
		public static string InsertEntryCreate => Text("FwAvaloniaDialogs.InsertEntryCreate");
		// A11Y-01 mnemonic variant ('_' access-key marker) for the Insert Entry primary button.
		public static string InsertEntryCreateMnemonic => Text("FwAvaloniaDialogs.InsertEntryCreateMnemonic");
		public static string InsertEntryLexemeFormLabel => Text("FwAvaloniaDialogs.InsertEntryLexemeFormLabel");
		public static string InsertEntryMorphTypeLabel => Text("FwAvaloniaDialogs.InsertEntryMorphTypeLabel");
		public static string InsertEntryGlossLabel => Text("FwAvaloniaDialogs.InsertEntryGlossLabel");
		public static string InsertEntryLexFormNotEmpty => Text("FwAvaloniaDialogs.InsertEntryLexFormNotEmpty");
		// Insert Entry duplicate-detection "matching entries" pane (P2). Seeded from the canonical legacy
		// InsertEntryDlg.resx wording (the m_matchingEntriesGroupBox caption "Similar Entries" + the
		// m_linkSimilarEntry link "Go to similar entry").
		public static string InsertEntryMatchingEntriesLabel => Text("FwAvaloniaDialogs.InsertEntryMatchingEntriesLabel");
		public static string InsertEntryUseSelectedEntry => Text("FwAvaloniaDialogs.InsertEntryUseSelectedEntry");
		// The grammatical-info section caption in the Insert Entry dialog (the legacy InsertEntryDlg m_msaGroupBox
		// group-box caption "Grammatical Information").
		public static string InsertEntryGrammaticalInfoLabel => Text("FwAvaloniaDialogs.InsertEntryGrammaticalInfoLabel");

		// Find/Replace pattern-setup dialog (Find/Replace Phase 1, bulk replace).
		public static string FindReplaceTitle => Text("FwAvaloniaDialogs.FindReplaceTitle");
		public static string FindReplaceFindLabel => Text("FwAvaloniaDialogs.FindReplaceFindLabel");
		public static string FindReplaceReplaceLabel => Text("FwAvaloniaDialogs.FindReplaceReplaceLabel");
		public static string FindReplaceMatchCase => Text("FwAvaloniaDialogs.FindReplaceMatchCase");
		public static string FindReplaceMatchDiacritics => Text("FwAvaloniaDialogs.FindReplaceMatchDiacritics");
		public static string FindReplaceMatchWholeWord => Text("FwAvaloniaDialogs.FindReplaceMatchWholeWord");
		public static string FindReplaceMatchWritingSystem => Text("FwAvaloniaDialogs.FindReplaceMatchWritingSystem");
		public static string FindReplaceUseRegex => Text("FwAvaloniaDialogs.FindReplaceUseRegex");
		public static string FindReplaceFindEmpty => Text("FwAvaloniaDialogs.FindReplaceFindEmpty");
		public static string FindReplaceInvalidRegex => Text("FwAvaloniaDialogs.FindReplaceInvalidRegex");

		// Configure-Columns dialog (Avalonia browse, P1: show/hide/reorder).
		public static string ConfigureColumnsTitle => Text("FwAvaloniaDialogs.ConfigureColumnsTitle");
		public static string ConfigureColumnsAvailableLabel => Text("FwAvaloniaDialogs.ConfigureColumnsAvailableLabel");
		public static string ConfigureColumnsShownLabel => Text("FwAvaloniaDialogs.ConfigureColumnsShownLabel");
		public static string ConfigureColumnsAdd => Text("FwAvaloniaDialogs.ConfigureColumnsAdd");
		public static string ConfigureColumnsRemove => Text("FwAvaloniaDialogs.ConfigureColumnsRemove");
		public static string ConfigureColumnsMoveUp => Text("FwAvaloniaDialogs.ConfigureColumnsMoveUp");
		public static string ConfigureColumnsMoveDown => Text("FwAvaloniaDialogs.ConfigureColumnsMoveDown");
		public static string ConfigureColumnsNeedsAColumn => Text("FwAvaloniaDialogs.ConfigureColumnsNeedsAColumn");

		// Message-box buttons (shared by FwMessageBox / MessageBoxViewModel) and severity-icon accessible names.
		public static string Yes => Text("FwAvaloniaDialogs.Yes");
		public static string No => Text("FwAvaloniaDialogs.No");
		public static string IconInformation => Text("FwAvaloniaDialogs.IconInformation");
		public static string IconWarning => Text("FwAvaloniaDialogs.IconWarning");
		public static string IconError => Text("FwAvaloniaDialogs.Error");
		public static string IconQuestion => Text("FwAvaloniaDialogs.IconQuestion");

		// Reusable entry-search ("go") dialog — the EntryGoDlg/BaseGoDlg family (Merge Entry is the first consumer).
		public static string EntryGoMustSelect => Text("FwAvaloniaDialogs.EntryGoMustSelect");
		public static string EntryGoSearchWatermark => Text("FwAvaloniaDialogs.EntryGoSearchWatermark");
		public static string EntryGoResultsLabel => Text("FwAvaloniaDialogs.EntryGoResultsLabel");

		// Merge Entry consumer of the entry-search dialog (legacy MergeEntryDlg / EntryGoDlg wording).
		public static string MergeTitle => Text("FwAvaloniaDialogs.Merge.Title");
		public static string MergeOkButton => Text("FwAvaloniaDialogs.Merge.OkButton");

		// Add Allomorph consumer of the entry-search dialog (legacy AddAllomorphDlg / LexTextControls wording).
		public static string AddAllomorphTitle => Text("FwAvaloniaDialogs.AddAllomorph.Title");
		public static string AddAllomorphOkButton => Text("FwAvaloniaDialogs.AddAllomorph.OkButton");
		public static string AddAllomorphUndo => Text("FwAvaloniaDialogs.AddAllomorph.Undo");
		public static string AddAllomorphRedo => Text("FwAvaloniaDialogs.AddAllomorph.Redo");

		// Link Entry or Sense consumer of the entry-search dialog (legacy LinkEntryOrSenseDlg wording).
		public static string LinkEntryOrSenseTitle => Text("FwAvaloniaDialogs.LinkEntryOrSense.Title");

		// Link Allomorph consumer of the entry-search dialog (legacy LinkAllomorphDlg wording).
		public static string LinkAllomorphTitle => Text("FwAvaloniaDialogs.LinkAllomorph.Title");

		// Link MSA consumer of the entry-search dialog (legacy LinkMSADlg wording).
		public static string LinkMsaTitle => Text("FwAvaloniaDialogs.LinkMsa.Title");

		// "Filter For…" pattern-match dialog (browse column filter parity). Seed text matches the canonical
		// legacy SimpleMatchDlg wording so the English fallback is identical and translation memory carries over.
		public static string FilterForTitle => Text("FwAvaloniaDialogs.FilterFor.Title");
		public static string FilterForMatchLabel => Text("FwAvaloniaDialogs.FilterFor.MatchLabel");
		public static string FilterForAnywhere => Text("FwAvaloniaDialogs.FilterFor.Anywhere");
		public static string FilterForAtStart => Text("FwAvaloniaDialogs.FilterFor.AtStart");
		public static string FilterForAtEnd => Text("FwAvaloniaDialogs.FilterFor.AtEnd");
		public static string FilterForWholeItem => Text("FwAvaloniaDialogs.FilterFor.WholeItem");
		public static string FilterForRegex => Text("FwAvaloniaDialogs.FilterFor.Regex");
		public static string FilterForMatchCase => Text("FwAvaloniaDialogs.FilterFor.MatchCase");
		public static string FilterForEmpty => Text("FwAvaloniaDialogs.FilterFor.Empty");
		public static string FilterForInvalidRegex => Text("FwAvaloniaDialogs.FilterFor.InvalidRegex");

		// "Restrict Date…" date-range dialog (browse date/genDate column filter parity). The relation labels seed
		// from the canonical legacy SimpleDateMatchDlg type-combo wording so the English fallback is identical and
		// translation memory carries over.
		public static string RestrictDateTitle => Text("FwAvaloniaDialogs.RestrictDate.Title");
		public static string RestrictDateRelationLabel => Text("FwAvaloniaDialogs.RestrictDate.RelationLabel");
		public static string RestrictDateDateLabel => Text("FwAvaloniaDialogs.RestrictDate.DateLabel");
		public static string RestrictDateEndLabel => Text("FwAvaloniaDialogs.RestrictDate.EndLabel");
		public static string RestrictDateOn => Text("FwAvaloniaDialogs.RestrictDate.On");
		public static string RestrictDateNotOn => Text("FwAvaloniaDialogs.RestrictDate.NotOn");
		public static string RestrictDateOnOrBefore => Text("FwAvaloniaDialogs.RestrictDate.OnOrBefore");
		public static string RestrictDateOnOrAfter => Text("FwAvaloniaDialogs.RestrictDate.OnOrAfter");
		public static string RestrictDateBetween => Text("FwAvaloniaDialogs.RestrictDate.Between");
		public static string RestrictDateNoDate => Text("FwAvaloniaDialogs.RestrictDate.NoDate");
		public static string RestrictDateRangeInverted => Text("FwAvaloniaDialogs.RestrictDate.RangeInverted");

		// "Choose…" list-choice chooser title (browse chooser column filter parity).
		public static string FilterChooseTitle => Text("FwAvaloniaDialogs.FilterChoose.Title");
		public static string FilterChoosePrompt => Text("FwAvaloniaDialogs.FilterChoose.Prompt");

		// Link Entry or Sense entry/sense toggle (the legacy LinkEntryOrSenseDlg m_rbEntry / m_rbSense radios). Seed
		// text matches the canonical legacy LinkEntryOrSenseDlg.resx wording (ampersand accelerators dropped — the
		// Avalonia toggle does not use WinForms-style mnemonics) so the translation memory carries over.
		public static string LinkEntryOrSenseEntryRadio => Text("FwAvaloniaDialogs.LinkEntryOrSense.EntryRadio");
		public static string LinkEntryOrSenseSenseRadio => Text("FwAvaloniaDialogs.LinkEntryOrSense.SenseRadio");

		// Commit-on-select confirmation for the (semi-destructive) Merge consumer: shown AFTER the user commits a
		// survivor selection and BEFORE the merge runs (the other Add/Link* consumers act immediately, no confirm).
		// Seeded from the canonical legacy LexTextControls.ksEntryXMergedIntoY wording ("Entry \"{0}\" will be merged
		// into \"{1}\",{2}resulting in one entry.") so the English fallback matches and the translation memory carries
		// over; {0} is the current entry, {1} the chosen survivor, {2} a newline.
		public static string MergeConfirm => Text("FwAvaloniaDialogs.Merge.Confirm");

		// MSA (grammatical-info) group box — the FwMsaGroupBox composite mirroring the WinForms MSAGroupBox.
		// Seed text matches the canonical legacy LexTextControls.resx / MSAGroupBox.resx wording (ampersand
		// mnemonics dropped — the Avalonia controls don't use WinForms-style accelerators) so the English
		// fallback is identical and translation memory carries over.

		// The Affix Type picker label + its three options (MSAGroupBox.resx m_lAfxType "Affix &Type:" and the
		// combo items seeded from LexTextControls.ksNotSure / ksInflectional / ksDerivational).
		public static string MsaAffixTypeLabel => Text("FwAvaloniaDialogs.Msa.AffixTypeLabel");
		public static string MsaAffixTypeNotSure => Text("FwAvaloniaDialogs.Msa.AffixTypeNotSure");
		public static string MsaAffixTypeInflectional => Text("FwAvaloniaDialogs.Msa.AffixTypeInflectional");
		public static string MsaAffixTypeDerivational => Text("FwAvaloniaDialogs.Msa.AffixTypeDerivational");

		// The Main-POS field label, which the WinForms box retitles per MsaType (LexTextControls.ksCategor_y for
		// stem/root, ksAttachesToCategor_y for every affix type).
		public static string MsaCategoryLabel => Text("FwAvaloniaDialogs.Msa.CategoryLabel");
		public static string MsaAttachesToCategoryLabel => Text("FwAvaloniaDialogs.Msa.AttachesToCategoryLabel");

		// The Slot/Secondary-POS field label, which the WinForms box retitles per MsaType
		// (LexTextControls.ks_FillsSlot for inflectional, ksC_hangesToCategory for derivational).
		public static string MsaFillsSlotLabel => Text("FwAvaloniaDialogs.Msa.FillsSlotLabel");
		public static string MsaChangesToCategoryLabel => Text("FwAvaloniaDialogs.Msa.ChangesToCategoryLabel");

		// Create-a-new-Part-of-Speech catalog chooser (MSA-port Stage 4) — the inline "Create a new Part of
		// Speech..." affordance opens the master-category (GOLDEtic) catalog as a hierarchical single-select
		// ChooserDialog. Seed text matches the canonical legacy MasterCategoryListDlg.resx wording ($this.Text
		// "Add from Catalog" and the label1 instruction prompt) so the English fallback is identical and the
		// translation memory carries over. APPEND-ONLY.
		public static string CreatePosTitle => Text("FwAvaloniaDialogs.CreatePos.Title");
		public static string CreatePosPrompt => Text("FwAvaloniaDialogs.CreatePos.Prompt");

		// Add New Sense dialog (MSA-port Stage 5) — the Avalonia replacement for the WinForms AddNewSenseDlg
		// (a read-only citation form + an editable gloss + the grammatical-info group box -> a new ILexSense).
		// Seed text matches the canonical legacy AddNewSenseDlg.resx wording (label2 "Citation Form:" and label1
		// "Gloss:", ampersand mnemonics + trailing colons dropped — the Avalonia labels carry neither) and the
		// shared LexTextControls.ksFillInGloss / ksMissingInformation OK-gate messages, so the English fallback is
		// identical and the translation memory carries over. APPEND-ONLY.
		public static string AddNewSenseTitle => Text("FwAvaloniaDialogs.AddNewSense.Title");
		public static string AddNewSenseCitationFormLabel => Text("FwAvaloniaDialogs.AddNewSense.CitationFormLabel");
		public static string AddNewSenseGlossLabel => Text("FwAvaloniaDialogs.AddNewSense.GlossLabel");
		public static string AddNewSenseGrammaticalInfoLabel => Text("FwAvaloniaDialogs.AddNewSense.GrammaticalInfoLabel");
		// The legacy OK gate when the gloss is empty (LexTextControls.ksFillInGloss "Please fill in the gloss.").
		public static string AddNewSenseFillInGloss => Text("FwAvaloniaDialogs.AddNewSense.FillInGloss");

		// Create New Grammatical Info. dialog (MSA-port Stage 5) — the Avalonia replacement for the WinForms
		// MsaCreatorDlg (a read-only lexical entry + read-only senses + the grammatical-info group box -> an MSA).
		// Seed text matches the canonical legacy MsaCreatorDlg.resx wording ($this.Text "Create New Grammatical
		// Info.", label1 "Lexical Entry:" and label2 "Senses:", trailing colons dropped) so the English fallback is
		// identical and the translation memory carries over. APPEND-ONLY.
		public static string MsaCreatorTitle => Text("FwAvaloniaDialogs.MsaCreator.Title");
		public static string MsaCreatorLexicalEntryLabel => Text("FwAvaloniaDialogs.MsaCreator.LexicalEntryLabel");
		public static string MsaCreatorSensesLabel => Text("FwAvaloniaDialogs.MsaCreator.SensesLabel");
		public static string MsaCreatorGrammaticalInfoLabel => Text("FwAvaloniaDialogs.MsaCreator.GrammaticalInfoLabel");
		public static string MsaCreatorCreate => Text("FwAvaloniaDialogs.MsaCreator.Create");

		// The inflection-class picker label (MSA-port Stage 6) shown for the stem/root MSA — the inflection class
		// of the selected main POS (the legacy InsertEntryDlg inflection-class affordance, IMoStemMsa.InflectionClassRA,
		// driven by InflectionClassPopupTreeManager). Seed text matches the canonical field label "Inflection Class"
		// (the m3 InflectionClass field label / DataTree "Inflection Class" slice). APPEND-ONLY.
		public static string MsaInflectionClassLabel => Text("FwAvaloniaDialogs.Msa.InflectionClassLabel");
		// The "<None>" row in the inflection-class picker (empty selection is valid). Seeded from the shared
		// "<None>" / not-sure wording the WinForms inflection-class tree uses (AddNotSureItem).
		public static string MsaInflectionClassNone => Text("FwAvaloniaDialogs.Msa.InflectionClassNone");

		// Complex Form Type picker in the Insert Entry dialog — the Avalonia parity of the WinForms
		// InsertEntryDlg m_cbComplexFormType combo (LT-21666). Seed text matches the canonical legacy wording:
		// the field caption (InsertEntryDlg.resx m_complexTypeLabel "Complex Form Type") and the leading
		// "<Not Applicable>" item (LexTextControls.ksNotApplicable). APPEND-ONLY.
		public static string InsertEntryComplexFormTypeLabel => Text("FwAvaloniaDialogs.InsertEntry.ComplexFormTypeLabel");
		public static string InsertEntryComplexFormTypeNotApplicable => Text("FwAvaloniaDialogs.InsertEntry.ComplexFormTypeNotApplicable");

		// Feature-structure editor (FsFeatStruc tree editor, Phase-1 §19b Stage 1) — the Avalonia replacement
		// for the WinForms FeatureStructureTreeView / MsaInflectionFeatureListDlg / PhonologicalFeatureChooserDlg.
		// Seed text matches the canonical legacy wording so the English fallback is identical and the
		// translation memory carries over: the unspecified value radio mirrors the legacy "None of the above"
		// (LexTextControls.ksNoneOfTheAbove), shortened to "<None>" for the compact new-view tree; the create
		// affordances mirror the MasterInflectionFeatureListDlg "create a feature" / feature-system "add value"
		// flows. APPEND-ONLY.

		/// <summary>The unspecified value radio leading/trailing a closed feature's values (legacy "None of the above" / AddNotSureItem). Selecting it clears that feature's assignment.</summary>
		public static string FeatureEditorNone => Text("FwAvaloniaDialogs.FeatureEditor.None");

		/// <summary>Inline affordance at the bottom of the feature tree that raises CreateNewFeatureRequested (legacy MasterInflectionFeatureListDlg create-feature link).</summary>
		public static string FeatureEditorCreateFeature => Text("FwAvaloniaDialogs.FeatureEditor.CreateFeature");

		/// <summary>"Add a value to {0}..." — the per-closed-feature affordance that raises CreateNewValueRequested ({0} = the feature name).</summary>
		public static string FeatureEditorCreateValueFormat => Text("FwAvaloniaDialogs.FeatureEditor.CreateValueFormat");

		/// <summary>Accessible name of the feature-structure editor control.</summary>
		public static string FeatureEditorName => Text("FwAvaloniaDialogs.FeatureEditor.Name");

		// MSA inflection-feature editor (Phase-1 §19b Stage 2) — the inflection-feature column the FwMsaGroupBox
		// shows for inflectional/derivational MSAs (where the WinForms box's "Inflection Features" affordance opens
		// MsaInflectionFeatureListDlg over IMoInflAffMsa.InflFeatsOA / IMoDerivAffMsa.FromMsFeaturesOA). Seed text
		// matches the canonical legacy field caption ("Inflection Features"). APPEND-ONLY.
		public static string MsaInflectionFeaturesLabel => Text("FwAvaloniaDialogs.Msa.InflectionFeaturesLabel");

		// Standalone feature-structure chooser dialogs (Phase-1 §19b Stage 3) — the Avalonia replacements for the
		// WinForms MsaInflectionFeatureListDlg (assign inflection feature values to an MSA's IFsFeatStruc) and
		// PhonologicalFeatureChooserDlg (the phonological feature system). Each hosts the shared
		// FwFeatureStructureEditor over OK/Cancel/Help. Seed text matches the canonical legacy wording (the dialog
		// captions + prompts from the StringTable FeatureChooser group) so the English fallback is identical and the
		// translation memory carries over. APPEND-ONLY.
		public static string InflectionFeatureChooserTitle => Text("FwAvaloniaDialogs.FeatureChooser.InflectionTitle");
		public static string InflectionFeatureChooserPrompt => Text("FwAvaloniaDialogs.FeatureChooser.InflectionPrompt");
		public static string PhonologicalFeatureChooserTitle => Text("FwAvaloniaDialogs.FeatureChooser.PhonologicalTitle");
		public static string PhonologicalFeatureChooserPrompt => Text("FwAvaloniaDialogs.FeatureChooser.PhonologicalPrompt");

		// Create-a-new-feature dialog (Phase-1 §19b Stage 3) — the Avalonia replacement for the
		// MasterInflectionFeatureListDlg / MasterPhonologicalFeatureListDlg blank-create affordance (create a new
		// closed feature in the feature system, naming it; the phonological variant auto-creates the +/- values).
		// Seed text matches the canonical legacy create-feature dialog wording. APPEND-ONLY.
		public static string CreateFeatureTitle => Text("FwAvaloniaDialogs.CreateFeature.Title");
		public static string CreateFeatureNameLabel => Text("FwAvaloniaDialogs.CreateFeature.NameLabel");
		public static string CreateFeatureAbbrLabel => Text("FwAvaloniaDialogs.CreateFeature.AbbrLabel");
		public static string CreateFeatureNameRequired => Text("FwAvaloniaDialogs.CreateFeature.NameRequired");

		// Create-a-new-value dialog (Phase-1 §19b Stage 3) — the Avalonia replacement for adding a symbolic value to
		// an existing closed feature (the feature-system value editor flow the WinForms "add value" reached).
		// APPEND-ONLY.
		public static string CreateValueTitle => Text("FwAvaloniaDialogs.CreateValue.Title");
		public static string CreateValueNameLabel => Text("FwAvaloniaDialogs.CreateValue.NameLabel");
		public static string CreateValueAbbrLabel => Text("FwAvaloniaDialogs.CreateValue.AbbrLabel");
		public static string CreateValueNameRequired => Text("FwAvaloniaDialogs.CreateValue.NameRequired");

		// Picture-properties dialog (§19d) — the Avalonia replacement for the WinForms PicturePropertiesDialog
		// (file pick + caption/description/license/creator). Seed text mirrors the canonical legacy picture
		// metadata labels. APPEND-ONLY.
		public static string PicturePropertiesTitle => Text("FwAvaloniaDialogs.PictureProperties.Title");
		public static string PicturePropertiesCaptionLabel => Text("FwAvaloniaDialogs.PictureProperties.CaptionLabel");
		public static string PicturePropertiesDescriptionLabel => Text("FwAvaloniaDialogs.PictureProperties.DescriptionLabel");
		public static string PicturePropertiesLicenseLabel => Text("FwAvaloniaDialogs.PictureProperties.LicenseLabel");
		public static string PicturePropertiesCreatorLabel => Text("FwAvaloniaDialogs.PictureProperties.CreatorLabel");
		public static string PicturePropertiesImageLabel => Text("FwAvaloniaDialogs.PictureProperties.ImageLabel");
		public static string PicturePropertiesChooseImage => Text("FwAvaloniaDialogs.PictureProperties.ChooseImage");
		public static string PicturePropertiesNoFile => Text("FwAvaloniaDialogs.PictureProperties.NoFile");
		public static string PicturePropertiesInsert => Text("FwAvaloniaDialogs.PictureProperties.Insert");
	}
}
