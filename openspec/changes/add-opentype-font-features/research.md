## Phase Scope

Phase 1 is LT-22324: add OpenType Font Features to current WinForms/Views while preserving Graphite. Implementation is assumed to start after `001-render-speedup` is merged.

The native Views feature-by-feature migration inventory is captured in `views-migration-matrix.md`. That matrix treats Views as a document/view engine, not only a renderer, and stages each subsystem across Phase 1 through Phase 4.

Phases 2-4 are research context only for this change:

- Phase 2: remove Graphite while retaining WinForms.
- Phase 3: add Avalonia alongside WinForms.
- Phase 4: retire WinForms years later.

## External Findings

### LT-22324 JIRA Findings

The refreshed Atlassian read-only script fetched `LT-22324` successfully on
2026-04-30. The issue asks to split `Font Features` from `Enable Graphite`,
rename the Graphite-only group to `Font Options`, list and save OpenType
features similarly to Graphite features, and keep feature enablement independent
from Graphite unless the selected font only exposes Graphite features.

The issue's developer note suggests considering HarfBuzzSharp. The Phase 1
decision remains to keep production rendering on the existing Views/Uniscribe
path and use HarfBuzzSharp only as test comparison infrastructure.

The issue points to LT-22351 for acceptance testing and says features should
work with both Graphite and OpenType. It specifically names these Lorna Evans
fonts as having both Graphite and OpenType tables:

- `https://software.sil.org/downloads/r/charis/CharisSIL-5.000.zip`
- `https://software.sil.org/downloads/r/abyssinica/AbyssinicaSIL-2.201.zip`

The issue has one comment: "This will affect FLEx Help." No attachments were
returned.

### Uniscribe OpenType

Microsoft documents the OpenType Uniscribe path as a coordinated API set: `ScriptItemizeOpenType`, `ScriptShapeOpenType`, and `ScriptPlaceOpenType`. OpenType feature data is supplied through `TEXTRANGE_PROPERTIES` and `OPENTYPE_FEATURE_RECORD`.

Useful references:

- https://learn.microsoft.com/en-us/windows/win32/intl/displaying-text-with-uniscribe
- https://learn.microsoft.com/en-us/windows/win32/api/usp10/nf-usp10-scriptitemizeopentype
- https://learn.microsoft.com/en-us/windows/win32/api/usp10/nf-usp10-scriptshapeopentype
- https://learn.microsoft.com/en-us/windows/win32/api/usp10/nf-usp10-scriptplaceopentype
- https://learn.microsoft.com/en-us/windows/win32/api/usp10/ns-usp10-textrange_properties
- https://learn.microsoft.com/en-us/windows/win32/api/usp10/ns-usp10-opentype_feature_record

Feature discovery can use `ScriptGetFontScriptTags`, `ScriptGetFontLanguageTags`, and `ScriptGetFontFeatureTags`. Required shaping features are controlled by the shaping engine and should not be exposed as user toggles.

### HarfBuzz / HarfBuzzSharp

HarfBuzz shapes a run of text into glyph IDs, clusters, advances, and offsets. It does not handle bidi paragraph analysis, line breaking, font fallback, drawing, editing, selection, or hit testing by itself.

Useful references:

- https://harfbuzz.github.io/what-is-harfbuzz.html
- https://harfbuzz.github.io/what-harfbuzz-doesnt-do.html
- https://harfbuzz.github.io/shaping-opentype-features.html
- https://harfbuzz.github.io/integration-uniscribe.html

HarfBuzzSharp exposes `Feature.Parse`, `Font.Shape`, `Buffer.GlyphInfos`, and `Buffer.GlyphPositions`, making it useful as a test oracle for feature effects.

### SkiaSharp / Avalonia

SkiaSharp.HarfBuzz can shape and render text for comparison images, but its rasterization differs from GDI/Uniscribe. Avalonia has a `FontFeatureCollection` and accepts HarfBuzz-like feature syntax such as `+smcp` and `-liga`.

Useful references:

- https://learn.microsoft.com/en-us/dotnet/api/skiasharp.harfbuzz.skshaper
- https://learn.microsoft.com/en-us/dotnet/api/skiasharp.sktextblob
- https://docs.avaloniaui.net/docs/styling/typography
- https://api-docs.avaloniaui.net/docs/T_Avalonia_Media_FontFeatureCollection

## Phase 2 Research: Remove Graphite, Keep WinForms

- Keep the renderer-neutral `tag=value` OpenType feature model.
- Remove Graphite UI labels/toggles only after compatibility and migration policy is defined.
- Preserve old project data even when Graphite feature values no longer apply.
- Add warnings or conversion guidance for Graphite-only feature settings.
- Retain visual baselines for no-feature and OpenType-feature rendering to detect unrelated regressions.

## Phase 3 Research: Avalonia Alongside WinForms

- Map FieldWorks feature strings to Avalonia `FontFeatureCollection` / `TextRunProperties.FontFeatures`.
- Use the legacy Views golden baseline set as comparison evidence, not as an exact pixel mandate.
- Classify comparisons as exact same-renderer, tolerant cross-renderer, shaping-data, and semantic layout checks.
- Prefer migration of text model and feature metadata before replacing editing/selection behaviors.

## Phase 4 Research: Retire WinForms

- Keep OpenSpec requirements and visual scenarios as renderer-agnostic acceptance tests.
- Remove legacy Graphite and Uniscribe adapters only after Avalonia paths pass feature, bidi, selection, and line-layout acceptance checks.
- Retire WinForms UI controls after equivalent Avalonia controls use the same feature provider and parser behavior.

## Clarifications To Resolve

- Confirm test font licensing and whether binary font assets may be committed. The current Phase 1 visual test intentionally uses common installed Windows fonts with feature probes and falls back to inconclusive if none produce a visible feature delta; a deterministic redistributable OFL font asset is still the preferred follow-up.
- Confirm whether friendly labels for OpenType features should be limited to common tags or come from font name tables where available.
- Confirm whether Help changes are part of Phase 1 deliverables or tracked in a linked documentation task.

## Phase 1 Implementation Notes

- Test font assets: `Src/views/Test/TestData/Fonts/CharisSIL-5.000` commits the JIRA-specified Charis SIL 5.000 regular TTF with its OFL license and README. Native `TestViews` copies this fixture beside `TestViews.exe`, loads it as a private GDI font, and renders small text runs with feature strings off/on through the production Uniscribe `FindBreakPoint` and `ILgSegment::DrawText` path. HarfBuzz/Skia comparison tests still use installed-font probes because they are test-only cross-renderer comparison coverage.
- JIRA font inventory: the current machine has `Charis SIL`, `Andika`, `Doulos SIL`, `Gentium Plus`, and `Quivira` installed; `Abyssinica SIL` was not installed. The repository also commits `DistFiles/Fonts/Raw/Quivira.otf` under raw font assets. Installer targets download/stage `Andika-6.101.zip`, `CharisSIL-6.101.zip`, `DoulosSIL-6.101.zip`, and `GentiumPlus-6.101.zip`, and WiX includes those plus Quivira. The exact older JIRA-linked `AbyssinicaSIL-2.201.zip` archive is not included in this workspace.
- Manual UIA2 evidence: WinForms MCP verified `Writing System Properties > Font` with `Font Options`, `Charis SIL` selected, `Enable Graphite` unchecked/disabled for that OpenType path, and the `Font Features` menu listing OpenType feature entries such as `Access All Alternates`, `Small Capitals From Capitals`, `Standard Ligatures`, `Small Capitals`, and `cv*` variants. Screenshots are under `evidence/manual-winforms/`.
- Cache identity: managed render-engine cache keys include the normalized feature string, and native `ShapeRunCache` entries include `LgCharRenderProps.szFontVar`. The render verification tests now cover writing-system default features, style-level features, and multi-writing-system text to guard stale output reuse.
- Native verification: `TestViews` includes Charis SIL fixture tests for `liga` metric changes, `smcp` rendered pixel changes, and switching feature state off/on without stale rendered output reuse. The tests exercise the updated production code by passing `szFontVar` feature strings into `FindBreakPoint`, drawing the resulting segment into a bitmap, and comparing rendered pixels.
- Test-only comparison: HarfBuzzSharp and SkiaSharp remain isolated to `RenderComparisonTests`. HarfBuzzSharp is used only as a test comparison path for shaping data; production rendering remains Uniscribe/Graphite.
- Export audit: CSS already emits `font-feature-settings` and is covered by `GenerateCssForConfiguration_CharStyleFontFeaturesWorks`. Notebook export preserves writing-system `DefaultFontFeatures`. `WordStylesGenerator` did not show a feature-string mapping and should be tracked separately if Word export parity is required.
- Help/docs: no existing FieldWorks help source for Font Options was found in this workspace. Phase 1 adds `Docs/opentype-font-features.md` to document the UI, storage model, temporary Graphite role, and export status.
