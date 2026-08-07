# Stage 6 Review — Lexicon completion + Grammar/Morphology detail

> Reviewer pass over **Stage 6** of `complete-migration-program.md`.
> Scope as written: Lexicon remaining slices/launchers (MSA, references,
> examples); Morphology (inflection features/classes, phonological
> environments, categories); Grammar detail editors via `FdoUi` (POS,
> inflection, phonological features); custom slice classes → plugin registry
> with burn-down tracking. Lead level: **Mid**.
> Grounding: repo inventory of `Src/LexText/Lexicon/`,
> `Src/LexText/LexTextControls/`, `Src/LexText/Morphology/`, `Src/FdoUi/`,
> `Src/Common/Controls/DetailControls/`, and the Stage-0 plugin/owned-control
> machinery in `Src/xWorks/` and `Src/Common/FwAvalonia/`.

## 1. Scope assessment

**Verdict: the epic as written is mis-bundled and under-sized in the wrong
dimension.** It groups three areas that look adjacent ("detail-view-heavy
grammar/lexicon") but have *different dependency profiles*:

- **(A) Lexicon references + MSA/feature launchers** — genuinely reuse the
  Stage-4 region/composer + plugin registry. Several are *already* lane-
  classified and partially absorbed in Stage 0 (see §4). This is the part
  that fits the "mid dev follows the exemplar" framing.
- **(B) Morphology rule/template/environment editors** — these are **not
  detail-field editors at all**. They are bespoke Views-based document
  surfaces (affix templates, phonological rule formulas, environment strings,
  interlinear). They belong with Stage 7/9, not here.
- **(C) `FdoUi` grammar editors (BulkPos/InflectionClass/InflectionFeature/
  PhonologicalFeature)** — these are **bulk-edit-bar controls living in the
  XMLViews browse surface, not the DataTree/region detail surface**. They
  have no place in a "detail" epic; they depend on Stage 3 (the editable grid)
  and the browse-bulk-edit machinery, not on the region composer.

So the epic bundles work that splits cleanly across **three different
substrates** (region composer, document engine, browse/bulk-edit). The "this
is too much for one epic" instinct in the prompt is correct, but the right cut
is **by substrate/dependency, not by headcount**. Recommend splitting (§5).

**One scope nuance the prompt got right:** these *are* detail-heavy, but a
large fraction does **not** reuse the Stage-4 pattern. The honest split is
roughly: a third reuses region/composer + plugins cleanly; a third is bulk-
edit (browse) work; a third is Views-based and Stage-9-blocked.

## 2. Feasibility (repo-grounded)

### Custom slice/launcher census (counts)

| Area | Path | Custom slice/launcher classes |
| --- | --- | --- |
| Lexicon | `Src/LexText/Lexicon/` | ~32 (21 slices + ~14 launchers) |
| Morphology | `Src/LexText/Morphology/` | 14 |
| FdoUi grammar editors | `Src/FdoUi/` | 4 (+1 sibling in Lexicon) |

The shared **base** classes live in
`Src/Common/Controls/DetailControls/` (`Slice.cs`, `ViewSlice.cs`,
`ViewPropertySlice.cs`, `FieldSlice.cs`, `StringSlice.cs`,
`ButtonLauncher.cs`, `ReferenceLauncher.cs`, `AtomicReferenceLauncher.cs`,
`VectorReferenceLauncher.cs`, plus `CustomAtomicReferenceSlice` /
`CustomReferenceVectorSlice`). DetailControls is frozen per the master plan —
these bases are deleted at cutover, never extracted.

### The dominant feasibility fact: native Views reaches deep into the bases

`ViewSlice` (`Src/Common/Controls/DetailControls/ViewSlice.cs`) takes a
`SimpleRootSite` in its constructor and exposes it as `RootSite`.
`ViewPropertySlice` and `StringSlice` derive from it → **transitively
Views-based**. Critically, *even the reference launchers are Views-based at
the base*: `AtomicReferenceLauncher` embeds `AtomicReferenceView` and
`VectorReferenceLauncher` embeds `VectorReferenceView`, both `RootSite`-backed.

This matters because it means the **legacy** reference slices render through
Views — but Stage 0 already proved they do **not** need the Views engine on
the Avalonia path: the region composer renders reference vectors with the
owned `FwReferenceVectorField`
(`Src/Common/FwAvalonia/Region/FwFieldControls.cs`, `RegionFieldKind.ReferenceVector`),
not by hosting a RootSite. So *reference-launcher* Views usage is **not** a
Stage-9 dependency — it is replaced by owned controls. Confirmed by Stage 0's
burn-down classifying `EntrySequenceReferenceSlice`, `GhostLexRefSlice`, and
`LexReferenceMultiSlice` as **LaneAbsorbed** (D3) into the composer.

### What *is* genuinely Stage-9-blocked

These embed a Views **document** surface (a `RootSiteControl`/`XmlView`/
`PatternView` rendering rich structured content), which the region's owned
controls do **not** cover:

- Lexicon: `ReversalIndexEntrySlice` (already deferred to **gate 6.13** in the
  Stage-0 burn-down), `MSADlgLauncherSlice` / `MsaInflectionFeatureListDlgLauncherSlice`
  / `PhonologicalFeatureListDlgLauncherSlice` — *but* these last three are
  already handled as **LauncherRouted (D4)** plugins (value-preview + dialog),
  so their Views preview is replaced by a text value, not by a hosted engine.
- Morphology: `InflAffixTemplateSlice` (`XmlView`), `InterlinearSlice`
  (`AnalysisInterlinearRs : RootSite`), `RuleFormulaSlice` +
  `AffixRuleFormulaSlice`/`MetaRuleFormulaSlice`/`RegRuleFormulaSlice`
  (`PatternView : RootSiteControl`), `PhEnvStrRepresentationSlice`
  (`StringRepSliceView : RootSiteControl` with a custom VC and inline
  environment validation). These are **4 distinct document-editor families,
  all hard-blocked on Stage 9** — they are not field launchers and have no
  owned-control equivalent today.

### What reuses Stage 4 cleanly (low risk, mid-appropriate)

- The **lexical-reference family** (~9 slices: `LexReferenceMultiSlice` parent
  + Pair/Collection/Sequence/Unidirectional/TreeBranches/TreeRoot variants +
  `EntrySequenceReferenceSlice`) — vector/atomic reference editing the
  composer + `FwReferenceVectorField` + `FwOptionPicker` already model. The
  one wrinkle: `LexReferenceMultiSlice` **dynamically generates child slices**
  by reference type and owns add/delete/edit context menus — the composer must
  reproduce that fan-out, which is real work but is region-pattern work.
- The **MSA / inflection-feature / phonological-feature launchers** — already
  routed via `LauncherRegionPlugin`/`DialogLauncherPlugins`
  (`Src/xWorks/RegionEditorPlugins.cs`, `DialogLauncherPlugins.cs`). Stage 6
  inherits a working pattern; remaining work is the missing
  `FwDialogLauncherField` owned control (referenced but **not yet a
  first-class `RegionFieldKind`** — see §3) and the actual chooser dialogs.
- `BasicIPASymbolSlice` (`StringSlice` + auto-population) — a text field with a
  side effect; fits an owned text control + edit-session hook.

### FdoUi bulk editors — feasible but wrong-substrate

`BulkPosEditor`/`BulkPosEditorBase`, `InflectionClassEditor`,
`InflectionFeatureEditor`, `PhonologicalFeatureEditor` (and
`BulkReversalEntryPosEditor` in Lexicon) all implement `IBulkEditSpecControl`
and embed a WinForms `TreeCombo` (no Views, no chooser dialog — inline popup
tree). **No Stage-9 dependency.** But they are bulk-edit-bar controls on the
**browse** surface, so they depend on Stage 3 (editable grid) + a migrated
bulk-edit bar, not the region composer. They should not gate or live inside a
"detail" epic.

## 3. Best practices

- **Census-first + burn-down is already a hard gate.** Stage 0 ships
  `LexemeEditorBurnDownTests` (`Src/xWorks/xWorksTests/`) which parses
  `class=`/`assemblyPath=` from the layout XML and forces *every* custom slice
  into exactly one lane (PluginRouted / CompanionDesignated / LauncherRouted /
  ExplicitlyDeferred / LaneAbsorbed). **Stage 6 must extend this same census to
  the Morphology and Grammar layout files** (not invent new tracking). The
  prompt's "burn-down tracking" line should point at this existing test.
- **Reuse the plugin contract verbatim.** `IRegionEditorPlugin`
  (`LegacyClassName` + `BuildControl(RegionEditorBuildContext)`) and
  `RegionEditorPluginRegistry.Register/Resolve/RegisteredClassNames` are the
  add-a-slice contract (architecture-patterns.md §5). New Stage-6 plugins key
  off legacy class identity → zero layout edits.
- **Promote `FwDialogLauncherField` to a real owned control / `RegionFieldKind`.**
  Today it is plugin-delivered (`DialogLauncherPlugins.cs`) and *not* in the
  Stage-0 `RegionFieldKind` vocabulary. Stage 6 multiplies launcher usage
  (MSA, features, allomorph/MSA ad-hoc links). This is a Stage-1/Stage-4
  platform gap that Stage 6 will hit immediately — flag it as a prerequisite.
- **Explicit "unsupported" rows over silent fallback** for the Stage-9-blocked
  document editors — never host a hidden RootSite (active-host contract).
- Per-surface Definition of Done (§7 of the plan) applies unchanged:
  100%/150% DPI, Path-3 bundle, `EngineIsolationAuditTests` green (which will
  *catch* any accidental Views/`RootSite` symbol in a migrated row).

## 4. Interactions & dependencies

- **Hard dep on Stage 4 (exemplar) and the plugin registry** — correct as
  drawn (`S4 → S6`). The reference-vector and launcher lanes are literally the
  Stage-0/Stage-4 machinery.
- **Undeclared hard dep on Stage 9.** The plan shows Stage 6 depending only on
  Stage 4, but the Morphology rule/template/environment/interlinear editors
  (4 families) and Lexicon `ReversalIndexEntrySlice` are Views-document
  surfaces with **no owned-control path** — they are blocked on the Stage-9
  managed document engine exactly like Stage 7. The current graph
  (`S9 → S7` only) **misses `S9 → S6`**. This is the single biggest plan
  error for this stage.
- **Undeclared dep on Stage 3 + browse-bulk-edit.** The `FdoUi` editors are
  bulk-edit-bar controls; they need the Stage-3 editable grid and a migrated
  bulk-edit bar. No `S3 → S6` edge exists today.
- **Overlap with Stage 5 (dialogs).** Confirmed and significant: the MSA,
  feature, phonological-feature, and ad-hoc-coprohib launchers all *open
  dialogs* (`MsaCreatorDlg`, `PhonologicalFeatureChooserDlg`,
  `MsaInflectionFeatureListDlg`, `LinkAllomorphDlg`, `LinkMSADlg`,
  `LinkEntryOrSenseDlg`). The launcher *row* is Stage-6 region work; the
  *dialog content* is Stage-5 work. These must be sequenced so the dialog
  exists (or stays a WinForms-owned modal per `dialog-ownership.md`) before the
  launcher row claims parity. Add an explicit `S5 → S6` (launcher-dialog)
  dependency.
- **Within-stage dep:** `CustomReferenceVectorSlice`/`CustomAtomicReferenceSlice`
  bases are shared by both Lexicon refs and Morphology ad-hoc-coprohib slices —
  one composer mapping serves both.

## 5. Recommended plan changes

1. **Split Stage 6 by substrate into three deliverables:**
   - **6a — Lexicon detail completion (mid, region/composer):** lexical
     references (the ~9-slice family incl. `LexReferenceMultiSlice` fan-out),
     MSA + inflection-feature + phonological-feature launcher rows, examples,
     `BasicIPASymbolSlice`. Depends on Stage 4 + Stage 5 (launcher dialogs).
   - **6b — Morphology + grammar document editors (senior, Stage-9-blocked):**
     affix templates, rule formulas (×4), phonological environment strings,
     interlinear slice, reversal-index entry. **Re-parent under / sequence
     after Stage 9**; this is effectively Stage-7-class work and should not be
     "mid."
   - **6c — Grammar bulk editors (mid, browse/bulk-edit):** the four `FdoUi`
     `IBulkEditSpecControl` editors + `BulkReversalEntryPosEditor`. **Depends
     on Stage 3** and the bulk-edit-bar migration; move to Stage 8 (browse/
     bulk-edit reservoir) or make it the first browse-bulk-edit slice.
2. **Add missing dependency edges to §5 graph:** `S9 → S6` (document editors),
   `S3 → S6` (bulk editors), `S5 → S6` (launcher dialogs).
3. **Down-grade lead level** for the document-editor portion from Mid to
   Senior (it is Views-engine work).
4. **Make "burn-down tracking" concrete:** extend
   `LexemeEditorBurnDownTests` census to the Grammar/Morphology layout XML so
   every Morphology/FdoUi custom class is lane-classified before work starts.
5. **Add a platform prerequisite:** promote `FwDialogLauncherField` to a
   first-class `RegionFieldKind`/owned control in Stage 1 or Stage 4 before
   Stage 6 scales launcher rows.
6. **Correct the scope sentence** in the plan: replace "Detail-view-heavy areas
   that reuse region/composer + plugin registry directly" — that is true only
   for ~1/3 of the listed work.

## 6. Open questions / risks

- **Stage-9 timing dominates 6b.** If Stage 9 slips, the Morphology rule/
  template/environment/interlinear editors and reversal-index entry cannot
  reach parity — they can only render explicit "unsupported" rows. Risk: the
  epic looks "mostly done" (references migrated) while the linguistically
  critical editors are blocked. Mitigation: split per §5 so 6a can close
  independently and 6b tracks Stage 9 honestly.
- **`LexReferenceMultiSlice` dynamic child-slice generation** is the highest-
  complexity *region-pattern* item: the composer must reproduce per-reference-
  type slice fan-out + add/delete/edit context menus. Needs a characterization
  baseline before refactor.
- **Phonological-environment validation** (`PhEnvStrRepresentationSlice` does
  inline slash/bar/error checking) — does this move to the `IValidationService`
  seam, or stay inside the document editor? Decide during the Stage-9 spike.
- **Bulk-editor "fill-in-the-blanks" semantics** (`InflectionFeatureEditor`
  pattern-matches values from the current sense) and
  `PhonologicalFeatureEditor`'s cross-control `EnableTargetFeatureCombo` event
  are non-trivial bulk-edit behaviors to characterize before grid migration.
- **TreeCombo replacement.** All four FdoUi editors depend on WinForms
  `TreeCombo`+`PopupTreeManager`; the Avalonia equivalent (a bounded popup-tree
  combo) is not yet in the owned-control set — confirm `FwOptionPicker` covers
  the popup-tree case or schedule a new owned control.

## 7. Confidence

**High** on the central findings: the custom-class census and base-class
Views-dependency facts are read directly from the repo; the bulk editors are
unambiguously browse/`IBulkEditSpecControl` controls; the Stage-0 burn-down
and plugin contract are confirmed in code. **High** that the epic is
mis-bundled across three substrates and that `S9 → S6` / `S3 → S6` edges are
missing. **Medium** on exact effort sizing of `LexReferenceMultiSlice` fan-out
and on whether every "examples" slice is already lane-absorbed (examples were
not separately enumerated in the inventory and may overlap the
already-migrated Stage-4 entry surface). **Medium** on whether the
feature-launcher dialogs are scheduled in Stage 5 or implicitly here.
