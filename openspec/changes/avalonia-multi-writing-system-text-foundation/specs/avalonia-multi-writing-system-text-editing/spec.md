## ADDED Requirements

### Requirement: Managed `ITsString` fields round-trip without flattening
The Avalonia lexical-edit path SHALL read and write LCModel managed `ITsString` values without converting them to plain text or losing supported run boundaries, writing-system assignments, or style properties already present in the model.

#### Scenario: Existing multi-writing-system field loads with styled runs
- **WHEN** a lexical-edit field containing multiple writing-system alternatives or styled runs is shown in Avalonia
- **THEN** the editor SHALL render the current text, writing-system assignments, and supported run formatting for each alternative
- **AND** a save with no semantic text change SHALL preserve an equivalent `ITsString` in LCModel.

#### Scenario: Cluster-safe edit preserves Unicode integrity
- **WHEN** a user inserts, deletes, or replaces text that contains combining marks, surrogate pairs, or zero-width joiner sequences
- **THEN** the editor SHALL treat user-visible grapheme clusters as the unit of caret movement and deletion
- **AND** the committed `ITsString` SHALL preserve valid Unicode text without splitting or reordering cluster members unexpectedly.

### Requirement: Writing-system defaults drive font, direction, and keyboard behavior
The Avalonia text editor SHALL use the language project's writing-system settings as the default source of font family, font size, flow direction, culture or script metadata, and keyboard activation for each alternative, while allowing supported `ITsString` run properties to override display formatting where the model already carries them.

#### Scenario: Focus moves between writing-system alternatives
- **WHEN** focus moves from one writing-system alternative to another in a multi-writing-system field
- **THEN** the editor SHALL activate the keyboard associated with the focused writing system
- **AND** the focused text SHALL render with the configured default font and direction for that writing system unless overridden by the run data.

### Requirement: IME composition is explicit and testable
The Avalonia text editor SHALL model IME composition as a distinct editing state and SHALL support composition, backspace-within-composition, cancellation, and commit without prematurely mutating committed LCModel text.

#### Scenario: Composition stays local until commit
- **WHEN** a user enters text through an IME and the composition has not yet been committed
- **THEN** the pending composition SHALL remain an editor-local state
- **AND** LCModel, undo grouping, and cross-surface refresh SHALL remain unchanged until the composition is committed or canceled.

#### Scenario: Backspace within composition does not delete committed text
- **WHEN** a user presses Backspace while an IME composition is active
- **THEN** the editor SHALL update or shrink the composition text first
- **AND** it SHALL NOT delete previously committed text outside the active composition range unless the composition is already empty.

### Requirement: RTL and mixed-direction editing match lexical-edit expectations
The Avalonia text editor SHALL support RTL and mixed-direction text with correct caret placement, arrow-key movement, selection ranges, and visual ordering for the writing systems and run directions present in the edited `ITsString`.

#### Scenario: Mixed-direction caret movement
- **WHEN** a field contains both LTR and RTL text in the same editable value
- **THEN** caret movement and selection extension SHALL follow the visual and logical rules expected for the active run direction
- **AND** the editor SHALL not require native Views runtime services to compute the result.

#### Scenario: RTL parity claim requires realized-window evidence
- **WHEN** the change claims editing parity for RTL text
- **THEN** it SHALL include automated evidence for mixed-direction rendering and selection behavior
- **AND** it SHALL include realized-window or manual evidence for at least one RTL writing system.

### Requirement: Ghost text fields materialize through the same text foundation
Ghost text rows that currently depend on legacy string slices SHALL materialize their backing objects and initial text through the Avalonia text foundation and the existing fenced edit-session boundary.

#### Scenario: First committed edit realizes a ghost field
- **WHEN** a user types into a ghost text field on the Avalonia path and commits the edit
- **THEN** the required LCModel object SHALL be created through the existing creation logic for that field
- **AND** the committed text SHALL be stored as an `ITsString` through the same edit-session and undo path as non-ghost text edits.

### Requirement: Cross-surface interchange and refresh preserve TsString fidelity
The Avalonia text editor SHALL reuse the existing `TsStringWrapper` clipboard and drag/drop contract and SHALL integrate with the existing edit-session, undo or redo, and refresh seams so that rich-text edits interoperate with legacy surfaces during coexistence.

#### Scenario: Clipboard round-trip preserves supported runs
- **WHEN** a user copies a supported multi-writing-system `ITsString` value from the Avalonia editor and pastes it into another Avalonia or legacy FieldWorks editor
- **THEN** the value SHALL round-trip through the existing `"TsString"` OS clipboard format and plain-text fallback as defined by the shared seam
- **AND** supported writing-system and style runs SHALL be preserved.

#### Scenario: Avalonia commit repaints the legacy surface
- **WHEN** an Avalonia text edit is committed for a record that is also shown on a legacy surface
- **THEN** the normal LCModel notification path SHALL refresh the legacy surface without a manual refresh command
- **AND** the shared undo stack SHALL treat the commit as one undoable action.

### Requirement: Parity claims require automated, manual, and performance evidence
No migrated region SHALL claim multi-writing-system text editing parity until this foundation has passing headless tests, passing coexistence integration tests, documented realized-window or manual evidence for at least one RTL and one complex-script writing system, and recorded typing-latency budgets at 100% and 150% DPI.

#### Scenario: Evidence is incomplete
- **WHEN** any one of the headless, coexistence, manual RTL or complex-script, or typing-latency evidence lanes is missing
- **THEN** the lexical-edit migration SHALL keep the relevant region-manifest gate in a blocked or partial state
- **AND** the Avalonia UI mode SHALL NOT use this change as proof of editing parity.