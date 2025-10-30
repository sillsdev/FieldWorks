---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# Discourse

## Purpose
Discourse analysis and charting functionality for linguistic text analysis.
Implements tools for analyzing and visualizing discourse structure, including constituent charts,
template-based analysis, and discourse hierarchy navigation. Enables linguists to study and
document how texts are organized at levels above the sentence.

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

## Interfaces and Data Models

- **IInterlinRibbon** (interface)
  - Path: `ConstituentChartLogic.cs`
  - Public interface definition

- **ChartHeaderView** (class)
  - Path: `ConstituentChart.cs`
  - Public class implementation

- **ChartLocation** (class)
  - Path: `ChartLocation.cs`
  - Public class implementation

- **ConstChartBody** (class)
  - Path: `ConstChartBody.cs`
  - Public class implementation

- **ConstituentChartLogic** (class)
  - Path: `ConstituentChartLogic.cs`
  - Public class implementation

- **DialogInterlinRibbon** (class)
  - Path: `InterlinRibbon.cs`
  - Public class implementation

- **DiscourseExportDialog** (class)
  - Path: `DiscourseExportDialog.cs`
  - Public class implementation

- **DiscourseExporter** (class)
  - Path: `DiscourseExporter.cs`
  - Public class implementation

- **FakeConstituentChart** (class)
  - Path: `DiscourseTests/ConstituentChartTests.cs`
  - Public class implementation

- **InterlinRibbon** (class)
  - Path: `InterlinRibbon.cs`
  - Public class implementation

- **InterlinRibbonDecorator** (class)
  - Path: `InterlinRibbonDecorator.cs`
  - Public class implementation

- **MultilevelHeaderModel** (class)
  - Path: `MultilevelHeaderModel.cs`
  - Public class implementation

- **NotifyChangeSpy** (class)
  - Path: `DiscourseTests/NotifyChangeSpy.cs`
  - Public class implementation

- **RowColPossibilityMenuItem** (class)
  - Path: `ConstituentChartLogic.cs`
  - Public class implementation

- **RowModifiedEventArgs** (class)
  - Path: `ConstituentChartLogic.cs`
  - Public class implementation

- **UpdateRibbonAction** (class)
  - Path: `ConstituentChartLogic.cs`
  - Public class implementation

- **MultilevelHeaderNode** (struct)
  - Path: `MultilevelHeaderModel.cs`

- **NotifyChangeInfo** (struct)
  - Path: `DiscourseTests/NotifyChangeSpy.cs`

- **FindWhereToAddResult** (enum)
  - Path: `ConstituentChartLogic.cs`

## References

- **Project files**: Discourse.csproj, DiscourseTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AdvancedMTDialog.cs, ConstChartBody.cs, ConstituentChart.cs, ConstituentChartLogic.cs, DiscourseExportDialog.cs, DiscourseExporter.cs, InterlinRibbon.cs, MaxStringWidthForChartColumn.cs, MultilevelHeaderModel.cs, SelectClausesDialog.cs
- **Source file count**: 36 files
- **Data file count**: 5 files

## Architecture
C# library with 36 source files. Contains 1 subprojects: Discourse.

## Interop & Contracts
Uses COM for cross-boundary calls.

## Threading & Performance
Threading model: UI thread marshaling.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Test Index
Test projects: DiscourseTests. 14 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.
