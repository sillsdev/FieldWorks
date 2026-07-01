## ADDED Requirements

### Requirement: DetailControls refactors introduce explicit service seams

WinForms DetailControls refactors SHALL introduce explicit interfaces for DataTree services, refresh coordination, editor selection, launcher behavior, LCModel access, diagnostics, and host integration before replacing equivalent UI with Avalonia.

#### Scenario: Slice creation is reachable through an editor registry
- **WHEN** a slice/editor is created from legacy XML metadata
- **THEN** the selection of editor kind SHALL pass through a registry or service boundary that can later resolve either legacy WinForms slices or Avalonia editors

#### Scenario: Refresh behavior is testable without full UI replacement
- **WHEN** DataTree refresh behavior is refactored
- **THEN** refresh state transitions SHALL be covered by tests independent of full Lexical Edit UI automation

### Requirement: WinForms controls expose automation metadata for migration baselines

Legacy WinForms controls involved in migration baselines SHALL expose stable accessible names, roles, or automation identifiers where practical.

#### Scenario: Baseline target has stable accessible identity
- **WHEN** a UIA2 baseline targets a DataTree, slice, launcher, table header, filter, popup, or chooser control
- **THEN** the target SHALL have a stable accessible identity or a documented fallback locator strategy
