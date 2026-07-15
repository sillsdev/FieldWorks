## ADDED Requirements

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
