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
