---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FwCoreDlgs

## Purpose
Common dialogs used across FieldWorks applications. Provides a standardized set of dialog boxes and UI components for common operations like file selection, configuration, and user input.

## Key Components
- **FwCoreDlgs.csproj** - Core dialog library
- **FwCoreDlgControls/FwCoreDlgControls.csproj** - Reusable dialog controls
- **FwCoreDlgsTests/FwCoreDlgsTests.csproj** - Dialog component tests

## Technology Stack
- C# .NET WinForms
- Standard dialog patterns
- Reusable UI components

## Dependencies
- Depends on: Common (UI infrastructure), FdoUi (data object UI)
- Used by: All major FieldWorks applications (xWorks, LexText)

## Build Information
- Three C# projects: dialogs, controls, and tests
- Build with MSBuild or Visual Studio
- Provides shared dialog infrastructure

## Testing
- Run tests: `dotnet test FwCoreDlgs/FwCoreDlgsTests/FwCoreDlgsTests.csproj`
- Tests cover dialog behavior and user interactions

## Entry Points
- Provides standard dialogs (file choosers, configuration dialogs, etc.)
- Reusable controls for building consistent UI

## Related Folders
- **Common/** - UI infrastructure that FwCoreDlgs builds upon
- **FdoUi/** - Data object UI that uses FwCoreDlgs
- **xWorks/** - Primary consumer of standard dialogs
- **LexText/** - Uses FwCoreDlgs for common operations


## References
- **Project Files**: FwCoreDlgs.csproj
- **Key C# Files**: AddCnvtrDlg.cs, AddNewUserDlg.cs, AddNewVernLangWarningDlg.cs, AdvancedEncProps.cs, AdvancedScriptRegionVariantModel.cs, AdvancedScriptRegionVariantView.cs, ArchiveWithRamp.cs, BackupProjectSettings.cs
