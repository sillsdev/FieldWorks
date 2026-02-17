---
last-reviewed: 2025-10-31
last-reviewed-tree: 34affdd6184eabeef6b25d21286a5b32c28de9afdac3dd3ef907deeafb42ae02
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - referenced-by
  - architecture
  - key-components
  - technology-stack
  - dependencies
  - upstream-consumes
  - downstream-consumed-by
  - interop--contracts
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# LexTextControls COPILOT summary

## Purpose
Shared UI controls and dialogs library for FLEx lexicon and text features. Provides reusable lexicon-specific dialogs (InsertEntryDlg, AddAllomorphDlg, AddNewSenseDlg), search dialogs (EntryGoDlg, ReverseGoDlg, BaseGoDlg), import wizards (LexImportWizard, CombineImportDlg), configuration dialogs (ConfigureHomographDlg, LexiconSettingsDlg), and specialized controls (FeatureStructureTreeView, InflectionClassPopupTreeManager, PopupTree). Critical shared infrastructure for Lexicon/, LexTextDll/, and other lexicon UI components. Massive 48.1K line library with 100+ source files covering comprehensive lexicon UI needs.

## Architecture
C# library (net48, OutputType=Library) organizing lexicon/text UI components for reuse. BaseGoDlg abstract base for search dialogs. InsertEntryDlg family for adding lexicon entries. LexImportWizard multi-step import process. PopupTreeManager family for hierarchical selection (InflectionClassPopupTreeManager, InflectionFeaturePopupTreeManager, PhonologicalFeaturePopupTreeManager). DataNotebook/ subfolder for notebook-style controls. Heavy integration with LCModel (ILexEntry, ILexSense, IMoForm), Views rendering, XCore framework.

## Key Components
- **InsertEntryDlg** (InsertEntryDlg.cs, 1.7K lines): Insert/find lexicon entries
  - Search and insert lexical entries
  - InsertEntrySearchEngine: Search logic
  - Entry object handling
- **BaseGoDlg** (BaseGoDlg.cs, 947 lines): Abstract base for "Go" search dialogs
  - Common infrastructure for entry/reversal search
  - Subclassed by EntryGoDlg, ReverseGoDlg
- **EntryGoDlg** (EntryGoDlg.cs, 236 lines): Entry search "Go To" dialog
  - Quick navigation to lexical entries
  - EntryGoSearchEngine: Entry search logic
  - EntryDlgListener: Event handling
- **ReverseGoDlg** (ReverseGoDlg*.cs, likely 200+ lines): Reversal index "Go To" dialog
  - Navigate reversal entries
- **AddAllomorphDlg** (AddAllomorphDlg.cs, 197 lines): Add allomorphs dialog
  - Add morpheme allomorphs to entries
  - Allomorph type selection (prefix, suffix, etc.)
- **AddNewSenseDlg** (AddNewSenseDlg.cs, 372 lines): Add new sense dialog
  - Create new lexical senses
  - Sense relationships (duplicate, new)
- **LexImportWizard** (LexImportWizard*.cs, 10K+ lines combined): Lexicon import wizard
  - Multi-step import process
  - LexImportWizardCharMarkerDlg, LexImportWizardDlg, LexImportWizardMarker, LexImportWizardMapping
  - LIFT, Toolbox, other format import
- **CombineImportDlg** (CombineImportDlg.cs, 348 lines): Import conflict resolution
  - Merge/combine imported entries with existing
  - Conflict resolution UI
- **ConfigureHomographDlg** (ConfigureHomographDlg.cs, 169 lines): Homograph numbering config
  - Configure homograph number display
  - Before/after entry, styling
- **LexiconSettingsDlg** (LexiconSettingsDlg*.cs, likely 500+ lines): Lexicon settings dialog
  - Global lexicon configuration
  - Writing systems, display options
- **FeatureStructureTreeView** (FeatureStructureTreeView.cs, 386 lines): Feature structure tree
  - Display/edit phonological/grammatical features
  - Tree view control
- **InflectionClassPopupTreeManager** (InflectionClassPopupTreeManager.cs, 107 lines): Inflection class chooser
  - Popup tree for selecting inflection classes
  - Hierarchical category selection
- **InflectionFeaturePopupTreeManager** (InflectionFeaturePopupTreeManager.cs, 170 lines): Inflection feature chooser
  - Popup tree for grammatical features
- **PhonologicalFeaturePopupTreeManager** (PhonologicalFeaturePopupTreeManager*.cs, likely 150+ lines): Phonological feature chooser
  - Popup tree for phonological features
- **PopupTree** (PopupTree*.cs, 1K+ lines): Generic popup tree control
  - Reusable popup tree infrastructure
- **MergeEntry** (MergeEntry*.cs, likely 800+ lines): Entry merging logic
  - Merge duplicate entries
  - Conflict resolution
- **MSAGroupBox** (MSAGroupBox*.cs, likely 500+ lines): Morphosyntactic analysis group box
  - MSA (category, features) selection UI
- **DataNotebook/** subfolder: Notebook-style controls
  - Tab-based interfaces for data entry
- **AddWritingSystemButton** (AddWritingSystemButton.cs, 229 lines): Add writing system button
  - UI button for adding writing systems
- **EntryObjects** (EntryObjects.cs, 208 lines): Entry object helpers
  - Utility functions for entry manipulation
- **IFwExtension** (IFwExtension.cs, 21 lines): Extension interface
  - Plugin/extension point interface
- **IPatternControl** (IPatternControl.cs, 48 lines): Pattern control interface
  - Interface for pattern-based controls

### Referenced By

- [Writing Systems](../../../openspec/specs/configuration/writing-systems.md#behavior) — Writing system controls
- [Entry Structure](../../../openspec/specs/lexicon/entries/structure.md#behavior) — Entry and sense dialogs
- [Entry Creation](../../../openspec/specs/lexicon/entries/creation.md#behavior) — Entry creation dialogs
- [Lexical Relations](../../../openspec/specs/lexicon/entries/relations.md#behavior) — Reference dialogs
- [SFM Import](../../../openspec/specs/lexicon/import/sfm.md#behavior) — LexImportWizard UI
- [LIFT Import](../../../openspec/specs/lexicon/import/lift.md#behavior) — LexImportWizard UI
- [LIFT Export](../../../openspec/specs/lexicon/export/lift.md#behavior) — Export dialogs
- [Interlinear Annotation](../../../openspec/specs/texts/interlinear/annotation.md#behavior) — Shared entry dialogs
- [Interlinear Baseline](../../../openspec/specs/texts/interlinear/baseline.md#behavior) — Interlinear configuration dialogs
- [Interlinear Import](../../../openspec/specs/texts/interlinear/import.md#behavior) — Import configuration dialogs

## Technology Stack
- C# .NET Framework 4.8.x (net8)
- OutputType: Library
- Windows Forms (dialogs, custom controls)
- LCModel (data model)
- Views (rendering)
- XCore (framework)

## Dependencies

### Upstream (consumes)
- **LCModel**: Data model (ILexEntry, ILexSense, IMoForm, ILexEntryRef, IMoMorphType)
- **Views**: Rendering engine
- **XCore**: Application framework
- **Common/FwUtils**: Utilities
- **Common/Controls**: Base controls
- **FwCoreDlgs**: Core dialogs

### Downstream (consumed by)
- **Lexicon/**: Lexicon editing UI
- **LexTextDll/**: Business logic
- **Morphology/**: Morphology features
- **FieldWorks.exe**: FLEx application host
- **xWorks**: Application shell

## Interop & Contracts
- **ILexEntry**: Lexical entry object
- **ILexSense**: Lexical sense
- **IMoForm**: Morpheme form (allomorph)
- **ILexEntryRef**: Entry relationships
- **IMoMorphType**: Morpheme type (prefix, suffix, etc.)
- **BaseGoDlg**: Abstract base for search dialogs
- **IFwExtension**: Extension interface

## Threading & Performance
UI thread operations. Fast incremental search. Large imports with progress reporting.

## Config & Feature Flags
Homograph numbering (ConfigureHomographDlg), writing systems (per field), import settings (LexImportWizard).

## Build Information
Build via FieldWorks.sln or `msbuild LexTextControls.csproj`. Test project: LexTextControlsTests. Output: SIL.FieldWorks.LexTextControls.dll.

## Interfaces and Data Models
BaseGoDlg (search base), InsertEntryDlg (entry finder), AddAllomorphDlg (allomorph adder), LexImportWizard (10K+ lines import), FeatureStructureTreeView (feature editor), PopupTreeManager family (hierarchical choosers).

## Entry Points
Library loaded by Lexicon/, LexTextDll/. Dialogs instantiated on demand (InsertEntryDlg, BaseGoDlg subclasses, LexImportWizard, etc.).

## Test Index
Test project: LexTextControlsTests. Run via `dotnet test` or Test Explorer.

## Usage Hints
InsertEntryDlg (entry insertion), BaseGoDlg subclasses (Ctrl+G navigation), LexImportWizard (File → Import), FeatureStructureTreeView (feature editing). Reused across Lexicon/, LexTextDll/, Morphology/.

## Related Folders
Lexicon (main UI), LexTextDll (business logic), Morphology (morphology UI), Common/FieldWorks (host).

## References
Project file: LexTextControls.csproj (net48). Key files (48129 lines, 100+ files): InsertEntryDlg.cs (1.7K), LexImportWizard family (10K+), BaseGoDlg.cs (947), FeatureStructureTreeView.cs (386), PopupTreeManager family. See `.cache/copilot/diff-plan.json` for details.
