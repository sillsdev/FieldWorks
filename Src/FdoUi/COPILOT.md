---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FdoUi

## Purpose
UI components for FieldWorks Data Objects (FDO). Provides user interface elements for interacting with the FieldWorks data model, including custom field management and data object visualization.

## Key Components
- **FdoUi.csproj** - Main UI library for data objects
- **FdoUiTests/FdoUiTests.csproj** - Comprehensive UI component tests


## Key Classes/Interfaces
- **InflectionFeatureEditor**
- **WfiWordformUi**
- **InflectionClassEditor**
- **BulkPosEditor**
- **LexPronunciationUi**
- **PartOfSpeechUi**
- **IFwGuiControl**
- **VcFrags**

## Technology Stack
- C# .NET WinForms/WPF
- UI controls and dialogs
- Data binding to FieldWorks data model

## Dependencies
- Depends on: Cellar (data model), Common (UI infrastructure), DbExtend (custom fields)
- Used by: xWorks, LexText, and other applications displaying data objects

## Build Information
- C# class library with UI components
- Includes comprehensive test suite
- Build with MSBuild or Visual Studio

## Testing
- Run tests: `dotnet test FdoUi/FdoUiTests/FdoUiTests.csproj`
- Tests cover UI components and data object interactions

## Entry Points
- Provides UI controls and dialogs for data object management
- Custom field editors and data visualization components

## Related Folders
- **Cellar/** - Core data model that FdoUi visualizes
- **DbExtend/** - Custom field extensions that FdoUi provides UI for
- **FwCoreDlgs/** - Additional dialogs that work with FdoUi components
- **xWorks/** - Uses FdoUi for data object display and editing


## References
- **Project Files**: FdoUi.csproj
- **Key C# Files**: BulkPosEditor.cs, DummyCmObject.cs, FdoUiCore.cs, FsFeatDefnUi.cs, FwLcmUI.cs, InflectionClassEditor.cs, InflectionFeatureEditor.cs, LexEntryUi.cs
