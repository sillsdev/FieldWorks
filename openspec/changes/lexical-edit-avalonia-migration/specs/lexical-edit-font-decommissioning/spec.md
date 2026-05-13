## ADDED Requirements

### Requirement: Graphite decommissioning starts with the migration

The Lexical Edit Avalonia migration SHALL begin Graphite decommissioning immediately, even when Avalonia work is hidden behind preview hosts, feature flags, or non-default entry points. Avalonia SHALL never support Graphite.

#### Scenario: Migration start creates Graphite retirement work
- **WHEN** implementation begins for Lexical Edit Avalonia migration
- **THEN** the task plan SHALL include inventory, migration, and validation tasks for retiring Graphite from the default path

#### Scenario: Default screen is blocked by Graphite dependency
- **WHEN** Avalonia is proposed as the default Lexical Edit screen
- **THEN** Graphite dependencies SHALL be fully retired from the default path or explicitly classified as unsupported legacy dependencies outside that path

### Requirement: Graphite code, settings, and assets are inventoried

The migration SHALL inventory Graphite across native code, managed render selection, writing-system settings, UI, tests, docs, assets, browser rendering, print, PDF, and build/package files.

#### Scenario: Inventory covers known Graphite surfaces
- **WHEN** Graphite decommissioning inventory runs
- **THEN** it SHALL cover at least `Lib/src/graphite2`, `Src/views/lib/GraphiteEngine.*`, `RenderEngineFactory`, `GraphiteFontFeatures`, `FontFeaturesButton`, `DefaultFontsControl`, `FwWritingSystemSetupModel`, `IsGraphiteEnabled`, `DefaultFontFeatures`, `FontEngines.Graphite`, Graphite-specific tests, `DistFiles/Graphite`, `Build/Windows.targets`, Gecko Graphite preferences, `XWebBrowser` preview consumers, and `GeckofxHtmlToPdf`/`FieldWorksPdfMaker` assumptions

### Requirement: Font options migrate to OpenType and HarfBuzz

Graphite font feature strings SHALL NOT be preserved as Avalonia runtime behavior. Avalonia font options SHALL use OpenType/HarfBuzz-compatible feature syntax and explicit font replacement or compatibility diagnostics for Graphite-only fonts.

#### Scenario: Graphite feature string has no OpenType mapping
- **WHEN** a stored Graphite feature string or Graphite-only font cannot be mapped to OpenType/HarfBuzz behavior
- **THEN** the migration SHALL report an actionable compatibility diagnostic and SHALL NOT silently render it as equivalent in Avalonia

#### Scenario: OpenType font features are applied per writing-system run
- **WHEN** a migrated Avalonia editor renders a writing-system run
- **THEN** it SHALL apply the writing system's OpenType/HarfBuzz-compatible feature settings, font family, fallback mapping, culture/script metadata, and flow direction

### Requirement: Gecko and PDF rendering stop relying on Graphite

Gecko/XULRunner and Gecko-backed PDF/print flows SHALL NOT be part of the default Avalonia Lexical Edit rendering path if they rely on Graphite rendering.

#### Scenario: Gecko Graphite preference blocks default path
- **WHEN** default-path validation observes Gecko startup with `gfx.font_rendering.graphite.enabled` or an equivalent Graphite rendering dependency
- **THEN** Avalonia SHALL NOT become the default Lexical Edit screen until that path is replaced, disabled, or moved outside the default boundary

#### Scenario: Browser/PDF replacement is validated
- **WHEN** XHTML preview, print, or PDF behavior is retained for migrated workflows
- **THEN** the replacement SHALL be validated with OpenType/HarfBuzz-compatible font behavior and SHALL NOT depend on `XWebBrowser` Graphite rendering or `GeckofxHtmlToPdf` Graphite shaping assumptions

### Requirement: Remaining native dependencies are classified

Native dependencies outside Graphite and window hosting SHALL be classified by migration impact before Avalonia becomes default. The classification SHALL distinguish native code that owns what the user views or edits from custom linguistics services that may remain behind managed contracts.

#### Scenario: Native Views remains a render blocker
- **WHEN** a dependency uses native Views layout, selection, hit testing, editing, or interlinear rendering
- **THEN** it SHALL be treated as a migrated-region blocker, not as advanced windowing

#### Scenario: Custom linguistics tools are isolated as services
- **WHEN** a dependency uses parser tools, XAmple, Encoding Converters, ICU, Expat/ParserObject, spelling interop, or reg-free COM tooling outside rendering
- **THEN** it SHALL be documented as a service/tooling dependency that supports FieldWorks language-documentation capability
- **AND** it SHALL be kept outside the Avalonia render/editor completion gate

#### Scenario: Native viewing code is not imported as a service
- **WHEN** native code owns display, layout, measurement, hit testing, selection, scrolling, or editor realization
- **THEN** it SHALL be treated as viewing/rendering infrastructure
- **AND** it SHALL NOT be brought into a completed Avalonia region as a retained service dependency
