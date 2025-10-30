---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FieldWorks

## Purpose
Core FieldWorks-specific utilities and application infrastructure. Provides fundamental application services including settings management, busy dialogs, and application-level helpers.

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
