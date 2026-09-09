# font-feature-settings Specification

## Purpose

Define OpenType font-feature support as a generic font capability independent of
Graphite: renderer-neutral `tag=value` storage, discovery and validation, cache
invalidation, inheritance, and DOCX export.
## Requirements
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
FieldWorks SHALL discover user-configurable OpenType features for the selected font, including feature parameter data (`featureParams`) and name-table strings from both GSUB and GPOS, and expose them through the existing Font Features UI pattern.

#### Scenario: OpenType font lists features with meaningful names
- **WHEN** a selected font advertises user-configurable OpenType features
- **THEN** the Font Features control SHALL list those features labeled by font-supplied names where the font provides them, catalog/resource-backed friendly names otherwise, and formatted fallback labels as a last resort

#### Scenario: Required shaping features are not exposed as toggles
- **WHEN** a feature is classified as hidden in the registered-feature catalog because it is required for script shaping or is otherwise not user-configurable
- **THEN** the Font Features control SHALL NOT present it as a user toggle

#### Scenario: Existing Graphite feature discovery still works
- **WHEN** a Graphite font is selected and Graphite remains enabled
- **THEN** existing Graphite feature labels and values SHALL continue to be available through the Font Features control, with unchanged Graphite label composition

### Requirement: Font-supplied feature names are displayed
FieldWorks SHALL read stylistic-set (`ss01`–`ss20`) UI names and character-variant (`cv01`–`cv99`) feature labels from the font's GSUB `featureParams` and name table, and SHALL display feature labels as the name alone without appending the four-character tag.

#### Scenario: Stylistic set shows its font-supplied name
- **WHEN** Charis SIL (6.200 or later) is the selected font and its features are listed
- **THEN** `ss01` SHALL be labeled "Single-story a and g" as supplied by the font

#### Scenario: Character variant shows its font-supplied label
- **WHEN** Charis SIL (6.200 or later) is the selected font and its features are listed
- **THEN** `cv43` SHALL be labeled "Capital Eng" as supplied by the font

#### Scenario: Features without font strings use numbered fallbacks
- **WHEN** a font declares an `ssXX` or `cvXX` feature without usable featureParams name strings
- **THEN** the feature SHALL be labeled with a localizable fallback of the form "Stylistic Set N" or "Character Variant N"

#### Scenario: Unknown tags stay visible with a formatted fallback
- **WHEN** a font declares a feature tag that is neither in the registered-feature catalog nor an `ssXX`/`cvXX` feature
- **THEN** the feature SHALL remain selectable and SHALL be labeled with the existing formatted fallback that includes the tag

### Requirement: Character variants expose multiple named values
FieldWorks SHALL present a character-variant feature that declares N named parameters as a multi-valued selection whose values are 0 ("None") through N, where value i selects the i-th named option, and SHALL persist the selection in the renderer-neutral `tag=value` form.

#### Scenario: Multi-option character variant offers each named option
- **WHEN** Charis SIL (6.200 or later) is the selected font and the user opens the `cv25` feature
- **THEN** the control SHALL offer "None" plus the two font-supplied option names, and selecting the second option SHALL persist `cv25=2`

#### Scenario: Persisted multi-values round-trip
- **WHEN** a feature string containing `cv25=2` is loaded into the Font Features control
- **THEN** the control SHALL show the second named option of `cv25` as the selected value

#### Scenario: Character variant without named parameters falls back to binary
- **WHEN** a font declares a `cvXX` feature without featureParams named options
- **THEN** the feature SHALL be presented as a binary on/off selection

### Requirement: Registered feature catalog governs visibility and defaults
FieldWorks SHALL classify registered OpenType features using a catalog of hidden and default-on/default-off flags audited against the OpenType feature registry, SHALL initialize unset features' displayed state from those defaults, and SHALL NOT write values for features the user has not set.

#### Scenario: Default-on features display honestly when unset
- **WHEN** a font supports `liga` and the stored feature string does not mention `liga`
- **THEN** the Font Features control SHALL show `liga` as enabled, and the stored feature string SHALL remain without a `liga` entry until the user changes it

#### Scenario: Disabling a default-on feature writes an explicit zero
- **WHEN** the user unchecks `liga` from its default-enabled display state
- **THEN** the persisted feature string SHALL contain `liga=0`

#### Scenario: Previously blocked shaping tags stay hidden
- **WHEN** a font declares tags that the prior implementation blocked as non-user-configurable, such as `mark`, `mkmk`, `init`, or `ccmp`
- **THEN** those tags SHALL remain absent from the Font Features control

#### Scenario: Discretionary ligatures remain user-visible
- **WHEN** a font declares `dlig`
- **THEN** `dlig` SHALL be listed as a user-selectable feature with its friendly name

### Requirement: Feature parameter parsing degrades safely
FieldWorks SHALL bounds-check all OpenType table parsing and SHALL degrade to tag-only feature records with fallback labels when featureParams or name-table data is truncated, malformed, or undecodable, without throwing exceptions into the UI.

#### Scenario: Truncated featureParams do not break discovery
- **WHEN** a font's GSUB declares a featureParams offset that runs past the end of the table
- **THEN** the affected feature SHALL still be listed using its fallback label and other features SHALL be unaffected

#### Scenario: Undecodable name records fall back
- **WHEN** a referenced name record has an unsupported platform/encoding pair or points outside the name-table storage
- **THEN** the feature SHALL be labeled with its fallback label instead of garbled text

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

### Requirement: Dual-technology fonts default to OpenType features
FieldWorks SHALL prefer OpenType feature discovery and selection when a selected font exposes both OpenType and Graphite feature sets, while still allowing explicit user selection of Graphite features.

#### Scenario: Dual-technology font defaults to OpenType in shared UI
- **WHEN** a selected font exposes both OpenType and Graphite feature sets
- **THEN** the shared Font Features UI SHALL open on the OpenType provider by default

#### Scenario: Explicit provider switch remains available
- **WHEN** a user intentionally switches from the OpenType provider to the Graphite provider for a dual-technology font
- **THEN** the UI SHALL update to the selected provider without silently discarding persisted font-feature data before the user confirms changes

### Requirement: Accepted OpenType tags are syntactically validated
FieldWorks SHALL accept any syntactically valid OpenType feature tag and SHALL reject malformed tags safely with trace logging.

#### Scenario: Valid custom tags are preserved
- **WHEN** a feature string contains a valid four-character printable ASCII tag that is not in a published registry allowlist
- **THEN** FieldWorks SHALL accept and persist that tag in renderer-neutral `tag=value` form

#### Scenario: Malformed tags are ignored and traced
- **WHEN** a feature string contains a malformed tag such as the wrong length, control characters, or non-ASCII characters
- **THEN** FieldWorks SHALL ignore the malformed entry, log the validation failure, and continue processing remaining valid entries

### Requirement: Malformed and overlong feature strings fail safe
FieldWorks SHALL handle malformed or overlong font-feature strings without crashing or hanging.

#### Scenario: Overlong string without comma boundary does not hang
- **WHEN** legacy truncation or normalization code receives an overlong feature string with no comma boundary
- **THEN** FieldWorks SHALL terminate safely, preserve progress where possible, and SHALL NOT loop indefinitely

#### Scenario: Mixed malformed input does not block valid settings
- **WHEN** a feature string contains both malformed entries and valid entries
- **THEN** valid entries SHALL continue to round-trip and apply while malformed entries are ignored and traced

### Requirement: Font-feature inheritance follows the authoritative path
FieldWorks SHALL use the existing style and writing-system inheritance path for default and explicit font features instead of maintaining a duplicate loading path.

#### Scenario: Default font features load through existing style processing
- **WHEN** a style inherits or overrides default font features
- **THEN** the loaded value SHALL come through the existing `BaseStyleInfo` / `FontInfo.m_features` processing path

#### Scenario: Style dialogs do not maintain a parallel default-feature loader
- **WHEN** the style dialog loads or saves font features
- **THEN** it SHALL adapt the authoritative inherited value rather than calculating a separate duplicate default-feature value path

### Requirement: Word DOCX export preserves supported OpenType font features
FieldWorks SHALL export supported OpenType font feature settings into configured dictionary/reversal Word DOCX output using documented Microsoft WordprocessingML typography elements.

#### Scenario: Character style features are exported to Word typography properties
- **WHEN** a configured Word DOCX export includes a character style with supported OpenType feature settings
- **THEN** the generated Word style run properties SHALL include the equivalent Office 2010 `w14` typography elements for supported features

#### Scenario: Explicit run features are exported to Word typography properties
- **WHEN** direct run font properties include supported OpenType feature settings
- **THEN** generated run properties SHALL include the equivalent Office 2010 `w14` typography elements for supported features

#### Scenario: Unsupported feature tags do not break Word export
- **WHEN** a feature string contains tags that WordprocessingML cannot represent, such as `cv01`, `smcp`, or private feature tags
- **THEN** Word export SHALL ignore those unsupported tags without failing the export or removing supported tags from the same feature string

#### Scenario: Word export uses a documented subset
- **WHEN** documentation describes Word export behavior
- **THEN** it SHALL list the supported WordprocessingML subset and identify arbitrary CSS-style feature tags as unsupported by DOCX export
