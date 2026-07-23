## Context

FieldWorks currently stores font feature strings generically (`tag=value`) but exposes and applies them mostly through Graphite-specific paths. Writing-system default features and style features flow through managed dialogs, `FontInfo.m_features`, `FwTextPropType.ktptFontVariations`, `VwPropertyStore`, and CSS export, but the non-Graphite Views renderer does not apply OpenType features.

LT-22324 Phase 1 must be implemented after `001-render-speedup` is merged. That branch adds render/layout dirty-state checks, warm rendering paths, and bitmap baseline infrastructure; this change must assume those optimizations exist and must treat font-feature changes as layout-changing.

The longer product phases are: add OpenType features now, remove Graphite later while retaining WinForms, add Avalonia alongside WinForms, and eventually retire WinForms. This design makes Phase 1 useful to the later phases without making HarfBuzzSharp or Avalonia part of production rendering yet.

## Goals / Non-Goals

**Goals:**

- Support OpenType font features in current WinForms/Views data entry and preview surfaces.
- Split Font Features from the `Enable Graphite` UI concept.
- Prefer OpenType for dual-technology fonts while preserving explicit access to Graphite features.
- Preserve Graphite behavior and existing Graphite feature support during Phase 1.
- Keep persisted feature strings renderer-neutral and compatible with future Avalonia/HarfBuzz-style consumption.
- Accept any syntactically valid OpenType tag and reject malformed tags safely with trace logging.
- Add trace logging for discovery, validation, native shaping, and fallback decisions.
- Keep style/default font-feature loading on the existing inheritance path (the interim `StyleInfo` compatibility adapter was later removed under LT-22351; see Decision 11).
- Fix truncation and malformed-input robustness gaps in legacy feature-string handling.
- Add tests for UI control behavior and visual rendering differences caused by feature toggles.
- Add test-only HarfBuzzSharp + SkiaSharp comparison tooling for future visual-fidelity confidence.

**Non-Goals:**

- Removing Graphite or changing Graphite project data in Phase 1.
- Replacing Views.cpp, WinForms, selection, editing, line breaking, or hit testing.
- Introducing HarfBuzzSharp, SkiaSharp, or Avalonia into production rendering.
- Guaranteeing pixel identity between GDI/Uniscribe and Skia/HarfBuzz output.

## Clarified Scope From The In-Depth Review

- Local staged/unstaged churn seen during review is not part of the intended scope. This design assumes the final implementation branch resolves that churn before validation.
- OpenType, not Graphite, is the default feature system for dual-technology fonts in Phase 1. Graphite remains available by explicit user choice.
- Tag acceptance is syntactic, not registry-based. Any valid four-character printable ASCII OpenType tag is accepted; filtering applies to UI exposure and specific export boundaries.
- Logging and robustness are Phase 1 requirements, not deferred cleanup work.
- Existing inheritance/data-flow paths remain authoritative unless a change is explicitly required by this proposal.

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

### 4. Font Features UI uses providers, with OpenType preferred by default

**Decision:** Refactor `FontFeaturesButton` around a feature provider concept: Graphite provider uses existing `IRenderingFeatures`; OpenType provider uses OpenType font/script/language/feature tag discovery; the button is enabled when the selected font has configurable features. When a font exposes both Graphite and OpenType feature sets, the default provider SHALL be OpenType and the UI SHALL expose a clear explicit toggle to switch providers.

**Rationale:** The control should depend on “has configurable font features,” not “is Graphite.” Making OpenType the default matches the Phase 1 product goal and avoids hiding the new behavior behind Graphite-first heuristics.

**Alternatives considered:** Continue preferring Graphite implicitly or add OpenType conditions only to `DefaultFontsControl`. Rejected because that would preserve confusing defaults and leave the shared button and style/font dialogs with duplicated logic.

### 5. HarfBuzzSharp + SkiaSharp are test-only comparison tools

**Decision:** Add HarfBuzzSharp and SkiaSharp only to test/comparison projects, not production projects. Use them to shape/render known feature scenarios and compare against legacy Views captures with tolerances.

**Rationale:** This starts migration evidence now and aligns with Avalonia/HarfBuzz direction without destabilizing production rendering.

**Alternatives considered:** Make HarfBuzzSharp the shared runtime renderer now. Rejected because current Views owns layout, drawing, selection, and editing behavior.

### 6. Visual baselines are migration assets

**Decision:** Use the post-`001-render-speedup` render snapshot framework as the golden legacy evidence set for feature-on/feature-off scenarios.

**Rationale:** Golden WinForms/Views captures help Phase 1 verification and later Avalonia comparison. Exact pixels are appropriate for same-renderer regressions; tolerant or semantic comparisons are appropriate across GDI/Uniscribe and Skia/HarfBuzz.

### 7. Word DOCX export maps only documented Word typography features

**Decision:** Map FieldWorks `tag=value` font feature strings to Office 2010 WordprocessingML `w14` typography elements only where Microsoft documents a Word representation: ligatures, number form, number spacing, contextual alternatives, and stylistic sets.

**Rationale:** CSS can preserve arbitrary OpenType feature tags, but WordprocessingML does not provide a general `font-feature-settings` equivalent. A best-effort documented subset avoids producing invalid DOCX while preserving the features Word can actually display and round-trip.

**Alternatives considered:** Store arbitrary tags in custom XML or undocumented extension markup. Rejected because Word would not apply those settings to text rendering and the export would give users a false parity signal.

### 8. OpenType feature discovery filters user-configurable features only

**Decision:** OpenType feature discovery SHALL filter out required shaping features and other non-user-configurable engine-controlled features before populating the Font Features UI.

**Rationale:** HarfBuzz and CSS guidance distinguish required/default shaping behavior from optional user-facing feature selection. Presenting all GSUB/GPOS tags as toggles produces confusing and potentially unsafe UI.

**Alternatives considered:** Expose every raw GSUB/GPOS feature found in the font. Rejected because that treats engine-required features as if they were safe end-user choices.

### 9. Tag validation is syntactic and liberal; output boundaries stay safe

**Decision:** FieldWorks SHALL accept any OpenType tag that is exactly four printable ASCII characters (`U+20`-`U+7E`), whether registered or custom. Malformed tags SHALL be ignored with trace logging. Output boundaries such as CSS SHALL escape or otherwise safely serialize valid tags instead of rejecting them.

**Rationale:** The OpenType/CSS contract is syntactic, not registry-based. Narrowing acceptance to a product-specific allowlist would break legitimate custom tags and weaken renderer-neutral storage.

**Alternatives considered:** Restrict tags to registered tags only, or restrict accepted characters more narrowly than the published syntax. Rejected because those approaches are incompatible with valid custom/private tags and would create artificial persistence/export divergence.

### 10. Trace logging is a Phase 1 requirement

**Decision:** Phase 1 SHALL add trace logging through the existing FieldWorks diagnostics infrastructure for malformed feature input, filtered feature discovery, provider selection/toggle decisions, native shaping failures, and fallback reasons.

**Rationale:** The feature must degrade gracefully for bad fonts and bad feature strings, but silent fallback makes regressions and field failures hard to diagnose.

**Alternatives considered:** Defer diagnostics to a later cleanup pass or rely on modal assertions. Rejected because the review explicitly requires graceful continuation plus actionable logs.

### 11. Existing inheritance paths remain authoritative

**Decision:** `FontInfo.m_features`, `FwTextPropType.ktptFontVariations`, and style rule round-tripping remain the authoritative inheritance/data-flow path for default and explicit font features. `StyleInfo` originally retained a minimal compatibility adapter that read default `ktptFontVariations` from `IStStyle.Rules` because focused validation showed that removing it lost persisted default font features in the then-current build graph.

**Rationale:** At the time of this decision, the active FieldWorks build/test path still required the `StyleInfo` adapter to reload persisted defaults even though the local LCM source contained `BaseStyleInfo.ProcessStyleRules` support for `ktptFontVariations`. The adapter was a compatibility boundary, not a second policy path.

**Alternatives considered:** Remove the `StyleInfo` adapter immediately. Rejected for this change because `SaveToDB_DefaultFontFeatures_RoundTripsThroughRules` failed after removal. Broader LCM dependency alignment could retire the adapter later with the same round-trip tests as the gate.

**Status update (LT-22351):** The gating condition has been satisfied. liblcm's `BaseStyleInfo.ProcessStyleRules` now loads default `ktptFontVariations` (sillsdev/liblcm#388), `SaveToDB_DefaultFontFeatures_RoundTripsThroughRules` passes through the authoritative `BaseStyleInfo` path, and the `StyleInfo.LoadDefaultFontFeatures` adapter was removed under LT-22351.

### 12. Overlong and malformed feature strings fail safe

**Decision:** When existing storage/truncation logic encounters overlong or malformed feature strings, the code SHALL either make forward progress or abandon safely, and SHALL log the event. It SHALL NOT loop indefinitely.

**Rationale:** Phase 1 must not crash or hang on malformed feature data from fonts, styles, or legacy persisted values.

**Alternatives considered:** Preserve comma-based truncation behavior without a no-progress guard. Rejected because the review identified a concrete hang risk.

### 13. State-of-the-art native fallback behavior is in scope

**Decision:** Native OpenType shaping SHALL treat `E_OUTOFMEMORY` as retryable, preserve authoritative script tags from `ScriptItemizeOpenType`, and favor an authoritative/generated language-tag mapping strategy over handwritten tables where OS APIs are insufficient.

**Rationale:** Current platform guidance treats OpenType shaping as an itemize/shape/place pipeline with retryable buffer sizing and explicit script/language inputs. That behavior is part of robust Phase 1 support, not a future renderer rewrite.

**Alternatives considered:** Fall back immediately on retryable errors or maintain ad hoc locale-to-tag heuristics indefinitely. Rejected because both approaches weaken correctness and diagnosability.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| OpenType APIs produce different metrics or line breaks | Add feature-on/off render baselines and native metric/selection tests. |
| Feature state is omitted from post-speedup caches | Add tasks and tests requiring feature strings in cache/dirty identity. |
| UI exposes required shaping features as toggles | Filter OpenType discovery to user-configurable optional features and provide fallback labels. |
| OpenType feature labels are incomplete or unlocalized | Use resource-backed labels for common tags and fall back to the four-character tag. |
| Test fonts cannot be redistributed | Confirm SIL Open Font License or another redistributable license before adding binaries. |
| HarfBuzz/Skia visual output differs from GDI/Uniscribe | Compare shaping data first; use tolerant image comparisons for cross-renderer evidence. |
| Word DOCX cannot represent every OpenType feature tag | Map only documented `w14` typography elements and document unsupported tags such as character variants and private features. |
| OpenType default preference conflicts with legacy Graphite-first assumptions | Make provider choice explicit in shared UI and cover dual-technology fonts with tests. |
| Accepting all valid tags can create unsafe raw CSS strings | Keep parser/storage liberal, but escape valid tags at CSS output boundaries and test serialization. |
| Silent fallback hides malformed-input and shaping bugs | Add trace switches and testable diagnostics points for filtering, validation, retry, and fallback. |
| Duplicate inheritance loaders drift from persisted style behavior | Keep the `StyleInfo` loader as a minimal compatibility adapter, document why it remains, and gate future removal with reopen/save round-trip tests. |
| Overlong strings without comma boundaries can hang truncation loops | Add no-progress guards and fail-safe truncation behavior with targeted tests. |

## Migration Plan

1. Wait until `001-render-speedup` is merged into the target branch.
2. Add parser/normalizer validation, malformed-input logging, and fail-safe truncation coverage before widening feature discovery.
3. Add provider abstractions, OpenType-preferred dual-tech toggle behavior, and shared UI tests.
4. Add filtered OpenType feature discovery for the UI while preserving Graphite provider behavior.
5. Add native OpenType shaping/placing support, retryable-error handling, script/language trace points, and native tests.
6. Attempt to reduce inheritance-path duplication, retain only required compatibility adapters, and verify style/default-feature round-tripping through the authoritative path.
7. Add render snapshot scenarios using the merged render baseline infrastructure.
8. Add test-only HarfBuzzSharp + SkiaSharp comparison tests in FieldWorks test projects.
9. Update help/localized UI text and review-driven docs.
10. Add Word DOCX export mapping for the documented WordprocessingML subset, CSS-safe serialization work, and tests that inspect generated Open XML.

Rollback strategy: disable the OpenType provider and native OpenType shaping path behind a feature flag or fallback path if regressions are found; Graphite and old Uniscribe behavior remain available.

## Implementation Update (2026-05-12)

- `FontFeatureSettings` now accepts any valid four-character printable ASCII tag, ignores malformed entries, and traces ignored input through `FwUtils_FontFeatureSettings`.
- `FontFeaturesButton` now defaults to OpenType provider selection, filters required shaping features from OpenType discovery, and traces provider/filter decisions through `FontFeatures.OpenType`.
- `VwPropertyStore` now stores full `ktptFontVariations`, fixes `get_FontVariations`, and copies only render-safe strings into `LgCharRenderProps.szFontVar`, with fail-safe behavior for overlong strings without comma boundaries.
- `UniscribeSegment` now retries `ScriptShapeOpenType` and `ScriptPlaceOpenType` after `E_OUTOFMEMORY`, traces retries/fallbacks, and keeps classic Uniscribe fallback intact.
- CSS export escapes valid tags inside `font-feature-settings`; DOCX export remains on the documented Word `w14` subset and ignores unsupported valid or malformed entries safely.
- Removing the `StyleInfo` default-font-feature loader was attempted and failed the focused round-trip test, so the loader remains as a documented compatibility adapter.

## Open Questions

1. What exact wording and placement should the dual-technology provider toggle use so users understand when they are viewing OpenType versus Graphite features?
2. Can the implementation vendor or reuse an authoritative script/language-tag mapping source, or must it rely only on OS APIs already present in the runtime environment?
3. Should new trace switches be split by area (UI/provider/native/export) or grouped under one OpenType font-features category?
4. Where should the test-only HarfBuzzSharp + SkiaSharp comparison project live after `001-render-speedup`: under RenderVerification, RootSiteTests, or a new dedicated test project?
