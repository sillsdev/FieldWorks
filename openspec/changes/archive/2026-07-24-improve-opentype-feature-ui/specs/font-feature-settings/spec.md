## MODIFIED Requirements

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

## ADDED Requirements

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
