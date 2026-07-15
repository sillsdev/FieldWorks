# Graphite and Native Rendering Decommissioning Plan

> **Superseded for Graphite policy (2026-06-09).** Graphite support policy now lives in the
> dedicated change **`graphite-transition-support`**: Graphite remains supported on legacy surfaces
> until the M2 mid-decommissioning milestone, Avalonia surfaces render with OpenType plus a graded
> per-writing-system **warning** (G0–G3 classification) instead of being blocked, and removal
> happens at M3 with WinForms. The "decommissioning starts with the migration" and "Avalonia
> default blocked by Graphite" posture in this document no longer applies. What REMAINS in scope
> here: the native-engine isolation audit (no `GraphiteEngineClass`/native Views shaping on the
> Avalonia path — true under the new policy too), the inventory tables below as reference, and the
> Gecko/browser/PDF classification (sections still owned by lexical-edit tasks 5.5–5.8).

## Handoff Notes (2026-06-15, from `avalonia-multi-writing-system-text-foundation`)

- Managed `ITsString` rich-run projection/edit/write-back landed for lexical text rows, including coexistence clipboard/drag-drop and shared undo/refresh behavior; this closes text-foundation blockers without changing Graphite fallback policy.
- The Avalonia text lane now has automated coverage for Unicode grapheme boundaries (combining marks/surrogate pairs/ZWJ), IME composition-state transitions, and mixed-direction selection regressions; these results should be consumed by `graphite-transition-support` as input evidence, not as a policy override.
- Explicitly deferred for Graphite-policy tracking: `StText` editing and unsupported object-content rich runs (read-only lane). Keep these in the G-classification backlog until dedicated support lands.

This plan treats Graphite and native Views rendering as a migration risk, not as a solved problem. The key correction from re-research: Avalonia using Skia/HarfBuzz does not automatically prove Graphite parity. HarfBuzz Graphite2 shaping is optional and its own documentation says Graphite2 support is currently not enabled by default when building HarfBuzz.

## 1. Current Repo Inventory

| Area | Source / Symbol | Role | Migration Classification |
|---|---|---|---|
| Native Graphite render engine | [Src/views/lib/GraphiteEngine.cpp](Src/views/lib/GraphiteEngine.cpp), `GraphiteEngineClass`, `FwGrEngine`, `GraphiteSegment` | Current native Views Graphite shaping/rendering path. | Default-path blocker until Avalonia text shaping evidence exists. |
| Native Graphite segmenting | [Src/views/lib/GraphiteSegment.cpp](Src/views/lib/GraphiteSegment.cpp), [Src/views/lib/GraphiteSegment.h](Src/views/lib/GraphiteSegment.h) | Segment data, glyph metrics, and render behavior. | Baseline source for parity fixtures. |
| Render factory | [Src/Common/FwUtils/RenderEngineFactory.cs](Src/Common/FwUtils/RenderEngineFactory.cs) | Chooses Graphite vs Uniscribe render engines based on writing-system/font configuration. | Hidden dependency audit target. |
| COM/native interfaces | [Src/views/lib/Render.idh](Src/views/lib/Render.idh), `IRenderEngine`, `IRenderEngineFactory` | COM interfaces for native rendering. | Forbidden from migrated Avalonia default path unless intentionally bridged and documented. |
| Writing-system storage | `IsGraphiteEnabled`, `DefaultFontFeatures`, `FontEngines.Graphite` usages across LCModel/Common | Stores project/language rendering preferences and feature strings. | Must be preserved in snapshots or diagnostics. |
| Browser/PDF paths | `GeckofxHtmlToPdf`, `FieldWorksPdfMaker`, PDF `--graphite` flags, XHTML preview/export | Non-edit rendering/export surfaces may still require legacy Graphite behavior. | Separate from Lexical Edit default path; do not remove during first migration. |
| Packaging/build | Graphite2/HarfBuzz native libraries under build/package scripts | Determines which shaping libraries are present. | Packaging audit required before any claim of parity. |

## 2. External Documentation Findings

- HarfBuzz building docs: Graphite2 support is controlled by the build option `-Dgraphite=enabled`; the default is not enabled.
- HarfBuzz Graphite2 integration docs: Graphite features work only when HarfBuzz was compiled with the Graphite2 shaping engine enabled.
- Avalonia font docs: Avalonia supports TrueType/OpenType custom fonts and OpenType `FontFeatures`, but the docs do not establish FieldWorks Graphite feature parity.

Conclusion: the migration may use Avalonia text rendering for many scripts, but Graphite parity must be proven with actual fonts, writing-system metadata, and packaged native shaping support.

## 3. Default-Path Policy

The migrated Lexical Edit default path MUST NOT depend on these symbols unless a specific exception is approved and tested:

- `System.Windows.Forms.Control`, `DataTree`, `Slice`, `RootSiteControl`, `XmlView`, `BrowseViewer`.
- `IVwRootBox`, `IVwEnv`, `IVwGraphics`, `IRenderEngine`, `IRenderEngineFactory`.
- `GraphiteEngineClass`, `UniscribeEngineClass`, `FwGrEngine`, `GraphiteSegment`.
- `GeckoWebBrowser`, `XWebBrowser`, `GeckofxHtmlToPdf`, `FieldWorksPdfMaker`.
- Global COM registration or registry hacks.

Legacy code may remain for non-migrated views, preview/export, tests, and rollback. The policy applies only to the new migrated default path.

## 4. Required Evidence Before Decommissioning

| Evidence | Required Checks |
|---|---|
| Repository audit | Search default-path projects for forbidden symbols; document any allowed baseline/test-only references. |
| Packaging audit | Confirm the exact HarfBuzz/Skia/native library build includes or excludes Graphite2 support; record the binary/source evidence. |
| LDML fixture scan | Identify writing systems with `IsGraphiteEnabled`, `DefaultFontFeatures`, Graphite-only features, right-to-left scripts, complex scripts, and custom fonts. |
| Rendering fixtures | Capture representative words/forms for Graphite-enabled fonts and compare legacy screenshot/metrics with Avalonia output where feasible. |
| Fallback behavior | For unsupported Graphite features, produce visible diagnostics and a rollback path rather than silently changing rendering. |
| Browser/PDF decision | Classify each browser/PDF/export path as legacy-retained, migrated later, or out of scope. |

## 5. Phased Plan

| Phase | Work | Exit Gate |
|---|---|---|
| Phase 1 inventory | Complete source and symbol inventory for native Views, Graphite, browser/PDF, writing-system metadata, and package inputs. | Inventory has source-backed entries and no unsupported Avalonia/HarfBuzz assumptions. |
| Phase 2 characterization | Add fixtures for Graphite-enabled writing systems and text samples, plus current native/default path coverage report. | Fixtures identify which scripts/fonts are safe, risky, or blocked. |
| Phase 3-5 first-slice migration | Keep Graphite and native Views as legacy fallback while the first Avalonia editor uses only proven text paths. | Default path forbidden-symbol audit passes or lists explicit approved exceptions. |
| Phase 6-8 parity expansion | Add measured Avalonia rendering evidence for complex scripts, IME, RTL, and Graphite feature scenarios. | User-visible rendering differences are accepted, fixed, or blocked with rollback. |
| Phase 9 retirement | Remove or disable a legacy rendering dependency only after all consumers are classified. | Browser/PDF/export and rollback owners sign off; full build/test/package checks pass. |

## 6. Test Matrix

| Scenario | Minimum Test |
|---|---|
| Graphite-enabled WS with feature string | Fixture loads WS metadata, text sample renders, and diagnostics record Graphite capability. |
| OpenType-only font features | Avalonia `FontFeatures` path preserves documented OpenType features where supported. |
| Graphite-required feature unsupported | UI exposes deterministic diagnostic and keeps rollback/default-off path. |
| Mixed writing systems | Per-run and per-span metadata is preserved in the typed presentation snapshot. |
| Packaging drift | CI or agent script records native shaping library versions/options for the build under test. |

This plan intentionally avoids promising Graphite decommissioning as part of the first editable slice. The first milestone is knowing exactly where Graphite matters and preventing silent rendering regressions.