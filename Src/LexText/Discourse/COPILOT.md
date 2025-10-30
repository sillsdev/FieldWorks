---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# Discourse

## Purpose
Discourse analysis features for FieldWorks. Provides tools for analyzing and charting discourse structure in texts, including constituent chart creation and management.

## Key Components
### Key Classes
- **ConstChartBody**
- **AdvancedMTDialog**
- **DiscourseExportDialog**
- **SelectClausesDialog**
- **MultilevelHeaderModel**
- **ConstituentChartLogic**
- **RowModifiedEventArgs**
- **RowColPossibilityMenuItem**
- **UpdateRibbonAction**
- **InterlinRibbon**

### Key Interfaces
- **IInterlinRibbon**

## Technology Stack
- C# .NET WinForms
- Discourse analysis algorithms
- Chart visualization

## Dependencies
- Depends on: Cellar (data model), Common (UI infrastructure), LexText core
- Used by: LexText application discourse analysis features

## Build Information
- C# class library project
- Build via: `dotnet build Discourse.csproj`
- Part of LexText suite

## Entry Points
- Discourse chart creation and editing
- Constituent analysis tools
- Chart visualization components

## Related Folders
- **LexText/Interlinear/** - Works with interlinear text for discourse analysis
- **LexText/LexTextControls/** - UI controls for discourse features
- **Cellar/** - Stores discourse analysis data

## Code Evidence
*Analysis based on scanning 30 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 1 public interfaces
- **Namespaces**: SIL.FieldWorks.Discourse
