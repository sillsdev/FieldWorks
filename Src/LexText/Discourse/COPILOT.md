---
last-reviewed: 2025-10-31
last-verified-commit: 29ea474
status: draft
---

# Discourse COPILOT summary

## Purpose
Constituent chart analysis tool for discourse/clause-level text organization. Allows users to arrange words/morphemes from interlinear texts into tables where rows represent clauses and columns represent clause constituents (pre-nuclear, nuclear SVO, post-nuclear). Provides visual discourse analysis framework supporting linguistic research into clause structure, topic/comment, reference tracking. Integrates with interlinear text (IText) displaying words in interlinear format within chart cells. Supports chart templates (column configuration), word movement between columns, clause markers, moved text markers, export functionality. Core business logic (ConstituentChartLogic 6.5K lines) separated from UI (ConstituentChart 2K lines). Library (13.3K lines total).

## Architecture
C# library (net462, OutputType=Library) with MVC-like separation. ConstituentChart main UI component (inherits InterlinDocChart, implements IHandleBookmark, IxCoreColleague, IStyleSheet). ConstituentChartLogic testable business logic class. ConstChartBody custom control for chart grid. ConstChartVc view constructor for chart rendering. InterlinRibbon displays source text words for dragging into chart. Chart data stored in LCModel (IDsConstChart, IConstChartRow, IConstituentChartCellPart, IConstChartWordGroup). Export support (DiscourseExporter) for sharing/publishing charts.

## Key Components
- **ConstituentChart** (ConstituentChart.cs, 2K lines): Main chart UI component
  - Inherits InterlinDocChart (interlinear integration)
  - Implements IHandleBookmark (bookmarking), IxCoreColleague (XCore), IStyleSheet (styling)
  - InterlinRibbon m_ribbon: Source text display for word selection
  - ConstChartBody m_body: Chart grid display
  - Template selection: m_templateSelectionPanel, ICmPossibility m_template
  - Column configuration: ICmPossibility[] m_allColumns
  - MoveHere buttons: m_MoveHereButtons for column-specific word insertion
  - Context menus: m_ContextMenuButtons for cell operations
  - ConstituentChartLogic m_logic: Business logic delegate
  - Split container: m_topBottomSplit (ribbon above, chart below)
  - Column layout: m_columnWidths, m_columnPositions for rendering
- **ConstituentChartLogic** (ConstituentChartLogic.cs, 6.5K lines): Testable business logic
  - Chart operations: Move words, create rows, manage cells
  - Factories/repositories: IConstChartRowFactory, IConstChartWordGroupRepository, IConstChartMovedTextMarkerFactory, IConstChartClauseMarkerFactory, etc.
  - ChartLocation m_lastMoveCell: Track last move operation
  - NumberOfExtraColumns = 2: Row number + Notes columns
  - indexOfFirstTemplateColumn = 1: Template columns start after row number
  - Events: RowModifiedEvent, Ribbon_Changed
- **ConstChartBody** (ConstChartBody.cs, 525 lines): Chart grid control
  - Custom control displaying chart cells in table format
  - Handles mouse events for cell selection/editing
  - Integrates with ConstChartVc for rendering
- **ConstChartVc** (ConstChartVc.cs, 871 lines): View constructor for chart rendering
  - Renders chart cells with interlinear word display
  - Column headers, row numbers, cell borders
  - Styling integration via IStyleSheet
- **InterlinRibbon** (InterlinRibbon.cs, 478 lines): Source text word display
  - Shows text words available for charting
  - Drag-and-drop support to chart cells
  - Interlinear format display
- **InterlinRibbonDecorator** (InterlinRibbonDecorator.cs, 149 lines): Ribbon rendering
  - View decorator for ribbon formatting
- **ConstChartRowDecorator** (ConstChartRowDecorator.cs, 602 lines): Row rendering
  - Handles row-specific display logic
  - Row selection, highlighting
- **AdvancedMTDialog** (AdvancedMTDialog.cs, 421 lines): Moved Text marker dialog
  - Configure moved text markers (tracking displaced constituents)
  - Preposed/Postposed/Speech markers
- **SelectClausesDialog** (SelectClausesDialog.cs, 29 lines): Clause selection dialog
  - Choose clauses for charting
- **DiscourseExporter** (DiscourseExporter.cs, 374 lines): Chart export
  - Export charts for publishing/sharing
  - Multiple format support
- **DiscourseExportDialog** (DiscourseExportDialog.cs, 208 lines): Export configuration dialog
- **MakeCellsMethod** (MakeCellsMethod.cs, 682 lines): Cell creation logic
  - Factory methods for creating chart cells
- **ChartLocation** (ChartLocation.cs, 78 lines): Row/column coordinate
  - Represents position in chart grid
- **MultilevelHeaderModel** (MultilevelHeaderModel.cs, 99 lines): Column header hierarchy
  - Multi-level column headers (e.g., Nuclear: S/V/O)
- **MaxStringWidthForChartColumn** (MaxStringWidthForChartColumn.cs, 76 lines): Layout helper
  - Calculate column widths for text content

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library
- Windows Forms (custom controls)
- LCModel (data model)
- IText (interlinear integration)
- XCore (application framework)

## Dependencies

### Upstream (consumes)
- **LCModel**: Data model (IDsConstChart, IConstChartRow, IConstituentChartCellPart, IConstChartWordGroup, IConstChartMovedTextMarker, IConstChartClauseMarker)
- **IText**: Interlinear text support (InterlinDocChart base, IInterlinRibbon)
- **XCore**: Application framework (IxCoreColleague, Mediator)
- **Common/FwUtils**: Utilities
- **FwCoreDlgControls**: Dialog controls
- **Common/Controls/Widgets**: Custom widgets

### Downstream (consumed by)
- **xWorks**: Interlinear text window (chart as tab)
- **LexTextExe**: FLEx application
- **Linguists**: Discourse analysis users

## Interop & Contracts
- **IDsConstChart**: LCModel chart object (rows, columns, cells)
- **IConstChartRow**: Chart row (clause)
- **IConstituentChartCellPart**: Cell content (word group, marker)
- **IConstChartWordGroup**: Group of words in cell
- **IConstChartMovedTextMarker**: Moved text indicator
- **IConstChartClauseMarkerRepository**: Clause boundary markers
- **InterlinDocChart**: Base class for interlinear integration
- **IHandleBookmark**: Bookmark support
- **IxCoreColleague**: XCore colleague pattern

## Threading & Performance
- **UI thread**: All operations on UI thread
- **Lazy loading**: Rows/cells loaded on demand
- **Chart caching**: Template/column config cached in memory

## Config & Feature Flags
- **Chart templates**: ICmPossibility defining column structure (pre-nuclear, nuclear, post-nuclear)
- **Display modes**: Interlinear vs simple text in cells

## Build Information
- **Project file**: Discourse.csproj (net462, OutputType=Library)
- **Test project**: DiscourseTests/
- **Output**: SIL.FieldWorks.Discourse.dll
- **Build**: Via top-level FW.sln or: `msbuild Discourse.csproj`
- **Run tests**: `dotnet test DiscourseTests/`

## Interfaces and Data Models

- **ConstituentChart** (ConstituentChart.cs)
  - Purpose: Main chart UI component with ribbon and grid
  - Inputs: LcmCache, PropertyTable, IDsConstChart, chart template
  - Outputs: Visual chart display, user interactions
  - Notes: Inherits InterlinDocChart, implements IHandleBookmark, IxCoreColleague, IStyleSheet

- **ConstituentChartLogic** (ConstituentChartLogic.cs)
  - Purpose: Testable business logic for chart operations
  - Key methods: MoveWordGroup(), CreateRow(), DeleteRow(), InsertClauseMarker(), InsertMovedTextMarker()
  - Inputs: LcmCache, IDsConstChart, IInterlinRibbon
  - Outputs: Chart structure changes
  - Notes: Separated from UI for testability

- **ConstChartBody** (ConstChartBody.cs)
  - Purpose: Custom control displaying chart grid
  - Inputs: Chart data, column configuration
  - Outputs: Visual grid with cells
  - Notes: Mouse event handling for cell interaction

- **ConstChartVc** (ConstChartVc.cs)
  - Purpose: View constructor for chart rendering
  - Inputs: Chart data, styling info
  - Outputs: Rendered chart display
  - Notes: Integrates with Views rendering engine

- **InterlinRibbon** (InterlinRibbon.cs)
  - Purpose: Source text display for word selection
  - Inputs: StText, segments
  - Outputs: Interlinear word display
  - Notes: Drag-and-drop to chart cells

- **Chart data model**:
  - IDsConstChart: Root chart object
  - IConstChartRow: Row (clause) with order, label, notes
  - IConstituentChartCellPart: Cell content (base for word groups, markers)
  - IConstChartWordGroup: Group of words in cell, column assignment
  - IConstChartMovedTextMarker: Indicator of moved text (preposed/postposed)
  - IConstChartClauseMarker: Clause boundary/dependency marker

## Entry Points
Loaded by reflection from xWorks interlinear text window. ConstituentChart constructor called when user opens chart tab.

## Test Index
- **Test project**: DiscourseTests/
- **Run tests**: `dotnet test DiscourseTests/`
- **Coverage**: ConstituentChartLogic business logic, cell creation, row management

## Usage Hints
- **Open chart**: In FLEx, open text in interlinear view, select Chart tab
- **Select template**: Choose chart template (column configuration) from dropdown
- **Move words**: Drag words from ribbon to chart cells, or use MoveHere buttons
- **Create rows**: Right-click to add rows (clauses)
- **Clause markers**: Insert markers for clause boundaries, dependencies
- **Moved text**: Mark displaced constituents with moved text markers
- **Export**: Use export dialog to publish chart
- **Templates**: Configure chart templates in Lists (Chart Template)
- **Columns**: Templates define column structure (pre-nuclear, nuclear SVO, post-nuclear)
- **Business logic**: ConstituentChartLogic class separated for testability

## Related Folders
- **Interlinear/**: Interlinear text integration
- **IText/**: Text infrastructure (InterlinDocChart base)
- **LexTextExe/**: FLEx application entry point
- **xWorks/**: Main application shell

## References
- **Project file**: Discourse.csproj (net462, OutputType=Library)
- **Key C# files**: ConstituentChartLogic.cs (6491 lines), ConstituentChart.cs (2033 lines), ConstChartVc.cs (871 lines), MakeCellsMethod.cs (682 lines), ConstChartRowDecorator.cs (602 lines), ConstChartBody.cs (525 lines), InterlinRibbon.cs (478 lines), AdvancedMTDialog.cs (421 lines), DiscourseExporter.cs (374 lines), DiscourseExportDialog.cs (208 lines)
- **Test project**: DiscourseTests/
- **Total lines of code**: 13280
- **Output**: SIL.FieldWorks.Discourse.dll
- **Namespace**: SIL.FieldWorks.Discourse
