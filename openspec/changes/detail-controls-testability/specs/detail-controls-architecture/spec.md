## ADDED Requirements

### Requirement: Architecture documentation exists for DetailControls

The DetailControls subsystem SHALL have an `AGENTS.md` file documenting the three-hierarchy composition pattern (Slices, Launchers, Views), key lifecycles, and the refresh notification protocol. The parent `Src/Common/Controls/AGENTS.md` currently links to `DetailControls/AGENTS.md` which does not exist.

#### Scenario: AGENTS.md is present and linked

- **WHEN** an agent or developer navigates to `Src/Common/Controls/DetailControls/`
- **THEN** an `AGENTS.md` file SHALL exist documenting the Slice, Launcher, and View hierarchies, the DataTree composition pattern, and the `DoNotRefresh`/`RefreshListNeeded`/`PropChanged` refresh protocol

#### Scenario: Parent link resolves

- **WHEN** the parent `Src/Common/Controls/AGENTS.md` references `DetailControls/AGENTS.md`
- **THEN** the link SHALL resolve to a valid file

### Requirement: Class hierarchy diagram is maintained

A Mermaid class diagram SHALL be maintained as a standalone file showing the complete inheritance and composition relationships for all Slice, Launcher, and View classes in DetailControls.

#### Scenario: Diagram renders all three hierarchies

- **WHEN** the diagram file is rendered
- **THEN** it SHALL show the Slice hierarchy (Slice → FieldSlice → ReferenceSlice → AtomicReferenceSlice → ...), the Launcher hierarchy (ButtonLauncher → ReferenceLauncher → AtomicReferenceLauncher → ...), and the View hierarchy (ReferenceViewBase → AtomicReferenceView → ...) with composition relationships to DataTree

#### Scenario: Diagram is referenced from AGENTS.md

- **WHEN** a developer reads `DetailControls/AGENTS.md`
- **THEN** it SHALL reference the class diagram file

### Requirement: Refresh protocol documentation

The `AGENTS.md` SHALL document the `DoNotRefresh` / `RefreshListNeeded` / `PropChanged` / `m_postponePropChanged` refresh protocol including the state transitions and the known bug patterns (as demonstrated by LT-22414).

#### Scenario: Refresh protocol pitfalls are documented

- **WHEN** a developer modifies code that sets `DoNotRefresh = false`
- **THEN** the documentation SHALL warn that `RefreshListNeeded` MUST be set to `true` before releasing `DoNotRefresh` if data was changed during the guarded window
