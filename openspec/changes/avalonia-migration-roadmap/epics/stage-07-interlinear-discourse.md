# Stage 7 — Texts & Words / Interlinear + Discourse (Epic draft)

> JIRA-ready draft generated from:
> - `complete-migration-program.md` §6 Stage 7 (lines 377–392, incl. post-review callout), §7 Definition of Done, §10 JIRA structure/labels.
> - `reviews/stage-07-interlinear-discourse.md` (repo-grounded coupling evidence).
> - `reviews/00-cross-comparison-synthesis.md` §3 (conflict resolutions), §6 (sub-epic map), §7 (sequencing edges).
>
> Decomposes one over-coarse stage into **four sub-epics (7A–7D)** per synthesis §6/§7. Issues under
> each sub-epic carry the §7 Definition of Done as a checklist and region-manifest fields as acceptance
> criteria. Planning artifact only — no code/behavior change.

---

## Epic

**Summary:** Stage 7 — Migrate the Texts & Words area (Interlinear document + Sandbox/FocusBox, constituent charts, concordance/statistics, and import wizards) from WinForms + native Views to Avalonia.

**Type:** Epic (Track II — Surfaces). Parent initiative: "FieldWorks → Avalonia complete migration."

**Labels:** `track-surfaces`, `lead-senior` (epic-level; per-sub-epic lead labels below), `parity-blocked-by:stage-9-engine` (7A), `parity-blocked-by:stage-3-grid` (7B/7C), `parallel-safe` (7C/7D only).

**Description:**
Stage 7 covers the Texts & Words domain, the most Views-engine-entangled non-shell area of FieldWorks. The five legacy surfaces span **1–2 orders of magnitude in Views coupling** (`reviews/stage-07` §1), so this epic is *not* a single deliverable: it decomposes into four sub-epics with distinct leads, dependencies, and critical-path positions.

- **7A — Interlinear doc + Sandbox/FocusBox** (~27K lines; `Src/LexText/Interlinear`). The deepest Views construction in the codebase — `SandboxBase : RootSite` with a private secondary `VwCacheDa` via `CachePair`, hit-test combo editing, and the aligned word/morpheme/gloss grid layout. Hard-blocks on an **extended** Stage 9.
- **7B — Constituent chart** (`Src/LexText/Discourse`). `ConstituentChartLogic` (~5.5K lines) is pure domain logic that ports as-is; rendering (~1.8K lines) rebuilds on the **Stage 3** owned grid, not the Stage 9 document engine.
- **7C — Concordance / ComplexConc / Statistics** (`UserControl` hosts, no `IVwEnv` in the host). Stage-5-class work; off the Stage-9 critical path.
- **7D — Import wizards** (SFM / LinguaLinks / BIRD / Words-SFM). WinForms wizard dialogs + non-UI importers; junior/MVVM per decision §11.3. Off the Stage-9 critical path.

**Acceptance criteria (epic-level):**
- All four sub-epics (7A–7D) closed with their own parity bundles green.
- Each migrated surface satisfies the §7 per-surface Definition of Done (custom-slice census, semantic+visual+workflow+performance baselines captured before refactor and matched after, seams reused, owned-control choices justified, AutomationIds, localization lanes, `EngineIsolationAuditTests` green, `./build.ps1` + `./test.ps1` green, retrospective folded into the skill set in the same PR).
- `EngineIsolationAuditTests` green on every migrated Texts & Words surface (no residual `IVwRootBox`/`IVwEnv`/`IVwSelection`/`RootSiteControl` on the Avalonia path).
- Performance ≤ legacy × 1.2 (or accepted delta recorded) on the **largest available corpus**, at 100% and 150% DPI — virtualization is mandatory (see Notes).

**Dependencies:**
- **Stage 9 (extended) → 7A** — the headline cross-stage dependency. Stage 9's written deliverables (StText + selection/caret + structured doc model) do **not** name the Sandbox/interlinear constructs; this epic is gated on the Stage 9.0 spike covering them and on the 9↔7A engine seam being frozen *after* a Sandbox spike (synthesis §3, §7; risk register).
- **Stage 3 → 7B/7C** — owned virtualized editable grid (chart table; concordance results grid).
- **Stage 3 → 7A** (display) for the aligned interlinear grid if grid-owned (undecided — see Notes).
- **Stage 5 pattern / decision §11.3 → 7D** — MVVM + compiled bindings dialog toolchain (delivered by Stage 1's dialog kit).
- **Stage 6 / FdoUi → 7A/7C** — reuse `FwOptionPicker`-style analysis choosers already built; do not rebuild.
- **Stage 10 (print/preview)** — `InterlinPrintView` and chart print/export overlap Stage 10; confirm ownership (see Notes).

**Rough size:** XXL (largest Track-II epic). Dominated by 7A (~27K lines, extreme coupling, gated on the long pole). 7B medium, 7C small–medium, 7D small. 7C/7D are parallelizable immediately; 7A is the long pole; 7B can start once Stage 3 lands.

---

## Sub-epics / stories

### 7A — Interlinear document + Sandbox/FocusBox  *(senior; hard-blocks on extended Stage 9)*

**Summary:** Migrate the interlinear document view and the Sandbox/FocusBox analysis builder to the managed Avalonia document engine.

**Type:** Sub-epic (Track II / Track III seam).

**Description:**
The single hardest surface in the whole program. Two distinct specialized Views constructions:
- **Sandbox / FocusBox** (`SandboxBase.cs` ~4973, `.ComboHandlers.cs` ~3450, `.MorphemeBreaker.cs` ~1152, `.SandboxVc.cs` ~797): `SandboxBase : RootSite` backed by a **private in-memory `VwCacheDa`** created via `CachePair.CreateSecCache()`, with a bidirectional HVO map between fake SbWord/SbMorph objects and real LCModel objects. Editing is driven by **selection hit-testing** (`ShowComboForSelection(IVwSelection)`, `ScanForIcon`, `m_rootb.MakeSelAt(...)`); re-render via `RootBox.Reconstruct()` after `PropChanged`. `MorphemeBreaker` mutates the secondary cache directly.
- **Interlinear doc** (`InterlinVc.cs` ~3376, `InterlinDocForAnalysis.cs` ~2606, `InterlinDocRootSiteBase.cs` ~1262): `InterlinVc : FwBaseVc` with 287 `IVwEnv` calls, 70+ `kfrag*` fragments, and an **aligned word/morpheme/gloss/POS grid** layout idiom (multiple WS rows under one vernacular word, column alignment across the paragraph) that ordinary StText editing does not produce. Plus `InterlinViewDataCache` and per-pane SDA decorators, the `InterlinTaggingVc`/tagging child, and `RawTextPane`/`TitleContentsPane` hosts (all `RootSite`-derived).

Approach (per `reviews/stage-07` §3): (a) replace the secondary `VwCacheDa` with a **managed presentation/decorator model**, not a port of `IVwCacheDa`; (b) replace icon/picture-anchored hit-test editing with a managed equivalent; (c) extract Sandbox logic (morpheme-break, approve-and-move, choose-analysis) and interlinear line-choice logic into **characterization-tested** units before touching the renderer; (d) map the aligned grid explicitly to a Stage-3 grid or Stage-9 layout primitive and record the pivot trigger. `RawTextPane` (plain StText) is the only piece clearly covered by Stage 9 as currently scoped. Fold `InterlinTaggingVc` and the `InterlinPrintView` print path in here (or hand the print path to Stage 10 — confirm).

**Acceptance criteria (ref §7 DoD):**
- §7 DoD met for the interlinear doc and Sandbox/FocusBox surfaces (custom-slice census; semantic+visual+workflow+performance baselines before/after; seams from `ISeams.cs`, new seams recorded with pivot trigger; owned-control choices per `architecture-patterns.md` §4; stable AutomationIds from StableId; explicit `HostUiBehavior`; Path-3 bundle per scenario; localization lanes; `EngineIsolationAuditTests` green; `./build.ps1` + `./test.ps1` green; retrospective in same PR).
- No native Views on the Avalonia path — secondary cache, hit-test editing, and aligned grid all expressed in managed engine primitives.
- Performance ≤ legacy × 1.2 on the **largest available text** at 100%/150% DPI; lazy/virtualized rendering preserved (legacy uses `AddLazyVecItems`/`AddObjVecItems`).
- Sandbox edit flows (morpheme break, approve-and-move, choose analysis, gloss/POS edit) covered by characterization tests passing pre- and post-migration.

**Dependencies:**
- **Stage 9 (extended) — hard block.** Requires Stage 9 to deliver (a) a secondary in-memory presentation cache / decorator editing model replacing `CachePair`/`VwCacheDa`, (b) icon/picture-anchored hit-test combo editing replacing `IVwSelection` scan + `MakeSelAt`, (c) the aligned multi-line word/morpheme/gloss grid layout. **Engine seam:** no Stage 9 API may be frozen until a Sandbox/interlinear scenario runs in the Stage 9.0 spike (synthesis §3; risk register).
- Stage 3 (display) if the aligned grid is grid-owned (undecided).
- Stage 6 / FdoUi analysis choosers (reuse `FwOptionPicker`).

**Labels:** `track-surfaces`, `lead-senior`, `parity-blocked-by:stage-9-engine`.

**Rough size:** XL (long pole of the whole surface track).

---

### 7B — Constituent chart  *(mid/senior; depends on Stage 3, light Stage 9)*

**Summary:** Migrate the discourse constituent chart — port the logic as-is, rebuild rendering on the Stage-3 grid, and rewrite the exporter as direct domain→XML.

**Type:** Sub-epic (Track II).

**Description:**
Splits cleanly (`reviews/stage-07` §2c). `ConstituentChartLogic.cs` (~5459 lines, ~49% of Discourse) is **pure domain logic** — `MoveToColumn`, `MakeWordGroup`, `InsertRow`, `ToggleMissingMarker`, drag/move/merge — with essentially no Views references; **port it as-is** behind characterization tests. Rendering is confined to `ConstChartVc` (`Display(IVwEnv)`), `ConstChartBody : RootSite`, `MakeCellsMethod` (table-cell emission), and `ChartRowEnvDecorator : IVwEnv` (RTL buffering — **delete it**, Avalonia handles `FlowDirection`). The chart is fundamentally a **table**, so rebuild rendering on the **Stage-3 owned virtualized grid**, not the Stage-9 document engine. Rewrite `DiscourseExporter : CollectorEnv` (currently a view-walk reusing `ConstChartVc.Display`) as a **direct domain→XML exporter** to drop the Views dependency. Confirm chart print/export ownership vs. Stage 10.

**Acceptance criteria (ref §7 DoD):**
- §7 DoD met for the chart surface (baselines before/after, seams, owned-control = Stage-3 grid with pivot trigger if deviating, AutomationIds, localization, `EngineIsolationAuditTests` green, build/test green, retrospective in same PR).
- `ConstituentChartLogic` ported with characterization tests passing on legacy and migrated code.
- `DiscourseExporter` rewritten as direct domain→XML with output parity against the legacy view-walk exporter.
- `ChartRowEnvDecorator` removed; RTL handled via Avalonia `FlowDirection`.

**Dependencies:**
- **Stage 3 → 7B** (owned virtualized editable grid — the chart table). Primary dependency.
- Light Stage 9 (any residual shared text primitives in cell content only).

**Labels:** `track-surfaces`, `lead-mid` (rendering may need senior review), `parity-blocked-by:stage-3-grid`.

**Rough size:** M (logic is free; rendering ~1.8K lines on the shared grid).

---

### 7C — Concordance / ComplexConc / Statistics  *(mid; Stage-5-class)*

**Summary:** Migrate the concordance, complex-concordance, and statistics views — `UserControl` hosts with low Views coupling.

**Type:** Sub-epic (Track II; off the Stage-9 critical path).

**Description:**
`ConcordanceControl` (~1846), `ComplexConcControl` (~812), `ConcordanceControlBase`, and `StatisticsView` (~301) are `UserControl` hosts with **no `IVwEnv`/`IVwRootBox` in the host itself** — Stage-5-class work. The results/preview pane re-hosts `InterlinVc`, so the embedded interlinear preview depends on 7A/Stage 9; the host chrome, search/filter UI, and results grid do not. The results grid rides the **Stage-3** owned table. `ComplexConcPatternVc` (part of the ~121 `IVwEnv` calls across pattern VCs noted in `reviews/stage-07` §6) is the one specialized VC here — fold it into this sub-epic's scope explicitly.

**Acceptance criteria (ref §7 DoD):**
- §7 DoD met for concordance, complex-concordance, and statistics surfaces (baselines before/after, seams, AutomationIds, localization, `EngineIsolationAuditTests` green on the host, build/test green, retrospective in same PR).
- Results grid built on the Stage-3 owned table; search/filter/criteria UI at parity.
- Embedded interlinear preview pane wired to the 7A/Stage-9 managed engine (or explicitly gated until 7A lands, never silent legacy fallback).

**Dependencies:**
- **Stage 3 → 7C** (results grid).
- Stage 9 / 7A — **only** for the embedded interlinear preview pane (not the host). Decouple so the host ships independently.

**Labels:** `track-surfaces`, `lead-mid`, `parallel-safe`, `parity-blocked-by:stage-3-grid`.

**Rough size:** M (hosts are small; gated only on the embedded preview).

---

### 7D — Import wizards (SFM / LinguaLinks / BIRD / Words-SFM)  *(junior; MVVM)*

**Summary:** Migrate the interlinear/text import wizards to Avalonia MVVM dialogs.

**Type:** Sub-epic (Track II; junior/Stage-5 pattern; off the Stage-9 critical path).

**Description:**
`InterlinearSfmImportWizard`, `LinguaLinksImport`, `BIRDInterlinearImporter`, `WordsSfmImportWizard` are ordinary WinForms wizard dialogs plus non-UI importers (`reviews/stage-07` §2d). **No Views coupling.** Per decision §11.3, build these as **MVVM + compiled bindings** (CommunityToolkit.Mvvm) — no IR/region machinery (they have no XML layout). Reuse owned WS-aware field controls (`FwMultiWsTextField`, `FwOptionPicker`) inside the dialogs where WS-aware text or chooser fields appear. During coexistence (until Stage 11), modality stays a WinForms-owned `Form` wrapping an Avalonia body via `WinFormsAvaloniaControlHost` (Stage-5 host-wrapped-body contract). The non-UI importers (`BIRDInterlinearImporter`) are framework-agnostic — reclassify as non-UI, migrate only the wizard chrome.

**Acceptance criteria (ref §7 DoD):**
- §7 DoD met per wizard (parity bundle, AutomationIds, localization review; `EngineIsolationAuditTests` green; build/test green; retrospective in same PR).
- MVVM + compiled bindings; no IR/region machinery; no `new Window().ShowDialog()` during coexistence (host-wrapped body).
- Import round-trip parity (sample SFM/LinguaLinks/BIRD inputs → identical LCModel result vs. legacy).

**Dependencies:**
- **Stage 1 dialog/MVVM kit** (CommunityToolkit.Mvvm + compiled bindings + dialog scaffolding) and the Stage-5 host-wrapped-body contract.
- None on Stage 9 or Stage 3.

**Labels:** `track-surfaces`, `lead-junior`, `parallel-safe`.

**Rough size:** S–M (several wizard dialogs; mechanical once the kit exists).

---

## Notes / open questions

1. **Stage 9 coverage gap for Sandbox/interlinear constructs (TOP RISK).** Stage 9's written deliverables (StText editing, managed selection/caret replacing `VwSelection`, structured doc model replacing `VwRootBox`) **do not name** the three load-bearing 7A constructs: (a) the secondary in-memory presentation cache/decorator model replacing `CachePair`/`VwCacheDa`; (b) icon/picture-anchored hit-test combo editing replacing `IVwSelection` scan + `MakeSelAt`; (c) the aligned multi-line word/morpheme/gloss grid layout. **Resolution (synthesis §3):** expand Stage 9 scope and add a named Sandbox/interlinear sub-spike to **Stage 9.0**; freeze no Stage 9 engine API until that spike runs, or 7A discovers a missing engine capability late and stalls. 7A must depend on the **9.0 spike output**, not merely on Stage 9 completion.
2. **Aligned-grid layout ownership undecided.** Interlinear column alignment crosses both the Stage-3 owned grid and the Stage-9 layout/box model. It is neither a plain table nor a plain paragraph. Record an explicit decision/pivot trigger in `seam-catalog.md` before 7A rendering starts.
3. **Performance on large corpora.** `InterlinVc` uses lazy `AddObjVecItems`/`AddLazyVecItems` precisely because texts are large; a naive non-virtualized managed reimplementation **will** regress. Baseline on the largest available text first; treat virtualization as a hard requirement, not an optimization.
4. **Specialized pattern VCs not separately budgeted.** `InterlinTaggingVc` (tagging) and `ComplexConcPatternVc` (~121 `IVwEnv` calls across the pattern VCs) are extra specialized VCs — folded into 7A (tagging) and 7C (pattern) scope above; confirm sizing.
5. **Print views overlap Stage 10.** `InterlinPrintView` and chart print/export overlap Stage 10 (print/preview). Confirm ownership to avoid a gap — recommend Stage 10 owns the print pipeline, 7A/7B own the on-screen view.
6. **`DiscourseExporter` rewrite vs. port.** Decision taken above (rewrite as direct domain→XML to drop the Views walk). Confirm no downstream consumer depends on the exact `CollectorEnv` walk ordering.
7. **Embedded interlinear preview seam (7C).** The concordance/statistics hosts re-host `InterlinVc`; ensure the host ships independent of 7A by gating only the preview pane (explicit "unsupported/preview-pending" render, never silent legacy fallback).
