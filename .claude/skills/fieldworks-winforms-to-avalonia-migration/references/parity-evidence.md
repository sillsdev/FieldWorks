# Parity Evidence — Shared Definitions

Canonical home for the evidence vocabulary used across all fieldworks-*
migration skills. Other skills reference these definitions instead of
redefining them.

Contents:

1. The Path 3 bundle (triangulated parity evidence)
2. Evidence types and what each can prove
3. Evidence language (claim-downgrading taxonomy)
4. Forbidden symbols (engine isolation)
5. Performance budgets
6. Artifact naming

## 1. The Path 3 bundle (triangulated parity evidence)

"Path 3" is the migration-quality visual-fidelity evidence type. A single artifact
cannot prove a region is migrated; the bundle triangulates. For one
scenario id, produce:

- `semantic.json` — semantic snapshot of the IR/region (the anchor; both
  legacy import and Avalonia compose must produce it)
- `visual.legacy.png` — WinForms screenshot (100% and 150% DPI)
- `visual.avalonia.png` — Avalonia capture, same framing/DPI
  (Avalonia.Headless rendered frames are acceptable when the scenario is
  explicitly control-scoped)
- diff/variance artifact interpreted against stable binding/focus/
  accessibility identity — never a raw pixel diff in isolation
- `workflow.legacy.md` / `workflow.avalonia.md` — accessibility/keyboard
  workflow evidence (UIA2 on realized windows for desktop claims)
- `performance.json` — init/populate/refresh timings
- `failure-summary.md` — when something fails, classify which evidence
  type failed with diagnostics; do not hand reviewers a raw image diff

Canonical harness: `Src/Common/FwAvalonia/FwAvaloniaTests/Path3BundleTests.cs`;
bundle contract provenance:
`openspec/changes/lexical-edit-avalonia-migration/coverage-map.md` §9.

## 2. Evidence types and what each can prove

| Evidence type | Tooling | Proves | Cannot prove |
| --- | --- | --- | --- |
| Semantic snapshot | IR `ToSnapshot()`, JSON per scenario | Binding, labels, editor kind, visibility, ghost state, focus order, WS metadata, accessibility identity | Typography, density, wrapping, native rendering |
| Visual/render | Avalonia.Headless Skia capture; legacy screenshots | Layout, density, fonts, wrapping | Why a field is missing (semantic evidence explains that) |
| Workflow/accessibility | Avalonia.Headless input simulation; UIA2/FlaUI on realized windows | Focus movement, invoke paths, chooser reachability, keyboard shortcuts, native automation tree | Pixel fidelity |
| Performance | Timing harness + committed baselines | Budgets vs. measured legacy | Anything functional |

Headless and desktop are different environments: a headless smoke test is
never a UIA2 baseline, and desktop workflow claims need realized-window
evidence.

### 2a. Headless integration scenarios (the prioritized default)

Behavior and workflow claims are proven **first** by Avalonia **headless
integration tests that script real user scenarios** — not deferred to manual
"live verification," and not a unit test that pokes a handler. Two fidelities
(pick by what the claim needs), both on `./test.ps1`:

- **Surface-workflow** (`FwAvaloniaTests`): co-host the owned control(s) in a
  headless window and drive them through page-object drivers (filter, clear,
  select, type, commit), asserting observable state. Proves control + seam
  round-trips (e.g. select-row→detail-follows, edit→cell-refresh).
- **Real-domain** (`xWorksTests`): a real `RecordClerk` over an in-memory
  LCModel cache, asserting the real list narrows/reorders/restores — the
  domain-fidelity claim that used to require running FLEx.

Harness + how it scales across phases: architecture-patterns.md §13 and
`openspec/changes/shared-editable-virtualized-table/headless-integration-harness.md`.
A surface is not "ready for manual testing" until its key workflows have headless
scenario coverage; manual/UIA2 desktop runs then confirm pixel/native-tree axes
the headless environment cannot.

## 3. Evidence language (claim-downgrading taxonomy)

When verifying task checkboxes or PR claims, scan the evidence text for
these words and downgrade the claim accordingly:

- **substitute** — a different artifact stands in for the claimed one;
  the claim is unproven.
- **placeholder** — data or metadata is fake; parity is not demonstrated.
- **skipped** — the test exists but did not run; no evidence.
- **future / planned** — work item, not evidence.
- **partial** — name exactly which axes are proven and which are not.
- **live-verification-only / manual-only** — a behavior/workflow claim resting
  solely on running FLEx by hand. Downgrade it: nearly all such claims (filter,
  sort, selection, edit→refresh, navigation, list narrowing) are provable by a
  headless integration scenario (§2a). Manual is the supplement for pixel/native
  axes, not the primary proof.

A checked task whose evidence says any of the above is a review blocker:
either the evidence improves or the checkbox is unchecked.

## 4. Forbidden symbols (engine isolation)

Migrated-region production code must not reference, in any runtime path:

- `System.Windows.Forms.Control`
- `DataTree`, `Slice`, `SliceFactory`, `RootSiteControl`
- `XmlView`, `BrowseViewer`
- `IVwRootBox`, `IVwEnv`, `IVwGraphics`, `IRenderEngine`
- `GraphiteEngineClass`, `UniscribeEngineClass`
- `GeckoWebBrowser`, `XWebBrowser`, `GeckofxHtmlToPdf`

Enforced by `Src/Common/FwAvalonia/FwAvaloniaTests/EngineIsolationAuditTests.cs`.
When a migration discovers a new legacy symbol that must not leak, add it
to the audit test AND this list in the same PR. Custom linguistic services
(XAmple, spelling, parsers, ICU, encoding converters) may remain behind
explicit service seams when they do not own the render/editor surface.

## 5. Performance budgets

Budgets are measured, never estimated:

1. Capture legacy timings with the characterization harness
   (`Src/Common/Controls/DetailControls/DetailControlsTests/DataTreeRenderTests.cs`)
   on a named machine profile; thresholds are generated into
   `DataTreeTimingBaselines.json` (gitignored, regenerated per machine, not
   checked into the repo).
2. Hold the Avalonia surface to within 20% of measured legacy total, or
   record an explicitly accepted delta with justification in the region
   manifest.
3. Measure cold vs. warm separately; IR compile latency must stay
   deterministic and small; refresh-after-edit has its own baseline.
4. Always include the 150% DPI path and the large fixtures before
   accepting a control choice.

## 6. Artifact naming

`{scenarioId}/{bundleId}/semantic.json`, `visual.legacy.png`,
`visual.avalonia.png`, `visual.diff.png` (optional diff artifact),
`workflow.legacy.md`, `workflow.avalonia.md`, `performance.json`,
`failure-summary.md`. Manual/hand captures use the same layout with a
`bundleId` of the form `manual-YYYYMMDD`.

Snapshots must normalize away pixel bounds, transient generated names,
timestamps, machine paths, and culture-dependent ordering — see
`fieldworks-semantic-render-parity` for include/exclude rules.
