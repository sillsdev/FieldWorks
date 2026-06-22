## ADDED Requirements

### Requirement: Graphite remains supported on legacy surfaces until the sunset milestone

Graphite rendering SHALL remain fully functional on legacy WinForms/native-Views surfaces until the
M2 sunset milestone (Avalonia default for Lexical Edit and the majority of `RecordEditView`
consumers), and SHALL remain functionally available for existing projects from M2 until M3
(WinForms removal). No Graphite code, writing-system setting, or shipped font SHALL be removed
before M2.

#### Scenario: Legacy surface renders Graphite throughout coexistence
- **WHEN** a project with a Graphite-enabled writing system uses a legacy surface at any point before M2
- **THEN** Graphite shaping SHALL behave exactly as it did before the Avalonia migration began

#### Scenario: Sunset does not strand existing projects
- **WHEN** M2 enforcement begins
- **THEN** existing projects with Graphite-enabled writing systems SHALL continue to render via legacy surfaces until M3
- **AND** migration tooling and a published font-replacement policy SHALL be available

### Requirement: Avalonia surfaces warn instead of blocking when Graphite is requested

When an Avalonia surface renders a writing system classified as Graphite-affected, it SHALL render
with OpenType/HarfBuzz shaping and SHALL raise a graded, actionable warning. The combination of
Graphite and Avalonia SHALL NOT hard-block the surface, and SHALL NOT silently change rendering
without the warning. Graphite retirement SHALL NOT gate a region's Avalonia-default decision; the
gate is warning and classification coverage.

#### Scenario: Graphite-only font on an Avalonia surface
- **WHEN** an Avalonia surface is about to render a writing system whose resolved font is classified G3 (Graphite-only)
- **THEN** a prominent warning SHALL be shown before first render naming the writing system and font
- **AND** the warning SHALL offer switching the project to Legacy UI mode and font-migration guidance
- **AND** the text SHALL still render with fallback shaping if the user proceeds

#### Scenario: Dual-engine font with Graphite feature settings
- **WHEN** an Avalonia surface renders a writing system classified G2 (dual-engine font with Graphite feature strings)
- **THEN** a warning SHALL state that Graphite font features do not apply on this surface, at most once per project session
- **AND** the determination SHALL be recorded as a structured diagnostic

#### Scenario: Avalonia default is not blocked by Graphite presence
- **WHEN** a region proposes Avalonia as its default surface while Graphite-enabled writing systems exist in the project
- **THEN** the region's default decision SHALL be permitted provided classification and warning coverage are in place
- **AND** the native-engine audit (no `GraphiteEngineClass`/native Views shaping on the Avalonia path) SHALL still pass

### Requirement: Writing systems are classified by actual rendering impact

A classification service SHALL grade each writing system G0–G3 from `IsGraphiteEnabled`,
`DefaultFontFeatures`/per-WS feature settings, and font-table evidence (`Silf` and `GSUB`/`GPOS`
presence in the resolved font), at surface-resolution time, from immutable inputs.

#### Scenario: Flag without Graphite tables is unaffected
- **WHEN** a writing system has `IsGraphiteEnabled` true but its resolved font carries no Graphite tables
- **THEN** it SHALL be classified G0 and produce no user-facing message

#### Scenario: Dual-engine font without feature settings is informational only
- **WHEN** a writing system resolves to a font with both OpenType and Graphite tables and no Graphite feature settings
- **THEN** it SHALL be classified G1 and produce a log/setup-visible diagnostic, not a popup

#### Scenario: Classification is deterministic and auditable
- **WHEN** the same project state is classified twice
- **THEN** the tiers SHALL be identical
- **AND** each determination SHALL be available as a structured diagnostic for parity bundles and support

### Requirement: Sunset is milestone-gated and tooled, not calendar-gated

M2 Graphite deprecation enforcement SHALL be gated on shipped migration tooling (Graphite
feature-string mapping where OpenType equivalents exist, font-replacement assistance), a completed
project/fixture scan quantifying G2/G3 prevalence, and at least one release of advance notice.
After M2, new projects SHALL NOT enable Graphite; existing projects SHALL see a deprecation notice
with the timeline.

#### Scenario: Tooling gates enforcement
- **WHEN** M2 is reached but migration tooling has not shipped or the fixture scan is incomplete
- **THEN** deprecation NOTICE may ship but enforcement (blocking new Graphite enablement) SHALL wait for the tooling

#### Scenario: Settings are user data
- **WHEN** migration tooling proposes converting a writing system's Graphite settings
- **THEN** `IsGraphiteEnabled`/`DefaultFontFeatures` SHALL be rewritten only on explicit user action with undo

### Requirement: The Path B contingency has recorded pivot triggers

Implementing Graphite shaping inside Avalonia (HarfBuzz with Graphite2) SHALL NOT be undertaken
unless a recorded pivot trigger fires: the fixture scan shows Graphite-only-font usage above the
agreed threshold, a shared SIL-maintained hb-graphite2 build becomes available, or post-WinForms
Avalonia adoption makes a custom shaper sustainable. Per-field legacy editor islands inside
Avalonia surfaces (Path C) SHALL NOT be built.

#### Scenario: Pivot requires evidence
- **WHEN** Graphite-in-Avalonia work is proposed
- **THEN** the proposal SHALL cite which pivot trigger fired and its evidence

#### Scenario: No legacy islands
- **WHEN** a Graphite field would render inside an Avalonia surface
- **THEN** the resolution SHALL be the warning plus whole-surface legacy mode, never a hosted native-Views field editor
