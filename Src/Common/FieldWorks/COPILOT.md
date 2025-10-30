---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# FieldWorks

## Purpose
Core FieldWorks-specific application infrastructure and utilities.
Provides fundamental application services including project management (FieldWorksManager, ProjectId),
settings management (FwRestoreProjectSettings), startup coordination (WelcomeToFieldWorksDlg),
and busy state handling (ApplicationBusyDialog). Central to coordinating application lifecycle
and managing shared resources across FieldWorks applications.

## Key Components
### Key Classes
- **WindowsInstallerQuery**
- **FwRestoreProjectSettings**
- **FieldWorks**
- **ProjectId**
- **FieldWorksManager**
- **MoveProjectsDlg**
- **ApplicationBusyDialog**
- **RemoteRequest**
- **FieldWorksTests**
- **PaObjectsTests**

### Key Interfaces
- **ILexicalServiceProvider**
- **ILexicalProvider**

## Technology Stack
- C# .NET
- Windows registry integration
- Application infrastructure patterns

## Dependencies
- Depends on: Common/Framework, Common/FwUtils
- Used by: All FieldWorks applications (xWorks, LexText)

## Build Information
- Build using the top-level FW.sln (Visual Studio/MSBuild) or run: `bash ./agent-build-fw.sh`
- Avoid building this project in isolation; solution builds ensure repo props/targets and interop settings are applied.
-

## Entry Points
- Provides application-level utilities
- Settings and configuration management
- Application state and busy indicators

## Related Folders
- **Common/Framework/** - Application framework components
- **Common/FwUtils/** - General utilities used by FieldWorks
- **xWorks/** - Main application using these utilities
- **XCore/** - Framework that integrates FieldWorks utilities

## Code Evidence
*Analysis based on scanning 30 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 2 public interfaces
- **Namespaces**: SIL.FieldWorks, SIL.FieldWorks.LexicalProvider, SIL.FieldWorks.PaObjects
- **Project references**: ..\..\LexText\LexTextControls\LexTextControls

## Interfaces and Data Models

- **ILexicalProvider** (interface)
  - Path: `LexicalProvider/ILexicalProvider.cs`
  - Public interface definition

- **ILexicalServiceProvider** (interface)
  - Path: `LexicalProvider/ILexicalProvider.cs`
  - Public interface definition

- **FieldWorks** (class)
  - Path: `FieldWorks.cs`
  - Public class implementation

- **FieldWorksManager** (class)
  - Path: `FieldWorksManager.cs`
  - Public class implementation

- **FwRestoreProjectSettings** (class)
  - Path: `FwRestoreProjectSettings.cs`
  - Public class implementation

- **LexGloss** (class)
  - Path: `LexicalProvider/ILexicalProvider.cs`
  - Public class implementation

- **LexSense** (class)
  - Path: `LexicalProvider/ILexicalProvider.cs`
  - Public class implementation

- **LexicalEntry** (class)
  - Path: `LexicalProvider/ILexicalProvider.cs`
  - Public class implementation

- **LexicalProviderImpl** (class)
  - Path: `LexicalProvider/LexicalProviderImpl.cs`
  - Public class implementation

- **LexicalServiceProvider** (class)
  - Path: `LexicalProvider/LexicalServiceProvider.cs`
  - Public class implementation

- **PaCmPossibility** (class)
  - Path: `PaObjects/PaCmPossibility.cs`
  - Public class implementation

- **PaLexEntry** (class)
  - Path: `PaObjects/PaLexEntry.cs`
  - Public class implementation

- **PaLexSense** (class)
  - Path: `PaObjects/PaLexSense.cs`
  - Public class implementation

- **PaMediaFile** (class)
  - Path: `PaObjects/PaMediaFile.cs`
  - Public class implementation

- **PaMultiString** (class)
  - Path: `PaObjects/PaMultiString.cs`
  - Public class implementation

- **PaRemoteRequest** (class)
  - Path: `PaObjects/PaRemoteRequest.cs`
  - Public class implementation

- **PaVariant** (class)
  - Path: `PaObjects/PaVariant.cs`
  - Public class implementation

- **PaVariantOfInfo** (class)
  - Path: `PaObjects/PaVariantOfInfo.cs`
  - Public class implementation

- **PaWritingSystem** (class)
  - Path: `PaObjects/PaWritingSystem.cs`
  - Public class implementation

- **ProjectId** (class)
  - Path: `ProjectId.cs`
  - Public class implementation

- **RemoteRequest** (class)
  - Path: `RemoteRequest.cs`
  - Public class implementation

- **WindowsInstallerQuery** (class)
  - Path: `WindowsInstallerQuery.cs`
  - Public class implementation

- **ButtonPress** (enum)
  - Path: `WelcomeToFieldWorksDlg.cs`

- **EntryType** (enum)
  - Path: `LexicalProvider/ILexicalProvider.cs`

- **LexemeType** (enum)
  - Path: `LexicalProvider/ILexicalProvider.cs`

- **ProjectMatch** (enum)
  - Path: `ProjectMatch.cs`

- **WaitFor** (enum)
  - Path: `ApplicationBusyDialog.cs`

## References

- **Project files**: FieldWorks.csproj, FieldWorksTests.csproj
- **Target frameworks**: net462
- **Key dependencies**: ..\..\LexText\LexTextControls\LexTextControls
- **Key C# files**: ApplicationBusyDialog.Designer.cs, FieldWorks.cs, FieldWorksManager.cs, FwRestoreProjectSettings.cs, MoveProjectsDlg.Designer.cs, MoveProjectsDlg.cs, ProjectId.cs, WelcomeToFieldWorksDlg.Designer.cs, WelcomeToFieldWorksDlg.cs, WindowsInstallerQuery.cs
- **Source file count**: 35 files
- **Data file count**: 5 files

## Architecture
C# library with 35 source files. Contains 1 subprojects: FieldWorks.

## Interop & Contracts
Uses COM, P/Invoke for cross-boundary calls.

## Threading & Performance
Threading model: explicit threading, UI thread marshaling, synchronization.

## Config & Feature Flags
Config files: App.config. configuration settings.

## Test Index
Test projects: FieldWorksTests. 4 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Console application. Build and run via command line or Visual Studio. See Entry Points section.
