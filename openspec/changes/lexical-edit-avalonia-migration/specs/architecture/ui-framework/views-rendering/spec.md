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
