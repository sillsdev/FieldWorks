---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# FdoUi

## Purpose
UI components for FieldWorks Data Objects (FDO). Provides user interface elements for interacting with the FieldWorks data model, including custom field management and data object visualization.

## Key Components
### Key Classes
- **InflectionFeatureEditor**
- **WfiWordformUi**
- **InflectionClassEditor**
- **BulkPosEditorBase**
- **BulkPosEditor**
- **LexPronunciationUi**
- **PartOfSpeechUi**
- **CmObjectUi**
- **CmObjectVc**
- **CmAnalObjectVc**

### Key Interfaces
- **IFwGuiControl**

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

## Entry Points
- Provides UI controls and dialogs for data object management
- Custom field editors and data visualization components

## Related Folders
- **Cellar/** - Core data model that FdoUi visualizes
- **DbExtend/** - Custom field extensions that FdoUi provides UI for
- **FwCoreDlgs/** - Additional dialogs that work with FdoUi components
- **xWorks/** - Uses FdoUi for data object display and editing

## Code Evidence
*Analysis based on scanning 25 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 1 public interfaces
- **Namespaces**: SIL.FieldWorks.FdoUi, SIL.FieldWorks.FdoUi.Dialogs
