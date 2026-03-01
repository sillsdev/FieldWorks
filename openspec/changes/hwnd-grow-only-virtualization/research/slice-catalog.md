# Comprehensive Slice Subclass Catalog

This document catalogs all 73 Slice subclasses in the FieldWorks codebase, their inheritance relationships, the controls they embed, their HWND state classifications, and whether they override the lazy-initialization methods (`BecomeRealInPlace`/`BecomeReal`).

## Inheritance Tree

```
Slice (UserControl)
├── ViewSlice
│   ├── ViewPropertySlice
│   │   ├── StringSlice
│   │   │   └── BasicIPASymbolSlice
│   │   ├── MultiStringSlice
│   │   ├── StTextSlice
│   │   ├── GhostStringSlice
│   │   ├── AtomicRefTypeAheadSlice
│   │   ├── ReversalIndexEntrySlice
│   │   └── PhEnvStrRepresentationSlice
│   ├── SummarySlice
│   ├── AudioVisualSlice
│   ├── MSADlgLauncherSlice
│   ├── PhonologicalFeatureListDlgLauncherSlice
│   ├── InterlinearSlice
│   ├── InflAffixTemplateSlice
│   ├── RuleFormulaSlice
│   │   ├── RegRuleFormulaSlice
│   │   ├── MetaRuleFormulaSlice
│   │   └── AffixRuleFormulaSlice
│   ├── MultiLevelConc.ConcSlice
│   └── TwoLevelConc.ConcSlice
├── FieldSlice (abstract)
│   ├── ReferenceSlice (abstract)
│   │   ├── AtomicReferenceSlice
│   │   │   ├── CustomAtomicReferenceSlice (abstract)
│   │   │   │   ├── AdhocCoProhibAtomicReferenceSlice
│   │   │   │   │   └── AdhocCoProhibAtomicReferenceDisabledSlice
│   │   │   │   ├── LexReferencePairSlice
│   │   │   │   └── LexReferenceTreeRootSlice
│   │   │   ├── AtomicReferenceDisabledSlice
│   │   │   ├── PossibilityAtomicReferenceSlice
│   │   │   │   └── MorphTypeAtomicReferenceSlice
│   │   │   ├── InflMSAReferenceSlice
│   │   │   └── DerivMSAReferenceSlice
│   │   ├── ReferenceVectorSlice
│   │   │   ├── CustomReferenceVectorSlice (abstract)
│   │   │   │   ├── AdhocCoProhibVectorReferenceSlice
│   │   │   │   │   └── AdhocCoProhibVectorReferenceDisabledSlice
│   │   │   │   ├── LexReferenceCollectionSlice
│   │   │   │   ├── LexReferenceSequenceSlice
│   │   │   │   ├── LexReferenceUnidirectionalSlice
│   │   │   │   ├── LexReferenceTreeBranchesSlice
│   │   │   │   ├── EntrySequenceReferenceSlice
│   │   │   │   ├── RevEntrySensesCollectionReferenceSlice
│   │   │   │   ├── RecordReferenceVectorSlice
│   │   │   │   └── RoledParticipantsSlice
│   │   │   ├── PossibilityReferenceVectorSlice
│   │   │   │   └── SemanticDomainReferenceVectorSlice
│   │   │   └── ReferenceVectorDisabledSlice
│   │   └── PhoneEnvReferenceSlice
│   ├── CheckboxSlice
│   │   └── CheckboxRefreshSlice
│   ├── DateSlice
│   ├── IntegerSlice
│   ├── GenDateSlice
│   ├── EnumComboSlice
│   ├── MSAReferenceComboBoxSlice
│   ├── ReferenceComboBoxSlice
│   ├── AtomicReferencePOSSlice
│   │   └── AutomicReferencePOSDisabledSlice
│   └── GhostReferenceVectorSlice
├── MessageSlice (DetailControls)
├── MessageSlice (LexEd/Chorus)
├── ImageSlice
├── CommandSlice
├── LexReferenceMultiSlice
├── GhostLexRefSlice
├── MsaInflectionFeatureListDlgLauncherSlice
│   └── FeatureSystemInflectionFeatureListDlgLauncherSlice
├── DummyObjectSlice (DataTree inner class)
└── MultiLevelConc.DummyConcSlice
```

## Statistics

| Category | Count |
|----------|-------|
| Total Slice classes | 73 (including abstract, inner classes, disabled variants) |
| Concrete, commonly-used | ~50 |
| Base/abstract classes | 7 (Slice, ViewSlice, ViewPropertySlice, FieldSlice, ReferenceSlice, CustomAtomicReferenceSlice, CustomReferenceVectorSlice) |
| Override BecomeRealInPlace | 1 (ViewSlice — inherited by all ViewSlice descendants) |
| Override BecomeReal | 2 (DummyObjectSlice, MultiLevelConc.DummyConcSlice) |
| Have RootSite (Views engine) in HWND tree | ~40+ |
| WinForms-only controls (no RootSite) | ~8 |
| No control at all | 3 |

## HWND State Classification

### Full RootSite (editable text, selection, cursor)

These slices embed a `RootSiteControl` (or subclass) directly as their content control. They have full Views-engine text editing capability: selections, cursor positioning, undo, complex script shaping.

| Class | File | Content Control | BecomeRealInPlace |
|-------|------|-----------------|-------------------|
| StringSlice | `Src/Common/Controls/DetailControls/StringSlice.cs` | `StringSliceView` (RootSiteControl) | Yes (inherited) |
| BasicIPASymbolSlice | `Src/LexText/Morphology/BasicIPASymbolSlice.cs` | `StringSliceView` (inherited) | Yes (inherited) |
| MultiStringSlice | `Src/Common/Controls/DetailControls/MultiStringSlice.cs` | `LabeledMultiStringView` (Panel + RootSiteControl) | Yes (inherited) |
| StTextSlice | `Src/Common/Controls/DetailControls/StTextSlice.cs` | `StTextView` (multi-paragraph RootSite) | Yes (inherited) |
| GhostStringSlice | `Src/Common/Controls/DetailControls/GhostStringSlice.cs` | `GhostStringSliceView` (RootSiteControl) | Yes (inherited) |
| AtomicRefTypeAheadSlice | `Src/Common/Controls/DetailControls/AtomicRefTypeAheadSlice.cs` | `AtomicRefTypeAheadView` (RootSiteControl) | Yes (inherited) |
| ReversalIndexEntrySlice | `Src/LexText/Lexicon/ReversalIndexEntrySlice.cs` | `ReversalIndexEntrySliceView` (RootSiteControl) | Yes (inherited) |
| PhEnvStrRepresentationSlice | `Src/LexText/Morphology/PhEnvStrRepresentationSlice.cs` | `StringRepSliceView` (RootSiteControl) | Yes (inherited) |
| SummarySlice | `Src/Common/Controls/DetailControls/SummarySlice.cs` | Panel(SummaryXmlView + ExpandCollapseButton + SummaryCommandControl) | Yes (inherited) |
| InterlinearSlice | `Src/LexText/Morphology/InterlinearSlice.cs` | `AnalysisInterlinearRs` (RootSite) | Yes (inherited) |
| InflAffixTemplateSlice | `Src/LexText/Morphology/InflAffixTemplateSlice.cs` | `InflAffixTemplateControl` (RootSiteControl) | Yes (inherited) |
| RuleFormulaSlice | `Src/LexText/Morphology/RuleFormulaSlice.cs` | `RuleFormulaControl` (RootSite + InsertionControl) | Yes (inherited) |
| RegRuleFormulaSlice | `Src/LexText/Morphology/RegRuleFormulaSlice.cs` | `RegRuleFormulaControl` | Yes (inherited) |
| MetaRuleFormulaSlice | `Src/LexText/Morphology/MetaRuleFormulaSlice.cs` | `MetaRuleFormulaControl` | Yes (inherited) |
| AffixRuleFormulaSlice | `Src/LexText/Morphology/AffixRuleFormulaSlice.cs` | `AffixRuleFormulaControl` | Yes (inherited) |
| MultiLevelConc.ConcSlice | `Src/Common/Controls/DetailControls/MultiLevelConc.cs` | `ConcView` (RootSiteControl) | Yes (inherited) |
| TwoLevelConc.ConcSlice | `Src/Common/Controls/DetailControls/TwoLevelConc.cs` | `ConcView` (RootSite) | Yes (inherited) |

### RootSite Inside ButtonLauncher

These slices embed a `ButtonLauncher` subclass which itself contains an inner `RootSite` view + a chooser `Button`. HWND count: Slice(1) + SplitContainer(3) + SliceTreeNode(1) + ButtonLauncher(1) + inner RootSite(1) + Button(1) = **8 HWNDs**.

| Class | File | Launcher |
|-------|------|----------|
| AudioVisualSlice | `Src/Common/Controls/DetailControls/AudioVisualSlice.cs` | AudioVisualLauncher |
| MSADlgLauncherSlice | `Src/LexText/Lexicon/MSADlgLauncherSlice.cs` | MSADlgLauncher |
| PhonologicalFeatureListDlgLauncherSlice | `Src/LexText/Lexicon/PhonologicalFeatureListDlgLauncherSlice.cs` | PhonologicalFeatureListDlgLauncher |
| MsaInflectionFeatureListDlgLauncherSlice | `Src/LexText/Lexicon/MsaInflectionFeatureListDlgLauncherSlice.cs` | MsaInflectionFeatureListDlgLauncher |
| FeatureSystemInflectionFeatureListDlgLauncherSlice | `Src/LexText/Lexicon/MsaInflectionFeatureListDlgLauncherSlice.cs` | FeatureSystemInflectionFeatureListDlgLauncher |
| GhostLexRefSlice | `Src/LexText/Lexicon/GhostLexRefSlice.cs` | GhostLexRefLauncher |
| AtomicReferenceSlice | `Src/Common/Controls/DetailControls/AtomicReferenceSlice.cs` | AtomicReferenceLauncher |
| AtomicReferenceDisabledSlice | `Src/Common/Controls/DetailControls/AtomicReferenceSlice.cs` | AtomicReferenceLauncher |
| PossibilityAtomicReferenceSlice | `Src/Common/Controls/DetailControls/PossibilityAtomicReferenceSlice.cs` | PossibilityAtomicReferenceLauncher |
| MorphTypeAtomicReferenceSlice | `Src/Common/Controls/DetailControls/MorphTypeAtomicReferenceSlice.cs` | MorphTypeAtomicLauncher |
| InflMSAReferenceSlice | `Src/Common/Controls/DetailControls/InflMSAReferenceSlice.cs` | AtomicReferenceLauncher |
| DerivMSAReferenceSlice | `Src/Common/Controls/DetailControls/DerivMSAReferenceSlice.cs` | AtomicReferenceLauncher |
| AdhocCoProhibAtomicReferenceSlice | `Src/LexText/Morphology/AdhocCoProhibAtomicReferenceSlice.cs` | AdhocCoProhibAtomicLauncher |
| AdhocCoProhibAtomicReferenceDisabledSlice | `Src/LexText/Morphology/AdhocCoProhibAtomicReferenceSlice.cs` | (same, disabled) |
| LexReferencePairSlice | `Src/LexText/Lexicon/LexReferencePairSlice.cs` | LexReferencePairLauncher |
| LexReferenceTreeRootSlice | `Src/LexText/Lexicon/LexReferenceTreeRootSlice.cs` | LexReferenceTreeRootLauncher |
| ReferenceVectorSlice | `Src/Common/Controls/DetailControls/ReferenceVectorSlice.cs` | VectorReferenceLauncher |
| ReferenceVectorDisabledSlice | `Src/Common/Controls/DetailControls/ReferenceVectorSlice.cs` | VectorReferenceLauncher |
| PossibilityReferenceVectorSlice | `Src/Common/Controls/DetailControls/PossibilityReferenceVectorSlice.cs` | PossibilityVectorReferenceLauncher |
| SemanticDomainReferenceVectorSlice | `Src/Common/Controls/DetailControls/SemanticDomainReferenceVectorSlice.cs` | SemanticDomainReferenceLauncher |
| AdhocCoProhibVectorReferenceSlice | `Src/LexText/Morphology/AdhocCoProhibVectorReferenceSlice.cs` | AdhocCoProhibVectorLauncher |
| AdhocCoProhibVectorReferenceDisabledSlice | `Src/LexText/Morphology/AdhocCoProhibVectorReferenceSlice.cs` | (same, disabled) |
| LexReferenceCollectionSlice | `Src/LexText/Lexicon/LexReferenceCollectionSlice.cs` | LexReferenceCollectionLauncher |
| LexReferenceSequenceSlice | `Src/LexText/Lexicon/LexReferenceSequenceSlice.cs` | LexReferenceSequenceLauncher |
| LexReferenceUnidirectionalSlice | `Src/LexText/Lexicon/LexReferenceUnidirectionalSlice.cs` | LexReferenceUnidirectionalLauncher |
| LexReferenceTreeBranchesSlice | `Src/LexText/Lexicon/LexReferenceTreeBranchesSlice.cs` | LexReferenceTreeBranchesLauncher |
| EntrySequenceReferenceSlice | `Src/LexText/Lexicon/EntrySequenceReferenceSlice.cs` | EntrySequenceReferenceLauncher |
| RevEntrySensesCollectionReferenceSlice | `Src/LexText/Lexicon/RevEntrySensesCollectionReferenceSlice.cs` | RevEntrySensesCollectionReferenceLauncher |
| RecordReferenceVectorSlice | `Src/LexText/Lexicon/RecordReferenceVectorSlice.cs` | RecordReferenceVectorLauncher |
| RoledParticipantsSlice | `Src/LexText/Lexicon/RoledParticipantsSlice.cs` | VectorReferenceLauncher |
| PhoneEnvReferenceSlice | `Src/Common/Controls/DetailControls/PhoneEnvReferenceSlice.cs` | PhoneEnvReferenceLauncher |
| GenDateSlice | `Src/Common/Controls/DetailControls/GenDateSlice.cs` | GenDateLauncher |

### WinForms Controls (Editable, Has State)

These slices embed standard WinForms controls with user-editable state that would need to be saved/restored if the HWND were ever destroyed.

| Class | File | Control | State |
|-------|------|---------|-------|
| IntegerSlice | `Src/Common/Controls/DetailControls/BasicTypeSlices.cs` | TextBox | Text, cursor position |
| EnumComboSlice | `Src/Common/Controls/DetailControls/EnumComboSlice.cs` | FwOverrideComboBox | Selected index |
| MSAReferenceComboBoxSlice | `Src/Common/Controls/DetailControls/MSAReferenceComboBoxSlice.cs` | TreeCombo | Selected item, font |
| ReferenceComboBoxSlice | `Src/Common/Controls/DetailControls/ReferenceComboBoxSlice.cs` | FwComboBox | Selected index |
| AtomicReferencePOSSlice | `Src/Common/Controls/DetailControls/AtomicReferencePOSSlice.cs` | TreeCombo | Selected POS |
| AutomicReferencePOSDisabledSlice | `Src/Common/Controls/DetailControls/AtomicReferencePOSSlice.cs` | TreeCombo (disabled) | Selected POS |

### WinForms Controls (Display-Only)

These slices embed read-only WinForms controls. Their display state is derived from the database and can be fully reconstructed without save/restore.

| Class | File | Control |
|-------|------|---------|
| CheckboxSlice | `Src/Common/Controls/DetailControls/BasicTypeSlices.cs` | CheckBox (commits immediately) |
| CheckboxRefreshSlice | `Src/Common/Controls/DetailControls/BasicTypeSlices.cs` | CheckBox (+ RefreshDisplay) |
| DateSlice | `Src/Common/Controls/DetailControls/BasicTypeSlices.cs` | RichTextBox (ReadOnly) |
| ImageSlice | `Src/Common/Controls/DetailControls/ImageSlice.cs` | PictureBox |
| MessageSlice (DetailControls) | `Src/Common/Controls/DetailControls/MessageSlice.cs` | Label |

### No HWND Content

These slices either have no content control or are pure placeholders.

| Class | File | Notes |
|-------|------|-------|
| DummyObjectSlice | `Src/Common/Controls/DetailControls/DataTree.cs` (inner class) | Placeholder — calls `BecomeReal()` to expand into real slices. Already has no HWND content. |
| MultiLevelConc.DummyConcSlice | `Src/Common/Controls/DetailControls/MultiLevelConc.cs` | Placeholder — calls `BecomeReal()` to create ConcSlice. |
| LexReferenceMultiSlice | `Src/LexText/Lexicon/LexReferenceMultiSlice.cs` | Header-only; generates child slices. Panel2 is collapsed. |
| CommandSlice | `Src/Common/Controls/DetailControls/CommandSlice.cs` | Button only — stateless. |

### Special: Chorus Integration

| Class | File | Control |
|-------|------|---------|
| MessageSlice (LexEd) | `Src/LexText/Lexicon/MessageSlice.cs` | NotesBarView (Chorus notes bar) |

## Virtualization Readiness Assessment

| Category | Count | grow-only Safe? | Notes |
|----------|-------|-----------------|-------|
| Full RootSite | ~17 | **Yes** — defer HWND creation; once created, never destroy | BecomeRealInPlace already defers RootBox |
| ButtonLauncher | ~31 | **Yes** — same defer pattern | No BecomeRealInPlace override, but deferred Control creation is additive |
| WinForms Editable | ~6 | **Yes** — trivial controls | No state save/restore needed in grow-only model |
| WinForms Display | ~5 | **Yes** — stateless | Easiest to virtualize |
| No Content | ~4 | **N/A** — already virtual | DummyObjectSlice is the existing virtualization |
| Chorus | 1 | **Yes** — deferred same as others | |
