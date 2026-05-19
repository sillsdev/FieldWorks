## ADDED Requirements

### Requirement: Migration is phased by risk and control complexity

The Lexical Edit migration SHALL proceed in phases: baseline test coverage, refactoring seams, simple Avalonia controls and popup hovers, table/browse views, slices, and then full Lexical Edit views.

#### Scenario: Refactor gates precede Avalonia replacement
- **WHEN** a migration task replaces a Lexical Edit surface with Avalonia
- **THEN** the affected legacy behavior SHALL already have unit/integration coverage or an explicit baseline plan in `lexical-edit-parity-automation`
- **AND** required service seams SHALL be identified before UI replacement begins

#### Scenario: Control complexity determines rollout order
- **WHEN** scheduling Avalonia replacement work
- **THEN** simple editors and popup hovers SHALL be attempted before table views
- **AND** table views SHALL be attempted before slice and full Lexical Edit replacement

### Requirement: User interaction and density are preserved

Avalonia replacements SHALL preserve the legacy user interaction model, information density, keyboard/focus behavior, popup semantics, and layout hierarchy within documented near-pixel tolerances.

#### Scenario: Dense field editing parity
- **WHEN** a migrated lexical entry field group is shown in Avalonia
- **THEN** it SHALL expose equivalent labels, editor affordances, focus order, hover/popup entry points, and visible data density as the legacy DataTree baseline

#### Scenario: Pixel differences are explained
- **WHEN** Avalonia output differs visually from the WinForms baseline
- **THEN** the comparison artifact SHALL identify whether the difference is accepted near-pixel variance, font/rendering variance, missing data, or a behavior regression

### Requirement: Avalonia text uses writing-system font settings and OpenType shaping

Avalonia lexical editors SHALL use FieldWorks writing-system font settings, Avalonia/Skia text rendering, and HarfBuzz/OpenType feature support. Graphite SHALL NOT be supported in Avalonia.

#### Scenario: Writing-system text editor binds font metadata
- **WHEN** a multi-writing-system field is rendered in Avalonia
- **THEN** each writing-system alternative SHALL use the configured font family, size, flow direction, culture/script metadata, and OpenType feature settings available for that writing system

#### Scenario: Graphite-only behavior blocks default switch
- **WHEN** a legacy writing system or font depends on Graphite-only shaping or feature IDs
- **THEN** the migration SHALL block Avalonia becoming the default screen until the writing system is migrated to OpenType/HarfBuzz-compatible font settings, replaced with an acceptable font option, or documented as an unsupported legacy dependency outside the default Avalonia path

### Requirement: Graphite is decommissioned before Avalonia becomes default

Graphite decommissioning SHALL start when the Lexical Edit Avalonia migration starts. Avalonia SHALL NOT become the default Lexical Edit screen until Graphite runtime dependencies, font options, Gecko rendering assumptions, and default-path tests/docs are removed or converted to OpenType/HarfBuzz-only behavior.

#### Scenario: Stealth migration still starts Graphite retirement
- **WHEN** Avalonia Lexical Edit work begins behind flags, preview hosts, or non-default entry points
- **THEN** the work SHALL include Graphite inventory and retirement tasks from the start

#### Scenario: Default switch requires Graphite-free evidence
- **WHEN** Avalonia is proposed as the default Lexical Edit screen
- **THEN** validation SHALL prove that the default path does not call Graphite native code, create Graphite render engines, enable Gecko Graphite rendering, depend on Graphite feature strings, or require Graphite-only sample fonts

### Requirement: FieldWorks-owned controls cover domain-specific editors

The migration SHALL prefer FieldWorks-owned Avalonia editor controls over permanent dependence on generic property-grid behavior for multi-writing-system text, rich text, choosers, feature structures, references, nested sequences, and TreeView-heavy views.

#### Scenario: PropertyGrid remains a bootstrap path
- **WHEN** the current Advanced Entry PropertyGrid prototype is used for migration learning
- **THEN** it SHALL NOT define the final Lexical Edit UI contract
- **AND** final editors SHALL bind to typed view-definition/IR nodes through owned editor interfaces

#### Scenario: TreeView supports multiple translations per sense or term
- **WHEN** a migrated tree view displays senses, terms, examples, glosses, definitions, or translations
- **THEN** each tree node SHALL be able to render multiple writing-system alternatives and compact inline metadata without requiring a separate modal dialog for normal inspection

### Requirement: Package updates and control hacks are gated by parity evidence

Avalonia package updates, third-party control additions, upstream patches, or local control hacks SHALL be allowed only when tied to a specific parity, density, text, table, or automation requirement.

#### Scenario: Package change has migration justification
- **WHEN** an Avalonia package version or control dependency changes
- **THEN** the change SHALL document the blocked requirement, package/control rationale, and validation evidence from Avalonia.Headless or render parity tests

### Requirement: Legacy XML and native Views are not new dependencies

New Avalonia Lexical Edit functionality SHALL NOT require WinForms slices, XMLViews rendering, or native Views runtime to operate, except through migration importers and baseline comparison harnesses.

#### Scenario: New Avalonia editor runs from typed contract
- **WHEN** a migrated Avalonia editor is launched in the Preview Host or headless tests
- **THEN** it SHALL receive a typed view-definition/IR model and injected services
- **AND** it SHALL NOT instantiate `DataTree`, `Slice`, `RootSite`, or native Views UI components

### Requirement: C++ viewing/rendering dependencies are decommissioned by migrated region

A Lexical Edit region SHALL NOT be considered fully migrated to Avalonia until that region has no runtime dependency on native code that owns display, layout, measurement, hit testing, selection, scrolling, editor realization, native Views box/layout/rendering, `RootSite`, `IVwEnv`, `ManagedVwWindow`, or equivalent native render adapters.

#### Scenario: Region completion requires native viewing/render seam audit
- **WHEN** a Lexical Edit region is proposed as fully migrated to Avalonia
- **THEN** a dependency audit SHALL show that the region renders, measures, hit-tests, scrolls, selects, and edits without calling native Views/C++ viewing/rendering/editor infrastructure
- **AND** any remaining native render usage SHALL be limited to non-migrated regions or offline baseline comparison tools

#### Scenario: Native viewing/render bridge blocks completion
- **WHEN** an Avalonia region still relies on native Views/C++ viewing/rendering infrastructure for display, layout, text measurement, selection, hit testing, scrolling, or editor realization
- **THEN** that region SHALL remain in migration status and SHALL NOT be marked complete

#### Scenario: Custom linguistics services remain allowed
- **WHEN** a migrated Avalonia region invokes native or external linguistics services such as XAmple, spelling, parser/conversion tools, ICU, or Encoding Converters
- **THEN** those services SHALL be accessed through explicit service contracts outside the Avalonia render/editor path
- **AND** they SHALL NOT own UI display, layout, measurement, hit testing, selection, or editor realization for the migrated region

#### Scenario: Shared native code deletion waits for all consumers
- **WHEN** a migrated Lexical Edit region no longer uses native Views/C++ viewing/rendering infrastructure but other FieldWorks regions still do
- **THEN** the migrated region SHALL be complete for its scope
- **AND** global native Views deletion SHALL remain a separate tracked dependency until remaining consumers are migrated or intentionally retained
