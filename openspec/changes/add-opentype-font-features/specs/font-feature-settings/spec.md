## ADDED Requirements

### Requirement: Font Features are independent from Graphite enablement
FieldWorks SHALL expose Font Features as a generic font capability for Graphite and OpenType fonts, and SHALL NOT require `Enable Graphite` to be checked before OpenType font features can be viewed or selected.

#### Scenario: OpenType writing-system font enables Font Features without Graphite
- **WHEN** a user selects an OpenType-capable default font in Writing System Properties and `Enable Graphite` is unchecked
- **THEN** the Font Features control SHALL remain available when the font has configurable OpenType features

#### Scenario: Graphite enablement remains separate
- **WHEN** a user checks or unchecks `Enable Graphite`
- **THEN** FieldWorks SHALL change only Graphite renderer selection behavior and SHALL NOT erase OpenType font-feature settings

#### Scenario: Non-feature fonts disable only feature selection
- **WHEN** a selected font exposes no configurable Graphite or OpenType features
- **THEN** the Font Features control SHALL be disabled while the rest of the font selection UI remains usable

### Requirement: Feature strings are stored in a renderer-neutral format
FieldWorks SHALL store user-selected font features as normalized `tag=value` strings suitable for OpenType, CSS export, test comparison tooling, and future Avalonia consumption.

#### Scenario: OpenType features round-trip through writing-system defaults
- **WHEN** a user selects `smcp=1` for a writing-system default font
- **THEN** the writing system SHALL persist `smcp=1` as the default font feature string and reload it when the dialog is reopened

#### Scenario: OpenType features round-trip through styles and font dialogs
- **WHEN** a user selects OpenType features in the Styles Font tab or shared Font dialog
- **THEN** the selected feature string SHALL be saved through `FontInfo.m_features` / `ktptFontVariations` and restored when the style or dialog is reopened

#### Scenario: Graphite conversion is isolated
- **WHEN** Graphite rendering requires numeric feature IDs
- **THEN** conversion from four-character tags to Graphite IDs SHALL occur only at the Graphite renderer boundary

### Requirement: OpenType feature discovery supports UI selection
FieldWorks SHALL discover user-configurable OpenType feature tags for the selected font and expose them through the existing Font Features UI pattern.

#### Scenario: OpenType font lists feature tags
- **WHEN** a selected font advertises user-configurable OpenType features
- **THEN** the Font Features control SHALL list those feature tags with resource-backed friendly labels where available and tag fallback labels otherwise

#### Scenario: Required shaping features are not exposed as toggles
- **WHEN** a feature is required for script shaping and not user-configurable
- **THEN** the Font Features control SHALL NOT present it as a user toggle

#### Scenario: Existing Graphite feature discovery still works
- **WHEN** a Graphite font is selected and Graphite remains enabled
- **THEN** existing Graphite feature labels and values SHALL continue to be available through the Font Features control

### Requirement: OpenType features affect current Views rendering
FieldWorks SHALL apply OpenType font features in current WinForms/Views data entry and preview rendering paths.

#### Scenario: Writing-system default feature changes preview and data entry
- **WHEN** a writing-system default font feature such as `smcp=1` is selected for a supported OpenType font
- **THEN** both preview and data-entry Views rendering SHALL show the corresponding glyph/metric change

#### Scenario: Style-specific feature changes preview and data entry
- **WHEN** a style such as Normal specifies an OpenType feature for the vernacular writing system
- **THEN** both preview and data-entry Views rendering SHALL show the corresponding glyph/metric change for text using that style

#### Scenario: Unsupported features are safe
- **WHEN** a feature tag is unsupported by the selected font or script
- **THEN** rendering SHALL remain stable and SHALL NOT crash across managed/native boundaries

### Requirement: Font-feature changes invalidate render and layout caches
FieldWorks SHALL treat feature strings as part of render/layout identity after `001-render-speedup` is merged.

#### Scenario: Feature toggle does not reuse stale layout
- **WHEN** a font feature value changes for text already rendered in a root site
- **THEN** subsequent rendering SHALL recompute any affected shaping, layout, line breaks, and cached visual output

#### Scenario: Same font with different features remains distinct
- **WHEN** two runs use the same font, size, bold, italic, writing system, and direction but different feature strings
- **THEN** renderer and layout caches SHALL NOT conflate their shaped output

### Requirement: Test-only HarfBuzzSharp and SkiaSharp comparisons exist
FieldWorks SHALL include a test-only comparison path using HarfBuzzSharp and SkiaSharp to support future Avalonia migration confidence; this path SHALL NOT be used by production rendering in Phase 1.

#### Scenario: HarfBuzzSharp comparison verifies shaping effect
- **WHEN** a deterministic test font and feature string are shaped by the test comparison path
- **THEN** the test SHALL verify glyph IDs, clusters, advances, or offsets differ as expected when the feature is toggled

#### Scenario: SkiaSharp comparison produces visual evidence
- **WHEN** a comparison render is generated for a supported feature scenario
- **THEN** the test SHALL produce or verify a visual comparison artifact with documented tolerance rules

#### Scenario: Production assemblies do not reference test-only renderers
- **WHEN** production FieldWorks projects are built
- **THEN** HarfBuzzSharp and SkiaSharp SHALL NOT be required for production rendering or application startup

### Requirement: Help and localized UI describe Font Features generically
FieldWorks SHALL update user-visible strings and help so Font Features are described as font features, not Graphite-only options.

#### Scenario: Writing-system UI labels are generic
- **WHEN** a user opens Writing System Properties
- **THEN** labels and help text SHALL describe Font Features or Font Options without implying they only apply to Graphite fonts

#### Scenario: Help covers OpenType features
- **WHEN** a user opens the relevant FieldWorks Help topic
- **THEN** the Help content SHALL describe OpenType Font Features and their relationship to Graphite during Phase 1
