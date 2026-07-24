# Native Views Feature Migration Matrix

This artifact answers the broader LT-22324 planning question: what custom native Views functionality exists today, what modern library support exists for each feature, and how each piece should be staged toward the Graphite-removal and Avalonia migration path.

Assumptions:

- "Views.cpp" means the native Views engine under `Src/views/`, not only one translation unit.
- Phase 1 remains OpenType font features in current WinForms/Views after `001-render-speedup` is merged.
- Phase 2 removes Graphite while retaining WinForms.
- Phase 3 adds Avalonia alongside WinForms.
- Phase 4 retires WinForms and the remaining native Views surface only after parity evidence exists.

## Library Fit Summary

No current standard library replaces all of Views. The closest full-stack candidates replace slices:

- DirectWrite is the best Windows-native replacement candidate for shaping, typography, paragraph layout, drawing callbacks, hit testing, and text metrics. It is strong if FieldWorks stays WinForms/Windows for a long time, but it is not the cross-platform Avalonia end state.
- HarfBuzz is the best shaping core, especially for OpenType and future Graphite removal, but it intentionally does not do bidi, line breaking, rich layout, selection, editing, drawing, or font fallback.
- ICU is the best standard library for bidi and text boundaries that FieldWorks should not keep reimplementing by hand.
- Skia/SkParagraph is the most relevant cross-platform paragraph engine because Avalonia already uses Skia in common configurations and SkParagraph exposes layout, painting, line metrics, hit testing, word boundaries, placeholders, and font fallback hooks.
- Avalonia `TextLayout`, `TextShaper`, `GlyphRun`, `TextElement.FontFeatures`, and automation/input infrastructure are the likely product end-game wrappers, but stock controls will not replace FieldWorks-specific view construction, lazy data loading, interlinear layout, selection semantics, undo, and model notifications by themselves.
- Pango is a strong Linux/Cairo precedent for paragraph layout, shaping, cursor positions, and hit testing, but it is less aligned with the existing Windows and Avalonia direction than DirectWrite or SkParagraph.

Key evidence:

- HarfBuzz shapes Unicode runs into glyphs and positions: https://harfbuzz.github.io/what-is-harfbuzz.html
- HarfBuzz explicitly does not do bidi, line breaking, justification, rich runs, or full layout: https://harfbuzz.github.io/what-harfbuzz-doesnt-do.html
- ICU `BreakIterator` supports character, word, line, and sentence boundaries: https://unicode-org.github.io/icu/userguide/boundaryanalysis/
- ICU `ubidi` implements the Unicode bidi algorithm: https://unicode-org.github.io/icu/userguide/transforms/bidi.html
- DirectWrite supports Unicode layout, advanced OpenType typography, measuring, drawing, and hit testing: https://learn.microsoft.com/en-us/windows/win32/directwrite/direct-write-portal
- DirectWrite `IDWriteTextLayout` exposes formatted text layout, drawing, line metrics, typography, and hit testing: https://learn.microsoft.com/en-us/windows/win32/api/dwrite/nn-dwrite-idwritetextlayout
- DirectWrite `IDWriteTextAnalyzer` exposes bidi, script, line break, glyph, and placement analysis: https://learn.microsoft.com/en-us/windows/win32/api/dwrite/nn-dwrite-idwritetextanalyzer
- DirectWrite `IDWriteTypography` exposes OpenType font features: https://learn.microsoft.com/en-us/windows/win32/api/dwrite/nn-dwrite-idwritetypography
- Avalonia typography exposes inherited font properties and `TextElement.FontFeatures`: https://docs.avaloniaui.net/docs/styling/typography
- Avalonia `TextLayout` exposes multiline layout, drawing, line data, and hit testing: https://api-docs.avaloniaui.net/docs/T_Avalonia_Media_TextFormatting_TextLayout
- Avalonia `TextShaper` shapes text through `ShapeText`: https://api-docs.avaloniaui.net/docs/T_Avalonia_Media_TextFormatting_TextShaper
- Avalonia `GlyphRun` exposes glyph run metrics, bidi level, caret hits, geometry, and intersections: https://api-docs.avaloniaui.net/docs/T_Avalonia_Media_GlyphRun
- SkParagraph `Paragraph`, `ParagraphBuilder`, and `FontCollection` expose paragraph layout, painting, line metrics, rectangles, hit testing, word boundaries, placeholders, font fallback, and paragraph caches: https://raw.githubusercontent.com/google/skia/main/modules/skparagraph/include/Paragraph.h, https://raw.githubusercontent.com/google/skia/main/modules/skparagraph/include/ParagraphBuilder.h, https://raw.githubusercontent.com/google/skia/main/modules/skparagraph/include/FontCollection.h
- Pango `PangoLayout` formats paragraphs with line breaking, justification, alignment, cursor positions, hit testing, and logical/physical conversions: https://docs.gtk.org/Pango/class.Layout.html
- Windows TSF remains the platform service for advanced multilingual text input: https://learn.microsoft.com/en-us/windows/win32/tsf/text-services-framework
- Windows UI Automation is the platform accessibility model for custom controls: https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-uiautomationoverview

## Feature-by-feature Matrix

### 1. COM/root-site public contract

What it is and where it is used:

- Native Views exposes document rendering/editing through COM-style interfaces such as root boxes, view constructors, view environments, graphics, selections, and render engines.
- Key areas: `Src/views/*.idl`, `Src/views/VwRootBox.cpp`, `Src/views/VwEnv.cpp`, `Src/views/VwSelection.cpp`, `Src/Common/SimpleRootSite/`, and WinForms root-site controls.
- This contract lets managed FieldWorks code construct views and host native layout while registration-free COM keeps deployment local.

Standard equivalents:

- No single standard equivalent. Avalonia controls/visuals and `TextLayout` cover UI and text layout pieces, but not the FieldWorks data/view-constructor contract.
- DirectWrite, SkParagraph, and Pango are text engines, not application document contracts.

Best end game:

1. Create a managed renderer-neutral document/view contract that can be backed by native Views, DirectWrite experiments, or Avalonia.
2. Keep a compatibility adapter for existing `IVwViewConstructor` callers until each canonical view has a managed/Avalonia equivalent.

Migration staging:

- Phase 1: keep COM unchanged except additive feature plumbing.
- Phase 2: isolate render-engine and feature contracts from Graphite/Uniscribe specifics.
- Phase 3: add managed/Avalonia adapters for non-editing preview surfaces first.
- Phase 4: remove native COM contracts after editing, printing, accessibility, and canonical data views have parity.

### 2. Root box lifecycle, reconstruction, and dirty layout

What it is and where it is used:

- `VwRootBox.cpp` owns root objects, view construction, full/partial reconstruction, relayout, invalidation, drawing, selection installation, printing entry points, and data-change response.
- `001-render-speedup` makes this even more important because cache/dirty identity must include feature strings.

Standard equivalents:

- Avalonia layout/visual invalidation can replace UI-tree invalidation, but not FieldWorks data reconstruction by itself.
- DirectWrite `IDWriteTextLayout` and SkParagraph `Paragraph` can be marked dirty/rebuilt for paragraph text, but they do not manage root object lifecycles.

Best end game:

1. Managed document-root controller that owns data subscriptions and produces renderer-neutral blocks/runs.
2. Avalonia custom control using normal layout invalidation for visible surfaces.

Migration staging:

- Phase 1: treat font-feature changes as layout dirty and cache identity changes.
- Phase 3: introduce a managed root controller for read-only or preview surfaces.
- Phase 4: move editing surfaces after selection and undo have parity.

### 3. View-construction DSL (`VwEnv` and `IVwViewConstructor`)

What it is and where it is used:

- `VwEnv.cpp` is an imperative DSL used by managed/native view constructors: `OpenParagraph`, `AddString`, `AddObjProp`, `AddObjVecItems`, `AddLazyVecItems`, `OpenTable`, `OpenTableCell`, and property setters build a custom box tree.
- It is the bridge between FieldWorks model objects and the visual tree.

Standard equivalents:

- Avalonia data templates, bindings, panels, and controls are the closest UI framework equivalents.
- HTML/CSS has a similar declarative layout model, but it does not integrate with FieldWorks editing or object notifications.
- No text library replaces this; text libraries consume already-built runs/paragraphs.

Best end game:

1. Convert canonical view constructors into managed view models/templates that emit paragraphs, inline objects, tables, and adornments.
2. Keep `VwEnv` as a compatibility adapter while each view moves.

Migration staging:

- Phase 1: do not change the DSL beyond feature propagation.
- Phase 3: prototype one read-only canonical surface in Avalonia using renderer-neutral view data.
- Phase 4: replace editing constructors only after command/selection tests pass.

### 4. Box tree and non-text layout

What it is and where it is used:

- Views builds a custom hierarchy of boxes for paragraphs, piles, divisions, inner piles, pictures, tables, table cells, borders, margins, and embedded objects.
- Key areas include `Src/views/VwBox.cpp`, `Src/views/VwTextBoxes.cpp`, `Src/views/VwTableBox.cpp`, and `Src/views/VwEnv.cpp`.

Standard equivalents:

- Avalonia panels, `Grid`, custom controls, and layout passes can replace much of the non-text box layout.
- SkParagraph placeholders and DirectWrite inline objects cover embedded inline spaces inside text.
- Pango and SkParagraph are paragraph engines, not general document layout systems.

Best end game:

1. Use Avalonia for non-text layout and a paragraph engine for text layout.
2. Preserve a small FieldWorks document-layout layer for interlinear, linguistic, and embedded-object semantics that are not stock UI patterns.

Migration staging:

- Phase 3: migrate read-only non-text layout blocks first.
- Phase 4: migrate tables/interlinear/embedded editing after hit testing and accessibility are validated.

### 5. Paragraph building, wrapping, and line layout

What it is and where it is used:

- `VwTextBoxes.cpp` contains `ParaBuilder` and paragraph/string box behavior for line breaking, measuring, fitting, backtracking, justification, drop caps, bullets/numbers, exact line height, widows/orphans, and physical box ordering.
- It is one of the densest custom areas and underlies rendering, selection, printing, search highlights, and overlays.

Standard equivalents:

- DirectWrite `IDWriteTextLayout` provides formatted text layout, line metrics, drawing, typography, and hit testing.
- SkParagraph `Paragraph` provides `layout`, `paint`, `getLineMetrics`, `getRectsForRange`, `getGlyphPositionAtCoordinate`, and word boundaries.
- Pango `PangoLayout` provides paragraph formatting, wrapping, justification, alignment, cursor positions, and logical/physical conversions.
- ICU `BreakIterator` provides standard character/word/line break points but not full line layout.

Best end game:

1. Use Avalonia/SkParagraph for cross-platform paragraph layout where possible.
2. Keep DirectWrite as a Windows-native spike/fallback candidate if Avalonia text layout cannot satisfy exotic-script or hit-test requirements.

Migration staging:

- Phase 1: add feature-on/off visual baselines for paragraphs before changing layout engines.
- Phase 2: remove Graphite only when OpenType/HarfBuzz shaping and line metrics are covered.
- Phase 3: compare legacy Views, Avalonia, and SkParagraph on canonical paragraph cases.
- Phase 4: replace `ParaBuilder` only after bidi, selection, printing, and interlinear cases are covered.

### 6. Text shaping and glyph placement

What it is and where it is used:

- `Src/views/lib/GraphiteEngine.cpp` and `GraphiteSegment` shape Graphite fonts and expose configurable Graphite features.
- `Src/views/lib/UniscribeEngine.cpp` and `UniscribeSegment.cpp` use classic Uniscribe `ScriptItemize`, `ScriptShape`, and `ScriptPlace` for non-Graphite text.
- Phase 1 must add OpenType feature application without replacing the Views layout model.

Standard equivalents:

- HarfBuzz is the standard shaping core for OpenType and many scripts, but it is shaping only.
- DirectWrite `IDWriteTextAnalyzer` exposes script, bidi, line-break analysis, glyph mapping, and placement.
- Avalonia `TextShaper` shapes text and `GlyphRun` represents shaped glyph runs.
- SkParagraph performs shaping through its paragraph pipeline and exposes glyph runs through visitors.

Best end game:

1. Phase 1 production: Uniscribe OpenType APIs because they are the smallest safe change to current Views.
2. Long term: HarfBuzz through Avalonia/Skia or another managed abstraction, with ICU for bidi/boundaries.

Migration staging:

- Phase 1: branch Uniscribe to OpenType APIs only when features are present; keep old behavior for empty features and keep Graphite intact.
- Phase 2: remove Graphite after font compatibility and project-data migration policy are ready.
- Phase 3: compare HarfBuzz/Avalonia shaping data against legacy baselines.

### 7. Font-feature discovery, storage, and application

What it is and where it is used:

- FieldWorks already stores feature strings as `tag=value` in writing systems/styles and passes them through `ktptFontVariations` and `LgCharRenderProps`.
- `FontFeaturesButton.cs` and `DefaultFontsControl.cs` currently gate feature UI mostly on Graphite.
- `GraphiteEngine` supports feature discovery through `IRenderingFeatures`; `UniscribeEngine` currently ignores feature data.

Standard equivalents:

- DirectWrite `IDWriteTypography::AddFontFeature` applies OpenType features to text ranges.
- Avalonia exposes `TextElement.FontFeatures` and `FontFeatureCollection` using HarfBuzz-like syntax.
- HarfBuzz supports OpenType features during shaping.
- CSS exposes `font-feature-settings`, which FieldWorks already emits for exports.

Best end game:

1. Renderer-neutral feature model in FieldWorks (`tag=value`) with renderer-specific adapters.
2. Avalonia/HarfBuzz syntax adapter for future UI, tests, and rendering.

Migration staging:

- Phase 1: shared parser/normalizer, provider-based UI, Uniscribe OpenType adapter, visual tests.
- Phase 2: preserve old Graphite values but remove Graphite-only UI behavior when policy is ready.
- Phase 3: reuse the same feature provider in Avalonia controls.

### 8. Bidi, script itemization, and number substitution

What it is and where it is used:

- Views handles paragraph direction, weak-direction adjustment, physical box ordering, script runs, and Uniscribe itemization as part of paragraph layout and selection.
- Key areas: `VwTextBoxes.cpp`, `UniscribeSegment.cpp`, and selection movement in `VwSelection.cpp`.

Standard equivalents:

- DirectWrite `IDWriteTextAnalyzer` has `AnalyzeBidi`, `AnalyzeScript`, `AnalyzeLineBreakpoints`, and `AnalyzeNumberSubstitution`.
- ICU `ubidi` implements the Unicode bidi algorithm.
- Pango and SkParagraph include bidi-aware paragraph layout.

Best end game:

1. Let the paragraph/text engine own bidi and script itemization when possible.
2. Use ICU directly for any FieldWorks semantic operations that need text boundaries independent of rendering.

Migration staging:

- Phase 1: add at least one bidi or multi-writing-system feature baseline to prevent OpenType regressions.
- Phase 3: compare bidi line layout and caret movement between Views and Avalonia/SkParagraph.
- Phase 4: replace custom bidi movement only after keyboard/selection parity is proven.

### 9. Grapheme, word, and line boundaries

What it is and where it is used:

- `VwSelection.cpp` and `VwTextBoxes.cpp` contain custom word expansion, arrow movement, editable substring selection, search ranges, line boundary adjustment, and boundary-sensitive deletion logic.
- `VwParagraphBox::MakeSourceNfd` and typing logic also reflect normalization-sensitive text behavior.

Standard equivalents:

- ICU `BreakIterator` supports grapheme/character, word, line, and sentence boundaries, including dictionary support for languages without spaces.
- SkParagraph exposes `getWordBoundary` and glyph/cluster information.
- Pango exposes cursor movement and logical attributes.
- DirectWrite supports hit testing and line metrics, but ICU is the better standalone boundary service.

Best end game:

1. Use ICU for renderer-independent boundaries and word movement rules.
2. Use the active paragraph engine for visual caret and line-position mapping.

Migration staging:

- Phase 2: introduce boundary-service tests around current behavior.
- Phase 3: use the same tests for Avalonia/SkParagraph caret and word movement.
- Phase 4: delete custom boundary logic only after language-specific cases pass.

### 10. Style/property resolution and writing-system defaults

What it is and where it is used:

- `VwPropertyStore.cpp` resolves text props, writing-system defaults, style inheritance, font variations, effects, and concrete character/paragraph properties.
- It feeds `LgCharRenderProps` consumed by render engines and paragraph layout.

Standard equivalents:

- Avalonia `TextElement` inherited properties and `TextRunProperties` can represent font family, size, style, weight, stretch, decorations, foreground, line height, and font features.
- CSS has a cascade model, but FieldWorks style/writing-system semantics are domain-specific.
- DirectWrite and SkParagraph accept formatted ranges after style resolution; they do not replace the resolver.

Best end game:

1. Move style resolution to managed renderer-neutral code that emits text runs and paragraph properties.
2. Keep renderer adapters thin: Views props, DirectWrite ranges, SkParagraph `TextStyle`, and Avalonia `TextRunProperties`.

Migration staging:

- Phase 1: ensure `ktptFontVariations` participates in render/layout cache identity.
- Phase 3: create a managed run model for one Avalonia preview/control.
- Phase 4: retire native property-store behavior after all writing-system/style cases pass.

### 11. Text source model (`ITsString`, runs, embedded objects)

What it is and where it is used:

- Views consumes FieldWorks `ITsString`/`TsTextProps` runs, object replacement behavior, writing-system runs, and string properties to build boxes and render text.
- Key areas: `VwEnv.cpp`, `VwTextBoxes.cpp`, `VwPropertyStore.cpp`, and selection string extraction in `VwSelection.cpp`.

Standard equivalents:

- Avalonia `ITextSource` and `TextRunProperties` are close structural equivalents for formatted text runs.
- DirectWrite `IDWriteTextLayout` consumes strings plus per-range formatting.
- SkParagraph `ParagraphBuilder` accepts UTF-8/UTF-16 text, pushed `TextStyle`, and placeholders.

Best end game:

1. Create a managed adapter from `ITsString` and object placeholders to renderer-neutral text runs.
2. Keep `ITsString` as the domain text model until a separate product decision replaces it.

Migration staging:

- Phase 1: pass feature strings through existing run props.
- Phase 3: build `ITsString` to Avalonia/SkParagraph run adapters for previews.
- Phase 4: route editing through the same adapter after round-trip tests are stable.

### 12. Selection, caret geometry, and hit testing

What it is and where it is used:

- `VwSelection.cpp` and `VwTextBoxes.cpp` implement insertion points, ranges, bidirectional caret behavior, physical/logical arrow movement, point-to-offset mapping, range rectangles, selection drawing, picture selections, and editable-position discovery.
- These behaviors are central to data-entry quality.

Standard equivalents:

- DirectWrite `IDWriteTextLayout` exposes `HitTestPoint`, `HitTestTextPosition`, and `HitTestTextRange`.
- Avalonia `TextLayout` exposes `HitTestPoint`, `HitTestTextPosition`, and `HitTestTextRange`; `GlyphRun` exposes caret-hit utilities.
- SkParagraph exposes `getGlyphPositionAtCoordinate`, `getRectsForRange`, and glyph cluster APIs.
- Pango exposes `index_to_pos`, `xy_to_index`, cursor positions, and visual cursor movement.

Best end game:

1. Use paragraph-engine hit testing for glyph/line geometry.
2. Keep a FieldWorks selection model above it for object paths, editable/non-editable regions, interlinear views, and undo grouping.

Migration staging:

- Phase 1: add selection-related OpenType metric/placement tests so features do not stale-cache hit testing.
- Phase 3: compare selection rectangles and caret locations in read-only/limited-edit Avalonia surfaces.
- Phase 4: migrate full editing only after keyboard, bidi, object, and interlinear selection tests pass.

### 13. Editing commands, typing, normalization, and undo

What it is and where it is used:

- `VwSelection.cpp` handles typing, backspace/delete, control-backspace/delete, replacing ranges, property cleanup, editable substring callbacks, undo tasks, and display updates.
- `OnTypingMethod` contains complex handling for combining marks, protected methods, integer fields, and selection updates.

Standard equivalents:

- Avalonia and WinUI text controls provide standard editing behavior, but FieldWorks editable views are not plain text boxes.
- Windows TSF provides platform multilingual input services.
- ICU can help with boundaries/normalization, but not FieldWorks data commits or undo semantics.

Best end game:

1. Managed FieldWorks editing command layer that owns domain updates, undo, and constraints.
2. Platform text-input integration through Avalonia/WinUI/TSF equivalents rather than native Views internals.

Migration staging:

- Phase 2: write characterization tests for typing/deletion in complex-script and combining-mark cases.
- Phase 3: use limited edit prototypes only after selection geometry is stable.
- Phase 4: migrate canonical editable surfaces and keep old Views fallback until undo/input parity is proven.

### 14. Lazy loading, notifier maps, and data invalidation

What it is and where it is used:

- `VwEnv::AddLazyVecItems`, root-box notifier maps, and `VwRootBox::PropChanged` allow large FieldWorks data sets to be displayed incrementally and updated from model changes.
- This is part data binding, part virtualization, part incremental document construction.

Standard equivalents:

- Avalonia virtualization, data templates, observable collections, and invalidation can replace UI-framework mechanics.
- No text engine provides FieldWorks object-notifier semantics.

Best end game:

1. Managed incremental document/view model with observable invalidation.
2. Avalonia virtualization for visible collections and lazy expansion.

Migration staging:

- Phase 3: migrate one read-only lazy vector scenario.
- Phase 4: migrate editable lazy views after notifier and selection path tests exist.

### 15. Tables, interlinear layout, pictures, and inline objects

What it is and where it is used:

- Views supports custom tables, piles, embedded pictures/objects, interlinear-style nested layout, paragraph numbers, and placeholders.
- Key areas include `VwEnv.cpp`, `VwTableBox.cpp`, `VwTextBoxes.cpp`, and `VwSelection.cpp` picture-selection handling.

Standard equivalents:

- Avalonia `Grid`, panels, custom controls, and item controls can handle many block/table layouts.
- DirectWrite inline objects and SkParagraph placeholders cover inline embedded items inside paragraphs.
- SkParagraph `addPlaceholder` is a direct paragraph placeholder concept.

Best end game:

1. Use Avalonia layout for block/table/interlinear structure.
2. Use paragraph-engine placeholders only for true inline object gaps.

Migration staging:

- Phase 3: port read-only table/interlinear preview cases.
- Phase 4: port editing, picture selection, and embedded object hit testing.

### 16. Overlays, tags, underlines, spellcheck, and search highlighting

What it is and where it is used:

- `VwTextBoxes.cpp` draws overlay tags, spelling squiggles, underline effects, search results, selection highlights, and other adornments on top of paragraph geometry.
- `SpellCheckMethod`, `DrawOverlayTags`, `DrawTags`, and `DrawUnderline` are tightly coupled to line boxes.

Standard equivalents:

- Avalonia supports text decorations and custom drawing/adorners.
- DirectWrite supports underline/strikethrough, drawing effects, and custom `IDWriteTextRenderer` callbacks.
- SkParagraph returns range rectangles and can be painted under custom overlays.
- Pango supports attributes and layout rectangles.

Best end game:

1. A renderer-neutral adornment layer that maps semantic ranges to paragraph-engine rectangles.
2. Avalonia custom drawing for overlays and tags.

Migration staging:

- Phase 1: include feature-on/off visual baselines with at least one decoration/highlight scenario if feasible.
- Phase 3: port read-only overlays/search highlights.
- Phase 4: port spellcheck/editing overlays after selection geometry parity.

### 17. Graphics abstraction and rasterization

What it is and where it is used:

- Views draws through `IVwGraphics` abstractions over platform graphics, with native render engines returning glyph geometry and metrics.
- This keeps the legacy engine decoupled from a single drawing backend but also preserves native/GDI-era assumptions.

Standard equivalents:

- DirectWrite can draw via Direct2D or custom `IDWriteTextRenderer` callbacks.
- Avalonia draws through `DrawingContext` and commonly uses Skia.
- Skia provides cross-platform 2D drawing, text blobs, and paragraph painting.
- Pango commonly pairs with Cairo.

Best end game:

1. Avalonia `DrawingContext`/Skia for product UI.
2. Keep DirectWrite/Direct2D as a Windows-native reference path only if needed for fidelity or migration debugging.

Migration staging:

- Phase 1: keep GDI/Uniscribe raster output and add baselines.
- Phase 3: add tolerant cross-renderer comparisons using Skia/Avalonia output.
- Phase 4: remove native graphics abstraction after all rendering consumers migrate.

### 18. Printing and pagination

What it is and where it is used:

- `VwRootBox.cpp` and paragraph boxes support page layout, page printing, page-line extraction, widows/orphans, and print-specific drawing.

Standard equivalents:

- DirectWrite can draw `IDWriteTextLayout` to Direct2D/print-compatible targets.
- Skia can render to surfaces and PDF-like outputs depending on integration.
- Avalonia printing support is not a complete replacement for FieldWorks document pagination by itself.

Best end game:

1. A shared document pagination service using the same paragraph/layout engine as screen rendering.
2. Platform-specific print output adapters underneath that service.

Migration staging:

- Phase 3: keep legacy printing while screen previews migrate.
- Phase 4: migrate printing only after on-screen layout parity and page baseline tests exist.

### 19. Accessibility and automation

What it is and where it is used:

- Views has custom selection, object-path, and rendered-document semantics that assistive technologies and UI tests may depend on, even where support is currently incomplete.
- Any replacement must expose text, caret, selection, tables, embedded objects, and navigation in platform accessibility terms.

Standard equivalents:

- Windows UI Automation provides provider/client APIs, control patterns, properties, events, and a tree model.
- Avalonia has automation infrastructure that maps controls to platform accessibility APIs, but custom text surfaces may need custom peers/patterns.

Best end game:

1. Use Avalonia automation peers for standard controls.
2. Implement custom text/document automation peers for FieldWorks document surfaces.

Migration staging:

- Phase 3: include accessibility checks in Avalonia preview prototypes.
- Phase 4: block retirement of native editable views until UIA/text-pattern parity is tested.

### 20. IME, TSF, and complex text input

What it is and where it is used:

- FieldWorks users rely on complex keyboards, input methods, dead keys, combining marks, and multilingual text entry. Native Views editing is tied to Windows input behavior.

Standard equivalents:

- Windows TSF is the platform framework for advanced text input, keyboard processors, handwriting, speech, and multilingual support.
- Avalonia/WinUI text input abstractions can handle standard control input, but custom document editors need careful integration and testing.

Best end game:

1. Let the UI framework handle ordinary input plumbing where possible.
2. Keep explicit FieldWorks tests for IME/composition/combining behavior and use platform TSF hooks only where framework support is insufficient.

Migration staging:

- Phase 2: characterize current input behavior for exotic-language scenarios.
- Phase 3: test Avalonia input prototypes with real keyboards/IME cases.
- Phase 4: migrate data-entry surfaces only after IME and composition parity.

### 21. Search, spellcheck, and linguistic services

What it is and where it is used:

- Paragraph boxes perform search and spellcheck highlighting with writing-system-specific behavior and dictionary selection.
- These services are semantically FieldWorks-specific even when their visual display is part of Views.

Standard equivalents:

- ICU helps with boundaries and collation-like text analysis, but spelling dictionaries and linguistic behavior remain FieldWorks/domain services.
- Modern UI frameworks can draw highlights but do not replace the services.

Best end game:

1. Keep search/spellcheck as managed/domain services.
2. Treat rendering as an adornment consumer of semantic ranges.

Migration staging:

- Phase 3: separate semantic service output from native drawing for read-only highlighting.
- Phase 4: migrate editing spellcheck underlines after paragraph rectangles and invalidation are stable.

### 22. Cache/reuse/performance identity

What it is and where it is used:

- Views caches/reuses layout boxes, render engines, paragraph fragments, and now post-`001-render-speedup` rendered output or buffered frames.
- Feature strings, font fallback, writing system, style props, direction, and text must be part of any shaped/layout/render cache identity.

Standard equivalents:

- SkParagraph has a `ParagraphCache` and font caches.
- Avalonia and DirectWrite have internal layout/font caches, but FieldWorks still owns when data and style changes dirty output.

Best end game:

1. Explicit renderer-neutral cache keys for text layout and rendered baselines.
2. Keep caches near the active renderer, but keep invalidation identity in FieldWorks-owned code.

Migration staging:

- Phase 1: add cache-invalidation tests for feature toggles.
- Phase 3: reuse the same identity model for Avalonia/SkParagraph comparison tests.

### 23. Vertical/inverted/physical-order special cases

What it is and where it is used:

- Views has `VwInvertedParaBox`, inverted paragraph builders, physical box order helpers, and paragraph direction depth logic.
- These special cases are easy to miss because most common surfaces look horizontal and LTR.

Standard equivalents:

- HarfBuzz supports vertical shaping directions, but not paragraph layout.
- DirectWrite, Pango, and SkParagraph have varying support for vertical text and physical/logical mapping.
- Avalonia supports `FlowDirection`, but vertical text should be treated as a separate capability check.

Best end game:

1. Decide whether each special case is still product-required.
2. Preserve required cases as explicit acceptance tests before replacement.

Migration staging:

- Phase 2: inventory real usage of inverted/vertical-like paths.
- Phase 3: spike required cases in Avalonia/SkParagraph/DirectWrite.
- Phase 4: remove dead special cases only with product sign-off.

### 24. Graphite-specific rendering and project compatibility

What it is and where it is used:

- Graphite support is implemented in `GraphiteEngine`/`GraphiteSegment`, enabled from writing-system settings, and exposed through Graphite-centric UI labels and feature discovery.
- Some existing projects may contain Graphite feature strings or rely on Graphite fonts for exotic scripts.

Standard equivalents:

- HarfBuzz/OpenType is the long-term shaping path, but Graphite-specific smart-font behavior may not have a one-to-one OpenType equivalent.
- Graphite2 remains the only direct equivalent for Graphite behavior until fonts and project data are migrated.

Best end game:

1. Move users to OpenType fonts/features where equivalents exist.
2. Preserve old project data and provide warnings/migration guidance for Graphite-only settings.

Migration staging:

- Phase 1: preserve Graphite while adding OpenType support.
- Phase 2: remove Graphite only after compatibility policy, user messaging, and baseline evidence exist.
- Phase 3/4: do not carry Graphite concepts into Avalonia except as migration metadata.

## Recommended Staging Across Phases

### Phase 1: OpenType features in current Views

- Keep native Views and WinForms as production rendering.
- Add renderer-neutral feature parsing/provider UI.
- Apply OpenType features in the existing Uniscribe path.
- Add high-level UI and visual tests in every canonical font-selection place.
- Add HarfBuzzSharp + SkiaSharp only as test/comparison tooling.
- Record baselines for feature-on/off, bidi/multi-writing-system, selection geometry, and cache invalidation.

### Phase 2: Remove Graphite, keep WinForms

- Use Phase 1 baselines to prove OpenType/Uniscribe behavior does not regress.
- Characterize typing, deletion, boundaries, Graphite-only settings, and complex keyboard behavior.
- Add compatibility warnings or migration guidance for Graphite-only project data.
- Introduce renderer-neutral boundary and text-run services where safe.

### Phase 3: Add Avalonia alongside WinForms

- Start with read-only surfaces and previews.
- Use managed adapters from `ITsString`/view-constructor output to Avalonia/SkParagraph text runs.
- Compare output to legacy Views baselines with exact checks for legacy renderer and tolerant/semantic checks across renderers.
- Keep native Views for canonical editing and printing until parity is proven.

### Phase 4: Retire WinForms/native Views

- Migrate editing surfaces after selection, IME/TSF, undo, accessibility, lazy loading, and printing all have tests.
- Remove native COM and Views box layout only after the managed/Avalonia document surface covers canonical FieldWorks workflows.
- Keep renderer-neutral feature strings and visual/semantic baselines as long-term regression assets.
