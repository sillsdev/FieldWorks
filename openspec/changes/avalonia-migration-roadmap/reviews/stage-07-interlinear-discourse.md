# Stage 7 Review — Texts & Words / Interlinear + Discourse

> Reviewer pass against the live branch `010-advanced-entry-view-phase-1-2`.
> Master plan: `openspec/changes/avalonia-migration-roadmap/complete-migration-program.md` §6 Stage 7
> (lines 277–283) and Stage 9 (lines 292–306). Repo evidence read directly from
> `Src/LexText/Interlinear/`, `Src/LexText/Discourse/`, `Src/Common/SimpleRootSite/`,
> and `Src/Common/FwUtils/CachePair.cs`. Architecture refs:
> `.claude/skills/fieldworks-winforms-to-avalonia-migration/references/{architecture-patterns,parity-evidence,migration-checklist}.md`.

## 1. Scope assessment

Stage 7 (plan lines 277–283) bundles five very different surfaces under one senior epic:

1. Interlinear doc + sandbox (`Src/LexText/Interlinear`, ~98 files) — word/morpheme breakdown, glossing, POS.
2. Concordance + raw-text + statistics views.
3. Constituent charts (`Src/LexText/Discourse`) — chart body, logic, export.
4. Import wizards (SFM, LinguaLinks) — flagged as splittable to Stage-5-style hand-off.
5. Stated dependencies: Stage 9 (managed document engine) + Stage 3 (shared tables).

**This is the right grouping by *domain* but wrong as a single deliverable.** The five items differ
by 1–2 orders of magnitude in Views coupling. Measured from the repo:

| Surface | Lead lines | Views coupling | Migratability |
| --- | --- | --- | --- |
| **Sandbox / FocusBox** (`SandboxBase.cs` 4973, `.ComboHandlers.cs` 3450, `.MorphemeBreaker.cs` 1152, `.SandboxVc.cs` 797) | ~14k | **Extreme** — `RootSite` base, secondary `VwCacheDa` cache, fragment VC, `IVwSelection` hit-test editing | Hardest surface in the whole program |
| **Interlinear doc** (`InterlinVc.cs` 3376, `InterlinDocForAnalysis.cs` 2606, `InterlinDocRootSiteBase.cs` 1262) | ~13k | **Extreme** — 287 `IVwEnv` calls in `InterlinVc` alone, 70+ `kfrag*` constants, 8 VC overrides | Very hard |
| **Constituent chart** (`ConstituentChartLogic.cs` 5459, `ConstChartVc.cs` 727, `ConstChartBody.cs` 513, `MakeCellsMethod.cs` 570) | ~11k | **Logic: none; rendering: high** — `ConstituentChartLogic` has ~zero Views refs; rendering concentrated in ~1,800 lines | Logic ports as-is; rendering medium |
| **Concordance / ComplexConc / Statistics** (`ConcordanceControl.cs` 1846, `ComplexConcControl.cs` 812, `StatisticsView.cs` 301) | ~3k | **Low** — `UserControl` bases, 0 `IVwEnv`/`IVwRootBox` in the host controls (the *preview* pane reuses `InterlinVc`) | Mostly Stage-5-style |
| **Import wizards** (`InterlinearSfmImportWizard`, `LinguaLinksImport`, `BIRDInterlinearImporter`, `WordsSfmImportWizard`) | ~several k | **None** — WinForms wizard dialogs + non-UI importers | Junior/Stage-5 dialogs |

**Recommendation: split Stage 7 into four epics** (details in §5). One epic this size, spanning a
trivial wizard dialog and the single most Views-entangled control in FieldWorks, cannot be sized,
staffed, or gated coherently.

## 2. Feasibility (repo-grounded)

### 2a. Sandbox is the deepest Views construction in the codebase — and Stage 9 does not describe it

`SandboxBase : RootSite` (`Src/LexText/Interlinear/SandboxBase.cs:34`). It is **not** a document/StText
editor. It is an interactive analysis-builder backed by a private in-memory data access:

- `protected CachePair m_caches` (`SandboxBase.cs:146`); `m_caches.CreateSecCache()` (`:993`) creates a
  `VwCacheDaClass` (`Src/Common/FwUtils/CachePair.cs:195–202`) with a bidirectional HVO map between
  fake "SbWord/SbMorph" objects and real LCModel objects. `IVwCacheDa cda = (IVwCacheDa)m_caches.DataAccess`
  (`SandboxBase.cs:1112`). `MakeRoot` binds it: `m_rootb.DataAccess = m_caches.DataAccess;
  m_rootb.SetRootObject(kSbWord, m_vc, SandboxVc.kfragBundle, m_stylesheet)` (`SandboxBase.cs:~4065–4074`).
- Editing is driven by **selection hit-testing**: `ShowComboForSelection(IVwSelection …)`, `ScanForIcon`,
  `FindNearestSelectionType`, and `m_rootb.MakeSelAt(pt.X, pt.Y, …)` (`SandboxBase.cs:2451–2624, ~4400`).
  Re-render is `RootBox.Reconstruct()` after `m_caches.DataAccess.PropChanged(...)` (multiple sites).
- `MorphemeBreaker` mutates the secondary cache directly: `m_cda = (IVwCacheDa)m_sda` (`SandboxBase.MorphemeBreaker.cs:53–65`).
- `SandboxVc.Display(IVwEnv vwenv, int hvo, int frag)` (`SandboxBase.SandboxVc.cs:292`) is a fragment
  switch (~8 cases) with 100+ `IVwEnv` calls.

None of this maps onto Stage 9's stated deliverables (lines 300–303): "multi-paragraph `StText`
editing, … managed selection/caret model replacing `VwSelection`, structured document model replacing
`VwRootBox`/box hierarchy." The Sandbox needs a **secondary in-memory presentation cache + decorator
model**, **icon/picture-anchored hit-test editing**, and a **combo/flyout editing harness** — none of
which is named in Stage 9.

### 2b. Interlinear doc is a second, distinct specialized Views construction

`InterlinVc : FwBaseVc` with `Display(IVwEnv)` at `InterlinVc.cs:604`; 287 `IVwEnv` invocations, 70+
`kfrag*` fragments, `OpenMappedPara`/`AddObjVecItems` interleaving of word/morph/gloss/POS lines into
aligned columns. Hosts (`InterlinDocForAnalysis`, `InterlinDocRootSiteBase`, `InterlinTaggingChild`,
`InterlinPrintView`, `RawTextPane`, `TitleContentsPane`, the Discourse `InterlinRibbon`) all derive
from `RootSite`. The aligned word/morpheme grid is a *layout idiom* (multiple WS rows under one
vernacular word, column alignment across the paragraph) that ordinary `TextLayout`/StText editing does
not produce. `InterlinViewDataCache` and the per-pane decorators add another SDA-decorator layer.

### 2c. Discourse splits cleanly — favorable

`ConstituentChartLogic.cs` (5459 lines = ~49% of Discourse) is **pure domain logic** — drag/move/merge,
`MoveToColumn`, `MakeWordGroup`, `InsertRow`, `ToggleMissingMarker` — with essentially no `IVwEnv`/
`IVwRootBox`/`IVwSelection` references (the few hits are comments/event-fire). It can port as-is.
Rendering is confined to `ConstChartVc` (`Display(IVwEnv)` at ~`:225`), `ConstChartBody : RootSite`,
`MakeCellsMethod` (`IVwEnv` table-cell emission), and `ChartRowEnvDecorator : IVwEnv` (RTL buffering —
deletable, Avalonia handles `FlowDirection`). `DiscourseExporter : CollectorEnv` is a view-walk export
(reuses `ConstChartVc.Display`); it should be rewritten as a direct domain→XML exporter to drop the
Views dependency. Net: the chart is a **table** with portable logic, and is the strongest candidate to
ride **Stage 3** (shared editable grid) rather than the Stage 9 document engine.

### 2d. Concordance / statistics / import are low-coupling

`ConcordanceControl`, `ComplexConcControl`, `ConcordanceControlBase`, `StatisticsView` are `UserControl`
hosts with no `IVwEnv`/`IVwRootBox` in the host itself (the result/preview pane re-hosts `InterlinVc`).
These plus the SFM/LinguaLinks/BIRD import wizards are ordinary WinForms dialogs and non-UI importers —
Stage-5-class work that does not need Stage 9 at all.

## 3. Best practices for migrating complex custom-rendered editing surfaces

1. **Spike the Sandbox as its own Stage 9 sub-spike, before committing the engine API.** It exercises
   secondary-cache editing + hit-test combos, which the StText spike (plan line 297) will not surface.
   If the managed engine API is frozen without it, Stage 7 inherits an engine that cannot express the
   Sandbox and stalls late.
2. **Replace the secondary `VwCacheDa` with a managed presentation/decorator model**, not a port of
   `IVwCacheDa`. The architecture already prefers projected presentation models over LCModel/COM
   surfaces (architecture-patterns §2). The Sandbox's SbWord/SbMorph HVO-map is exactly an off-thread
   presentation model with an edit-context seam.
3. **Separate logic from rendering first, prove with characterization tests.** Discourse already did
   this (`ConstituentChartLogic`); do the equivalent extraction for the Sandbox (morpheme-break,
   approve-and-move, choose-analysis) and Interlinear line-choice logic so the hard logic is unit-tested
   independent of the renderer, per checklist Phase 2.
4. **Map the interlinear aligned grid to the Stage-3 owned table / Stage-9 layout primitives explicitly.**
   It is neither a plain table nor a plain paragraph; record which primitive owns column alignment as a
   pivot trigger in `seam-catalog.md`.
5. **Capture legacy timing baselines on the largest texts first.** `InterlinVc` uses lazy/`AddLazyVecItems`
   rendering precisely because texts are large; a non-virtualized managed reimplementation will regress.
6. **Engine-isolation audit (`parity-evidence.md` §4) will flag every one of these surfaces today**
   (`IVwRootBox`, `IVwEnv`, `IVwSelection`, `RootSiteControl`). That is expected; the gate is that the
   *migrated* surface is clean.

## 4. Interactions & dependencies

- **Stage 9 (critical) — currently under-scoped for Stage 7.** Stage 9's deliverables (lines 300–303)
  cover multi-paragraph `StText` + selection/caret + structured doc model. They do **not** name (a) the
  Sandbox secondary-cache/decorator editing model, (b) icon-anchored hit-test combo editing, or (c) the
  interlinear aligned word/morpheme/gloss grid layout. RawTextPane (plain `StText`) is the only Stage-7
  surface Stage 9 as-written clearly covers. **This is the headline cross-stage conflict.**
- **Stage 3 (tables) — correctly relevant, and more so than the plan implies.** The constituent chart
  and the concordance results grid are table surfaces; the chart in particular should be built on the
  Stage-3 owned virtualized grid, not the Stage-9 document engine. The plan lists Stage 3 as a Stage-7
  dependency (line 129) but the prose (line 283) emphasizes Stage 9; rebalance toward Stage 3 for the
  chart + concordance.
- **Stage 5 (dialogs).** The plan already allows splitting import wizards to Stage 5 (line 282). Concur,
  and extend: concordance/statistics host UI are also Stage-5-class. Per decision §11.3, the import
  wizards should use MVVM + compiled bindings (no IR/region machinery — they have no XML layout).
- **Stage 6 / FdoUi.** Interlinear info-pane and the analysis choosers reuse `FwOptionPicker`-style
  field controls already built in Stage 0; reuse, do not rebuild.

## 5. Recommended plan changes

1. **Split Stage 7 into four epics** with distinct leads/dependencies:
   - **7A — Interlinear doc + Sandbox/FocusBox (senior; hard-blocks on extended Stage 9).** The long pole
     of the surface track.
   - **7B — Constituent chart (mid/senior; depends on Stage 3, light Stage 9).** Port
     `ConstituentChartLogic` as-is; rebuild rendering on the Stage-3 grid; rewrite `DiscourseExporter` as
     direct domain→XML.
   - **7C — Concordance + ComplexConc + Statistics + raw-text host (mid; depends on Stage 3 + 9 only for
     the embedded interlinear preview).**
   - **7D — Import wizards: SFM / LinguaLinks / BIRD / Words-SFM (junior; Stage 5 pattern; MVVM).**
2. **Expand Stage 9 scope (lines 292–306) to explicitly name the interlinear/sandbox needs**, or add a
   named Stage 9 sub-spike: (a) secondary in-memory presentation cache + decorator editing model
   replacing `CachePair`/`VwCacheDa`; (b) picture/icon-anchored hit-test editing replacing
   `IVwSelection` scan + `MakeSelAt`; (c) aligned multi-line word/morpheme/gloss grid layout. Without
   this, Stage 9's "Views replacement" claim is incomplete and Stage 7A is mis-gated.
3. **Re-tag the Stage-7 row dependency** in the §4 table from "3,9" to per-sub-epic dependencies, and add
   a risk-register line: "Stage 9 scoped to StText but Interlinear/Sandbox need specialized constructs —
   verify in the Stage 9 spike."
4. **Move 7C/7D off the "depends on 9" critical path** so concordance/statistics/import can ship in
   parallel with the engine work rather than waiting on it.
5. **Add a Stage-7A explicit dependency on the Stage 9 spike output** (not just Stage 9 completion):
   freeze no engine API until the Sandbox spike runs.

## 6. Open questions / risks

- **Does the Stage 9 spike (line 297) include the Sandbox?** As written it lists bidi/IME/CJK/custom-WS/
  multi-paragraph/structured selection — not secondary-cache hit-test editing. If "no," Stage 7A is at
  high risk of discovering a missing engine capability late. **(Top risk.)**
- **Aligned-grid layout ownership:** Stage 3 grid vs. Stage 9 layout — undecided. Interlinear column
  alignment crosses both; needs an explicit decision/pivot record.
- **Tagging / ComplexConcordance pattern VCs** (`InterlinTaggingVc`, `ComplexConcPatternVc` — 121 `IVwEnv`
  calls) are extra specialized VCs not separately budgeted; fold into 7A/7C scope explicitly.
- **Performance on large corpora:** lazy `AddObjVecItems`/`AddLazyVecItems` in `InterlinVc` implies real
  virtualization is mandatory; a naive managed port will regress. Baseline on the largest available text.
- **Print views** (`InterlinPrintView`, chart print/export) overlap Stage 10 (print/preview); confirm
  ownership to avoid a gap.

## 7. Confidence

**Code-coupling characterization: High.** Base classes, `Display(IVwEnv)` signatures, the secondary
`VwCacheDa` cache, hit-test combo editing, and the `ConstituentChartLogic` separation are all confirmed
by direct file/line evidence cited above.

**Stage-9-coverage gap conclusion: High.** Stage 9's written deliverables (lines 300–303) demonstrably
do not name the Sandbox/interlinear-specific constructs the repo shows are load-bearing.

**Effort sizing & exact split boundaries: Medium.** Directionally firm (Sandbox/Interlinear are the long
pole; chart logic is free; concordance/import are Stage-5-class), but precise epic boundaries and the
grid-vs-engine layout decision should be confirmed during the Stage 9 spike.
