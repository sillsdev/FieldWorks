---
spec-id: architecture/ui-framework/winforms-patterns
created: 2026-02-05
status: draft
---

# WinForms Patterns

## Purpose

Document the standard WinForms composition patterns used across FieldWorks applications.

## Context

FieldWorks relies on WinForms for UI while layering mediator-driven composition and shared controls. This spec captures shared patterns for window and control composition.

## UI Composition Patterns

- Application shells derive from FwApp and use MainWindowDelegate for window coordination.
- XCore provides XML-driven UI composition and command routing.
- Shared controls are centralized under Common/Controls to avoid UI duplication.
- UI adapter interfaces isolate shell components from concrete sidebar/tooling implementations.

### References


## Constraints

- Keep WinForms UI on the UI thread; marshal long-running work.
- Reuse shared controls rather than duplicating custom widgets.

## Anti-patterns

- Mixing mediator logic into view controls directly.
- Creating bespoke UI adapters instead of reusing interfaces.

## Requirements

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

## Open Questions

- Should we document WPF/Avalonia experimentation boundaries here?
