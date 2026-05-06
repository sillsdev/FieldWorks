## Context

FieldWorks currently stores font feature strings generically (`tag=value`) but exposes and applies them mostly through Graphite-specific paths. Writing-system default features and style features flow through managed dialogs, `FontInfo.m_features`, `FwTextPropType.ktptFontVariations`, `VwPropertyStore`, and CSS export, but the non-Graphite Views renderer does not apply OpenType features.

LT-22324 Phase 1 must be implemented after `001-render-speedup` is merged. That branch adds render/layout dirty-state checks, warm rendering paths, and bitmap baseline infrastructure; this change must assume those optimizations exist and must treat font-feature changes as layout-changing.

The longer product phases are: add OpenType features now, remove Graphite later while retaining WinForms, add Avalonia alongside WinForms, and eventually retire WinForms. This design makes Phase 1 useful to the later phases without making HarfBuzzSharp or Avalonia part of production rendering yet.

## Goals / Non-Goals

**Goals:**

- Support OpenType font features in current WinForms/Views data entry and preview surfaces.
- Split Font Features from the `Enable Graphite` UI concept.
- Preserve Graphite behavior and existing Graphite feature support during Phase 1.
- Keep persisted feature strings renderer-neutral and compatible with future Avalonia/HarfBuzz-style consumption.
- Add tests for UI control behavior and visual rendering differences caused by feature toggles.
- Add test-only HarfBuzzSharp + SkiaSharp comparison tooling for future visual-fidelity confidence.

**Non-Goals:**

- Removing Graphite or changing Graphite project data in Phase 1.
- Replacing Views.cpp, WinForms, selection, editing, line breaking, or hit testing.
- Introducing HarfBuzzSharp, SkiaSharp, or Avalonia into production rendering.
- Guaranteeing pixel identity between GDI/Uniscribe and Skia/HarfBuzz output.

## Decisions

### 1. Renderer-neutral feature contract first

**Decision:** Keep FieldWorks feature settings as normalized `tag=value` strings at the model/UI boundary and convert only at renderer boundaries.

**Rationale:** The same stored value can be used by current Views, CSS export, test-only HarfBuzzSharp, and future Avalonia. Graphite numeric feature IDs remain an implementation detail of the Graphite adapter.

**Alternatives considered:** Reuse `GraphiteFontFeatures` for OpenType conversion. Rejected because OpenType feature tags should stay four-character tags, not Graphite numeric IDs.

### 2. Current Views renderer remains production path for Phase 1

**Decision:** Apply OpenType features in the existing native Uniscribe path using Microsoft OpenType Uniscribe APIs (`ScriptItemizeOpenType`, `ScriptShapeOpenType`, `ScriptPlaceOpenType`) while preserving the old path for empty feature sets.

**Rationale:** This is the smallest production change that preserves Views layout, drawing, selection, hit testing, bidi handling, and Graphite split. HarfBuzz is a shaper, not a full FieldWorks renderer.

**Alternatives considered:** Add a production HarfBuzz engine now. Rejected for Phase 1 because it would require a new renderer contract, COM/build/install work, and broad selection/layout parity validation.

### 3. Feature application is run/property based, not Graphite-checkbox based

**Decision:** The renderer SHALL apply OpenType feature strings from `ktptFontVariations` / `LgCharRenderProps` for the run being shaped. Engine-level feature state may be used only if it cannot produce stale style-specific output.

**Rationale:** Style-specific features and writing-system default features can differ while using the same font. Per-run feature state avoids cache collisions and covers preview, data entry, and style scenarios.

**Alternatives considered:** Pass writing-system default features to `UniscribeEngine.InitRenderer`. Rejected as insufficient because it misses style-specific `ktptFontVariations`.

### 4. Font Features UI uses providers

**Decision:** Refactor `FontFeaturesButton` around a feature provider concept: Graphite provider uses existing `IRenderingFeatures`; OpenType provider uses OpenType font/script/language/feature tag discovery; the button is enabled when the selected font has configurable features.

**Rationale:** The control should depend on “has configurable font features,” not “is Graphite.” This preserves current UI reuse in writing-system defaults, styles, and font dialogs.

**Alternatives considered:** Add OpenType conditions directly to `DefaultFontsControl`. Rejected because it would leave the shared button and style/font dialogs with duplicated logic.

### 5. HarfBuzzSharp + SkiaSharp are test-only comparison tools

**Decision:** Add HarfBuzzSharp and SkiaSharp only to test/comparison projects, not production projects. Use them to shape/render known feature scenarios and compare against legacy Views captures with tolerances.

**Rationale:** This starts migration evidence now and aligns with Avalonia/HarfBuzz direction without destabilizing production rendering.

**Alternatives considered:** Make HarfBuzzSharp the shared runtime renderer now. Rejected because current Views owns layout, drawing, selection, and editing behavior.

### 6. Visual baselines are migration assets

**Decision:** Use the post-`001-render-speedup` render snapshot framework as the golden legacy evidence set for feature-on/feature-off scenarios.

**Rationale:** Golden WinForms/Views captures help Phase 1 verification and later Avalonia comparison. Exact pixels are appropriate for same-renderer regressions; tolerant or semantic comparisons are appropriate across GDI/Uniscribe and Skia/HarfBuzz.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| OpenType APIs produce different metrics or line breaks | Add feature-on/off render baselines and native metric/selection tests. |
| Feature state is omitted from post-speedup caches | Add tasks and tests requiring feature strings in cache/dirty identity. |
| UI exposes required shaping features as toggles | Filter OpenType discovery to user-configurable optional features and provide fallback labels. |
| OpenType feature labels are incomplete or unlocalized | Use resource-backed labels for common tags and fall back to the four-character tag. |
| Test fonts cannot be redistributed | Confirm SIL Open Font License or another redistributable license before adding binaries. |
| HarfBuzz/Skia visual output differs from GDI/Uniscribe | Compare shaping data first; use tolerant image comparisons for cross-renderer evidence. |

## Migration Plan

1. Wait until `001-render-speedup` is merged into the target branch.
2. Add provider abstractions, parser/normalizer tests, and UI tests without changing rendering behavior.
3. Add OpenType feature discovery for the UI and preserve Graphite provider behavior.
4. Add native OpenType shaping/placing support and native tests.
5. Add render snapshot scenarios using the merged render baseline infrastructure.
6. Add test-only HarfBuzzSharp + SkiaSharp comparison tests in FieldWorks test projects.
7. Update help/localized UI text.

Rollback strategy: disable the OpenType provider and native OpenType shaping path behind a feature flag or fallback path if regressions are found; Graphite and old Uniscribe behavior remain available.

## Open Questions

1. Which redistributable fonts should be committed as deterministic test assets: Charis SIL 5.000, Abyssinica SIL, Lorna Evans, or a smaller purpose-built test font?
2. Should OpenType feature UI list only detected font features or also expose common tags not advertised by all fonts?
3. Should the production OpenType path use Uniscribe OpenType APIs only, or is a DirectWrite spike required before implementation?
4. Where should the test-only HarfBuzzSharp + SkiaSharp comparison project live after `001-render-speedup`: under RenderVerification, RootSiteTests, or a new dedicated test project?
