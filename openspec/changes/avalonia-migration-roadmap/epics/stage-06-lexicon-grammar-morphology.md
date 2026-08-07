# Stage 6 — Lexicon completion + Grammar/Morphology detail (Epic draft)

> JIRA-ready epic + sub-epic draft for **Stage 6** of the FieldWorks → Avalonia migration program.
> Source of truth: `complete-migration-program.md` (Stage 6 detail + §7 Definition of Done + §10 labels),
> `reviews/stage-06-lexicon-grammar-morphology.md`, and `reviews/00-cross-comparison-synthesis.md` (§3, §6, §7).
>
> **Headline from review:** the stage as originally scoped is *mis-bundled across three different substrates*
> (region/composer detail, Views-based document editors, browse bulk-editors). The right cut is **by
> substrate/dependency, not by headcount**. This draft splits Stage 6 into **6a / 6b / 6c**, re-parents **6b
> under Stage 9** (Views-based document surfaces) and routes **6c into Stage 8 / browse** (Stage-3-gated),
> and adds the missing dependency edges `S9 → S6`, `S3 → S6`, `S5 → S6`.

---

## Epic

**Summary:** Stage 6 — Lexicon completion + Grammar/Morphology detail

**Type:** Epic

**Labels:** `track-surfaces`, `lead-mid`, `parallel-safe`,
`parity-blocked-by:region-composer`, `parity-blocked-by:stage-9-document-engine`,
`parity-blocked-by:stage-3-grid`

**Description**

Bring the remaining **Lexicon** and **Grammar/Morphology** detail surfaces to parity on the Avalonia
region/composer + plugin-registry machinery proven by Stage 4. As written in the program plan this is
"detail-view-heavy areas that reuse region/composer + plugin registry directly" — but a repo-grounded review
found that is true only for roughly **one third** of the listed work. The work splits cleanly across three
substrates with different dependency profiles, so this epic is decomposed into three sub-epics:

- **6a — Lexicon detail completion** *(mid; region/composer + plugin registry)* — the part that genuinely
  follows the Stage-4 exemplar. Lexical-reference family, MSA / inflection-feature / phonological-feature
  launcher rows, examples, `BasicIPASymbolSlice`.
- **6b — Morphology / grammar document editors** *(senior; **re-parented under Stage 9**)* — affix templates,
  rule formulas, phonological environment strings, interlinear, reversal-index entry. These are **Views-based
  document surfaces**, not detail-field editors, with no owned-control path today; they are blocked on the
  Stage-9 managed document engine exactly like Stage 7.
- **6c — FdoUi grammar bulk editors** *(mid; → Stage 8 / browse, Stage-3-gated)* — the four `FdoUi`
  `IBulkEditSpecControl` editors (+ `BulkReversalEntryPosEditor`). These are **bulk-edit-bar controls on the
  XMLViews browse surface**, not detail/region work; they depend on Stage 3 (editable grid) + a migrated
  bulk-edit bar.

The legacy DetailControls slice bases (`Slice`/`ViewSlice`/`ViewPropertySlice`/`FieldSlice`/`StringSlice` +
the reference launchers in `Src/Common/Controls/DetailControls/`) are **frozen** — they are deleted at cutover
(Stage 13), never extended. New work keys off legacy class identity through the plugin registry
(`IRegionEditorPlugin.LegacyClassName` + `RegionEditorPluginRegistry`), so it touches zero layout XML.

**Custom-slice census (story-generation guidance, from the Stage 6 review):**

| Area | Path | Custom slice/launcher classes |
| --- | --- | --- |
| Lexicon | `Src/LexText/Lexicon/`, `Src/LexText/LexTextControls/` | **~32** (21 slices + ~14 launchers) |
| Morphology | `Src/LexText/Morphology/` | **14** |
| FdoUi grammar editors | `Src/FdoUi/` | **4** (+1 sibling, `BulkReversalEntryPosEditor`, in Lexicon) |

Each census class becomes a candidate story under the appropriate sub-epic, lane-classified by the
burn-down test before work starts (see acceptance criteria).

**Acceptance criteria**

- Stage 6 is decomposed into 6a / 6b / 6c (below); 6b is re-parented under Stage 9 and 6c is routed to
  Stage 8 / browse — this epic owns **6a** outright and tracks 6b/6c as cross-references.
- Dependency edges `S9 → S6`, `S3 → S6`, `S5 → S6` are recorded in the program graph (§5) and on the relevant
  sub-epics.
- `FwDialogLauncherField` is promoted to a first-class `RegionFieldKind` / owned control (platform
  prerequisite — see Notes) before launcher rows scale.
- The `LexemeEditorBurnDownTests` census (`Src/xWorks/xWorksTests/`) is **extended to the Morphology and
  Grammar layout XML** so every Morphology/FdoUi custom class is lane-classified
  (PluginRouted / CompanionDesignated / LauncherRouted / ExplicitlyDeferred / LaneAbsorbed) before its story
  starts. "Burn-down tracking" means *this existing test*, not new tracking.
- Every surface-migrating story satisfies the per-surface **Definition of Done** (§7 of the program plan):
  census taken; semantic+visual+workflow+performance baselines captured before refactor and matched after;
  seams reused from `ISeams.cs`; owned-control choices per `architecture-patterns.md` §4; composer walks
  compiled IR with stable AutomationIds; explicit `HostUiBehavior`; Path-3 bundle per scenario with perf
  ≤ legacy × 1.2 at **100% + 150% DPI**; localization lanes correct; `EngineIsolationAuditTests` + active-host
  contract tests green; `./build.ps1` + `./test.ps1` green; retrospective folded into the skill set in the
  **same** PR.
- Evidence-language enforced: any checked task whose evidence says *substitute / placeholder / skipped /
  future / partial* is a review blocker.

**Dependencies**

- **Blocked by:** Stage 4 (exemplar — region/composer + plugin registry; the reference-vector and launcher
  lanes are literally the Stage-0/Stage-4 machinery).
- **Blocked by (newly added):** Stage 9 (managed document engine — for 6b document editors + reversal-index
  entry), Stage 3 (editable grid — for 6c bulk editors), Stage 5 (launcher dialog bodies — for 6a launcher
  rows).
- **Blocks:** Stage 11 (shell switch consumes migrated Lexicon/Grammar surfaces).

**Rough size:** Large epic, but only **6a** is mid-owned and closes independently. 6b is Stage-9-class senior
work that tracks Stage 9 timing; 6c is browse/bulk-edit work that lands in Stage 8. Estimate ~32 Lexicon +
14 Morphology + 5 FdoUi = ~51 candidate custom classes across the three sub-epics, of which roughly a third
(the 6a region-pattern work) is the only part schedulable now.

---

## Sub-epics / stories

### 6a — Lexicon detail completion *(mid; region/composer)*

**Summary:** 6a — Lexicon detail completion on region/composer + plugin registry

**Type:** Sub-epic (Story group)

**Description**

The part of Stage 6 that genuinely reuses the Stage-4 exemplar. Migrate the remaining Lexicon detail rows to
the Avalonia region/composer using owned controls and launcher plugins:

- **Lexical-reference family (~9 slices):** `LexReferenceMultiSlice` parent + Pair / Collection / Sequence /
  Unidirectional / TreeBranches / TreeRoot variants + `EntrySequenceReferenceSlice` /
  `GhostLexRefSlice`. Vector/atomic reference editing the composer already models with
  `FwReferenceVectorField` (`RegionFieldKind.ReferenceVector`) + `FwOptionPicker`. These are already
  classified **LaneAbsorbed (D3)** by the Stage-0 burn-down. **Highest-complexity region-pattern item:**
  `LexReferenceMultiSlice` *dynamically generates child slices by reference type* and owns add/delete/edit
  context menus — the composer must reproduce that fan-out (real work, but region-pattern work). Needs a
  characterization baseline before refactor.
- **MSA / inflection-feature / phonological-feature launcher rows:** already routed via
  `LauncherRegionPlugin` / `DialogLauncherPlugins` (`Src/xWorks/RegionEditorPlugins.cs`,
  `DialogLauncherPlugins.cs`), classified **LauncherRouted (D4)**. Remaining work is the missing
  `FwDialogLauncherField` owned control (see Notes) plus sequencing the chooser dialog bodies behind Stage 5.
- **`BasicIPASymbolSlice`** (`StringSlice` + auto-population): owned text control + edit-session side-effect
  hook.
- **Examples** slices/launchers (may partly overlap the already-migrated Stage-4 entry surface — confirm
  during census; medium-confidence item).

Custom-class census guidance: **~32** Lexicon classes (21 slices + ~14 launchers) — generate one story per
class, lane-classified first.

**Acceptance criteria (ref §7 DoD)**

- Census of all ~32 Lexicon custom classes taken and lane-classified via the extended
  `LexemeEditorBurnDownTests` before work; plugins exist or explicit "unsupported" rows render (never silent
  fallback).
- `LexReferenceMultiSlice` fan-out reproduced by the composer with a captured characterization baseline
  (semantic + visual + workflow) matched after refactor.
- Launcher rows render via the promoted `FwDialogLauncherField` `RegionFieldKind`; the launcher *dialog body*
  exists (Stage 5) or stays a WinForms-owned modal per `dialog-ownership.md` before the row claims parity.
- Full §7 per-surface DoD per row: Path-3 bundle per scenario, perf ≤ legacy × 1.2 at 100% + 150% DPI, stable
  AutomationIds from StableId, localization lanes correct, `EngineIsolationAuditTests` green (catches any
  accidental `RootSite`/Views symbol), `./build.ps1` + `./test.ps1` green, retrospective in same PR.

**Dependencies:** Stage 4 (exemplar) — hard; Stage 5 (launcher dialog bodies) — `S5 → S6`. Platform
prerequisite: `FwDialogLauncherField` promoted to `RegionFieldKind`.

**Labels:** `track-surfaces`, `lead-mid`, `parallel-safe`, `parity-blocked-by:region-composer`,
`parity-blocked-by:stage-5-dialogs`

**Rough size:** Medium. The bulk reuses the Stage-4 machinery; `LexReferenceMultiSlice` fan-out is the one
genuinely non-trivial item. Mid-appropriate, the part of Stage 6 schedulable immediately after Stage 4.

---

### 6b — Morphology / grammar document editors *(senior; re-parent under Stage 9)*

**Summary:** 6b — Morphology + grammar Views-based document editors (gated on Stage 9)

**Type:** Sub-epic (Story group) — **re-parented under / sequenced after Stage 9**

**Description**

These are **not detail-field editors**. They embed a Views **document** surface (a
`RootSiteControl` / `XmlView` / `PatternView` rendering rich structured content) that the region's owned
controls do **not** cover. They are four distinct document-editor families, all hard-blocked on the Stage-9
managed document engine — effectively Stage-7-class work, and should **not** be "mid":

- `InflAffixTemplateSlice` (`XmlView`) — affix templates.
- `RuleFormulaSlice` + `AffixRuleFormulaSlice` / `MetaRuleFormulaSlice` / `RegRuleFormulaSlice`
  (`PatternView : RootSiteControl`) — phonological/morphological rule formulas (×4).
- `PhEnvStrRepresentationSlice` (`StringRepSliceView : RootSiteControl`, custom VC + inline environment
  validation) — phonological environment strings.
- `InterlinearSlice` (`AnalysisInterlinearRs : RootSite`) — interlinear analysis.
- **Lexicon** `ReversalIndexEntrySlice` — already deferred to **gate 6.13** in the Stage-0 burn-down; same
  Views-document profile, so it tracks here.

Until Stage 9 lands the constructs these need, they can only render **explicit "unsupported" rows** — never a
hidden hosted `RootSite` (active-host contract). Lead level **downgraded Mid → Senior** because this is
Views-engine work.

Custom-class census guidance: **14** Morphology classes — the document-editor families above plus their
companions; lane-classify all 14 first (the ones not in the families likely land in 6a or as `ExplicitlyDeferred`).

**Acceptance criteria (ref §7 DoD)**

- All 14 Morphology custom classes lane-classified via the extended `LexemeEditorBurnDownTests` before work;
  the four document-editor families + `ReversalIndexEntrySlice` marked **ExplicitlyDeferred** until Stage 9
  proves the required constructs.
- Stage-9 engine seam confirmed to cover affix-template `XmlView`, `PatternView` rule formulas, and the
  environment-string custom VC **before** these stories leave "deferred" (verify against the Stage-9.0 spike
  output).
- While deferred, surfaces render explicit "unsupported" rows; `EngineIsolationAuditTests` stays green (no
  accidental `RootSite`/Views symbol on the Avalonia path).
- When unblocked, full §7 per-surface DoD per family: characterization baseline before refactor, Path-3
  bundle, perf ≤ legacy × 1.2 at 100% + 150% DPI, one global undo stack, retrospective in same PR.
- **Open decision captured:** whether `PhEnvStrRepresentationSlice`'s inline slash/bar/error validation moves
  to the `IValidationService` seam or stays inside the document editor — decided during the Stage-9 spike.

**Dependencies:** **Stage 9** (managed document engine) — hard, `S9 → S6` (the single biggest graph error in
the original plan); Stage 4 (region scaffolding for the non-document companions).

**Labels:** `track-surfaces`, `lead-senior`, `parity-blocked-by:stage-9-document-engine`

**Rough size:** Large / senior, but the schedule is **owned by Stage 9 timing**, not by this sub-epic. Risk:
the parent epic can look "mostly done" (6a references migrated) while these linguistically critical editors
are still blocked — track honestly against Stage 9.

---

### 6c — FdoUi grammar bulk editors *(mid; → Stage 8 / browse, Stage-3-gated)*

**Summary:** 6c — FdoUi grammar bulk-edit-bar editors (route to Stage 8 / browse)

**Type:** Sub-epic (Story group) — **routed to Stage 8 (browse / bulk-edit reservoir)**

**Description**

`BulkPosEditor` / `BulkPosEditorBase`, `InflectionClassEditor`, `InflectionFeatureEditor`,
`PhonologicalFeatureEditor` (+ `BulkReversalEntryPosEditor` in Lexicon) all implement `IBulkEditSpecControl`
and embed a WinForms `TreeCombo` (inline popup tree — **no Views, no chooser dialog → no Stage-9 dependency**).
But they are **bulk-edit-bar controls on the XMLViews browse surface**, not the DataTree/region detail
surface. They have no place in a "detail" epic: they depend on **Stage 3** (the editable virtualized grid) +
a migrated **bulk-edit bar**, not on the region composer. Route to **Stage 8** (or make them the first
browse-bulk-edit slice).

Custom-class census guidance: **4** FdoUi editors + 1 Lexicon sibling = **5** classes.

**Acceptance criteria (ref §7 DoD)**

- 5 `IBulkEditSpecControl` classes lane-classified and confirmed wrong-substrate (browse, not detail);
  relocated under Stage 8 with an `S3 → S6`/`S3 → S8` dependency recorded.
- Non-trivial bulk-edit behaviors characterized before grid migration: `InflectionFeatureEditor`'s
  "fill-in-the-blanks" value pattern-matching from the current sense; `PhonologicalFeatureEditor`'s
  cross-control `EnableTargetFeatureCombo` event.
- `TreeCombo` + `PopupTreeManager` replacement decided: confirm `FwOptionPicker` covers the bounded
  popup-tree-combo case, or schedule a new owned control (open item — see Notes).
- Full §7 per-surface DoD once on the Stage-3 grid: Path-3 bundle, perf ≤ legacy × 1.2 at 100% + 150% DPI,
  `EngineIsolationAuditTests` green, retrospective in same PR.

**Dependencies:** **Stage 3** (editable grid) — hard, `S3 → S6`; the Stage-8 bulk-edit-bar migration.

**Labels:** `track-surfaces`, `lead-mid`, `parity-blocked-by:stage-3-grid`

**Rough size:** Small / medium (5 classes), but **does not belong in Stage 6** — sized and scheduled under
Stage 8. Listed here only to record the route-out and the dependency edge.

---

## Notes / open questions

- **6b re-parenting under Stage 9 (the central restructuring).** The original plan graph showed Stage 6
  depending only on Stage 4. The four Morphology document-editor families (`InflAffixTemplateSlice`,
  `RuleFormulaSlice` ×4, `PhEnvStrRepresentationSlice`, `InterlinearSlice`) and Lexicon
  `ReversalIndexEntrySlice` are Views-document surfaces with **no owned-control path** — blocked on the
  Stage-9 managed document engine exactly like Stage 7. `S9 → S6` is *"the single biggest plan error for this
  stage."* These should be **re-parented under / sequenced after Stage 9**, lead level **downgraded Mid →
  Senior**. Whether 6b lives as a Stage-6 sub-epic that *links to* Stage 9 or is physically moved into the
  Stage 9 epic family is a JIRA-structure call for the program owner; this draft keeps it as a Stage-6
  sub-epic with a hard `S9 → S6` block so the Lexicon (6a) work can close independently.
- **Platform prerequisite — `FwDialogLauncherField` → `RegionFieldKind`.** Today it is plugin-delivered
  (`DialogLauncherPlugins.cs`) and *not* in the Stage-0 `RegionFieldKind` vocabulary. Stage 6 multiplies
  launcher usage (MSA, inflection feature, phonological feature, allomorph/MSA ad-hoc links), so this gap is
  hit immediately. Promote it to a first-class owned control / `RegionFieldKind` in **Stage 1 or Stage 4**
  before 6a scales launcher rows.
- **Missing dependency edges to add to the program §5 graph:** `S9 → S6` (document editors), `S3 → S6`
  (bulk editors), `S5 → S6` (launcher dialogs). The launcher *row* is Stage-6 region work; the launcher
  *dialog content* (`MsaCreatorDlg`, `PhonologicalFeatureChooserDlg`, `MsaInflectionFeatureListDlg`,
  `LinkAllomorphDlg`, `LinkMSADlg`, `LinkEntryOrSenseDlg`) is Stage-5 work — sequence so the dialog exists
  (or stays a WinForms-owned modal) before the launcher row claims parity.
- **Burn-down is an existing hard gate, not new tracking.** Extend `LexemeEditorBurnDownTests`
  (`Src/xWorks/xWorksTests/`) — which parses `class=`/`assemblyPath=` from layout XML and forces every custom
  slice into one of five lanes — to the Morphology and Grammar layout files. This makes "burn-down tracking"
  concrete.
- **Reference launchers are *not* a Stage-9 dependency.** Even though the legacy reference slices render
  through Views at the base (`AtomicReferenceLauncher`/`VectorReferenceLauncher` embed `RootSite`-backed
  views), Stage 0 already proved the region composer replaces them with the owned `FwReferenceVectorField` —
  no hosted RootSite. Do not let the base-class Views coupling pull the reference family into 6b.
- **Open: `TreeCombo` / popup-tree-combo owned control (6c).** All four FdoUi editors depend on WinForms
  `TreeCombo` + `PopupTreeManager`; confirm `FwOptionPicker` covers the bounded popup-tree case or schedule a
  new owned control before the Stage-8 bulk-edit work.
- **Open: `PhEnvStrRepresentationSlice` validation placement (6b).** Inline environment validation →
  `IValidationService` seam vs. inside the document editor — decide during the Stage-9 spike.
- **Confidence (from review):** High on the census counts, the base-class Views facts, the bulk-editor
  wrong-substrate finding, and the missing `S9 → S6` / `S3 → S6` edges. Medium on exact `LexReferenceMultiSlice`
  fan-out sizing and on whether every "examples" slice is already Stage-4 lane-absorbed.
