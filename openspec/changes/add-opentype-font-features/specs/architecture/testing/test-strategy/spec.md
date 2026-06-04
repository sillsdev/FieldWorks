## ADDED Requirements

### Requirement: Font-feature behavior has layered tests
FieldWorks SHALL verify font-feature behavior with layered tests covering parser/provider logic, WinForms controls, native shaping, and high-level visual rendering.

#### Scenario: UI control tests cover OpenType without Graphite
- **WHEN** managed UI tests run for the shared Font Features controls
- **THEN** they SHALL verify OpenType feature availability and persistence without requiring Graphite enablement

#### Scenario: Native tests cover feature shaping and placement
- **WHEN** native Views tests run for the Uniscribe renderer
- **THEN** they SHALL verify OpenType feature-on and feature-off shaping/placement behavior with deterministic font data

#### Scenario: Render baselines cover visual feature effects
- **WHEN** render snapshot tests run after `001-render-speedup` is merged
- **THEN** they SHALL include scenarios proving selected font features visibly affect WinForms/Views output

### Requirement: Visual baselines support future renderer migration
The render baseline test framework SHALL distinguish same-renderer regression baselines from cross-renderer migration comparisons.

#### Scenario: Legacy renderer uses stricter comparisons
- **WHEN** comparing WinForms/Views output before and after Phase 1 changes
- **THEN** tests MAY use strict or near-strict bitmap comparisons where the same renderer stack is expected

#### Scenario: HarfBuzzSharp and SkiaSharp comparisons use tolerances
- **WHEN** comparing GDI/Uniscribe output with HarfBuzzSharp/SkiaSharp output
- **THEN** tests SHALL document tolerance rules and prefer shaping-data assertions for exactness

#### Scenario: Test assets live in FieldWorks test projects
- **WHEN** deterministic fonts, baselines, or comparison specifications are added
- **THEN** they SHALL be committed under FieldWorks test projects or OpenSpec change artifacts with clear licensing and build inclusion rules
