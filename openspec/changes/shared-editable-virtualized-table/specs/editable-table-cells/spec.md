## ADDED Requirements

### Requirement: Cells edit in place using owned field controls

The table SHALL support in-cell editing by hosting the existing owned field controls
(`FwMultiWsTextField`, `FwChooserField`) inside the active cell, selected per column from the view
definition's editor descriptor / editor registry. It SHALL NOT introduce a second editor mechanism
and SHALL NOT reintroduce native `RootSite` cells.

#### Scenario: Text cell edits with the multi-writing-system field
- **WHEN** the user begins editing a multi-string column cell
- **THEN** the cell SHALL host `FwMultiWsTextField` with the column's writing systems and accept input

#### Scenario: Reference cell edits with the chooser field
- **WHEN** the user begins editing a reference/atomic-chooser column cell
- **THEN** the cell SHALL host `FwChooserField` and allow choosing a target

### Requirement: Keyboard edit flow matches the legacy contract

The table SHALL support the legacy in-cell editing keyboard flow: F2 / typing begins edit, Enter
commits and advances, Esc cancels and restores, and Tab / Shift+Tab move between editable cells
committing the current one.

#### Scenario: Enter commits and advances
- **WHEN** the user edits a cell and presses Enter
- **THEN** the edit SHALL commit and selection SHALL advance to the next row's editable cell

#### Scenario: Esc cancels and restores
- **WHEN** the user edits a cell and presses Esc before committing
- **THEN** the cell SHALL restore its prior value and no model change SHALL occur

#### Scenario: Tab moves between editable cells
- **WHEN** the user presses Tab while editing
- **THEN** the current cell SHALL commit and editing SHALL move to the next editable cell

### Requirement: Edits route through the edit-session and undo seams

Cell commit and cancel SHALL drive the existing `IEditSession` boundary, and edits SHALL be
recorded on the global undo stack through the `avalonia-undo-redo` capability so a single undo
reverts a cell edit. Edits SHALL NOT bypass the edit-session fence.

#### Scenario: Commit fences a single undoable change
- **WHEN** a cell edit commits
- **THEN** it SHALL run inside an `IEditSession` and produce one entry on the global undo stack

#### Scenario: Undo reverts a committed cell edit
- **WHEN** the user commits a cell edit and then invokes undo
- **THEN** the cell SHALL return to its prior value through the shared undo stack

#### Scenario: Cancel performs no undoable change
- **WHEN** a cell edit is canceled
- **THEN** `IEditSession.Cancel` SHALL be invoked and no entry SHALL be added to the undo stack

### Requirement: Editing meets the typing-latency budget

In-cell typing SHALL meet the existing typing-latency budget: ≤ 6 ms/keystroke at 100% DPI and
≤ 8 ms/keystroke at 150% DPI, including RTL/bidi input, measured by the existing
`TypingLatencyHarnessTests` harness.

#### Scenario: Typing latency within budget
- **WHEN** 500 keystrokes (including RTL/bidi) are applied to an editing cell
- **THEN** per-keystroke latency SHALL stay within ≤ 6 ms at 100% DPI and ≤ 8 ms at 150% DPI

### Requirement: Lexical Edit main table view edits at parity

The Lexical Edit main table view, once wired onto the shared table, SHALL support in-cell editing
of its editable columns at parity with the legacy `BrowseViewer` editing path, honoring the one
global undo stack.

#### Scenario: Lexical Edit table edits a cell
- **WHEN** the user edits an editable column in the Lexical Edit main table and commits
- **THEN** the change SHALL persist to the model through the edit session and be reversible via the global undo stack
