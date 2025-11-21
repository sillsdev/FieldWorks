---
last-reviewed: 2025-10-31
last-reviewed-tree: 114dd448c4f3b6d56bdea5387e1297baa4539cba7ae636bed02508cdad5bae32
status: draft
---

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
- OutputType: Library
- Windows Forms (System.Windows.Forms)
- XCore for command routing (IxCoreColleague)
- Views engine integration (IVwViewConstructor)
- LCModel for data access

## Dependencies

### Upstream (consumes)
- **SIL.LCModel**: Data model (ICmObject, LcmCache)
- **SIL.LCModel.DomainServices**: Domain services
- **SIL.LCModel.Infrastructure**: Infrastructure layer
- **Common/Framework**: Application framework (Mediator, PropertyTable)
- **Common/Controls**: Common controls
- **Common/RootSites**: View hosting
- **Common/FwUtils**: Utilities
- **LexText/Controls**: Lexicon controls
- **XCore**: Command routing
- **Windows Forms**: UI framework

### Downstream (consumed by)
- **xWorks**: Uses FdoUi for data object editing
- **LexText**: Lexicon editing uses object-specific UI
- Any application displaying/editing FieldWorks data objects

## Interop & Contracts
- **IFwGuiControl**: Contract for dynamically initialized GUI controls
- **IxCoreColleague**: XCore command routing integration
- **IVwViewConstructor**: View construction for Views engine
- Factory pattern via m_subclasses for object-specific UI

## Threading & Performance
- **UI thread required**: All UI operations
- **Factory caching**: m_subclasses dictionary caches clsid mappings
- **Performance**: UI components optimized for responsive editing

## Config & Feature Flags
- **XML configuration**: IFwGuiControl.Init() accepts XML config nodes
- **VcFrags enum**: Fragment identifiers control view construction
- No explicit feature flags

## Build Information
- **Project file**: FdoUi.csproj (net48, OutputType=Library)
- **Test project**: FdoUiTests/
- **Output**: FdoUi.dll
- **Build**: Via top-level FieldWorks.sln
- **Run tests**: `dotnet test FdoUiTests/`

## Interfaces and Data Models

- **CmObjectUi** (FdoUiCore.cs)
  - Purpose: Base class for object-specific UI behavior
  - Inputs: Mediator, PropertyTable, ICmObject, LcmCache
  - Outputs: UI operations, command handling
  - Notes: Factory pattern creates subclass instances based on clsid

- **IFwGuiControl** (FdoUiCore.cs)
  - Purpose: Contract for dynamically initialized GUI controls
  - Inputs: Init(mediator, propertyTable, configurationNode, sourceObject)
  - Outputs: Configured GUI control via Launch()
  - Notes: Enables plugin-style extensibility

- **VcFrags enum** (FdoUiCore.cs)
  - Purpose: View fragment identifiers for view construction
  - Values: kfragShortName, kfragName, kfragInterlinearName, etc.
  - Notes: All CmObject subclasses support these fragments

- **LexEntryUi** (LexEntryUi.cs)
  - Purpose: Lexical entry-specific UI behavior
  - Inputs: LexEntry object
  - Outputs: Headword display, entry editing UI
  - Notes: Overrides CmObjectUi for lexical entries

- **BulkPosEditor** (BulkPosEditor.cs)
  - Purpose: Bulk editing of part-of-speech assignments
  - Inputs: Multiple objects, POS values
  - Outputs: Mass POS updates
  - Notes: Efficiency for large-scale edits

- **InflectionClassEditor, InflectionFeatureEditor, PhonologicalFeatureEditor**
  - Purpose: Specialized editors for linguistic features
  - Inputs: Feature definitions, values
  - Outputs: Feature assignments
  - Notes: Complex editing interfaces for morphological/phonological data

## Entry Points
Referenced as library for data object UI. CmObjectUi factory creates appropriate UI subclass instances.

## Test Index
- **Test project**: FdoUiTests/
- **Run tests**: `dotnet test FdoUiTests/`
- **Coverage**: Object UI behavior, editors, dummy objects

## Usage Hints
- Use CmObjectUi factory to get appropriate UI for any ICmObject
- Implement IFwGuiControl for custom dynamic GUI controls
- VcFrags enum for consistent view fragment usage
- Extend CmObjectUi subclasses for custom object UI behavior
- DummyCmObject for unit testing UI components

## Related Folders
- **LexText/**: Major consumer of FdoUi for lexicon editing
- **xWorks/**: Uses FdoUi for data display and editing
- **Common/Controls**: Complementary control library

## References
- **Project files**: FdoUi.csproj (net48), FdoUiTests/
- **Target frameworks**: .NET Framework 4.8.x
- **Key C# files**: FdoUiCore.cs, LexEntryUi.cs, PartOfSpeechUi.cs, BulkPosEditor.cs, InflectionClassEditor.cs, and others
- **Total lines of code**: 8408
- **Output**: FdoUi.dll
- **Namespace**: SIL.FieldWorks.FdoUi
