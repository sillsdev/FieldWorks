---
last-reviewed: 2025-10-31
last-reviewed-tree: 9e9735ee7ccc66fb16ce0a68066e1b4ec9760f2cd45e7dffee0606822dcc5ad8
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/Common/Framework. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


# Framework COPILOT summary

## Purpose
Application framework components providing core infrastructure services for FieldWorks applications. Includes FwApp base class for application coordination, editing helpers (FwEditingHelper for edit operations), publication interfaces (IPublicationView, IPageSetupDialog for printing/publishing), settings management (FwRegistrySettings, ExternalSettingsAccessorBase, SettingsXmlAccessorBase, StylesXmlAccessor), main window coordination (MainWindowDelegate, IFwMainWnd), application manager interface (IFieldWorksManager), status bar progress handling (StatusBarProgressHandler), undo/redo UI (UndoRedoDropDown), and XHTML export utilities (XhtmlHelper). Establishes architectural patterns, lifecycle management, and shared functionality for all FieldWorks applications.

## Architecture
C# class library (.NET Framework 4.8.x) providing base classes and interfaces for FieldWorks applications. FwApp abstract class serves as application base with cache management, window coordination, and undo/redo infrastructure. Delegate pattern via MainWindowDelegate separates main window concerns. Settings abstraction via SettingsXmlAccessorBase and ExternalSettingsAccessorBase. Test project (FrameworkTests) validates framework components.

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

## Technology Stack
- C# .NET Framework 4.8.x (target framework: net48)
- OutputType: Library (class library DLL)
- Windows Forms for UI (System.Windows.Forms)
- Windows Registry access (Microsoft.Win32)
- XCore for mediator/command pattern
- SIL.LCModel for data access

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
- **UI thread marshaling**: Framework ensures UI operations on UI thread
- **Explicit threading**: Some operations use background threads with progress reporting
- **Synchronization**: Cache access coordinated across windows/threads

## Config & Feature Flags
- **FwRegistrySettings**: Windows registry for application settings
- **XML settings**: SettingsXmlAccessorBase, StylesXmlAccessor for XML-based configuration
- No explicit feature flags; behavior controlled by settings

## Build Information
- **Project file**: Framework.csproj (.NET Framework 4.8.x, OutputType=Library)
- **Test project**: FrameworkTests/FrameworkTests.csproj
- **Output**: Framework.dll (to Output/Debug or Output/Release)
- **Build**: Via top-level FieldWorks.sln or: `msbuild Framework.csproj /p:Configuration=Debug`
- **Run tests**: `dotnet test FrameworkTests/FrameworkTests.csproj` or Visual Studio Test Explorer

## Interfaces and Data Models

- **FwApp** (FwApp.cs)
  - Purpose: Abstract base class for FieldWorks applications
  - Inputs: LcmCache, action handler, mediator, window list
  - Outputs: Application lifecycle management, window coordination
  - Notes: Subclasses implement application-specific behavior

- **IFieldWorksManager** (IFieldWorksManager.cs)
  - Purpose: Contract for application manager facade
  - Inputs: Application instances, window management requests
  - Outputs: Cache access, shutdown coordination
  - Notes: Implemented by FieldWorksManager (Common/FieldWorks)

- **IFwMainWnd** (IFwMainWnd.cs)
  - Purpose: Contract for main application windows
  - Inputs: N/A (properties)
  - Outputs: Main window services (mediator, synchronization, refresh)
  - Notes: Main windows implement to participate in framework

- **IRecordListUpdater** (FwApp.cs)
  - Purpose: Contract for updating record lists with side-effect handling
  - Inputs: IRecordChangeHandler, refresh flags
  - Outputs: UpdateList(), RefreshCurrentRecord()
  - Notes: Helps coordinate list updates after object changes

- **IRecordListOwner** (FwApp.cs)
  - Purpose: Contract for finding record list updaters by name
  - Inputs: string name
  - Outputs: IRecordListUpdater or null
  - Notes: Allows components to locate and update specific lists

- **IRecordChangeHandler** (FwApp.cs)
  - Purpose: Contract for handling side-effects of object changes
  - Inputs: Object change events
  - Outputs: Fixup() method for pre-refresh processing
  - Notes: Ensures side-effects complete before list refresh

- **IPublicationView** (PublicationInterfaces.cs)
  - Purpose: Contract for views supporting print/publish
  - Inputs: N/A (properties)
  - Outputs: Print services, page layout access
  - Notes: Views implement for print/export functionality

- **IPageSetupDialog** (PublicationInterfaces.cs)
  - Purpose: Contract for page setup dialogs
  - Inputs: Page setup parameters
  - Outputs: ShowDialog(), page configuration
  - Notes: Standard interface for page setup UI

- **MainWindowDelegate** (MainWindowDelegate.cs)
  - Purpose: Coordinates main window operations via delegation
  - Inputs: IMainWindowDelegateCallbacks (callbacks to main window)
  - Outputs: IMainWindowDelegatedFunctions (delegated operations)
  - Notes: Separates concerns between FwApp and main window

## Entry Points
- FwApp subclasses instantiated as application entry points
- IFieldWorksManager accessed via FieldWorksManager
- Framework components referenced by all FieldWorks applications

## Test Index
- **Test project**: FrameworkTests (FrameworkTests.csproj)
- **Run tests**: `dotnet test FrameworkTests/FrameworkTests.csproj` or Visual Studio Test Explorer
- **Coverage**: Unit tests for framework components

## Usage Hints
- Extend FwApp to create FieldWorks applications
- Implement IFwMainWnd for main windows
- Use IRecordListUpdater pattern for side-effect coordination
- Implement IPublicationView for print/export support
- Use StatusBarProgressHandler for progress reporting
- Access settings via FwRegistrySettings or XML accessor classes

## Related Folders
- **Common/FwUtils/**: Utilities used by framework
- **Common/ViewsInterfaces/**: View interfaces used by framework
- **Common/RootSites/**: Root site infrastructure
- **Common/FieldWorks/**: FieldWorksManager implements IFieldWorksManager
- **XCore/**: Command/mediator framework integrated with Framework
- **xWorks/**: Main consumer extending FwApp
- **LexText/**: Lexicon application using framework

## References
- **Project files**: Framework.csproj (net48, OutputType=Library), FrameworkTests/FrameworkTests.csproj
- **Target frameworks**: .NET Framework 4.8.x (net48)
- **Key dependencies**: SIL.LCModel, SIL.LCModel.Infrastructure, SIL.LCModel.DomainServices, XCore, Common/FwUtils, Common/ViewsInterfaces, Common/RootSites
- **Key C# files**: FwApp.cs, MainWindowDelegate.cs, FwEditingHelper.cs, IFieldWorksManager.cs, IFwMainWnd.cs, FwRegistrySettings.cs, ExternalSettingsAccessorBase.cs, SettingsXmlAccessorBase.cs, StylesXmlAccessor.cs, ExportStyleInfo.cs, UndoRedoDropDown.cs, StatusBarProgressHandler.cs, XhtmlHelper.cs, PublicationInterfaces.cs, AssemblyInfo.cs
- **Designer files**: FrameworkStrings.Designer.cs
- **Resources**: FrameworkStrings.resx, UndoRedoDropDown.resx
- **Total lines of code**: 10034
- **Output**: Output/Debug/Framework.dll, Output/Release/Framework.dll
- **Namespace**: SIL.FieldWorks.Common.Framework