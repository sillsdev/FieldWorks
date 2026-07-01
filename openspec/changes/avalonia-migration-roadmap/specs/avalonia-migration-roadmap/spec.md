## ADDED Requirements

### Requirement: The migration follows one ordered, gated sequence

The Avalonia migration SHALL proceed in the order proof-of-concept spike, DataTree migrated region,
Lexical Edit program, then shell, with explicit gates between phases. A phase SHALL NOT start before
its predecessor's gate evidence exists.

#### Scenario: A phase is blocked without its gate
- **WHEN** a later phase is proposed to start
- **THEN** the preceding gate's evidence (phase-0 spike evidence, region parity, or regional manifest) SHALL be
  present and passing before that phase begins

#### Scenario: Shell waits for the regional gates
- **WHEN** shell migration work is scheduled
- **THEN** it SHALL NOT default to Avalonia until the Lexical Edit regional gate (Gate 2) passes

### Requirement: WinForms remains the default until each region's gate passes

During the transition the same build SHALL run either Avalonia or the legacy WinForms controls behind
a feature flag, with WinForms as the default until a region's completion gate passes.

#### Scenario: Default path is WinForms during transition
- **WHEN** a build is produced before a region's gate passes
- **THEN** the default runtime path for that region SHALL be the WinForms controls
- **AND** the Avalonia path SHALL be reachable only by enabling the flag

### Requirement: The region-model path is the first migrated region

The first concrete migrated region of the Lexical Edit program SHALL be built via the typed
view-definition IR path (`ViewDefinitionModel` → `LexicalEditRegionModel`), not as a standalone end
state.

> **Status note (2026-06-09 — supersedes this requirement as originally written).** This requirement
> originally described Plan A (`datatree-model-view-separation`: extracting
> `DataTreeModel`/`SliceSpec`/`IDataTreeView` from `DataTree`). Execution diverged: Phase 1 was built
> as the region-model path (`ViewDefinitionModel`/`LexicalEditRegionModel`) inside
> `lexical-edit-avalonia-migration`, bypassing `DataTree` internals entirely on the Avalonia side.
> `DataTree` is frozen on the legacy side and deleted wholesale at end of the coexistence phase; see
> `datatree-model-view-separation/hybrid-alignment.md` and this change's `design.md` for the full
> context. The scenario below is retained for historical traceability but no longer reflects the
> as-built architecture — see the as-built vocabulary diagram in `design.md` instead.

#### Scenario (historical, superseded): SliceSpec and IDataTreeView align to the program seams
- **WHEN** the DataTree region is implemented
- **THEN** `SliceSpec` SHALL be a concrete realization of the typed view-definition node
- **AND** `IDataTreeView` SHALL be one of the adapters selected by the two-adapter flag
- **AND** `AvaloniaDataTreeView` SHALL consume the same `DataTreeModel`/`SliceSpec` as the WinForms view

#### Scenario: As-built region-model boundary
- **WHEN** the first migrated region is implemented
- **THEN** `ViewDefinitionModel` SHALL be the typed IR compiled from XML layouts
- **AND** `LexicalEditRegionModel` (via `IRegionValueProvider`) SHALL be the value-bound Avalonia region built from that IR
- **AND** `RecordEditView` SHALL route between the legacy and Avalonia surfaces without driving hidden `DataTree` infrastructure when Avalonia is active

### Requirement: Functional fidelity and density are the parity target

Each migrated region SHALL be judged on functional fidelity and information density to near-pixel
tolerance, not on pixel-perfect reproduction.

#### Scenario: Region completion uses semantic and density evidence
- **WHEN** a region is proposed as complete
- **THEN** its evidence SHALL include a normalized semantic snapshot comparison and density
  measurements, with every difference classified rather than requiring identical pixels
