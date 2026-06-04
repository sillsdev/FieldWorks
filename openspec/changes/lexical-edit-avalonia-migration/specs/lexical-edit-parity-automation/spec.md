## ADDED Requirements

### Requirement: Legacy WinForms behavior has layered baselines

Before refactoring or replacing a Lexical Edit surface, the system SHALL have unit, integration, render, semantic, or UIA2 baseline coverage appropriate to the surface risk.

#### Scenario: Baseline plan exists before refactor
- **WHEN** a refactor touches DataTree, SliceFactory, Slice, launchers, XMLViews table views, popup choosers, or RecordEditView hosting
- **THEN** the change SHALL identify existing tests or add a baseline task covering the affected behavior

### Requirement: UIA2 automation covers legacy workflow reachability

Legacy WinForms automation SHALL use UIA2-style workflow tests only for stable, user-observable reachability such as focus, launcher buttons, chooser dialogs, context menus, table headers, filters, and popup windows.

#### Scenario: UIA2 smoke test drives chooser launch
- **WHEN** a legacy reference or possibility field exposes a chooser launcher
- **THEN** the UIA2 baseline SHALL be able to focus the field, invoke the launcher, observe the chooser window, and cancel or accept it deterministically

#### Scenario: Owner-drawn content uses fallback assertions
- **WHEN** UIA2 cannot inspect owner-drawn or Views-backed content deeply
- **THEN** the baseline SHALL pair workflow automation with render snapshots, semantic snapshots, or model assertions

### Requirement: Avalonia.Headless covers new control interaction

Avalonia controls SHALL have headless tests for behavior that cannot be proven by pure unit tests, including input, expand/collapse, hover/flyout activation, context menus, selection, validation state, and virtualized sequence behavior.

#### Scenario: Headless text input updates staged state
- **WHEN** an Avalonia headless test focuses a migrated text editor and sends text input
- **THEN** the editor SHALL update the bound staged state or LCModel-backed edit session according to the active migration phase

#### Scenario: Headless tree expansion realizes expected nodes
- **WHEN** a headless test expands a migrated tree node for senses, terms, or translations
- **THEN** the expected child nodes SHALL be realized without creating unrelated off-screen editor controls

#### Scenario: Headless test covers editor platform seams
- **WHEN** an Avalonia editor is tested headlessly
- **THEN** the test SHALL cover command shortcuts, popup focus return, validation state, edit commit/cancel, keyboard/IME behavior where practical, accessibility metadata, and disposal/subscription cleanup

### Requirement: Render framework captures semantic parity

Render verification SHALL capture semantic snapshots in addition to pixel/timing artifacts for legacy WinForms, typed IR, and Avalonia outputs.

Semantic snapshots SHALL normalize volatile values and key comparisons around stable node IDs, class/flid/object binding, editor descriptors, writing-system metadata, ghost state, focus order, accessibility identity, and migration diagnostics.

#### Scenario: Semantic snapshot identifies fields and bindings
- **WHEN** a lexical entry view is captured
- **THEN** the semantic artifact SHALL include visible sections, field labels, editor kinds, object/class/flid or binding identity, writing-system metadata, visibility state, ghost state, expansion state, focus order, and accessibility identity where available

#### Scenario: Legacy and Avalonia comparison reports meaningful differences
- **WHEN** legacy and Avalonia outputs are compared
- **THEN** the report SHALL distinguish missing semantic nodes, accepted visual variance, accessibility differences, timing differences, and unsupported migration gaps

#### Scenario: Snapshot avoids incidental layout brittleness
- **WHEN** a visual layout difference does not alter stable semantic identity, editor kind, binding, focus order, accessibility identity, or accepted density thresholds
- **THEN** the parity result SHALL classify it as visual variance rather than a semantic regression

### Requirement: Migration gates include behavior matrices

Each region proposed for Avalonia completion SHALL provide behavior matrices for undo/redo, LCModel transactions, keyboard/focus behavior, accessibility metadata, localization/resource identity, customer overrides, dynamic editor diagnostics, performance budgets, native-call instrumentation, and Graphite-free default behavior.

#### Scenario: Missing behavior matrix blocks completion
- **WHEN** a region is proposed as completed
- **THEN** missing required behavior matrix evidence SHALL block completion even if visual rendering appears correct

### Requirement: Failure artifacts are actionable

Failed parity automation SHALL preserve enough evidence to diagnose the failing layer.

#### Scenario: Parity failure emits bundled evidence
- **WHEN** a parity test fails
- **THEN** it SHALL write or reference the relevant trace log, semantic snapshot, screenshot or diff image, timing data, and root capability/scenario id

### Requirement: Graphite decommissioning has validation artifacts

The migration SHALL include validation artifacts proving Graphite is absent from the Avalonia default path and that affected projects/fonts receive actionable migration results.

#### Scenario: Graphite-dependent fixture is detected
- **WHEN** a fixture contains Graphite-enabled writing-system settings, Graphite feature strings, or Graphite-only sample fonts
- **THEN** parity automation SHALL report a decommissioning diagnostic, replacement/migration status, or explicit unsupported legacy status

#### Scenario: Gecko/PDF Graphite path is not part of Avalonia default
- **WHEN** preview, print, or PDF flows are validated for the default Avalonia Lexical Edit path
- **THEN** the artifacts SHALL prove they do not depend on Gecko Graphite rendering, `XWebBrowser` Graphite behavior, or `GeckofxHtmlToPdf` Graphite shaping assumptions
