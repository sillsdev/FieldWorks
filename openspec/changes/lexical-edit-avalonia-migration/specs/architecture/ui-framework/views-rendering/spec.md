## ADDED Requirements

### Requirement: Render verification supports semantic migration comparison

The render verification framework SHALL support semantic capture for migration comparisons between legacy Views/DataTree output, typed view-definition output, and Avalonia output.

#### Scenario: Semantic capture runs beside pixel capture
- **WHEN** a render baseline scenario captures a Lexical Edit view
- **THEN** it SHALL be able to emit both visual artifacts and semantic artifacts for fields, editors, visibility, bindings, focus order, and accessibility identity

#### Scenario: Avalonia comparison is supported
- **WHEN** an Avalonia implementation exists for the same scenario
- **THEN** the render verification framework SHALL compare legacy, typed IR, and Avalonia semantic artifacts before treating pixel differences as behavior regressions

### Requirement: Render timing separates compilation and control creation

Render and migration timing artifacts SHALL separate XML/import work, typed compilation, cache hits, control realization, text rendering, and capture/comparison time.

#### Scenario: Timing identifies migration bottleneck
- **WHEN** a Lexical Edit migration benchmark runs
- **THEN** the timing output SHALL identify whether time is spent in XML import, typed compilation, cache miss, legacy control creation, Avalonia control realization, text shaping, or render capture

### Requirement: Migrated Avalonia regions do not use native render pipeline

The rendering architecture SHALL treat native Views/C++ viewing, layout, measurement, hit testing, selection, and editor realization as legacy-only for migrated regions. Completed Avalonia regions SHALL render and edit through Avalonia-managed controls and text services without runtime calls into native Views render code.

#### Scenario: Native render use is visible during comparison only
- **WHEN** render verification compares a legacy region and a migrated Avalonia region
- **THEN** native Views/C++ viewing/rendering SHALL be permitted only for the legacy baseline capture
- **AND** the Avalonia capture SHALL use the managed/Avalonia rendering path exclusively

#### Scenario: Runtime native render call fails completion audit
- **WHEN** instrumentation or dependency analysis detects a migrated Avalonia region calling native Views/C++ viewing/rendering/editor infrastructure at runtime
- **THEN** the region SHALL fail the migration completion audit

#### Scenario: Linguistics service call does not fail render audit
- **WHEN** a migrated Avalonia region calls a native or external linguistics service through a managed contract
- **AND** the service does not own display, layout, hit testing, selection, or editor realization
- **THEN** the call SHALL be classified outside the render pipeline audit

### Requirement: Migrated Avalonia rendering is Graphite-free

Avalonia rendering SHALL use managed/Avalonia text services with OpenType/HarfBuzz font features and SHALL NOT use Graphite render engines, Graphite font tables, or Gecko Graphite rendering in the default Lexical Edit path.

#### Scenario: Graphite engine use fails default-readiness audit
- **WHEN** validation detects `graphite2`, `GraphiteEngine`, Graphite-enabled `RenderEngineFactory` selection, or Gecko Graphite rendering in the default Avalonia path
- **THEN** the default-readiness audit SHALL fail
