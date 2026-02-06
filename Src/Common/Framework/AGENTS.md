---
last-reviewed: 2025-10-31
last-reviewed-tree: 9e9735ee7ccc66fb16ce0a68066e1b4ec9760f2cd45e7dffee0606822dcc5ad8
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - referenced-by
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

# Framework COPILOT summary

## Purpose
Application framework components providing core infrastructure services for FieldWorks applications. Includes FwApp base class for application coordination, editing helpers (FwEditingHelper for edit operations), publication interfaces (IPublicationView, IPageSetupDialog for printing/publishing), settings management (FwRegistrySettings, ExternalSettingsAccessorBase, SettingsXmlAccessorBase, StylesXmlAccessor), main window coordination (MainWindowDelegate, IFwMainWnd), application manager interface (IFieldWorksManager), status bar progress handling (StatusBarProgressHandler), undo/redo UI (UndoRedoDropDown), and XHTML export utilities (XhtmlHelper). Establishes architectural patterns, lifecycle management, and shared functionality for all FieldWorks applications.

### Referenced By

- [Multi-User Collaboration](../../../openspec/specs/integration/collaboration/multi-user.md#behavior) — Shared collaboration services

## Architecture
C# class library (.NET Framework 4.8.x) providing base classes and interfaces for FieldWorks applications. FwApp abstract class serves as application base with cache management, window coordination, and undo/redo infrastructure. Delegate pattern via MainWindowDelegate separates main window concerns. Settings abstraction via SettingsXmlAccessorBase and ExternalSettingsAccessorBase. Test project (FrameworkTests) validates framework components.

### Referenced By

- [Layer Model](../../../openspec/specs/architecture/layers/layer-model.md#layer-model) — Framework layer responsibilities
- [WinForms Patterns](../../../openspec/specs/architecture/ui-framework/winforms-patterns.md#ui-composition-patterns) — Framework UI composition

## Key Components
- **FwApp** class (FwApp.cs): Abstract base class for FieldWorks applications
  - Manages LcmCache, action handler, mediator, window list
  - IRecordListOwner, IRecordListUpdater, IRecordChangeHandler interfaces for list management
  - Application lifecycle: startup, synchronize, shutdown
  - Window management: CreateAndInitNewMainWindow, RemoveWindow
  - Cache management: SetupCache, ShutdownCache
- **MainWindowDelegate** class (MainWindowDelegate.cs): Main window coordination
  - IMainWindowDelegatedFunctions, IMainWindowDelegateCallbacks interfaces
  - Separates main window logic from FwApp
- **FwEditingHelper** class (FwEditingHelper.cs): Editing operations helper
  - Clipboard operations, paste handling, undo/redo coordination
  - Provides shared editing functionality across applications
- **IFieldWorksManager** interface (IFieldWorksManager.cs): Application manager contract
  - Cache access, application shutdown, window management
  - Implemented by FieldWorksManager in Common/FieldWorks
- **IFwMainWnd** interface (IFwMainWnd.cs): Main window contract
  - Main window behavior expected by framework
- **FwRegistrySettings** class (FwRegistrySettings.cs): Windows registry settings access
  - Read/write application settings to registry
- **ExternalSettingsAccessorBase** class (ExternalSettingsAccessorBase.cs): External settings base
  - Abstract base for accessing external settings (registry, files)
- **SettingsXmlAccessorBase** class (SettingsXmlAccessorBase.cs): XML settings base
  - Abstract base for XML-based settings persistence
- **StylesXmlAccessor** class (StylesXmlAccessor.cs): Styles XML persistence
  - Read/write style definitions to XML
- **ExportStyleInfo** class (ExportStyleInfo.cs): Style export information
  - Metadata for style export operations
- **UndoRedoDropDown** class (UndoRedoDropDown.cs/.resx): Undo/redo dropdown control
  - UI control showing undo/redo stack with multiple level selection
- **StatusBarProgressHandler** class (StatusBarProgressHandler.cs): Progress reporting
  - Displays progress in status bar during long operations
- **XhtmlHelper** class (XhtmlHelper.cs): XHTML export utilities
  - Helper functions for XHTML generation
- **IPublicationView** interface (PublicationInterfaces.cs): Publication view contract
  - Implemented by views supporting print/publish operations
- **IPageSetupDialog** interface (PublicationInterfaces.cs): Page setup contract
  - Interface for page setup dialogs

### Referenced By

- [Undo/Redo](../../../openspec/specs/architecture/data-access/undo-redo.md#undoredo-flow) — Shared undo/redo controls
- [Lists](../../../openspec/specs/configuration/lists.md#behavior) — Shared list configuration
- [Projects](../../../openspec/specs/configuration/projects.md#behavior) — Shared project settings
- [Text Export Formats](../../../openspec/specs/texts/export/formats.md#behavior) — Publishing interfaces

## Technology Stack
- C# .NET Framework 4.8.x (target framework: net48)
- OutputType: Library (class library DLL)
- Windows Forms for UI (System.Windows.Forms)
- Windows Registry access (Microsoft.Win32)
- XCore for mediator/command pattern
- SIL.LCModel for data access

### Referenced By

- [Localization](../../../openspec/specs/architecture/build-deploy/localization.md#localization-patterns) — Framework resx usage

## Dependencies

### Upstream (consumes)
- **SIL.LCModel**: Language and Culture Model (LcmCache, action handler)
- **SIL.LCModel.Infrastructure**: Infrastructure services
- **SIL.LCModel.DomainServices**: Domain service layer
- **Common/FwUtils**: FieldWorks utilities
- **Common/ViewsInterfaces**: View interfaces
- **Common/RootSites**: Root site infrastructure
- **Common/Controls**: Common controls
- **XCore**: Command/mediator framework
- **FwCoreDlgs**: Core dialogs
- **System.Windows.Forms**: Windows Forms UI

### Referenced By

- [Dependency Graph](../../../openspec/specs/architecture/layers/dependency-graph.md#dependency-rules) — Layer dependency rules
- [XCore Mediator](../../../openspec/specs/architecture/ui-framework/xcore-mediator.md#mediator-routing) — Mediator dependencies
- [LCM Patterns](../../../openspec/specs/architecture/data-access/lcm-patterns.md#data-access-patterns) — Data access dependencies

### Downstream (consumed by)
- **xWorks/**: Main FLEx application extends FwApp
- **LexText/**: Lexicon application uses framework
- All FieldWorks applications requiring application framework services

## Interop & Contracts
- **IFieldWorksManager**: Contract for application manager
- **IFwMainWnd**: Contract for main windows
- **IRecordListUpdater, IRecordListOwner, IRecordChangeHandler**: Contracts for list management and side-effect handling
- **IPublicationView, IPageSetupDialog**: Contracts for print/publish functionality
- Uses COM interop for legacy components

## Threading & Performance
UI thread marshaling ensured. Background threads with progress reporting. Cache access coordinated.

## Config & Feature Flags
FwRegistrySettings (registry), XML settings (SettingsXmlAccessorBase, StylesXmlAccessor). Behavior controlled by settings.

## Build Information
Build via FieldWorks.sln or `msbuild Framework.csproj`. Test project: FrameworkTests. Output: Framework.dll.

### Referenced By

- [Build Phases](../../../openspec/specs/architecture/build-deploy/build-phases.md#build-ordering) — Managed build targets

## Interfaces and Data Models
FwApp (application base), IFieldWorksManager (manager contract), IFwMainWnd (main window contract), IRecordListUpdater/Owner/ChangeHandler (list management), IPublicationView/IPageSetupDialog (print/publish), MainWindowDelegate (delegation pattern).

## Entry Points
FwApp subclasses instantiated as application entry points. IFieldWorksManager accessed via FieldWorksManager facade.

### Referenced By

- [Entry Points](../../../openspec/specs/architecture/layers/entry-points.md#application-entry-points) — Application lifecycle entry points

## Test Index
Test project: FrameworkTests. Run via `dotnet test` or Test Explorer.

### Referenced By

- [Test Strategy](../../../openspec/specs/architecture/testing/test-strategy.md#strategy) — Managed test guidance

## Usage Hints
Extend FwApp for applications. Implement IFwMainWnd for main windows. Use IRecordListUpdater for list updates. StatusBarProgressHandler for progress reporting.

## Related Folders
Common/FwUtils (utilities), Common/ViewsInterfaces (view interfaces), Common/FieldWorks (FieldWorksManager), XCore (command/mediator), xWorks (main consumer), LexText (lexicon app).

## References
Project files: Framework.csproj (net48), FrameworkTests. Key files (10034 lines): FwApp.cs, MainWindowDelegate.cs, FwEditingHelper.cs, settings classes, UndoRedoDropDown.cs. See `.cache/copilot/diff-plan.json` for details.
