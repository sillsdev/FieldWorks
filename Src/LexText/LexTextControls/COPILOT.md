---
last-reviewed: 2025-10-31
last-reviewed-tree: 1df0295ce1593a7e633207f32408b108fd3730269eb184a240586b98dab6df5d
status: draft
---

# LexTextControls COPILOT summary

## Purpose
Shared UI controls and dialogs library for FLEx lexicon and text features. Provides reusable lexicon-specific dialogs (InsertEntryDlg, AddAllomorphDlg, AddNewSenseDlg), search dialogs (EntryGoDlg, ReverseGoDlg, BaseGoDlg), import wizards (LexImportWizard, CombineImportDlg), configuration dialogs (ConfigureHomographDlg, LexiconSettingsDlg), and specialized controls (FeatureStructureTreeView, InflectionClassPopupTreeManager, PopupTree). Critical shared infrastructure for Lexicon/, LexTextDll/, and other lexicon UI components. Massive 48.1K line library with 100+ source files covering comprehensive lexicon UI needs.

## Architecture
C# library (net462, OutputType=Library) organizing lexicon/text UI components for reuse. BaseGoDlg abstract base for search dialogs. InsertEntryDlg family for adding lexicon entries. LexImportWizard multi-step import process. PopupTreeManager family for hierarchical selection (InflectionClassPopupTreeManager, InflectionFeaturePopupTreeManager, PhonologicalFeaturePopupTreeManager). DataNotebook/ subfolder for notebook-style controls. Heavy integration with LCModel (ILexEntry, ILexSense, IMoForm), Views rendering, XCore framework.

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

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
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
- **LexTextExe/**: FLEx application
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
- **UI thread**: All operations on UI thread
- **Search**: Fast incremental search in dialogs
- **Import**: Large imports may take time (progress reporting)

## Config & Feature Flags
- **Homograph numbering**: Configurable via ConfigureHomographDlg
- **Writing systems**: Configurable per field
- **Import settings**: LexImportWizard configuration

## Build Information
- **Project file**: LexTextControls.csproj (net462, OutputType=Library)
- **Test project**: LexTextControlsTests/
- **Output**: SIL.FieldWorks.LexTextControls.dll
- **Build**: Via top-level FieldWorks.sln or: `msbuild LexTextControls.csproj`
- **Run tests**: `dotnet test LexTextControlsTests/`

## Interfaces and Data Models

- **BaseGoDlg** (BaseGoDlg.cs)
  - Purpose: Abstract base for "Go To" search dialogs
  - Subclasses: EntryGoDlg, ReverseGoDlg
  - Features: Incremental search, entry navigation
  - Notes: 947 lines of common search infrastructure

- **InsertEntryDlg** (InsertEntryDlg.cs)
  - Purpose: Insert/find lexical entries dialog
  - Inputs: Search string
  - Outputs: Selected ILexEntry
  - Notes: Used throughout lexicon editing

- **AddAllomorphDlg** (AddAllomorphDlg.cs)
  - Purpose: Add allomorph to entry
  - Inputs: ILexEntry, morpheme type
  - Outputs: New IMoForm (allomorph)
  - Notes: Supports prefix, suffix, stem, etc.

- **LexImportWizard** (LexImportWizard*.cs)
  - Purpose: Multi-step lexicon import wizard
  - Inputs: Import file (LIFT, Toolbox, etc.)
  - Outputs: Imported entries in LCModel
  - Notes: 10K+ lines for comprehensive import

- **FeatureStructureTreeView** (FeatureStructureTreeView.cs)
  - Purpose: Display/edit feature structures
  - Inputs: IFsFeatureStructure
  - Outputs: Modified feature structure
  - Notes: Tree view for phonological/grammatical features

- **PopupTreeManager family**:
  - InflectionClassPopupTreeManager: Choose inflection class
  - InflectionFeaturePopupTreeManager: Choose grammatical features
  - PhonologicalFeaturePopupTreeManager: Choose phonological features
  - PopupTree base: Generic popup tree infrastructure

## Entry Points
Loaded by Lexicon/, LexTextDll/, and other lexicon UI components. Dialogs instantiated as needed.

## Test Index
- **Test project**: LexTextControlsTests/
- **Run tests**: `dotnet test LexTextControlsTests/`
- **Coverage**: Dialog logic, search engines, import wizards

## Usage Hints
- **InsertEntryDlg**: Used for entry insertion throughout FLEx
- **BaseGoDlg subclasses**: Quick navigation dialogs (Ctrl+G)
- **AddAllomorphDlg**: Add allomorphs in lexicon editing
- **LexImportWizard**: File → Import → Lexicon
- **FeatureStructureTreeView**: Edit phonological/grammatical features
- **PopupTreeManager**: Hierarchical selection UI pattern
- **Shared library**: Reused across Lexicon/, LexTextDll/, Morphology/
- **Large codebase**: 48.1K lines, 100+ files

## Related Folders
- **Lexicon/**: Main lexicon UI (consumes controls)
- **LexTextDll/**: Business logic
- **Morphology/**: Morphology UI
- **LexTextExe/**: FLEx application

## References
- **Project file**: LexTextControls.csproj (net462, OutputType=Library)
- **Key C# files**: InsertEntryDlg.cs (1.7K), LexImportWizard family (10K+ combined), BaseGoDlg.cs (947), FeatureStructureTreeView.cs (386), AddNewSenseDlg.cs (372), CombineImportDlg.cs (348), EntryGoDlg.cs (236), AddWritingSystemButton.cs (229), EntryObjects.cs (208), AddAllomorphDlg.cs (197), and 90+ more files
- **Test project**: LexTextControlsTests/
- **Total lines of code**: 48129
- **Output**: SIL.FieldWorks.LexTextControls.dll
- **Namespace**: Various (SIL.FieldWorks.LexText, SIL.FieldWorks.LexText.Controls, etc.)
- **Subsystems**: Search dialogs, Entry insertion, Allomorph management, Import wizards, Feature editing, Popup trees, Homograph configuration