## ADDED Requirements

### Requirement: Global undo and redo remain domain-authoritative

Global undo and redo for migrated lexical editing SHALL remain authoritative at the FieldWorks or LCModel transaction layer.

#### Scenario: Domain edit participates in global undo
- **WHEN** a migrated lexical edit changes persisted project state
- **THEN** the resulting undo and redo behavior SHALL be recorded through the FieldWorks or LCModel undo infrastructure rather than a package-local view-model history

### Requirement: Control-local undo is allowed only as leaf history

Avalonia control-local undo and redo MAY be used as leaf editing history while focus remains inside a control, but SHALL NOT replace the global domain-authoritative undo model for persisted lexical edits.

#### Scenario: TextBox undo stays local until commit boundary
- **WHEN** a user is actively editing text inside a focused Avalonia control
- **THEN** the control MAY expose local undo and redo behavior for in-control text changes
- **AND** persisted lexical undo history SHALL still be routed through the domain-authoritative undo boundary

### Requirement: Grouped edits and chooser workflows use domain transactions

Grouped edits, chooser dialogs, nested edit scopes, and other workflows that affect project state SHALL use FieldWorks-owned undo grouping and transaction boundaries.

#### Scenario: Chooser confirm creates grouped undo item
- **WHEN** a chooser or popup confirms a change that mutates project state
- **THEN** the change SHALL participate in a grouped FieldWorks undo transaction with deterministic cancel and rollback behavior
