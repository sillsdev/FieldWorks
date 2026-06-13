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

### Requirement: The DataTree split is the first migrated region

The DataTree model/view separation SHALL be executed as the first concrete migrated region of the
Lexical Edit program, not as a standalone end state.

#### Scenario: SliceSpec and IDataTreeView align to the program seams
- **WHEN** the DataTree region is implemented
- **THEN** `SliceSpec` SHALL be a concrete realization of the typed view-definition node
- **AND** `IDataTreeView` SHALL be one of the adapters selected by the two-adapter flag
- **AND** `AvaloniaDataTreeView` SHALL consume the same `DataTreeModel`/`SliceSpec` as the WinForms view

### Requirement: Functional fidelity and density are the parity target

Each migrated region SHALL be judged on functional fidelity and information density to near-pixel
tolerance, not on pixel-perfect reproduction.

#### Scenario: Region completion uses semantic and density evidence
- **WHEN** a region is proposed as complete
- **THEN** its evidence SHALL include a normalized semantic snapshot comparison and density
  measurements, with every difference classified rather than requiring identical pixels
