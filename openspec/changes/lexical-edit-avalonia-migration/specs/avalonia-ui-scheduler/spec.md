## ADDED Requirements

### Requirement: Non-view layers use a thin UI scheduling seam

Presenters, view models outside view code, edit sessions, and migration services that need UI-thread marshalling SHALL use a thin FieldWorks-owned UI scheduling seam instead of reaching directly to Avalonia dispatcher APIs.

#### Scenario: Background load marshals through seam
- **WHEN** a migrated workflow completes background work and needs to publish results to the UI thread
- **THEN** it SHALL marshal through the UI scheduling seam rather than directly calling Avalonia dispatcher APIs from non-view layers

### Requirement: Direct Avalonia dispatcher use stays at the UI edge

Direct use of `Dispatcher.UIThread` or equivalent Avalonia dispatcher APIs SHALL remain allowed in `Program`, `App`, `Window`, `UserControl`, preview-host startup, and headless-test adapter code.

#### Scenario: Window code uses direct dispatcher
- **WHEN** view-specific code-behind or startup code needs direct dispatcher access
- **THEN** it MAY use Avalonia dispatcher APIs without routing through the scheduling seam

### Requirement: Reactive schedulers are optional local helpers

Reactive or framework-specific schedulers MAY be used as local implementation details when a screen explicitly adopts them, but SHALL NOT become the global migration scheduling contract by default.

#### Scenario: Reactive screen does not redefine app scheduler contract
- **WHEN** a migrated screen uses ReactiveUI or similar reactive helpers internally
- **THEN** that choice SHALL remain local to the screen and SHALL NOT replace the shared UI scheduling seam for the migration
