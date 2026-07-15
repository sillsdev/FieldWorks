## ADDED Requirements

### Requirement: Views rendering consumes renderer-neutral font features
The Views rendering architecture SHALL treat `ktptFontVariations` / run font-feature strings as renderer-neutral input that may be consumed by Graphite, OpenType Uniscribe, or future renderers.

#### Scenario: OpenType features are applied at shaping time
- **WHEN** a run reaches the Uniscribe renderer with a non-empty OpenType feature string
- **THEN** shaping and placement SHALL use the feature string when producing glyphs, advances, and offsets

#### Scenario: Empty feature strings preserve existing behavior
- **WHEN** a run has no feature string
- **THEN** Views rendering SHALL preserve the existing no-feature Uniscribe behavior

### Requirement: Post-speedup caches include font-feature identity
After `001-render-speedup` is merged, Views rendering caches and dirty-state guards SHALL include font-feature changes anywhere those changes can alter glyphs, metrics, layout, or captured pixels.

#### Scenario: NeedsReconstruct or layout dirty state observes feature changes
- **WHEN** a writing-system default feature or style feature changes
- **THEN** affected root boxes SHALL be marked for reconstruct/layout as needed before drawing

#### Scenario: Warm render paths do not reuse stale feature output
- **WHEN** a warm render path or buffered frame path is entered after a feature change
- **THEN** it SHALL not reuse a visual or shaped result created with a different feature string

### Requirement: Production renderer changes remain additive to COM contracts
OpenType feature support SHALL avoid breaking existing COM vtables and reg-free COM activation.

#### Scenario: Feature discovery needs additional metadata
- **WHEN** OpenType feature discovery needs metadata not representable by existing interfaces
- **THEN** the implementation SHALL add an additive interface or managed seam rather than changing existing interface method order or signatures

### Requirement: OpenType shaping failures are observable and safe
Views rendering SHALL log retry and fallback decisions for OpenType shaping failures and SHALL preserve graceful continuation.

#### Scenario: Retryable buffer sizing errors are retried first
- **WHEN** `ScriptShapeOpenType` or `ScriptPlaceOpenType` returns `E_OUTOFMEMORY`
- **THEN** the renderer SHALL retry with larger buffers before abandoning the OpenType shaping path

#### Scenario: OpenType fallback reason is traced
- **WHEN** native rendering falls back from the OpenType path to an older shaping path or ignores malformed feature input
- **THEN** the renderer SHALL emit trace diagnostics describing the fallback reason

### Requirement: Script and language inputs use authoritative sources
Views rendering SHALL preserve authoritative script and language inputs for OpenType shaping wherever practical.

#### Scenario: Script tags come from itemization
- **WHEN** OpenType shaping is used for a run
- **THEN** the authoritative script tag SHALL come from `ScriptItemizeOpenType` / `SCRIPT_ANALYSIS` rather than a handwritten guess

#### Scenario: Language-tag fallback is explicit
- **WHEN** the renderer cannot obtain an authoritative language tag directly from its preferred mapping path
- **THEN** it SHALL use a documented fallback strategy and trace that fallback
