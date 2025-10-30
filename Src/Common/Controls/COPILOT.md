---
owner: FIXME(set-owner)
last-reviewed: 2025-10-29
status: draft
---

# Common/Controls

## Purpose
Shared UI controls library providing reusable widgets and XML-based view components used throughout FieldWorks applications.

## Key Components
- **Design/** - Design-time support for controls
- **DetailControls/** - Detailed view controls for data display
- **FwControls/** - Core FieldWorks control implementations
- **Widgets/** - Reusable UI widget components
- **XMLViews/** - XML-driven view rendering system

## Technology Stack
- C# .NET WinForms
- Custom control development
- XML-driven UI configuration

## Dependencies
- Depends on: Common/Framework, Common/ViewsInterfaces
- Used by: xWorks, LexText, FwCoreDlgs (UI-heavy applications)

## Build Information
- Build using the top-level FW.sln (Visual Studio/MSBuild) or run: `bash ./agent-build-fw.sh`
- Avoid building this project in isolation; solution builds ensure repo props/targets and interop settings are applied.
- Contains multiple control libraries under this folder; build as part of the full solution.
- FIXME(build): If per-project SDK-style builds are supported for any subproject, add exact commands here.

## Entry Points
- Provides reusable controls for application UIs
- XML view system for declarative UI definition

## Related Folders
- **Common/Framework/** - Application framework using these controls
- **Common/ViewsInterfaces/** - Interfaces implemented by controls
- **xWorks/** - Major consumer of Common controls
- **FwCoreDlgs/** - Dialog system using Common controls

## Review Notes (FIXME)
- FIXME(accuracy): Verify the dependency on ViewsInterfaces and Framework, and add any missing key subfolders or notable controls.

