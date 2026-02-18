---
last-reviewed: 2025-10-31
last-reviewed-tree: 114dd448c4f3b6d56bdea5387e1297baa4539cba7ae636bed02508cdad5bae32
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - technology-stack
  - dependencies
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

# FdoUi COPILOT summary

## Purpose
User interface components for FieldWorks Data Objects (FDO/LCModel). Provides specialized UI controls, dialogs, and view constructors for editing and displaying linguistic data model objects. CmObjectUi base class and subclasses (LexEntryUi, PartOfSpeechUi, ReversalIndexEntryUi, etc.) implement object-specific UI behavior. Editors for complex data (BulkPosEditor, InflectionClassEditor, InflectionFeatureEditor, PhonologicalFeatureEditor) provide specialized editing interfaces. DummyCmObject for testing, FwLcmUI for LCModel UI integration, ProgressBarWrapper for progress reporting. Essential UI layer between data model and applications.

## Architecture
C# class library (.NET Framework 4.8.x) with UI components for data objects. CmObjectUi base class with factory pattern for creating object-specific UI instances (m_subclasses dictionary maps clsids). IFwGuiControl interface for dynamically initialized GUI controls. VcFrags enum defines view fragments supported by all objects. Test project FdoUiTests validates functionality. 8408 lines of UI code.

## Key Components
- **CmObjectUi** class (FdoUiCore.cs): Base UI class for all data objects
  - Implements IxCoreColleague for XCore command routing
  - Factory pattern: maps clsid to UI subclass via m_subclasses dictionary
  - Mediator, PropertyTable, LcmCache integration
  - IVwViewConstructor m_vc for view construction
  - Subclasses override for object-specific UI behavior
- **IFwGuiControl** interface (FdoUiCore.cs): Dynamic GUI control initialization
  - Init(): Configure with mediator, property table, XML config, source object
  - Launch(): Start control operation
  - Enables plugin-style GUI components
- **VcFrags** enum (FdoUiCore.cs): View fragment identifiers
  - kfragShortName, kfragName: Name display variants
  - kfragInterlinearName, kfragInterlinearAbbr: Interlinear view fragments
  - kfragFullMSAInterlinearname: MSA (Morphosyntactic Analysis) display
  - kfragHeadWord: Lexical entry headword
  - kfragPosAbbrAnalysis: Part of speech abbreviation
- **LexEntryUi** (LexEntryUi.cs): Lexical entry UI behavior
- **PartOfSpeechUi** (PartOfSpeechUi.cs): Part of speech UI behavior
- **ReversalIndexEntryUi** (ReversalIndexEntryUi.cs): Reversal entry UI
- **LexPronunciationUi** (LexPronunciationUi.cs): Pronunciation UI
- **FsFeatDefnUi** (FsFeatDefnUi.cs): Feature definition UI
- **BulkPosEditor** (BulkPosEditor.cs): Bulk part-of-speech editing
- **InflectionClassEditor** (InflectionClassEditor.cs): Inflection class editing
- **InflectionFeatureEditor** (InflectionFeatureEditor.cs): Inflection feature editing
- **PhonologicalFeatureEditor** (PhonologicalFeatureEditor.cs): Phonological feature editing
- **DummyCmObject** (DummyCmObject.cs): Test double for data objects
- **FwLcmUI** (FwLcmUI.cs): FieldWorks LCModel UI integration
- **ProgressBarWrapper** (ProgressBarWrapper.cs): Progress reporting UI
- **FdoUiStrings** (FdoUiStrings.Designer.cs): Localized UI strings

## Technology Stack
- C# .NET Framework 4.8.x (net8)

## Dependencies
- Upstream: Data model (ICmObject, LcmCache)
- Downstream: Uses FdoUi for data object editing

## Interop & Contracts
- **IFwGuiControl**: Contract for dynamically initialized GUI controls

## Threading & Performance
- **UI thread required**: All UI operations

## Config & Feature Flags
- **XML configuration**: IFwGuiControl.Init() accepts XML config nodes

## Build Information
- **Project file**: FdoUi.csproj (net48, OutputType=Library)

## Interfaces and Data Models
CmObjectUi, IFwGuiControl, LexEntryUi, BulkPosEditor.

## Entry Points
Referenced as library for data object UI. CmObjectUi factory creates appropriate UI subclass instances.

## Test Index
- **Test project**: FdoUiTests/

## Usage Hints
- Use CmObjectUi factory to get appropriate UI for any ICmObject

## Related Folders
- **LexText/**: Major consumer of FdoUi for lexicon editing

## References
See `.cache/copilot/diff-plan.json` for file details.
