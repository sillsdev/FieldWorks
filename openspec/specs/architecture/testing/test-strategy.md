---
spec-id: architecture/testing/test-strategy
created: 2026-02-05
status: draft
---

# Test Strategy

## Purpose

Describe the shared testing strategy across FieldWorks components.

## Context

Testing spans managed and native components with shared guidance in minimal AGENTS guidance. This spec captures the high-level approach.

## Strategy

- Managed tests use NUnit or VSTest and live alongside source projects.
- Native tests use Unit++ test runners.
- Use repository test scripts for consistent execution.

### References


## Constraints

- Use repo test scripts for consistent setup.
- Keep tests deterministic and data-driven where possible.

## Anti-patterns

- Ad-hoc test execution that bypasses repo scripts.

## Requirements

### Requirement: UI migration tests are layered by responsibility

FieldWorks UI migration tests SHALL separate pure logic, integration, semantic render verification, WinForms UIA2 workflow smoke tests, and Avalonia.Headless interaction tests.

#### Scenario: Business logic is not asserted only through UI automation
- **WHEN** behavior can be tested through services, view-definition compilation, LCModel integration, or render semantics
- **THEN** it SHALL have a non-UIA test path rather than relying only on WinForms or Avalonia UI automation

#### Scenario: UI automation remains focused
- **WHEN** a test uses UIA2 or Avalonia.Headless
- **THEN** it SHALL verify interaction wiring, accessibility/reachability, input handling, or visual realization that cannot be covered by lower-level tests

### Requirement: Test plans cover coverage gaps before refactor

Any refactor or Avalonia replacement touching Lexical Edit SHALL include either existing coverage evidence or planned tests for the affected behavior before implementation proceeds.

#### Scenario: Coverage gap is explicit
- **WHEN** a migration task identifies missing test coverage for a legacy behavior
- **THEN** the task SHALL add coverage first or record why coverage must be deferred and what parity artifact will replace it

## Open Questions

- Should we standardize test categories for faster CI filtering?
