## ADDED Requirements

### Requirement: Typed view definition is the canonical migration boundary

The system SHALL define a managed typed view-definition model and Presentation IR for Lexical Edit that represents sections, fields, sequences, table regions, tree nodes, labels, visibility, ghost behavior, editor descriptors, writing-system metadata, and stable node identity.

#### Scenario: IR represents LexEntry layout semantics
- **WHEN** the LexEntry detail contract is compiled
- **THEN** the typed model SHALL include stable nodes for identity fields, morphology, senses, examples, references, custom fields, ghost add-first-item affordances, and nested sequences required by the parity checklist

#### Scenario: IR is renderer independent
- **WHEN** a typed view-definition model is produced
- **THEN** it SHALL be consumable by semantic tests, legacy comparison adapters, and Avalonia renderers without exposing XML nodes or WinForms controls as the public contract

### Requirement: XML Parts/Layout imports into the typed model during transition

The system SHALL import existing XML Parts/Layout definitions, including override/unification behavior, into the typed view-definition model during the migration period.

#### Scenario: Production XML imports with overrides
- **WHEN** a project uses shipped LexEntry Parts/Layout plus user overrides
- **THEN** the importer SHALL apply the same effective ordering and override/unification semantics as the legacy view resolution for covered constructs

#### Scenario: Unsupported XML construct is explicit
- **WHEN** the importer encounters an XML construct not yet supported by the typed model
- **THEN** it SHALL emit a diagnostic tied to the layout part and node path rather than silently dropping the construct

### Requirement: View-definition services use dependency injection

View-definition compilation and rendering SHALL depend on interfaces for layout source access, XML import, schema/model metadata, writing-system services, editor registry, cache, diagnostics, and LCModel access.

#### Scenario: Compiler has replaceable services
- **WHEN** a unit test compiles a view definition
- **THEN** it SHALL be able to supply fake layout source, metadata, writing-system, editor registry, and diagnostics services without constructing WinForms controls

### Requirement: Compilation is cacheable, deterministic, and off the UI thread

Typed view-definition compilation SHALL be deterministic, cacheable by stable keys, cancellable, and runnable off the UI thread.

#### Scenario: Warm compile reuses cache
- **WHEN** the same root class, layout id, project configuration fingerprint, writing-system profile, and override set are compiled twice
- **THEN** the second compile SHALL reuse the cached typed result

#### Scenario: UI thread is not blocked by heavy compilation
- **WHEN** a Lexical Edit view opens or changes root layout
- **THEN** heavy XML import, custom-field expansion, and semantic compilation SHALL run outside the UI thread and support cancellation

### Requirement: XML retirement requires migration tooling and parity gates

Runtime XML dependency SHALL be retired only after typed view-definition authoring/import/migration tooling and parity gates cover production layouts, user overrides, custom fields, ghost items, choosers, table views, and nested lexical structures.

#### Scenario: XML retirement is blocked by uncovered behavior
- **WHEN** a covered production layout behavior cannot be represented in the typed view-definition model
- **THEN** runtime XML retirement SHALL remain blocked for that surface

#### Scenario: Canonical view definition replaces XML at runtime
- **WHEN** a Lexical Edit surface has passed migration gates
- **THEN** the runtime UI SHALL load the canonical typed definition directly while retaining XML import only for migration/audit scenarios
