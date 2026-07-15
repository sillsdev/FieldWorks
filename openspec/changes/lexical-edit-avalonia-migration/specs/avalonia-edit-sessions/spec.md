## ADDED Requirements

### Requirement: Editable Avalonia regions use a hybrid edit-session boundary

Editable Avalonia regions SHALL use a hybrid edit-session boundary where UI-facing draft or staged state remains detached from live LCModel mutation until an explicit FieldWorks-owned edit session commits the change.

#### Scenario: Draft state commits through FieldWorks session
- **WHEN** a migrated editor saves changes
- **THEN** the editor SHALL apply staged changes through a FieldWorks-owned edit-session or edit-transaction service
- **AND** the commit path SHALL own LCModel transaction semantics, rollback behavior, and commit fencing

### Requirement: The authoritative edit-session contract remains FieldWorks-owned

The authoritative edit-session contract for migrated lexical editing SHALL remain FieldWorks-owned and LCModel-aware rather than delegated to a package-specific view-model framework.

#### Scenario: Package helpers do not replace commit boundary
- **WHEN** CommunityToolkit, ReactiveUI, or similar UI helpers are used for draft state, commands, or validation
- **THEN** those helpers SHALL remain outside the authoritative LCModel commit and rollback boundary

### Requirement: Simple non-persistent dialogs may use lighter draft state

Simple non-persistent dialogs or preview-only surfaces SHALL be allowed to use lighter screen-local draft state when they do not commit directly to LCModel and do not participate in migrated lexical edit-session guarantees.

#### Scenario: Preview host avoids live edit session
- **WHEN** a preview host or sample-data surface renders an editor without a live project cache
- **THEN** it MAY use staged or sample draft state without opening a live LCModel edit session
