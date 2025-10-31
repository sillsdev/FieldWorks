---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# FieldWorks COPILOT summary

## Purpose
Core FieldWorks-specific application infrastructure and utilities providing fundamental application services. Includes project management (FieldWorksManager interface, ProjectId for project identification), settings management (FwRestoreProjectSettings for backup restoration), application startup coordination (WelcomeToFieldWorksDlg, FieldWorks main class), busy state handling (ApplicationBusyDialog), Windows installer querying (WindowsInstallerQuery), remote request handling (RemoteRequest), lexical service provider integration (ILexicalProvider, LexicalServiceProvider), and Phonology Assistant integration objects (PaObjects/ namespace). Central to coordinating application lifecycle, managing shared resources, and enabling interoperability across FieldWorks applications.

## Architecture
C# Windows executable (WinExe) targeting .NET Framework 4.6.2. Main entry point for FieldWorks application launcher. Contains FieldWorks singleton class managing application lifecycle, project opening/closing, and window management. Includes three specialized namespaces: LexicalProvider/ for lexicon service integration, PaObjects/ for Phonology Assistant data transfer objects, and main SIL.FieldWorks namespace for core infrastructure. Test project (FieldWorksTests) provides unit tests for project ID, PA objects, and welcome dialog.

## Key Components
- **FieldWorks** class (FieldWorks.cs): Main application singleton
  - Manages application lifecycle and FwApp instances
  - Handles project selection, opening, and closing
  - Coordinates window creation and management
  - Provides LcmCache access
  - Main entry point: OutputType=WinExe
- **FieldWorksManager** class (FieldWorksManager.cs): IFieldWorksManager implementation
  - Pass-through facade ensuring single FieldWorks instance per process
  - `Cache` property: Access to LcmCache
  - `ShutdownApp()`: Shutdown application and dispose
  - `ExecuteAsync()`: Asynchronous method execution via UI thread
  - `OpenNewWindowForApp()`: Create new main window for application
  - `ChooseLangProject()`: User selects and opens language project
- **ProjectId** class (ProjectId.cs): Project identification
  - Implements ISerializable, IProjectIdentifier
  - Represents FW project identity (may or may not exist)
  - Fields: m_path (project path), m_type (BackendProviderType)
  - Supports serialization for inter-process communication
  - Constructors for local projects, type inference from file extension
- **ApplicationBusyDialog** (ApplicationBusyDialog.cs/.Designer.cs/.resx): Busy indicator dialog
  - Shows progress and status during long-running operations
  - WaitFor enum: Cancel, NoCancel, TimeLimit, Cancelled
- **WelcomeToFieldWorksDlg** (WelcomeToFieldWorksDlg.cs/.Designer.cs/.resx): Startup dialog
  - Welcome screen for FieldWorks application launch
  - ButtonPress enum: Open, New, Import, ChooseDifferentProject
- **MoveProjectsDlg** (MoveProjectsDlg.cs/.Designer.cs/.resx): Project relocation dialog
  - Dialog for moving projects to different locations
- **FwRestoreProjectSettings** (FwRestoreProjectSettings.cs): Backup restoration settings
  - Configuration for restoring projects from backups
- **WindowsInstallerQuery** (WindowsInstallerQuery.cs): Windows installer integration
  - Queries Windows Installer API for installed products
  - Checks for installed versions of FieldWorks components
- **RemoteRequest** class (RemoteRequest.cs): Inter-process communication
  - Handles remote requests between FieldWorks instances
  - Enables opening projects in existing processes

**LexicalProvider namespace** (LexicalProvider/):
- **ILexicalProvider** interface (ILexicalProvider.cs): Lexicon service contract
  - Methods for querying lexical data (entries, senses, glosses)
  - LexicalEntry, LexSense, LexGloss classes
  - EntryType enum: Word, Affix, Phrase
  - LexemeType enum: Stem, Prefix, Suffix, Infix, Clitic
- **ILexicalServiceProvider** interface (ILexicalProvider.cs): Service provider contract
  - Manages ILexicalProvider instances
- **LexicalProviderImpl** class (LexicalProviderImpl.cs): ILexicalProvider implementation
  - Concrete implementation providing lexical data access
- **LexicalServiceProvider** class (LexicalServiceProvider.cs): ILexicalServiceProvider implementation
  - Manages lexical provider instances
- **LexicalProviderManager** class (LexicalProviderManager.cs): Provider lifetime management
  - Coordinates lexical provider creation and disposal

**PaObjects namespace** (PaObjects/): Phonology Assistant data transfer objects
- **PaLexEntry** (PaLexEntry.cs): Lexical entry for PA integration
- **PaLexSense** (PaLexSense.cs): Lexical sense for PA integration
- **PaCmPossibility** (PaCmPossibility.cs): Possibility list item
- **PaMediaFile** (PaMediaFile.cs): Media file reference
- **PaMultiString** (PaMultiString.cs): Multi-writing-system string
- **PaRemoteRequest** (PaRemoteRequest.cs): PA remote request
- **PaVariant** (PaVariant.cs): Lexical variant information
- **PaVariantOfInfo** (PaVariantOfInfo.cs): Variant relationship data
- **PaWritingSystem** (PaWritingSystem.cs): Writing system metadata
- **PaLexPronunciation** (PaLexPronunciation.cs): Pronunciation information
- **PaLexicalInfo** (PaLexicalInfo.cs): Lexical metadata
- **PaComplexFormInfo** (PaComplexFormInfo.cs): Complex form relationships

## Technology Stack
- C# .NET Framework 4.6.2 (target framework: net462)
- OutputType: WinExe (Windows executable with UI)
- Windows Forms for UI (System.Windows.Forms)
- System.Runtime.Serialization for ProjectId serialization
- Windows Installer API integration (WindowsInstallerQuery)
- Inter-process communication for multi-instance coordination

## Dependencies

### Upstream (consumes)
- **SIL.LCModel**: Language and Culture Model (LcmCache, IProjectIdentifier, BackendProviderType)
- **SIL.LCModel.Utils**: Utility classes
- **Common/Framework**: Application framework (IFwMainWnd, FwApp)
- **Common/FwUtils**: FieldWorks utilities (IFieldWorksManager, ThreadHelper)
- **LexText/LexTextControls**: Referenced in project dependencies
- **System.Windows.Forms**: Windows Forms UI framework
- **DesktopAnalytics**: Analytics library

### Downstream (consumed by)
- **xWorks/**: Main FLEx application using FieldWorks infrastructure
- **LexText/**: Uses FieldWorks for project management and application lifecycle
- Any FieldWorks application requiring project management, startup coordination, or lexical service integration

## Interop & Contracts
- **IFieldWorksManager**: Contract for application manager (implemented by FieldWorksManager)
- **IProjectIdentifier**: Contract for project identification (implemented by ProjectId)
- **ISerializable**: ProjectId supports serialization for inter-process communication
- **ILexicalProvider, ILexicalServiceProvider**: Contracts for lexical data access
- **COM/P/Invoke**: Windows Installer API via WindowsInstallerQuery

## Threading & Performance
- **UI thread marshaling**: FieldWorksManager.ExecuteAsync() uses ThreadHelper.InvokeAsync for UI thread invocation
- **Synchronization**: Application lifecycle methods coordinate across multiple FwApp instances
- **Performance**: Singleton pattern (FieldWorks class) ensures single instance per process
- **Dialog responsiveness**: ApplicationBusyDialog provides cancellation and progress feedback

## Config & Feature Flags
- **App.config**: Application configuration file
- **BuildInclude.targets**: MSBuild custom targets for build configuration
- No explicit feature flags detected in source

## Build Information
- **Project file**: FieldWorks.csproj (.NET Framework 4.6.2, WinExe)
- **Test project**: FieldWorksTests/FieldWorksTests.csproj
- **Output**: FieldWorks.exe (main executable), FieldWorks.xml (documentation)
- **Icon**: BookOnCube.ico (multiple variants: BookOnCube, CubeOnBook, versions, sizes)
- **Build**: Via top-level FW.sln or: `msbuild FieldWorks.csproj /p:Configuration=Debug`
- **Run tests**: `dotnet test FieldWorksTests/FieldWorksTests.csproj` or Visual Studio Test Explorer
- **Auto-generate binding redirects**: Enabled for assembly version resolution

## Interfaces and Data Models

- **IFieldWorksManager** (implemented by FieldWorksManager)
  - Purpose: Pass-through facade for FieldWorks application access
  - Inputs: FwApp instances, Forms, Actions
  - Outputs: LcmCache, window creation, async execution
  - Notes: Ensures single FieldWorks instance per process

- **IProjectIdentifier** (implemented by ProjectId)
  - Purpose: Contract for project identification
  - Inputs: Project name, type
  - Outputs: Project identity for opening/management
  - Notes: Supports serialization for inter-process communication

- **ProjectId** (ProjectId.cs)
  - Purpose: Represents FW project identification (existing or potential)
  - Inputs: name (string), type (BackendProviderType or inferred)
  - Outputs: Serializable project identity
  - Notes: Implements ISerializable for marshaling across process boundaries

- **FieldWorksManager.ChooseLangProject** (FieldWorksManager.cs)
  - Purpose: User selects language project; opens in existing or new process
  - Inputs: FwApp app, Form dialogOwner
  - Outputs: Opens project in appropriate FieldWorks process
  - Notes: Coordinates multi-instance scenarios via RemoteRequest

- **ILexicalProvider, ILexicalServiceProvider** (LexicalProvider/ILexicalProvider.cs)
  - Purpose: Contracts for lexicon service integration
  - Inputs: Lexical queries (entries, senses, glosses)
  - Outputs: LexicalEntry, LexSense, LexGloss data
  - Notes: Enables external tools to query FW lexicon

- **PaObjects namespace classes** (PaObjects/*.cs)
  - Purpose: Data transfer objects for Phonology Assistant integration
  - Inputs: FW lexical data (entries, senses, pronunciations, variants)
  - Outputs: Serializable objects for PA consumption
  - Notes: Facilitates data exchange between FieldWorks and Phonology Assistant

- **ApplicationBusyDialog** (ApplicationBusyDialog.cs)
  - Purpose: Modal or modeless busy indicator during long operations
  - Inputs: WaitFor option (Cancel, NoCancel, TimeLimit, Cancelled)
  - Outputs: User interaction (cancel request) or timeout
  - Notes: Provides responsiveness during database/file operations

- **WindowsInstallerQuery** (WindowsInstallerQuery.cs)
  - Purpose: Query Windows Installer for installed FieldWorks components
  - Inputs: Product codes, component identifiers
  - Outputs: Installation status, versions
  - Notes: Used for upgrade/installation checks

## Entry Points
- **Main executable**: FieldWorks.exe (OutputType=WinExe)
- **FieldWorks singleton**: Application entry point managing lifecycle
- **FieldWorksManager**: Facade for external access to FieldWorks instance
- **WelcomeToFieldWorksDlg**: Startup dialog for project selection (Open, New, Import)
- **ILexicalProvider**: Service interface for external lexical queries

## Test Index
- **Test project**: FieldWorksTests (FieldWorksTests.csproj)
- **Test files**: FieldWorksTests.cs, PaObjectsTests.cs, ProjectIDTests.cs, WelcomeToFieldWorksDlgTests.cs
- **Run tests**: `dotnet test FieldWorksTests/FieldWorksTests.csproj` or Visual Studio Test Explorer
- **Coverage**: Unit tests for ProjectId serialization, PA objects, welcome dialog, core infrastructure

## Usage Hints
- **Launch FieldWorks**: Run FieldWorks.exe; WelcomeToFieldWorksDlg appears for project selection
- **Access from code**: Use FieldWorksManager facade to interact with FieldWorks singleton
- **Open project programmatically**: Use FieldWorksManager.ChooseLangProject() or OpenNewWindowForApp()
- **Lexical service integration**: Implement ILexicalProvider for external tool access to FW lexicon
- **PA integration**: Use PaObjects namespace for data exchange with Phonology Assistant
- **Multi-instance coordination**: ProjectId and RemoteRequest handle opening projects in existing processes

## Related Folders
- **Common/Framework/**: Application framework (FwApp, IFwMainWnd) used by FieldWorks
- **Common/FwUtils/**: FieldWorks utilities (IFieldWorksManager, ThreadHelper) consumed by FieldWorks
- **XCore/**: Framework components integrating FieldWorks infrastructure
- **xWorks/**: Main FLEx application consumer of FieldWorks infrastructure
- **LexText/**: Uses FieldWorks for project management and application lifecycle

## References
- **Project files**: FieldWorks.csproj (net462, WinExe), FieldWorksTests/FieldWorksTests.csproj, BuildInclude.targets
- **Target frameworks**: .NET Framework 4.6.2 (net462)
- **Key dependencies**: SIL.LCModel, SIL.LCModel.Utils, DesktopAnalytics, System.Windows.Forms
- **Key C# files**: FieldWorks.cs, FieldWorksManager.cs, ProjectId.cs, ApplicationBusyDialog.cs, WelcomeToFieldWorksDlg.cs, MoveProjectsDlg.cs, FwRestoreProjectSettings.cs, WindowsInstallerQuery.cs, RemoteRequest.cs
- **LexicalProvider files**: ILexicalProvider.cs, LexicalProviderImpl.cs, LexicalServiceProvider.cs, LexicalProviderManager.cs
- **PaObjects files**: PaLexEntry.cs, PaLexSense.cs, PaCmPossibility.cs, PaMediaFile.cs, PaMultiString.cs, PaRemoteRequest.cs, PaVariant.cs, PaVariantOfInfo.cs, PaWritingSystem.cs, PaLexPronunciation.cs, PaLexicalInfo.cs, PaComplexFormInfo.cs
- **Designer files**: ApplicationBusyDialog.Designer.cs, MoveProjectsDlg.Designer.cs, WelcomeToFieldWorksDlg.Designer.cs
- **Resources**: ApplicationBusyDialog.resx, MoveProjectsDlg.resx, WelcomeToFieldWorksDlg.resx, Properties/Resources.resx
- **Icons**: BookOnCube.ico, CubeOnBook.ico, variants (Large, Small, Version)
- **Configuration**: App.config
- **Total lines of code**: 8685
- **Output**: Output/Debug/FieldWorks.exe, Output/Debug/FieldWorks.xml
- **Namespace**: SIL.FieldWorks, SIL.FieldWorks.LexicalProvider, SIL.FieldWorks.PaObjects
