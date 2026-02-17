---
last-reviewed: 2025-10-31
last-reviewed-tree: 2dd2ff2dfc5c4ad0fc418053ca70e45274db5128d86185c5dfefefb2529c5434
status: draft
---
anchors:
  - change-log-auto
  - purpose
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
  - referenced-by
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

# FieldWorks COPILOT summary

## Purpose
Core FieldWorks-specific application infrastructure and utilities providing fundamental application services. Includes project management (FieldWorksManager interface, ProjectId for project identification), settings management (FwRestoreProjectSettings for backup restoration), application startup coordination (WelcomeToFieldWorksDlg, FieldWorks main class), busy state handling (ApplicationBusyDialog), Windows installer querying (WindowsInstallerQuery), remote request handling (RemoteRequest), lexical service provider integration (ILexicalProvider, LexicalServiceProvider), and Phonology Assistant integration objects (PaObjects/ namespace). Central to coordinating application lifecycle, managing shared resources, and enabling interoperability across FieldWorks applications.

## Architecture
C# Windows executable (WinExe) targeting .NET Framework 4.8.x. Main entry point for FieldWorks application launcher. Contains FieldWorks singleton class managing application lifecycle, project opening/closing, and window management. Includes three specialized namespaces: LexicalProvider/ for lexicon service integration, PaObjects/ for Phonology Assistant data transfer objects, and main SIL.FieldWorks namespace for core infrastructure. Test project (FieldWorksTests) provides unit tests for project ID, PA objects, and welcome dialog.

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
C# .NET Framework 4.8.x (WinExe), Windows Forms, System.Runtime.Serialization, Windows Installer API, inter-process communication.

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

### Referenced By

- [FLExTools Integration](../../../openspec/specs/integration/external/flextools.md#behavior) — Core service contracts for scripting

## Threading & Performance
UI thread marshaling via ThreadHelper.InvokeAsync; lifecycle synchronization across FwApp instances; singleton per process.

## Config & Feature Flags
App.config, BuildInclude.targets; no explicit feature flags detected.

## Build Information
Build via FieldWorks.sln or `msbuild FieldWorks.csproj`. Test project: FieldWorksTests. Output: FieldWorks.exe, FieldWorks.xml.

### Referenced By

- [Build Phases](../../../openspec/specs/architecture/build-deploy/build-phases.md#build-ordering) — Traversal build expectations

## Interfaces and Data Models
IFieldWorksManager (pass-through facade), IProjectIdentifier (project identity), ProjectId (serializable project ID), ILexicalProvider/ILexicalServiceProvider (lexicon service contracts), PaObjects namespace (Phonology Assistant DTOs), ApplicationBusyDialog (busy indicator), WindowsInstallerQuery (installer checks).

## Entry Points
FieldWorks.exe (WinExe); FieldWorks singleton (lifecycle), FieldWorksManager (facade), WelcomeToFieldWorksDlg (startup).

## Test Index
Test project: FieldWorksTests. Run via `dotnet test` or Test Explorer.

## Usage Hints
Run FieldWorks.exe for startup dialog. Use FieldWorksManager facade for programmatic access. Implement ILexicalProvider for external lexicon queries.

## Related Folders
Common/Framework (FwApp base), Common/FwUtils (utilities), XCore (framework), xWorks (main consumer), LexText (project management).

## References
Project files: FieldWorks.csproj (net48, WinExe), FieldWorksTests, BuildInclude.targets. Key files (8685 lines): FieldWorks.cs, FieldWorksManager.cs, ProjectId.cs, LexicalProvider/, PaObjects/. See `.cache/copilot/diff-plan.json` for file details.
