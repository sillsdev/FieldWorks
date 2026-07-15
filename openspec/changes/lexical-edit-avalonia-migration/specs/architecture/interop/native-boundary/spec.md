## ADDED Requirements

### Requirement: Migrated Avalonia regions eliminate viewing/render interop boundary

Completed Avalonia regions SHALL NOT use the managed-to-native Views render interop boundary for display, layout, measurement, selection, hit testing, scrolling, or editor realization.

#### Scenario: Native render interop is absent from completed region
- **WHEN** a Lexical Edit region is marked complete for Avalonia migration
- **THEN** dependency analysis or instrumentation SHALL show no runtime use of Views COM render interfaces, `RootSite`/`SimpleRootSite`, `IVwEnv`, `ManagedVwWindow`, RootBox rendering, or equivalent native render adapters from that region

#### Scenario: Native interop remains allowed outside completed region
- **WHEN** another FieldWorks region still depends on native Views rendering
- **THEN** that dependency SHALL remain outside the completed Avalonia region boundary
- **AND** it SHALL be tracked as a separate repo-wide native Views retirement blocker

### Requirement: Graphite native interop is decommissioned for Avalonia default

The Avalonia default Lexical Edit path SHALL NOT instantiate or call native Graphite render interop, including `graphite2`, `GraphiteEngine`, or Graphite-enabled `IRenderEngine` selection.

#### Scenario: Graphite COM renderer is absent
- **WHEN** Avalonia is proposed as the default Lexical Edit screen
- **THEN** dependency analysis SHALL show no default-path creation of `GraphiteEngineClass`, no Graphite `IRenderEngine` selection, and no default-path dependency on `Lib/src/graphite2`

### Requirement: Non-viewing native dependencies are classified by boundary

Native dependencies that are not windowing and not Graphite SHALL be classified before default switch as migrated-region blockers, service dependencies outside the viewing/render/editor path, or repo-wide legacy dependencies.

#### Scenario: Custom linguistics native services may remain
- **WHEN** native or external code provides custom linguistics capability such as XAmple, spelling, parser/conversion tools, ICU, or Encoding Converters
- **THEN** it SHALL be allowed to remain behind explicit service contracts
- **AND** it SHALL be kept outside Avalonia display, layout, measurement, hit testing, selection, scrolling, and editor-realization responsibilities

#### Scenario: Spell-check interop is replaced or isolated
- **WHEN** a migrated Avalonia region offers spelling behavior
- **THEN** it SHALL use a managed service boundary or another Avalonia-compatible service
- **AND** it SHALL NOT depend on RootBox `SetSpellingRepository(IGetSpellChecker)` integration

#### Scenario: Parser and conversion tools remain outside viewing/render completion gate
- **WHEN** Lexical Edit workflows invoke native/external parser, XAmple, encoding-converter, ICU, or reg-free COM infrastructure
- **THEN** those dependencies SHALL be documented as service/tooling dependencies outside Avalonia rendering
- **AND** they SHALL NOT be used to justify keeping native Views or Graphite in the migrated UI region
