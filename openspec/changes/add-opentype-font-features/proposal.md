## Why

LT-22324 requires FieldWorks to split Font Features from the Graphite-only UI and apply OpenType font features in current WinForms/Views rendering without regressing complex-script and exotic-language support. This is needed before Graphite can be sunset and before future Avalonia work can consume the same feature settings.

The phase also needs explicit OpenType-first behavior for dual-technology fonts, traceable fallback and validation behavior, and safe handling of malformed or overlong feature strings so bad font-feature input does not crash or hang FieldWorks.

## What Changes

- Add renderer-neutral Font Feature behavior for OpenType feature strings such as `smcp=1`, preserving the existing `tag=value` storage format used by writing-system defaults, styles, and export.
- Decouple Font Features UI from `Enable Graphite` in writing-system setup, style/font dialogs, and shared font attribute controls.
- Filter discovered OpenType features so the UI exposes user-configurable optional features and does not offer required shaping features as toggles.
- Prefer OpenType feature discovery and selection by default when a font exposes both OpenType and Graphite feature sets, while still allowing an explicit Graphite choice.
- Preserve existing Graphite rendering and Graphite feature behavior during Phase 1.
- Add OpenType feature discovery for supported fonts and OpenType feature application in the current Views renderer path.
- Update render/cache invalidation rules so feature changes are treated as layout-changing, especially after `001-render-speedup` is merged.
- Add trace logging for malformed feature strings, malformed tags, filtered feature discovery, provider selection, native shaping failures, and fallback decisions.
- Accept any syntactically valid OpenType tag name, including custom/private tags; reject malformed tags safely and log them instead of narrowing accepted tags to a registry allowlist.
- Fix legacy truncation logic so overlong feature strings without comma boundaries fail safe.
- Remove duplicate default-font-feature loading and follow the existing style/font-feature inheritance path.
- Add UI/component tests for font-feature controls and high-level visual rendering tests proving feature settings change output.
- Add robustness tests for malformed input, feature filtering, OpenType-preferred toggles, truncation safety, fallback behavior, CSS/DOCX export safety, and inheritance-path round-tripping.
- Add a test-only HarfBuzzSharp + SkiaSharp comparison path for shaping/rendering confidence toward future Avalonia migration; this path is not a production renderer in Phase 1.
- Add Word DOCX export support for the subset of OpenType font features that Microsoft WordprocessingML can represent, and document unsupported feature tags.
- Bring Phase 1 closer to current best practice by retrying retryable OpenType shaping failures, preserving authoritative script/language inputs, and making output boundaries such as CSS safe for all valid tags.
- Document research for later phases: Graphite removal while retaining WinForms, Avalonia alongside WinForms, and eventual WinForms retirement.

## Non-goals

- Removing Graphite in Phase 1.
- Replacing Views.cpp, WinForms, or the FieldWorks editing/selection/layout engine in Phase 1.
- Making HarfBuzzSharp or SkiaSharp part of production rendering in Phase 1.
- Delivering Avalonia UI in Phase 1.
- Changing persisted project schema unless implementation discovers an unavoidable compatibility requirement.
- Rejecting valid custom/private OpenType tags solely because they are not in the OpenType registry.

## Capabilities

### New Capabilities

- `font-feature-settings`: User-visible and renderer-visible behavior for OpenType font feature discovery, persistence, application, cache invalidation, and verification while preserving Graphite compatibility.

### Modified Capabilities

- `architecture/ui-framework/views-rendering`: Record how current Views rendering must consume renderer-neutral font features and how `001-render-speedup` layout/render caches must treat feature changes.
- `architecture/ui-framework/winforms-patterns`: Record that Font Features UI is not Graphite-gated and must remain resource/localization friendly.
- `architecture/testing/test-strategy`: Record visual rendering baselines and test-only HarfBuzzSharp + SkiaSharp comparisons as migration evidence for future Avalonia work.

## Impact

- **Managed C# UI:** `Src/FwCoreDlgs/FwCoreDlgControls/FontFeaturesButton.cs`, `DefaultFontsControl.cs`, `FwFontAttributes.cs`, `FwFontTab.cs`, `Src/FwCoreDlgs/FwFontDialog.cs`, related `.resx` files and tests.
- **Managed rendering bridge:** `Src/Common/SimpleRootSite/RenderEngineFactory.cs` and post-`001-render-speedup` render/cache invalidation paths.
- **Native C++ Views:** `Src/views/lib/UniscribeEngine.cpp`, `UniscribeSegment.cpp`, `Render.idh` only through additive interfaces if needed, and existing Graphite code for regression coverage.
- **Feature string validation and safety:** `Src/Common/FwUtils/FontFeatureSettings.cs`, `Src/views/VwPropertyStore.cpp`, `Src/xWorks/CssGenerator.cs`.
- **Inheritance path cleanup:** `Src/FwCoreDlgs/FwCoreDlgControls/StyleInfo.cs`, `Localizations/LCM/src/SIL.LCModel/DomainServices/BaseStyleInfo.cs`.
- **Tests:** FwCore dialog/control tests, SimpleRootSite/render-factory tests, native Views tests, and post-`001-render-speedup` render baseline/snapshot tests.
- **Word DOCX export:** `Src/xWorks/WordStylesGenerator.cs`, configured dictionary/reversal DOCX tests in `Src/xWorks/xWorksTests/LcmWordGeneratorTests.cs`, and OpenType export documentation.
- **Test-only dependencies:** HarfBuzzSharp + SkiaSharp in test/comparison projects only.
- **Documentation/help:** FieldWorks Help and localized UI text for the renamed Font Features/Font Options surfaces, plus review-driven OpenSpec artifacts.
