---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: draft
---

# Common/FieldWorks

## Purpose
Core FieldWorks-specific utilities and application infrastructure. Provides fundamental application services including settings management, busy dialogs, and application-level helpers.

## Key Components
- **FieldWorks.csproj** - Main utilities library
- **ApplicationBusyDialog** - UI for long-running operations
- **FwRegistrySettings.cs** (in Framework) - Settings management
- Application icons and resources

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


## References
- **Project Files**: FieldWorks.csproj
- **Key Dependencies**: ..\..\LexText\LexTextControls\LexTextControls
- **Key C# Files**: ApplicationBusyDialog.cs, FieldWorks.cs, FieldWorksManager.cs, FwRestoreProjectSettings.cs, MoveProjectsDlg.cs, ProjectId.cs, ProjectMatch.cs, RemoteRequest.cs
