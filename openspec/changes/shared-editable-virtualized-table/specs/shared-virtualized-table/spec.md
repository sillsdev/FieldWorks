## ADDED Requirements

### Requirement: Owned virtualized table renders large lists from a lazy source

The shared table SHALL render rows from the lazy `IBrowseRowSource`/`BrowseRowList` facade over a
stock `ListBox`/`VirtualizingStackPanel` (`Src/Common/FwAvalonia/Region/LexicalBrowseView.cs` and
its supporting owned types), materializing only the visible window. It SHALL NOT depend on
TreeDataGrid, `Avalonia.Controls.DataGrid`, or any commercial grid, and SHALL use only Avalonia
11.x in-box primitives.

#### Scenario: 10k rows realize only the visible window
- **WHEN** the table is bound to a 10,000-row `IBrowseRowSource`
- **THEN** fewer than 100 row containers SHALL be realized and fewer than 300 cells materialized at any time (the bound asserted by `BrowseAndCanonicalJsonTests.TenThousandRows_RealizeOnlyTheVisibleWindow`)

#### Scenario: Row count is read without materializing rows
- **WHEN** the table binds to the source
- **THEN** the row count SHALL come from `IBrowseRowSource.RowCount` without invoking `GetCellValues` for unrealized rows

#### Scenario: No commercial or unmaintained grid dependency
- **WHEN** the FwAvalonia project's dependencies are inspected
- **THEN** no reference to `Avalonia.Controls.TreeDataGrid`, `Avalonia.Controls.DataGrid`, or a third-party commercial grid SHALL be present

### Requirement: Scroll and expand meet the measured production budget

The table SHALL meet the legacy perf budget for scroll and expand on production fixtures at both
100% and 150% DPI, measured as latency — not merely realization count. The spike SHALL evaluate the
upstream `VirtualizingStackPanel` scroll/GC condition (Avalonia #18626) and record the
fire/clear of the `VirtualizingStackPanel` pivot trigger with numbers in the change manifest.

#### Scenario: Scroll budget proven on the 10k fixture
- **WHEN** the 10k-row production fixture is scrolled top-to-bottom
- **THEN** per-frame scroll latency SHALL stay within the recorded budget at 100% and 150% DPI
- **AND** the measured numbers SHALL be recorded in the change's evidence manifest

#### Scenario: Pivot trigger is resolved with evidence
- **WHEN** the 3a spike completes
- **THEN** the manifest SHALL record whether the `VirtualizingStackPanel` pivot trigger fired (escalate to an owned realization window) or cleared (stock panel retained), with the supporting measurements

### Requirement: Selection model and keyboard navigation

The table SHALL provide row and cell selection and keyboard navigation matching the legacy browse
contract: arrow keys, Home/End, PageUp/PageDown, and type-ahead, with programmatic selection that
works under virtualization (including selection of a currently de-realized row).

#### Scenario: Keyboard moves the selected row
- **WHEN** a row is selected and the user presses Down/Up/Home/End/PageDown/PageUp
- **THEN** selection SHALL move to the expected row and the row SHALL be brought into view

#### Scenario: Programmatic selection of a de-realized row
- **WHEN** code selects a row outside the realized window
- **THEN** the table SHALL scroll the row into view, realize it, and report it as the selected row

### Requirement: Column headers expose sort affordance

The table SHALL render an owned column-header bar from the view definition's field nodes, each
header carrying a sort affordance and a stable automation id, with sort applied through the row
source ordering.

#### Scenario: Header click requests sort
- **WHEN** the user activates a sortable column header
- **THEN** the table SHALL request the corresponding sort order from the row source and reflect the active sort direction in the header

### Requirement: Custom AutomationPeers enumerate virtualized rows and cells

The table SHALL provide custom AutomationPeers (an `ItemsControlAutomationPeer` subclass) that
synthesize child peers from the row count rather than from realized containers, so the UIA tree
exposes all rows and cells — including de-realized ones — with stable automation ids derived from
each row's StableId. It SHALL implement the Selection, Grid, and Invoke patterns.

#### Scenario: UIA tree exposes de-realized rows
- **WHEN** a UIA client enumerates the table while only a window of rows is realized
- **THEN** the automation tree SHALL report the full row/cell structure with stable automation ids for rows outside the realized window

#### Scenario: Stable automation ids
- **WHEN** the same row is enumerated before and after scrolling
- **THEN** its automation id SHALL be identical and derived from its StableId

### Requirement: Lexical Edit main table view is the first read consumer

FLEx's main Lexical Edit browse/table view SHALL be wired onto the shared table for read-only
display, replacing the legacy `BrowseViewer` for that surface's display path, driven by the typed
view definition's field columns and a lazy `IBrowseRowSource` over the record list.

#### Scenario: Lexical Edit browse renders on the shared table
- **WHEN** the Lexical Edit main browse view opens against a production-sized lexicon
- **THEN** it SHALL render columns from the typed view definition and rows from the lazy source on the shared table, within the read budget at 100%/150% DPI

### Requirement: Density and DPI parity

The table SHALL honor `FwAvaloniaDensity` tokens for header, row, and cell spacing and SHALL pass
the existing density/DPI parity gates at 100% and 150% DPI.

#### Scenario: Density tokens drive spacing
- **WHEN** the table renders headers, rows, and cells
- **THEN** spacing SHALL come from `FwAvaloniaDensity` and pass `VisualParityAndDensityTests`/`DensityTokenGateTests`
